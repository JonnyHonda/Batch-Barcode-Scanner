using SQLite;
using System.Collections.Generic;

namespace Batch_Barcode_Scanner
{
    // BarcodeScan: contains image resource ID and caption:
    public class RegExPattern

    {
        // GetBarcodeText text for this Barcode:
        public string Courier;
        public string RegexString;

        /// <summary>
        /// 
        /// </summary>
        public string GetCourierText
        {
            get { return Courier; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string GetRegexString
        {
            get { return RegexString; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class RegExList
    {
        static readonly string dbPath = System.IO.Path.Combine(
                        System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                        "localscandata.db3");
        SQLiteConnection db = new SQLiteConnection(dbPath);

        List<RegExPattern> CurrentScans = new List<RegExPattern>();
        // Array of barcodes that make up the RecycleView
        private RegExPattern[] patterns;


        public RegExList()
        {
            this.FetchPatternList();

        }

        public void FetchPatternList()
        {
            var scans = db.Query<ScanSKUDataBase.TrackingNumberPatterns>("SELECT * FROM TrackingNumberPatterns");
            CurrentScans.Clear();
            foreach (var scan in scans)
            {
                RegExPattern t = new RegExPattern {
                    Courier = scan.Courier,
                    RegexString = scan.Pattern
               
                };
                CurrentScans.Add(t);
            }
            patterns = CurrentScans.ToArray();
        }
        // Return the number of barcodes in the view 
        public int NumPatterns
        {
            get { return patterns.Length; }
        }
        
        // Indexer (read only) for accessing a barcode:
        public RegExPattern this[int i]
        {
            get { return patterns[i]; }
        }

    }
}
 