using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Util;
using Android.Widget;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Xamarin.Controls;
using Environment = Android.OS.Environment;

namespace Batch_Barcode_Scanner
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = false)]
    public class SignaturPadActivity : AppCompatActivity
    {


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);


            SetContentView(Resource.Layout.activity_signature_pad);

            SignaturePadView signatureView = FindViewById<SignaturePadView>(Resource.Id.signatureView);

            Button btnCancel = FindViewById<Button>(Resource.Id.btnCancel);

            Button btnComplete = FindViewById<Button>(Resource.Id.btnComplete);

            EditText SignatureText = FindViewById<EditText>(Resource.Id.txtSignature);

            /*
            SignatureText.TextChanged += delegate {
                
                    if (SignatureText.Length() > 0)
                    {
                        btnComplete.Enabled = true;
                    }
                    else
                    {
                        btnComplete.Enabled = true;
                    }
                };
                */

            btnCancel.Click += delegate
        {
            StartActivity(typeof(MainActivity));
        };


            btnComplete.Click += async delegate
            {
                string path = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures).AbsolutePath;
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) == (int)Permission.Granted ||
               ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) == (int)Permission.Granted)
                {

                    Context mContext = Application.Context;
                    AppPreferences applicationPreferences = new AppPreferences(mContext);
                    string batch = applicationPreferences.GetAccessKey("batchnumber");
                    string file = System.IO.Path.Combine(path, batch + ".jpg");
                    string result = "";
                    try
                    {
                        using (var bitmap = await signatureView.GetImageStreamAsync(SignatureImageFormat.Jpeg, Color.Black, Color.White, 1f))
                        using (var dest = File.OpenWrite(file))
                        {
                            await bitmap.CopyToAsync(dest);
                        }



                        byte[] data = File.ReadAllBytes(file);
                        // ... Convert byte array to Base64 string.
                        result = Convert.ToBase64String(data);
                        // ... Write Base64 string.
                        Console.WriteLine("ENCODED: " + result);
                    }
                    catch { }
                    PushDataToEndPoint(result, SignatureText.Text);
                }
            };

        }

        private void PushDataToEndPoint(string SignatureImage, string SignatureText)
        {
            Context AppContext = Application.Context;
            AppPreferences ap = new AppPreferences(AppContext);
            string httpEndPoint = ap.GetAccessKey("submitDataUrl");
            string loadConfigUrl = ap.GetAccessKey("loadConfigUrl");
            string applicationKey = ap.GetAccessKey("applicationKey");
            string lastKnownLongitude = ap.GetAccessKey("lastKnownLongitude");
            string lastKnownLatitude = ap.GetAccessKey("lastKnownLatitude");
            string retentionPeriod = ap.GetAccessKey("retentionPeriod");
            string databasePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                 GetString(Resource.String.database_name));
            // Create a Dictionary for the parameters
            Dictionary<string, string> Parameters = new Dictionary<string, string>
                    {
                        { "httpEndPoint", httpEndPoint },
                        { "userAgent", "@string/user_agent" },
                        { "token", applicationKey },
                        { "retentionPeriod", retentionPeriod },
                        {"SignatureImage",SignatureImage },
                        {"SignatureText",SignatureText }

                    };
            try
            {
                Parameters.Add("serialNumber", ap.GetAccessKey("serial" +
                    "Number"));
            }
            catch
            {
                Parameters.Add("serialNumber", "");
            }
            try
            {
                Parameters.Add("lontitude", lastKnownLongitude);
                Parameters.Add("latitude", lastKnownLatitude);
            }
            catch
            {
                Parameters.Add("lontitude", "");
                Parameters.Add("latitude", "");
            }
            Parameters.Add("databasePath", databasePath);

            bool status = false;
            try
            {
                // Run the SubmitCollectionData as a Async Task
                System.Threading.Tasks.Task taskA = System.Threading.Tasks.Task.Factory.StartNew(() => status = SubmitCollectionData(Parameters));

            }
            catch (Exception ex)
            {
                Log.Info("SubmitCollectionData", ex.Message);
            }



            // Create a new Batch number;
            SetBatchNumber(true);
            StartActivity(typeof(MainActivity));
        }


        private void SetBatchNumber(bool regenerate)
        {
            Context AppContext = Application.Context;
            AppPreferences applicationPreferences = new AppPreferences(AppContext);
            if (string.IsNullOrEmpty(applicationPreferences.GetAccessKey("batchnumber")) || regenerate)
            {
                Guid batch = Guid.NewGuid();
                applicationPreferences.SaveAccessKey("batchnumber", batch.ToString());
            }

            string batchnumber = applicationPreferences.GetAccessKey("batchnumber");
        }

        private bool SubmitCollectionData(Dictionary<string, string> parameters)
        {

            Log.Info("TAG-ASYNCTASK", "Beginning SubmitCollectionData");
            string startTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            string httpEndPoint = parameters["httpEndPoint"];
            string lontitude = parameters["lontitude"];
            string latitude = parameters["latitude"];
            string userAgent = parameters["userAgent"];
            string token = parameters["token"];
            string setialNumber = parameters["serialNumber"];
            string retentionPeriod = parameters["retentionPeriod"];
            string SignatureImage = parameters["SignatureImage"];
            string SignatureText = parameters["SignatureText"];

            bool status = true;
            string databasePath = parameters["databasePath"];
            Log.Info("TAG-ASYNCTASK", "Connect to Database");

            SQLiteConnection databaseConnection = new SQLiteConnection(databasePath);
            // Create a new Collection
            Collection collection = new Collection();
            // Set the Base values
            Gps collectionLocation = new Gps();
            try
            {
                collectionLocation.Latitude = Convert.ToDouble(latitude);
                collectionLocation.Longitude = Convert.ToDouble(lontitude);
            }
            catch { }

            // Fetch all the batches that have not been uploaded
             var batchnumbers = databaseConnection.Query<ScanSKUDataBase.ParcelScans>("SELECT Batch FROM ParcelScans WHERE Sent IS null GROUP BY Batch");

            foreach (var batch in batchnumbers)
            {
                collection.Gps = collectionLocation;
                collection.batchnumber = batch.Batch;
                collection.Timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                try { collection.SignatureImage = SignatureImage; } catch { collection.SignatureImage = ""; }
                try { collection.SignatureText = SignatureText; } catch { collection.SignatureText = ""; }

                Log.Info("TAG-ASYNCTASK", "Collection created");

                // regardless of whether we get a successful upload we still must flag the items as being collected, 
                // the assumtion being that they will have been taken away even if the dirver did not upload the collection 
                var parcelScans = databaseConnection.Query<ScanSKUDataBase.ParcelScans>("UPDATE ParcelScans set IsCollected = 1  WHERE Sent IS null and batch=?", collection.batchnumber);

                // Need to select all the scans that have not been uploaded and match the current batch
                parcelScans = databaseConnection.Query<ScanSKUDataBase.ParcelScans>("SELECT * FROM ParcelScans WHERE Sent IS null and batch=?", collection.batchnumber);

                List<Scan> scannedParcelList = new List<Scan>();

                foreach (var parcel in parcelScans)
                {
                    Scan scannedParcelListElement = new Scan();
                    Gps scannedParcelLocation = new Gps();
                    scannedParcelListElement.Timestamp = parcel.ScanTime;
                    // Because Locations can be null
                    try
                    {
                        scannedParcelLocation.Longitude = (double)parcel.Longitude;
                        scannedParcelLocation.Latitude = (double)parcel.Latitude;
                    }
                    catch { }
                    scannedParcelListElement.Barcode = parcel.TrackingNumber;
                    scannedParcelListElement.Gps = scannedParcelLocation;
                    scannedParcelList.Add(scannedParcelListElement);
                }
                collection.Scans = scannedParcelList;
                string jsonToUpload;
                Log.Info("TAG-ASYNCTASK", "JSON Created");
                jsonToUpload = collection.ToJson();
                Log.Info("TAG-ASYNCTASK", jsonToUpload);
                Log.Info("TAG-ASYNCTASK", "Webrequest Created");
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(httpEndPoint);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.UserAgent += userAgent;
                httpWebRequest.Headers["x-scansku-api-key"] = token;
                httpWebRequest.Headers["x-scansku-batch"] = collection.batchnumber;
                httpWebRequest.Headers["x-scansku-serial-number"] = setialNumber;

                try
                {
                    using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(jsonToUpload);
                        streamWriter.Flush();
                        streamWriter.Close();
                    }
                    Log.Info("TAG-ASYNCTASK", "Fetch Response");

                    HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    Log.Info("TAG-ASYNCTASK", "Fetch Response");
                    using (StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        string jsonResult = streamReader.ReadToEnd();
                        RemoteServiceResult result = new RemoteServiceResult();
                        result = JsonConvert.DeserializeObject<RemoteServiceResult>(jsonResult);
                        Log.Info("TAG-ASYNCTASK", jsonResult);

                        if (httpResponse.StatusCode == HttpStatusCode.OK)
                        {
                            Log.Info("TAG-ASYNCTASK", "Success, update parcels");
                            parcelScans = databaseConnection.Query<ScanSKUDataBase.ParcelScans>("UPDATE ParcelScans set Sent=? WHERE Sent IS null and batch=?", startTime, collection.batchnumber);
                        }
                        else
                        {
                            Log.Info("TAG-ASYNCTASK", "Did recieve a success response");

                        }
                    }
                    httpResponse.Close();
                    Log.Info("TAG-ASYNCTASK", "Response Closes");
                }
                catch (Exception ex)
                {
                    Notification.Builder builder = null;
                    try
                    {
                        builder = new Notification.Builder(this, "NOTI_CH_ID");
                    }
                    catch
                    {
                        builder = new Notification.Builder(this);
                    }

                    builder.SetContentTitle("Failed Uploads");
                    builder.SetContentText("There are uploads that may have failed.");
                    builder.SetSmallIcon(Resource.Mipmap.ic_warning_black_24dp);


                    // Build the notification:
                    Notification notification = builder.Build();

                    // Get the notification manager:
                    NotificationManager notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                    const int notificationId = 0;
                    notificationManager.Notify(notificationId, notification);
                    Log.Info("TAG-ASYNCTASK", "Response Failed");
                    Log.Info("TAG-ASYNCTASK", ex.Message);
                    status = false;
                }

            }
            Int16 days = Convert.ToInt16(retentionPeriod);
            DateTime dateTime = DateTime.Now.AddDays(-days);
            var deleteScans = databaseConnection.Query<ScanSKUDataBase.ParcelScans>("DELETE FROM ParcelScans WHERE Sent <= ?", dateTime.ToString("yyyy-MM-ddTHH:mm:ss"));

            Log.Info("TAG-ASYNCTASK", "Work complete");

            return status;


        }
    }
}
