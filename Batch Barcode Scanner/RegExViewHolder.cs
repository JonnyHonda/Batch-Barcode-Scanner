using Android.Views;
using Android.Widget;

namespace Batch_Barcode_Scanner
{
    public class RegExViewHolder : Android.Support.V7.Widget.RecyclerView.ViewHolder
    {
        public TextView Courier { get; private set; }
        public TextView Regex { get; private set; }

        public RegExViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            Courier = itemView.FindViewById<TextView>(Resource.Id.courierTextView);
            Regex = itemView.FindViewById<TextView>(Resource.Id.regexTextView);
        }
    }
}