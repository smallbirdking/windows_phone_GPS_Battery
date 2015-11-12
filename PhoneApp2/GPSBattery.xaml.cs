using System;
using Windows.UI.Popups;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhoneApp2.Resources;
using System.Device.Location;
using System.Threading;
using Windows.Phone.Devices.Power;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Collections.ObjectModel;

using Microsoft.Phone.Maps.Controls;
using System.Device.Location;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Collections;



namespace PhoneApp2
{
    public enum Places{
        UNKNOWN = -1,  WORK = 0, HOME = 1, SCHOOL = 2, BEACH = 3
    }

    public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        GeoCoordinateWatcher watcher;
        Places currentPlace = Places.UNKNOWN;
        Places placetoshow;
        bool trackingOn = false;
        Color[] tabColor = new Color[13]; 

        Microsoft.Phone.Controls.Maps.Pushpin myPushpin = new Microsoft.Phone.Controls.Maps.Pushpin();
        int batteryLevelWhenEnteringCurrentPlace;
        // Data context for the local database
        private LocationsAndBatteryDataContext locationsBaterryDB;
        Double myCurrentLatitude = 0.0;
        Double myCurrentLongitude = 0.0;
        // Constructeur
        public MainPage()
        {


            
            

            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            watcher.MovementThreshold = 10.0f;
            watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            new Thread(startLocServInBackground).Start();
            //statusTextBlock.Text = "Starting Location Service...";

            // Exemple de code pour la localisation d'ApplicationBar
            //BuildLocalizedApplicationBar();
            // Connect to the database and instantiate data context.
            locationsBaterryDB = new LocationsAndBatteryDataContext(LocationsAndBatteryDataContext.DBConnectionString);


            var batteryUsagesInDB = from BatteryUsageDB bu in locationsBaterryDB.batteryUsage
                                                        select bu;

            // Execute the query and place the results into a collection.
            BatteryUsage = new ObservableCollection<BatteryUsageDB>(batteryUsagesInDB);



            var locationsInDB = from Locations bu in locationsBaterryDB.locations
                                    select bu;

            // Execute the query and place the results into a collection.
            Locations = new ObservableCollection<Locations>(locationsInDB);

            // Data context and observable collection are children of the main page.
            this.DataContext = this;
            init();

        }

        void init()
        {
            InitializeComponent();
            tabColor[0] = Colors.Black;
            tabColor[1] = Colors.Blue;
            tabColor[2] = Colors.Yellow;
            tabColor[3] = Colors.Green;
            tabColor[4] = Colors.Magenta;
            tabColor[5] = Colors.Orange;
            tabColor[6] = Colors.Purple;
            tabColor[7] = Colors.Red;
            tabColor[8] = Colors.Cyan;
            tabColor[9] = Colors.DarkGray;
            tabColor[10] = Colors.Gray;
            tabColor[11] = Colors.LightGray;
            tabColor[12] = Colors.Brown;
            initsymulatedata();

        }

        void initsymulatedata()
        {


            DateTime date = new DateTime(2015, 11, 1, 9, 10, 10);
            DateTime date2 = new DateTime(2015, 11, 2, 9, 10, 10);
            DateTime date3 = DateTime.Now; //new DateTime(2015, 11, 3, 9, 10, 10);
            DateTime date4 = DateTime.Now; //new DateTime(2015, 11, 4, 9, 10, 10);
            myMap.Layers.Clear();
            symulateplaces( 43.62444936188, 7.04948675243, Places.HOME, date, 1 ,(int)Places.HOME);
            symulateplaces(43.62444936188, 7.04948675243, Places.HOME, date3, 2, (int)Places.HOME);
            symulateplaces(43.62544936188, 7.05048675243, Places.SCHOOL, date2, 3, (int)Places.SCHOOL);
            symulateplaces(43.62544936188, 7.05048675243, Places.SCHOOL, date4, 4, (int)Places.SCHOOL);

            //43.6155409436964, 7.0726958032125
            
        }

