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
using Windows.Storage;
using System.Xml;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Net.Http;
using Bing.Maps;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Mu3
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MusicMap : Mu3.Common.LayoutAwarePage
    {
        public MusicMap()
        {
            this.InitializeComponent();
            myMap.Center = new Location(21.7679, 78.8718);
            myMap.ZoomLevel = 4;
            myMap.MapType = MapType.Birdseye;

        }
        
        private async void myMap_Loaded_1(object sender, RoutedEventArgs e)
        {
            progbar.Visibility = Visibility.Visible;

            var uri = new Uri("ms-appx:///Files/pushpin_info.xml", UriKind.RelativeOrAbsolute);
            StorageFile sampleFile = await StorageFile.GetFileFromApplicationUriAsync(uri);

            string metro_info = await Windows.Storage.FileIO.ReadTextAsync(sampleFile);


            //TODO: save response once in a file and avoid further http requests
            //string metro_info = await Lastfm.geo_Metros();
            List<string> countries = new List<string>();
            using (XmlReader rd = XmlReader.Create(new StringReader(metro_info)))
            {
                try
                {
                    while (true)
                    {
                        //Metropolis m = new Metropolis();
                        rd.ReadToFollowing("metro");
                        rd.ReadToDescendant("name");
                        //m.name = rd.ReadElementContentAsString();
                        rd.ReadToNextSibling("country");
                        //m.country = rd.ReadElementContentAsString();
                        countries.Add(rd.ReadElementContentAsString());
                    }
                }
                catch(Exception) { }

                }
            IEnumerable<string> nodup = countries.Distinct();
            List<string> nodups = nodup.ToList();
            Globalv.AllMetros.Clear();
            foreach (string m in nodups)
            {
                Metropolis c = new Metropolis();
                c.country = m;
                Globalv.AllMetros.Add(c);
            }

            StorageFolder folder = ApplicationData.Current.RoamingFolder;
            bool isfilethere = true;
            try
            {
                StorageFile metrofile = await folder.GetFileAsync("GeocodedMetros.xml");
            }
            catch (FileNotFoundException) { isfilethere = false; }

            if (!isfilethere) //if file doesn't exist
            {
                HttpClient cli = new HttpClient();

                foreach (Metropolis c in Globalv.AllMetros)
                {
                    try
                    {
                        var geocoder = await cli.GetAsync(@"https://maps.googleapis.com/maps/api/geocode/json?address=" + c.country + "&sensor=false");
                        string geo_resp = await geocoder.Content.ReadAsStringAsync();
                        JObject jo = JObject.Parse(geo_resp);

                        Location l = new Location();
                        l.Latitude = (double)jo["results"][0]["geometry"]["location"]["lat"];
                        l.Longitude = (double)jo["results"][0]["geometry"]["location"]["lng"];
                        //c.latlng = l;
                        c.lat = l.Latitude;
                        c.lon = l.Longitude;
                    }
                    catch (Exception) { }

                }

                //Write to file
                XmlSerializer serializer = new XmlSerializer(typeof(List<Metropolis>));

                //try
                //{
                    StorageFile geoCodedMetroFile = await folder.CreateFileAsync("GeocodedMetros.xml", CreationCollisionOption.ReplaceExisting);
                    var file = await geoCodedMetroFile.OpenAsync(FileAccessMode.ReadWrite);
                    Stream outStream = Task.Run(() => file.AsStreamForWrite()).Result;

                    serializer.Serialize(outStream, Globalv.AllMetros);
                //}
                //catch { }

            }
            else
            {
                StorageFile info = await folder.GetFileAsync("GeocodedMetros.xml");
                metro_info = await Windows.Storage.FileIO.ReadTextAsync(info);

                List<Metropolis> mt = new List<Metropolis>();
                using (XmlReader rd = XmlReader.Create(new StringReader(metro_info)))
                {
                    try
                    {
                        //rd.ReadToFollowing("ArrayOfMetropolis"); 
                        while (true)
                        {
                            //rd.ReadToFollowing("ArrayOfMetropolis");
                            //rd.ReadToFollowing("Metropolis");
                            //rd.ReadToFollowing("name");
                            rd.ReadToFollowing("country");
                            string country = rd.ReadElementContentAsString();
                            rd.ReadToFollowing("lat");
                            double lat = rd.ReadElementContentAsDouble();
                            rd.ReadToFollowing("lon");
                            double lon = rd.ReadElementContentAsDouble();

                            Metropolis m = new Metropolis();
                            m.country = country;
                            m.lat = lat;
                            m.lon = lon;
                            mt.Add(m);
                        }
                    }
                    catch (Exception) { }

                }

                Globalv.AllMetros = mt;
            }
            //for adding push pins to countries
            foreach (Metropolis m in Globalv.AllMetros)
            {
                Pushpin pin = new Pushpin();
                pin.Text = m.country;
                Location L = new Location(m.lat, m.lon);
                MapLayer.SetPosition(pin, L);
                myMap.Children.Add(pin);
                pin.Tapped += pin_Tapped;
            }

            progbar.Visibility = Visibility.Collapsed;
        }

        async void pin_Tapped(object sender, TappedRoutedEventArgs e)
        {
            progbar.Visibility = Visibility.Visible;
            Pushpin pin = (Pushpin)sender;

            string resp = await Lastfm.geo_topTrack(pin.Text);
            SubHeaderTb.Text = "Current trends in " + pin.Text;
            Globalv.CountryTrends.Clear();
            itemsGridView.ItemsSource = null;
            using (XmlReader rd = XmlReader.Create(new StringReader(resp)))
            {
                for (int i = 0; i < 12; i++)
                {
                    Song s2 = new Song();
                    rd.ReadToFollowing("name");
                    s2.Title = rd.ReadElementContentAsString();
                    rd.ReadToFollowing("artist");
                    rd.ReadToDescendant("name");
                    s2.Artist = rd.ReadElementContentAsString();
                    //s2.content = "Artist: " + s2.Artist + "\nTrack heard over " + pclist.ToString() + " times by " + listenerslist.ToString() + " listeners worldwide.";
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
                    Globalv.CountryTrends.Add(s2);
                }
                itemsGridView.ItemsSource = Globalv.CountryTrends;
                itemsGridView.UpdateLayout();

            }
            progbar.Visibility = Visibility.Collapsed;
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
    }
}
