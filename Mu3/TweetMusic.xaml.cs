using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using System.Net.Http;

using TwitterRtLibrary;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Windows.Data.Json;

//using MinTwit;
// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Mu3
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class TweetMusic : Mu3.Common.LayoutAwarePage
    {
        public TweetMusic()
        {
            this.InitializeComponent();
            TweetBox.Text = "#nowPlaying Metro Music via #Mu";


        }
        TwitterRt tr = new TwitterRt(Globalv.ConsumerKey, Globalv.ConsumerSecret, @"http://Mu3.com");

        List<TweetViewModel> ListTweetModel = new List<TweetViewModel>();
        List<TweetViewModel> Timeline = new List<TweetViewModel>();


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
        private bool UsingLogicalPageNavigation(ApplicationViewState? viewState = null)
        {
            if (viewState == null) viewState = ApplicationView.Value;
            return viewState == ApplicationViewState.FullScreenPortrait ||
                viewState == ApplicationViewState.Snapped;
        }
        void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Invalidate the view state when logical page navigation is in effect, as a change
            // in selection may cause a corresponding change in the current logical page.  When
            // an item is selected this has the effect of changing from displaying the item list
            // to showing the selected item's details.  When the selection is cleared this has the
            // opposite effect.
            if (this.UsingLogicalPageNavigation()) this.InvalidateVisualState();
        }
        private async void TwitterConnectBtn_Click_1(object sender, RoutedEventArgs e)
        {
            await tr.GainAccessToTwitter();
            TweetBox.Text = tr.Status;

        }

        private async void RefreshButton_Click_1(object sender, RoutedEventArgs e)
        {
            progbar.Visibility = Visibility.Visible;
            string resp2 = await Twitter.Get_tweets("#NowPlaying");
            JObject jo2 = JObject.Parse(resp2);
            //try{
            for (int i = 0; i < 19; i++)
            {
                TweetViewModel tvm = new TweetViewModel();
                tvm.handle = (string)jo2["results"][i]["from_user"];
                tvm.tweet = (string)jo2["results"][i]["text"];
                tvm.dp = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri((string)jo2["results"][i]["profile_image_url"], UriKind.Absolute));
                ListTweetModel.Add(tvm);
            }
            itemListView2.ItemsSource = ListTweetModel;

            progbar.Visibility = Visibility.Collapsed;

        }

        private async void pageRoot_Loaded_1(object sender, RoutedEventArgs e)
        {
            progbar.Visibility = Visibility.Visible;
            string resp2 = await Twitter.Get_tweets("#NowPlaying");
            JObject jo2 = JObject.Parse(resp2);
            //try{
            for (int i = 0; i < 19; i++)
            {
                TweetViewModel tvm = new TweetViewModel();
                tvm.handle = (string)jo2["results"][i]["from_user"];
                tvm.tweet = (string)jo2["results"][i]["text"];
                tvm.dp = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri((string)jo2["results"][i]["profile_image_url"], UriKind.Absolute));
                ListTweetModel.Add(tvm);
            }
            itemListView2.ItemsSource = ListTweetModel;

            progbar.Visibility = Visibility.Collapsed;

        }

        private async void TweetIt_Click_1(object sender, RoutedEventArgs e)
        {
            await tr.UpdateStatus(TweetBox.Text +" " + DateTime.Now);
            //_statusTextBlock.Text = tr.Status;


        }
    }
}
