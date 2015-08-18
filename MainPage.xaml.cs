using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Office365Video.Models;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Office365Video
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            VideoRepository videoRepository = new VideoRepository();
            string videoPortalUrl = await videoRepository.GetVideoPortalHubUrl();
            var videoChannels = await videoRepository.GetVideoChannels();
            string channelId = videoChannels[0].Id;
            var videos = await videoRepository.GetVideos(channelId);
            string videoId = videos[0].VideoId;
            var videoPlayback = await videoRepository.GetVideoPlayback(channelId, videoId, 0);

            mediaElement.Volume = 0;
            mediaElement.AreTransportControlsEnabled = true;
            mediaElement.IsFullWindow = true;
            mediaElement.Source = new Uri(videoPlayback.Value);
        }
    }
}
