select coalesce(vendor1.itempatch, vendor2.itempatch, vendor3.itempatch, allvendors.itempatch, mattjames.itempatch) itempatch
                ,vendor1.itemid vendor1_itemid, vendor1.patch vendor1_patch, vendor2.itemid vendor2_itemid, vendor2.patch vendor2_patch, vendor3.itemid vendor3_itemid, vendor3.patch vendor3_patch,allvendors.itemid allvendors_itemid, allvendors.patch allvendors_patch, mattjames.itemid mattjames_itemid, mattjames.patch mattjames_patch
                ,vendor1.vendordesc vendor1_vendordesc, vendor2.vendordesc vendor2_vendordesc, vendor3.vendordesc vendor3_vendordesc, allvendors.vendordesc allvendors_vendordesc, mattjames.vendordesc mattjames_vendordesc
                ,vendor1.gmsvenid vendor1_gmsvenid, vendor2.gmsvenid vendor2_gmsvenid,vendor3.gmsvenid vendor3_gmsvenid, allvendors.gmsvenid allvendors_gmavenid, mattjames.gmsvenid mattjames_gmsvenid
                ,vendor1.itemdesc vendor1_itemdesc, vendor2.itemdesc vendor2_itemdesc,vendor3.itemdesc vendor3_itemdesc, allvendors.itemdesc allvendors_itemdesc, mattjames.itemdesc mattjames_itemdesc
                ,vendor1.itemconcat vendor1_itemconcat, vendor2.itemconcat vendor2_itemconcat, vendor3.itemconcat vendor3_itemconcat,allvendors.itemconcat allvendors_itemconcat, mattjames.itemconcat mattjames_itemconcat
                ,vendor1.parentid vendor1_parentid, vendor2.parentid vendor2_parentid,vendor3.parentid vendor3_parentid, allvendors.parentid allvendors_parentid, mattjames.parentid mattjames_parentid
                ,vendor1.parentdesc vendor1_parentdesc, vendor2.parentdesc vendor2_parentdesc,vendor3.parentdesc vendor3_parentdesc, allvendors.parentdesc allvendors_parentdesc, mattjames.parentdesc mattjames_parentdesc
                ,vendor1.assrtid vendor1_assrtid, vendor2.assrtid vendor2_assrtid,vendor3.assrtid vendor3_assrtid, allvendors.assrtid allvendors_assrtid, mattjames.assrtid mattjames_assrtid
                ,vendor1.assrtconcat vendor1_assrtconcat, vendor2.assrtconcat vendor2_assrtconcat,vendor3.assrtconcat vendor3_assrtconcat, allvendors.assrtconcat allvendors_assrtconcat, mattjames.assrtconcat mattjames_assrtconcat
                ,vendor1.prodgrpid vendor1_prodgrpid, vendor2.prodgrpid vendor2_prodgrpid,vendor3.prodgrpid vendor3_prodgrpid, allvendors.prodgrpid allvendors_prodgrpid, mattjames.prodgrpid mattjames_prodgrpid
                ,vendor1.prodgrpconcat vendor1_prodgrpconcat, vendor2.prodgrpconcat vendor2_prodgrpconcat, vendor3.prodgrpconcat vendor3_prodgrpconcat,allvendors.prodgrpconcat allvendors_prodgrpconcat, mattjames.prodgrpconcat mattjames_prodgrpconcat
                ,vendor1.salesunits_fc vendor1_salesunits_fc, vendor2.salesunits_fc vendor2_salesunits_fc,vendor3.salesunits_fc vendor3_salesunits_fc, allvendors.salesunits_fc allvendors_salesunits_fc, mattjames.salesunits_fc mattjames_salesunits_fc
                ,vendor1.units_fc_vendor vendor1_units_fc_vendor,  vendor2.units_fc_vendor vendor2_units_fc_vendor, vendor3.units_fc_vendor vendor3_units_fc_vendor, allvendors.units_fc_vendor allvendors_units_fc_vendor, mattjames.units_fc_vendor mattjames_units_fc_vendor
                ,vendor1.lowunits vendor1_lowunits, vendor2.lowunits vendor2_lowunits,vendor3.lowunits vendor3_lowunits, allvendors.lowunits allvendors_lowunits, mattjames.lowunits mattjames_lowunits
                ,vendor1.retail_ly vendor1_retail_ly, vendor2.retail_ly vendor2_retail_ly, vendor3.retail_ly vendor3_retail_ly, allvendors.retail_ly allvendors_retail_ly, mattjames.retail_ly mattjames_retail_ly
                ,prices.retail_ly retail_ly_original
                ,vendor1.retail_ty vendor1_retail_ty, vendor2.retail_ty vendor2_retail_ty,vendor3.retail_ty vendor3_retail_ty, allvendors.retail_ty allvendors_retail_ty, mattjames.retail_ty mattjames_retail_ty
                ,vendor1.retail_fc vendor1_retail_fc, vendor2.retail_fc vendor2_retail_fc,vendor3.retail_fc vendor3_retail_fc, allvendors.retail_fc allvendors_retail_fc, mattjames.retail_fc mattjames_retail_fc
                ,prices.retailprice_fc retail_ty_fc_original
                ,vendor1.cost_ly vendor1_cost_ly, vendor2.cost_ly vendor2_cost_ly,vendor3.cost_ly vendor3_cost_ly, allvendors.cost_ly allvendors_cost_ly, mattjames.cost_ly mattjames_cost_ly
                ,prices.cost_ly cost_ly_original
                ,vendor1.cost_ty vendor1_cost_ty, vendor2.cost_ty vendor2_cost_ty, vendor3.cost_ty vendor3_cost_ty,allvendors.cost_ty allvendors_cost_ty, mattjames.cost_ty mattjames_cost_ty
                ,vendor1.cost_fc vendor1_cost_fc, vendor2.cost_fc vendor2_cost_fc,vendor3.cost_fc vendor3_cost_fc, allvendors.cost_fc allvendors_cost_fc, mattjames.cost_fc mattjames_cost_fc
                ,prices.cost_fc cost_ty_fc_original
                ,vendor1.vendor_comments vendor1_vendor_comments, vendor2.vendor_comments vendor2_vendor_comments, vendor3.vendor_comments vendor3_vendor_comments,allvendors.vendor_comments allvendors_vendor_comments, mattjames.vendor_comments mattjames_vendor_comments
                ,vendor1.mm_comments vendor1_mm_comments, vendor2.mm_comments vendor2_mm_comments,vendor3.mm_comments vendor3_mm_comments, allvendors.mm_comments allvendors_mm_comments, mattjames.mm_comments mattjames_mm_comments
                
                from
                (
                select concat(itemid, patch) itempatch, vendordesc,gmsvenid,  itemid, patch,itemdesc,itemconcat,parentid, parentdesc, assrtid, assrtconcat, prodgrpid, prodgrpdesc,prodgrpconcat ,sum(salesunits_fc) salesunits_fc, sum(units_fc_vendor) units_fc_vendor, sum(units_fc_low) LowUnits,avg(retailprice_ly) retail_ly, avg(retailprice_ty) retail_ty, avg(retailprice_fc) retail_fc, avg(cost_ly) cost_ly, avg(cost_ty) cost_ty, avg(cost_fc) cost_fc, max(vendor_comments)  vendor_comments, max(mm_comments) mm_comments 
                from forecast_dev.tbl_allvendors ${whereClase}$
                group by itemid, itemdesc, patch,parentid, assrtid, prodgrpid,vendordesc, gmsvenid, prodgrpdesc, itemconcat, assrtconcat, prodgrpconcat, parentdesc
                ) allvendors
                full outer join

                (select concat(itemid, patch) itempatch,vendordesc,gmsvenid, itemid,patch, itemdesc,itemconcat,parentid, parentdesc,assrtid, assrtconcat, prodgrpid, prodgrpdesc,prodgrpconcat,   sum(salesunits_fc) salesunits_fc,sum(units_fc_vendor) units_fc_vendor, sum(units_fc_low) LowUnits, avg(retailprice_ly) retail_ly, avg(retailprice_ty) retail_ty, avg(retailprice_fc) retail_fc, avg(cost_ly) cost_ly, avg(cost_ty) cost_ty,avg(cost_fc) cost_fc, max(vendor_comments)  vendor_comments, max(mm_comments) mm_comments 
                from forecast_dev.tbl_${vendor2}$ ${whereClase}$
                group by itemid, itemdesc, patch, assrtid,parentid, prodgrpid, vendordesc, gmsvenid, prodgrpdesc, itemconcat, assrtconcat, prodgrpconcat, parentdesc
                ) vendor2
                on allvendors.itemid = vendor2.itemid and allvendors.patch = vendor2.patch 
                full outer join
                (select concat(itemid, patch) itempatch,vendordesc,gmsvenid,itemid,patch, itemdesc,itemconcat,parentid, parentdesc,assrtid, assrtconcat, prodgrpid, prodgrpdesc,prodgrpconcat,   sum(salesunits_fc) salesunits_fc,sum(units_fc_vendor) units_fc_vendor, sum(units_fc_low) LowUnits, avg(retailprice_ly) retail_ly, avg(retailprice_ty) retail_ty, avg(retailprice_fc) retail_fc, avg(cost_ly) cost_ly, avg(cost_ty) cost_ty,avg(cost_fc) cost_fc, max(vendor_comments)  vendor_comments, max(mm_comments) mm_comments 
                from forecast_dev.tbl_${vendor3}$ ${whereClase}$
                group by itemid, itemdesc, patch, assrtid,parentid, prodgrpid,vendordesc, gmsvenid,prodgrpdesc, itemconcat, assrtconcat, prodgrpconcat, parentdesc
                ) vendor3
                on allvendors.itemid = vendor3.itemid and allvendors.patch = vendor3.patch 
                full outer join
                (
                select concat(itemid, patch) itempatch,'allvendors' datasource,vendordesc,gmsvenid, itemid, patch,itemdesc,itemconcat,parentid, parentdesc,assrtid, assrtconcat, prodgrpid, prodgrpdesc,prodgrpconcat,  sum(salesunits_fc) salesunits_fc,sum(units_fc_vendor) units_fc_vendor,sum(units_fc_low) LowUnits,avg(retailprice_ly) retail_ly, avg(retailprice_ty) retail_ty, avg(retailprice_fc) retail_fc, avg(cost_ly) cost_ly, avg(cost_ty) cost_ty,avg(cost_fc) cost_fc,max(vendor_comments)  vendor_comments, max(mm_comments) mm_comments
                from forecast_dev.tbl_${vendor1}$ ${whereClase}$
                group by itemid, itemdesc, patch, assrtid, prodgrpid,md, vendordesc, gmsvenid, prodgrpdesc,parentid, itemconcat, assrtconcat, prodgrpconcat, parentdesc
                ) vendor1
                on allvendors.itemid = vendor1.itemid and allvendors.patch = vendor1.patch
                full outer join
                (
                select concat(itemid, patch) itempatch,'mattjames' datasource,vendordesc,gmsvenid, itemid,patch, itemdesc,itemconcat,parentid, parentdesc,assrtid, assrtconcat, prodgrpid, prodgrpdesc,prodgrpconcat , 
                sum(salesunits_fc) salesunits_fc,sum(units_fc_vendor) units_fc_vendor,sum(units_fc_low) LowUnits,avg(retailprice_ly) retail_ly, avg(retailprice_ty) retail_ty, avg(retailprice_fc) retail_fc, avg(cost_ly) cost_ly, avg(cost_ty) cost_ty, avg(cost_fc) cost_fc, max(vendor_comments)  vendor_comments, max(mm_comments) mm_comments
                from forecast_dev.tbl_mattjames  ${whereClase}$
                group by itemid, itemdesc, patch, assrtid, prodgrpid,md, vendordesc, gmsvenid, prodgrpdesc,parentid, itemconcat, assrtconcat, prodgrpconcat, parentdesc
                ) mattjames
                on allvendors.itemid = mattjames.itemid and allvendors.patch = mattjames.patch

                left join
                forecast_dev.build_prices prices
                on allvendors.itemid = prices.itemid and allvendors.patch = prices.patch

                order by allvendors.itemid
                
                -- whereClause = where itemid in (1234567,1234568,1234569,1234570,1234571,1234572,1234573,1234574,1234575,1234576,1234577,1234578,1234579,1234580,1234581,1234582)