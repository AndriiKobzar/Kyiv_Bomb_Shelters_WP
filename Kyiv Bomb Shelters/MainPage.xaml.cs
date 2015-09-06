using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using Windows.Devices.Geolocation;
using System.Device.Location;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Maps.Services;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kyiv_Bomb_Shelters
{
    public partial class MainPage : PhoneApplicationPage
    {
        Dictionary<string, string> image = new Dictionary<string, string>();
        // Constructor
        GeoCoordinate userCoordinate = new GeoCoordinate();
        List<Shelter> shelters = new List<Shelter>();
        RouteQuery MyQuery = null;
        GeocodeQuery Mygeocodequery = null;
        List<GeoCoordinate> MyCoordinates = new List<GeoCoordinate>();
        Shelter nearest = new Shelter();

        Shelter alternative = new Shelter();
        RouteQuery MyQuery1 = null;
        GeocodeQuery Mygeocodequery1 = null;
        List<GeoCoordinate> MyCoordinates1 = new List<GeoCoordinate>();

        string FILE_PATH = "Assets/new_tt.txt";
        string SAFE = "Assets/safe.png";
        string MEDIUM = "Assets/medium.png";
        string DANGEROUS = "Assets/dangerous.png";
        public MainPage()
        {
            image.Add("Метро",SAFE);
            image.Add("Підвальне приміщення", MEDIUM);
            InitializeComponent();
            if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                // User has opted in or out of Location
                //return;
            }
            else
            {
                MessageBoxResult result =
                    MessageBox.Show("This app accesses your phone's location. Is that ok?",
                    "Location",
                    MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
                }
                else
                {
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
                }

                IsolatedStorageSettings.ApplicationSettings.Save();
            }

        }

        private void initializeShelters()
        {
            List<string> lines = new List<string>();
            using(StreamReader reader = new StreamReader(new FileStream(FILE_PATH, FileMode.Open)))
            {
                while(!reader.EndOfStream)
                {
                    lines.Add(reader.ReadLine());
                }
            }
            string[] pre_txt = lines.ToArray();
            
            foreach (string line in pre_txt)
            {
                var obj = line.Split(';');
                if (obj[3].ToLower().Contains("підвальне приміщення")) obj[3] = "підвальне приміщення";
                shelters.Add(new Shelter() { Adress = obj[3], Type = obj[4], Coordinate = new GeoCoordinate() { Latitude = double.Parse(obj[1]), Longitude = double.Parse(obj[2]) } });
            }           
        }
        void DrawShelters()
        {
            MapLayer layer = new MapLayer();
            foreach(Shelter shelter in shelters)
            {
                string imagePath;
                if (image.ContainsKey(shelter.Type)) imagePath = image[shelter.Type];
                else imagePath = DANGEROUS;
                BitmapImage imageSource = new BitmapImage(new Uri(imagePath, UriKind.Relative));
                int offset = -imageSource.DecodePixelWidth / 2;
                layer.Add(new MapOverlay() { Content = new Image() { Source = imageSource, Margin=new Thickness(-32,-64,0,0) }, GeoCoordinate = shelter.Coordinate });
            }
            map.Layers.Add(layer);
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            setMapCenter();
            initializeShelters();
            DrawShelters();
        }
        async void setMapCenter()
        {
            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracyInMeters = 50;

            try
            {
                Geoposition geoposition = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromMinutes(5),
                    timeout: TimeSpan.FromSeconds(10)
                    );
                userCoordinate = geoposition.Coordinate.ToGeoCoordinate();
                map.Center = userCoordinate;
                setUserLayer();
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    // the application does not have the right capability or the location master switch is off
                    MessageBox.Show("Location  is disabled in phone settings.");
                }
                //else
                {
                    MessageBox.Show("some problems");
                    // something else happened acquring the location
                }
            }
        }
        private void setUserLayer()
        {

            MapLayer userLayer = new MapLayer();
            MapOverlay overlay = new MapOverlay()
            {
                GeoCoordinate = userCoordinate,
                Content = new UserLocationMarker()
                {
                    GeoCoordinate = userCoordinate,
                    Margin = new Thickness(-18, -18, 0, 0)
                }
            };
            userLayer.Add(overlay);
            if (map.Layers.Count == 2)
            {
                map.Layers[0].Clear();
            }
            map.Layers.Add(userLayer);
        }

        private void Button_Click(object sender, RoutedEventArgs e)//find nearest shelter and draw path to it
        {
            //find nearest
            shelters.Sort((Shelter s1, Shelter s2) =>
            {
                return Math.Sqrt(Math.Pow(s1.Coordinate.Latitude - userCoordinate.Latitude, 2) + Math.Pow(s1.Coordinate.Longitude - userCoordinate.Longitude, 2)).CompareTo(
                        Math.Sqrt(Math.Pow(s2.Coordinate.Latitude - userCoordinate.Latitude, 2) + Math.Pow(s2.Coordinate.Longitude - userCoordinate.Longitude, 2)));
            });
            nearest = shelters[0];
            alternative = shelters[1];
            Mygeocodequery = new GeocodeQuery();
            Mygeocodequery.SearchTerm = nearest.Adress;
            Mygeocodequery.GeoCoordinate = nearest.Coordinate;
            Mygeocodequery.QueryCompleted += Mygeocodequery_QueryCompleted;
            Mygeocodequery.QueryAsync();

            Mygeocodequery1 = new GeocodeQuery();
            Mygeocodequery1.SearchTerm = alternative.Adress;
            Mygeocodequery1.GeoCoordinate = alternative.Coordinate;
            Mygeocodequery1.QueryCompleted += Mygeocodequery_QueryCompleted1;
            Mygeocodequery1.QueryAsync();
        }

        private void Mygeocodequery_QueryCompleted1(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Error == null)
            {
                try
                {
                    MyQuery1 = new RouteQuery();
                    MyCoordinates1.Add(userCoordinate);
                    MyCoordinates1.Add(alternative.Coordinate);
                    MyQuery1.Waypoints = MyCoordinates1;
                    MyQuery1.TravelMode = TravelMode.Walking;
                    MyQuery1.QueryCompleted += MyQuery_QueryCompleted1;
                    MyQuery1.QueryAsync();
                    Mygeocodequery1.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void MyQuery_QueryCompleted1(object sender, QueryCompletedEventArgs<Route> e)
        {
            if (e.Error == null)
            {
                try
                {
                    Route MyRoute = e.Result;
                    MapRoute MyMapRoute = new MapRoute(MyRoute);
                    int path = MyRoute.LengthInMeters;
                    Border border = new Border();
                    border.Background = new SolidColorBrush(Colors.Black);
                    border.Child = new TextBlock() { Text = path.ToString() + " metres" };
                    map.Layers[1].Add(new MapOverlay() { GeoCoordinate = alternative.Coordinate, Content = border });
                    MyMapRoute.Color = Colors.Gray;
                    map.AddRoute(MyMapRoute);
                    MyQuery.Dispose();
                }
                catch
                {
                    MessageBox.Show("Не вдалося побудувати маршрут");
                }
            }
        }
        void Mygeocodequery_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Error == null)
            {
                try
                {
                    MyQuery = new RouteQuery();
                    MyCoordinates.Add(userCoordinate);
                    MyCoordinates.Add(nearest.Coordinate);
                    MyQuery.TravelMode = TravelMode.Walking;
                    MyQuery.Waypoints = MyCoordinates;
                    MyQuery.QueryCompleted += MyQuery_QueryCompleted;
                    MyQuery.QueryAsync();
                    Mygeocodequery.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        void MyQuery_QueryCompleted(object sender, QueryCompletedEventArgs<Route> e)
        {
            if (e.Error == null)
            {
                try
                {
                    Route MyRoute = e.Result;
                    int path = MyRoute.LengthInMeters;
                    Border border = new Border();
                    border.Background = new SolidColorBrush(Colors.Black);
                    border.Child = new TextBlock() { Text = path.ToString() + " metres" };
                    map.Layers[1].Add(new MapOverlay() { GeoCoordinate = nearest.Coordinate, Content = border });
                    MapRoute MyMapRoute = new MapRoute(MyRoute);
                    map.AddRoute(MyMapRoute);
                    MyQuery.Dispose();
                }
                catch
                {
                    MessageBox.Show("Не вдалося побудувати маршрут");
                }
            }
        }

        
    }
}