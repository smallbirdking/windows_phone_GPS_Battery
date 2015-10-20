using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhoneApp2.Resources;
using Microsoft.Phone.Controls.Maps;
using System.Device.Location;
using System.Threading;
using Windows.Phone.Devices.Power;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Collections.ObjectModel;


namespace PhoneApp2
{
    public enum Places{
        UNKNOWN = -1,  WORK = 0, HOME = 1
    }
    public partial class MainPage : PhoneApplicationPage
    {
        GeoCoordinateWatcher watcher;
        Places currentPlace;
        bool trackingOn = false;

        Pushpin myPushpin = new Pushpin();
        TimeSpan batteryLevelWhenEnteringCurrentPlace;
        // Constructeur
        public MainPage()
        {
            InitializeComponent();
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            watcher.MovementThreshold = 10.0f;
            watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            new Thread(startLocServInBackground).Start();
            statusTextBlock.Text = "Starting Location Service...";

            // Exemple de code pour la localisation d'ApplicationBar
            //BuildLocalizedApplicationBar();
           
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前手机默认电池的Battery实例
            var myBattery = Battery.GetDefault();
            // 获取剩余电量
            int pc = myBattery.RemainingChargePercent;
            // 获取剩余续航时间
            TimeSpan tsp = myBattery.RemainingDischargeTime;
            string msg = string.Format("for now：{0}%\n time reste：{1}jour{2}hour{3}min{4}sec", pc, tsp.Days, tsp.Hours, tsp.Minutes, tsp.Seconds);
            this.tbInfo.Text = msg;
        }

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
                        statusTextBlock.Text = "You have disabled Location Service.";
                    }
                    else
                    {
                        statusTextBlock.Text = "Location Service is not functioning on this device.";
                    }
                    break;
                case GeoPositionStatus.Initializing:
                    statusTextBlock.Text = "Location Service is retrieving data...";
                    break;
                case GeoPositionStatus.NoData:
                    statusTextBlock.Text = "Location data is not available.";
                    break;
                case GeoPositionStatus.Ready:
                    statusTextBlock.Text = "Location data is available.";
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
                statusTextBlock.Text = "Location Services stopped...";
                watcher.Stop();
            }
            else if (startStop.Content.ToString() == "Start LocServ")
            {
                startStop.Content = "Stop LocServ";
                statusTextBlock.Text = "Starting Location Services...";
                new Thread(startLocServInBackground).Start();
            }
        }
        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            latitudeTextBlock.Text = e.Position.Location.Latitude.ToString("0.0000000000000");
            longitudeTextBlock.Text = e.Position.Location.Longitude.ToString("0.0000000000000");
            speedreadout.Text = e.Position.Location.Speed.ToString("0.0") + " meters per second";
            coursereadout.Text = e.Position.Location.Course.ToString("0.0");
            coursereadout.Text = e.Position.Location.Course.ToString("0.0");
            if (trackingOn)
            {
                myPushpin.Location = e.Position.Location;
                myMap.Center = e.Position.Location;
                if (myMap.Children.Contains(myPushpin) == false)
                {
                    myMap.Children.Add(myPushpin);
                }
            }
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
            int pc = myBattery.RemainingChargePercent;
            this.batteryLevelWhenEnteringCurrentPlace = myBattery.RemainingDischargeTime;

            //start recording the positions and save it with the Place name
            // newPlace | long | lat
        }
        /*
            Leaving the current place. This suppose that we previously entered a place.
            It's now time to check the battery level, 
        */
        void leavingCurrentPlace()
        {
            //How much did we used the battery at that place ?
            var myBattery = Battery.GetDefault();
            int pc = myBattery.RemainingChargePercent;
            //By doing this we also suppose that the user didn't charge his phone (otherwise the value will be negative)
            TimeSpan batteryUsage = this.batteryLevelWhenEnteringCurrentPlace - myBattery.RemainingDischargeTime;
            //Here we have to Save the data usage for currentPlace (batteryUsage)

            this.currentPlace = Places.UNKNOWN;

        }


    }
    public class LocationsAndBatteryDataContext : DataContext
    {
        // Specify the connection string as a static, used in main page and app.xaml.
        public static string DBConnectionString = "Data Source=isostore:/ToDo.sdf";

        // Pass the connection string to the base class.
        public LocationsAndBatteryDataContext(string connectionString)
            : base(connectionString)
        { }

        public Table<Locations> locations;
        public Table<BatteryUsage> batteryUsage;
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

        private TimeSpan _locationstime;

        [Column]
        public TimeSpan LocationsTime
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

        private Double _locationsBatteryLevel;

        [Column]
        public Double LocationsBatteryLevel
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
    public class BatteryUsage : INotifyPropertyChanged, INotifyPropertyChanging
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

        private Double _batteryUsageValue;

        [Column]
        public Double BatteryUsageValue
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