using SoundStudio.Event;
using strange.extensions.context.api;
using strange.extensions.dispatcher.eventdispatcher.api;
using System;
using System.Collections;
using UnityEngine;

namespace SoundStudio.Service
{
	public class AssetBundleDownloadService : IDisposable
	{
		private const float TIMER_INTERVAL_SECONDS = 1f;

		private MonoBehaviour root;

		private WWW www;

		private bool lastBundleSuccess;

		private Timer timer;

		[Inject(ContextKeys.CONTEXT_DISPATCHER)]
		public IEventDispatcher dispatcher
		{
			get;
			set;
		}

		[Inject(ContextKeys.CONTEXT_VIEW)]
		public GameObject contextView
		{
			get;
			set;
		}

		[PostConstruct]
		public void Initialize()
		{
			dispatcher.AddListener(SoundStudioEvent.APPLICATION_QUIT, OnApplicationQuit);
			if (root == null)
			{
				root = contextView.GetComponent<SoundStudioRoot>();
			}
			if (root == null)
			{
				throw new InvalidOperationException("The root was not set!");
			}
		}

		private void OnWatchDogTimerTick()
		{
			try
			{
				if (www != null)
				{
					if (string.IsNullOrEmpty(www.error))
					{
						dispatcher.Dispatch(SoundStudioEvent.GET_ASSET_BUNDLE_PROGRESS, www.progress);
					}
					else
					{
						dispatcher.Dispatch(SoundStudioEvent.GET_ASSET_BUNDLE_FAILED);
						Dispose();
						timer.Stop();
					}
				}
			}
			catch (Exception)
			{
			}
		}

		private void OnApplicationQuit(IEvent payload)
		{
			dispatcher.RemoveListener(SoundStudioEvent.APPLICATION_QUIT, OnApplicationQuit);
			if (!lastBundleSuccess)
			{
				Caching.ClearCache();
			}
			Dispose();
		}

		public void DownloadBundle(string bundleURL, int version)
		{
			try
			{
				timer = new Timer(1f, true, delegate
				{
					OnWatchDogTimerTick();
				});
				root.StartCoroutine(timer.Start());
				root.StartCoroutine(DownloadAndCache(bundleURL, version));
			}
			catch (Exception)
			{
			}
		}

		private IEnumerator DownloadAndCache(string bundleURL, int version)
		{
			lastBundleSuccess = false;
			while (!Caching.ready)
			{
				yield return null;
			}
			if (Caching.IsVersionCached(bundleURL, version))
			{
				dispatcher.Dispatch(SoundStudioEvent.ASSET_BUNDLE_CACHED);
			}
			else
			{
				dispatcher.Dispatch(SoundStudioEvent.GET_ASSET_BUNDLE_STARTED);
			}
			DateTime startTime = DateTime.Now;
			using (www = WWW.LoadFromCacheOrDownload(bundleURL, version))
			{
				if (www != null)
				{
					if (!www.isDone)
					{
						dispatcher.Dispatch(SoundStudioEvent.GET_ASSET_BUNDLE_PROGRESS, www.progress);
						yield return www;
					}
					if (!string.IsNullOrEmpty(www.error))
					{
						dispatcher.Dispatch(SoundStudioEvent.GET_ASSET_BUNDLE_FAILED);
					}
					else if (www.assetBundle != null)
					{
						lastBundleSuccess = true;
						dispatcher.Dispatch(SoundStudioEvent.GET_ASSET_BUNDLE_PROGRESS, www.progress);
						dispatcher.Dispatch(SoundStudioEvent.GET_ASSET_BUNDLE_SUCCESS, www.assetBundle);
					}
					else
					{
						dispatcher.Dispatch(SoundStudioEvent.GET_ASSET_BUNDLE_FAILED);
					}
					DateTime endTime = DateTime.Now;
					TimeSpan timeSpan = endTime - startTime;
					timer.Stop();
				}
			}
			www = null;
		}

		public void Dispose()
		{
			if (www != null)
			{
				www.Dispose();
			}
		}
	}
}
