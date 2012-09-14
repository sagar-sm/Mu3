using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
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
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.Authentication;
using Windows.Security.Authentication.Web;
using Windows.Security.Credentials;
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
            BG1.Begin();

        }


        bool isScrobbledOnce = false; //[IMPORTANT]: IMPLEMENT THIS COMPLETELY
        
        bool justLoggedOut = false;

        System.Uri EndUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();

        private void MediaControl_StopPressed(object sender, object e)
        {
            App.GlobalAudioElement.Stop();
            MediaControl.IsPlaying = false;
        }

        private async void MediaControl_PlayPauseTogglePressed(object sender, object e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (MediaControl.IsPlaying == true)
                {
                    App.GlobalAudioElement.Pause();
                    MediaControl.IsPlaying = false;
                }
                else
                {
                    App.GlobalAudioElement.Play();
                    MediaControl.IsPlaying = true;
                }
            });
        }


        private void MediaControl_PausePressed(object sender, object e)
        {
        }

        private void MediaControl_PlayPressed(object sender, object e)
        {
            
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
                    MediaControl.ArtistName = id3.Artist;
                    MediaControl.TrackName = id3.Title;
                    Playlist.NowPlaying.Add(id3);
                    Lastfm.track_updateNowPlaying(id3);

                    string xmlinfo = await Lastfm.track_getInfo(id3);
                    string artistinfo = await Lastfm.artist_getInfo(id3.Artist);
                    try
                    {
                        using (XmlReader rd = XmlReader.Create(new StringReader(xmlinfo)))
                        {
                            rd.ReadToFollowing("name");
                            //TitleInfoTbx.Text = rd.ReadElementContentAsString();
                            rd.ReadToFollowing("artist");
                            rd.ReadToDescendant("name");
                            //SubtitleInfoTbx.Text = rd.ReadElementContentAsString();
                            
                        }

                        Uri src;
                        using (XmlReader rd = XmlReader.Create(new StringReader(xmlinfo)))
                        {
                            rd.ReadToFollowing("image");
                            rd.ReadToNextSibling("image");
                            rd.ReadToNextSibling("image");
                            src = new Uri(rd.ReadElementContentAsString(), UriKind.Absolute);
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
                                MediaControl.AlbumArt = src;
                            }
                        }
                        catch (Exception) 
                        { 
                            AlbumArtHolder.Source = null;
                        }
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

            if (Security._vault.RetrieveAll().Count != 0)
            {
                Scrobble.SetValue(AutomationProperties.NameProperty, "Last.fm Signout");
                PasswordCredential rt = Security._vault.Retrieve("Session Key", "user");
                rt.RetrievePassword();
                Globalv.session_key = rt.Password;
            }

            /*
            if (Globalv.session_key != null)
                LoginBtn.Content = "Logoff";
            else
                LoginBtn.Content = "Login";
             */ 


        }

        private async void Scrobble_Click_1(object sender, RoutedEventArgs e)
        {
            if (Security._vault.RetrieveAll().Count == 0)
            {
                Globalv.session_key = null;
                Scrobble.SetValue(AutomationProperties.NameProperty, "Last.fm Connect");
                justLoggedOut = false;
            }
            else
            {
                PasswordCredential rt = Security._vault.Retrieve("Session Key", "user");
                rt.RetrievePassword();
                Globalv.session_key = rt.Password;
                //sign out

                Security._vault.Remove(rt);
                Globalv.session_key = null;
                Scrobble.SetValue(AutomationProperties.NameProperty, "Last.fm Connect");
                justLoggedOut = true;

            }
            if (Globalv.session_key == null && justLoggedOut == false)
            {
                String lfmURL = "https://www.last.fm/api/auth/?api_key=" + Globalv.lfm_api_key + "&cb=" + EndUri;

                System.Uri StartUri = new Uri(lfmURL);

                WebAuthenticationResult WebAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(
                                                        WebAuthenticationOptions.None,
                                                        StartUri,
                                                        EndUri);

                if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.Success)
                {
                    //get and save lfm session key
                    string[] responseData = WebAuthenticationResult.ResponseData.ToString().Split('?');
                    string token = responseData[1].Substring(6);

                    HttpClient cli = new HttpClient();
                    string getsk_sig = "api_key" + Globalv.lfm_api_key + "methodauth.getSessiontoken" + token + "0e6e780c3cfa3faedf0c58d5aa6de92f";
                    HashAlgorithmProvider objAlgProv = HashAlgorithmProvider.OpenAlgorithm("MD5");
                    CryptographicHash objHash = objAlgProv.CreateHash();
                    IBuffer buffSig = CryptographicBuffer.ConvertStringToBinary(getsk_sig, BinaryStringEncoding.Utf8);
                    objHash.Append(buffSig);
                    IBuffer buffSighash = objHash.GetValueAndReset();

                    string api_sig = CryptographicBuffer.EncodeToHexString(buffSighash);

                    string get_sk = @"http://ws.audioscrobbler.com/2.0/?method=auth.getSession&api_key=" + Globalv.lfm_api_key + "&api_sig=" + api_sig + "&token=" + token;
                    HttpResponseMessage r = await cli.GetAsync(get_sk);
                    string xml_resp = await r.Content.ReadAsStringAsync();

                    using (XmlReader rd = XmlReader.Create(new StringReader(xml_resp)))
                    {
                        rd.ReadToFollowing("key");

                        Globalv.session_key = rd.ReadElementContentAsString();
                        var c = new PasswordCredential("Session Key", "user", Globalv.session_key);
                        Mu3.Security._vault.Add(c);

                        justLoggedOut = false;
                        Scrobble.SetValue(AutomationProperties.NameProperty, "Last.fm Signout");
                        /*
                        PasswordCredential rt = Security._vault.Retrieve("Session Key", "user");
                        rt.RetrievePassword();
                        MessageDialog m = new MessageDialog(rt.Password);
                        await m.ShowAsync();
                         */
                    }
                    //MessageDialog m1 = new MessageDialog(Globalv.session_key);
                    //await m1.ShowAsync();
                }
                else if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.ErrorHttp)
                {
                    MessageDialog m = new MessageDialog("HTTP Error returned by AuthenticateAsync() : " + WebAuthenticationResult.ResponseErrorDetail.ToString());
                    await m.ShowAsync();
                }
                else
                {
                    MessageDialog m = new MessageDialog("Error returned by AuthenticateAsync() : " + WebAuthenticationResult.ResponseStatus.ToString());
                    await m.ShowAsync();
                }

            }

        }

        private async void Love_Click_1(object sender, RoutedEventArgs e)
        {
            if (MediaControl.IsPlaying)
            {
                await Lastfm.track_love(id3);
            }
        }

        private async void Ban_Click_1(object sender, RoutedEventArgs e)
        {
            if (MediaControl.IsPlaying)
            {
                await Lastfm.track_love(id3);
            }
        }

        private void BG1_Completed_1(object sender, object e)
        {
            BG1.Begin();
        }
    }
}
