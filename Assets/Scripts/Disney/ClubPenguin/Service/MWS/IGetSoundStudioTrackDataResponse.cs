using Disney.ClubPenguin.Service.MWS.Domain;
using Disney.HTTP.Client;

namespace Disney.ClubPenguin.Service.MWS
{
	public interface IGetSoundStudioTrackDataResponse : IHTTPResponse
	{
		SoundStudioTrackData SoundStudioTrackData { get; }
	}
}
