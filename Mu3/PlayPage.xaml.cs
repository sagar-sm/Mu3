using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.ApplicationSettings;

using Windows.System;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.Media;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Mu3
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class PlayPage : Mu3.Common.LayoutAwarePage
    {
        public PlayPage()
        {
            this.InitializeComponent();
            this.InitializeComponent();
            MediaControl.PlayPressed += MediaControl_PlayPressed;
            MediaControl.PausePressed += MediaControl_PausePressed;
            MediaControl.PlayPauseTogglePressed += MediaControl_PlayPauseTogglePressed;
            MediaControl.StopPressed += MediaControl_StopPressed;

        }

        bool isScrobbledOnce = false;
        private void MediaControl_StopPressed(object sender, object e)
        {
            App.GlobalAudioElement.Stop();
            MediaControl.IsPlaying = false;
        }

        private void MediaControl_PlayPauseTogglePressed(object sender, object e)
        {/*
            if (MediaControl.IsPlaying == true)
            {
                mediaPlayer.Pause();
                MediaControl.IsPlaying = false;
            }
            else
            {
                mediaPlayer.Play();
                MediaControl.IsPlaying = true;
            }*/
        }

        private void MediaControl_PausePressed(object sender, object e)
        {
            App.GlobalAudioElement.Pause();
        }

        private void MediaControl_PlayPressed(object sender, object e)
        {
            App.GlobalAudioElement.Play();
        }

        string lfm_api_key = Globalv.lfm_api_key;
        MusicProperties id3;

        private async void Collection_Click_1(object sender, RoutedEventArgs e)
        {
            if (EnsureUnsnapped())
            {
                FileOpenPicker pkr = new FileOpenPicker();
                pkr.ViewMode = PickerViewMode.List;
                pkr.SuggestedStartLocation = PickerLocationId.MusicLibrary;
                pkr.FileTypeFilter.Add(".mp3");

                StorageFile file = await pkr.PickSingleFileAsync();
                if (null != file)
                {

                    var strm = await file.OpenAsync(FileAccessMode.Read);
                    Playlist.NowPlaying.Clear();
                    App.GlobalAudioElement.AudioCategory = Windows.UI.Xaml.Media.AudioCategory.BackgroundCapableMedia;
                    //mediaPlayer.SetSource(strm, file.ContentType);
                    App.GlobalAudioElement.SetSource(strm, file.ContentType);
                    App.GlobalAudioElement.Play();
                    //timelineSlider.Maximum = App.GlobalAudioElement.NaturalDuration.TimeSpan.TotalMilliseconds;
                    isScrobbledOnce = false;

                    MediaControl.IsPlaying = true;
                    //PlayPauseBtn.Content = "";

                    id3 = await file.Properties.GetMusicPropertiesAsync();
                    SongTitle.Text = id3.Title;
                    Artist.Text = id3.Artist;

                    Playlist.NowPlaying.Add(id3);
                    Lastfm.track_updateNowPlaying(id3);

                    string xmlinfo = await Lastfm.track_getInfo(id3);
                    string artistinfo = await Lastfm.artist_getInfo(id3.Artist);
                    try
                    {
                        using (XmlReader rd = XmlReader.Create(new StringReader(xmlinfo)))
                        {
                            rd.ReadToFollowing("name");
                            TitleInfoTbx.Text = rd.ReadElementContentAsString();
                            rd.ReadToFollowing("artist");
                            rd.ReadToDescendant("name");
                            SubtitleInfoTbx.Text = rd.ReadElementContentAsString();
                        }

                        using (XmlReader rd = XmlReader.Create(new StringReader(xmlinfo)))
                        {
                            rd.ReadToFollowing("image");
                            rd.ReadToNextSibling("image");
                            rd.ReadToNextSibling("image");
                            Uri src = new Uri(rd.ReadElementContentAsString(), UriKind.Absolute);
                            AlbumArtHolder.Source = new BitmapImage(src);
                        }
                        using (XmlReader rd = XmlReader.Create(new StringReader(xmlinfo)))
                        {
                            rd.ReadToFollowing("wiki");
                            rd.ReadToDescendant("summary");
                            SummaryInfoTbx.Text = rd.ReadElementContentAsString();
                        }
                    }
                    catch (Exception)
                    {
                        try
                        {
                            using (XmlReader rd = XmlReader.Create(new StringReader(artistinfo)))
                            {
                                rd.ReadToFollowing("image");
                                rd.ReadToNextSibling("image");
                                rd.ReadToNextSibling("image");
                                Uri src = new Uri(rd.ReadElementContentAsString(), UriKind.Absolute);
                                AlbumArtHolder.Source = new BitmapImage(src);
                            }
                        }
                        catch (Exception) { AlbumArtHolder.Source = null; }
                    }
                    //prepare for scrobble
                    TimelineMarker tlm = new TimelineMarker();
                    tlm.Time = new System.TimeSpan(0, 0, (int)id3.Duration.TotalSeconds / 2);
                    App.GlobalAudioElement.Markers.Clear();
                    App.GlobalAudioElement.Markers.Add(tlm);
                    if (id3.Duration > new System.TimeSpan(0, 0, 30))
                    {
                        await Lastfm.track_scrobble(id3);
                        isScrobbledOnce = true;

                        //App.GlobalAudioElement.MarkerReached += mediaPlayer_MarkerReached_scrobble; //scrobble
                    }
                }
                else
                { return; }
            }
        }

        /*void mediaPlayer_MarkerReached_scrobble(object sender, TimelineMarkerRoutedEventArgs e)
        {
            if (!isScrobbledOnce)
            {
            }
            //throw new NotImplementedException();
        }         
         */

        bool EnsureUnsnapped()
        {
            return ((ApplicationView.Value != ApplicationViewState.Snapped) ||
                ApplicationView.TryUnsnap());
        }


        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        private void pageRoot_Loaded_1(object sender, RoutedEventArgs e)
        {
            if (MediaControl.IsPlaying)
            {
                id3 = Playlist.NowPlaying[0];
                SongTitle.Text = id3.Title;
                Artist.Text = id3.Title;
            }
            else
            {
                SongTitle.Text = "Play a song now!";
                Artist.Text = "Swipe up from bottom for more options!";
            }
            /*
            if (Globalv.session_key != null)
                LoginBtn.Content = "Logoff";
            else
                LoginBtn.Content = "Login";
             */ 


        }
    }
}
