using Android.Support.V7.Widget;
using Android.Views;

namespace Batch_Barcode_Scanner
{
    public class RegExDataAdapter : RecyclerView.Adapter
    {
        public RegExList mBarcodeScannerList;
        public RegExDataAdapter(RegExList bcl)
        {
            mBarcodeScannerList = bcl;
        }

        public override RecyclerView.ViewHolder
            OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).
                        Inflate(Resource.Layout.settings_recycler_view_regex_item
                        , parent, false);
            RegExViewHolder vh = new RegExViewHolder(itemView);
            return vh;
        }

        public override void
            OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            RegExViewHolder vh = holder as RegExViewHolder;
            try
            {
                vh.Courier.Text = mBarcodeScannerList[position].GetCourierText;
                vh.Regex.Text = mBarcodeScannerList[position].GetRegexString;
                
            }
            catch { vh.Courier.Text = "Missing Tracking Number"; }
        }

        public override int ItemCount
        {
            get { return mBarcodeScannerList.NumPatterns; }
        }
    }
}