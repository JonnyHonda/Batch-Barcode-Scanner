using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static Batch_Barcode_Scanner.ScanSKUDataBase;

namespace Batch_Barcode_Scanner
{
    [Activity(WindowSoftInputMode = SoftInput.StateAlwaysHidden, Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = false)]
    public class SettingsActivity : AppCompatActivity
    {
        RegExList regExList;
        EditText TrackingScan;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestedOrientation = ScreenOrientation.Portrait;
            Context AppContext = Application.Context;
            AppPreferences applicationPreferences = new AppPreferences(AppContext);
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_settings);
            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            // Load up any stored applicationPreferences
            TextView submitDataUrl = FindViewById<TextView>(Resource.Id.submit_data_url);
            submitDataUrl.Text = applicationPreferences.GetAccessKey("submitDataUrl");
            submitDataUrl.Text = submitDataUrl.Text.TrimEnd('\r', '\n');

            TextView loadConfigUrl = FindViewById<TextView>(Resource.Id.load_config_url);
            loadConfigUrl.Text = applicationPreferences.GetAccessKey("loadConfigUrl");
            loadConfigUrl.Text = loadConfigUrl.Text.TrimEnd('\r', '\n');

            TextView applicationKey = FindViewById<TextView>(Resource.Id.application_key);
            applicationKey.Text = applicationPreferences.GetAccessKey("applicationKey");
            applicationKey.Text = applicationKey.Text.TrimEnd('\r', '\n');

            TextView retentionPeriod = FindViewById<TextView>(Resource.Id.retention_period);
            retentionPeriod.Text = applicationPreferences.GetAccessKey("retentionPeriod");
            retentionPeriod.Text = retentionPeriod.Text.TrimEnd('\r', '\n');

            string databasePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                 GetString(Resource.String.database_name));
            SQLiteConnection databaseConnection = new SQLiteConnection(databasePath);
            databaseConnection.CreateTable<ScanSKUDataBase.TrackingNumberPatterns>();

            PopulateRegExView();
            TrackingScan = FindViewById<EditText>(Resource.Id.txtentry);


            TrackingScan.Text = "";
            TrackingScan.RequestFocus();

            TrackingScan.KeyPress += (object sender, View.KeyEventArgs e) =>
            {
                if ((e.Event.Action == KeyEventActions.Down) && (e.KeyCode == Keycode.Enter))
                {
                    if (e.Event.RepeatCount == 0)
                    {

                        string jsonstring = TrackingScan.Text;
                        //jsonstring = Regex.Replace(jsonstring, @"\s+", "");
                        Configuration configuration = new Configuration();
                        try
                        {
                            configuration = JsonConvert.DeserializeObject<Configuration>(jsonstring);
                            if (configuration.UpdateConfiguration.Count == 1)
                            {
                                foreach (UpdateConfiguration configItem in configuration.UpdateConfiguration)
                                {
                                    submitDataUrl.Text = configItem.UploadEndPoint.ToString();
                                    loadConfigUrl.Text = configItem.RegexEndPoint.ToString();
                                    applicationKey.Text = configItem.ApplicationKey.ToString();
                                    retentionPeriod.Text = configItem.RetentionPeriod.ToString();
                                }
                                // Save some application preferences
                                applicationPreferences.SaveAccessKey("submitDataUrl", submitDataUrl.Text, true);
                                applicationPreferences.SaveAccessKey("loadConfigUrl", loadConfigUrl.Text, true);
                                applicationPreferences.SaveAccessKey("applicationKey", applicationKey.Text, true);
                                applicationPreferences.SaveAccessKey("retentionPeriod", retentionPeriod.Text, true);
                                try
                                {
                                     applicationPreferences.SaveAccessKey("serialNumber", Android.OS.Build.Serial.ToString(), true);
                                }
                                catch
                                {
                                    applicationPreferences.SaveAccessKey("serialNumber", "", true);
                                }
                                Log.Info("TAG-SETTINGS", "Settings - Call FetchTrackingRegExData");
                                System.Threading.Tasks.Task taskA = System.Threading.Tasks.Task.Factory.StartNew(() => FetchTrackingRegExData(loadConfigUrl.Text));
                                taskA.Wait();
                                Toast.MakeText(this, "Config QR code read successful", ToastLength.Long).Show();

                            }
                        }
                        catch (Exception ex)
                        {
                            // Any Error in the above block will cause this catch to fire - Even if the json keys don't exist
                            Toast.MakeText(this, "Config QR code not recognised", ToastLength.Long).Show();
                        }
                        PopulateRegExView();
                        TrackingScan.Text = "";
                    }
                }
            };

        }

        private void PopulateRegExView()
        {
            TextView RegExView = FindViewById<TextView>(Resource.Id.regex_viewer);
            RegExView.Text = "";
            regExList = new RegExList();
            for (var i = 0; i < regExList.NumPatterns; i++)
            {
                RegExView.Text += regExList[i].Courier + ":" + regExList[i].RegexString+ System.Environment.NewLine;
            }

        }


        private void FetchTrackingRegExData(string httpRegExPatternEndPoint)
        {
            string jsonTrackingRegexs = null;
            //string httpRegExPatternEndPoint = intent.GetStringExtra("httpEndPoint");

            string databasePath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
             GetString(Resource.String.database_name));
            SQLiteConnection databaseConnection = new SQLiteConnection(databasePath);
            // Delete the current Regex data
            try
            {
                Log.Info("TAG-SETTINGS", "Delete Exisiting data");
                databaseConnection.DeleteAll<ScanSKUDataBase.TrackingNumberPatterns>();
            }
            catch
            {
                Log.Info("TAG-SETTINGS", "Unable to delete Exisiting data");
            }

            // Attempt to fetch the new data, on fail use a hard coded set
            try
            {
                using (var webClient = new System.Net.WebClient())
                {
                    jsonTrackingRegexs = webClient.DownloadString(httpRegExPatternEndPoint);
                    Log.Info("TAG-SETTINGS", "DownLoad Regexs");
                }
            }
            catch (Exception e)
            {
                Log.Info("TAG-SETTINGS", "Loading regexs failed");
                jsonTrackingRegexs = "[{\"Failed\": \"/" + e.Message + "/\"}]";
            }
            databaseConnection.CreateTable<TrackingNumberPatterns>();


            List<Dictionary<string, string>> obj = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonTrackingRegexs);

            foreach (Dictionary<string, string> lst in obj)
            {
                foreach (KeyValuePair<string, string> item in lst)
                {
                    // @Todo: There is an ecoding bug here, in the DX numbers because /b encodes incorrectly
                    string testText = item.Value;
                    int startIndex = testText.IndexOf('/');
                    int endIndex = testText.LastIndexOf('/');
                    string patternString = testText.Substring(startIndex + 1, endIndex - startIndex - 1);
                    TrackingNumberPatterns record = new TrackingNumberPatterns
                    {
                        Courier = item.Key,
                        Pattern = patternString,
                        IsEnabled = true
                    };
                    databaseConnection.Insert(record);
                }
            }

            Log.Info("TAG-SETTINGS", "FetchTrackingRegExData Complete");
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_settings, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_main:
                    StartActivity(typeof(MainActivity));
                    break;

            }
            return base.OnOptionsItemSelected(item);
        }
    }
}