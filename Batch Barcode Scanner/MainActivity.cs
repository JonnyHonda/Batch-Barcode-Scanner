using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using static Batch_Barcode_Scanner.ScanSKUDataBase;
using Permission = Android.Content.PM.Permission;


namespace Batch_Barcode_Scanner
{
    [Activity(WindowSoftInputMode = SoftInput.StateAlwaysHidden, Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ILocationListener
    {
        static readonly int REQUEST_LOCATION = 1;
        // static readonly Keycode SCAN_BUTTON = (Keycode)301;
        SQLiteConnection databaseConnection = null;
        string databasePath;

        TextView coordinates;
        Location currentLocation;
        LocationManager locationManager;
        string locationProvider;

        MediaPlayer mediaPlayer;

        // RecyclerView.LayoutManager mLayoutManager;
        // TrackingNumberDataAdapter mAdapter;
        BarcodeScannerList mBarcodeScannerList;
        EditText TrackingScan;
        Guid batch;
        string batchnumber;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestedOrientation = ScreenOrientation.Portrait;
            Context AppContext = Application.Context;
            AppPreferences applicationPreferences = new AppPreferences(AppContext);
            // Check application Preferences have been saved previously if not open Settings Activity and wait there.
            if (
                string.IsNullOrEmpty(applicationPreferences.GetAccessKey("submitDataUrl")) ||
                string.IsNullOrEmpty(applicationPreferences.GetAccessKey("loadConfigUrl")) ||
                string.IsNullOrEmpty(applicationPreferences.GetAccessKey("applicationKey")) ||
                string.IsNullOrEmpty(applicationPreferences.GetAccessKey("retentionPeriod"))
                )
            {
                // No, well start the setting activity
                StartActivity(typeof(SettingsActivity));
            }
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            // We only want to create a batch number here once when the app first starts and not everytime the activity loads
            if (batch == Guid.Empty)
            {
                SetBatchNumber(false);

            }
            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            databasePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                GetString(Resource.String.database_name));
            databaseConnection = new SQLiteConnection(databasePath);
            // Create the ParcelScans table
            databaseConnection.CreateTable<ScanSKUDataBase.ParcelScans>();
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == (int)Permission.Granted)
            {
                mediaPlayer = MediaPlayer.Create(this, Resource.Raw.beep_07);
                TrackingNumberDataProvider();

                // We have permission, go ahead and use the GPS.
                Log.Debug("GPS", "We have permission, go ahead and use the GPS.");
                InitializeLocationManager();

                coordinates = FindViewById<TextView>(Resource.Id.footer_text);
                TrackingScan = FindViewById<EditText>(Resource.Id.txtentry);
                TrackingScan.Text = "";
                TrackingScan.RequestFocus();
                TrackingScan.SetOnKeyListener(new MyKeyListener(this));
            }
            else
            {
                // GPS permission is not granted. If necessary display rationale & request.
                Log.Debug("GPS", "GPS permission is not granted");

                if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation))
                {
                    // Provide an additional rationale to the user if the permission was not granted
                    // and the user would benefit from additional context for the use of the permission.
                    // For example if the user has previously denied the permission.
                    Log.Info("GPS", "Displaying GPS permission rationale to provide additional context.");
                    CoordinatorLayout rootView = FindViewById<CoordinatorLayout>(Resource.Id.root_view);


                    String[] requiredPermissions = new String[] { Manifest.Permission.AccessFineLocation };
                    ActivityCompat.RequestPermissions(this, requiredPermissions, REQUEST_LOCATION);
                }
                else
                {
                    ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.AccessFineLocation }, REQUEST_LOCATION);
                }

            }

        }

        /// <summary>
        /// Provides the data adapter for the RecyclerView
        /// This simple gets all the current tracking numbers and populates the recycler
        /// </summary>
        private void TrackingNumberDataProvider()
        {
            mBarcodeScannerList = new BarcodeScannerList();
            mBarcodeScannerList.FetchUnCollected();
            TextView mRecyclerView = FindViewById<TextView>(Resource.Id.tracking_list);
            for (var i = mBarcodeScannerList.NumBarcodes - 1; i > -1; i--)
            {
                mRecyclerView.Text += mBarcodeScannerList[i].BarcodeText + System.Environment.NewLine;
            }
        }

        private void SetBatchNumber(bool regenerate)
        {
            Context mContext = Application.Context;
            AppPreferences applicationPreferences = new AppPreferences(mContext);
            if (string.IsNullOrEmpty(applicationPreferences.GetAccessKey("batchnumber")) || regenerate)
            {
                batch = Guid.NewGuid();
                applicationPreferences.SaveAccessKey("batchnumber", batch.ToString());
            }

            batchnumber = applicationPreferences.GetAccessKey("batchnumber");
        }


        private void ExportScanData()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) == (int)Permission.Granted ||
                ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) == (int)Permission.Granted)
            {


                List<ParcelScans>  parcelScans = databaseConnection.Query<ScanSKUDataBase.ParcelScans>("SELECT * FROM ParcelScans");

                string fileName = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") + ".csv";
                // Set a variable to the Documents path.
                string docPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, Android.OS.Environment.DirectoryDownloads);
                string filepath = (Path.Combine(docPath, fileName));
                using (StreamWriter outputFile = new StreamWriter(filepath))
                {
                    foreach (ParcelScans parcelScan in parcelScans)
                        outputFile.WriteLine(parcelScan.ToCSV());
                }

                // Notify the user about the completed "download"
                DownloadManager downloadManager = DownloadManager.FromContext(Android.App.Application.Context);
                downloadManager.AddCompletedDownload(fileName, "Barcode Scan Data Export", true, "application/txt", filepath, File.ReadAllBytes(filepath).Length, true);
            }
            else

                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.WriteExternalStorage, Manifest.Permission.ReadExternalStorage }, 2);

        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            switch (item.ItemId)
            {

                case Resource.Id.menu_settings:
                    StartActivity(typeof(SettingsActivity));
                    break;

                case Resource.Id.menu_signature:
                    //Context mContext = Application.Context;
                    //AppPreferences applicationPreferences = new AppPreferences(mContext);
                    //applicationPreferences.SaveAccessKey("lastKnownLongitude", currentLocation.Longitude.ToString());
                    //applicationPreferences.SaveAccessKey("lastKnownLatitude", currentLocation.Latitude.ToString());
                    StartActivity(typeof(SignaturPadActivity));
                    break;
                case Resource.Id.menu_sqldata:
                    StartActivity(typeof(SqliteActivity));
                    break;

                case Resource.Id.menu_about:
                    StartActivity(typeof(AboutActivity));
                    break;

                case Resource.Id.menu_exportdata:
                    ExportScanData();
                    break;
                case Resource.Id.menu_exit:
                    // This should exit the app
                    this.FinishAffinity();
                    break;
                    ;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            StartActivity(typeof(SignaturPadActivity));
        }

  /*
    * From here on these functions releate to GPS and GPS permissions
    * 
    * 
    *
    **/
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == REQUEST_LOCATION)
            {

            }
            else if (requestCode == 2 || requestCode == 3)
            {
                ExportScanData();
            }
            else
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            }
        }

        private void InitializeLocationManager()
        {
            locationManager = (LocationManager)GetSystemService(LocationService);
            Criteria criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };
            IList<string> acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService, true);
            if (acceptableLocationProviders.Any())
            {
                locationProvider = acceptableLocationProviders.First();
            }
            else
            {
                locationProvider = string.Empty;
            }
            Log.Debug("GPS", "Using " + locationProvider + ".");
        }

        public void OnLocationChanged(Location location)
        {
            currentLocation = location;
            Context mContext = Application.Context;
            AppPreferences applicationPreferences = new AppPreferences(mContext);
            if (currentLocation == null)
            {
                TrackingScan.SetBackgroundColor(Android.Graphics.Color.LightPink);
                coordinates.Text = "No GPS fix yet ";
                //Error Message  
            }
            else
            {
                applicationPreferences.SaveAccessKey("lastKnownLongitude", currentLocation.Longitude.ToString());
                applicationPreferences.SaveAccessKey("lastKnownLatitude", currentLocation.Latitude.ToString());
                coordinates.Text = "Lat:" + currentLocation.Latitude.ToString(("#.00000")) + " / Long:" + currentLocation.Longitude.ToString(("#.00000"));
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            try
            {
                locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);
            }
            catch (Exception ex)
            {
                Log.Debug("GPS", "Error creating location service: " + ex.Message);
            }

        }

        protected override void OnPause()
        {
            base.OnPause();
            try
            {
                locationManager.RemoveUpdates(this);
            }
            catch (Exception ex)
            {
                Log.Debug("GPS", "Error creating location service: " + ex.Message);
            }
        }

        public void OnProviderDisabled(string provider)
        {
            throw new NotImplementedException();
        }

        public void OnProviderEnabled(string provider)
        {
            throw new NotImplementedException();
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            // throw new NotImplementedException();
        }

    }
    public class MyKeyListener : Java.Lang.Object, View.IOnKeyListener
    {
        readonly MainActivity activity;
        SQLiteConnection databaseConnection = null;

        String databasePath;
        EditText Barcode;
        AppPreferences applicationPreferences;
        bool patternFound;
        MediaPlayer mediaPlayer;
        string batchnumber;
        Context mContext;


        public MyKeyListener(MainActivity _activity)
        {
            activity = _activity; 
             mContext = Application.Context;
            applicationPreferences = new AppPreferences(mContext);
            databasePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                "localscandata.db3");
            databaseConnection = new SQLiteConnection(databasePath);
            mediaPlayer = MediaPlayer.Create(mContext, Resource.Raw.beep_07);
            batchnumber = applicationPreferences.GetAccessKey("batchnumber");
        }

        public bool OnKey(View v, Keycode keyCode, KeyEvent e)
        {
            if (e.KeyCode == Keycode.Enter && e.Action == 0)
            {
                /// need to regex the scan against the Tracking Patterns
                /// 
                TableQuery<TrackingNumberPatterns> trackingPatterns = databaseConnection.Table<TrackingNumberPatterns>();
                Barcode = (EditText)v;
                bool patternFound = false;
                try
                {
                    foreach (var trackingPattern in trackingPatterns)
                    {
                        Match m = Regex.Match(Barcode.Text, @trackingPattern.Pattern, RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            patternFound = true;
                        }
                    }
                }
                catch { }

                if (patternFound)
                {
                    ParcelScans newScan = new ScanSKUDataBase.ParcelScans
                    {
                        TrackingNumber = Barcode.Text.ToUpper(),
                        ScanTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                        Batch = applicationPreferences.GetAccessKey("batchnumber"),
                        Sent = null
                    };
                    try
                    {
                        newScan.Longitude = Convert.ToDouble(applicationPreferences.GetAccessKey("lastKnownLongitude"));
                            }
                    catch
                    {
                        newScan.Longitude = null;
                    }
                    try
                    {
                        newScan.Latitude = Convert.ToDouble(applicationPreferences.GetAccessKey("lastKnownLLatitude"));
                    }
                    catch
                    {
                        newScan.Latitude = null;
                    }
                    try
                    {
                        databaseConnection.Insert(newScan);
                        TextView TrackingListView = activity.FindViewById<TextView>(Resource.Id.tracking_list);
                        TrackingListView.Text = Barcode.Text.ToUpper() + System.Environment.NewLine + TrackingListView.Text;

                        mediaPlayer.Start();
                    }
                    catch (SQLiteException ex)
                    {
                               Toast.MakeText(mContext, "Scan Error : Duplicated Barcode Scan", ToastLength.Long).Show();
                        Log.Info("SCANNER", "Scan Error : " + ex.Message);

                    }
                }
                else
                {
                            Toast.MakeText(mContext, "Barcode format not recognised", ToastLength.Short).Show();
                }

                Barcode.RequestFocus();
                Barcode.Text = "";

                return true;
            }
            return false;
        }
    }
}