        void symulateplaces(double lat, double l, Places place, DateTime date,int dropbat,int col)
        {

            double[] d = new double[] {lat, l };

            double[][] rans = Place_symylator.randomPlace(d, 0.0005);
            
            int bat = 100;
            
            for (int i = 0; i < rans.Length; i++)
            {
                //System.Diagnostics.Debug.WriteLine("DEBUG remaining battery " + batteryLevelWhenEnteringCurrentPlace);

                insertNewLocationsValue(date, d[0], d[1], bat, place);
                insertNewBatteryUsageValue(place, batteryLevelWhenEnteringCurrentPlace);
                //Display the average of all the values in BatteryUsageTable for this newPlace
                getAverageBatteryUsage(place);
                date.AddMinutes(30);
                bat -= dropbat;
                ShowMyLocationOnTheMap(rans[i][0], rans[i][1], tabColor[col]);
            }
        }

        List<double> batUsedByDay(Places p)
        {
            List<double> bl = new List<double>();
           
            List<Locations> l = Locations.ToList();
            int day = 0;
            double bat1 = 0;
            double bat2 = 0;
            DateTime date = DateTime.Now;
            foreach (Locations m in l)
            {
                //System.Diagnostics.Debug.WriteLine("batttt00");
                if(m.LocationsPlace==(int)p){
                    //System.Diagnostics.Debug.WriteLine("batttt0");
                    //if (!m.LocationsTime.Day.Equals(date.Day))
                    //{
                        if (m.LocationsTime.Day.Equals(day) == false)
                        {
                            //System.Diagnostics.Debug.WriteLine("batttt" );
                            bl.Add(bat1 - bat2);
                            day = m.LocationsTime.Day;
                            bat1 = m.LocationsBatteryLevel;
                        }
                        else
                        {
                            //double d = (bl.Count != 0) ? bl[bl.Count - 1] : -1;
                            //if (d >0)
                            //{ 
                            //System.Diagnostics.Debug.WriteLine("batttt2" );
                            bat2 = m.LocationsBatteryLevel;
                            //}
                        }
                    //}
                }
                
            }
            bl.Add(bat1 - bat2);
            return bl;
        }

        // Define an observable collection property that controls can bind to.
        private ObservableCollection<Locations> _locations;
        public ObservableCollection<Locations> Locations
        {
            get
            {
                return _locations;
            }
            set
            {
                if (_locations != value)
                {
                    _locations = value;
                    NotifyPropertyChanged("Locations");
                }
            }
        }
        // Define an observable collection property that controls can bind to.
        private ObservableCollection<BatteryUsageDB> _batteryUsage;
        public ObservableCollection<BatteryUsageDB> BatteryUsage
        {
            get
            {
                return _batteryUsage;
            }
            set
            {
                if (_batteryUsage != value)
                {
                    _batteryUsage = value;
                    NotifyPropertyChanged("BatteryUsage");
                }
            }
        }
        private void ShowMyLocationOnTheMap(double latitude, double longitude, Color color)
        {

            longitude = (double.IsNaN(longitude)) ? 0.0 : longitude;
            latitude = (double.IsNaN(latitude)) ? 0.0 : latitude;

            GeoCoordinate myGeocoordinate = new GeoCoordinate(latitude, longitude);

            // Make my current location the center of the Map.
            this.myMap.Center = myGeocoordinate;
            this.myMap.ZoomLevel = 13;

            Grid MyGrid = new Grid();
            MyGrid.RowDefinitions.Add(new RowDefinition());
            MyGrid.RowDefinitions.Add(new RowDefinition());
            MyGrid.Background = new SolidColorBrush(Colors.Transparent);

            Ellipse myCircle = new Ellipse();
            myCircle.Fill = new SolidColorBrush(color);
            myCircle.Height = 5;
            myCircle.Width = 5;
            myCircle.Opacity = 50;

            MyGrid.Children.Add(myCircle);
            // Create a MapOverlay to contain the circle.
            MapOverlay myLocationOverlay = new MapOverlay();
            myLocationOverlay.Content = MyGrid;
            myLocationOverlay.PositionOrigin = new Point(0.5, 0.5);
            myLocationOverlay.GeoCoordinate = myGeocoordinate;

            // Create a MapLayer to contain the MapOverlay.
            MapLayer myLocationLayer = new MapLayer();
            myLocationLayer.Add(myLocationOverlay);


            // Add the MapLayer to the Map.
            this.myMap.Layers.Add(myLocationLayer);


        }

       
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify the app that a property has changed.
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        /*private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前手机默认电池的Battery实例
            var myBattery = Battery.GetDefault();
            // 获取剩余电量
            int pc = myBattery.RemainingChargePercent;
            // 获取剩余续航时间
            TimeSpan tsp = myBattery.RemainingDischargeTime;
            string msg = string.Format("for now：{0}%\n time reste：{1}jour{2}hour{3}min{4}sec", pc, tsp.Days, tsp.Hours, tsp.Minutes, tsp.Seconds);
            this.tbInfo.Text = msg;
        }*/

