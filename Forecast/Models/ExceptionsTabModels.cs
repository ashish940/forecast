
using System;
using System.Collections.Generic;

namespace Forecast.Models
{
    public class OverlappingIPOTable
    {
        public int GMSVenID { get; set; }
        public string VendorDesc { get; set; }
        public bool Owner { get; set; }
        public long ItemID { get; set; }
        public string ItemDesc { get; set; }
        public string Patch { get; set; }
        public string MM { get; set; }
        public string MD { get; set; }
        public string TimeStamp { get; set; }
    }

    public class OverlappingIPOTableUI : OverlappingIPOTable
    {
        public string RequestingOwners { get; set; }
        public IDictionary<string, DateTime> RequestingOwnersByDate { get; set; }
    }

    public class OverlappingIPOTableExport
    {
        public string VendorDesc { get; set; }
        public string RequestingOwners { get; set; }
        public long ItemID { get; set; }
        public string ItemDesc { get; set; }
        public string Patch { get; set; }
        public string MM { get; set; }
        public string MD { get; set; }
    }

    public class ItemPatchOverlap
    {
        public ItemPatch[] ItemPatches { get; set; }
    }

    public class ItemPatch
    {
        public long ItemID { get; set; }
        public string ItemDesc { get; set; }
        public string Patch { get; set; }
    }
}