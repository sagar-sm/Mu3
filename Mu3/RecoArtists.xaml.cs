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
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;
using Windows.Security.Authentication;
using Windows.Security.Authentication.Web;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.Credentials;
using Windows.Storage.Streams;
using System.Net.Http;
using System.Xml;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace Mu3
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class RecoArtists : Mu3.Common.LayoutAwarePage
    {
        public RecoArtists()
        {
            this.InitializeComponent();
            
            //For debugging purposes only.
            //Security._vault.Remove(Security._vault.Retrieve("Session Key", "user"));
        }
        System.Uri EndUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri();

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
            // TODO: Assign a bindable collection of items to this.DefaultViewModel["Items"]
        }

        private void itemGridView_ItemClick_1(object sender, ItemClickEventArgs e)
        {
            this.Frame.Navigate(typeof(ArtistDetails), e.ClickedItem);
        }

        private async void pageRoot_Loaded_1(object sender, RoutedEventArgs e)
        {
            if (Security._vault.RetrieveAll().Count == 0)
                Globalv.session_key = null;
            else
            {
                PasswordCredential rt = Security._vault.Retrieve("Session Key", "user");
                rt.RetrievePassword();
                Globalv.session_key = rt.Password;
            }
            if (Globalv.session_key == null)
            {
                progbar.Visibility = Visibility.Visible;

                String lfmURL = "https://www.last.fm/api/auth/?api_key=" + Globalv.lfm_api_key + "&cb=" + EndUri;

                System.Uri StartUri = new Uri(lfmURL);
                //System.Uri EndUri = new Uri("ms-app://mu3.com");

                //DebugPrint("Navigating to: " + FacebookURL);

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
                    string getsk_sig = "api_key" + Globalv.lfm_api_key + "methodauth.getSessiontoken"+ token + "0e6e780c3cfa3faedf0c58d5aa6de92f";
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
            string resp = await Lastfm.user_getRecommendedArtists();
            Globalv.RecommendedArtists.Clear();
            bool success = false;
            try
            {
                using (XmlReader rd = XmlReader.Create(new StringReader(resp)))
                {
                    rd.ReadToFollowing("recommendations");
                    rd.MoveToAttribute("perPage");
                    int size = 34;

                    for (int i = 0; i < size; i++)
                    {
                        Artist ar = new Artist();
                        //rd.ReadToFollowing("artist");
                        rd.ReadToFollowing("name");
                        ar.name = rd.ReadElementContentAsString();
                        rd.ReadToNextSibling("mbid");
                        ar.mbid = rd.ReadElementContentAsString();
                        rd.ReadToNextSibling("image");
                        rd.ReadToNextSibling("image");
                        rd.ReadToNextSibling("image");
                        ar.image = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(rd.ReadElementContentAsString(), UriKind.Absolute));
                        Globalv.RecommendedArtists.Add(ar);
                    }
                    
                    List<Artist> dps = Globalv.RecommendedArtists.Distinct().ToList();
                    itemGridView.ItemsSource = dps;
                    success = true;
                }
            }
            catch (Exception) 
            {
                success = false;
            }
            if (!success)
            {
                MessageDialog m = new MessageDialog("There was some error in fetching content. Please try after sometime.", "Oops!");
                await m.ShowAsync();
            }

            progbar.Visibility = Visibility.Collapsed;
        
        }

    }
}
