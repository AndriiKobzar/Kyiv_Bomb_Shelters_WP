using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Kyiv_Bomb_Shelters.Resources;
using System.IO.IsolatedStorage;
using Windows.Devices.Geolocation;
using System.Device.Location;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Maps.Services;
using System.Xml.Linq;

namespace Kyiv_Bomb_Shelters
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        GeoCoordinate userCoordinate = new GeoCoordinate();
        List<Shelter> shelters = new List<Shelter>();
        RouteQuery MyQuery = null;
        GeocodeQuery Mygeocodequery = null;
        List<GeoCoordinate> MyCoordinates = new List<GeoCoordinate>();
        Shelter nearest = new Shelter();
        public MainPage()
        {
            InitializeComponent();

            if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                // User has opted in or out of Location
                return;
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
            initializeShelters();
        }

        private void initializeShelters()
        {
            XDocument doc = XDocument.Load("Assets");
            
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            setMapCenter();
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
                map.Layers[1].Clear();
            }
            map.Layers.Add(userLayer);
        }
        private async void OneShotLocation_Click(object sender, RoutedEventArgs e)
        {

            if ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] != true)
            {
                // The user has opted out of Location.
                return;
            }

            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracyInMeters = 50;

            try
            {
                Geoposition geoposition = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromMinutes(5),
                    timeout: TimeSpan.FromSeconds(10)
                    );

                
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    // the application does not have the right capability or the location master switch is off

                }
                //else
                {
                    // something else happened acquring the location
                }
            }
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
            Mygeocodequery = new GeocodeQuery();
            Mygeocodequery.SearchTerm = nearest.Adress;
            Mygeocodequery.GeoCoordinate = nearest.Coordinate;
            Mygeocodequery.QueryCompleted += Mygeocodequery_QueryCompleted;
            Mygeocodequery.QueryAsync();
        }
        void Mygeocodequery_QueryCompleted(object sender, QueryCompletedEventArgs<IList<MapLocation>> e)
        {
            if (e.Error == null)
            {
                try
                {
                    MyQuery = new RouteQuery();
                    MyCoordinates.Add(nearest.Coordinate);
                    MyQuery.Waypoints = MyCoordinates;
                    MyQuery.QueryCompleted += MyQuery_QueryCompleted;
                    MyQuery.QueryAsync();
                    Mygeocodequery.Dispose();
                }
                catch (Exception)
                {
                    MessageBox.Show("Не вдалося побудувати маршрут");
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