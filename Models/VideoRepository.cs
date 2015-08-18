using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Office365.Discovery;
using Microsoft.Office365.SharePoint.CoreServices;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Newtonsoft.Json;
using Office365Video.Models.JsonHelpers;

namespace Office365Video.Models
{
    public class VideoRepository
    {

        private static string sharePointAccessToken;
        private static string sharePointServiceEndpointUri;
        private static string videoPortalUrl;

        public async Task<string> GetAccessTokenForResource(string resource)
        {
            string token = null;

            //first try to get the token silently
            WebAccountProvider aadAccountProvider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.windows.net");
            WebTokenRequest webTokenRequest = new WebTokenRequest(aadAccountProvider, String.Empty, App.Current.Resources["ida:ClientID"].ToString(), WebTokenRequestPromptType.Default);
            webTokenRequest.Properties.Add("authority", "https://login.windows.net");
            webTokenRequest.Properties.Add("resource", resource);
            WebTokenRequestResult webTokenRequestResult = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(webTokenRequest);
            if (webTokenRequestResult.ResponseStatus == WebTokenRequestStatus.Success)
            {
                WebTokenResponse webTokenResponse = webTokenRequestResult.ResponseData[0];
                token = webTokenResponse.Token;
            }
            else if (webTokenRequestResult.ResponseStatus == WebTokenRequestStatus.UserInteractionRequired)
            {
                //get token through prompt
                webTokenRequest = new WebTokenRequest(aadAccountProvider, String.Empty, App.Current.Resources["ida:ClientID"].ToString(), WebTokenRequestPromptType.ForceAuthentication);
                webTokenRequest.Properties.Add("authority", "https://login.windows.net");
                webTokenRequest.Properties.Add("resource", resource);
                webTokenRequestResult = await WebAuthenticationCoreManager.RequestTokenAsync(webTokenRequest);
                if (webTokenRequestResult.ResponseStatus == WebTokenRequestStatus.Success)
                {
                    WebTokenResponse webTokenResponse = webTokenRequestResult.ResponseData[0];
                    token = webTokenResponse.Token;
                }
            }

            return token;
        }

        public async Task<string> GetVideoPortalHubUrl()
        {
            try
            {
                var requestUrl = String.Format("{0}/VideoService.Discover", await GetSharePointServiceEndpointUri());

                Func<HttpRequestMessage> requestCreator = () =>
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                    request.Headers.Add("Accept", "application/json;odata=verbose");
                    return request;
                };

                var httpClient = new HttpClient();

                var response = await SendRequestAsync(sharePointAccessToken, sharePointServiceEndpointUri, httpClient, requestCreator);

                string responseString = await response.Content.ReadAsStringAsync();

                var jsonResponse = JsonConvert.DeserializeObject<VideoServiceDiscovery>(responseString);

                videoPortalUrl = jsonResponse.Data.VideoPortalUrl;

                return jsonResponse.Data.VideoPortalUrl;

            }
            catch (Exception e)
            {
                return null;
            }
        }

        public async Task<List<VideoChannel>> GetVideoChannels()
        {
            var requestUrl = string.Format("{0}/_api/VideoService/Channels", videoPortalUrl);

            Func<HttpRequestMessage> requestCreator = () =>
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("Accept", "application/json;odata=verbose");
                return request;
            };


            var httpClient = new HttpClient();

            var response = await SendRequestAsync(sharePointAccessToken, sharePointServiceEndpointUri, httpClient, requestCreator);

            string responseString = await response.Content.ReadAsStringAsync();

            var jsonResponse = JsonConvert.DeserializeObject<VideoChannelCollection>(responseString);

            // convert to model object
            var channels = new List<VideoChannel>();

            foreach (var videoChannel in jsonResponse.Data.Results)
            {
                var channel = new VideoChannel
                {
                    Id = videoChannel.Id,
                    HtmlColor = videoChannel.TileHtmlColor,
                    Title = videoChannel.Title,
                    Description = videoChannel.Description,
                    ServerRelativeUrl = videoChannel.ServerRelativeUrl
                };
                channels.Add(channel);
            }

            return channels;
        }

        public async Task<List<Video>> GetVideos(string channelId)
        {
            var requestUrl = string.Format("{0}/_api/VideoService/Channels('{1}')/Videos", videoPortalUrl, channelId);

            Func<HttpRequestMessage> requestCreator = () =>
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("Accept", "application/json;odata=verbose");
                return request;
            };


            var httpClient = new HttpClient();

            var response = await SendRequestAsync(sharePointAccessToken, sharePointServiceEndpointUri, httpClient, requestCreator);

            string responseString = await response.Content.ReadAsStringAsync();

            var jsonResponse = JsonConvert.DeserializeObject<ChannelVideosCollection>(responseString);

            var videos = new List<Video>();

            foreach (var channelVideo in jsonResponse.Data.Results)
            {
                var video = new Video
                {
                    ChannelId = channelId,
                    VideoId = channelVideo.ID,
                    Title = channelVideo.Title,
                    DisplayFormUrl = channelVideo.DisplayFormUrl,
                    DurationInSeconds = channelVideo.VideoDurationInSeconds
                };
                videos.Add(video);
            }

            return videos;
        }

        public async Task<VideoPlayback> GetVideoPlayback(string channelId, string videoId, int streamingFormatType)
        {
            var requestUrl = string.Format("{0}/_api/VideoService/Channels('{1}')/Videos('{2}')/GetPlaybackUrl('{3}')",
                new string[] { videoPortalUrl, channelId, videoId, streamingFormatType.ToString() });

            Func<HttpRequestMessage> requestCreator = () =>
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("Accept", "application/json;odata=verbose");
                return request;
            };


            var httpClient = new HttpClient();

            var response = await SendRequestAsync(sharePointAccessToken, sharePointServiceEndpointUri, httpClient, requestCreator);

            string responseString = await response.Content.ReadAsStringAsync();

            var jsonResponse = JsonConvert.DeserializeObject<VideoPlaybackData>(responseString);

            return jsonResponse.Data;
        }

        private async Task<string> GetSharePointServiceEndpointUri()
        {
            if (sharePointServiceEndpointUri != null)
                return sharePointServiceEndpointUri;
            else
            {
                string accessToken = await GetAccessTokenForResource("https://api.office.com/discovery/");
                DiscoveryClient discoveryClient = new DiscoveryClient(() =>
                {
                    return accessToken;
                });

                CapabilityDiscoveryResult result = await discoveryClient.DiscoverCapabilityAsync("RootSite");
                sharePointAccessToken = await GetAccessTokenForResource(result.ServiceResourceId);
                sharePointServiceEndpointUri = result.ServiceEndpointUri.ToString();

                return sharePointServiceEndpointUri;
            }
        }

        public async Task<HttpResponseMessage> SendRequestAsync(string accessToken, string resourceId, HttpClient httpClient, Func<HttpRequestMessage> requestCreator)
        {
            using (var request = requestCreator.Invoke())
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("X-ClientService-ClientTag", "Office 365 API Tools 1.5");

                var response = await httpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    string accessToken2 = await GetAccessTokenForResource("https://api.office.com/discovery/");
                    using (HttpRequestMessage retryRequest = requestCreator.Invoke())
                    {
                        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken2);
                        retryRequest.Headers.Add("X-ClientService-ClientTag", "Office 365 API Tools 1.5");
                        response = await httpClient.SendAsync(retryRequest);
                    }
                }

                return response;
            }
        }
    }
}
