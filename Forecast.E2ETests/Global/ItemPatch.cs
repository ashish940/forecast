using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forecast.E2ETests.Global
{
    class ItemPatch
    {
        
        String itemID;
        String patch;

        
        struct ForecastTable
        {
            //Data from querying the table
            String itemID, patch, vendorDesc, gmsVenID, mm, itemDesc, itemConcat, parentID, parentDesc, assrtID, assrtDesc, prodGrpID, prodGrpConcat,
                salesUnits_FC, units_fc_vendor, lowUnits, retail_LY, retail_TY, retail_FC, cost_LY, cost_TY, cost_FC, vendor_Comments, mm_Comments,
                salesDollars_FC_Vendor;
            //Info about table
            String VendorDesc;

        }
        //fill with item/patch query results
        ForecastTable Vendor1;
        ForecastTable Vendor2;
        ForecastTable Vendor3;
        ForecastTable Allvendors;
        ForecastTable MattJames;
        String itemPatch;
        // pulled from forecast build_price table
        String retail_LY_Original;
        String retail_TY_FC_Original;
        String cost_LY_Original;
        String cost_TY_FC_Original;

        public ItemPatch(String itemID, String patch)
        {
            this.itemID = itemID;
            this.patch = patch;

        }

        public void FillItemPatchData(Boolean isFrozen)
        {
            //fill ItemPatch object with results from ItemPatch check query
            //isFrozen use frozen query, !isFrozen use unfrozen query
        }

        public Boolean CheckItemPatchData(Boolean isFrozen, String currentOwner )
        {


            return false;
        }



    }
}
