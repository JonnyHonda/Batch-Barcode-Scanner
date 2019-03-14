using SQLite;

namespace Batch_Barcode_Scanner
{
    /// <summary>
    /// ScanSku Database
    /// </summary>
    public class ScanSKUDataBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:JonnyHonda.ScanSKUDataBase"/> class.
        /// </summary>
        public ScanSKUDataBase()
        {
        }

        /// <summary>
        /// Parcel.
        /// </summary>
        public class ParcelScans
        {
            [PrimaryKey]
            [AutoIncrement]
            public int ID { get; set; } // an auto increment data base ID

            [Indexed(Unique = true)]
            public string TrackingNumber { get; set; } // the TrackingNumber

            public string ScanTime { get; set; }

            // ? allow  nulls
            public double? Longitude { get; set; }
            public double? Latitude { get; set; }

            public string Batch { get; set; }
            public string Sent { get; set; }

            public bool IsCollected { get; set; } = false;


            public override string ToString()
            {
                return string.Format("[Scan: ID={0}, Tracking Number={1}, Scan Time={2}, Longtitude={3}, Latitude={4}, Batch={5}, Sent={6}], isCollected={7}", ID, TrackingNumber, ScanTime, Longitude, Latitude, Batch, Sent, IsCollected);
            }

            public string ToCSV()
            {
                return string.Format("{0},{1},{2},{3},{4},{5},{6},{7}", ID, TrackingNumber, ScanTime, Longitude, Latitude, Batch, Sent, IsCollected);
            }
        }

        /// <summary>
        /// TrackingNumberPatterns
        /// </summary>
        public class TrackingNumberPatterns
        {
            [PrimaryKey]
            [AutoIncrement]
            public int ID { get; set; } // an auto increment data base ID

            public string Courier { get; set; }
            public string Pattern { get; set; }
            public bool IsEnabled { get; set; }

            public override string ToString()
            {
                return string.Format("[Pattern: ID={0}, Courier={1}, Pattern={2}, isEnabled={3}]", ID, Courier, Pattern, IsEnabled);
            }
        }
    }
}



