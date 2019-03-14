using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using SQLite;
using static Batch_Barcode_Scanner.ScanSKUDataBase;

namespace Batch_Barcode_Scanner
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = false)]
    public class SqliteActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestedOrientation = ScreenOrientation.Portrait;
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_sqlite);
            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            string dbPath = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                    "localscandata.db3");
            var db = new SQLiteConnection(dbPath);
            TableQuery<ParcelScans> parcelScans = db.Table<ParcelScans>();
            TextView Tv = FindViewById<TextView>(Resource.Id.text_view);

            Tv.Text = "";
            Tv.Append("======= Parcel Scans ==========");
            Tv.Append(System.Environment.NewLine);
            try
            {
                foreach (var parcelScan in parcelScans)
                {
                    Tv.Append(parcelScan.ToString());
                    Tv.Append(System.Environment.NewLine);
                }
            }catch{}

            TableQuery<TrackingNumberPatterns> patterns = db.Table<TrackingNumberPatterns>();
            
            Tv.Append("======= TrackingNumberPatterns ==========");
            Tv.Append(System.Environment.NewLine);
            try
            {
                foreach (var pattern in patterns)
                {
                    Tv.Append(pattern.ToString());
                    Tv.Append(System.Environment.NewLine);
                }
            }
            catch { }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_sql, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_main:
                    StartActivity(typeof(MainActivity));
                    break;
                case Resource.Id.menu_sqldatadelete:
                    string dbPath = System.IO.Path.Combine(
                   System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                   "localscandata.db3");
                    var db = new SQLiteConnection(dbPath);
                    db.DeleteAll<ScanSKUDataBase.ParcelScans>();

                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

    }
}
