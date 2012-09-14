using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using System.Xml;
using Windows.Media;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Mu3
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class Trends : Mu3.Common.LayoutAwarePage
    {
        public Trends()
        {
            this.InitializeComponent();
            TopTracks = new List<Song>();
        }
        public BitmapImage GroupImage { get; set; }
        public string GroupName { get; set; }
        public string GroupDescription { get; set; }
        public List<Song> TopTracks;


        private void itemsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.Frame.Navigate(typeof(TrendsDetails));
        }

        private void itemsGridView_ItemClick_1(object sender, ItemClickEventArgs e)
        {
            this.Frame.Navigate(typeof(TrendsDetails));
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

        private async void pageRoot_Loaded_1(object sender, RoutedEventArgs e)
        {
            if (MediaControl.IsPlaying)
            {
                BG1.Begin();
            }

            if (Globalv.GlobalTopTracks.Count == 0)
            {
                progbar.Visibility = Visibility.Visible;
                bool success = false;
                try
                {
                    string resp = await Lastfm.chart_topTracks();

                    using (XmlReader rd = XmlReader.Create(new StringReader(resp)))
                    {
                        //for headliner
                        rd.ReadToFollowing("name");
                        GroupName = rd.ReadElementContentAsString();
                        rd.ReadToFollowing("playcount");
                        long pc = rd.ReadElementContentAsLong();
                        rd.ReadToFollowing("listeners");
                        long listeners = rd.ReadElementContentAsLong();
                        rd.ReadToFollowing("artist");
                        rd.ReadToDescendant("name");
                        string artist = rd.ReadElementContentAsString();
                        GroupDescription = "Artist: " + artist + "\nTrack heard over " + pc.ToString() + " times by " + listeners.ToString() + " listeners worldwide.";
                        Song s = new Song();
                        s.Artist = artist;
                        s.Title = GroupName;
                        string resp2 = await Lastfm.track_getInfo(s);
                        using (XmlReader rd2 = XmlReader.Create(new StringReader(resp2)))
                        {
                            rd2.ReadToFollowing("album");
                            rd2.ReadToDescendant("image");
                            rd2.ReadToNextSibling("image");
                            rd2.ReadToNextSibling("image");

                            GroupImage = new BitmapImage(new Uri(rd2.ReadElementContentAsString(), UriKind.Absolute));
                        }
                        GNameTb.Text = GroupName;
                        GImage.Source = GroupImage;
                        GDesc.Text = GroupDescription;

                        //for other items
                        for (int i = 0; i < 19; i++)
                        {
                            //for headliner
                            Song s2 = new Song();
                            rd.ReadToFollowing("name");
                            s2.Title = rd.ReadElementContentAsString();
                            rd.ReadToFollowing("playcount");
                            long pclist = rd.ReadElementContentAsLong();
                            rd.ReadToFollowing("listeners");
                            long listenerslist = rd.ReadElementContentAsLong();
                            rd.ReadToFollowing("artist");
                            rd.ReadToDescendant("name");
                            s2.Artist = rd.ReadElementContentAsString();
                            s2.content = "Artist: " + s2.Artist + "\nTrack heard over " + pclist.ToString() + " times by " + listenerslist.ToString() + " listeners worldwide.";
                            string resp22 = await Lastfm.track_getInfo(s2);

                            try
                            {
                                using (XmlReader rd2 = XmlReader.Create(new StringReader(resp22)))
                                {
                                    rd2.ReadToFollowing("album");
                                    rd2.ReadToFollowing("image");
                                    rd2.ReadToNextSibling("image");
                                    rd2.ReadToNextSibling("image");
                                    s2.image = new BitmapImage(new Uri(rd2.ReadElementContentAsString(), UriKind.Absolute));
                                }
                            }
                            catch (Exception) { }
                            TopTracks.Add(s2);
                        }
                        itemsGridView.ItemsSource = TopTracks;
                        Globalv.GlobalTopTracks = TopTracks;
                    }
                    success = true;
                }
                catch (Exception)
                { success = false; }

                if (!success)
                {
                    MessageDialog m = new MessageDialog("This feature requires you to be connected to the internet. Connect to the internet and try again", "You're offline");
                    await m.ShowAsync();
                }
                progbar.Visibility = Visibility.Collapsed;
            }
            else
            {
                itemsGridView.ItemsSource = TopTracks;
            }
        

        }

        private void BG1_Completed_1(object sender, object e)
        {
            if (MediaControl.IsPlaying)
            {
                BG1.Begin();
            }

        }
    }
}