        // Exemple de code pour la conception d'une ApplicationBar localisée
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Définit l'ApplicationBar de la page sur une nouvelle instance d'ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Crée un bouton et définit la valeur du texte sur la chaîne localisée issue d'AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Crée un nouvel élément de menu avec la chaîne localisée d'AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
        void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    if (watcher.Permission == GeoPositionPermission.Denied)
                    {
                        //statusTextBlock.Text = "You have disabled Location Service.";
                    }
                    else
                    {
                        //statusTextBlock.Text = "Location Service is not functioning on this device.";
                    }
                    break;
                case GeoPositionStatus.Initializing:
                    //statusTextBlock.Text = "Location Service is retrieving data...";
                    break;
                case GeoPositionStatus.NoData:
                    //statusTextBlock.Text = "Location data is not available.";
                    break;
                case GeoPositionStatus.Ready:
                    //statusTextBlock.Text = "Location data is available.";
                    break;
            }
        }
        void startLocServInBackground()
        {
            watcher.TryStart(true, TimeSpan.FromMilliseconds(60000));
        }
        private void trackMe_Click(object sender, RoutedEventArgs e)
        {
            if (trackingOn)
            {
                trackMe.Content = "Track Me On Map";
                trackingOn = false;
                myMap.ZoomLevel = 1.0f;
            }
            else
            {
                trackMe.Content = "Stop Tracking";
                trackingOn = true;
                myMap.ZoomLevel = 16.0f;
            }
        }

        private void startStop_Click(object sender, RoutedEventArgs e)
        {
            if (startStop.Content.ToString() == "Stop LocServ")
            {
                startStop.Content = "Start LocServ";
                //statusTextBlock.Text = "Location Services stopped...";
                watcher.Stop();
            }
            else if (startStop.Content.ToString() == "Start LocServ")
            {
                startStop.Content = "Stop LocServ";
                //statusTextBlock.Text = "Starting Location Services...";
                new Thread(startLocServInBackground).Start();
            }
        }
        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
         
            latitudeTextBlock.Text = e.Position.Location.Latitude.ToString("0.0000000000000");
            this.myCurrentLatitude = e.Position.Location.Latitude;
            this.myCurrentLongitude = e.Position.Location.Longitude;
            longitudeTextBlock.Text = e.Position.Location.Longitude.ToString("0.0000000000000");
            //batteryUsed.Text = e.Position.Location.Speed.ToString("0.0") + " meters per second";
           
            //coursereadout.Text = e.Position.Location.Course.ToString("0.0");
            //coursereadout.Text = e.Position.Location.Course.ToString("0.0");

            
            if (trackingOn)
            {
                //try to save on DB
                System.Diagnostics.Debug.WriteLine("test debug " + e.Position.Location);
                DateTime date = DateTime.Now;
                var myBattery = Battery.GetDefault();
                //insertNewLocationsValue(date, e.Position.Location.Longitude, e.Position.Location.Latitude, myBattery.RemainingChargePercent, Places.UNKNOWN);
                //insertNewBatteryUsageValue(Places.UNKNOWN, 55);
                enteringAPlace(currentPlace);


                //System.Diagnostics.Debug.WriteLine("test debug " + e.Position.Location);
                //System.Diagnostics.Debug.WriteLine("test 32 average " + getAverageBatteryUsage(Places.UNKNOWN));

                myPushpin.Location = e.Position.Location;
                myMap.Center = e.Position.Location;
                /*Grid MyGrid = new Grid();
                MyGrid.RowDefinitions.Add(new RowDefinition());
                MyGrid.RowDefinitions.Add(new RowDefinition());
                MyGrid.Background = new SolidColorBrush(Colors.Transparent);
                Polygon MyPolygon = new Polygon();
                MyPolygon.Points.Add(new Point(2, 0));
                MyPolygon.Points.Add(new Point(22, 0));
                MyPolygon.Points.Add(new Point(2, 40));
                MyPolygon.Stroke = new SolidColorBrush(Colors.Black);
                MyPolygon.Fill = new SolidColorBrush(Colors.Black);
                MyPolygon.SetValue(Grid.RowProperty, 1);
                MyPolygon.SetValue(Grid.ColumnProperty, 0);

                //Adding the Polygon to the Grid
                MyGrid.Children.Add(MyPolygon);
                //Creating a MapOverlay and adding the Grid to it.
                MapOverlay MyOverlay = new MapOverlay();
                MyOverlay.Content = MyGrid;
                MyOverlay.GeoCoordinate = e.Position.Location;
                MyOverlay.PositionOrigin = new Point(0, 0.5);
                MapLayer MyLayer = new MapLayer();
                MyLayer.Add(MyOverlay);
                if (myMap.Layers.Contains(MyLayer) == false)
                {
                    myMap.Layers.Add(MyLayer);
                }*/

                /*double[] d = new double[] { e.Position.Location.Latitude, e.Position.Location.Longitude};

                //double[][] rans = Place_symylator.randomPlace(d, 0.0005);
                double[][] rans = getLocationFromDB();
                myMap.Layers.Clear();
                for (int i = 0; i < rans.Length; i++)
                {
                    //ShowMyLocationOnTheMap(rans[i][0], rans[i][1], tabColor[0]);
                }*/
                //myMap.Layers.Clear();
                ShowMyLocationOnTheMap(e.Position.Location.Latitude, e.Position.Location.Longitude, tabColor[Math.Max(0,(int)currentPlace)]);
            }
            else
            {
                //leavingCurrentPlace();
            }
        }

        double[][] getLocationFromDB()
        {
            List<Locations> l = getAllLocation();
            double[] d;
            double[][] ds = new double[l.Count][];
            int i =0;
            foreach (Locations m in l)
            {
                d = new double[] { m.LocationsLatitude, m.LocationsLongitude };
                ds[i] = d;
                i++;
            }
            return ds;
        }
        /*
            Entering in a new place = check if enough battery compared to the former battery usage data
            and start recording the battery usage to improve the datas.
        */
        void enteringAPlace(Places newPlace)
        {
            this.currentPlace = newPlace;
            //current battery level
            var myBattery = Battery.GetDefault();
            this.batteryLevelWhenEnteringCurrentPlace = myBattery.RemainingChargePercent;

            System.Diagnostics.Debug.WriteLine("DEBUG remaining battery " + batteryLevelWhenEnteringCurrentPlace);
            DateTime date = DateTime.Now;
            insertNewLocationsValue(date, myCurrentLongitude, myCurrentLatitude, batteryLevelWhenEnteringCurrentPlace, newPlace);
            insertNewBatteryUsageValue(newPlace, batteryLevelWhenEnteringCurrentPlace);
            //Display the average of all the values in BatteryUsageTable for this newPlace
            getAverageBatteryUsage(newPlace);
            //start recording the positions and save it with the Place name

            //launch a timeout which call insertNewLocationsValue every minute

        }
        /*
            Leaving the current place. This suppose that we previously entered a place.
            It's now time to check the battery level, 
        */
        void leavingCurrentPlace()
        {
            //How much did we used the battery at that place ?
            var myBattery = Battery.GetDefault();
            //By doing this we also suppose that the user didn't charge his phone (otherwise the value will be negative)
            //int batteryUsage = this.batteryLevelWhenEnteringCurrentPlace - myBattery.RemainingChargePercent; 
            //Here we have to Save the data usage for currentPlace (batteryUsage)
            insertNewBatteryUsageValue(currentPlace, myBattery.RemainingChargePercent);

            this.currentPlace = Places.UNKNOWN;

        }

        void insertNewBatteryUsageValue(Places place, int batteryUsageValue)
        {
            // Create a new  item based on the text box.
            BatteryUsageDB newbatteryUsage = new BatteryUsageDB { Place = (int)place , BatteryUsageValue = batteryUsageValue};

            // Add anew item to the observable collection.
            BatteryUsage.Add(newbatteryUsage);
            System.Diagnostics.Debug.WriteLine("DEBUG batteryusage size" + BatteryUsage.Count);

            // Add a to-do item to the local database.
            locationsBaterryDB.batteryUsage.InsertOnSubmit(newbatteryUsage);
        }

        List<Locations> getAllLocation()
        {
            /*IList<Locations> list = null;
            var locationsInDB = from bu in locationsBaterryDB.locations
                                select bu;
            list =  locationsInDB.ToList();
            List<Locations> l = new List<Locations>();
            foreach (Locations m in l)
            {
                l.Add(m);
            }
            System.Diagnostics.Debug.WriteLine("locations count : " + l.Count);
            if(l.Count>0)
                System.Diagnostics.Debug.WriteLine("locations : " + l.First().ToString());
             */
            List<Locations> l = Locations.ToList();
            //foreach (Locations m in l)
                //System.Diagnostics.Debug.WriteLine("locations All: " + m.LocationsID + " " + m.LocationsLatitude + " " + m.LocationsLongitude + " " + m.LocationsPlace + " " + m.LocationsTime + " " + m.LocationsBatteryLevel);
            return l;
        }
        void insertNewLocationsValue(DateTime date, Double longitude, Double latitude, int battery_level, Places place)
        {
            // Create a new item based on the text box.
            Locations newLocation = new Locations { LocationsTime = date, LocationsLongitude = longitude, LocationsLatitude = latitude , LocationsBatteryLevel = battery_level , LocationsPlace = (int)place};

            // Add anew item to the observable collection.
            Locations.Add(newLocation);

            //System.Diagnostics.Debug.WriteLine("DEBUG locations size" + Locations.Count);
            

            // Add a to-do item to the local database.
            locationsBaterryDB.locations.InsertOnSubmit(newLocation);
            //foreach (Locations m in getAllLocation())
               // System.Diagnostics.Debug.WriteLine("locations id : " + m.LocationsTime);
        }
        int getAverageBatteryUsage(Places place)
        {
            int average = 0;
           
            int cpt = 0;
            foreach (BatteryUsageDB batteryUsagesValue in BatteryUsage)
            {
                cpt++;
                average += batteryUsagesValue.BatteryUsageValue;
                //System.Diagnostics.Debug.WriteLine("DEBUG batteryUsageValue loop value" + batteryUsagesValue.BatteryUsageValue);
                //System.Diagnostics.Debug.WriteLine("DEBUG batteryUsageValue loop average" + average);
            }
            if (cpt == 0)
            {
                return 0;
            }
            return (int)(average/ cpt);
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            placetoshow = Places.BEACH;
            currentPlace = Places.BEACH;
            place.Text = "beach";
            double d = averageBatUsedByDay(Places.BEACH);
            String s = "" + d;
            batteryUsed.Text = s;
            if (d > 60)
            {
                MessageBox.Show("You shell need battery charge to go to the beach, last time you used " + d + "% battery", "Attention", MessageBoxButton.OKCancel);
            }
           
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            placetoshow = Places.SCHOOL;
            currentPlace = Places.SCHOOL;
            place.Text = "school";
            double d = averageBatUsedByDay(Places.SCHOOL);
            String s = "" + d;
            batteryUsed.Text = s;
            if (d > 60)
            {
                MessageBox.Show("You shell need battery charge to go to school, last time you used " + d + "% battery", "Attention", MessageBoxButton.OKCancel);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            placetoshow = Places.HOME;
            currentPlace = Places.HOME;
            place.Text = "home";
            //List<double> bl = batUsedByDay(Places.HOME);
            //double d = (bl.Count != 0) ? bl[bl.Count-1] : -1;
            double d = averageBatUsedByDay(Places.HOME);
            String s =""+d;
            batteryUsed.Text = s;
            if(d>60){
                MessageBox.Show("You shell need battery charge to go home, last time you used "+d+"% battery", "Attention", MessageBoxButton.OKCancel);
            }
            //MessageBox.Show("Hello Word");
        }
        double averageBatUsedByDay(Places p)
        {
            double d = 0;
            List<double> bl = batUsedByDay(p);
            if (bl.Count > 0) { 
                for (int i = 0; i < bl.Count; i++ )
                {
                    d += bl[i];
                }   
            }
            return d/Math.Max(1,bl.Count-1);
        }


    }


    /*

        DATABASE STUFF


    */
    public class LocationsAndBatteryDataContext : DataContext
    {
        // Specify the connection string as a static, used in main page and app.xaml.
        public static string DBConnectionString = "Data Source=isostore:/BatteryUsage.sdf";

        // Pass the connection string to the base class.
        public LocationsAndBatteryDataContext(string connectionString)
            : base(connectionString)
        { }

        public Table<Locations> locations;
        public Table<BatteryUsageDB> batteryUsage;
    }

    [Table]
    public class Locations : INotifyPropertyChanged, INotifyPropertyChanging
    {
        // Define ID: private field, public property and database column.
        private int _locationsId;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int LocationsID
        {
            get
            {
                return _locationsId;
            }
            set
            {
                if (_locationsId != value)
                {
                    NotifyPropertyChanging("LocationsID");
                    _locationsId = value;
                    NotifyPropertyChanged("LocationsID");
                }
            }
        }

        private DateTime _locationstime;

        [Column]
        public DateTime LocationsTime
        {
            get
            {
                return _locationstime;
            }
            set
            {
                if (_locationstime != value)
                {
                    NotifyPropertyChanging("LocationsTime");
                    _locationstime = value;
                    NotifyPropertyChanged("LocationsTime");
                }
            }
        }

        private Double _locationsLatitude;

        [Column]
        public Double LocationsLatitude
        {
            get
            {
                return _locationsLatitude;
            }
            set
            {
                if (_locationsLatitude != value)
                {
                    NotifyPropertyChanging("LocationsLatitude");
                    _locationsLatitude = value;
                    NotifyPropertyChanged("LocationsLatitude");
                }
            }
        }

        private Double _locationsLongitude;

        [Column]
        public Double LocationsLongitude
        {
            get
            {
                return _locationsLongitude;
            }
            set
            {
                if (_locationsLongitude != value)
                {
                    NotifyPropertyChanging("LocationsLongitude");
                    _locationsLongitude = value;
                    NotifyPropertyChanged("LocationsLongitude");
                }
            }
        }

        private int _locationsBatteryLevel;

        [Column]
        public int LocationsBatteryLevel
        {
            get
            {
                return _locationsBatteryLevel;
            }
            set
            {
                if (_locationsBatteryLevel != value)
                {
                    NotifyPropertyChanging("LocationsBatteryLevel");
                    _locationsBatteryLevel = value;
                    NotifyPropertyChanged("LocationsBatteryLevel");
                }
            }
        }

        private int _locationsPlace;

        [Column]
        public int LocationsPlace
        {
            get
            {
                return _locationsPlace;
            }
            set
            {
                if (_locationsPlace != value)
                {
                    NotifyPropertyChanging("LocationsPlace");
                    _locationsPlace = value;
                    NotifyPropertyChanged("LocationsPlace");
                }
            }
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify the page that a data context property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;

        // Used to notify the data context that a data context property is about to change
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        #endregion
    }

    [Table]
    public class BatteryUsageDB : INotifyPropertyChanged, INotifyPropertyChanging
    {
        // Define ID: private field, public property and database column.
        private int _batteryUsageId;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int LocationsID
        {
            get
            {
                return _batteryUsageId;
            }
            set
            {
                if (_batteryUsageId != value)
                {
                    NotifyPropertyChanging("LocationsID");
                    _batteryUsageId = value;
                    NotifyPropertyChanged("LocationsID");
                }
            }
        }

        private int _place;

        [Column]
        public int Place
        {
            get
            {
                return _place;
            }
            set
            {
                if (_place != value)
                {
                    NotifyPropertyChanging("Place");
                    _place = value;
                    NotifyPropertyChanged("Place");
                }
            }
        }

        private int _batteryUsageValue;

        [Column]
        public int BatteryUsageValue
        {
            get
            {
                return _batteryUsageValue;
            }
            set
            {
                if (_batteryUsageValue != value)
                {
                    NotifyPropertyChanging("BatteryUsageValue");
                    _batteryUsageValue = value;
                    NotifyPropertyChanged("BatteryUsageValue");
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify the page that a data context property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;

        // Used to notify the data context that a data context property is about to change
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        #endregion
    }



}