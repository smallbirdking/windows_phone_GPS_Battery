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

namespace PhoneApp2
{
    public partial class MainPage : PhoneApplicationPage
    {
        GeoCoordinateWatcher watcher;

        bool trackingOn = false;

        Pushpin myPushpin = new Pushpin();
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
    }
}