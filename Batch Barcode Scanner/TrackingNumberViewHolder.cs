using Android.Views;
using Android.Widget;

namespace Batch_Barcode_Scanner
{
    public class TrackingNumberViewHolder : Android.Support.V7.Widget.RecyclerView.ViewHolder
    {
     //   public ImageView Image { get; private set; }
        public TextView Caption { get; private set; }
        public TrackingNumberViewHolder(View itemView) : base(itemView)
        {
            // Locate and cache view references:
            Caption = itemView.FindViewById<TextView>(Resource.Id.textView);
        }
    }
}