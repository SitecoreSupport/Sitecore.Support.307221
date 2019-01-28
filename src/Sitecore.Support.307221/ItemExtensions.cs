namespace Sitecore.Support.Marketing.Client.Shims
{
  using Analytics.Data;
  using Data.Fields;
  using Data.Items;
  using Diagnostics;

  internal static class ItemExtensions
    {
      public static TrackingField GetTrackingField(this Item item)
      {
        Assert.ArgumentNotNull(item, "item");
        Field field = item.Fields["__Tracking"];
        if (field == null)
        {
          return null;
        }
        return new TrackingField(field);
      }
    }
}