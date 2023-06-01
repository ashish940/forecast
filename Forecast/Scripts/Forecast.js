///-----------------------------------------------------------------
///   File Name:      Forecast.js
///
///   Description:    Handles a variety of UI related functions.
///
///   Contributors:   Anthony Castillo
///
///   Date:           11/20/2017
///
///   Contributor(s): Joe McCarty
///
///   Revision History: https://bitbucket.org/demandlinkdevelopment/forecast
///-----------------------------------------------------------------

// var DEBUG = true;

/*=============================================>>>>>
                 = GLOBALS =
===============================================>>>>>*/

var colReorder = [];
var columns = {};
var editor; // The DT editor object
var editMode = 'main';
var fixedColumns; // Fixed columns constructor
var iFixedColumns = 21; // Number of columns to fix
var lastEditedCell; // Keeps the last edited cell DOM
var offset = 0;
var order;
var orders = ['column', 'dir'];
var pageLength = 0;
var pageNumber;
var start = 0;
var storage = {};
var rowid = [];
var state = {};
var timestamp = new Date().getTime();
var loadFromBookmark = false;
var bookmarkName;
var params = {};
var isRetailVarEditable = false;
var isMerchandisingManager = false;
var isMerchandisingDirector = false;
var isMMComment = false;
var isVendorComment = false;
var editVendorComment = true;
var isFirstTimeLoading = true; // true == The site is loading
var isLastDraw = true; // Used to execute or skip functions triggered by a table draw.
var isNewUser = false; // First time this user has logged in OR absence of local storage
var rotator = []; // Rotator array
var api; // Object for datatable API calls

// Variables to set group visible state.
var salesDollarsGroup = false;
var turnsGroup = false;
var salesUnitsGroup = false;
var retailPriceGroup = false;
let mpSalesAndMarginGroup = false;
var priceSensGroup = false;
var aspGroup = false;
var marginPercGroup = false;
var marginDollGroup = false;
var sellThruGroup = false;
var recDollGroup = false;
var recUnitGroup = false;
var forecastGroup = false;
var costGroup = false;
var itemDescGroup = false;
var assrtDescGroup = false;
var prodgrpDescGroup = false;
var commentGroup = false;
var exclude = false;

// Record count populated from Forecast table ajax call.
var recordsFiltered;
var recordsTotal;
/*=============================================>>>>>
              = Filter Stuff =
===============================================>>>>>*/
//Geo
var filter_vendor = [];
var filter_md = [];
var filter_mm = [];
var filter_region = [];
var filter_district = [];
var filter_patch = [];
//Product
var filter_prodgrp = [];
var filter_assrt = [];
var filter_item = [];
var filter_parent = [];
//Time
var filter_fiscalwk = [];
var filter_fiscalqtr = [];
var filter_fiscalmo = [];

//Exclude Geo
var exclude_vendor = [];
var exclude_md = [];
var exclude_mm = [];
var exclude_region = [];
var exclude_district = [];
var exclude_patch = [];
//Exclude Product
var exclude_prodgrp = [];
var exclude_assrt = [];
var exclude_item = [];
var exclude_parent = [];
//Exclude Time
var exclude_fiscalwk = [];
var exclude_fiscalqtr = [];
var exclude_fiscalmo = [];

//all
var all_item = [];

/**
 * All filtes selected when holding down Ctrl button
 * */
var filters_selected = [];

//excluded filters
var filters_excluded = [];

//Time button/key events for fiscal select boxes
var fiscal_select_ctrl_event = false;
var fiscal_select_mouse_click = false;

//Multi-Sort Stuff
var column_sort_ctrl_event = false;
var filter_columnsort = [];

//Filter key event tracker
var filter_ctrl_key = false;

// Patterns utilized by the bookmark updating process
const noRotatorPattern = /((,?\})"?)(\s)*\,*$/gm;
const stateRotatorPattern = /"rotator":(\[((\{("[0-9a-zA-Z]+":"?([0-9a-zA-Z]*_*[0-9a-zA-Z]*)"?,?)*\}),?)*\])/gm;
const columnObjPattern =
  /\[({("[a-zA-Z]*":("?[0-9a-zA-Z]*"?)\,?)*\{(\,?"[a-zA-Z]*":("?[0-9a-zA-Z\!*\s*\.*\,*\-*\'*\&*\$*\#*\@*\(*\)*\/*\?*\%*\_*\^*\"*\:*\;*\<*\>*\+*]*"?)\,?)*\},(("data":([0-9]*),)*|("name":"[0-9a-zA-Z]*(_*[0-9a-zA-Z])*"\,)*"[a-zA-Z]*":("?[0-9a-zA-Z]*"?)\,*)*\}\,*)*\]/gm;
const colReorderPattern = /\"[cC]ol[rR]eorder":(\[(\s*\"?[0-9a-zA-Z]*_*-*\"?,?)*\])/gm;

// Notification associated stuff
var notificationsManager;
var dlEventsManager;
var tutorialsManager;
var adminModalManager;

// Forecast debugger re-initialization for production.
var ForecastDebugger = typeof ForecastDebugger !== 'undefined' ? ForecastDebugger : null;

// Filter Logic
//Vendor
$('#filter-vendor').select2({
  width: '98%',
  placeholder: 'Select Vendor',
  ajax: {
    url: '/Home/GetFilterData',
    dataType: 'json',
    method: 'POST',
    data: function (param) {
      return {
        TableName: document.getElementById('TableName').value,
        Type: 'VendorDesc',
        Search: param.term, //,
        //columns: params.columns
      };
    },
    complete: function () {
      var targetId = 'filter_vendor';
      var targetId2 = 'exclude_vendor';
      CheckSelectedFilters(targetId);
      CheckSelectedFilters(targetId2);
    },
    delay: 500,
    placeholder: 'VendorDesc',
    allowClear: true,
    multiple: 'multiple',
    tags: true,
  },
});

$('#filter-vendor').on('select2:select', function (e) {
  var param = e.params.data.text;
  if (exclude == false) {
    if (filter_vendor.indexOf(param) == -1) {
      //check if in exclude list and remove if it is
      if (exclude_vendor.indexOf('!' + param) != -1) {
        var index = exclude_vendor.indexOf('!' + param);
        exclude_vendor.splice(index, 1);
      }
      //add to include list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      if (DEBUG) {
        console.log('ADD TO INCLUDE LIST');
      }
      AddFilterToList(filterId, param, e);
    }
  } else {
    if (exclude_vendor.indexOf('!' + param) == -1) {
      //check if in include list and remove if it is
      if (filter_vendor.indexOf(param) != -1) {
        var index = filter_vendor.indexOf(param);
        filter_vendor.splice(index, 1);
      }
      //add to exclude list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      param = '!' + param;
      if (DEBUG) {
        console.log('ADD TO EXCLUDE LIST' + param);
      }
      AddFilterToExcludeList(filterId, param, e);
    }
  }
  $('#filter-vendor').val(null).trigger('change');
});

//MD
$('#filter-md').select2({
  width: '98%',
  placeholder: 'Select SRLGM',
  ajax: {
    url: '/Home/GetFilterData',
    dataType: 'json',
    method: 'POST',
    data: function (param) {
      return {
        TableName: document.getElementById('TableName').value,
        Type: 'MD',
        Search: param.term,
      };
    },
    complete: function () {
      var targetId = 'filter_md';
      var targetId2 = 'exclude_md';
      CheckSelectedFilters(targetId);
      CheckSelectedFilters(targetId2);
    },
    delay: 500,
    placeholder: 'SRLGM',
    allowClear: true,
    multiple: 'multiple',
    tags: true,
  },
});

$('#filter-md').on('select2:select', function (e) {
  var param = e.params.data.text;
  if (exclude == false) {
    if (filter_md.indexOf(param) == -1) {
      //check if in exclude list and remove if it is
      if (exclude_md.indexOf('!' + param) != -1) {
        var index = exclude_md.indexOf('!' + param);
        exclude_md.splice(index, 1);
      }
      //add to include list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      if (DEBUG) {
        console.log('ADD TO INCLUDE LIST');
      }
      AddFilterToList(filterId, param, e);
    }
  } else {
    if (exclude_md.indexOf('!' + param) == -1) {
      //check if in include list and remove if it is
      if (filter_md.indexOf(param) != -1) {
        var index = filter_md.indexOf(param);
        filter_md.splice(index, 1);
      }
      //add to exclude list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      param = '!' + param;
      if (DEBUG) {
        console.log('ADD TO EXCLUDE LIST' + param);
      }
      AddFilterToExcludeList(filterId, param, e);
    }
  }
  $('#filter-md').val(null).trigger('change');
});

//MM
$('#filter-mm').select2({
  width: '98%',
  placeholder: 'Select LGM',
  ajax: {
    url: '/Home/GetFilterData',
    dataType: 'json',
    method: 'POST',
    data: function (param) {
      return {
        TableName: document.getElementById('TableName').value,
        Type: 'MM',
        Search: param.term,
      };
    },
    complete: function () {
      var targetId = 'filter_mm';
      var targetId2 = 'exclude_mm';
      CheckSelectedFilters(targetId);
      CheckSelectedFilters(targetId2);
    },
    delay: 500,
    placeholder: 'LGM',
    allowClear: true,
    multiple: 'multiple',
    tags: true,
  },
});

$('#filter-mm').on('select2:select', function (e) {
  var param = e.params.data.text;
  if (exclude == false) {
    if (filter_mm.indexOf(param) == -1) {
      //check if in exclude list and remove if it is
      if (exclude_mm.indexOf('!' + param) != -1) {
        var index = exclude_mm.indexOf('!' + param);
        exclude_mm.splice(index, 1);
      }
      //add to include list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      if (DEBUG) {
        console.log('ADD TO INCLUDE LIST');
      }
      AddFilterToList(filterId, param, e);
    }
  } else {
    if (exclude_mm.indexOf('!' + param) == -1) {
      //check if in include list and remove if it is
      if (filter_mm.indexOf(param) != -1) {
        var index = filter_mm.indexOf(param);
        filter_mm.splice(index, 1);
      }
      //add to exclude list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      param = '!' + param;
      if (DEBUG) {
        console.log('ADD TO EXCLUDE LIST' + param);
      }
      AddFilterToExcludeList(filterId, param, e);
    }
  }
  $('#filter-mm').val(null).trigger('change');
});

//Region
$('#filter-region').select2({
  width: '98%',
  placeholder: 'Select Region',
  ajax: {
    url: '/Home/GetFilterData',
    dataType: 'json',
    method: 'POST',
    data: function (param) {
      return {
        TableName: document.getElementById('TableName').value,
        Type: 'Region',
        Search: param.term,
      };
    },
    complete: function () {
      var targetId = 'filter_region';
      var targetId2 = 'exclude_region';
      CheckSelectedFilters(targetId);
      CheckSelectedFilters(targetId2);
    },
    delay: 500,
    placeholder: 'Region',
    allowClear: true,
    multiple: 'multiple',
    tags: true,
  },
});

$('#filter-region').on('select2:select', function (e) {
  var param = e.params.data.text;
  if (exclude == false) {
    if (filter_region.indexOf(param) == -1) {
      //check if in exclude list and remove if it is
      if (exclude_region.indexOf('!' + param) != -1) {
        var index = exclude_region.indexOf('!' + param);
        exclude_region.splice(index, 1);
      }
      //add to include list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      if (DEBUG) {
        console.log('ADD TO INCLUDE LIST');
      }
      AddFilterToList(filterId, param, e);
    }
  } else {
    if (exclude_region.indexOf('!' + param) == -1) {
      //check if in include list and remove if it is
      if (filter_region.indexOf(param) != -1) {
        var index = filter_region.indexOf(param);
        filter_region.splice(index, 1);
      }
      //add to exclude list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      param = '!' + param;
      if (DEBUG) {
        console.log('ADD TO EXCLUDE LIST' + param);
      }
      AddFilterToExcludeList(filterId, param, e);
    }
  }
  $('#filter-region').val(null).trigger('change');
});

//District
$('#filter-district').select2({
  width: '98%',
  placeholder: 'Select District',
  ajax: {
    url: '/Home/GetFilterData',
    dataType: 'json',
    method: 'POST',
    data: function (param) {
      return {
        TableName: document.getElementById('TableName').value,
        Type: 'District',
        Search: param.term,
      };
    },
    complete: function () {
      var targetId = 'filter_district';
      var targetId2 = 'exclude_district';
      CheckSelectedFilters(targetId);
      CheckSelectedFilters(targetId2);
    },
    delay: 500,
    placeholder: 'District',
    allowClear: true,
    multiple: 'multiple',
    tags: true,
  },
});

$('#filter-district').on('select2:select', function (e) {
  var param = e.params.data.text;
  if (exclude == false) {
    if (filter_district.indexOf(param) == -1) {
      //check if in exclude list and remove if it is
      if (exclude_district.indexOf('!' + param) != -1) {
        var index = exclude_district.indexOf('!' + param);
        exclude_district.splice(index, 1);
      }
      //add to include list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      if (DEBUG) {
        console.log('ADD TO INCLUDE LIST');
      }
      AddFilterToList(filterId, param, e);
    }
  } else {
    if (exclude_district.indexOf('!' + param) == -1) {
      //check if in include list and remove if it is
      if (filter_district.indexOf(param) != -1) {
        var index = filter_district.indexOf(param);
        filter_district.splice(index, 1);
      }
      //add to exclude list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      param = '!' + param;
      if (DEBUG) {
        console.log('ADD TO EXCLUDE LIST' + param);
      }
      AddFilterToExcludeList(filterId, param, e);
    }
  }
  $('#filter-district').val(null).trigger('change');
});

//Patch
$('#filter-patch').select2({
  width: '98%',
  placeholder: 'Select Patch',
  ajax: {
    url: '/Home/GetFilterData',
    dataType: 'json',
    method: 'POST',
    data: function (param) {
      return {
        TableName: document.getElementById('TableName').value,
        Type: 'Patch',
        Search: param.term,
      };
    },
    complete: function () {
      var targetId = 'filter_patch';
      var targetId2 = 'exclude_patch';
      CheckSelectedFilters(targetId);
      CheckSelectedFilters(targetId2);
    },
    delay: 500,
    placeholder: 'Patch',
    allowClear: true,
    multiple: 'multiple',
    tags: true,
  },
});

$('#filter-patch').on('select2:select', function (e) {
  var param = e.params.data.text;
  if (exclude == false) {
    if (filter_patch.indexOf(param) == -1) {
      //check if in exclude list and remove if it is
      if (exclude_patch.indexOf('!' + param) != -1) {
        var index = exclude_patch.indexOf('!' + param);
        exclude_patch.splice(index, 1);
      }
      //add to include list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      if (DEBUG) {
        console.log('ADD TO INCLUDE LIST');
      }
      AddFilterToList(filterId, param, e);
    }
  } else {
    if (exclude_patch.indexOf('!' + param) == -1) {
      //check if in include list and remove if it is
      if (filter_patch.indexOf(param) != -1) {
        var index = filter_patch.indexOf(param);
        filter_patch.splice(index, 1);
      }
      //add to exclude list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      param = '!' + param;
      if (DEBUG) {
        console.log('ADD TO EXCLUDE LIST' + param);
      }
      AddFilterToExcludeList(filterId, param, e);
    }
  }
  $('#filter-patch').val(null).trigger('change');
});

//parent
$('#filter-parent').select2({
  width: '98%',
  placeholder: 'Select Parent',
  ajax: {
    url: '/Home/GetFilterData',
    dataType: 'json',
    method: 'POST',
    data: function (param) {
      return {
        TableName: document.getElementById('TableName').value,
        Type: 'ParentConcat',
        Search: param.term,
      };
    },
    complete: function () {
      var targetId = 'filter_parent';
      var targetId2 = 'exclude_parent';
      CheckSelectedFilters(targetId);
      CheckSelectedFilters(targetId2);
    },
    delay: 500,
    placeholder: 'ParentConcat',
    allowClear: true,
    multiple: 'multiple',
    tags: true,
  },
});

$('#filter-parent').on('select2:select', function (e) {
  var param = e.params.data.text;
  if (exclude == false) {
    if (filter_parent.indexOf(param) == -1) {
      //check if in exclude list and remove if it is
      if (exclude_parent.indexOf('!' + param) != -1) {
        var index = exclude_parent.indexOf('!' + param);
        exclude_parent.splice(index, 1);
      }
      //add to include list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      if (DEBUG) {
        console.log('ADD TO INCLUDE LIST');
      }
      AddFilterToList(filterId, param, e);
    }
  } else {
    if (exclude_parent.indexOf('!' + param) == -1) {
      //check if in include list and remove if it is
      if (filter_parent.indexOf(param) != -1) {
        var index = filter_parent.indexOf(param);
        filter_parent.splice(index, 1);
      }
      //add to exclude list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      param = '!' + param;
      if (DEBUG) {
        console.log('ADD TO EXCLUDE LIST' + param);
      }
      AddFilterToExcludeList(filterId, param, e);
    }
  }
  $('#filter-parent').val(null).trigger('change');
});

//ProdGrp
$('#filter-prodgrp').select2({
  width: '98%',
  placeholder: 'Select Product Group',
  ajax: {
    url: '/Home/GetFilterData',
    dataType: 'json',
    method: 'POST',
    data: function (param) {
      return {
        TableName: document.getElementById('TableName').value,
        Type: 'ProdGrpConcat',
        Search: param.term,
      };
    },
    complete: function () {
      var targetId = 'filter_prodgrp';
      var targetId2 = 'exclude_prodgrp';
      CheckSelectedFilters(targetId);
      CheckSelectedFilters(targetId2);
    },
    delay: 500,
    placeholder: 'ProdGrpConcat',
    allowClear: true,
    multiple: 'multiple',
    tags: true,
  },
});

$('#filter-prodgrp').on('select2:select', function (e) {
  var param = e.params.data.text;
  if (exclude == false) {
    if (filter_prodgrp.indexOf(param) == -1) {
      //check if in exclude list and remove if it is
      if (exclude_prodgrp.indexOf('!' + param) != -1) {
        var index = exclude_prodgrp.indexOf('!' + param);
        exclude_prodgrp.splice(index, 1);
      }
      //add to include list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      if (DEBUG) {
        console.log('ADD TO INCLUDE LIST');
      }
      AddFilterToList(filterId, param, e);
    }
  } else {
    if (exclude_prodgrp.indexOf('!' + param) == -1) {
      //check if in include list and remove if it is
      if (filter_prodgrp.indexOf(param) != -1) {
        var index = filter_prodgrp.indexOf(param);
        filter_prodgrp.splice(index, 1);
      }

      //add to exclude list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      param = '!' + param;
      if (DEBUG) {
        console.log('ADD TO EXCLUDE LIST' + param);
      }
      AddFilterToExcludeList(filterId, param, e);
    }
  }
  $('#filter-prodgrp').val(null).trigger('change');
});

//Assortment
$('#filter-assrt').select2({
  width: '98%',
  placeholder: 'Select Assortment',
  ajax: {
    url: '/Home/GetFilterData',
    dataType: 'json',
    method: 'POST',
    data: function (param) {
      return {
        TableName: document.getElementById('TableName').value,
        Type: 'AssrtConcat',
        Search: param.term,
      };
    },
    complete: function () {
      var targetId = 'filter_assrt';
      var targetId2 = 'exclude_assrt';
      CheckSelectedFilters(targetId);
      CheckSelectedFilters(targetId2);
    },
    delay: 500,
    placeholder: 'AssrtConcat',
    allowClear: true,
    multiple: 'multiple',
    tags: true,
  },
});

$('#filter-assrt').on('select2:select', function (e) {
  var param = e.params.data.text;
  if (exclude == false) {
    if (filter_assrt.indexOf(param) == -1) {
      //check if in exclude list and remove if it is
      if (exclude_assrt.indexOf('!' + param) != -1) {
        var index = exclude_assrt.indexOf('!' + param);
        exclude_assrt.splice(index, 1);
      }
      //add to include list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      if (DEBUG) {
        console.log('ADD TO INCLUDE LIST');
      }
      AddFilterToList(filterId, param, e);
    }
  } else {
    if (exclude_assrt.indexOf('!' + param) == -1) {
      //check if in include list and remove if it is
      if (filter_assrt.indexOf(param) != -1) {
        var index = filter_assrt.indexOf(param);
        filter_assrt.splice(index, 1);
      }
      //add to exclude list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      param = '!' + param;
      if (DEBUG) {
        console.log('ADD TO EXCLUDE LIST' + param);
      }
      AddFilterToExcludeList(filterId, param, e);
    }
  }
  $('#filter-assrt').val(null).trigger('change');
});

var changeStyle = function (selector, prop, value) {
  var style = document.styleSheets[3].cssRules || document.styleSheets[3].rules;
  console.log('StyleSheet: ' + style.toString);
  for (var i = 0; i < style.length; i++) {
    if (style[i].selectorText == selector) {
      style[i].style[prop] = value;
    }
  }
};

//Item
$('#filter-item').select2({
  width: '98%',
  placeholder: 'Select Item',
  ajax: {
    url: '/Home/GetFilterData',
    dataType: 'json',
    method: 'POST',
    data: function (param) {
      return {
        TableName: document.getElementById('TableName').value,
        Type: 'ItemConcat',
        Search: param.term,
        // columns: params.columns
      };
    },
    complete: function () {
      var targetId = 'filter_item';
      var targetId2 = 'exclude_item';
      //add check for exclude and modify checkselectedfilters
      CheckSelectedFilters(targetId);
      CheckSelectedFilters(targetId2);
    },

    delay: 500,
    placeholder: 'ItemConcat',
    allowClear: true,
    multiple: 'multiple',
    tags: true,
  },
});

$('#filter-item').on('select2:select', function (e) {
  var param = e.params.data.text;
  if (exclude == false) {
    if (filter_item.indexOf(param) == -1) {
      //check if in exclude list and remove if it is
      if (exclude_item.indexOf('!' + param) != -1) {
        var index = exclude_item.indexOf('!' + param);
        exclude_item.splice(index, 1);
      }
      //add to include list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      if (DEBUG) {
        console.log('ADD TO INCLUDE LIST');
      }
      AddFilterToList(filterId, param, e);
    }
  } else {
    if (exclude_item.indexOf('!' + param) == -1) {
      //check if in include list and remove if it is
      if (filter_item.indexOf(param) != -1) {
        var index = filter_item.indexOf(param);
        filter_item.splice(index, 1);
      }
      //add to exclude list
      var filterId = GetFilterNameFromId(e.currentTarget.id);
      param = '!' + param;
      if (DEBUG) {
        console.log('ADD TO EXCLUDE LIST' + param);
      }
      AddFilterToExcludeList(filterId, param, e);
    }
  }

  $('#filter-item').val(null).trigger('change');
});

//bulk filter
$('#filter-input-sub').click(function (e) {
  ShowProcessingLoader();
  if (DEBUG) console.log('line 879');
  //needed to decrease the time between clicking submit and processing screen showing
  setTimeout(function () {
    BulkSelect(e); //call bulkselect
  }, 100);
});

function BulkSelect(e) {
  var filterId = 'item';
  var items = document.getElementById('filter-input').value;
  var tableName = document.getElementById('TableName').value;
  $('#filter-input').val('');
  var numericID = new Array();
  var invalidID = new Array();
  var first = items.split(',');

  //find invalid ids
  for (i = 0; i < first.length; i++) {
    if ($.isNumeric(first[i])) {
      numericID.push(first[i]);
    } else {
      invalidID.push(first[i]);
    }
  }
  // Trim the excess whitespace from numeric ItemIDs.
  if (numericID.length > 0) {
    for (var i = 0; i < numericID.length; i++) {
      numericID[i] = numericID[i].replace(/^\s*/, '').replace(/\s*$/, '');
    }

    desc = ''; //initial list of returned ItemIDs and descriptions
    list = new Array(); //array returned ItemIDs and descriptions are added to
    noMatch = new Array(); //all numeric ItemIDs not matched

    $.ajax({
      type: 'POST',
      url: 'Home/GetItemDesc',
      async: false,
      data: { item: numericID, table: tableName },
      dataType: 'text',
      success: function (data) {
        var param = JSON.parse(data);
        desc = param.fullItem;

        list = desc.split(',');

        //Alert user about invalid itemIDs
        if (invalidID.length > 0) {
          alert('Invalid ItemIDs: ' + invalidID.toString());
        }
        //Check if ItemID was found
        for (i = 0; i < numericID.length; i++) {
          //if ItemID in numericID is not in desc no match was found
          if (!desc.includes(numericID[i])) {
            noMatch.push(numericID[i]);
          }
        }
        if (noMatch.length > 0) {
          //Alert user no match was found for ItemID
          alert('No item matching ItemID: ' + noMatch.toString());
        }
        // If no items returned them hide the processing loader.
        if (list[0] === '') {
          HideProcessingLoader();
        }
      },
      error: function (response) {
        alert('An error occured while applying filter');
      },
    });
    if (list[0] != '') {
      if (exclude == false) {
        //Add found ItemIDs to filter
        for (i = 0; i < list.length; i++) {
          if (exclude_item.includes('!' + list[i])) {
            RemoveItemFromArray(exclude_item, '!' + list[i]);
          }
        }

        AddBulkFilterToList(filterId, list, e);
      } else {
        for (i = 0; i < list.length; i++) {
          if (filter_item.includes(list[i])) {
            RemoveItemFromArray(filter_item, list[i]);
          }
        }
        for (i = 0; i < list.length; i++) {
          list[i] = '!' + list[i];
        }
        AddBulkExcludeFilterToList(filterId, list, e);
      }
    }
  }
  // If only invalid IDs were entered "123 123" "asdf" ""
  else if (invalidID.length > 0) {
    alert('Invalid ItemIDs: ' + invalidID.toString());
    HideProcessingLoader();
  }
}

//Populate list of bookmarks
$('#bookmarkList').select2({
  ajax: {
    url: '/Home/GetBookmarkList',
    dataType: 'JSON',
    method: 'POST',
    data: function (params) {
      return {
        gmsvenid: parseInt(document.getElementById('GMSVenID').value),
        username: document.getElementById('Username').value,
        vendorGroup: document.getElementById('VendorGroup').value,
        tableName: document.getElementById('TableName').value,
        bookmarkName: bookmarkName,
        search: params.term,
      };
    },
  },
});

/*=============================================>>>>>
              = Main (On Load) =
===============================================>>>>>*/

/**
 * Get the amount of time a table state should be valid for in milliseconds.
 */
function getStateDuration() {
  var oneWeek = 7;
  return DEBUG && ForecastDebugger ? ForecastDebugger.getStaleStateTime() || Util.getTimeMillis(oneWeek) : Util.getTimeMillis(oneWeek);
}

// Get the local storage state if it exists.
state = JSON.parse(localStorage.getItem('DLState'));

// If the state is older than one week then reset it.
if (state) {
  try {
    var date = new Date();
    var stateAge = date.getTime() - state.time;
    var stateLifetime = getStateDuration();
    if (stateAge > stateLifetime) {
      state = 'undefined';
      localStorage.removeItem('DLState');
    }
  } catch (e) {
    state = 'undefined';
    localStorage.removeItem('DLState');
  }
}

if (state) {
  state = PatchParentColumns(state);
}

if (
  document.getElementById('Username').value == 'amyhollinger' ||
  document.getElementById('Username').value == 'shannawright' ||
  document.getElementById('Username').value == 'bradjulian' ||
  document.getElementById('Username').value == 'harrybarker' ||
  document.getElementById('Username').value == 'bobsuds'
) {
  isMerchandisingDirector = true;
} else if (document.getElementById('GMSVenID').value == 0 && document.getElementById('TableName').value !== 'tbl_AllVendors') {
  isMerchandisingManager = true;
}

if (isMerchandisingDirector == true || isMerchandisingManager == true || parseInt(document.getElementById('GMSVenID').value) === 0) {
  editVendorComment = false;
}

if (!$.isEmptyObject(JSON.parse(localStorage.getItem('DLState')))) {
  state = JSON.parse(localStorage.getItem('DLState'));
  if (state) {
    state = PatchParentColumns(state);
  }
  isNewUser = false;
} else {
  state = null;
  isNewUser = true;
}
if (state != null) {
  rotator = JSON.parse(JSON.stringify(state.rotator));
}

//Initialize bookmark popup modal
$(document).ready(function () {
  // the "href" attribute of the modal trigger must specify the modal ID that wants to be triggered
  $('.modal').modal();
  $('#hide_filters').tooltip({ delay: 25, tooltip: 'Go Fullscreen' });
  $('#hide_sum_boxes').tooltip({ delay: 25, tooltip: 'Collapse All Summary Boxes' });
  $('#fixed-column-switch input').prop('checked', true);
  $('#downloadModal').modal({
    complete: CloseDownloadTemplateHeaders, // Close all collapsibles when the modal closes
  });

  tutorialsManager = new TutorialsManager();
  dlEventsManager = new DlEventsManager();
  notificationsManager = new NotificationsManager(dlEventsManager, tutorialsManager);

  HideAllFilterCats();

  if (isNewUser == true) {
    SetDefaultColumnsView();
  } else {
    SetFilterButtons();
    // Remove the left border if the user is not a first time visiter.
    $('.su-def-col').removeClass('bol');
    $('.rp-def-col').removeClass('bol');
  }

  MoveRotatorButton();
  MoveFixedColumnSwitch();

  if (DEBUG) console.log(document.getElementById('Username').value + ' isMerchandisingDirector = ' + isMerchandisingDirector);
  if (DEBUG) console.log(document.getElementById('Username').value + ' isMerchandisingManager = ' + isMerchandisingManager);

  isFirstTimeLoading = false;
  // LoadSums();

  $('.preloader-background').delay(2000).fadeOut('slow');
  $('.preloader-wrapper').delay(2000).fadeOut();

  isNewUser = false;

  //fixedColumns.fnRedrawLayout();
  //DTable.columns.adjust();
  AdjustForecastTable();

  //Expand all TIME fiscal boxes for quick initial expanding load time
  ShowFiscalBoxes();

  //Display modal upon page load if there is no data from filter, or in the table.
  if (recordsFiltered == 0 && recordsTotal > 0) {
    NoFilteredRecords();
  } else if (recordsTotal == 0) {
    NoTotalRecords();
  }
});

//Create all tables for reference
var DTable;
var VUTable;
var DolTable;
var MarPerTable;
var MarDolTable;
LoadUnitsSummaryTable();
LoadDollarSummaryTable();
LoadMarginPercentSummaryTable();
LoadMarginDollarSummaryTable();
LoadDateUpdated();
LoadForecastTable();

AdjustForecastTable();
if (DEBUG) console.log('Main functions complete');

/*=============================================>>>>>
                 = Clickable Events =
===============================================>>>>>*/

//Clear the filters/bookmarks event.
$('#btn_ClearBookmark').click(function () {
  ClearBookmark();
});

//Create a bookmark event.
$('#btn_CreateBookmark').click(function () {
  CreateBookmark();
});

//Load a bookmark
$('#btn_LoadBookmark').click(function () {
  ShowProcessingLoader();
  setTimeout(function () {
    LoadBookmark();
  }, 100);
});

//Delete a bookmark
$('#btn_DeleteBookmark').click(function () {
  DeleteBookmark();
});

//Stop propagation on the rotator to allow for multiple clicks.
$('.dropdown-button + .dropdown-content').on('click', function (e) {
  e.stopPropagation();
});

//Event for the Full Export button.
$('#btn_ExportFull').click(function () {
  RunFullDownloadExport('ExportReportFullDownload');
});

//Event for the Export New Items button.
$('#btn_ExportNewItems').click(function () {
  RunFullDownloadExport('NewItemsExport');
});

//Event for the Full Export button.
$('#btn_TemplateItemPatchWeek').click(function () {
  RunFullDownloadExport('ItemPatchWeekTemplate');
});

//Event for the the Item Patch Week Template with data
$('#btn_DataItemPatchWeek').click(function () {
  RunFullDownloadExport('ItemPatchWeekData');
});

//Event for the Full Export button.
$('#btn_TemplateItemMMWeek').click(function () {
  RunFullDownloadExport('ItemMMWeekTemplate');
});

//Event for the the Item MM Week Template with data
$('#btn_DataItemMMWeek').click(function () {
  RunFullDownloadExport('ItemMMWeekData');
});

//Event for the Full Export button.
$('#btn_TemplateItemPatchTotal').click(function () {
  RunFullDownloadExport('ItemPatchTotalTemplate');
});

//Event for the the Patch Total Template with data
$('#btn_DataItemPatchTotal').click(function () {
  RunFullDownloadExport('ItemPatchTotalData');
});

//Event for the Full Export button.
$('#btn_TemplateItemMMTotal').click(function () {
  RunFullDownloadExport('ItemMMTotalTemplate');
});

//Event for the the MM Total Template with data
$('#btn_DataItemMMTotal').click(function () {
  RunFullDownloadExport('ItemMMTotalData');
});

//Event for the Full Export button.
$('#btn_TemplateItemRegionMM').click(function () {
  RunFullDownloadExport('ItemRegionMMTemplate');
});

//Event for the New Items Upload Template.
$('#btn_TemplateNewItemsUpload').click(function () {
  RunFullDownloadExport('NewItemsUploadTemplate');
});

//Event for the Item Ownership Template.
$('#btn_TemplateItemPatchOwnership').click(function () {
  RunFullDownloadExport('ItemPatchOwnershipTemplate');
});

//Event for the Item Ownership data.
$('#btn_DataItemPatchOwnership').click(function () {
  RunFullDownloadExport('ItemPatchOwnershipData');
});

//Event for the Item Overlap data.
$('#btn_DataItemPatchOverlap').click(function () {
  RunFullDownloadExport('ItemPatchOverlapData', 'IPOTable');
});

//Event for the the Item Region MM Template with data
$('#btn_DataItemRegionMM').click(function () {
  RunFullDownloadExport('ItemRegionMMData');
});

// Event for the Lowe's Forecasting file export
$('#btn_ExportLowesForecasting').click(() => RunFullDownloadExport('LowesForecastingTemplate'));

//toggle exclude filters changed
$('#exclude').on('change', function () {
  if ($(this).is(':checked')) {
    //exclud
    exclude = $(this).is(':checked');
    //change background-color to red
    changeStyle('.select2-container--default .select2-results__option--highlighted[aria-selected]', 'background-color', '#ff0000');
    changeStyle(
      '.select2-container--default .select2-results__option--highlighted[aria-selected][forecast-selected="true"]',
      'background-color',
      '#ff0000'
    );
    changeStyle(
      '.select2-container--default .select2-results__option--highlighted[aria-selected][forecast-selected-exclude="true"]',
      'background-color',
      '#ff0000'
    );
    changeStyle('.drag-selectable .ui-selecting', 'background', '#c00000');
  } else {
    //include
    switchStatus = $(this).is(':checked');
    exclude = $(this).is(':checked');
    //change background-color to blue
    changeStyle('.select2-container--default .select2-results__option--highlighted[aria-selected]', 'background-color', '#008dff');
    changeStyle(
      '.select2-container--default .select2-results__option--highlighted[aria-selected][forecast-selected="true"]',
      'background-color',
      '#008dff'
    );
    changeStyle(
      '.select2-container--default .select2-results__option--highlighted[aria-selected][forecast-selected-exclude="true"]',
      'background-color',
      '#008dff'
    );
    changeStyle('.drag-selectable .ui-selecting', 'background', '#0069c0');
  }
});

//With Rotator as an Array
$('.rotator-checkbox').change(function () {
  if ($(this).prop('checked') == true) {
    var count = rotator
      .map(function (e) {
        return e.column;
      })
      .indexOf(this.value);
    rotator[count].included = true;
  }

  if ($(this).prop('checked') == false) {
    var count = rotator
      .map(function (e) {
        return e.column;
      })
      .indexOf(this.value);
    rotator[count].included = false;
  }

  //DTable.columns.adjust().draw();
});

//Event for the accept button in the rotator dropdown.
$('#rotate-accept').on('click', function (e) {
  ShowProcessingLoader();
  if (DEBUG) console.log('line 1074');
  //Click on the body to close the dropdown.
  $('body').trigger('click');

  RunWithUpdater(function () {
    //Used to stop from jumping to the top of the page after 'Accept' is clicked.
    e.preventDefault();
    SetTableRotating(true);
    DTable.columns.adjust().draw();
    AdjustForecastTable();
  }, 280);
});

//Reset the rotator back to its original state before opening the dropdown
$('#rotate-btn').on('click', function () {
  ResetRotatorToState();
});

$('#updating').on('click', function (e) {
  PreventIfLoading(e);
});

//Event for hiding the grouping columns from the Add/Remove column visibility box.
$('button.btn.waves-effect.buttons-collection.buttons-colvis').on('click', function () {
  $('button.btn.waves-effect.buttons-columnVisibility').hide();
  if (salesDollarsGroup == true) {
    $('.sales-dollar-group').addClass('active');
  } else {
    $('.sales-dollar-group').removeClass('active');
  }
  if (turnsGroup == true) {
    $('.turns-group').addClass('active');
  } else {
    $('.turns-group').removeClass('active');
  }
  if (salesUnitsGroup == true) {
    $('.sales-units-group').addClass('active');
  } else {
    $('.sales-units-group').removeClass('active');
  }
  if (retailPriceGroup == true) {
    $('.retail-price-group').addClass('active');
  } else {
    $('.retail-price-group').removeClass('active');
  }
  if (mpSalesAndMarginGroup == true) {
    $('.mp-sales-and-margin-group').addClass('active');
  } else {
    $('.mp-sales-and-margin-group').removeClass('active');
  }
  if (priceSensGroup == true) {
    $('.price-sensitivity-group').addClass('active');
  } else {
    $('.price-sensitivity-group').removeClass('active');
  }
  if (aspGroup == true) {
    $('.asp-group').addClass('active');
  } else {
    $('.asp-group').removeClass('active');
  }
  if (marginPercGroup == true) {
    $('.margin-percent-group').addClass('active');
  } else {
    $('.margin-percent-group').removeClass('active');
  }
  if (marginDollGroup == true) {
    $('.margin-dollar-group').addClass('active');
  } else {
    $('.margin-dollar-group').removeClass('active');
  }
  if (sellThruGroup == true) {
    $('.sell-thru-group').addClass('active');
  } else {
    $('.sell-thru-group').removeClass('active');
  }
  if (recDollGroup == true) {
    $('.receipt-dollar-group').addClass('active');
  } else {
    $('.receipt-dollar-group').removeClass('active');
  }
  if (recUnitGroup == true) {
    $('.receipt-units-group').addClass('active');
  } else {
    $('.receipt-units-group').removeClass('active');
  }
  if (forecastGroup == true) {
    $('.forecast-group').addClass('active');
  } else {
    $('.forecast-group').removeClass('active');
  }
  if (costGroup == true) {
    $('.cost-group').addClass('active');
  } else {
    $('.cost-group').removeClass('active');
  }
  if (commentGroup == true) {
    $('.comments-group').addClass('active');
  } else {
    $('.comments-group').removeClass('active');
  }
});

//Event for closing individual filter chips
$('#filter-box-body').on('click', '#filter-chip', function (e) {
  ShowProcessingLoader();
  if (DEBUG) console.log('line 1246');
  var classes = ($(this).attr('class') || '').split(' ');
  var filterArr;
  var colName;
  var filterName;
  var isColumnSort = false;

  for (i = 0; i < classes.length; i++) {
    //get the actual name of the filter
    filterName = classes[i];

    switch (classes[i]) {
      case 'vendor':
        colName = 'VendorDesc:name';
        filterArr = filter_vendor;
        filter_vendor.remove(this.firstChild.wholeText);
        if (filter_vendor.length < 1) {
          $(this).remove();
          $('#filter-head-vendor').hide();
        }
        break;
      case 'md':
        colName = 'MD:name';
        filterArr = filter_md;
        filter_md.remove(this.firstChild.wholeText);
        if (filter_md.length < 1) {
          $(this).remove();
          $('#filter-head-md').hide();
        }
        break;
      case 'mm':
        colName = 'MM:name';
        filterArr = filter_mm;
        filter_mm.remove(this.firstChild.wholeText);
        if (filter_mm.length < 1) {
          $(this).remove();
          $('#filter-head-mm').hide();
        }
        break;
      case 'region':
        colName = 'Region:name';
        filterArr = filter_region;
        filter_region.remove(this.firstChild.wholeText);
        if (filter_region.length < 1) {
          $(this).remove();
          $('#filter-head-region').hide();
        }
        break;
      case 'district':
        colName = 'District:name';
        filterArr = filter_district;
        filter_district.remove(this.firstChild.wholeText);
        if (filter_district.length < 1) {
          $(this).remove();
          $('#filter-head-district').hide();
        }
        break;
      case 'patch':
        colName = 'Patch:name';
        filterArr = filter_patch;
        filter_patch.remove(this.firstChild.wholeText);
        if (filter_patch.length < 1) {
          $(this).remove();
          $('#filter-head-patch').hide();
        }
        break;
      case 'parent':
        colName = 'ParentConcat:name';
        filterArr = filter_parent;
        filter_parent.remove(this.firstChild.wholeText);
        if (filter_parent.length < 1) {
          $(this).remove();
          $('#filter-head-parent').hide();
        }
        break;
      case 'item':
        colName = 'ItemConcat:name';
        filterArr = filter_item;
        filter_item.remove(this.firstChild.wholeText);
        if (filter_item.length < 1) {
          $(this).remove();
          $('#filter-head-item').hide();
        }
        break;
      case 'prodgrp':
        colName = 'ProdGrpConcat:name';
        filterArr = filter_prodgrp;
        filter_prodgrp.remove(this.firstChild.wholeText);
        if (filter_prodgrp.length < 1) {
          $(this).remove();
          $('#filter-head-prodgrp').hide();
        }
        break;
      case 'assrt':
        colName = 'AssrtConcat:name';
        filterArr = filter_assrt;
        filter_assrt.remove(this.firstChild.wholeText);
        if (filter_assrt.length < 1) {
          $(this).remove();
          $('#filter-head-assrt').hide();
        }
        break;
      case 'fiscalwk':
        colName = 'FiscalWk:name';
        filterArr = filter_fiscalwk;
        filter_fiscalwk.remove(this.firstChild.wholeText);
        //Unselect fiscal week in the selectable box
        FiscalSelector('fiscal-wk-selectable', 'unselect', this.firstChild.wholeText, 'include');
        if (filter_fiscalwk.length < 1) {
          $(this).remove();
          $('#filter-head-fiscalwk').hide();
        }
        break;
      case 'fiscalmo':
        colName = 'FiscalMo:name';
        filterArr = filter_fiscalmo;
        filter_fiscalmo.remove(this.firstChild.wholeText);
        //Unselect fiscal month in the selectable box
        FiscalSelector('fiscal-mo-selectable', 'unselect', this.firstChild.wholeText, 'include');
        if (filter_fiscalmo.length < 1) {
          $(this).remove();
          $('#filter-head-fiscalmo').hide();
        }
        break;
      case 'fiscalqtr':
        colName = 'FiscalQtr:name';
        filterArr = filter_fiscalqtr;
        filter_fiscalqtr.remove(this.firstChild.wholeText);
        //Unselect fiscal quarter in the selectable box
        FiscalSelector('fiscal-qtr-selectable', 'unselect', this.firstChild.wholeText, 'include');
        if (filter_fiscalqtr.length < 1) {
          $(this).remove();
          $('#filter-head-fiscalqtr').hide();
        }
        break;
      case 'columnsort':
        isColumnSort = true;
        var index = $(this).index(); // This tells us where the filter is in the filter_columnsort
        // We can now remove it from the filter list
        setTimeout(function () {
          RemoveColumnToSort([filter_columnsort[index][0], filter_columnsort[index][1]]);
          UpdateColumnSortFilter(filter_columnsort);

          // Sort and redraw the table
          DTable.order(filter_columnsort).draw();
          if (filter_columnsort.length < 1) {
            $(this).remove();
            $('#filter-head-columnsort').hide();
          }
        }, 1);
        break;
        DTable.columns(colName).search(filterArr, false, false);
        $(this).remove();
    }
  }

  if (isColumnSort === false) {
    setTimeout(function () {
      UpdateFilter(filterName, filterArr);
      AdjustForecastTable();
    }, 1);
  }
});

//Event for closing individual exclude filter chips changing!!!
$('#filter-box-body').on('click', '#filter-chip-exclude', function (e) {
  ShowProcessingLoader();
  var classes = ($(this).attr('class') || '').split(' ');
  var excludeArr;
  var colName;
  var filterName;
  var isColumnSort = false;

  for (i = 0; i < classes.length; i++) {
    //get the actual name of the filter
    filterName = classes[i];

    switch (classes[i]) {
      case 'vendor':
        colName = 'VendorDesc:name';
        excludeArr = exclude_vendor;
        exclude_vendor.remove('!' + this.firstChild.wholeText);
        if (exclude_vendor.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-vendor').hide();
        }
        break;
      case 'md':
        colName = 'MD:name';
        excludeArr = exclude_md;
        exclude_md.remove('!' + this.firstChild.wholeText);
        if (exclude_md.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-md').hide();
        }
        break;
      case 'mm':
        colName = 'MM:name';
        excludeArr = exclude_mm;
        exclude_mm.remove('!' + this.firstChild.wholeText);
        if (exclude_mm.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-mm').hide();
        }
        break;
      case 'region':
        colName = 'Region:name';
        excludeArr = exclude_region;
        exclude_region.remove('!' + this.firstChild.wholeText);
        if (exclude_region.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-region').hide();
        }
        break;
      case 'district':
        colName = 'District:name';
        excludeArr = exclude_district;
        exclude_district.remove('!' + this.firstChild.wholeText);
        if (exclude_district.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-district').hide();
        }
        break;
      case 'patch':
        colName = 'Patch:name';
        excludeArr = exclude_patch;
        exclude_patch.remove('!' + this.firstChild.wholeText);
        if (exclude_patch.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-patch').hide();
        }
        break;
      case 'parent':
        colName = 'ParentConcat:name';
        excludeArr = exclude_parent;
        exclude_parent.remove('!' + this.firstChild.wholeText);
        if (exclude_parent.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-parent').hide();
        }
        break;
      case 'item':
        colName = 'ItemConcat:name';
        excludeArr = exclude_item;
        exclude_item.remove('!' + this.firstChild.wholeText);
        if (exclude_item.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-item').hide();
        }
        break;
      case 'prodgrp':
        colName = 'ProdGrpConcat:name';
        excludeArr = exclude_prodgrp;
        exclude_prodgrp.remove('!' + this.firstChild.wholeText);
        if (exclude_prodgrp.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-prodgrp').hide();
        }
        break;
      case 'assrt':
        colName = 'AssrtConcat:name';
        excludeArr = exclude_assrt;
        exclude_assrt.remove('!' + this.firstChild.wholeText);
        if (exclude_assrt.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-assrt').hide();
        }
        break;
      case 'fiscalwk':
        colName = 'FiscalWk:name';
        excludeArr = exclude_fiscalwk;
        exclude_fiscalwk.remove('!' + this.firstChild.wholeText);
        //Unselect fiscal week in the selectable box
        FiscalSelector('fiscal-wk-selectable', 'unselect', this.firstChild.wholeText, 'exclude');
        if (exclude_fiscalwk.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-fiscalwk').hide();
        }
        break;
      case 'fiscalmo':
        colName = 'FiscalMo:name';
        excludeArr = exclude_fiscalmo;
        exclude_fiscalmo.remove('!' + this.firstChild.wholeText);
        //Unselect fiscal month in the selectable box
        FiscalSelector('fiscal-mo-selectable', 'unselect', this.firstChild.wholeText, 'exclude');
        if (exclude_fiscalmo.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-fiscalmo').hide();
        }
        break;
      case 'fiscalqtr':
        colName = 'FiscalQtr:name';
        excludeArr = exclude_fiscalqtr;
        exclude_fiscalqtr.remove('!' + this.firstChild.wholeText);
        //Unselect fiscal quarter in the selectable box
        FiscalSelector('fiscal-qtr-selectable', 'unselect', this.firstChild.wholeText, 'exclude');
        if (exclude_fiscalqtr.length < 1) {
          $(this).remove();
          $('#filter-head-exclude-fiscalqtr').hide();
        }
        break;
      case 'columnsort':
        isColumnSort = true;
        var index = $(this).index(); // This tells us where the filter is in the filter_columnsort
        // We can now remove it from the filter list
        RemoveColumnToSort([filter_columnsort[index][0], filter_columnsort[index][1]]);
        UpdateColumnSortFilter(filter_columnsort);

        // Sort and redraw the table
        DTable.order(filter_columnsort).draw();

        if (filter_columnsort.length < 1) {
          $(this).remove();
          $('#filter-head-columnsort').hide();
        }
        break;

        DTable.columns(colName).search(excludeArr, false, false);
        $(this).remove();
    }
  }

  if (isColumnSort === false) {
    UpdateFilterExclude(filterName, excludeArr);
    if (DEBUG) console.log('line 1666');
    AdjustForecastTable();
  }
});

//Event for collapsing the side navigation menu.  Where Filters are located.
$('#hide_filters').click(function () {
  if ($('#nav_list')[0].hidden == false) {
    //Close
    document.getElementById('hide_filters').style.left = '0%';
    $('#nav_list').hide();
    document.getElementById('container').style.marginLeft = '1%';
    document.getElementById('container').style.maxWidth = '99%';
    $('i', this)[0].innerText = 'arrow_forward';
    $('#hide_filters').tooltip({ delay: 25, tooltip: 'Leave Fullscreen' });
    $('#nav_list')[0].hidden = true;
  } else {
    //Open
    $('#nav_list').show();
    document.getElementById('container').style.marginLeft = '16.6666666667%';
    document.getElementById('hide_filters').style.left = '14%';
    document.getElementById('container').style.maxWidth = '82%';
    $('i', this)[0].innerText = 'arrow_back';
    $('#hide_filters').tooltip({ delay: 25, tooltip: 'Go Fullscreen' });
    $('#nav_list')[0].hidden = false;
  }
  AdjustForecastTable();
});

//Event for collapsing all of the summary boxes.
$('#hide_sum_boxes').click(function () {
  if ($('#sum-row')[0].hidden == false) {
    //Close
    $('.collapsible.sum-nav').collapsible('close', 0);
    $('i', this)[0].innerText = 'arrow_downward';
    $('#hide_sum_boxes').tooltip({ delay: 25, tooltip: 'Expand All Summary Boxes' });
    $('#sum-row')[0].hidden = true;
    $('#ForecastTable_wrapper').addClass('full-screen-table');
  } else {
    //Open
    $('.collapsible.sum-nav').collapsible('open', 0);
    $('i', this)[0].innerText = 'arrow_upward';
    $('#hide_sum_boxes').tooltip({ delsay: 25, tooltip: 'Collapse All Summary Boxes' });
    $('#sum-row')[0].hidden = false;
    $('#ForecastTable_wrapper').removeClass('full-screen-table');
  }
  AdjustForecastTable();
});

// Event for clearing filter chips. This handles the clear event for every filter available
// Garbage can clear
$('i.filter-clear-button.material-icons.wave.bottom-wave').on('click', function (event) {
  ShowProcessingLoader();
  // This gets the id of the filter body, for example: filter-body-item
  var chipBodyId = event.currentTarget.parentElement.nextElementSibling.id;

  // This gets the last item from the above array which is "item"
  var filterName = GetFilterNameFromId(chipBodyId);
  if (chipBodyId.includes('exclude') == true) {
    var filterNameConcat = 'exclude_' + filterName;
  } else {
    // Here we build the filter array property name like such "filter_item"
    var filterNameConcat = 'filter_' + filterName;
  }
  setTimeout(function () {
    // Here we update the filter status in the table and filter box
    if (filterName === 'columnsort') {
      ShowProcessingLoader();
      // By calling window[filterNameConcate] we get filter_item.length and set it to zero to
      // clear the array. This allows us to access all filters using this one event.
      window[filterNameConcat].length = 0;

      UpdateColumnSortFilter(window[filterNameConcat]);

      DTable.order(window[filterNameConcat]).draw();
    } else {
      // By calling window[filterNameConcate] we get filter_item.length and set it to zero to
      // clear the array. This allows us to access all filters using this one event.
      window[filterNameConcat].length = 0;

      if (chipBodyId.includes('exclude') == true) {
        UpdateFilterExclude(filterName, window[filterNameConcat]);
        if (DEBUG) console.log('line 1748');
      } else {
        UpdateFilter(filterName, window[filterNameConcat]);
        if (DEBUG) console.log('line 1751');
      }
    }
  }, 1);
});

// Event that stops the menu from being set in the wrong place
$('#user-menu-icon').on('click', function (e) {
  e.stopImmediatePropagation();
});

// Prevents the 'Uploads' tab from being highlighted per ticket request and opens the upload modal
// It doesn't toggle it because the tab is hidden by an overlay and when you click on that overlay the
// modal closes by itself.
$('#uploads-modal-trigger').on('click', function (e) {
  e.stopImmediatePropagation();
  $('#uploadModal').modal('open');
});

// Prevents the 'Downloads' tab from being highlighted per ticket request and opens the upload modal
// It doesn't toggle it because the tab is hidden by an overlay and when you click on that overlay the
// modal closes by itself.
$('#downloads-modal-trigger').on('click', function (e) {
  e.stopImmediatePropagation();
  $('#downloadModal').modal('open');
});

// Prevents the 'Bookmarks' tab from being highlighted per ticket request and opens the upload modal
// It doesn't toggle it because the tab is hidden by an overlay and when you click on that overlay the
// modal closes by itself.
$('#bookmarks-modal-trigger').on('click', function (e) {
  e.stopImmediatePropagation();
  $('#bookmarkModal').modal('open');
});

// Event for openning the user dropdown
$('#user-dropdown-trigger').on('click', function (e) {
  e.stopImmediatePropagation();
  $('#user-dropdown-trigger').dropdown('open');
  var xys = GetElementXYAsPercent(e, $('#user-dropdown'), 'right');

  $('#user-dropdown').css('right', parseInt(xys.X.right) + '%');
  $('#user-dropdown').css('left', '');
  $('#user-dropdown').css('top', xys.Y.top + 1 + '%');
});

// Event that gets the notifications
$('#notifications-button').on('click', function (e) {
  notificationsManager.open();
});

// Event that opens the tutorials modal
$('#tutorial-button').on('click', function (e) {
  if (typeof tutorialsManager === 'undefined') {
    tutorialsManager = new TutorialsManager();
  }

  tutorialsManager.open();
});

// Event that opens the DlEvent modal
$('#dl-event-button').on('click', function (e) {
  if (typeof dlEventsManager === 'undefined') {
    dlEventsManager = new DlEventsManager();
  }

  dlEventsManager.open();
});

// Event that opens the admin modal
$('#admin-create-button').on('click', function (e) {
  if (!adminModalManager) {
    adminModalManager = new AdminModalManager(notificationsManager, tutorialsManager, dlEventsManager);
  }

  adminModalManager.open();
});

// Event that shows all tables for the forecast tab
$('#forecast-tab-item').on('click', function (e) {
  var isForecastTableVisible = $('#ForecastTable_wrapper').is(':visible');
  if (isForecastTableVisible) {
    return;
  }
  $('#sum-row').show();
  $('#ForecastTable_wrapper').show();
  $('#hide_sum_boxes').show();
  $('#hide_filters').show();
  $('#nav_list').show();
  ShrinkNavs();
  $('#ipo_table_container').css('display', 'none');
  if (ExceptionsTabModule.isForecastTableDirty()) {
    RunWithUpdater(function () {
      DTable.columns.adjust().draw();
      ExceptionsTabModule.setForecastTableStateClean();
    }, 200);
  }
});

// Event that shows all tables for the forecast tab
$('#exceptions-tab-item').on('click', function (e) {
  var isIPOTableVisible = $('#ipo_table_wrapper').is(':visible');
  if (isIPOTableVisible) {
    return;
  }
  $('#sum-row').hide();
  $('#ForecastTable_wrapper').hide();
  $('#hide_sum_boxes').hide();
  $('#hide_filters').hide();
  $('#nav_list').hide();
  ExpandNavs();
  $('#ipo_table_container').css('display', 'block');
  ExceptionsTabModule.ipoOverlapTable.draw(true);
});

function ShrinkNavs() {
  $('#container').css('margin-left', '16.6666666667%');
  $('#container').css('max-width', '82%');
}

function ExpandNavs() {
  $('#container').css('margin-left', '0px');
  $('#container').css('max-width', '100%');
}

/**************************************************************
 *              Fiscal Selector Box Events
 **************************************************************/

//Create the fiscal month box when the fiscal month collapsible is open for the first time
$('#fiscal-mo-selectable-header').on('click', function () {
  //Only call the fiscal month seletor box to be created if it doesn't already exist. This way
  //we don't call the function every time the fiscal month header is clicked
  if ($('#fiscal-mo-selectable')[0].childElementCount < 1) {
    ShowFiscalMonthSelectorBox();
  }
});

//Create the fiscal quarter box when the fiscal quarter collapsible is open for the first time
$('#fiscal-qtr-selectable-header').on('click', function () {
  //Only call the fiscal quarter seletor box to be created if it doesn't already exist. This way
  //we don't call the function every time the fiscal quarter header is clicked
  if ($('#fiscal-qtr-selectable')[0].childElementCount < 1) {
    ShowFiscalQuarterSelectorBox();
  }
});

//Create the fiscal week box when the fiscal week collapsible is open for the first time
$('#fiscal-wk-selectable-header').on('click', function () {
  //Only call the fiscal week seletor box to be created if it doesn't already exist. This way
  //we don't call the function every time the fiscal week header is clicked
  if ($('#fiscal-wk-selectable')[0].childElementCount < 1) {
    ShowFiscalWeekSelectorBox();
  }
});

//This listens for any fiscal weeks/months/quarters being selected. If the Ctrl key is pressed down then we assign the
//fiscal_select_ctrl_event to true for later use in the ctrl keyup event.
$('.drag-selectable').on('selectableselecting', function (event, ui) {
  fiscal_select_ctrl_event = event.ctrlKey ? true : false;

  //We need to keep track of whether the mouse key is down or not just incase the user lets go of the
  //Ctrl key. We don't want to trigger a filter update if the user is still selecting weeks by dragging the mouse.
  fiscal_select_mouse_click = true;
});

//This listens for any fiscal weeks/months/quarters being unselected. If the Ctrl key is pressed down then we assign the
//fiscal_select_ctrl_event to true for later use in the ctrl keyup event.
$('.drag-selectable').on('selectableunselecting', function (event, ui) {
  fiscal_select_ctrl_event = event.ctrlKey ? true : false;
});

// This events handles an keyup events that happen in the body.
$('body').keyup(function (event) {
  // This section handles updating the table sort order but does not redraw the table.
  // The redrawing of the table has been moved to the end of the function to allow the
  // filters to be updated if any were selected.
  if (column_sort_ctrl_event) {
    UpdateColumnSortFilter(filter_columnsort);
    DTable.order(filter_columnsort);
  }
  /* This sections listens for the Ctrl keyup event inside the Fiscal weeks/months/quarters Selector box.

       If a user multiselects some weeks and lets go of the Ctrl key after all the selections
       then we need a way to lister for that keyup event to start the filter process. When a user is selecting
       weeks/months/quarters, there's a selecting event that is fired off and sets the fiscal_select_ctrl_event to true if the user
       is holding the Ctrl key down so we make sure that fiscal_select_ctrl_event is true before filtering the weeks.

       A new addition to this has been made and that's the select2 filters. which is filter_ctrl_key that is set when
       the select2 filters are being selected. This allows the user to select any filter while holding the Ctrl key
       and all filters will be updated upon letting go of the Ctrl key.
     */
  if (event.which == 17 && (fiscal_select_ctrl_event || filter_ctrl_key) && !fiscal_select_mouse_click) {
    for (var i = 0; i < filters_selected.length; i++) {
      if (exclude == false) {
        UpdateFilter(filters_selected[i].id, filters_selected[i].filter);
        if (DEBUG) {
          console.log('INCLUDE');
        }
      } else {
        UpdateFilterExclude(filters_selected[i].id, filters_selected[i].filter);
        if (DEBUG) {
          console.log('EXCLUDE');
        }
      }
    }

    // There were instances where filter dropdowns werent closing by themselves
    // so we close them this way.
    for (var j = 0; j < filters_selected.length; j++) {
      var filterId = '#filter-' + filters_selected[j].id;

      if ($(filterId).length > 0 && $(filterId).select2('isOpen')) {
        $(filterId).select2('close');
      }
    }

    filters_selected.length = 0;
    DTable.columns.adjust();
  }
  //jumps here first
  // This has been moved to the bottom to allow all filters to updated the table in any way then need
  // to before redrawing it.
  if (event.which == 17 && (fiscal_select_ctrl_event || filter_ctrl_key || column_sort_ctrl_event) && !fiscal_select_mouse_click) {
    ShowProcessingLoader();
    if (DEBUG) console.log('line 1760');
    //have to add so processing screen comes up for multi select
    setTimeout(function () {
      // We handle updating the table here just incase the user selects more than one category of fiscal filters
      // at the same time when holding the Ctrl button. This is so we don't end up reloading the table up
      // to three times in one selection
      column_sort_ctrl_event = false;
      fiscal_select_ctrl_event = false;
      filter_ctrl_key = false;
      if (DEBUG) console.log('line 1832 before DTable.draw()');
      DTable.draw();
    }, 1);
  }
});

//Set fiscal_wk_mouse_click to false that way it doesn't interfere with the Ctrl key up filter trigger
$('body').mouseup(function () {
  fiscal_select_mouse_click = false;
});

//Event to open the fiscal boxes when the Time collapsible opens
$('#time-collapsible-header').on('click', function (e) {
  if ($(e.currentTarget).hasClass('active') == false) {
    ShowFiscalBoxes();
  }
});

/**************************************************************
 *          End of Fiscal Selector Box Events
 **************************************************************/

//Event to toggle filter arrow up or down according to being open or closed.
$('a.collapsible-header.waves-effect').click(function () {
  // Added check for non-sortable columns.
  var header = $('i', this)[0];
  if (!header) {
    return;
  }

  if ($('i', this)[0].innerText === 'keyboard_arrow_down') {
    $('i', this)[0].innerText = 'keyboard_arrow_up';
  } else {
    $('i', this)[0].innerText = 'keyboard_arrow_down';
  }
});

//Event to log out.
$('#lnk_logout').click(function () {
  Logout();
});

//Switch to enable or disable fixed columns.
$('#fixed-column-switch')
  .find('input[type=checkbox]')
  .on('change', function () {
    if ($(this).prop('checked')) {
      ShowProcessingLoader();
      if (DEBUG) console.log('fixed-column event checked');
      setTimeout(function () {
        fixedColumns = new $.fn.dataTable.FixedColumns(DTable, {
          iLeftColumns: iFixedColumns,
        });
      }, 1);
    } else {
      if (DEBUG) console.log('fixed-column event unchecked');
      if (!fixedColumns) {
        DTable.fixedColumns().context[0]._oFixedColumns.destroy();
      } else {
        fixedColumns.destroy();
      }
      delete DTable.fixedColumns().context[0]._oFixedColumns;
    }
  });

//Event to upload file
$('#upload').click(function () {
  disableUploadButtonById('upload', true);
  UploadFile();
});

//Trigger the upload browse.
$('#uploadTrigger').click(function () {
  $('#uploadBrowse').click();
});

//Event to upload new items file
$('#new_item_upload').click(function () {
  disableUploadButtonById('new_item_upload', true);
  UploadNewItemsFile();
});

//Trigger the new items upload browse.
$('#upload_new_item_trigger').click(function () {
  $('#upload_new_item_browse').click();
});

// Listens to the state change in the file inputs in the uploads modal and assigns the file
// name to the appropriate section.
$('#uploadBrowse, #upload_new_item_browse, #upload_iou_browse').on('change', function (e) {
  var inputId = '#' + e.target.id;

  // Goes up the DOM tree and finds the first file form.
  var fileFormElement = $(inputId).closest('.upload-modal-file-form')[0];

  // Clear all file forms but exclude the current one.
  ClearUploadForms(fileFormElement.id);
  // Clear all file names for the file inputs.
  ClearUploadFileNames();

  DisableUploadButtons();

  // Get the form data and file name for the current file form.
  var fileData = new FormData(fileFormElement);
  var fileName = fileData.values().next().value.name;

  // Get the parent of the file browse button to find the file name div.
  var parentElement = $(inputId).parent()[0];
  var fileTitleElement = $('#' + parentElement.id + ' .upload-modal-section-file-name-title')[0];

  // Assign the file name.
  if (fileTitleElement) {
    fileTitleElement.innerHTML = fileName;
    if (fileName.length > 0) {
      var triggerButton = fileTitleElement.attributes['triggerButton'].value;
      if (triggerButton) {
        $('#' + triggerButton)
          .parent()
          .removeClass('disabled');
        disableUploadButtonById(triggerButton, false);
      }
    }
  }
});

//Event to upload item patch ownership file
$('#iou_upload').click(function () {
  disableUploadButtonById('iou_upload', true);
  UploadItemPatchOwnershipFile();
});

//Trigger the item patch ownership upload browse.
$('#upload_iou_trigger').click(function () {
  $('#upload_iou_browse').click();
});

/*=============================================>>>>>
          = Data Table / Summary Boxes =
===============================================>>>>>*/

//Populate the main datatable.
function LoadForecastTable() {
  var tempParams = {};
  PopulateRotator();
  ///
  /// Instantiates the editor and sets options.
  ///
  editor = new $.fn.dataTable.Editor({
    ajax: {
      url: '/Home/UpdateTableData',
      data: function (e) {
        if (DEBUG) console.log('editor: data: function()');
        e.columns = JSON.parse(JSON.stringify(params.columns));
        e.gmsvenid = parseInt(document.getElementById('GMSVenID').value);
        e.tableName = document.getElementById('TableName').value;
        e.username = document.getElementById('Username').value;
        e.rotator = JSON.parse(JSON.stringify(rotator));
        e.isMM = isMerchandisingManager;
        e.isMD = isMerchandisingDirector;
        e.editMode = editMode;
        e.vendorGroup = document.getElementById('VendorGroup').value;
        if (editMode == 'inline') {
          var i = 0;
          var j = 0;
          for (i = 0; i < rotator.length; i++) {
            if (rotator[i].included === true) {
              // Look in the rotator for included columns
              for (j = 0; j < e.columns.length; j++) {
                // Put the data for the included columns into the where clause
                if (e.columns[j].name == rotator[i].column) {
                  e.columns[j].search.value = DTable.cell(lastEditedCell._DT_CellIndex.row, j).data(); // Cell(row, column)
                  if (DEBUG) console.log('This data:' + e.columns[j].search.value + ' is added to the search column ' + e.columns[j]);
                }
              }
            }
          }
        }
        tempParams = e;
      },
      beforeSend: function () {
        ShowProcessingLoader();
      },
      success: function (x) {
        if (DEBUG) console.log('Editor ajax success');

        // record the status of the last request for later use
        editor['dlResponse'] = x.response === 'zero-allocation' ? 'error' : 'ok';

        // if edit was not successful then display error to user and prevent further execution
        if (x.response && x.response === 'zero-allocation') {
          HideProcessingLoader();
          alert(x.message);
          return;
        }

        if (x.count !== undefined) {
          alert("You're modifying too many records. Please limit your dataset with filters and try again.");
          return;
        }

        UpdateEditedCells(editMode);
      },
      error: function () {
        if (DEBUG) console.log('Editor ajax error');
      },
    },
    table: '#ForecastTable',
    i18n: {
      multi: {
        title: '0',
        info: ' ',
      },
    },
    idSrc: 'ForecastID',
    display: 'lightbox',
    template: '#editableForm',
    fields: [
      {
        label: 'Forecast Variance',
        name: 'Units_FC_LOW_Var',
        attr: {
          maxlength: 4,
          placeholder: 'Forecast Variance',
        },
        //"fieldInfo": recordsFiltered.toString() + " records will be affected "
      },
      {
        label: 'Sales Units',
        name: 'SalesUnits_FC',
        attr: {
          maxlength: 12,
          placeholder: 'Sales Units',
        },
        //"fieldInfo": recordsFiltered.toString() + " records will be affected "
      },
      {
        label: 'Retail Price',
        name: 'RetailPrice_FC',
        attr: {
          maxlength: 12,
          placeholder: 'Retail Price',
        },
        //"fieldInfo": "Item and Patch must be selected to edit retail price. " + recordsFiltered.toString() + " records will be affected "
      },
      {
        label: 'MM Comments',
        name: 'MM_Comments',
        attr: {
          maxlength: 500,
          placeholder: 'LGM Comments',
        },
        //"fieldInfo": recordsFiltered + " records will be affected "
      },
      {
        label: 'Vendor Comments',
        name: 'Vendor_Comments',
        attr: {
          maxlength: 500,
          placeholder: 'Vendor Comments',
        },
        //"fieldInfo": recordsFiltered + " records will be affected "
      },
    ],
    formOptions: {
      main: {
        buttons: false,
        focus: 1,
        message: true,
        onBackground: 'blur',
        onBlur: 'close',
        onComplete: 'close',
        onEsc: 'close',
        onFieldError: 'focus',
        onReturn: 'submit',
        submit: 'changed',
        title: false,
        drawType: false,
      },
      inline: {
        message: true,
        onBackground: 'blur',
        onBlur: 'close',
        onComplete: 'close',
        onEsc: 'close',
        onFieldError: 'focus',
        onReturn: 'submit',
        submit: 'changed',
        title: false,
        drawType: false,
      },
    },
  });

  ///
  /// Event that fires prior to submit event.
  /// Used for client side validation of the Forecast Sales Unit field.
  /// Checks for a negative number. If exists, returns false and submit event will not be fired.
  ///
  editor.on('preSubmit', function (e, o, action) {
    var salesUnitsVal = this.field('SalesUnits_FC').val();
    if (salesUnitsVal < 0) {
      alert('Negative units are not allowed.  Please enter a number greater than or equal to zero.');
      return false;
    }
  });

  ///
  /// Event that fires when the editor request has been
  /// submitted to the server and returned successfully or
  /// with an error.
  ///
  editor.on('submitComplete', function (e, json, data) {
    if (DEBUG) console.log('submitComplete event');
    if (editor['dlResponse'] && editor['dlResponse'] === 'error') return;
    // Automatically click into the next cell.
    if (editMode == 'inline') {
      $(DTable.cell(lastEditedCell._DT_CellIndex.row + 1, lastEditedCell._DT_CellIndex.column).node()).click();
    }
    RunWithUpdater(function () {
      LoadSums();
      MarDolTable.draw();
      MarPerTable.draw();
      DolTable.draw();
      VUTable.draw();
      DTable.columns.adjust();
    }, 200);
  });

  ///
  /// Event that fires when the editor request has been
  /// submitted to the server.
  ///
  editor.on('submitSuccess', function (e, json, data) {
    if (DEBUG) console.log('submitSuccess event');
  });

  ///
  ///
  ///
  editor.on('initEdit', function () {
    if (DEBUG) console.log('initEdit event');
  });

  ///
  /// On closing an editor menu event
  ///
  editor.on('close', function () {
    if (DEBUG) console.log('onClose event');
    if (editor['dlResponse'] && editor['dlResponse'] === 'error') {
      HideProcessingLoader();
      return;
    }

    // Invalidating the cells to clear the cache, which prevents
    // the server-side cell updates from working.
    DTable.rows().every(function (rowIdx, tableLoop, rowLoop) {
      DTable.row(rowIdx).invalidate();
    });
    HideProcessingLoader();
  });

  ///
  /// This is triggered when the editing form opens, either the
  /// main multi-edit for or the simple input with the inline form.
  /// Params: "e" is the jQuery object, mode is either 'main' or 'inline'
  /// and action is 'edit' since we aren't doing 'create' or 'remove'.
  ///
  editor.on('open', function (e, mode, action) {
    if (DEBUG) console.log('Open event');

    e.stopPropagation();
    var cellType = 'all';
    editMode = mode;
    if (DEBUG) console.log(editMode);
    if (mode == 'main') {
      // Show the dynamic message
      editor.field('SalesUnits_FC').fieldInfo(function () {
        if (recordsFiltered === undefined) {
          return ' ';
        } else {
          return formatNumberComma(recordsFiltered) + ' records will be affected ';
        }
      });

      // Show the dynamic message
      editor.field('RetailPrice_FC').fieldInfo(function () {
        if (recordsFiltered === undefined) {
          return 'Item and Patch must be selected to edit retail price.';
        } else {
          return 'Item and Patch must be selected to edit retail price. ' + formatNumberComma(recordsFiltered) + ' records will be affected ';
        }
      });

      // Show the dynamic message
      editor.field('Units_FC_LOW_Var').fieldInfo(function () {
        if (recordsFiltered === undefined) {
          return ' ';
        } else {
          return formatNumberComma(recordsFiltered) + ' records will be affected ';
        }
      });

      // Show the dynamic message
      editor.field('MM_Comments').fieldInfo(function () {
        if (recordsFiltered === undefined) {
          return ' ';
        } else {
          return recordsFiltered + ' records will be affected ';
        }
      });

      // Show the dynamic message
      editor.field('Vendor_Comments').fieldInfo(function () {
        if (recordsFiltered === undefined) {
          return ' ';
        } else {
          return recordsFiltered + ' records will be affected ';
        }
      });

      // Add a class to the modal button so we can style it.
      $('.DTE_Form_Buttons').children('button').addClass('waves-effect waves-light btn white-text');
      $('.DTE_Form_Buttons').children('button').css('background', 'blue');

      if (IsRetailPriceEditValid()) {
        editor.enable('RetailPrice_FC'); // Enable editing
      } else {
        editor.disable('RetailPrice_FC'); // disable it
      }

      if (isMerchandisingDirector == true) {
        editor.enable('Units_FC_LOW_Var');
        $(editor.node()[0]).css('display', '');
      } else {
        editor.disable('Units_FC_LOW_Var'); // disable it
        $(editor.node()[0]).css('display', 'none'); // hide it
      }
    } else if (mode == 'inline') {
      $('#DTE_Field_' + this.s.includeFields[0]).select();
      if (
        CheckMMCommentState() === 'Valid' &&
        ((isMerchandisingManager === true && isMerchandisingDirector === false) || IsUser('amyhollinger') || IsUser('shannawright'))
      ) {
        editor.enable('MM_Comments');
      } else {
        editor.disable('MM_Comments');
      }
      if (isMerchandisingManager == true || isMerchandisingDirector == true) {
        editor.disable('Vendor_Comments');
      } else if (CheckVendorCommentState() === 'Valid' && editVendorComment == true) {
        editor.enable('Vendor_Comments');
      } else {
        editor.disable('Vendor_Comments');
      }

      // Show the dynamic message
      //editor.field('SalesUnits_FC', 'RetailPrice_FC', 'Units_FC_LOW_Var', 'MM_Comments', 'Vendor_Comments').fieldInfo(function () { return " "; } );

      // Show the dynamic message
      editor.field('SalesUnits_FC').fieldInfo(function () {
        if (recordsFiltered === undefined) {
          return ' ';
        } else {
          return ' ';
        }
      });

      // Show the dynamic message
      editor.field('RetailPrice_FC').fieldInfo(function () {
        if (recordsFiltered === undefined) {
          return ' ';
        } else {
          return ' ';
        }
      });

      // Show the dynamic message
      editor.field('Units_FC_LOW_Var').fieldInfo(function () {
        if (recordsFiltered === undefined) {
          return ' ';
        } else {
          return ' ';
        }
      });

      // Show the dynamic message
      editor.field('MM_Comments').fieldInfo(function () {
        if (recordsFiltered === undefined) {
          return ' ';
        } else {
          return ' ';
        }
      });

      // Show the dynamic message
      editor.field('Vendor_Comments').fieldInfo(function () {
        if (recordsFiltered === undefined) {
          return ' ';
        } else {
          return ' ';
        }
      });
    }
  });

  ///
  /// Activate the inline editor on click of a table cell
  ///
  $('#ForecastTable').on('click', 'tbody td', function (e) {
    if (DEBUG) console.log('Edit table click event');
    PreventIfLoading(e);
    lastEditedCell = this; // We will use this later when doing auto-click.
    if (DTable.init().aoColumns[this._DT_CellIndex.column].sName == 'RetailPrice_FC' && isRetailVarEditable == false) {
      alert('Retail price is only editable at the ItemID & Patch level.');
    } else if (DTable.init().aoColumns[this._DT_CellIndex.column].sName == 'Units_FC_LOW_Var' && isMerchandisingDirector == false) {
      alert('Only merchandising directors can edit this cell.');
    } else if (
      DTable.init().aoColumns[this._DT_CellIndex.column].sName == 'MM_Comments' &&
      (editVendorComment == true ||
        (isMerchandisingManager == false &&
          isMerchandisingDirector == true &&
          parseInt(document.getElementById('GMSVenID').value) === 0 &&
          !(IsUser('amyhollinger') || IsUser('shannawright'))))
    ) {
      alert('Must be a merchandising manager to edit this cell.');
    } else if (DTable.init().aoColumns[this._DT_CellIndex.column].sName == 'MM_Comments' && isMMComment == false) {
      alert('LGM comments are only editable at the ItemID, LGM & Vendor level.');
    } else if (
      DTable.init().aoColumns[this._DT_CellIndex.column].sName === 'Vendor_Comments' &&
      (editVendorComment == false || parseInt(document.getElementById('GMSVenID').value) === 0)
    ) {
      alert('Only vendors can edit this cell.');
    } else if (
      DTable.init().aoColumns[this._DT_CellIndex.column].sName == 'Vendor_Comments' &&
      isVendorComment == false &&
      editVendorComment == true
    ) {
      alert('Vendor comments are only editable at the ItemID & LGM level.');
    } else {
      editor.inline(this);
    }
  });

  ///
  /// Table Object
  ///
  DTable = $('#ForecastTable').DataTable({
    processing: true,
    language: {
      //"zeroRecords": "There are no records for the selected filters.  Please remove the most recently added filter. <br></br> " +
      //"If you are seeing this message and have no filters applied, please contact support@demandlink.com.",
      processing: "<img src='/Content/Images/updating.gif' />",
    },
    serverSide: true,
    deferRender: true,
    preDrawCallback: function (settings) {
      ShowProcessingLoader();
      if (DEBUG) console.log('draw() called');
    },
    stateSave: true,
    stateDuration: getStateDuration(),
    stateSaveParams: function (settings, data) {
      if (loadFromBookmark == true && state !== undefined) {
        data = params;
      } else {
        data.gmsvenid = parseInt(document.getElementById('GMSVenID').value).toString();
        data.tableName = document.getElementById('TableName').value;
        data.rotator = rotator;
        if (data.columns !== undefined) {
          len = data.columns.length;
          for (i = 0; i < len; i++) {
            data.columns[i].search.value = params.columns[i].search.value;
            data.columns[i].searchable = params.columns[i].searchable;
            data.columns[i].data = i;
            data.columns[i].name = params.columns[i].name;
            data.columns[i].orderable = params.columns[i].orderable;
          }
        }
      }
    },
    stateSaveCallback: function (settings, data) {
      if (DEBUG) console.log('stateSaveCallBack');
      if (loadFromBookmark == false) {
        data = PatchParentColumns(data);
        params = data;
        localStorage.setItem('DLState', JSON.stringify(data));
      }
    },
    stateLoadCallback: function (settings, callback) {
      try {
        var tempState = JSON.parse(localStorage.getItem('DLState'));
        if (tempState) {
          tempState = PatchParentColumns(tempState);
        }
        return tempState;
      } catch (e) {
        return JSON.parse(localStorage.getItem('DataTables_ForecastTable_/'));
      }
    },
    drawCallback: function (settings) {
      if (DEBUG) console.log('drawCallBack');

      api = this.api();
      isLastDraw = false;
      var apiArr = [];
      for (var i = 0; i < rotator.length; i++) {
        if (rotator[i].included == true) {
          apiArr.push(rotator[i].column);
        }
      }

      if (
        (api.column('VendorDesc:name').data(0)[0] === '-1' || api.column('VendorDesc:name').data(0)[0] == undefined) &&
        //&& rotator.find(x => x.column === 'VendorDesc').included == false)
        apiArr.indexOf(rotator['VendorDesc']) == -1
      ) {
        api.column('VendorDesc:name').visible(false);
        api.column('VBUPercent:name').visible(false);
      } else {
        api.column('VendorDesc:name').visible(true);
        // TEMP: Remove/Restore later.
        // api.column('VendorDesc:name').visible(true);
        api.column('VBUPercent:name').visible(false);
      }
      if (
        (api.column('ItemID:name').data(0)[0] === -1 || api.column('ItemID:name').data(0)[0] == undefined) &&
        // && rotator.find(x => x.column === 'ItemID').included == false){
        apiArr.indexOf(rotator['ItemID']) == -1
      ) {
        api.column('ItemID:name').visible(false);
        api.column('ItemDesc:name').visible(false);
      } else {
        api.column('ItemID:name').visible(true);
        api.column('ItemDesc:name').visible(true);
      }
      if (
        (api.column('FiscalWk:name').data(0)[0] === -1 || api.column('FiscalWk:name').data(0)[0] == undefined) &&
        //&& rotator.find(x => x.column === 'FiscalWk').included == false)
        apiArr.indexOf(rotator['FiscalWk']) == -1
      ) {
        api.column('FiscalWk:name').visible(false);
      } else {
        api.column('FiscalWk:name').visible(true);
      }
      if (
        (api.column('FiscalMo:name').data(0)[0] === -1 || api.column('FiscalMo:name').data(0)[0] == undefined) &&
        //&& rotator.find(x => x.column === 'FiscalMo').included == false)
        apiArr.indexOf(rotator['FiscalMo']) == -1
      ) {
        api.column('FiscalMo:name').visible(false);
      } else {
        api.column('FiscalMo:name').visible(true);
      }
      if (
        (api.column('FiscalQtr:name').data(0)[0] === -1 || api.column('FiscalQtr:name').data(0)[0] == undefined) &&
        // && rotator.find(x => x.column === 'FiscalQtr').included == false)
        apiArr.indexOf(rotator['FiscalQtr']) == -1
      ) {
        api.column('FiscalQtr:name').visible(false);
      } else {
        api.column('FiscalQtr:name').visible(true);
      }
      if (
        (api.column('MD:name').data(0)[0] == '-1' || api.column('MD:name').data(0)[0] == undefined) &&
        //&& rotator.find(x => x.column === 'MD').included == false)
        apiArr.indexOf(rotator['MD']) == -1
      ) {
        api.column('MD:name').visible(false);
      } else {
        api.column('MD:name').visible(true);
      }
      if (
        (api.column('MM:name').data(0)[0] === '-1' || api.column('MM:name').data(0)[0] == undefined) &&
        // && rotator.find(x => x.column === 'MM').included == false)
        apiArr.indexOf(rotator['MM']) == -1
      ) {
        api.column('MM:name').visible(false);
      } else {
        api.column('MM:name').visible(true);
      }
      if (
        (api.column('Region:name').data(0)[0] === '-1' || api.column('Region:name').data(0)[0] == undefined) &&
        // && rotator.find(x => x.column === 'Region').included == false)
        apiArr.indexOf(rotator['Region']) == -1
      ) {
        api.column('Region:name').visible(false);
      } else {
        api.column('Region:name').visible(true);
      }
      if (
        (api.column('District:name').data(0)[0] === '-1' || api.column('District:name').data(0)[0] == undefined) &&
        //&& rotator.find(x => x.column === 'District').included == false)
        apiArr.indexOf(rotator['District']) == -1
      ) {
        api.column('District:name').visible(false);
      } else {
        api.column('District:name').visible(true);
      }
      if (
        (api.column('Patch:name').data(0)[0] === '-1' || api.column('Patch:name').data(0)[0] == undefined) &&
        //&& rotator.find(x => x.column === 'Patch').included == false)
        apiArr.indexOf(rotator['Patch']) == -1
      ) {
        api.column('Patch:name').visible(false);
      } else {
        api.column('Patch:name').visible(true);
      }
      if (
        (api.column('ParentID:name').data(0)[0] === '-1' || api.column('ParentID:name').data(0)[0] == undefined) &&
        // && rotator.find(x => x.column === 'ParentID').included == false)
        apiArr.indexOf(rotator['Parent']) == -1
      ) {
        api.column('ParentID:name').visible(false);
        api.column('ParentDesc:name').visible(false);
      } else {
        api.column('ParentID:name').visible(true);
        api.column('ParentDesc:name').visible(true);
      }
      if (
        (api.column('ProdGrpID:name').data(0)[0] === -1 || api.column('ProdGrpID:name').data(0)[0] == undefined) &&
        // && rotator.find(x => x.column === 'ProdGrpID').included == false)
        apiArr.indexOf(rotator['ProdGrpID']) == -1
      ) {
        api.column('ProdGrpID:name').visible(false);
        api.column('ProdGrpDesc:name').visible(false);
      } else {
        api.column('ProdGrpID:name').visible(true);
        api.column('ProdGrpDesc:name').visible(true);
      }
      if (
        (api.column('AssrtID:name').data(0)[0] === -1 || api.column('AssrtID:name').data(0)[0] == undefined) &&
        // && rotator.find(x => x.column === 'AssrtID').included == false)
        apiArr.indexOf(rotator['AssrtID']) == -1
      ) {
        api.column('AssrtID:name').visible(false);
        api.column('AssrtDesc:name').visible(false);
      } else {
        api.column('AssrtID:name').visible(true);
        api.column('AssrtDesc:name').visible(true);
      }
      isLastDraw = true;
      //$(".DTFC_LeftBodyLiner").attr('id', "DTFC_LeftBodyLiner");
      //document.getElementById('DTFC_LeftBodyLiner').style.overflowY = "visible";
      api.columns.adjust();
    },
    rowCallback: function (row, data, index) {},
    autoWidth: false,
    scrollX: true,
    scrollY: 400,
    scrollCollapse: true,
    paging: true,
    pagingType: 'full_numbers',
    lengthChange: true,
    orderCellsTop: true,
    colReorder: false,
    order: [
      [8, 'asc'],
      [1, 'asc'],
    ],
    lengthMenu: [20, 50, 100, 500, 1000],
    filter: true,
    initComplete: function () {
      if (DEBUG) console.log('Init Complete');
    },
    dom: 'Brtip',
    select: {
      style: 'os',
      selector: 'td:first-child',
    },
    fixedColumns: {
      leftColumns: iFixedColumns,
    },
    buttons: {
      dom: {
        button: {
          tag: 'button',
          className: 'btn waves-effect',
        },
      },
      buttons: [
        'pageLength',
        {
          extend: 'colvis',
          attr: {
            id: 'show-hide-column-groups-button',
          },
          columns: ':not(.filter)',
          text: 'Add/Remove',
          collectionLayout: 'fixed four-column',
          background: false,
          postfixButtons: [
            {
              extend: 'colvisGroup',
              text: 'Default',
              className: 'button-default',
              action: function () {
                if (DEBUG) console.log('default button click');
                ShowProcessingLoader();
                HideAllForecastColumns();
                SetDefaultColumnsView();
                CallLoadSums();
                AdjustForecastTable();
                HideProcessingLoader();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Show All',
              className: 'button-showAll',
              action: function () {
                if (DEBUG) console.log('show-all button click');
                RunWithUpdater(function () {
                  isLastDraw = false;
                  ShowAllForecastColumns();
                  isLastDraw = true;
                  CallLoadSums();
                  AdjustForecastTable();
                }, 800);
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Hide all',
              className: 'button-hideAll',
              action: function () {
                if (DEBUG) console.log('hide-all button click');

                RunWithUpdater(function () {
                  isLastDraw = false;
                  HideAllForecastColumns();
                  isLastDraw = true;
                  AdjustForecastTable();
                }, 800);
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Sales $',
              className: 'sales-dollar-group',
              action: function () {
                ToggleSalesDollarsGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Turns',
              className: 'turns-group',
              action: function () {
                ToggleTurnsGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Sales Units',
              className: 'sales-units-group',
              action: function () {
                ToggleSalesUnitsGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Retail $',
              className: 'retail-price-group',
              action: function () {
                ToggleRetailPriceGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'MP',
              className: 'mp-sales-and-margin-group',
              action: function () {
                ToggleMPGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Price Sensitivity',
              className: 'price-sensitivity-group',
              action: function () {
                TogglePriceSensGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'ASP',
              className: 'asp-group',
              action: function () {
                ToggleAspGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Margin %',
              className: 'margin-percent-group',
              action: function () {
                ToggleMarginPercGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Margin $',
              className: 'margin-dollar-group',
              action: function () {
                ToggleMarginDollGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Sell-through',
              className: 'sell-thru-group',
              action: function () {
                ToggleSellThruGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Receipt $',
              className: 'receipt-dollar-group',
              action: function () {
                ToggleRecDollarsGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Receipt Units',
              className: 'receipt-units-group',
              action: function () {
                ToggleRecUnitsGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Forecast',
              className: 'forecast-group',
              action: function () {
                ToggleForecastGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Cost',
              className: 'cost-group',
              action: function () {
                ToggleCostGroup();
              },
            },
            {
              extend: 'colvisGroup',
              text: 'Comments',
              className: 'comments-group',
              action: function () {
                ToggleCommentGroup();
              },
            },
          ],
        },
        {
          text: 'Edit Forecast',
          action: function (e, dt, node, config) {
            if (DEBUG) console.log('Custom Edit button activated');
            editor.edit().title('Edit Forecast').buttons('Update').open();
          },
        },
      ],
    },
    rowId: function (e) {
      return e.toString();
    },
    ajax: {
      url: '/Home/GetForecastTable',
      type: 'POST',
      datatype: 'json',
      async: false,
      dataSrc: function (x) {
        if (x.recordsFiltered) {
          recordsFiltered = x.recordsFiltered;
        } else if (recordsFiltered) {
          x.recordsFiltered = recordsFiltered;
        }

        if (x.recordsTotal) {
          recordsTotal = x.recordsTotal;
        } else if (recordsTotal) {
          x.recordsTotal = recordsTotal;
        }

        return x.data;
      },
      beforeSend: function () {
        ShowProcessingLoader();
      },
      complete: function (x) {
        if (x.responseJSON.sums) {
          setTimeout(function () {
            CacheTableSums(x.responseJSON.sums);
          }, 500);
        }

        if (isLastDraw) {
          //LoadSums();
          setTimeout(function () {
            if (((DTable && !DTable.isSorting) || !DTable) && x.responseJSON.sums) {
              SetSums(x.responseJSON.sums);
            }
          }, 500);
        }
        if (DEBUG) console.log('LoadSums in Complete');

        if (recordsFiltered == 0 && recordsTotal > 0) {
          NoFilteredRecords();
        }
        if (DTable !== undefined)
          setTimeout(function () {
            DTable.columns.adjust();
          }, 700);

        setTimeout(function () {
          SetTableFiltering(false);
          SetTableRotating(false);
        }, 1000);

        HideProcessingLoader();
      },
      data: function (e) {
        if (e !== undefined) {
          e.gmsvenid = parseInt(document.getElementById('GMSVenID').value);
          e.tableName = document.getElementById('TableName').value;
          e.rotator = rotator;
          e.isMM = isMerchandisingManager;
          e.isMD = isMerchandisingDirector;

          if (loadFromBookmark == false) {
            params = e;
          } else {
            e.columns = params.columns;
            //e.order = params.order;
            e.start = params.start;
            e.length = params.length;
          }

          // Set flag for is table rotating.
          if (DTable && typeof DTable.isRotating !== undefined) {
            e.isRotating = DTable.isRotating;
          } else {
            e.isRotating = true;
          }

          // Set flag for is table filtering.
          if (DTable && typeof DTable.isFiltering !== undefined) {
            e.isFiltering = DTable.isFiltering;
          } else {
            e.isFiltering = true;
          }
        }
        return e;
      },
    },
    error: function (xhr, status, error) {
      var err = eval('(' + xhr.responeText + ')');
      if (DEBUG) console.log(err.Message);
    },
    columns: [
      {
        sName: 'ForecastID',
        data: null,
        orderable: false,
        searchable: false,
        filterBox: false,
        className: 'ForecastID',
        visible: false,
        render: function (data, type, row, meta) {
          return data.ForecastID;
        },
      },
      {
        sName: 'VendorDesc',
        data: 'VendorDesc',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'ItemID',
        data: 'ItemID',
        searchable: true,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'dt-left',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'ItemDesc',
        data: 'ItemDesc',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-left',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'ItemConcat',
        data: 'ItemConcat',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-left',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'FiscalWk',
        data: 'FiscalWk',
        searchable: true,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'FiscalMo',
        data: 'FiscalMo',
        searchable: true,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'FiscalQtr',
        data: 'FiscalQtr',
        searchable: true,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'MD',
        data: 'MD',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'MM',
        data: 'MM',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'Region',
        data: 'Region',
        searchable: true,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
        render: function (data, type, row) {
          if (data == null) {
            return 'None';
          } else {
            return data;
          }
        },
      },
      {
        sName: 'District',
        data: 'District',
        searchable: true,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'Patch',
        data: 'Patch',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'ParentID',
        data: 'ParentID',
        searchable: true,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'ParentDesc',
        data: 'ParentDesc',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'ParentConcat',
        data: 'ParentConcat',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'ProdGrpID',
        data: 'ProdGrpID',
        searchable: true,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'ProdGrpDesc',
        data: 'ProdGrpDesc',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-left',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'ProdGrpConcat',
        data: 'ProdGrpConcat',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'AssrtID',
        data: 'AssrtID',
        searchable: true,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'AssrtDesc',
        data: 'AssrtDesc',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-left',
        filterBox: true,
        visible: false,
      },
      {
        sName: 'AssrtConcat',
        data: 'AssrtConcat',
        searchable: true,
        orderable: true,
        orderSequence: ['asc', 'desc'],
        className: 'dt-center',
        filterBox: true,
        visible: false,
      },
      // TEMP: Remove/Restore later.
      // { "sName": "VBUPercent", "data": "VBUPercent", "searchable": false, "orderable": true, "orderSequence": ["desc", "asc"], "className": "dt-center", "visible": false, "render": $.fn.dataTable.render.number(',', '.', 1, '', '%') },
      {
        sName: 'SalesDollars_2LY',
        data: 'SalesDollars_2LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '$'),
      },
      {
        sName: 'SalesDollars_LY',
        data: 'SalesDollars_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '$'),
      },
      {
        sName: 'SalesDollars_TY',
        data: 'SalesDollars_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '$'),
      },
      {
        sName: 'SalesDollars_Curr',
        data: 'SalesDollars_Curr',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'SalesDollars_Var',
        data: 'SalesDollars_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'SalesDollars_FR_FC',
        data: 'SalesDollars_FR_FC',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'CAGR',
        data: 'CAGR',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
      },
      {
        sName: 'Turns_LY',
        data: 'Turns_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
      },
      {
        sName: 'Turns_TY',
        data: 'Turns_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
      },
      {
        sName: 'Turns_FC',
        data: 'Turns_FC',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
      },
      {
        sName: 'Turns_Var',
        data: 'Turns_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'SalesUnits_2LY',
        data: 'SalesUnits_2LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0),
      },
      {
        sName: 'SalesUnits_LY',
        data: 'SalesUnits_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0),
      },
      {
        sName: 'SalesUnits_TY',
        data: 'SalesUnits_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0),
      },
      {
        sName: 'SalesUnits_FC',
        data: 'SalesUnits_FC',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        editField: 'SalesUnits_FC',
        render: function (data, type, row) {
          if (type === 'display') {
            var numberRenderer = $.fn.dataTable.render.number(',', '.', 0, '').display;
            return numberRenderer(data) + ' <i class="tiny material-icons">create</i>';
          }
          return data;
        },
      },
      {
        sName: 'SalesUnits_Var',
        data: 'SalesUnits_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'RetailPrice_LY',
        data: 'RetailPrice_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '$'),
      },
      {
        sName: 'RetailPrice_TY',
        data: 'RetailPrice_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '$'),
      },
      {
        sName: 'RetailPrice_FC',
        data: 'RetailPrice_FC',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        editField: 'RetailPrice_FC',
        render: function (data, type, row) {
          for (var i = 0; i < rotator.length; i++) {
            if (rotator[i].column === 'ItemID' && rotator[i].included == true) {
              // Check for ItemID
              for (var j = 0; j < rotator.length; j++) {
                if (rotator[j].column === 'Patch' && rotator[j].included == true) {
                  // Check for Patch
                  //if (rotator.find(x => x.column === 'ItemID').included == true
                  //  && rotator.find(x => x.column === 'Patch').included == true) {
                  if (DEBUG) console.log('Show retail edits');
                  var numberRenderer = $.fn.dataTable.render.number(',', '.', 2, '$').display;
                  isRetailVarEditable = true;
                  return numberRenderer(data) + ' <i class="tiny material-icons">create</i>';
                }
              }
            }
          }

          var numberRenderer = $.fn.dataTable.render.number(',', '.', 2, '$').display;
          isRetailVarEditable = false;
          return numberRenderer(data);
        },
      },
      {
        sName: 'RetailPrice_Var',
        data: 'RetailPrice_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
        },
        {
            sName: 'RetailPrice_Erosion_Rate',
            data: 'RetailPrice_Erosion_Rate',
            searchable: false,
            orderable: true,
            orderSequence: ['desc', 'asc'],
            className: 'filter dt-center',
            visible: false,
            render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
        },
      {
        sName: 'SalesDollars_FR_LY',
        data: 'SalesDollars_FR_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '$'),
      },
      {
        sName: 'SalesDollars_FR_TY',
        data: 'SalesDollars_FR_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '$'),
      },
      {
        sName: 'MarginDollars_FR_LY',
        data: 'MarginDollars_FR_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'MarginDollars_FR_TY',
        data: 'MarginDollars_FR_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'MarginDollars_FR_Var',
        data: 'MarginDollars_FR_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'PriceSensitivityPercent',
        data: 'PriceSensitivityPercent',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
      },
      {
        sName: 'PriceSensitivityImpact',
        data: 'PriceSensitivityImpact',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
      },
      {
        sName: 'Asp_LY',
        data: 'Asp_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
      },
      {
        sName: 'Asp_TY',
        data: 'Asp_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
      },
      {
        sName: 'Asp_FC',
        data: 'Asp_FC',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '', ''),
      },
      {
        sName: 'Asp_Var',
        data: 'Asp_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Margin_Percent_LY',
        data: 'Margin_Percent_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Margin_Percent_TY',
        data: 'Margin_Percent_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Margin_Percent_Curr',
        data: 'Margin_Percent_Curr',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Margin_Percent_Var',
        data: 'Margin_Percent_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Margin_Percent_FR',
        data: 'Margin_Percent_FR',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Margin_Dollars_LY',
        data: 'Margin_Dollars_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'Margin_Dollars_TY',
        data: 'Margin_Dollars_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'Margin_Dollars_Var_Curr',
        data: 'Margin_Dollars_Var_Curr',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Margin_Dollars_Curr',
        data: 'Margin_Dollars_Curr',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'Margin_Dollars_FR',
        data: 'Margin_Dollars_FR',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '$'),
      },
      {
        sName: 'SellThru_LY',
        data: 'SellThru_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'SellThru_TY',
        data: 'SellThru_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'ReceiptDollars_LY',
        data: 'ReceiptDollars_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'ReceiptDollars_TY',
        data: 'ReceiptDollars_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'ReceiptUnits_LY',
        data: 'ReceiptUnits_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0).display,
      },
      {
        sName: 'ReceiptUnits_TY',
        data: 'ReceiptUnits_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0).display,
      },
      {
        sName: 'Dollars_FC_DL',
        data: 'Dollars_FC_DL',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'Dollars_FC_LOW',
        data: 'Dollars_FC_LOW',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'Dollars_FC_Vendor',
        data: 'Dollars_FC_Vendor',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      {
        sName: 'Units_FC_DL',
        data: 'Units_FC_DL',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0).display,
      },
      {
        sName: 'Units_FC_LOW',
        data: 'Units_FC_LOW',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0),
      },
      {
        sName: 'Units_FC_Vendor',
        data: 'Units_FC_Vendor',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 0).display,
      },
      {
        sName: 'Dollars_FC_DL_Var',
        data: 'Dollars_FC_DL_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Dollars_FC_LOW_Var',
        data: 'Dollars_FC_LOW_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Dollars_FC_Vendor_Var',
        data: 'Dollars_FC_Vendor_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Units_FC_DL_Var',
        data: 'Units_FC_DL_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Units_FC_LOW_Var',
        data: 'Units_FC_LOW_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        editField: 'Units_FC_LOW_Var',
        render: function (data, type, row) {
          if (isMerchandisingDirector == true) {
            var numberRenderer = $.fn.dataTable.render.number(',', '.', 1, '', '%').display;
            return numberRenderer(data) + ' <i class="tiny material-icons">create</i>';
          } else if (type === 'display') {
            var numberRenderer = $.fn.dataTable.render.number(',', '.', 1, '', '%').display;
            return numberRenderer(data);
          } else {
            return data;
          }
        },
      },
      {
        sName: 'Units_FC_Vendor_Var',
        data: 'Units_FC_Vendor_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Cost_LY',
        data: 'Cost_LY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '$'),
      },
      {
        sName: 'Cost_TY',
        data: 'Cost_TY',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '$'),
      },
      {
        sName: 'Cost_FC',
        data: 'Cost_FC',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 2, '$'),
      },
      {
        sName: 'Cost_Var',
        data: 'Cost_Var',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'MM_Comments',
        data: 'MM_Comments',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        editField: 'MM_Comments',
        render: function (data, type, row) {
          if (
            (CheckMMCommentState() === 'Valid' && isMerchandisingManager == true && isMerchandisingDirector == false) ||
            IsUser('amyhollinger') ||
            IsUser('shannawright')
          ) {
            editor.enable('MM_Comments');
            isMMComment = true;
            if (data.length <= 30) return data.substr(0, 30) + '<a href="#" title="' + data + '"></a>' + ' <i class="tiny material-icons">create</i>';
            else {
              var esc = function (t) {
                return t.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
              };
              return data.substr(0, 30) + '<a href="#" title="' + esc(data) + '"> ... </a>' + ' <i class="tiny material-icons">create</i>';
            }
          } else if (
            CheckMMCommentState() === 'MissingColumns' &&
            (isMerchandisingManager == true || parseInt(document.getElementById('GMSVenID').value) === 0)
          ) {
            editor.disable('MM_Comments');
            isMMComment = false;
            data = 'Must rotate on Item, LGM, Vendor';
            return data;
          } else if (CheckMMCommentState() === 'Invalid') {
            editor.disable('MM_Comments');
            isMMComment = false;
            if (data.length <= 30) return data.substr(0, 30) + '<a href="#" title="' + data + '"></a>';
            else {
              var esc = function (t) {
                return t.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
              };
              return data.substr(0, 30) + '<a href="#" title="' + esc(data) + '"> ... </a>';
            }
          } else if (CheckVendorCommentState() === 'MissingColumns') {
            editor.disable('MM_Comments');
            isMMComment = false;
            data = 'Must rotate on Item, LGM';
            return data;
          } else {
            isMMComment = false;
            editor.disable('MM_Comments');
            if (data.length <= 30) return data.substr(0, 30) + '<a href="#" title="' + data + '"></a>';
            else {
              var esc = function (t) {
                return t.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
              };
              return data.substr(0, 30) + '<a href="#" title="' + esc(data) + '"> ... </a>';
            }
          }
        },
      },
      {
        sName: 'Vendor_Comments',
        data: 'Vendor_Comments',
        searchable: false,
        orderable: true,
        orderSequence: ['desc', 'asc'],
        className: 'filter dt-center',
        visible: false,
        editField: 'Vendor_Comments',
        render: function (data, type, row) {
          if (CheckVendorCommentState() === 'Valid' && editVendorComment == true) {
            editor.enable('Vendor_Comments');
            isVendorComment = true;
            //$.fn.dataTable.render.ellipsis(17, true);
            if (data.length <= 30) return data.substr(0, 30) + '<a href="#" title="' + data + '"></a>' + ' <i class="tiny material-icons">create</i>';
            else {
              var esc = function (t) {
                return t.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
              };
              return data.substr(0, 30) + '<a href="#" title="' + esc(data) + '"> ... </a>' + ' <i class="tiny material-icons">create</i>';
            }
          } else if (CheckVendorCommentState() === 'MissingColumns' && editVendorComment == true) {
            editor.disable('Vendor_Comments');
            isVendorComment = true;
            data = parseInt(document.getElementById('GMSVenID').value) === 0 ? 'Must rotate on Item, LGM, Vendor' : 'Must rotate on Item, LGM';
            return data;
          } else if ((CheckVendorCommentState() === 'Valid' || CheckVendorCommentState() === 'MissingColumns') && editVendorComment == false) {
            editor.disable('Vendor_Comments');
            isVendorComment = false;
            data = 'Must rotate on Item, LGM, Vendor';
            return data;
          } else if (editVendorComment == false) {
            editor.disable('Vendor_Comments');
            isVendorComment = false;
            if (data.length <= 30) return data.substr(0, 30) + '<a href="#" title="' + data + '"></a>';
            else {
              var esc = function (t) {
                return t.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
              };
              return data.substr(0, 30) + '<a href="#" title="' + esc(data) + '"> ... </a>';
            }
          } else {
            isVendorComment = false;
            editor.disable('Vendor_Comments');
            if (data.length <= 30) return data.substr(0, 30) + '<a href="#" title="' + data + '"></a>';
            else {
              var esc = function (t) {
                return t.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
              };
              return data.substr(0, 30) + '<a href="#" title="' + esc(data) + '"> ... </a>';
            }
          }
        },
      },
    ],
  });

  CacheTableSums([]);

  $('#ForecastTable').on('column-visibility.dt', function (e, settings, column, state) {
    if (DEBUG) console.log('Column ' + column + ' has changed to ' + (state ? 'visible' : 'hidden'));
  });

  $.fn.dataTable.ext.errMode = 'throw';
}

function LoadUnitsSummaryTable() {
  VUTable = $('#units-summary').DataTable({
    processing: true,
    language: {
      zeroRecords: 'No rows match current filters',
      processing: "<img src='/Content/Images/updating.gif' />",
    },
    serverSide: true,
    orderMulti: true,
    Paging: false,
    Filter: false,
    autoWidth: false,
    sortable: false,
    dom: '',
    background: false,
    ajax: {
      url: '/Home/GetUnitSummaryTotals',
      type: 'POST',
      dataType: 'json',
      async: true,
      dataSrc: 'data',
      data: function (e) {
        e.GMSVenID = parseInt(document.getElementById('GMSVenID').value);
        e.tableName = document.getElementById('TableName').value;
        //e.columns = params.columns;
      },
    },
    columns: [
      { sName: 'Forecast', className: 'filter dt-center', defaultContent: '0', orderable: false },
      { sName: 'Actual', className: 'filter dt-center', defaultContent: '0', orderable: false, render: $.fn.dataTable.render.number(',', '.', 0) },
      { sName: 'FC', className: 'filter dt-center', defaultContent: '0', orderable: false, render: $.fn.dataTable.render.number(',', '.', 0) },
      {
        sName: 'Var',
        className: 'filter dt-center',
        defaultContent: '0',
        orderable: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
    ],
  });
}

function LoadDollarSummaryTable() {
  DolTable = $('#dollar-summary').DataTable({
    processing: true,
    language: {
      zeroRecords: 'No rows match current filters',
      processing: "<img src='/Content/Images/updating.gif' />",
    },
    serverSide: true,
    orderMulti: true,
    Paging: false,
    Filter: false,
    autoWidth: false,
    sortable: false,
    dom: '',
    background: false,
    ajax: {
      url: '/Home/GetDollarSummaryTotals',
      type: 'POST',
      dataType: 'json',
      async: true,
      dataSrc: 'data',
      data: function (e) {
        e.GMSVenID = parseInt(document.getElementById('GMSVenID').value);

        e.tableName = document.getElementById('TableName').value;
        //e.columns = params.columns;
      },
    },
    columns: [
      { sName: 'Forecast', className: 'filter dt-center', defaultContent: '0', orderable: false },
      {
        sName: 'Actual',
        className: 'filter dt-center',
        defaultContent: '0',
        orderable: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      { sName: 'FC', className: 'filter dt-center', defaultContent: '0', orderable: false, render: $.fn.dataTable.render.number(',', '.', 0, '$') },
      {
        sName: 'Var',
        className: 'filter dt-center',
        defaultContent: '0',
        orderable: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
    ],
  });
}

function LoadMarginPercentSummaryTable() {
  MarPerTable = $('#margin-percent-summary').DataTable({
    processing: true,
    language: {
      zeroRecords: 'No rows match current filters',
      processing: "<img src='/Content/Images/updating.gif' />",
    },
    serverSide: true,
    orderMulti: true,
    Paging: false,
    Filter: false,
    autoWidth: false,
    sortable: false,
    dom: '',
    background: false,
    ajax: {
      url: '/Home/GetMarginPercentSummaryTotals',
      type: 'POST',
      dataType: 'json',
      async: true,
      dataSrc: 'data',
      data: function (e) {
        e.GMSVenID = parseInt(document.getElementById('GMSVenID').value);
        e.tableName = document.getElementById('TableName').value;
        //e.columns = params.columns;
      },
    },
    columns: [
      {
        sName: 'Actual',
        className: 'filter dt-center',
        defaultContent: '0',
        orderable: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'FC',
        className: 'filter dt-center',
        defaultContent: '0',
        orderable: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
      {
        sName: 'Var',
        className: 'filter dt-center',
        defaultContent: '0',
        orderable: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
    ],
  });
}

function LoadMarginDollarSummaryTable() {
  MarDolTable = $('#margin-dollar-summary').DataTable({
    processing: true,
    language: {
      zeroRecords: 'No rows match current filters',
      processing: "<img src='/Content/Images/updating.gif' />",
    },
    serverSide: true,
    orderMulti: true,
    Paging: false,
    Filter: false,
    autoWidth: false,
    sortable: false,
    dom: '',
    background: false,
    ajax: {
      url: '/Home/GetMarginDollarSummaryTotals',
      type: 'POST',
      dataType: 'json',
      async: true,
      dataSrc: 'data',
      data: function (e) {
        e.GMSVenID = parseInt(document.getElementById('GMSVenID').value);
        e.tableName = document.getElementById('TableName').value;
        //e.columns = params.columns;
      },
    },
    columns: [
      {
        sName: 'Actual',
        className: 'filter dt-center',
        defaultContent: '0',
        orderable: false,
        render: $.fn.dataTable.render.number(',', '.', 0, '$'),
      },
      { sName: 'FC', className: 'filter dt-center', defaultContent: '0', orderable: false, render: $.fn.dataTable.render.number(',', '.', 0, '$') },
      {
        sName: 'Var',
        className: 'filter dt-center',
        defaultContent: '0',
        orderable: false,
        render: $.fn.dataTable.render.number(',', '.', 1, '', '%'),
      },
    ],
  });
}

function LoadDateUpdated() {
  $.ajax({
    url: '/Home/GetUpdatedDates',
    type: 'POST',
    dataType: 'json',
    async: true,
    dataSrc: 'data',
    success: function (x) {
      $('#date-updated-through').html('Updated Through ' + '<br/>' + x.data[0][0].toString() + ' - ' + x.data[0][1].toString());
    },
  });
}

//Populate the summary row in the datatable.
function LoadSums(redrawTable) {
  if (DEBUG) console.log('LoadSums()');

  if (isFirstTimeLoading == true) return;
  //params = jQuery.extend(true, {}, tempParams);
  var x = $.ajax({
    url: '/Home/GetSums',
    data: params,
    async: false,
    dataType: 'json',
    method: 'POST',
    success: function (x) {},
    error: function (ts) {
      if (DEBUG) console.log(ts.responseText);
    },
  }).responseJSON;

  if (x !== undefined && x.data.length > 0) {
    CacheTableSums(x.data);
    SetSums(x.data);
  }
}

function SetSums(data) {
  if (data !== undefined && data.length > 0) {
    $('#sums_salesdollars_2ly').text(formatCurrency(data[0].SalesDollars_2LY));
    $('#sums_salesdollars_ly').text(formatCurrency(data[0].SalesDollars_LY));
    $('#sums_salesdollars_ty').text(formatCurrency(data[0].SalesDollars_TY));
    $('#sums_salesdollars_fy_asp').text(formatCurrency(data[0].SalesDollars_Curr));
    $('#sums_salesdollars_var').text(formatDecimalPercent(data[0].SalesDollars_Var));
    $('#sums_salesdollars_fr').text(formatCurrency(data[0].SalesDollars_FR_FC));
    $('#sums_salesdollars_cagr').text(data[0].CAGR);
    $('#sums_turns_ly').text(data[0].Turns_LY);
    $('#sums_turns_ty').text(data[0].Turns_TY);
    $('#sums_turns_fy').text(data[0].Turns_FC);
    $('#sums_turns_var').text(formatDecimalPercent(data[0].Turns_Var));
    $('#sums_salesunits_2ly').text(formatNumberComma(data[0].SalesUnits_2LY));
    $('#sums_salesunits_ly').text(formatNumberComma(data[0].SalesUnits_LY));
    $('#sums_salesunits_ty').text(formatNumberComma(data[0].SalesUnits_TY));
    $('#sums_salesunits_fy').text(formatNumberComma(data[0].SalesUnits_FC));
    $('#sums_salesunits_var').text(formatDecimalPercent(data[0].SalesUnits_Var));
    $('#sums_retailprice_ly').text(formatCurrency(data[0].RetailPrice_LY));
    $('#sums_retailprice_ty').text(formatCurrency(data[0].RetailPrice_TY));
    $('#sums_retailprice_fy').text(formatCurrency(data[0].RetailPrice_FC));
    $('#sums_retailprice_var').text(formatDecimalPercent(data[0].RetailPrice_Var));
    $('#sums_retailprice_erosion_rate').text(formatDecimalPercent(data[0].RetailPrice_Erosion_Rate));
    $('#sums_mp_salesdollars_ty').text(formatCurrency(data[0].SalesDollars_FR_TY));
    $('#sums_mp_salesdollars_ly').text(formatCurrency(data[0].SalesDollars_FR_LY));
    $('#sums_mp_margindollars_ty').text(formatCurrency(data[0].MarginDollars_FR_TY));
    $('#sums_mp_margindollars_ly').text(formatCurrency(data[0].MarginDollars_FR_LY));
    $('#sums_mp_margindollars_var').text(formatDecimalPercent(data[0].MarginDollars_FR_Var));
    $('#sums_pricesens_per').text(data[0].PriceSensitivityPercent);
    $('#sums_pricesens_is').text(data[0].PriceSensitivityImpact);
    $('#sums_asp_ly').text(data[0].Asp_LY);
    $('#sums_asp_ty').text(data[0].Asp_TY);
    $('#sums_asp_fy').text(data[0].Asp_FC);
    $('#sums_asp_var').text(formatDecimalPercent(data[0].Asp_Var));
    $('#sums_marginperc_ly').text(formatDecimalPercent(data[0].Margin_Percent_LY));
    $('#sums_marginperc_ty').text(formatDecimalPercent(data[0].Margin_Percent_TY));
    $('#sums_marginperc_fy').text(formatDecimalPercent(data[0].Margin_Percent_Curr));
    $('#sums_marginperc_var').text(formatDecimalPercent(data[0].Margin_Percent_Var));
    $('#sums_marginperc_ftr').text(formatDecimalPercent(data[0].Margin_Percent_FR));
    $('#sums_margindollars_ly').text(formatCurrency(data[0].Margin_Dollars_LY));
    $('#sums_margindollars_ty').text(formatCurrency(data[0].Margin_Dollars_TY));
    $('#sums_margindollars_fy_ret_var').text(formatDecimalPercent(data[0].Margin_Dollars_Var_Curr));
    $('#sums_margindollars_fy_asp').text(formatCurrency(data[0].Margin_Dollars_Curr));
    $('#sums_margindollars_ft_ret').text(formatCurrency(data[0].Margin_Dollars_FR));
    $('#sums_sellthru_ly').text(formatDecimalPercent(data[0].SellThru_LY));
    $('#sums_sellthru_ty').text(formatDecimalPercent(data[0].SellThru_TY));
    $('#sums_recdollars_ly').text(formatCurrency(data[0].ReceiptDollars_LY));
    $('#sums_recdollars_ty').text(formatCurrency(data[0].ReceiptDollars_TY));
    $('#sums_recunits_ly').text(formatNumberComma(data[0].ReceiptUnits_LY));
    $('#sums_recunits_ty').text(formatNumberComma(data[0].ReceiptUnits_TY));
    $('#sums_dl_fy_sales_dollars').text(formatCurrency(data[0].Dollars_FC_DL));
    $('#sums_low_fy_sales_dollars').text(formatCurrency(data[0].Dollars_FC_LOW));
    $('#sums_vend_fy_sales_dollars').text(formatCurrency(data[0].Dollars_FC_Vendor));
    $('#sums_dl_fy_sales_units').text(formatNumberComma(data[0].Units_FC_DL));
    $('#sums_low_fy_sales_units').text(formatNumberComma(data[0].Units_FC_LOW));
    $('#sums_vend_fy_sales_units').text(formatNumberComma(data[0].Units_FC_Vendor));
    $('#sums_dl_fy_sales_dollars_var').text(formatDecimalPercent(data[0].Dollars_FC_DL_Var));
    $('#sums_low_fy_sales_dollars_var').text(formatDecimalPercent(data[0].Dollars_FC_LOW_Var));
    $('#sums_vend_fy_sales_dollars_var').text(formatDecimalPercent(data[0].Dollars_FC_Vendor_Var));
    $('#sums_dl_fy_sales_units_var').text(formatDecimalPercent(data[0].Units_FC_DL_Var));
    $('#sums_low_fy_sales_units_var').text(formatDecimalPercent(data[0].Units_FC_LOW_Var));
    $('#sums_vend_fy_sales_units_var').text(formatDecimalPercent(data[0].Units_FC_Vendor_Var));
    $('#sums_cost_ly').text(data[0].Cost_LY);
    $('#sums_cost_ty').text(data[0].Cost_TY);
    $('#sums_cost_fy').text(data[0].Cost_FC);
    $('#sums_cost_var').text(formatDecimalPercent(data[0].Cost_Var));
    $('#mm_comments').text('N/A');
    $('#vendor_comments').text('N/A');
  }

  AdjustForecastTable();
}

function UpdateEditedCells(mode) {
  if (DEBUG) console.log('UpdateEditedCells(' + mode + ')');
  //var e = params;
  var e = jQuery.extend(true, {}, params);
  var len = DTable.order().length;
  e.order = [];
  for (i = 0; i < len; i++) {
    var obj = { column: DTable.order()[i][0], dir: DTable.order()[i][1] };
    e.order.push(obj);
  }

  e.rotator = rotator;

  if (1 == 1) {
    var i = 0;
    var j = 0;
    for (i = 0; i < rotator.length; i++) {
      if (rotator[i].included === true) {
        // Look in the rotator for included columns
        for (j = 0; j < e.columns.length; j++) {
          // Put the data for the included columns into the where clause
          if (e.columns[j].name == rotator[i].column && rotator[i].included === true) {
            // We have to make a call for all the cells in the available units column
            // so get the replenIds for all of the rows in the table.
            var rowid = [];
            var name = rotator[i].column;
            if (mode == 'main') {
              DTable.draw();
              return;
            } else if (mode == 'inline') {
              rowid.push(DTable.cell(lastEditedCell._DT_CellIndex.row, name + ':name').data());
              console.log(rowid.push(DTable.cell(lastEditedCell._DT_CellIndex.row, name + ':name').data()));
            }
            var arr = RemoveDuplicatesFromArray(rowid);
            var ids = arr.join(',');
            e.columns[j].search.value = ids; //DTable.cell(lastEditedCell._DT_CellIndex.row, j).data(); // Cell(row, column)
          }
        }
      }
    }
  }

  // First make a call for the data, passing the replenIds
  $.ajax({
    url: '/Home/GetUpdatedCellsByForecastIDs',
    async: false,
    dataType: 'json',
    type: 'POST',
    data: e,
    success: function (x) {
      // Use the return data to update those cells based on the replenIds.
      // Order is handled at the DataProvider level.
      if (DEBUG) console.log(x);
      for (i = 0; i < DTable.rows().count(); i++) {
        if (DEBUG) console.log(i);
        var r = DTable.cell(i, 'ForecastID:name').data().ForecastID;
        for (var j = 0; j < x.data.length; j++) {
          var s = x.data[j].ForecastID;
          if (DEBUG) console.log(s + '==' + r);
          if (s == r) {
            if (DEBUG) console.log('s == r');
            DTable.cell(i, 'SalesDollars_FR_FC:name').data(x.data[j].SalesDollars_FR_FC);
            DTable.cell(i, 'SalesDollars_Curr:name').data(x.data[j].SalesDollars_Curr);
            DTable.cell(i, 'SalesDollars_Var:name').data(x.data[j].SalesDollars_Var);

            DTable.cell(i, 'SalesUnits_Var:name').data(x.data[j].SalesUnits_Var);
            DTable.cell(i, 'SalesUnits_FC:name').data(x.data[j].SalesUnits_FC);

            DTable.cell(i, 'Asp_FC:name').data(x.data[j].Asp_FC);
            DTable.cell(i, 'Asp_Var:name').data(x.data[j].Asp_Var);

            DTable.cell(i, 'Cost_FC:name').data(x.data[j].Cost_FC);
            DTable.cell(i, 'Cost_Var:name').data(x.data[j].Cost_Var);

            DTable.cell(i, 'RetailPrice_FC:name').data(x.data[j].RetailPrice_FC);
            DTable.cell(i, 'RetailPrice_Var:name').data(x.data[j].RetailPrice_Var);
            DTable.cell(i, 'RetailPrice_Erosion_Rate:name').data(x.data[j].RetailPrice_Erosion_Rate);

            DTable.cell(i, 'MarginDollars_FR_Var:name').data(x.data[j].MarginDollars_FR_Var);

            DTable.cell(i, 'Margin_Dollars_Curr:name').data(x.data[j].Margin_Dollars_Curr);
            DTable.cell(i, 'Margin_Dollars_FR:name').data(x.data[j].Margin_Dollars_FR);
            DTable.cell(i, 'Margin_Dollars_Var_Curr:name').data(x.data[j].Margin_Dollars_Var_Curr);

            DTable.cell(i, 'Margin_Percent_Curr:name').data(x.data[j].Margin_Percent_Curr);
            DTable.cell(i, 'Margin_Percent_FR:name').data(x.data[j].Margin_Percent_FR);
            DTable.cell(i, 'Margin_Percent_Var:name').data(x.data[j].Margin_Percent_Var);

            DTable.cell(i, 'Turns_FC:name').data(x.data[j].Turns_FC);
            DTable.cell(i, 'Turns_Var:name').data(x.data[j].Turns_Var);

            DTable.cell(i, 'Dollars_FC_DL:name').data(x.data[j].Dollars_FC_DL);
            DTable.cell(i, 'Dollars_FC_LOW:name').data(x.data[j].Dollars_FC_LOW);
            DTable.cell(i, 'Dollars_FC_Vendor:name').data(x.data[j].Dollars_FC_Vendor);

            DTable.cell(i, 'Dollars_FC_DL_Var:name').data(x.data[j].Dollars_FC_DL_Var);
            DTable.cell(i, 'Dollars_FC_LOW_Var:name').data(x.data[j].Dollars_FC_LOW_Var);
            DTable.cell(i, 'Dollars_FC_Vendor_Var:name').data(x.data[j].Dollars_FC_Vendor_Var);

            DTable.cell(i, 'Units_FC_LOW:name').data(x.data[j].Units_FC_LOW);
            DTable.cell(i, 'Units_FC_Vendor:name').data(x.data[j].Units_FC_Vendor);

            DTable.cell(i, 'Units_FC_LOW_Var:name').data(x.data[j].Units_FC_LOW_Var);
            DTable.cell(i, 'Units_FC_Vendor_Var:name').data(x.data[j].Units_FC_Vendor_Var);

            DTable.cell(i, 'MM_Comments:name').data(x.data[j].MM_Comments);
            DTable.cell(i, 'Vendor_Comments:name').data(x.data[j].Vendor_Comments);

            return;
          }
        }
      }

      // DataTables holds cached information about the contents of each cell in
      // the table to increase performance of table operations such as ordering and searching.
      // We have no use for cached data since all of our operations are done server side. To prevent
      // the cell data from reverting to it's pre-updated value on the front end, we have to invalidate
      // the cache in those cells.
      DTable.rows().every(function (rowIdx, tableLoop, rowLoop) {
        this.row(rowIdx).invalidate();
      });
    },
    error: function (xhr, status, error) {
      var err = eval('(' + xhr.responseText + ')');
      HandleError(err.Message);
    },
  });
}

/*=============================================>>>>>
            = Helper Functions =
===============================================>>>>>*/

//---------------- Begin Bookmarks ------------------//

//Clear the filters/bookmarks
function ClearBookmark() {
  DTable.state.clear();
  VUTable.state.clear();
  DolTable.state.clear();
  window.location.reload();
}

/**
 * Function to close all expanded collapsibles in the download modal
 * */
function CloseDownloadTemplateHeaders() {
  $('#downloadModal')
    .find('li.collection-item.avatar.active')
    .each(function (key, val) {
      $(val).removeClass('active');
    });
  $('#downloadModal')
    .find('a.collapsible-header.waves-effect.waves-light.active')
    .each(function (key, val) {
      $(val).removeClass('active');
    });
  $('#downloadModal')
    .find('div.collapsible-body')
    .each(function (key, val) {
      $(val).hide();
    });
}

//Create a bookmark
function CreateBookmark() {
  bookmarkName = document.getElementById('BookmarkName').value;
  state = localStorage.getItem('DLState');

  state = Bookmark(state, 'col-reorder-to-name');
  if (state) {
    state = PatchParentColumns(state);
  }

  $.ajax({
    method: 'POST',
    url: '/Home/CreateBookmark',
    dataType: 'JSON',
    data: {
      gmsvenid: parseInt(document.getElementById('GMSVenID').value),
      username: document.getElementById('Username').value,
      bookmarkName: bookmarkName,
      state: state,
    },
    success: function () {
      alert('The bookmark ' + bookmarkName + ' has been created.');
    },
    error: function () {
      alert('The bookmark ' + bookmarkName + ' already exists.  Please rename.');
    },
  });
}

/**
 * Function to update an existing bookmark.
 * @param {any} bookmarkName The name of the bookmark to update.
 * @param {any} state The new state string to insert.
 */
function UpdateBookmark(bookmarkName, state) {
  state = Bookmark(state, 'col-reorder-to-name');

  $.ajax({
    method: 'POST',
    url: '/Home/UpdateBookmark',
    dataType: 'JSON',
    data: {
      gmsvenid: parseInt(document.getElementById('GMSVenID').value),
      username: document.getElementById('Username').value,
      bookmarkName: bookmarkName,
      state: state,
    },
    success: function () {
      console.log('The bookmark ' + bookmarkName + ' has been updated');
    },
    error: function () {
      console.log('The bookmark ' + bookmarkName + " couldn't be updated.");
    },
  });
}

//Delete Bookmark
function DeleteBookmark() {
  var bookmarkToDelete = $('#bookmarkList option:selected').text();

  $.ajax({
    type: 'POST',
    url: '/Home/DeleteBookmark',
    data: {
      gmsvenid: parseInt(document.getElementById('GMSVenID').value),
      username: document.getElementById('Username').value,
      bookmarkName: bookmarkToDelete,
    },
    dataType: 'JSON',
    success: function (x) {},
  });

  alert('The bookmark ' + bookmarkToDelete + ' has been deleted');
}

/**
 * Gets the associated datatable column name from a filter id name like
 * filter-body-md or just md
 * @param {any} filterName Could be a filter id or just the last section that describes the
 * filter category like vendor, md, mm, item, etc...
 */
function GetFilterColumnName(filterName) {
  // If filterName is not separated by the '-' delimiter then it's already
  // a single name so we assign filterName to tempName
  var tempName = GetFilterNameFromId(filterName);

  switch (tempName) {
    case 'vendor':
      return 'VendorDesc';
    case 'md':
      return 'MD';
    case 'mm':
      return 'MM';
    case 'region':
      return 'Region';
    case 'district':
      return 'District';
    case 'patch':
      return 'Patch';
    case 'prodgrp':
      return 'ProdGrpConcat';
    case 'assrt':
      return 'AssrtConcat';
    case 'item':
      return 'ItemConcat';
    case 'parent':
      return 'ParentConcat';
    case 'fiscalwk':
      return 'FiscalWk';
    case 'fiscalqtr':
      return 'FiscalQtr';
    case 'fiscalmo':
      return 'FiscalMo';
    default:
      return '';
  }
}

/**
 * If filterName is not separated by the '-' delimiter then it's already
 * a single name so we assign filterName to tempName
 * @param {any} filterId Could be a name or an id like such: filter-body-item, filter-header-fiscalwk, etc...
 */
function GetFilterNameFromId(filterId) {
  return filterId.split('-')[filterId.split('-').length - 1] || filterId;
}

function IsUser(username) {
  return document.getElementById('Username').value === username;
}

//Load Bookmark list
function LoadBookmark() {
  bookmarkName = $('#bookmarkList option:selected').text();
  SetTableFiltering(true);

  $.ajax({
    type: 'POST',
    url: '/Home/GetBookmark',
    data: {
      gmsvenid: parseInt(document.getElementById('GMSVenID').value),
      username: document.getElementById('Username').value,
      bookmarkName: bookmarkName,
    },
    dataType: 'JSON',
    success: function (x) {
      loadFromBookmark = true;

      // Get the new state of the table for comparison
      var newState = localStorage.getItem('DLState');

      // Check if the two states differ in column count and or order
      var isStateModified = IsTableStateModified(x.data.State, newState);

      // If the two states differ then update the bookmark state
      if (isStateModified) {
        var parsedBookmark = PatchParentColumns(JSON.parse(x.data.State));
        var bookmarkState = JSON.stringify(parsedBookmark);
        var tempState = Bookmark(bookmarkState, 'update-columns', newState);
        state = JSON.parse(tempState);

        // If both states match up then save it
        isStateModified = IsTableStateModified(tempState, newState);

        if (isStateModified === false) {
          UpdateBookmark(bookmarkName, tempState);
        }
      } else {
        state = JSON.parse(x.data.State);
        state = PatchParentColumns(state);
      }

      params = state;
      localStorage.setItem('DLState', JSON.stringify(state));

      //Reasignment of gmsvenid was causing issues because
      //after parsing x.data.State gmsvenid would zero out
      //since State doesn't contain gmsvenid.
      //gmsvenid = parseInt(state.gmsvenid);

      tableName = state.tableName;
      rotator = state.rotator;
      PopulateRotator();

      //Rows per page
      params.length = state.length;
      DTable.page.len(state.length);

      //Search and visible filters
      for (i = 0; i < state.columns.length; i++) {
        var columnName = state.columns[i].name;
        var columnValue = state.columns[i].search.search;
        DTable.columns(columnName + ':name').search(columnValue);
      }

      //Visible filters
      for (i = 0; i < state.columns.length; i++) {
        DTable.column(i).visible(state.columns[i].visible, false);
      }

      //Column position
      //DTable.colReorder.order(state.ColReorder);

      //Order filters
      DTable.isRotating = true;
      DTable.order(state.order).draw();
      //DTable.columns.adjust(); Commented out due to AdjustForecastTable in complete section below.

      loadFromBookmark = false;
      UnselectAllFiscalBoxes(); //needs to be before ClearFilterButtons()
      ClearFilterButtons();
      SetFilterButtons();
      UpdateFiscalBoxes();
      UpdateColumnSortFilter(filter_columnsort);
    },
    complete: function () {
      //Close the bookmark Modal
      $('#bookmarkModal').modal('close');
      AdjustForecastTable();
    },
    error: function (ts) {
      if (DEBUG) console.log(ts.responseText);
    },
  });
}

//---------------- End Bookmarks ------------------//

//Modal that will display a message to indicate user filtered out of their dataset.
function NoFilteredRecords() {
  $('#noRecordsModal').modal();
  $('#noRecordsModal').modal('open');
}

//Modal that will display message stating no records are being returned in the data table.
function NoTotalRecords() {
  $('#noRecordsTotalModal').modal();
  $('#noRecordsTotalModal').modal('open');
}

//Check for the state of the rotator to restrict ability to edit comments.
function CheckMMCommentState() {
  var mmCommentState = '';
  var rotatorArr = [];
  for (var i = 0; i < rotator.length; i++) {
    if (rotator[i].included == true) {
      rotatorArr.push(rotator[i].column);
    }
  }
  for (var i = 0; i < rotator.length; i++) {
    //if (rotator.find(x => x.column === 'ItemID').included == true && rotator.find(x => x.column === 'MM').included == true && rotator.find(x => x.column === 'VendorDesc').included == true) {
    if (rotatorArr.indexOf('ItemID') >= 0 && rotatorArr.indexOf('MM') >= 0 && rotatorArr.indexOf('VendorDesc') >= 0) {
      if (rotator[i].column === 'ItemID' || rotator[i].column === 'MM' || rotator[i].column === 'VendorDesc') {
        //Do Nothing but don't break the for loop.
      } else {
        if (rotator[i].included == false) {
          //all non Item, MM, VendDesc columns are set to false.  Valid to edit.
          mmCommentState = 'Valid';
        } else {
          mmCommentState = 'Invalid';
          break;
        }
      }
    } else {
      //Not all 3 of Item, MM, VendorDesc are selected.  Not valid state.
      mmCommentState = 'MissingColumns';
    }
  }
  return mmCommentState;
}

//Check for the state of the rotator to restrict vendors ability to edit coments.
function CheckVendorCommentState() {
  var vendorCommentState = '';
  var rotatorArr = [];
  var gid = parseInt(document.getElementById('GMSVenID').value);

  for (var i = 0; i < rotator.length; i++) {
    if (rotator[i].included == true) {
      rotatorArr.push(rotator[i].column);
    }
  }

  for (var i = 0; i < rotator.length; i++) {
    // if (rotator.find(x => x.column === 'ItemID').included == true && rotator.find(x => x.column === 'MM').included == true) {
    if (rotatorArr.indexOf('ItemID') >= 0 && rotatorArr.indexOf('MM') >= 0 && (gid === 0 ? rotatorArr.indexOf('VendorDesc') >= 0 : true)) {
      if (rotator[i].column === 'ItemID' || rotator[i].column === 'MM' || (gid === 0 && rotatorArr.indexOf('VendorDesc') >= 0)) {
        //Do Nothing but don't break the for loop.
      } else {
        if (rotator[i].included == false) {
          //all non Item, MM, VendDesc columns are set to false.  Valid to edit.
          vendorCommentState = 'Valid';
        } else {
          vendorCommentState = 'Invalid';
          break;
        }
      }
    } else {
      //Not all 3 of Item, MM, VendorDesc are selected.  Not valid state.
      vendorCommentState = 'MissingColumns';
    }
  }
  return vendorCommentState;
}

//Loops through each checkbox in the rotator dropdown and assigns it's to rotator[].
function PopulateRotator() {
  if (DEBUG) console.log('PopulateRotator()');
  if ($.isEmptyObject(rotator)) {
    if (DEBUG) console.log('rotator object empty - using defaults');
    $('#rotator-dropdown li .rotator-checkbox').each(function () {
      if ($(this).hasClass('default-rotate')) {
        rotator.push({ column: this.value, included: true });
      } else {
        rotator.push({ column: this.value, included: false });
      }
    });
  }
  SetRotatorState();
}

//Check boxes based off values that have been saved in state.
function SetRotatorState() {
  rotator.forEach(function (element) {
    if (element.included == true) {
      var inputVar = document.querySelector('input[value=' + element.column + ']');
      $(inputVar).prop('checked', true);
    } else if (element.included == false) {
      var inputVar = document.querySelector('input[value=' + element.column + ']');
      $(inputVar).prop('checked', false);
    }
  });
}

/**
 * Set the table as rotating if the user initiated a rotation.
 * @param {boolean} rotating True if the user initiated a rotation. False to reset it.
 */
function SetTableRotating(rotating) {
  if (DTable) {
    if (DTable.isRotating) {
      DTable.isRotating = rotating;
    } else {
      DTable['isRotating'] = rotating;
    }
  }
}

/**
 * Set the state of table has initiated a filter or not.
 * @param {boolean} filtering A boolean of true for the table is requesting new data based filters
 * and false to reset the filter state.
 */
function SetTableFiltering(filtering) {
  if (DTable) {
    if (DTable.isFiltering) {
      DTable.isFiltering = filtering;
    } else {
      DTable['isFiltering'] = filtering;
    }
  }
}

/**
 * Cache the sums for use when showing or hiding columns to avoid requerying them.
 * @param {[{}]} sums
 */
function CacheTableSums(sums) {
  if (DTable) {
    if (DTable.sums) {
      DTable.sums = sums;
    } else {
      DTable['sums'] = sums;
    }
  }
}

/**
 * Clears the data and file name from a file form.
 * @param {string} formId The id of the form you want to clear.
 */
function ClearForm(formId) {
  if (!formId) {
    return;
  }

  if (formId[0] !== '#') {
    formId = '#' + formId;
  }

  $(formId).wrap('<form>').closest('form').get(0).reset();
  $(formId).unwrap();
}

/**
 * Clears all the file names in the uploads modal.
 */
function DisableUploadButtons() {
  $('.dl-mat-card.upload-modal-section .upload-button').each(function (_, d) {
    var isDisabled = $(d).hasClass('disabled');
    if (!isDisabled) {
      $(d).addClass('disabled');
    }
  });
}

/**
 * Clears all the file names in the uploads modal.
 */
function ClearUploadFileNames() {
  $('.upload-modal-section-file-name-title').each(function (_, d) {
    $(d).html('');
  });
}

/**
 * Clears all the upload file forms.
 * @param {string} excludeId Give and id of a form that you want to exclude from being cleared.
 */
function ClearUploadForms(excludeId) {
  if (excludeId) {
    excludeId = excludeId[0] === '#' ? excludeId.replace('#', '') : excludeId;
  }

  $('.upload-modal-file-form').each(function (_, form) {
    if (excludeId) {
      if (form.id && form.id !== excludeId) {
        $(form).wrap('<form>').closest('form').get(0).reset();
        $(form).unwrap();
      }
    } else {
      $(form).wrap('<form>').closest('form').get(0).reset();
      $(form).unwrap();
    }
  });
}

/**
 * Function to reset the rotator back to its original state.
 */
function ResetRotatorToState() {
  var state = JSON.parse(localStorage.getItem('DLState'));
  if (state) {
    state = PatchParentColumns(state);
  }
  if (state !== undefined || state !== null) {
    rotator = state.rotator;
    SetRotatorState();
  }
}

//Move the rotator so it looks like it's attached to the table.
function MoveRotatorButton() {
  $('#rotate-btn.dropdown-button.btn').prependTo('div.dt-buttons');
  $('#rotator-dropdown.dropdown-content').appendTo('#rotate-btn.dropdown-button.btn');
  //$('div.dt-buttons');
}

function MoveFixedColumnSwitch() {
  $('#fixed-column-switch.switch').prependTo('div.dt-buttons');
}

/**
 * Adds a filter to the an array of filters if the array doesn't already contain the filter.
 * The filter is set by giving the function a filter name like vendor, item, mm, md, etc...
 * @param {any} id A string name for a filter id such as vendor, item, fiscalwk, mm, etc...
 * @param {any} param The value to add to the filter
 */
function AddFilterToList(id, param, event) {
  param = param.replace(/\"/gm, '');
  var filterName = GetFilterNameFromId(id);
  var filterId = '#filter-' + filterName;
  var filter = window['filter_' + filterName];
  //Add the filter to the selected filters list

  AddFilterToFilterList(id, filter);

  //If the user is holding the Ctrl key then highlingth the selected filter
  if (event.params.originalEvent.originalEvent.ctrlKey) {
    filter_ctrl_key = true; // We need this to keep track of the Ctrl key later
    HighlightSelect2(id, event);
  } else {
    // If the user isn't holding the Ctrl key then we just add the filter and reload the table
    ShowProcessingLoader();
    if (DEBUG) console.log('line 3965');
    filter_ctrl_key = false;
    if ($.inArray(param, filter) === -1) {
      filter.push(param);
      for (var i = 0; i < filters_selected.length; i++) {
        UpdateFilter(filters_selected[i].id, filters_selected[i].filter);
        if (DEBUG) console.log('line 4093');
      }
      filters_selected.length = 0;
    }
  }
  $(filterId).val(null).trigger('change');
}

function AddBulkFilterToList(id, params, event) {
  for (i = 0; i < params.length; i++) {
    param = params[i].replace(/\"/gm, '');
    var filterName = GetFilterNameFromId(id);
    var filterId = '#filter-' + filterName;
    var filter = window['filter_' + filterName];

    //Add the filter to the selected filters list
    AddFilterToFilterList(id, filter);
    // If the user isn't holding the Ctrl key then we just add the filter and reload the table
    filter_ctrl_key = false;

    if ($.inArray(param, filter) === -1) {
      filter.push(param);
    }
  }
  for (var i = 0; i < filters_selected.length; i++) {
    UpdateFilter(filters_selected[i].id, filters_selected[i].filter);
    if (DEBUG) console.log('line 4121');
  }

  filters_selected.length = 0;

  $(filterId).val(null).trigger('change');
}

function AddBulkExcludeFilterToList(id, params, event) {
  for (i = 0; i < params.length; i++) {
    param = params[i].replace(/\"/gm, '');
    var filterName = GetFilterNameFromId(id);
    var filterId = '#filter-exclude-' + filterName;
    var filter = window['exclude_' + filterName];

    //Add the filter to the selected filters list
    AddFilterToExcludeFilterList(id, filter);
    // If the user isn't holding the Ctrl key then we just add the filter and reload the table
    filter_ctrl_key = false;

    if ($.inArray(param, filter) === -1) {
      filter.push(param);
    }
  }
  for (var i = 0; i < filters_selected.length; i++) {
    UpdateFilterExclude(filters_selected[i].id, filters_selected[i].filter);
    if (DEBUG) console.log('line 4147');
  }

  filters_selected.length = 0;
  $(filterId).val(null).trigger('change');
}

//Adds filter to exclude array
function AddFilterToExcludeList(id, param, event) {
  param = param.replace(/\"/gm, '');

  var filterName = GetFilterNameFromId(id);
  var filterId = '#filter-exclude-' + filterName;
  var filter = window['exclude_' + filterName];
  //Add the filter to the selected filters list
  AddFilterToExcludeFilterList(id, filter);

  //If the user is holding the Ctrl key then highlingth the selected filter
  if (event.params.originalEvent.originalEvent.ctrlKey) {
    filter_ctrl_key = true; // We need this to keep track of the Ctrl key later
    HighlightSelect2(id, event);
  } else {
    // If the user isn't holding the Ctrl key then we just add the filter and reload the table
    filter_ctrl_key = false;
    ShowProcessingLoader();
    if (DEBUG) console.log('line 4049');
    if ($.inArray(param, filter) === -1) {
      filter.push(param);
      //changed filters_excluded to filters_selected
      for (var i = 0; i < filters_selected.length; i++) {
        UpdateFilterExclude(filters_selected[i].id, filters_selected[i].filter);
        if (DEBUG) console.log('line 4179');
      }
      filters_selected.length = 0;
    }
  }
  $(filterId).val(null).trigger('change');
}

//Minor adjustments to the table that need to be made after it changes.
function AdjustForecastTable() {
  if (DEBUG) console.log('AdjustForecastTable()');
  DTable.columns.adjust();
  DTable.fixedColumns().relayout();
}

//Function that takes the filter list and adds a collapsible group for the applied filters to be added under.
function AddNewFilterBoxGroup(filterList, filterHeader, filterBodyId, onLoad) {
  if (DEBUG) console.log('AddNewFilterBoxGroup()');
  if (filterList.length <= 1 || onLoad == true) {
    $('#' + filterHeader).show();
    var newFilterHeader = '<div id="' + filterBodyId + '" class="collapsible-body" style="display: block;"></div></div>';
    $('#filter-box-body').append(newFilterHeader);
  }
}

//Function that takes the filter list and adds a collapsible group for the applied filters to be added under.
function AddNewFilterBoxGroupExclude(filterList, filterHeader, filterBodyId, onLoad) {
  if (DEBUG) console.log('AddNewFilterBoxGroupExclude()');
  if (filterList.length <= 1 || onLoad == true) {
    $('#' + filterHeader).show();
    var newFilterHeader = '<div id="' + filterBodyId + '" class="collapsible-body" style="display: block;"></div></div>';
    $('#filter-box-body').append(newFilterHeader);
  }
}

//Function that adds a chip with the applied filters in it.  Allows users to remove certain filters.
function AddNewFilter(param, filterBodyId, filterClass) {
  if (DEBUG) console.log('AddNewFilter()');
  $('#' + filterBodyId).append('<div class="chip ' + filterClass + '" id="filter-chip">' + param + '<i class="close material-icons">close</i></div>');
}

//Function that adds a chip with the applied filters in it.  Allows users to remove certain filters.
function AddNewFilterExclude(param, filterBodyId, filterClass) {
  if (DEBUG) console.log('AddNewFilterExclude()');
  var param2 = param;
  param2 = param2.replace('!', '');
  $('#' + filterBodyId).append(
    '<div class="chip exclude ' + filterClass + '" id="filter-chip-exclude">' + param2 + '<i class="close material-icons">close</i></div>'
  );
  //$('#' + 'vend-chip').append('<div class="chip">' + param + '<i class="close material-icons">close</i></div>');
}

//Hide all of the filter box categories on page load.  Only display when there is a filter added.
function HideAllFilterCats() {
  $('#filter-head-vendor').hide();
  $('#filter-head-md').hide();
  $('#filter-head-mm').hide();
  $('#filter-head-region').hide();
  $('#filter-head-district').hide();
  $('#filter-head-patch').hide();
  $('#filter-head-parent').hide();
  $('#filter-head-item').hide();
  $('#filter-head-prodgrp').hide();
  $('#filter-head-assrt').hide();
  $('#filter-head-item').hide();
  $('#filter-head-fiscalwk').hide();
  $('#filter-head-fiscalmo').hide();
  $('#filter-head-fiscalqtr').hide();
  $('#filter-head-columnsort').hide();
  ////exclude
  $('#filter-head-exclude-vendor').hide();
  $('#filter-head-exclude-md').hide();
  $('#filter-head-exclude-mm').hide();
  $('#filter-head-exclude-region').hide();
  $('#filter-head-exclude-district').hide();
  $('#filter-head-exclude-patch').hide();
  $('#filter-head-exclude-parent').hide();
  $('#filter-head-exclude-item').hide();
  $('#filter-head-exclude-prodgrp').hide();
  $('#filter-head-exclude-assrt').hide();
  $('#filter-head-exclude-item').hide();
  $('#filter-head-exclude-fiscalwk').hide();
  $('#filter-head-exclude-fiscalmo').hide();
  $('#filter-head-exclude-fiscalqtr').hide();
  $('#filter-head-exclude-columnsort').hide();
}

/**
 * Retail price is only editable at the ItemID/Patch level. If the rotator is checked on anything
 * other than these two groups, disable retail price editing.
 * */
function IsRetailPriceEditValid() {
  for (var i = 0; i < rotator.length; i++) {
    if (rotator[i].column === 'ItemID' && rotator[i].included == true) {
      // Check for ItemID
      for (var j = 0; j < rotator.length; j++) {
        if (rotator[j].column === 'Patch' && rotator[j].included == true) {
          // Check for Patch
          return true;
        }
      }
    }
  }
  return false;
}

// Show only the default columns.
function SetDefaultColumnsView() {
  if (DEBUG) console.log('SetDefaultColumnsView()');
  ShowProcessingLoader();
  if (DEBUG) console.log('line 4162');

  ToggleSalesDollarsGroup();
  ToggleSalesUnitsGroup();
  ToggleRetailPriceGroup();
  ToggleCommentGroup();

  //Add borders to TY columns for Retail Price and Sales Units groups. Removes these borders when ToggleGroup is called.
  $('.su-def-col').addClass('bol'); //Sales Units default column.
  $('.rp-def-col').addClass('bol'); //Retail Price default column.

  DTable.column('SalesDollars_2LY:name').visible(false);
  DTable.column('SalesDollars_LY:name').visible(false);
  DTable.column('SalesDollars_FR_FC:name').visible(false);
  DTable.column('CAGR:name').visible(false);

  DTable.column('SalesUnits_2LY:name').visible(false);
  DTable.column('SalesUnits_LY:name').visible(false);

  DTable.column('RetailPrice_LY:name').visible(false);

  HideProcessingLoader();
}

//Will check if any filters are applied from last state and add them to the filter box.
function SetFilterButtons() {
  if (DEBUG) console.log('SetFilterButtons()');
  var filterArr = [];
  //NEW for exclude
  var excludeArr = [];
  var colName;
  var filterHead;
  var filterBody;
  var filterHeadEx;
  var filterBodyEx;
  var filterDesc;
  var test = params;
  // Set filter search
  //This is a loop only over columns with a filterBox switch set to true.
  if (params.columns !== undefined) {
    for (i = 1; params.columns[i].searchable == true; i++) {
      //colName = $.trim((DTable.column(i).header().innerText).replace(/ /g, ''));
      colName = DTable.init().columns[i].sName;
      if (DTable.columns(i).search()[0].length > 0) {
        var tmp = DTable.columns(colName + ':name')
          .search()[0]
          .toString();
        // Some filters have commas in the text so we need to split on a comma that doesn't have trailing
        // text after it with a double quote and no comma
        var search = tmp.split(/,(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)/gm);
        for (var j = 0; j < search.length; j++) {
          console.log(search[j].toString());
          if (search[j].startsWith('!') && excludeArr.indexOf(search[j]) == -1) {
            // Do not add duplicates
            excludeArr.push(search[j]);
            if (DEBUG) console.log('EXCLUDE ' + search[j].toString());
          } else {
            if (filterArr.indexOf(search[j]) == -1)
              // Do not add duplicates
              filterArr.push(search[j]);
            if (DEBUG) console.log('INCLUDE' + search[j].toString());
          }
        }
      }
      switch (colName) {
        case 'VendorDesc':
          filter_vendor = filterArr;
          exclude_vendor = excludeArr;
          filterHeadEx = 'filter-head-exclude-vendor';
          filterBodyEx = 'filter-body-exclude-vendor';
          filterHead = 'filter-head-vendor';
          filterBody = 'filter-body-vendor';
          filterDesc = 'vendor';
          break;
        case 'MD':
          filter_md = filterArr;
          exclude_md = excludeArr;
          filterHeadEx = 'filter-head-exclude-md';
          filterBodyEx = 'filter-body-exclude-md';
          filterHead = 'filter-head-md';
          filterBody = 'filter-body-md';
          filsterDesc = 'md';
          break;
        case 'MM':
          filter_mm = filterArr;
          exclude_mm = excludeArr;
          filterHead = 'filter-head-mm';
          filterBody = 'filter-body-mm';
          filterHeadEx = 'filter-head-exclude-mm';
          filterBodyEx = 'filter-body-exclude-mm';
          filterDesc = 'mm';
          break;
        case 'Region':
          filter_region = filterArr;
          exclude_region = excludeArr;
          filterHeadEx = 'filter-head-exclude-region';
          filterBodyEx = 'filter-body-exclude-region';
          filterHead = 'filter-head-region';
          filterBody = 'filter-body-region';
          filterDesc = 'region';
          break;
        case 'District':
          filter_district = filterArr;
          exclude_district = excludeArr;
          filterHeadEx = 'filter-head-exclude-district';
          filterBodyEx = 'filter-body-exclude-district';
          filterHead = 'filter-head-district';
          filterBody = 'filter-body-district';
          filterDesc = 'district';
          break;
        case 'Patch':
          filter_patch = filterArr;
          exclude_patch = excludeArr;
          filterHeadEx = 'filter-head-exclude-patch';
          filterBodyEx = 'filter-body-exclude-patch';
          filterHead = 'filter-head-patch';
          filterBody = 'filter-body-patch';
          filterDesc = 'patch';
          break;
        case 'ParentConcat':
          filter_parent = filterArr;
          exclude_parent = excludeArr;
          filterHeadEx = 'filter-head-exclude-parent';
          filterBodyEx = 'filter-body-exclude-parent';
          filterHead = 'filter-head-parent';
          filterBody = 'filter-body-parent';
          filterDesc = 'parent';
          break;
        case 'ItemConcat':
          filter_item = filterArr;
          exclude_item = excludeArr;
          filterHeadEx = 'filter-head-exclude-item';
          filterBodyEx = 'filter-body-exclude-item';
          if (DEBUG) console.log('EXCLUDE ' + filterHeadEx + ' ' + filterBodyEx);
          filterHead = 'filter-head-item';
          filterBody = 'filter-body-item';
          if (DEBUG) console.log('INCLUDE ' + filterHead + ' ' + filterBody);
          filterDesc = 'item';
          break;
        case 'ProdGrpConcat':
          filter_prodgrp = filterArr;
          exclude_prodgrp = excludeArr;
          filterHeadEx = 'filter-head-exclude-prodgrp';
          filterBodyEx = 'filter-body-exclude-prodgrp';
          filterHead = 'filter-head-prodgrp';
          filterBody = 'filter-body-prodgrp';
          filterDesc = 'prodgrp';
          break;
        case 'AssrtConcat':
          filter_assrt = filterArr;
          exclude_assrt = excludeArr;
          filterHeadEx = 'filter-head-exclude-assrt';
          filterBodyEx = 'filter-body-exclude-assrt';
          filterHead = 'filter-head-assrt';
          filterBody = 'filter-body-assrt';
          filterDesc = 'assrt';
          break;
        case 'FiscalWk':
          filter_fiscalwk = filterArr;
          exclude_fiscalwk = excludeArr;
          filterHeadEx = 'filter-head-exclude-fiscalwk';
          filterBodyEx = 'filter-body-exclude-fiscalwk';
          filterHead = 'filter-head-fiscalwk';
          filterBody = 'filter-body-fiscalwk';
          filterDesc = 'fiscalwk';
          break;
        case 'FiscalMo':
          filter_fiscalmo = filterArr;
          exclude_fiscalmo = excludeArr;
          filterHeadEx = 'filter-head-exclude-fiscalmo';
          filterBodyEx = 'filter-body-exclude-fiscalmo';
          filterHead = 'filter-head-fiscalmo';
          filterBody = 'filter-body-fiscalmo';
          filterDesc = 'fiscalmo';
          break;
        case 'FiscalQtr':
          filter_fiscalqtr = filterArr;
          exclude_fiscalqtr = excludeArr;
          filterHeadEx = 'filter-head-exclude-fiscalqtr';
          filterBodyEx = 'filter-body-exclude-fiscalqtr';
          filterHead = 'filter-head-fiscalqtr';
          filterBody = 'filter-body-fiscalqtr';
          filterDesc = 'fiscalqtr';
          break;
      }

      for (var k = 0; k < filterArr.length; k++) {
        AddNewFilterBoxGroup(filterArr, filterHead, filterBody, true);
        if (DEBUG) console.log('INCLUDE ' + filterArr[k].toString() + ' ' + filterHead + ' ' + filterBody);
        AddNewFilter(filterArr[k], filterBody, filterDesc);
      }
      for (var k = 0; k < excludeArr.length; k++) {
        AddNewFilterBoxGroupExclude(excludeArr, filterHeadEx, filterBodyEx, true);
        if (DEBUG) console.log('EXCLUDE ' + excludeArr[k].toString() + ' ' + filterHeadEx + ' ' + filterBodyEx);
        AddNewFilterExclude(excludeArr[k], filterBodyEx, filterDesc);
      }
      filterArr = [];
      excludeArr = [];
    }
  }

  // Update the sort filter for the table
  filter_columnsort = DTable.order();
  UpdateColumnSortFilter(filter_columnsort);

  //Set all column groups visiblity to false before re-enabling visibility for groups that are
  //actually visible. This is needed incase some bookmars are hiding column groups but don't set the
  //column group visibility flags to reflect their state.
  SetAllGroupsVisibility(false);

  // Set visibility
  for (i = 1; i < params.columns.length; i++) {
    colName = DTable.init().columns[i].sName;
    if (params.columns[i].visible == true) {
      switch (colName) {
        case 'SalesDollars_2LY':
        case 'SalesDollars_LY':
        case 'SalesDollars_TY':
        case 'SalesDollars_Curr':
        case 'SalesDollars_Var':
        case 'SalesDollars_FR_FC':
        case 'CAGR':
          $('.sales-dollar-group').addClass('active');
          salesDollarsGroup = true;
          break;
        case 'Turns_LY':
        case 'Turns_TY':
        case 'Turns_FC':
        case 'Turns_Var':
          $('.turns-group').addClass('active');
          turnsGroup = true;
          break;
        case 'SalesUnits_2LY':
        case 'SalesUnits_LY':
        case 'SalesUnits_TY':
        case 'SalesUnits_FC':
        case 'SalesUnits_Var':
          $('.sales-unit-group').addClass('active');
          salesUnitsGroup = true;
          break;
        case 'RetailPrice_LY':
        case 'RetailPrice_TY':
        case 'RetailPrice_Var':
        case 'RetailPrice_Erosion_Rate':
          $('.retail-price-group').addClass('active');
          retailPriceGroup = true;
          break;
        case 'SalesDollars_FR_TY':
        case 'SalesDollars_FR_LY':
        case 'MarginDollars_FR_TY':
        case 'MarginDollars_FR_LY':
        case 'MarginDollars_FR_Var':
          $('.mp-sales-and-dollars').addClass('active');
          mpSalesAndMarginGroup = true;
          break;
        case 'PriceSensitivityPercent':
        case 'PriceSensitivityImpact':
          $('.price-sensitivity-group').addClass('active');
          priceSensGroup = true;
          break;
        case 'Asp_LY':
        case 'Asp_TY':
        case 'Asp_FC':
        case 'Asp_Var':
          $('.asp-group').addClass('active');
          aspGroup = true;
          break;
        case 'Margin_Percent_LY':
        case 'Margin_Percent_TY':
        case 'Margin_Percent_Curr':
        case 'Margin_Percent_Var':
        case 'Margin_Percent_FR':
          $('.margin-percent-group').addClass('active');
          marginPercGroup = true;
          break;
        case 'Margin_Dollars_LY':
        case 'Margin_Dollars_TY':
        case 'Margin_Dollars_Var_Curr':
        case 'Margin_Dollars_Curr':
        case 'Margin_Dollars_FR':
          $('.margin-dollar-group').addClass('active');
          marginDollGroup = true;
          break;
        case 'SellThru_LY':
        case 'SellThru_TY':
          $('.sell-thru-group').addClass('active');
          sellThruGroup = true;
          break;
        case 'ReceiptDollars_LY':
        case 'ReceiptDollars_TY':
          $('.reciept-dollar-group').addClass('active');
          recDollGroup = true;
          break;
        case 'ReceiptUnits_LY':
        case 'ReceiptUnits_TY':
          $('.receipt-units-group').addClass('active');
          recUnitGroup = true;
          break;
        case 'Dollars_FC_DL':
        case 'Units_FC_DL':
        case 'Dollars_FC_DL_Var':
        case 'Units_FC_DL_Var':
        case 'Dollars_FC_LOW':
        case 'Units_FC_LOW':
        case 'Dollars_FC_LOW_Var':
        case 'Dollars_FC_Vendor':
        case 'Units_FC_Vendor':
        case 'Dollars_FC_Vendor_Var':
        case 'Units_FC_Vendor_Var':
          $('.forecast-group').addClass('active');
          forecastGroup = true;
          break;
        case 'Cost_LY':
        case 'Cost_TY':
        case 'Cost_FC':
        case 'Cost_Var':
          $('.cost-group').addClass('active');
          costGroup = true;
          break;
        case 'MM_Comments':
        case 'Vendor_Comments':
          $('.comments-group').addClass('active');
          commentGroup = true;
          break;
        default:
          break;
      }
    }
  }
}

/**
 * Goes through all the column groups and sets the visibility to the boolean state provided
 * @param {any} visible A boolean that sets the visibility of all volumn groups
 */
function SetAllGroupsVisibility(visible) {
  salesDollarsGroup = visible;
  turnsGroup = visible;
  salesUnitsGroup = visible;
  retailPriceGroup = visible;
  mpSalesAndMarginGroup = visible;
  priceSensGroup = visible;
  aspGroup = visible;
  marginPercGroup = visible;
  marginDollGroup = visible;
  sellThruGroup = visible;
  recDollGroup = visible;
  recUnitGroup = visible;
  forecastGroup = visible;
  costGroup = visible;
  itemDescGroup = visible;
  assrtDescGroup = visible;
  prodgrpDescGroup = visible;
  cartegoryDescGroup = visible;
  commentGroup = visible;
}

//Will clear the filter button list.  Call this prior to calling SetFilterButtons.
function ClearFilterButtons() {
  HideAllFilterCats();
  $('#filter-box-body ul div').html('');
}

function CallLoadSums(setSums, loadSums) {
  setSums = typeof setSums === 'undefined' ? true : setSums;
  loadSums = typeof loadSums === 'undefined' ? false : loadSums;
  if (setSums && !loadSums) {
    if (DTable && DTable.sums) {
      SetSums(DTable.sums);
    }
  } else if (setSums && loadSums) {
    LoadSums();
  }
}

//Show or hide the Item Description column.
function ToggleParentGroup() {
  if (parentDescGroup == null || parentDescGroup == false) {
    DTable.column('ParentDesc:name').visible(true);
    DTable.column('ParentID:name').visible(true);
    $('.parent-desc-group').addClass('active');
    parentDescGroup = true;
  } else {
    DTable.column('ParentDesc:name').visible(false);
    DTable.column('ParentID:name').visible(false);
    $('.parent-desc-group').removeClass('active');
    parentDescGroup = false;
  }
  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide the Comment columns.
function ToggleCommentGroup() {
  if (commentGroup == null || commentGroup == false) {
    DTable.column('MM_Comments:name').visible(true);
    DTable.column('Vendor_Comments:name').visible(true);
    $('.comments-group').addClass('active');
    commentGroup = true;
  } else {
    DTable.column('MM_Comments:name').visible(false);
    DTable.column('Vendor_Comments:name').visible(false);
    $('.comments-group').removeClass('active');
    commentGroup = false;
  }
  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide the Sales Dollars group
function ToggleSalesDollarsGroup() {
  if (salesDollarsGroup == null || salesDollarsGroup == false) {
    DTable.column('SalesDollars_2LY:name').visible(true);
    DTable.column('SalesDollars_LY:name').visible(true);
    DTable.column('SalesDollars_TY:name').visible(true);
    DTable.column('SalesDollars_Curr:name').visible(true);
    DTable.column('SalesDollars_Var:name').visible(true);
    DTable.column('SalesDollars_FR_FC:name').visible(true);
    DTable.column('CAGR:name').visible(true);
    $('.sales-dollar-group').addClass('active');
    salesDollarsGroup = true;
  } else {
    DTable.column('SalesDollars_2LY:name').visible(false);
    DTable.column('SalesDollars_LY:name').visible(false);
    DTable.column('SalesDollars_TY:name').visible(false);
    DTable.column('SalesDollars_Curr:name').visible(false);
    DTable.column('SalesDollars_Var:name').visible(false);
    DTable.column('SalesDollars_FR_FC:name').visible(false);
    DTable.column('CAGR:name').visible(false);
    $('.sales-dollar-group').removeClass('active');
    salesDollarsGroup = false;
  }

  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide the Turns group
function ToggleTurnsGroup() {
  if (turnsGroup == null || turnsGroup == false) {
    DTable.column('Turns_LY:name').visible(true);
    DTable.column('Turns_TY:name').visible(true);
    DTable.column('Turns_FC:name').visible(true);
    DTable.column('Turns_Var:name').visible(true);
    $('.turns-group').addClass('active');
    turnsGroup = true;
  } else {
    DTable.column('Turns_LY:name').visible(false);
    DTable.column('Turns_TY:name').visible(false);
    DTable.column('Turns_FC:name').visible(false);
    DTable.column('Turns_Var:name').visible(false);
    $('.turns-group').removeClass('active');
    turnsGroup = false;
  }
  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide Sales Units group
function ToggleSalesUnitsGroup() {
  if (salesUnitsGroup == null || salesUnitsGroup == false) {
    DTable.column('SalesUnits_2LY:name').visible(true);
    DTable.column('SalesUnits_LY:name').visible(true);
    DTable.column('SalesUnits_TY:name').visible(true);
    DTable.column('SalesUnits_FC:name').visible(true);
    DTable.column('SalesUnits_Var:name').visible(true);
    $('.sales-units-group').addClass('active');
    $('.su-def-col').removeClass('bol'); //Retail Price default column.
    salesUnitsGroup = true;
  } else {
    DTable.column('SalesUnits_2LY:name').visible(false);
    DTable.column('SalesUnits_LY:name').visible(false);
    DTable.column('SalesUnits_TY:name').visible(false);
    DTable.column('SalesUnits_FC:name').visible(false);
    DTable.column('SalesUnits_Var:name').visible(false);
    $('.sales-units-group').removeClass('active');
    salesUnitsGroup = false;
  }

  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide Retail Price Group
function ToggleRetailPriceGroup() {
  if (retailPriceGroup == null || retailPriceGroup == false) {
    DTable.column('RetailPrice_LY:name').visible(true);
    DTable.column('RetailPrice_TY:name').visible(true);
    DTable.column('RetailPrice_FC:name').visible(true);
    DTable.column('RetailPrice_Var:name').visible(true);
    DTable.column('RetailPrice_Erosion_Rate:name').visible(true);
    $('.retail-price-group').addClass('active');
    $('.rp-def-col').removeClass('bol'); //Remove left border from TY column.
    retailPriceGroup = true;
  } else {
    DTable.column('RetailPrice_LY:name').visible(false);
    DTable.column('RetailPrice_TY:name').visible(false);
    DTable.column('RetailPrice_FC:name').visible(false);
    DTable.column('RetailPrice_Var:name').visible(false);
    DTable.column('RetailPrice_Erosion_Rate:name').visible(false);
    $('.retail-price-group').removeClass('active');
    retailPriceGroup = false;
  }
  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide MP Group
function ToggleMPGroup() {
  let visible = mpSalesAndMarginGroup === null || mpSalesAndMarginGroup === false;

  DTable.column('SalesDollars_FR_LY:name').visible(visible);
  DTable.column('SalesDollars_FR_TY:name').visible(visible);
  DTable.column('MarginDollars_FR_LY:name').visible(visible);
  DTable.column('MarginDollars_FR_TY:name').visible(visible);
  DTable.column('MarginDollars_FR_Var:name').visible(visible);
  if (visible) {
    $('.mp-sales-and-margin-group').addClass('active');
  } else {
    $('.mp-sales-and-margin-group').removeClass('active');
  }
  mpSalesAndMarginGroup = visible;

  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide Price Sensitivity Group
function TogglePriceSensGroup() {
  if (priceSensGroup == null || priceSensGroup == false) {
    DTable.column('PriceSensitivityPercent:name').visible(true);
    DTable.column('PriceSensitivityImpact:name').visible(true);
    $('.price-sensitivity-group').addClass('active');
    priceSensGroup = true;
  } else {
    DTable.column('PriceSensitivityPercent:name').visible(false);
    DTable.column('PriceSensitivityImpact:name').visible(false);
    $('.price-sensitivity-group').removeClass('active');
    priceSensGroup = false;
  }
  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide ASP Group
function ToggleAspGroup() {
  if (aspGroup == null || aspGroup == false) {
    DTable.column('Asp_LY:name').visible(true);
    DTable.column('Asp_TY:name').visible(true);
    DTable.column('Asp_FC:name').visible(true);
    DTable.column('Asp_Var:name').visible(true);
    $('.asp-group').addClass('active');
    aspGroup = true;
  } else {
    DTable.column('Asp_LY:name').visible(false);
    DTable.column('Asp_TY:name').visible(false);
    DTable.column('Asp_FC:name').visible(false);
    DTable.column('Asp_Var:name').visible(false);
    $('.asp-group').removeClass('active');
    aspGroup = false;
  }
  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide Margin % Group
function ToggleMarginPercGroup() {
  if (marginPercGroup == null || marginPercGroup == false) {
    DTable.column('Margin_Percent_LY:name').visible(true);
    DTable.column('Margin_Percent_TY:name').visible(true);
    DTable.column('Margin_Percent_Curr:name').visible(true);
    DTable.column('Margin_Percent_Var:name').visible(true);
    DTable.column('Margin_Percent_FR:name').visible(true);
    $('.margin-percent-group').addClass('active');
    marginPercGroup = true;
  } else {
    DTable.column('Margin_Percent_LY:name').visible(false);
    DTable.column('Margin_Percent_TY:name').visible(false);
    DTable.column('Margin_Percent_Curr:name').visible(false);
    DTable.column('Margin_Percent_Var:name').visible(false);
    DTable.column('Margin_Percent_FR:name').visible(false);
    $('.margin-percent-group').removeClass('active');
    marginPercGroup = false;
  }

  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide Margin $ Group
function ToggleMarginDollGroup() {
  if (marginDollGroup == null || marginDollGroup == false) {
    DTable.column('Margin_Dollars_LY:name').visible(true);
    DTable.column('Margin_Dollars_TY:name').visible(true);
    DTable.column('Margin_Dollars_Var_Curr:name').visible(true);
    DTable.column('Margin_Dollars_Curr:name').visible(true);
    DTable.column('Margin_Dollars_FR:name').visible(true);
    $('.margin-dollar-group').addClass('active');
    marginDollGroup = true;
  } else {
    DTable.column('Margin_Dollars_LY:name').visible(false);
    DTable.column('Margin_Dollars_TY:name').visible(false);
    DTable.column('Margin_Dollars_Var_Curr:name').visible(false);
    DTable.column('Margin_Dollars_Curr:name').visible(false);
    DTable.column('Margin_Dollars_FR:name').visible(false);
    $('.margin-dollar-group').removeClass('active');
    marginDollGroup = false;
  }

  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide SellThru Group
function ToggleSellThruGroup() {
  if (sellThruGroup == null || sellThruGroup == false) {
    DTable.column('SellThru_LY:name').visible(true);
    DTable.column('SellThru_TY:name').visible(true);
    $('.sell-thru-group').addClass('active');
    sellThruGroup = true;
  } else {
    DTable.column('SellThru_LY:name').visible(false);
    DTable.column('SellThru_TY:name').visible(false);
    $('.sell-thru-group').removeClass('active');
    sellThruGroup = false;
  }
  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide Rec Dollars Group
function ToggleRecDollarsGroup() {
  if (recDollGroup == null || recDollGroup == false) {
    DTable.column('ReceiptDollars_LY:name').visible(true);
    DTable.column('ReceiptDollars_TY:name').visible(true);
    $('.receipt-dollar-group').addClass('active');
    recDollGroup = true;
  } else {
    DTable.column('ReceiptDollars_LY:name').visible(false);
    DTable.column('ReceiptDollars_TY:name').visible(false);
    $('.receipt-dollar-group').removeClass('active');
    recDollGroup = false;
  }
  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide Rec Units Group
function ToggleRecUnitsGroup() {
  if (recUnitGroup == null || recUnitGroup == false) {
    DTable.column('ReceiptUnits_LY:name').visible(true);
    DTable.column('ReceiptUnits_TY:name').visible(true);
    $('.receipt-units-group').addClass('active');
    recUnitGroup = true;
  } else {
    DTable.column('ReceiptUnits_LY:name').visible(false);
    DTable.column('ReceiptUnits_TY:name').visible(false);
    $('.receipt-units-group').removeClass('active');
    recUnitGroup = false;
  }
  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide Forecast Comparison Group
function ToggleForecastGroup() {
  if (forecastGroup == null || forecastGroup == false) {
    DTable.column('Dollars_FC_DL:name').visible(true);
    DTable.column('Units_FC_DL:name').visible(true);
    DTable.column('Dollars_FC_DL_Var:name').visible(true);
    DTable.column('Units_FC_DL_Var:name').visible(true);
    DTable.column('Dollars_FC_LOW:name').visible(true);
    DTable.column('Units_FC_LOW:name').visible(true);
    DTable.column('Dollars_FC_LOW_Var:name').visible(true);
    DTable.column('Units_FC_LOW_Var:name').visible(true);
    DTable.column('Dollars_FC_Vendor:name').visible(true);
    DTable.column('Units_FC_Vendor:name').visible(true);
    DTable.column('Dollars_FC_Vendor_Var:name').visible(true);
    DTable.column('Units_FC_Vendor_Var:name').visible(true);
    $('.forecast-group').addClass('active');
    forecastGroup = true;
  } else {
    DTable.column('Dollars_FC_DL:name').visible(false);
    DTable.column('Units_FC_DL:name').visible(false);
    DTable.column('Dollars_FC_DL_Var:name').visible(false);
    DTable.column('Units_FC_DL_Var:name').visible(false);
    DTable.column('Dollars_FC_LOW:name').visible(false);
    DTable.column('Units_FC_LOW:name').visible(false);
    DTable.column('Dollars_FC_LOW_Var:name').visible(false);
    DTable.column('Units_FC_LOW_Var:name').visible(false);
    DTable.column('Dollars_FC_Vendor:name').visible(false);
    DTable.column('Units_FC_Vendor:name').visible(false);
    DTable.column('Dollars_FC_Vendor_Var:name').visible(false);
    DTable.column('Units_FC_Vendor_Var:name').visible(false);
    $('.forecast-group').removeClass('active');
    forecastGroup = false;
  }

  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Show or hide Cost Group
function ToggleCostGroup() {
  if (costGroup == null || costGroup == false) {
    DTable.column('Cost_LY:name').visible(true);
    DTable.column('Cost_TY:name').visible(true);
    DTable.column('Cost_FC:name').visible(true);
    DTable.column('Cost_Var:name').visible(true);
    $('.cost-group').addClass('active');
    costGroup = true;
  } else {
    DTable.column('Cost_LY:name').visible(false);
    DTable.column('Cost_TY:name').visible(false);
    DTable.column('Cost_FC:name').visible(false);
    DTable.column('Cost_Var:name').visible(false);
    $('.cost-group').removeClass('active');
    costGroup = false;
  }
  if (isLastDraw) {
    CallLoadSums(true);
    AdjustForecastTable();
  }
}

//Create unique rowID based on rotator selection
function CreateRowID(rowIdx) {
  var arr = rotator;
  var i = 0;
  var j = 0;
  var id = [];

  if (DTable !== undefined) {
    for (i = 0; i < rotator.length; i++) {
      var name = rotator[i].column;
      if (rotator[i].included == true) {
        id.push(DTable.cell(rowIdx, name + ':name').data());
      }
    }
  }
  id.join();
  return id;
}

function Logout() {
  //alert("You have been logged off from the website");
  $('#logoutModal').modal();
  $('#logoutModal').modal('open');
  $('#logout_yes').on('click', function () {
    document.execCommand('ClearAuthenticationCache', 'false');
    window.location = '/Home/Logout';
  });
}

/**
 * This function executes a given function parameter with the updating screen.
 * You must provide a function that you want to execute and a time in milliseconds that
 * you want the updating screen to play for before the execution of the function
 * provided actually starts.
 * @param {function} f A function in the form of "function(){}" or "functionName" it cannot be an IIFE (Immediately
 * Invoked Function Expression) because that will be passing in a value and not the function.
 * @param {any} timeout The time in milliseconds that you want the updating screen to play for.
 * default is 400.
 */
function RunWithUpdater(f, timeout) {
  // Can't use default parameters with IE so this is a
  // old fasion workaround.
  timeout = timeout || 400;

  ShowProcessingLoader();
  setTimeout(function () {
    f();
    HideProcessingLoader();
  }, parseInt(timeout));
}

function ShowAllForecastColumns() {
  $(
    '.sales-dollar-group, .turns-group, .sales-units-group, .retail-price-group, .mp-sales-and-margin-group, .price-sensitivity-group, .asp-group, .margin-percent-group, .margin-dollar-group, .sell-thru-group, .receipt-dollar-group, .receipt-units-group, .forecast-group, .cost-group, .comments-group'
  ).addClass('active');
  SetAllGroupsVisibility(true);
  DTable.columns('.filter').visible(true);
  $('.rp-def-col').removeClass('bol'); //Remove left border from TY column.
  $('.su-def-col').removeClass('bol'); //Retail Price default column.
}

function HideAllForecastColumns() {
  $(
    '.sales-dollar-group, .turns-group, .sales-units-group, .retail-price-group, .mp-sales-and-margin-group, .price-sensitivity-group, .asp-group, .margin-percent-group, .margin-dollar-group, .sell-thru-group, .receipt-dollar-group, .receipt-units-group, .forecast-group, .cost-group, .comments-group'
  ).removeClass('active');
  SetAllGroupsVisibility(false);
  DTable.columns('.filter').visible(false);
}

function ShowProcessingLoader() {
  if (DEBUG) {
    console.log('ShowProcessingLoader()');
  }
  $('#updating-background').css('display', 'flex');
  $('#updating-gif').css('display', 'block');
}

function HideProcessingLoader() {
  if (DEBUG) {
    console.log('HideProcessingLoader()');
  }
  $('#updating-background').css('display', 'none');
  $('#updating-gif').css('display', 'none');
}

function UploadFile() {
  $.ajax({
    // Your server script to process the upload
    url: '/Home/Upload',
    type: 'POST',

    // Form data
    data: new FormData($('form')[0]),

    // Tell jQuery not to process data or worry about content-type
    // You *must* include these options!
    cache: false,
    contentType: false,
    processData: false,

    // Custom XMLHttpRequest
    xhr: function () {
      var myXhr = $.ajaxSettings.xhr();
      if (myXhr.upload) {
        // For handling the progress of the upload
        myXhr.upload.addEventListener(
          'progress',
          function (e) {
            if (e.lengthComputable) {
              $('#uploadProgress').attr({
                value: e.loaded,
                max: e.total,
              });
            }
          },
          false
        );
      }
      return myXhr;
    },
    success: function (x) {
      if (x.success) {
        $('#uploadModal').modal('close');
        alert('Upload success! You will receive an email when this file is done processing.');
      } else {
        $('#uploadModal').modal('close');
        alert(x.msg);
      }
    },
    error: function (jqXHR, textStatus, errorThrown) {
      alert('An error occurred!');

      if (DEBUG) console.log('jqXHR:');
      if (DEBUG) console.log(jqXHR);
      if (DEBUG) console.log('textStatus:');
      if (DEBUG) console.log(textStatus);
      if (DEBUG) console.log('errorThrown:');
      if (DEBUG) console.log(errorThrown);
    },
    complete: function () {
      $('#uploadModal').modal('close');
      $('#uploadProgress').attr({
        value: 0,
      });

      // Clear all form data in the uploads modal.
      ClearUploadForms();
      ClearUploadFileNames();
    },
  });
}

function UploadNewItemsFile() {
  ShowProcessingLoader();
  $.ajax({
    url: '/Home/UploadNewItems',
    type: 'POST',

    // Form data
    data: new FormData($('#new_items_file_form')[0]),

    // Tell jQuery not to process data or worry about content-type
    // You *must* include these options!
    cache: false,
    contentType: false,
    processData: false,

    // Custom XMLHttpRequest
    xhr: function () {
      var myXhr = $.ajaxSettings.xhr();
      if (myXhr.upload) {
        myXhr.upload.addEventListener(
          'progress',
          function (e) {
            if (e.lengthComputable) {
              $('#new_item_upload_progress').attr({
                value: e.loaded,
                max: e.total,
              });
            }
          },
          false
        );
      }
      return myXhr;
    },
    success: function (x) {
      if (x.success) {
        $('#uploadModal').modal('close');
        alert(x.message);
        HideProcessingLoader();
      } else {
        $('#uploadModal').modal('close');
        alert(x.message);
        if (x.fileName && x.fileName.length > 0) {
          if (ForecastDebugger) {
            ForecastDebugger.setDownloadedFile('newItems', x.fileName);
          }
          window.location.href = '/Home/DownloadFile?fileName=' + x.fileName;
        } else {
          HideProcessingLoader();
        }
      }
      if (typeof x.isPreFreeze !== undefined && !x.isPreFreeze) {
        DTable.columns.adjust().draw();
      }
    },
    error: function (jqXHR, textStatus, errorThrown) {
      alert('An error occurred!');

      if (DEBUG) console.log('jqXHR:');
      if (DEBUG) console.log(jqXHR);
      if (DEBUG) console.log('textStatus:');
      if (DEBUG) console.log(textStatus);
      if (DEBUG) console.log('errorThrown:');
      if (DEBUG) console.log(errorThrown);
      HideProcessingLoader();
    },
    complete: function (x) {
      $('#uploadModal').modal('close');
      $('#new_item_upload_progress').attr({
        value: 0,
      });
      setTimeout(function () {
        HideProcessingLoader();
        var rj = x.responseJSON;
        if (rj && (rj.success || (!rj.success && rj.fileName && rj.fileName.length > 0)) && ExceptionsTabModule.ipoOverlapTable.isVisible()) {
          ExceptionsTabModule.ipoOverlapTable.draw();
        }
      }, 2000);

      // Clear all form data in the uploads modal.
      ClearUploadForms();
      ClearUploadFileNames();
    },
  });
}

/**
 * Function that triggers an upload for an 'Item Patch Ownership' file.
 */
function UploadItemPatchOwnershipFile() {
  ShowProcessingLoader();
  $.ajax({
    url: '/Home/UploadItemPatchOwnership',
    type: 'POST',

    // Form data
    data: new FormData($('#iou_file_form')[0]),

    // Tell jQuery not to process data or worry about content-type
    // You *must* include these options!
    cache: false,
    contentType: false,
    processData: false,

    // Custom XMLHttpRequest
    xhr: function () {
      var myXhr = $.ajaxSettings.xhr();
      if (myXhr.upload) {
        myXhr.upload.addEventListener(
          'progress',
          function (e) {
            if (e.lengthComputable) {
              $('#iou_upload_progress').attr({
                value: e.loaded,
                max: e.total,
              });
            }
          },
          false
        );
      }
      return myXhr;
    },
    success: function (x) {
      if (x.success) {
        $('#uploadModal').modal('close');
        alert(x.message);
        HideProcessingLoader();
      } else {
        $('#uploadModal').modal('close');
        alert(x.message);
        if (x.fileName && x.fileName.length > 0) {
          if (ForecastDebugger) {
            ForecastDebugger.setDownloadedFile('itemPatchOwnership', x.fileName);
          }
          window.location.href = '/Home/DownloadFile?fileName=' + x.fileName;
        } else {
          HideProcessingLoader();
        }
      }
      if (typeof x.isPreFreeze !== undefined && !x.isPreFreeze) {
        DTable.columns.adjust().draw();
      }
    },
    error: function (jqXHR, textStatus, errorThrown) {
      HideProcessingLoader();
      alert('An error occurred!');

      if (DEBUG) console.log('jqXHR:');
      if (DEBUG) console.log(jqXHR);
      if (DEBUG) console.log('textStatus:');
      if (DEBUG) console.log(textStatus);
      if (DEBUG) console.log('errorThrown:');
      if (DEBUG) console.log(errorThrown);
    },
    complete: function (x) {
      $('#uploadModal').modal('close');
      $('#iou_upload_progress').attr({
        value: 0,
      });
      setTimeout(function () {
        HideProcessingLoader();
        var rj = x.responseJSON;
        if (rj && (rj.success || (!rj.success && rj.fileName && rj.fileName.length > 0)) && ExceptionsTabModule.ipoOverlapTable.isVisible()) {
          ExceptionsTabModule.ipoOverlapTable.draw();
        }
      }, 2000);

      // Clear all form data in the uploads modal.
      ClearUploadForms();
      ClearUploadFileNames();
    },
  });
}

function disableUploadButtonById(id, disable) {
  if (disable) {
    $('#' + id)
      .parent()
      .addClass('disabled');
  } else {
    $('#' + id)
      .parent()
      .removeClass('disabled');
  }
}

function PreventIfLoading(e) {
  if (!e) {
    return;
  }
  if ($('#updating-background').css('display') !== 'none') {
    e.stopImmediatePropagation();
    return;
  }
}

//***************************************************************
//*              Fiscal Selector Box Functions
//**************************************************************/

/**
 * Function that adds a filter to a list of filters that will be updated at once.
 * @param {any} filterId The name of the filter. Such as: item, mm, fiscalwk, fiscalmo, md etc...
 * @param {any} filter The coresponding filter array.
 */
function AddFilterToFilterList(filterId, filter) {
  if (!IsFilterIdInFilterList(filters_selected, filterId)) {
    filters_selected.push({ id: filterId, filter: filter });
  }
}
//changed filters excluded to filters_selected
//adds a filter to the exclude list of filters
function AddFilterToExcludeFilterList(filterId, filter) {
  if (DEBUG) {
    console.log('AddFilterToExcludeFilterList()');
  }
  if (!IsFilterIdInFilterList(filters_selected, filterId)) {
    filters_selected.push({ id: filterId, filter: filter });
    if (DEBUG) {
      console.log('ADDING to excludefilterlist');
    }
  }
}

/**
 * Adds an items to an array if it doesn't exist.
 * @param {any} array The array to modify.
 * @param {any} item The item to add.
 */
function AddItemToArray(array, item) {
  if (array.indexOf(item) <= -1) {
    array.push(item);
  }
}

/**
 * Function that checks to see if the filter array has elements in it
 * and then calls the select2 to be highlighted.
 * @param {any} targetId The filter id of the select2 dropdown. This name is also used to find
 * its corresponding array.
 */
function CheckSelectedFilters(targetId) {
  if (window[targetId] !== undefined && window[targetId].length > 0) {
    setTimeout(function (e) {
      SetAllSelect2Selected(targetId);
    }, 20);
  }
}

/**
 * Function that higlights a selected filter element and adds the selected element
 * to its corresponding filter.
 * @param {any} id The id of the element that was clicked. Such as: item, mm, fiscalwk, prodgrp, etc...
 * @param {any} e The original event that occured.
 */
function HighlightSelect2(id, e) {
  if (DEBUG) console.log('HighlightSelect2!');
  var tempId = GetFilterNameFromId(id);
  var elemId = 'ul[id*="select2-filter-' + tempId + '"]';
  //need this if ctrl key is used for multi select
  if (exclude == false) {
    var filterName = 'filter_' + tempId;
  } else {
    var filterName = 'exclude_' + tempId;
  }

  var target = e.params.originalEvent.currentTarget;
  var selected = false;

  // If the element that was clicked doesn't have the custom forecast-select attr
  // or is set to false then assign it to true
  if (exclude == false) {
    if ($(target).attr('forecast-selected') === undefined || $(target).attr('forecast-selected') == 'false') {
      $(target).attr('forecast-selected', 'true');
      $(target).attr('aria-selected', 'false');
      selected = true;
    } else {
      $(target).attr('forecast-selected', 'false');
      $(target).attr('aria-selected', 'false');
    }
  } else {
    if ($(target).attr('forecast-selected-exclude') === undefined || $(target).attr('forecast-selected-exclude') == 'false') {
      $(target).attr('forecast-selected-exclude', 'true');
      $(target).attr('aria-selected', 'false');
      selected = true;
    } else {
      $(target).attr('forecast-selected-exclude', 'false');
      $(target).attr('aria-selected', 'false');
    }
  }

  // Either adds or removes the element from the filter array depending on whether
  // it was selected or de-selected.
  if (exclude == false) {
    if (selected) {
      AddItemToArray(window[filterName], e.params.data.text);
    } else {
      RemoveItemFromArray(window[filterName], e.params.data.text);
    }
  } else {
    if (selected) {
      AddItemToArray(window[filterName], '!' + e.params.data.text);
    } else {
      RemoveItemFromArray(window[filterName], '!' + e.params.data.text);
    }
  }
}

/**
 * Function that checks if the filter already exists in the filter list.
 * @param {any} filterList The filter list that we are checking that should contain a list of
 * objects with the parameter "id" and "filter".
 * @param {any} filterId The filter name that you are trying to find in the list of filters.
 */
function IsFilterIdInFilterList(filterList, filterId) {
  for (var i = 0; i < filterList.length; i++) {
    if (filterId == filterList[0].id) {
      return true;
    }
  }

  return false;
}

/**
 * Removes an items from an array if it exists.
 * @param {any} array The array to modify.
 * @param {any} item The item to remove.
 */
function RemoveItemFromArray(array, item) {
  var index = array.indexOf(item);
  if (index > -1) {
    array.splice(index, 1);
  }
}

/**
 * Function that goes through a select2 list and sets the selected elements from the
 * filter array to selected.
 * @param {any} filterId The filter id of the select2 dropdown. This name is also used to find
 * its corresponding array.
 */
function SetAllSelect2Selected(filterId) {
  var tempId = filterId.replace(/\_/gm, '-');

  if (tempId.includes('exclude')) {
    tempId = tempId.replace('exclude', 'filter');
  }
  tempId = 'select2-' + tempId + '-results';
  if (DEBUG) console.log('window filterId = ' + window[filterId]);

  //if user selected before previouse selections found skip
  if ($('#' + tempId).length < 1) {
  } else {
    $.each($('#' + tempId)[0].children, function (key, val) {
      var temp = val.innerText.replace(/\"/gm, '');
      if (ListContains(window[filterId], temp)) {
        if (filterId.includes('exclude')) {
          //$(val).addClass('exclude_allow_reselect');
          $(val).attr('forecast-selected-exclude', 'true');
        } else {
          $(val).attr('forecast-selected', 'true');
        }
      }
    });
  }
}

//Function that expands all fiscal selector boxes
function ShowFiscalBoxes() {
  ShowFiscalWeekSelectorBox();
  ShowFiscalMonthSelectorBox();
  ShowFiscalQuarterSelectorBox();
}

/**
 * Function that goes through and unselects all fiscal weeks/months/quarters.
 * */
function UnselectAllFiscalBoxes() {
  FiscalSelector('#fiscal-wk-selectable', 'unselect-all', filter_fiscalwk, 'include');
  FiscalSelector('#fiscal-mo-selectable', 'unselect-all', filter_fiscalmo, 'include');
  FiscalSelector('#fiscal-qtr-selectable', 'unselect-all', filter_fiscalqtr, 'include');

  FiscalSelector('#fiscal-wk-selectable', 'unselect-all', exclude_fiscalwk, 'exclude');
  FiscalSelector('#fiscal-mo-selectable', 'unselect-all', exclude_fiscalmo, 'exclude');
  FiscalSelector('#fiscal-qtr-selectable', 'unselect-all', exclude_fiscalqtr, 'exclude');
}

/**
 * Function that goes through and updates the selected fiscal weeks/months/quarters
 * based on the elements currently in their respective filters.
 * */
function UpdateFiscalBoxes() {
  if (DEBUG) console.log('UpdateFiscalBoexes()');

  FiscalSelector('#fiscal-wk-selectable', 'select', filter_fiscalwk, 'include');
  FiscalSelector('#fiscal-mo-selectable', 'select', filter_fiscalmo, 'include');
  FiscalSelector('#fiscal-qtr-selectable', 'select', filter_fiscalqtr, 'include');

  //exclude
  if (DEBUG) console.log('UpdateFiscalBoexesExclude()');

  FiscalSelector('#fiscal-wk-selectable', 'select', exclude_fiscalwk, 'exclude');
  FiscalSelector('#fiscal-mo-selectable', 'select', exclude_fiscalmo, 'exclude');
  FiscalSelector('#fiscal-qtr-selectable', 'select', exclude_fiscalqtr, 'exclude');
}

/**
 * This function sets up the fiscal month selector box.
 */
function ShowFiscalMonthSelectorBox() {
  var fiscalMonthSelectableId = '#fiscal-mo-selectable';
  var monthChildCount = $(fiscalMonthSelectableId)[0].childElementCount;

  //We check to see if the fiscal month selector box hasn't already been created to prevent
  //anyone from creating it again and again.
  if (monthChildCount < 1) {
    $.ajax({
      url: '/Home/GetFilterData',
      dataType: 'json',
      method: 'POST',
      data: { TableName: params.tableName, type: 'FiscalMo', search: params.term },
      tags: true,
      success: function (data) {
        FiscalSelector(fiscalMonthSelectableId, 'init', data.results);

        //Instantiate Fiscal Month Selector Box
        $(fiscalMonthSelectableId).selectable({
          cancel: '#X,.cancel', //This prevents the X buttons from filtering or clearing any months selected
          stop: function (event, ui) {
            //Clear the filter_fiscalmo array for a fresh start everytime.
            //Clear the filter_fiscalqtr array for a fresh start everytime.
            if (exclude == false) {
              filter_fiscalmo.length = 0;
            } else {
              exclude_fiscalmo.length = 0;
            }

            //Unselect any X months that were seleted
            FiscalSelector(fiscalMonthSelectableId, 'unselect-x');

            //Get all of the months that are currently selected and push them into the filter_fiscalmo array
            FiscalSelector(fiscalMonthSelectableId, 'get-selected', filter_fiscalmo, 'include');
            FiscalSelector(fiscalMonthSelectableId, 'get-selected', exclude_fiscalmo, 'exclude');

            //Add the filter to be updated with all other filters if multi-selecting
            if (exclude == false) {
              //check if exclude list has new values and remove if it does
              for (i = 0; i < exclude_fiscalmo.length; i++) {
                for (j = 0; j < filter_fiscalmo.length; j++)
                  if (exclude_fiscalmo[i] == '!' + filter_fiscalmo[j]) {
                    exclude_fiscalmo.splice(i, 1);
                  }
              }
              AddFilterToFilterList('fiscalmo', filter_fiscalmo);
            } else {
              //check if include list has values and remove if it does
              for (i = 0; i < filter_fiscalmo.length; i++) {
                for (j = 0; j < exclude_fiscalmo.length; j++)
                  if ('!' + filter_fiscalmo[i] == exclude_fiscalmo[j]) {
                    filter_fiscalmo.splice(i, 1);
                  }
              }
              AddFilterToExcludeFilterList('fiscalmo', exclude_fiscalmo);
            }

            //Only start the filter process if the user isn't holding the Ctrl key down
            if (!event.ctrlKey) {
              if (exclude == false) {
                UpdateFilter('fiscalmo', filter_fiscalmo);
                filters_selected.length = 0;
              } else {
                UpdateFilterExclude('fiscalmo', exclude_fiscalmo);
                filters_selected.length = 0;
              }
            }
          },
        });

        //If there are any filters selected upon page reload and first time opening the fiscal month selector box
        //we set the filters that are in state as seleted
        if (exclude == false) {
          FiscalSelector(fiscalMonthSelectableId, 'select', filter_fiscalmo, 'include');
          FiscalSelector(fiscalMonthSelectableId, 'select', exclude_fiscalmo, 'exclude');
        } else {
          FiscalSelector(fiscalMonthSelectableId, 'select', exclude_fiscalmo, 'exclude');
          FiscalSelector(fiscalMonthSelectableId, 'select', filter_fiscalmo, 'include');
        }

        ShowFiscalMonthHeader();
      },
    });
  } else {
    ShowFiscalMonthHeader();
  }
}

/**
 * This function sets up the fiscal quarter selector box.
 */
function ShowFiscalQuarterSelectorBox() {
  var fiscalQuarterSelectableId = '#fiscal-qtr-selectable';
  var quarterChildCount = $(fiscalQuarterSelectableId)[0].childElementCount;

  //We check to see if the fiscal quarter selector box hasn't already been created to prevent
  //anyone from creating it again and again.
  if (quarterChildCount < 1) {
    $.ajax({
      url: '/Home/GetFilterData',
      dataType: 'json',
      method: 'POST',
      data: { TableName: params.tableName, type: 'FiscalQtr', search: params.term },
      tags: true,
      success: function (data) {
        FiscalSelector(fiscalQuarterSelectableId, 'init', data.results);

        //Instantiate Fiscal Quarter Selector Box
        $(fiscalQuarterSelectableId).selectable({
          cancel: '#X,.cancel', //This prevents the X buttons from filtering or clearing any quarters selected
          stop: function (event, ui) {
            //Clear the filter_fiscalqtr array for a fresh start everytime.
            if (exclude == false) {
              filter_fiscalqtr.length = 0;
            } else {
              exclude_fiscalqtr.length = 0;
            }

            //Get all of the quarters that are currently selected and push them into the filter_fiscalqtr array
            FiscalSelector(fiscalQuarterSelectableId, 'get-selected', filter_fiscalqtr, 'include');
            FiscalSelector(fiscalQuarterSelectableId, 'get-selected', exclude_fiscalqtr, 'exclude');

            //Add the filter to be updated with all other filters if multi-selecting
            if (exclude == false) {
              //check if exclude list has new values and remove if it does
              for (i = 0; i < exclude_fiscalqtr.length; i++) {
                for (j = 0; j < filter_fiscalqtr.length; j++)
                  if (exclude_fiscalqtr[i] == '!' + filter_fiscalqtr[j]) {
                    exclude_fiscalqtr.splice(i, 1);
                  }
              }
              AddFilterToFilterList('fiscalqtr', filter_fiscalqtr);
            } else {
              //check if include list has values and remove if it does
              for (i = 0; i < filter_fiscalqtr.length; i++) {
                for (j = 0; j < exclude_fiscalqtr.length; j++)
                  if ('!' + filter_fiscalqtr[i] == exclude_fiscalqtr[j]) {
                    filter_fiscalqtr.splice(i, 1);
                  }
              }
              AddFilterToExcludeFilterList('fiscalqtr', exclude_fiscalqtr);
            }

            //Only start the filter process if the user isn't holding the Ctrl key down
            if (!event.ctrlKey) {
              ShowProcessingLoader();
              if (exclude == false) {
                UpdateFilter('fiscalqtr', filter_fiscalqtr);
                filters_selected.length = 0;
              } else {
                UpdateFilterExclude('fiscalqtr', exclude_fiscalqtr);
                filters_selected.length = 0;
              }
            }
          },
        });

        //If there are any filters selected upon page reload and first time opening the fiscal quarter selector box
        //we set the filters that are in state as seleted
        if (exclude == false) {
          FiscalSelector(fiscalQuarterSelectableId, 'select', filter_fiscalqtr, 'include');
          FiscalSelector(fiscalQuarterSelectableId, 'select', exclude_fiscalqtr, 'exclude');
          if (DEBUG) console.log('line 5772');
        } else {
          FiscalSelector(fiscalQuarterSelectableId, 'select', exclude_fiscalqtr);
          FiscalSelector(fiscalQuarterSelectableId, 'select', filter_fiscalqtr);
          if (DEBUG) console.log('line 5777');
        }
        ShowFiscalQuarterHeader();
      },
    });
  } else {
    ShowFiscalQuarterHeader();
  }
}
/**
 * This function sets up the fiscal week selector box.
 */
function ShowFiscalWeekSelectorBox() {
  if (DEBUG) console.log('ShowFiscalWeekSelectorBox()');
  var fiscalWeekSelectableId = '#fiscal-wk-selectable';
  var weekChildCount = $(fiscalWeekSelectableId)[0].childElementCount;

  //We check to see if the fiscal week selector box hasn't already been created to prevent
  //anyone from creating it again and again.
  if (weekChildCount < 1) {
    $.ajax({
      url: '/Home/GetFilterData',
      dataType: 'json',
      method: 'POST',
      data: { TableName: params.tableName, type: 'FiscalWk', search: params.term },
      tags: true,
      success: function (data) {
        FiscalSelector(fiscalWeekSelectableId, 'init', data.results);

        //Instantiate Fiscal Week Selector Box
        $(fiscalWeekSelectableId).selectable({
          cancel: '#X,.cancel', //This prevents the X buttons from filtering or clearing any weeks selected
          stop: function (event, ui) {
            //Clear the filter_fiscalwk array for a fresh start everytime.

            if (exclude == false) {
              filter_fiscalwk.length = 0;
            } else {
              exclude_fiscalwk.length = 0;
            }

            //Unselect any X weeks that were seleted
            FiscalSelector(fiscalWeekSelectableId, 'unselect-x');

            //Get all of the weeks that are currently selected and push them into the filter_fiscalwk array
            FiscalSelector(fiscalWeekSelectableId, 'get-selected', filter_fiscalwk, 'include');
            FiscalSelector(fiscalWeekSelectableId, 'get-selected', exclude_fiscalwk, 'exclude');

            //Add the filter to be updated with all other filters if multi-selecting
            if (exclude == false) {
              AddFilterToFilterList('fiscalwk', filter_fiscalwk);
              if (DEBUG) console.log('AddFilterToExcludeFilterList line 5828');
            } else {
              AddFilterToExcludeFilterList('fiscalwk', exclude_fiscalwk);
              if (DEBUG) console.log('AddFilterToExcludeFilterList line 5831');
            }

            //Only start the filter process if the user isn't holding the Ctrl key down
            if (!event.ctrlKey) {
              ShowProcessingLoader();
              if (DEBUG) console.log('line 5677');
              if (exclude == false) {
                UpdateFilter('fiscalwk', filter_fiscalwk);
                if (DEBUG) console.log('line 5837');
                filters_selected.length = 0;
              } else {
                UpdateFilterExclude('fiscalwk', exclude_fiscalwk);
                if (DEBUG) console.log('line 5840');
                filters_selected.length = 0;
              }
            }
          },
        });

        //If there are any filters selected upon page reload and first time opening the fiscal week selector box
        //we set the filters that are in state as seleted
        if (exclude == false) {
          FiscalSelector(fiscalWeekSelectableId, 'select', filter_fiscalwk, 'include');
          FiscalSelector(fiscalWeekSelectableId, 'select', exclude_fiscalwk, 'exclude');
        } else {
          FiscalSelector(fiscalWeekSelectableId, 'select', exclude_fiscalwk, 'exclude');
          FiscalSelector(fiscalWeekSelectableId, 'select', filter_fiscalwk, 'include');
        }
        ShowFiscalWeekHeader();
      },
    });
  } else {
    ShowFiscalWeekHeader();
  }
}

/**
 *
 * This function creates/updates/gets any weeks/months/quartes in the fiscal selector box for a given id. More things to do with the
 * fiscal boxes should be added here with associated options.
 *
 * @param {any} id The id of a fiscal selector box. You do not need to provide the # prefix
 * but you can if you want.
 *
 * @param {any} option An option could be any of the following:
 * init           -> Initialize the fiscal week selector by creating all the buttons
 * clear          -> Clears the selected buttons
 * select         -> Set the provided weeks/months/quarters as selected. This can either be an array or an integer or a string
 * toggle         -> Toggles the selected state of the given id
 * unselect       -> Unselect the provided weeks of either an array or an integer/string
 * unselect-all   -> Unselect all selected items
 * unselect-x     -> Unselect all X buttons
 * get-selected   -> Get all selected weeks and put them into the data array provided
 * get-unselected -> Get all unselected weeks and put them into the data array provided
 *
 * @param {any} data An array of numbers or single number that will be used to set the buttons
 * according to the option provided
 * @param {any} type optional strng indicating if array is include or exclude for fiscal display
 */
function FiscalSelector(id, option, data, type) {
  if (id[0] === '#') {
    id = id.slice(1);
  }
  type = type || 0;
  var fiscalBoxId = '#' + id;
  var fiscalBoxUl = 'ul' + fiscalBoxId;
  var fiscalBoxUlLi = fiscalBoxUl + ' li#';

  var selectedElement = '.ui-widget-content.waves-effect.waves-light.ui-selectee.ui-selected';
  var excludeElement = '.ui-widget-content.waves-effect.waves-light.ui-selectee.ui-selected-exclude';
  var includeElement = '.ui-widget-content.waves-effect.waves-light.ui-selectee.ui-selected-include';
  var nonSelectedElement = '.ui-widget-content.waves-effect.waves-light.ui-selectee';

  var size = 0;
  if (id === 'fiscal-wk-selectable') {
    size = 60;
  } else if (id === 'fiscal-mo-selectable') {
    size = 12;
  } else if (id === 'fiscal-qtr-selectable') {
    size = 4;
  }

  if (option === 'init') {
    //Initialize the fiscal box. It checks to see if the box has already been created. If the box
    //already exists then it doesn't create it. This is to prevent recreating the box because it will just append
    //another box to the bottom of an existing one if not checked for existing boxes.
    var childCount = $(fiscalBoxId)[0].childElementCount;

    if (childCount < 1) {
      //Set the appropriate size for the selected fiscal box
      if (id === 'fiscal-wk-selectable') {
        size = 60;
      } else if (id === 'fiscal-mo-selectable') {
        size = 12;
      } else if (id === 'fiscal-qtr-selectable') {
        size = 4;
      }
      for (var i = 1; i <= size; i++) {
        if (i <= data.length) {
          $(fiscalBoxId).append('<li class="ui-widget-content waves-effect waves-light" id="' + i + '">' + i + '</li>');
        } else {
          $(fiscalBoxId).append('<li class="ui-widget-content waves-effect waves-light" id="X">X</li>');
        }
      }
    }
  } else if (option === 'clear') {
    if (DEBUG) console.log('CLEAR');

    //Clear the current selected items
    $(fiscalBoxUl + ' ' + selectedElement).each(function () {
      $(this).removeClass('ui-selected'); //this is now the default that get's changed to either include or exclude based on toggle
    });
    $(fiscalBoxUl + ' ' + excludeElement).each(function () {
      $(this).removeClass('ui-selected-exclude');
    });
    $(fiscalBoxUl + ' ' + includeElement).each(function () {
      $(this).removeClass('ui-selected-include');
    });
    //make sure either exclude or include filter wasn't cleared as well
    if (id === 'fiscal-wk-selectable') {
      FiscalSelector(id, 'select', filter_fiscalwk, 'include');
      FiscalSelector(id, 'select', exclude_fiscalwk, 'exclude'); //temp
    } else if (id === 'fiscal-mo-selectable') {
      FiscalSelector(id, 'select', filter_fiscalmo, 'include');
      FiscalSelector(id, 'select', exclude_fiscalmo, 'exclude');
    } else if (id === 'fiscal-qtr-selectable') {
      FiscalSelector(id, 'select', filter_fiscalqtr, 'include');
      FiscalSelector(id, 'select', exclude_fiscalqtr, 'exclude');
    }
  } else if (option === 'select') {
    if (DEBUG) console.log('SELECTING');
    //Set the items as selected
    if (Array.isArray(data)) {
      if (type == 'exclude') {
        $.each(data, function (key, value) {
          value = value.replace('!', '');
          $(getNonSelectedName(value)).addClass('ui-selected-exclude');
        });
      } else {
        $.each(data, function (key, value) {
          $(getNonSelectedName(value)).addClass('ui-selected-include');
        });
      }
    } else {
      if (type == 'exclude') {
        data = data.replace('!', '');
        $(getNonSelectedName(data)).addClass('ui-selected-exclude');
      } else {
        data = data.replace('!', '');
        $(getNonSelectedName(data)).addClass('ui-selected-include');
      }
    }
  } else if (option === 'toggle') {
    //Toggles the state of a fiscal item (month/quarter/week)
    if ($(fiscalBoxUlLi + data).hasClass('ui-selected-include')) {
      $(getSelectedName(data)).removeClass('ui-selected-include');
    } else {
      $(getSelectedName(data)).addClass('ui-selected-include');
    }
  } else if (option === 'unselect') {
    if (DEBUG) console.log('unselect called ');
    //Set the items as unselected
    if (Array.isArray(data)) {
      if (type == 'exclude') {
        $.each(data, function (key, value) {
          $(getSelectedNameExclude(value)).removeClass('ui-selected-exclude');
        });
      } else {
        $.each(data, function (key, value) {
          $(getSelectedNameInclude(value)).removeClass('ui-selected-include');
        });
      }
    } else {
      if (type == 'exclude') {
        $(getSelectedNameExclude(data)).removeClass('ui-selected-exclude');
      } else {
        $(getSelectedNameInclude(data)).removeClass('ui-selected-include');
      }
    }
  } else if (option === 'unselect-all') {
    //remove all that are in the exclude array
    if (Array.isArray(data)) {
      if (type == 'exclude') {
        //remove !
        $.each(data, function (key, value) {
          var value2 = value.replace('!', '');
          $(getSelectedNameExclude(value2)).removeClass('ui-selected-exclude');
        });
      } else {
        $.each(data, function (key, value) {
          $(getSelectedNameInclude(value)).removeClass('ui-selected-include');
        });
      }
    } else {
      if (type == 'exclude') {
        //remove !
        var data2 = data.replace('!', '');
        $(getSelectedNameExclude(data2)).removeClass('ui-selected-exclude');
      } else {
        $(getSelectedNameInclude(data)).removeClass('ui-selected-include');
      }
    }
    // $.each($(fiscalBoxUl + ' > ' + selectedElement), function (key, val) {
    //        $(val).removeClass('ui-selected-include');
    //});
  } else if (option === 'unselect-x') {
    if (exclude == false) {
      //X is the id of a placeholder box that can't be selected
      if (DEBUG) console.log('testing' + fiscalBoxUlLi + 'X' + selectedElement);
      $(fiscalBoxUlLi + 'X' + selectedElement).each(function () {
        $(this).removeClass('ui-selected-include');
      });
    } else {
      //X is the id of a placeholder box that can't be selected
      $(fiscalBoxUlLi + 'X' + selectedElement + '-exclude').each(function () {
        var a = this;
        $(this).removeClass('ui-selected-exclude');
      });
    }
  } else if (option === 'get-selected') {
    $(fiscalBoxUl + ' ' + selectedElement).each(function () {
      //The fiscal element filter box has a bunch of X's in the end to fill in some empty spaces so we don't want these values, we only want the numbers.
      if (this.id != 'X') {
        //Each element has an id of its week/month/quarter number so we just get that
        if (size == 60) {
          fiscalHelper(this.id, filter_fiscalwk, exclude_fiscalwk);
        } else if (size == 12) {
          fiscalHelper(this.id, filter_fiscalmo, exclude_fiscalmo);
        } else if (size == 4) {
          fiscalHelper(this.id, filter_fiscalqtr, exclude_fiscalqtr);
        }
      }
    });
  } else if (option === 'get-unselected') {
    $(fiscalBoxUl + ' ' + nonSelectedElement).each(function () {
      //The fiscal element filter box has a bunch of X's in the end to fill in some empty spaces so
      //we don't want these values, we only want the numbers.
      if (this.id != 'X') {
        //Each element has an id of its week/month/quarter number so we just get that
        data.push('!' + this.id);
      }
    });
  }

  //Returns the name of an element that has already been selected. The element could be a week, month or quarter
  function getSelectedName(id) {
    return fiscalBoxUlLi + id + selectedElement;
  }

  //Returns the name of an element that has already been selected. The element could be a week, month or quarter
  function getSelectedNameExclude(id) {
    return fiscalBoxUlLi + id + excludeElement;
  }
  //Returns the name of an element that has already been selected. The element could be a week, month or quarter
  function getSelectedNameInclude(id) {
    return fiscalBoxUlLi + id + includeElement;
  }

  //Returns the name of an element that hasn't already been selected. The element could be a week, month or quarter
  function getNonSelectedName(id) {
    return fiscalBoxUlLi + id + nonSelectedElement;
  }

  //Helper functions for get-selected option
  function fiscalHelper(id, inc, exc) {
    if (exclude == false) {
      //if ctrl is down we want to keep all ui-selected-include in fiscalwk
      if (fiscal_select_ctrl_event == true) {
        //check for previously selected include values
        $(fiscalBoxUl + ' ' + includeElement).each(function () {
          //Each element has an id of its week/month/quarter number so we just get that
          if (!inc.includes(this.id)) {
            inc.push(this.id);
          }
        });
      }

      fiscal_select_mouse_click = false;
      $('.drag-selectable').on('selectableselecting', function (event, ui) {
        fiscal_select_ctrl_event = event.ctrlKey ? true : false;
        fiscal_select_mouse_click = true;
      });

      AddItemToArray(inc, id);
      if (fiscal_select_ctrl_event == false && !fiscal_select_mouse_click) {
        $(fiscalBoxUl + ' .ui-widget-content.waves-effect.waves-light.ui-selectee.ui-selected-include').each(function () {
          if (!inc.includes(this.id)) {
            $(getSelectedNameInclude(this.id)).removeClass('ui-selected-include');
          }
        });
      }

      //if in exclude filter remove
      if (exc.includes('!' + id)) {
        RemoveItemFromArray(exc, '!' + id);
        AddItemToArray(inc, id);
        $(getSelectedName(id)).removeClass('ui-selected-exclude'); //remove exclude
        $(getSelectedName(id)).addClass('ui-selected-include'); //add its new class value
        $(getSelectedName(id)).removeClass('ui-selected'); //remove the default we check for
      }
      //if id isn't in either array add to include
      else {
        $(getSelectedName(id)).addClass('ui-selected-include');
        $(getSelectedName(id)).removeClass('ui-selected');
      }

      //if one value was selected for include make sure all exclude values are reselected
      for (i = 0; i < exc.length; i++) {
        var value = exc[i].replace('!', '');
        $(getNonSelectedName(value)).addClass('ui-selected-exclude');
        $(getSelectedName(id)).removeClass('ui-selected');
      }
    }
    //if toggled to exclude
    else if (exclude == true) {
      if (fiscal_select_ctrl_event == true) {
        //check for previously selected include values
        $(fiscalBoxUl + ' ' + excludeElement).each(function () {
          //Each element has an id of its week/month/quarter number so we just get that
          if (!exc.includes('!' + this.id)) {
            exc.push('!' + this.id);
          }
        });
      }

      fiscal_select_mouse_click = false;
      $('.drag-selectable').on('selectableselecting', function (event, ui) {
        fiscal_select_ctrl_event = event.ctrlKey ? true : false;
        fiscal_select_mouse_click = true;
      });
      AddItemToArray(exc, '!' + id);
      if (fiscal_select_ctrl_event == false && !fiscal_select_mouse_click) {
        $(fiscalBoxUl + ' .ui-widget-content.waves-effect.waves-light.ui-selectee.ui-selected-exclude').each(function () {
          if (!exc.includes('!' + this.id)) {
            $(getSelectedNameExclude(this.id)).removeClass('ui-selected-exclude');
          }
        });
      }

      //if in include filter remove and add to exclude
      if (inc.includes(id)) {
        RemoveItemFromArray(inc, id);
        AddItemToArray(exc, '!' + id);
        $(getSelectedName(id)).addClass('ui-selected-exclude');
        $(getSelectedName(id)).removeClass('ui-selected');
        $(getSelectedNameInclude(id)).removeClass('ui-selected-include');
      }
      //if id isn't in either array add to exclude
      else {
        $(getSelectedName(id)).addClass('ui-selected-exclude');
        $(getSelectedName(id)).removeClass('ui-selected');
      }
      //if one value was selected for exclude make sure all include values are reselected
      for (i = 0; i < inc.length; i++) {
        var value = inc[i];
        $(getNonSelectedName(value)).addClass('ui-selected-include');
      }
    }
  }
}

/**
 * This function deletes the selected filter chips, filters the table by the new
 * filters selected and creates new chips.
 * @param {any} filterId This should be the id of the filter header.
 * @param {any} filterData Filter data that should be separated by commas ','.
 */
function UpdateFilter(filterId, filterData) {
  var filterName = GetFilterNameFromId(filterId);

  var excludeFilterData = window['exclude_' + filterName];

  // We show the filter header hear if there are any filters selected.
  // This is handled here to avoid calling this in every select2 list
  if (filterData.length > 0) {
    ShowFilterChipHeader(filterName);
  }
  if (excludeFilterData.length > 0) {
    ShowFilterChipHeaderExclude(filterName);
  }

  // Clear any chips in the filter body
  ClearFilterChips(filterName);
  ClearFilterChipsExclude(filterName);

  // The column name in the datatable is different from the filter name so we need its
  // corresponding column name to clear the filter in the datatable
  var columnName = GetFilterColumnName(filterName);

  var temp = window['filter_' + filterName].concat(window['exclude_' + filterName]);

  // Clear the filter in the datatable otherwise it'll just come back upon page reload
  DTable.columns(columnName + ':name').search(temp, false, false);

  // This is to repopulate the chips with whatever elements are left in the current filter
  $.each(filterData, function (index, value) {
    value = value.replace(/\"/gm, '');
    AddNewFilterBoxGroup(value, 'filter-head-' + filterName, 'filter-body-' + filterName);
    AddNewFilter(value, 'filter-body-' + filterName, filterName);
    if (DEBUG) console.log('AddNewFilte ' + value);
  });

  // This is to repopulate the chips with whatever elements are left in the current filter
  $.each(excludeFilterData, function (index, value) {
    value = value.replace(/\"/gm, '');
    AddNewFilterBoxGroupExclude(value, 'filter-head-exclude-' + filterName, 'filter-body-exclude-' + filterName);
    AddNewFilterExclude(value, 'filter-body-exclude-' + filterName, filterName);
    if (DEBUG) console.log('AddNewFilte ' + value);
  });

  // This is mainly for the fiscal box selectors.
  // If all the weeks/mondth/quartes have been unselected then hide the fiscal box chip headers
  if (filterData.length < 1) {
    HideFilterChipHeader(filterName);

    //If there are any selected buttons then we remove the highlighting from them.
    //This is mostly intended for left over X's that are selected but just incase there are
    //any weeks selected then we'll unselect them too
    if (filterName === 'fiscalwk' && FiscalSelector('fiscal-wk-selectable', 'clear', 'include')) {
    } else if (filterName === 'fiscalmo' && FiscalSelector('fiscal-mo-selectable', 'clear', 'include')) {
    } else if (filterName === 'fiscalqtr' && FiscalSelector('fiscal-qtr-selectable', 'clear', 'include')) {
    }
  }

  if (excludeFilterData.length < 1) {
    HideFilterChipHeaderExclude(filterName);
  }

  // In the event of a filtering event even if the table doesn't reload then set the table as filtering to avoid issues
  // with other possible locations that will call a redraw on the table. This will make sure we get a new sums row.
  SetTableFiltering(true);

  //Only update the table if the user isn't currently holding the Ctrl key
  if (!fiscal_select_ctrl_event && !filter_ctrl_key) {
    RunWithUpdater(DTable.columns.adjust().draw, 0);
  }
}

//getting passed fiscalwk, 10
function UpdateFilterExclude(filterId, filterData) {
  var filterName = GetFilterNameFromId(filterId);
  var includeFilterData = window['filter_' + filterName];
  // We show the filter header hear if there are any filters selected.
  // This is handled here to avoid calling this in every select2 list
  if (filterData.length > 0) {
    ShowFilterChipHeaderExclude(filterName);
  }
  if (includeFilterData.length > 0) {
    ShowFilterChipHeader(filterName);
  }

  // Clear any chips in the filter body
  ClearFilterChipsExclude(filterName);
  ClearFilterChips(filterName);

  // The column name in the datatable is different from the filter name so we need its
  // corresponding column name to clear the filter in the datatable
  var columnName = GetFilterColumnName(filterName);

  // Clear the filter in the datatable otherwise it'll just come back upon page reload
  var temp = window['filter_' + filterName].concat(window['exclude_' + filterName]);
  DTable.columns(columnName + ':name').search(temp, false, false);

  // This is to repopulate the chips with whatever elements are left in the current filter
  $.each(filterData, function (index, value) {
    value = value.replace(/\"/gm, '');
    AddNewFilterBoxGroupExclude(value, 'filter-head-exclude-' + filterName, 'filter-body-exclude-' + filterName);
    AddNewFilterExclude(value, 'filter-body-exclude-' + filterName, filterName);
  });

  // This is to repopulate the chips with whatever elements are left in the current filter
  $.each(includeFilterData, function (index, value) {
    value = value.replace(/\"/gm, '');
    AddNewFilterBoxGroup(value, 'filter-head-' + filterName, 'filter-body-' + filterName);
    AddNewFilter(value, 'filter-body-' + filterName, filterName);
  });

  // This is mainly for the fiscal box selectors.
  // If all the weeks/mondth/quartes have been unselected then hide the fiscal box chip headers
  if (filterData.length < 1) {
    HideFilterChipHeaderExclude(filterName);
    //If there are any selected buttons then we remove the highlighting from them.
    //This is mostly intended for left over X's that are selected but just incase there are
    //any weeks selected then we'll unselect them too
    if (filterName === 'fiscalwk' && FiscalSelector('fiscal-wk-selectable', 'clear', 'exclude')) {
    } else if (filterName === 'fiscalmo' && FiscalSelector('fiscal-mo-selectable', 'clear', 'exclude')) {
    } else if (filterName === 'fiscalqtr' && FiscalSelector('fiscal-qtr-selectable', 'clear', 'exclude')) {
    }
  }
  if (includeFilterData.length < 1) {
    HideFilterChipHeader(filterName);
  }

  // In the event of a filtering event even if the table doesn't reload then set the table as filtering to avoid issues
  // with other possible locations that will call a redraw on the table. This will make sure we get a new sums row.
  SetTableFiltering(true);

  //Only update the table if the user isn't currently holding the Ctrl key
  if (!fiscal_select_ctrl_event && !filter_ctrl_key) {
    RunWithUpdater(DTable.columns.adjust().draw, 0);
    // DTable.columns.adjust().draw();
  }
}

//Hides a filter chip header based on the given filter name
function HideFilterChipHeader(filterName) {
  $('#filter-head-' + filterName).hide();
}

//Hides a filter chip header based on the given filter name
function HideFilterChipHeaderExclude(filterName) {
  $('#filter-head-exclude-' + filterName).hide();
}

//Hide fiscal month header
function HideFiscalMonthHeader() {
  if ($('#fiscal-mo-selectable-header').hasClass('active')) {
    $('#fiscal-mo-selectable-header').click();
  }
}

//Hide fiscal quarter header
function HideFiscalQuarterHeader() {
  if ($('#fiscal-qtr-selectable-header').hasClass('active')) {
    $('#fiscal-qtr-selectable-header').click();
  }
}

//Hide fiscal week header
function HideFiscalWeekHeader() {
  if ($('#fiscal-wk-selectable-header').hasClass('active')) {
    $('#fiscal-wk-selectable-header').click();
  }
}

/**
 * Show a filter chip header
 * @param {any} filterId
 */
function ShowFilterChipHeader(filterId) {
  var filterName = '#filter-head-' + GetFilterNameFromId(filterId);
  $(filterName).show();

  //Even showing the header doesn't show the body so it must be clicked on
  //to show the filters after ading one
  if ($(filterName).hasClass('active') === false) {
    $(filterName).click();
  }
}

function ShowFilterChipHeaderExclude(filterId) {
  var filterName = '#filter-head-exclude-' + GetFilterNameFromId(filterId);
  $(filterName).show();

  //Even showing the header doesn't show the body so it must be clicked on
  //to show the filters after ading one
  if ($(filterName).hasClass('active') === false) {
    $(filterName).click();
  }
}

//Show the fiscal month filter chips header
function ShowFiscalMonthChipHeader() {
  $('#filter-head-fiscalmo').show();

  //Even showing the header doesn't show the body so it must be clicked on
  //to show the filters after ading one
  if ($('#filter-head-fiscalmo').hasClass('active') === false) {
    $('#filter-head-fiscalmo').click();
  }
}

//Show the fiscal quarter filter chips header
function ShowFiscalQuarterChipHeader() {
  $('#filter-head-fiscalqtr').show();

  //Even showing the header doesn't show the body so it must be clicked on
  //to show the filters after ading one
  if ($('#filter-head-fiscalqtr').hasClass('active') === false) {
    $('#filter-head-fiscalqtr').click();
  }
}

//Show the fiscal week filter chips header
function ShowFiscalWeekChipHeader() {
  $('#filter-head-fiscalwk').show();

  //Even showing the header doesn't show the body so it must be clicked on
  //to show the filters after ading one
  if ($('#filter-head-fiscalwk').hasClass('active') === false) {
    $('#filter-head-fiscalwk').click();
  }
}

//Show fiscal month header
function ShowFiscalMonthHeader() {
  if (!$('#fiscal-mo-selectable-header').hasClass('active')) {
    $('#fiscal-mo-selectable-header').click();
  }
}

//Show fiscal quarter header
function ShowFiscalQuarterHeader() {
  if (!$('#fiscal-qtr-selectable-header').hasClass('active')) {
    $('#fiscal-qtr-selectable-header').click();
  }
}

//Show fiscal week header
function ShowFiscalWeekHeader() {
  if (!$('#fiscal-wk-selectable-header').hasClass('active')) {
    $('#fiscal-wk-selectable-header').click();
  }
}

/**
 * Clears the chips from the element id provided
 * @param {any} filterId The id of the collapsible body to clear the chips from
 */
function ClearFilterChips(filterId) {
  var filterHeadName = GetFilterNameFromId(filterId);
  $('#filter-body-' + filterHeadName)
    .children()
    .each(function () {
      $(this).remove();
    });
}

function ClearFilterChipsExclude(filterId) {
  var filterHeadName = GetFilterNameFromId(filterId);
  $('#filter-body-exclude-' + filterHeadName)
    .children()
    .each(function () {
      $(this).remove();
    });
}

//***************************************************************
//*          End Fiscal Week Selector Box Functions
//***************************************************************

//********************************************
//        Random Formatting functions.
//********************************************

/**
 * Function to format a value with sarrounding double quotes. This is needed to
 * prevent splitting on a comma in the value.
 * @param {any} data
 */
function formatForCommas(data) {
  return '"' + data + '"';
}

function formatCurrency(num) {
  num = num.toString().replace(/\$|\,/g, '');
  if (isNaN(num)) num = '0';
  sign = num == (num = Math.abs(num));
  num = Math.floor(num * 100 + 0.50000000001);
  cents = num % 100;
  if (cents === '0') cents = '';
  else cents = '.' + cents;
  num = Math.floor(num / 100).toString();
  for (var i = 0; i < Math.floor((num.length - (1 + i)) / 3); i++)
    num = num.substring(0, num.length - (4 * i + 3)) + ',' + num.substring(num.length - (4 * i + 3));
  return (sign ? '' : '-') + '$' + num + cents;
}

// Converts the input into a percentage with 1 precision
function formatDecimalPercent(num) {
  if (num === 'N/A') {
    return '0%';
  } else {
    return parseFloat(num).toFixed(1) + '%';
  }
}

// Converts the input into a number with commas every 3 characters
function formatNumberComma(num) {
  return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ',');
}

///
/// Removes duplicate values from a string array
///
function RemoveDuplicatesFromArray(arr) {
  var i,
    len = arr.length,
    result = [],
    obj = {};
  for (i = 0; i < len; i++) {
    obj[arr[i]] = 0;
  }
  for (i in obj) {
    result.push(i);
  }
  return result;
}

///
/// Removes a string from an array.
/// Use like:
///     var ary = ['three', 'seven', 'eleven'];
///     ary.remove('seven')
///
Object.defineProperty(Array.prototype, 'remove', {
  enumerable: false,
  value: function () {
    var what,
      a = arguments,
      L = a.length,
      ax;
    while (L && this.length) {
      what = a[--L];
      while ((ax = this.indexOf(what)) != -1) {
        this.splice(ax, 1);
      }
    }
    return this;
  },
});

// This is a prototype that destroys the fixed column object
$.fn.dataTable.FixedColumns.prototype.destroy = function () {
  var nodes = ['body', 'footer', 'header'];

  //remove the cloned nodes
  for (var i = 0, l = nodes.length; i < l; i++) {
    if (this.dom.clone.left[nodes[i]]) {
      this.dom.clone.left[nodes[i]].parentNode.removeChild(this.dom.clone.left[nodes[i]]);
    }
    if (this.dom.clone.right[nodes[i]]) {
      this.dom.clone.right[nodes[i]].parentNode.removeChild(this.dom.clone.right[nodes[i]]);
    }
  }

  //remove event handlers
  $(this.s.dt.nTable).off('column-sizing.dt.DTFC destroy.dt.DTFC draw.dt.DTFC');

  $(this.dom.scroller).off('scroll.DTFC mouseover.DTFC');
  $(window).off('resize.DTFC');

  $(this.dom.grid.left.liner).off('scroll.DTFC wheel.DTFC mouseover.DTFC');
  $(this.dom.grid.left.wrapper).remove();

  $(this.dom.grid.right.liner).off('scroll.DTFC wheel.DTFC mouseover.DTFC');
  $(this.dom.grid.right.wrapper).remove();

  $(this.dom.body).off('mousedown.FC mouseup.FC mouseover.FC click.FC');

  //remove DOM elements
  var $scroller = $(this.dom.scroller).parent();
  var $wrapper = $(this.dom.scroller).closest('.DTFC_ScrollWrapper');
  $scroller.insertBefore($wrapper);
  $wrapper.remove();

  //cleanup variables for GC
  //delete this.s;
  //delete this.dom;
};

/********************************************
        Upload/Download functions.
********************************************/

function RunFullDownloadExport(exportSelection, paramsType) {
  paramsType = paramsType ? paramsType : '';
  var data = paramsType === 'IPOTable' ? ExceptionsTabModule.ipoOverlapTable.params() : params;
  data.gmsvenid = parseInt(document.getElementById('GMSVenID').value);
  data.ExportChoice = exportSelection;

  $('.preloader-background').fadeIn('slow');
  $('.preloader-wrapper').fadeIn('slow');

  $.ajax({
    url: '/Home/RunExport',
    dataType: 'JSON',
    method: 'POST',
    data: data,
    success: function (data) {
      if (DEBUG) console.log('Export success');
      if (ForecastDebugger) {
        ForecastDebugger.setDownloadedFile(exportSelection, data.fileName);
      }
      window.location.href = '/Home/DownloadFile?fileName=' + data.fileName;
    },
    complete: function () {
      $('.preloader-wrapper').delay(2000).fadeOut();

      $('.preloader-background').delay(2000).fadeOut('slow');
    },
  });
}

//*********************************************
//*          Bookmark Restoring               *
//*********************************************

/**
 * Function to modify the state of a DataTable state string (a Bookmark).
 * There are a few options such as:
 * col-reorder-to-name      -> Converts the ColReorder integer indexes into their coresponding column names.
 * col-reorder-to-index     -> Converts the ColReorder column name indexes into their coresponding DataTable column indexes.
 * update-columns           -> Updates the column objects, column Reorder, and Rotator column objects based on another state.
 * stringify                -> Stringifies the state and sets the ColReorder indexes to their column names.
 *
 * @param {any} oldState The DataTable state string in question that might need to be updated or its ColReorder modified.
 * @param {any} option The option you would like to select and they could be:
 *      col-reorder-to-name      -> Converts the ColReorder integer indexes into their coresponding column names.
 *      col-reorder-to-index     -> Converts the ColReorder column name indexes into their coresponding DataTable column indexes.
 *      update-columns           -> Updates the column objects, column Reorder, and Rotator column objects based on another state.
 *      stringify                -> Stringifies the state and sets the ColReorder indexes to their column names.
 * @param {any} newState The new DataTable state that will be used to update the old DataTable state string.
 */
function Bookmark(oldState, option, newState) {
  newState = newState || oldState;

  if (option === 'col-reorder-to-name') {
    oldState = UpdateColIndexToName(oldState);
  } else if (option === 'col-reorder-to-index') {
    oldState = UpdateColNameToIndex(oldState);
  } else if (option === 'update-columns') {
    oldState = UpdateStateColumns(oldState, newState);
  } else if (option === 'stringify') {
    if (typeof oldState === 'string') {
      oldState = JSON.stringify(oldState);
      oldState = UpdateColIndexToName(oldState);
    }
  }

  return oldState;
}

/**
 * Function to check if either table states are different from one
 * another in terms of column count and position.
 * @param {any} oldState The state string from the DataTable as a raw string
 * un parsed.
 * @param {any} newState The state string from the DataTable as a raw string
 * un parsed.
 */
function IsTableStateModified(oldState, newState) {
  // We need to convert each state into arrays of column objects
  var oldTableState = GetStateColumnObjs(oldState);
  var newTableState = GetStateColumnObjs(newState);

  // If both states don't have the same amount of columns then the
  // table has either added or removed a column so the table state
  // is now changed.
  if (oldTableState.length != newTableState.length) {
    return true;
  }

  // Loop through both states since they're the same length.
  // If any columns arent in the same position then the column layout has changed
  // and therefore the table has changed.
  for (var i = 0; i < oldTableState.length; i++) {
    if (oldTableState[i].name !== newTableState[i].name) {
      return true;
    }
  }

  // If we get this far then the table state in terms of column amount
  // and column positino hasn't changed so the table state hasn't changed.
  return false;
}

/**
 * Function that gets the columns objects out of the DataTable state.
 * @param {any} state The raw state string un-parsed.
 */
function GetStateColumnObjs(state) {
  var columnObjs = [];

  var m = columnObjPattern.exec(state);

  while (m !== null) {
    // This is necessary to avoid infinite loops with zero-width matches
    if (m.index === columnObjPattern.lastIndex) {
      columnObjPattern.lastIndex++;
    }

    columnObjs = JSON.parse(m[0]);

    m = columnObjPattern.exec(state);
  }

  return columnObjs;
}

/**
 * Function that gets the ColReorder as an array of integers.
 * @param {any} state The raw state string un-parsed.
 */
function GetStateColumnOrder(state) {
  var colOrderArr = [];

  var m = colReorderPattern.exec(state);

  while (m !== null) {
    if (m.index === colReorderPattern.lastIndex) {
      colReorderPattern.lastIndex++;
    }

    colOrderArr = JSON.parse(m[1]);

    m = colReorderPattern.exec(state);
  }

  return colOrderArr;
}

/**
 * Function that gets the Rotator as an array of rotator column objects.
 * @param {any} state The raw state string un-parsed.
 */
function GetStateRotatorColumns(state) {
  var rotatorCols = [];

  var m = stateRotatorPattern.exec(state);

  while (m !== null) {
    if (m.index === stateRotatorPattern.lastIndex) {
      stateRotatorPattern.lastIndex++;
    }

    rotatorCols = JSON.parse(m[1]);

    m = stateRotatorPattern.exec(state);
  }

  return rotatorCols;
}

/**
 * A function to check if a list contains a certain item.
 * @param {any} list The list you want to check the item against.
 * @param {any} item The item you are looking for in the list.
 */
function ListContains(list, item) {
  for (var i = 0; i < list.length; i++) {
    var list2 = Object.assign({}, list);
    list2[i] = list2[i].replace('!', '');
    if (list2[i] === item) {
      return true;
    }
  }

  return false;
}

function PatchParentColumns(state) {
  if (!state) {
    return state;
  }

  if (state.columns) {
    state.columns = state.columns.map(function (column) {
      if (column.name.indexOf('Category') !== -1) {
        column.name = column.name.replace('Category', 'Parent');
      }
      return column;
    });
  }

  if (state.rotator) {
    state.rotator = state.rotator.map(function (r) {
      if (r.column.indexOf('Category') !== -1) {
        r.column = r.column.replace('Category', 'Parent');
      }
      return r;
    });
  }

  return state;
}

/**
 * Function to replace the colReorder index array with the index's corresponding
 * column names. If the state string contains the colReorder indexs as column names already
 * then it'll ignore it and return the original state string.
 * @param {any} state The DataTable state string un formatted.
 */
function UpdateColIndexToName(state) {
  var colIndexArray = GetStateColumnOrder(state);
  var colObjsArray = GetStateColumnObjs(state);
  var colNamesArray = [];

  // Check to see if the state string contains the colReorder as integer indexes
  // if not then it must already have the indexes as column names.
  if (colIndexArray.length > 0 && typeof colIndexArray[0] === 'number') {
    // Here we get the column names from each index in the colReorder array
    // by accessing each colum objects data/name properties
    for (var i = 0; i < colIndexArray.length; i++) {
      // Index is the actual column index in the colReorder array.
      // Example: [2, 3, 44, 23] index could be 44.
      var index = colIndexArray[i];

      // Each column object has the above index in its data propertie so we filter
      // it out to get the corresponding column name.
      //var colObj = colObjsArray.filter(val => val.data === index); // Not compatible with IE
      var colObj = colObjsArray.filter(function (val) {
        return val.data === index;
      });

      if (colObj.length > 0) {
        colNamesArray.push('"' + colObj[0].name.toString() + '"');
      }
    }

    var colString = '"colReorder":[' + colNamesArray.toString() + ']';
    state = state.replace(colReorderPattern, colString);
  }

  return state;
}

/**
 * Function to replace the colReorder index array with the index's corresponding
 * column names. If the colReorder indexes are already integers then it'll return the original
 * state string.
 * @param {any} oldState A table state that usually predates the current table state. This
 * can be a bookmark that is used to be modified.
 * @param {any} newState This should be the latest table state that is used to get the column
 * indexes for the oldState column names. This, however, doesn't need to be a new state. This
 * simply be left blank and the oldState will be used.
 */
function UpdateColNameToIndex(oldState, newState) {
  newState = newState || oldState;

  var colNamesArray = GetStateColumnOrder(oldState);
  var colObjsArray = GetStateColumnObjs(newState);
  var colIndexArray = [];

  if (colNamesArray.length > 0 && typeof colNamesArray[0] === 'string') {
    // Here we get the column index from each name in the colReorder array
    // by accessing each colum objects data/name properties
    for (var i = 0; i < colNamesArray.length; i++) {
      // colName is the actual column name in the colReorder array.
      // Example: ["ForecastID", "ParentID", "ParentConcat", "ItemID"] name could be "ParentConcat".
      var colName = colNamesArray[i];

      // Each column object has the above index in its data propertie so we filter
      // it out to get the corresponding column name.
      //var colObj = colObjsArray.filter(val => val.name === colName);
      var colObj = colObjsArray.filter(function (val) {
        return val.name === colName;
      });

      if (colObj.length > 0 && (typeof colObj[0].data !== 'undefined' || colObj[0].data !== null)) {
        colIndexArray.push(colObj[0].data);
      }
    }

    var colString = '"colReorder":[' + colIndexArray.toString() + ']';
    oldState = oldState.replace(colReorderPattern, colString);
  }

  return oldState;
}

/**
 * Function to update the column object, column reorder, and rotator columns
 * for a given DataTable state string.
 * @param {any} oldState The old DataTable state string.
 * @param {any} newState The new DataTable state string to campare the old state
 * string against.
 */
function UpdateStateColumns(oldState, newState) {
  // Update column objects to reflect the new table state
  oldState = UpdateStateColObjs(oldState, newState);

  // Update ColReorder
  oldState = UpdateStateColReorder(oldState, newState);

  // Update Rotator
  oldState = UpdateStateRotator(oldState, newState);

  // Set the ColReorder back to using indexes ready to be used by the DataTables
  oldState = UpdateColNameToIndex(oldState);

  return oldState;
}

/**
 * Function to Update the DataTable state column objects with new ones provided by
 * the newState parameter.
 * @param {any} oldState The old DataTable state as a string. Such as a bookmark.
 * @param {any} newState The new DataTable state to campare the old one against.
 */
function UpdateStateColObjs(oldState, newState) {
  var oldStateCols = GetStateColumnObjs(oldState);
  var newStateCols = GetStateColumnObjs(newState);

  // This goes through the new table state and checks if the old state
  // contains the new existing/deleted columns and assigns their proper visibility
  // and order
  for (var i = 0; i < newStateCols.length; i++) {
    var newColName = newStateCols[i].name;
    //var oldColObj = oldStateCols.filter(col => col.name === newColName);
    var oldColObj = oldStateCols.filter(function (col) {
      return col.name === newColName;
    });

    // If the column doesn't exist in the old state then set it
    // to be invisible
    if (oldColObj.length <= 0) {
      newStateCols[i].visible = false;
    } else {
      oldColObj[0].data = newStateCols[i].data;
      newStateCols[i] = oldColObj[0];
    }
  }

  var colString = JSON.stringify(newStateCols);
  oldState = oldState.replace(columnObjPattern, colString);

  return oldState;
}

/**
 * Function to update the ColReorder in the old DataTable state string based on
 * the new DataTable state string. It keeps the column order from the old state string
 * and add any new columns to the end of the ColReorder array.
 * @param {any} oldState
 * @param {any} newState
 */
function UpdateStateColReorder(oldState, newState) {
  // Convert the colReorder indexes to names if they are numbers
  oldState = UpdateColIndexToName(oldState);
  var oldReorder = GetStateColumnOrder(oldState);

  newState = UpdateColIndexToName(newState);
  var newReorder = GetStateColumnOrder(newState);

  // This array holds any new columns that were added
  var addedReorderCols = [];
  // Check for new columns
  for (var j = 0; j < newReorder.length; j++) {
    var containsItem = ListContains(oldReorder, newReorder[j]);
    if (containsItem === false) {
      addedReorderCols.push(newReorder[j]);
    }
  }

  // Check if any columns from the old state string are missing from the new state string
  // and remove them from the old ColReorder array
  for (var i = 0; i < oldReorder.length; i++) {
    var containsOldItem = ListContains(newReorder, oldReorder[j]);
    if (containsOldItem === false) {
      oldReorder = oldReorder.splice(i, 1);
    }
  }

  // Add new columns to the end of the array
  $.each(addedReorderCols, function (key, val) {
    oldReorder.push(val);
  });

  var reorderString = '"ColReorder":' + JSON.stringify(oldReorder);
  oldState = oldState.replace(colReorderPattern, reorderString);

  return oldState;
}

/**
 * Function to update the old state rotator with any new state rotator.
 * @param {any} oldState The old DataTable state as a string.
 * @param {any} newState The new DataTable state as a string to give to the old state string.
 */
function UpdateStateRotator(oldState, newState) {
  // Get the rotators from both states
  var oldRotator = GetStateRotatorColumns(oldState);
  var newRotator = GetStateRotatorColumns(newState);

  // Only if the old state has a rotator will we actually compare and set the
  // included field
  if (oldRotator.length > 0) {
    for (var k = 0; k < newRotator.length; k++) {
      //var rotCol = oldRotator.filter(col => newRotator[k].column === col.column);
      var rotCol = oldRotator.filter(function (col) {
        return newRotator[k].column === col.column;
      });

      if (rotCol.length > 0) {
        newRotator[k].included = rotCol[0].included;
      } else {
        newRotator[k].included = false;
      }
    }
  } else {
    // The rotator doesn't exist in the old state string so just give it the new one
    $.each(newRotator, function (key, val) {
      val.included = false;
    });
  }

  // If the rotator exists then put the new one in its place
  if (oldRotator.length > 0) {
    var rotString = '"rotator":' + JSON.stringify(newRotator);
    oldState = oldState.replace(stateRotatorPattern, rotString);
  } // If the rotator doesn't exist then put it at the end of all other objects
  else {
    var rotString = ',"rotator":' + JSON.stringify(newRotator) + '}';
    oldState = oldState.replace(noRotatorPattern, rotString);
  }

  return oldState;
}

//*********************************************
//*        End Bookmark Restoring             *
//*********************************************

//*********************************************
//*         Multi Sorting                     *
//*********************************************

// This runs through every column in the DataTable and removes the default click event
// and assigns a custom one for multi-sorting columns
$.each(DTable.columns().header(), function (key, val) {
  $(val).unbind('click');
  // Event that handles single and multi column sorting.
  $(val).click(function (e) {
    PreventIfLoading(e);

    ShowProcessingLoader();
    if (DEBUG) console.log('line 6635');

    var visIdx = $(this).index(); //This is the visiable index based on the columns showing, remember there are invisible ones too
    var index = DTable.column.index('fromVisible', visIdx); //Get the actual index for this column. This includes visible and invisible.

    // Contains the previous and next sorting class
    var sorts = GetNextSortingDirection(this);

    // The actual sorting value that will be given to datatables
    var sortDir = sorts[1].split('_')[1];

    // The array that contains the column index and sort direction
    var columnSort = [index, sortDir === undefined ? '' : sortDir];

    setTimeout(function () {
      // If the sort option is empty then that is the default sort and
      // will be removed from the sort list if it exists.
      if (columnSort[1] === '') {
        RemoveColumnToSort(columnSort);
      } else {
        // Otherwise add/update it to/in the list
        AddColumnToSort(columnSort);
      }

      // This adds the column to the list without updating the table
      if (e.originalEvent.ctrlKey) {
        column_sort_ctrl_event = true;
        ToggleColumnSortArrow(this, sorts);
        UpdateColumnSortFilter(filter_columnsort);
      } else {
        // This section is for sortin a single column. It clears any previous column sorts if they exist.
        column_sort_ctrl_event = false;
        filter_columnsort.length = 0; // Clear any previous filters
        AddColumnToSort(columnSort); // Now add the current column back in
        UpdateColumnSortFilter(filter_columnsort); // Update the chips with the one column
        RunWithUpdater(DTable.order(filter_columnsort).draw, 0);
        // DTable.order(filter_columnsort).draw();
      }
    });
  });
});

/**
 * Function to remove a sort if it exists.
 * @param {any} colArray This shold be an array as such: [index, 'string which is optional'].
 */
function RemoveColumnToSort(colArray) {
  var index = ColumnSortListContains(colArray[0]);

  if (index > -1) {
    filter_columnsort.splice(index, 1);
  }
}

/**
 * Function to add a sort to the filter_columnsort filter if one doesn't already exist.
 * If it exists then it is updated in its exact index.
 * @param {any} colArray This should be an array as with these options:
 * [index, 'asc'] or [index, 'desc'] or this [int, ''] for default sort.
 */
function AddColumnToSort(colArray) {
  var index = ColumnSortListContains(colArray[0]);

  if (index > -1) {
    filter_columnsort.splice(index, 1, colArray);
  } else {
    filter_columnsort.push(colArray);
  }
}

/**
 * Check if a the filter_columnsort contains a certain sort array.
 * @param {any} colIndex This should be an integer that corresponds to the column index.
 */
function ColumnSortListContains(colIndex) {
  if (filter_columnsort.length > 0) {
    for (var i = 0; i < filter_columnsort.length; i++) {
      if (filter_columnsort[i][0] == colIndex) {
        return i;
      }
    }

    return -1;
  } else {
    return -1;
  }
}

/**
 * Function to toggle the sort direction arrow.
 * @param {any} element The element that was clicked on.
 * @param {any} array An array of a previous class to remove and the next
 * class to add. This should be used with the function GetNextSortingDirection.
 */
function ToggleColumnSortArrow(element, array) {
  $(element).removeClass(array[0]);
  $(element).addClass(array[1]);
}

/**
 * Function that gets the current sort direction and the next sort direction as a class
 * that will be given to the column. This class toggles the sort arrow.
 * @param {any} element This element is pretty specific. It should be passed in as the current
 * element that was clicked on.
 */
function GetNextSortingDirection(element) {
  var cList = $(element)[0].classList;

  if (ListContains(cList, 'sorting')) {
    return ['sorting', 'sorting_asc'];
  } else if (ListContains(cList, 'sorting_asc')) {
    return ['sorting_asc', 'sorting_desc'];
  } else if (ListContains(cList, 'sorting_desc')) {
    return ['sorting_desc', 'sorting'];
  }
}

/**
 * This function updates the filter chips for the filter_columnsort filter.
 * It numbers the chips and sets the text to display which columns are being sorted in what direction.
 *
 * @param {any} filterData Filter data that should be a 2D array as such [[0, 'asc'], [5, 'desc']].
 * Even it there's only one item/array in it, it should still be in another array
 */
function UpdateColumnSortFilter(filterData) {
  if (filterData.length > 0 && (filterData[0] === undefined || typeof filterData[0] !== 'object')) {
    return;
  }
  var filterName = 'columnsort';
  var filterValues = [];

  // Filter out any columns that are in default position. This way they're cleared from the filter dropdown.
  filterData = filterData.filter(function (f) {
    return f[1] !== '';
  });

  // We show the filter header hear if there are any filters selected.
  if (filterData.length > 0) {
    ShowFilterChipHeader(filterName);

    // This section sets up each chip with a:
    // Number that acts as the columns sort index for the user to know where the column falls into sort wise
    // The name of the column with its header column name it it isn't part of the Rotator
    // The direction the column is being sorted in
    for (var i = 0; i < filterData.length; i++) {
      var header = DTable.column(filterData[i][0]).header();
      var columnClass = header.classList[0];
      var columnName = header.innerText;
      var children = header.offsetParent !== null ? header.offsetParent.children[0].children[0].children : DTable.columns().header();

      // This part checks for the presense of a parent header
      $.each(children, function (key, val) {
        if ($(val).attr('id') == columnClass) {
          columnName = val.innerText + ' ' + columnName;
        }
      });

      var direction = filterData[i][1] === 'asc' ? 'Ascending' : 'Descending';
      filterValues.push(columnName + ', ' + direction);
    }
  } else {
    HideFilterChipHeader(filterName);
  }
  // Clear any chips in the filter body
  ClearFilterChips(filterName);
  ClearFilterChipsExclude(filterName);
  // This is to repopulate the chips with whatever elements are left in the current filter
  $.each(filterValues, function (index, value) {
    if (value.includes('!')) {
      if (DEBUG) console.log('exclude addnewfilterboxGroup');
      AddNewFilterBoxGroupExclude(value, 'filter-head-exclude-' + filterName, 'filter-body-exclude-' + filterName);
      AddNewFilter(value, 'filter-body-exclude-' + filterName, filterName);
    } else {
      AddNewFilterBoxGroup(value, 'filter-head-' + filterName, 'filter-body-' + filterName);
      AddNewFilter(value, 'filter-body-' + filterName, filterName);
    }
  });
}

//*********************************************
//*         End Multi Sorting                 *
//*********************************************

//*********************************************
//*         Start Notifications               *
//*********************************************

/**
 * Function that manages the creation and deletion of the Admin modal.
 * Some of the options you can pass to the function are:
 * init -> Initializes the whole modal.
 * open -> Opens the modal. If you pass in open and not init then it will initialize for you anyway.
 * destroy -> Destroy's the modal and its resources. It's a good idea to destroy it when you are done
 * with it. Otherwise it's taking up more space than needed.
 * @param {any} options An array of commands like ['init', 'open'] or ['destroy']
 */
var AdminModalManager = function (nm, tm, dem) {
  this.notifManager = nm;
  this.tutorialsManager = tm;
  this.dlEventsManager = dem;

  this.params = {
    allApps: 'all',
    appName: 'forecast',
    // Admin event buttons
    adminEventCancelId: '#admin-dl-event-cancel',
    adminEventCreateId: '#admin-dl-event-create',
    adminEventEditId: '#admin-dl-event-edit',
    adminEventDeleteId: '#admin-dl-event-delete',

    // Admin notification buttons
    adminNotifCancelId: '#admin-notifications-cancel',
    adminNotifCreateId: '#admin-notifications-create',
    adminNotifDeleteId: '#admin-notifications-delete',
    adminNotifEditId: '#admin-notifications-edit',

    // Admin tutorial buttons
    adminTutorialCancelId: '#admin-tutorial-cancel',
    adminTutorialCreateId: '#admin-tutorial-create',
    adminTutorialEditId: '#admin-tutorial-edit',
    adminTutorialDeleteId: '#admin-tutorial-delete',

    // Event tab ids/properties/functions
    events: [],
    eventDeleteListId: '#dl-event-delete-list',
    eventEditListId: '#dl-event-edit-list',
    eventEditListItem: '',
    eventEditItem: false,
    EventEditList: function (option) {
      this.SetDropdownSelection(this.eventEditListId, 'eventEditListItem', option);
    },
    // Event title text stuff
    eventTitleText: '',
    eventTitleInputId: '#dl-event-title',
    EventTitleText: function (text) {
      this.SetTextViewText(this.eventTitleInputId, 'eventTitleText', text);
    },
    // Event description text stuff
    eventBodyText: '',
    eventBodyTextId: '#dl-event-body',
    EventBodyText: function (text) {
      this.SetTextViewText(this.eventBodyTextId, 'eventBodyText', text);
    },
    // Event end date stuff
    eventEndDate: '',
    eventEndDateId: '#dl-event-end-date',
    EventEndDate: function (date) {
      return this.SetDatePicker(this.eventEndDateId, 'eventEndDate', 'eventEndTime', date);
    },
    eventFile: {},
    EventFileTitle: function (title) {
      $(this.fileTitleId).val(title);
      $(this.fileTitleId).trigger('autoresize');
    },
    fileContainerDivId: '#get-file-for-dl-event',
    fileBrowseId: '#browse-dl-event-file',
    fileFormData: {},
    fileId: '',
    fileName: '',
    fileUploadTriggerId: '#upload-dl-event-trigger',
    fileUploadFormId: '#fileUploadForm',
    fileTitleId: '#dl-event-file-title',
    // Event start date stuff
    eventStartDate: '',
    eventStartDateId: '#dl-event-start-date',
    EventStartDate: function (date) {
      return this.SetDatePicker(this.eventStartDateId, 'eventStartDate', 'eventStartTime', date);
    },
    // Event end time stuff
    eventEndTime: '',
    eventEndTimeId: '#dl-event-end-time',
    EventEndTime: function (time) {
      this.SetTimePicker(this.eventEndTimeId, 'eventEndTime', time);
    },
    // Event start time stuff
    eventStartTime: '',
    eventStartTimeId: '#dl-event-start-time',
    EventStartTime: function (time) {
      this.SetTimePicker(this.eventStartTimeId, 'eventStartTime', time);
    },
    GetEvent: function (option) {
      switch (option) {
        case 'original':
          return {
            Title: this.eventEditItem.title,
            Body: this.eventEditItem.body,
            FileId: this.eventEditItem.fileId,
            StartTime: this.eventEditItem.startTime,
            EndTime: this.eventEditItem.endTime,
            Target: this.eventEditItem.target,
          };
        case 'edited':
          return {
            EventId: this.eventEditItem.eventId,
            Title: this.eventTitleText,
            Body: this.eventBodyText,
            FileId: this.eventEditItem.fileId,
            StartTime: this.EventStartDate('get'),
            EndTime: this.EventEndDate('get'),
            Target: this.eventProjName,
          };
        default:
          return {};
      }
    },
    // DlEvent radio button stuff
    eventProjName: 'forecast',
    eventRadioAllId: '#dl-event-radio-all',
    eventRadioAppId: '#dl-event-radio-app',
    EventRadioButton: function (val) {
      return this.RadioVal('event', val);
    },

    // Notifications stuff
    notifications: [],
    notifCategories: [],
    notifDeleteListId: '#notifications-delete-list',
    // Notifications Category list stuff
    notifCategoryListItem: '',
    notifCategoryListId: '#notifications-category-list',
    NotifCategoryList: function (option) {
      this.SetDropdownSelection(this.notifCategoryListId, 'notifCategoryListItem', option);
    },
    // Notifications edit list stuff
    notifEditItem: false,
    notifEditListItem: '',
    notifEditListId: '#notifications-edit-list',
    NotifEditList: function (option) {
      this.SetDropdownSelection(this.notifEditListId, 'notifEditListItem', option);
    },
    // Notifications end date stuff
    notifEndDate: '',
    notifEndDateId: '#notifications-end-date',
    NotifEndDate: function (date) {
      return this.SetDatePicker(this.notifEndDateId, 'notifEndDate', 'notifEndTime', date);
    },
    // Notifications start date stuff
    notifStartDate: '',
    notifStartDateId: '#notifications-start-date',
    NotifStartDate: function (date) {
      return this.SetDatePicker(this.notifStartDateId, 'notifStartDate', 'notifStartTime', date);
    },
    // Notifications end time stuff
    notifEndTime: '',
    notifEndTimeId: '#notifications-end-time',
    NotifEndTime: function (time) {
      this.SetTimePicker(this.notifEndTimeId, 'notifEndTime', time);
    },
    // Notifications start time stuff
    notifStartTime: '',
    notifStartTimeId: '#notifications-start-time',
    NotifStartTime: function (time) {
      this.SetTimePicker(this.notifStartTimeId, 'notifStartTime', time);
    },
    // Notifications intro text stuff
    notifIntroText: '',
    notifIntroInputId: '#create-notifications-intro',
    NotifIntroText: function (text) {
      this.SetTextViewText(this.notifIntroInputId, 'notifIntroText', text);
    },
    // Notification radio button stuff
    notifProjName: 'forecast',
    notifRadioAllId: '#admin-notification-radio-all',
    notifRadioAppId: '#admin-notification-radio-app',
    NotifRadioButton: function (val) {
      return this.RadioVal('notif', val);
    },
    // Notifications temp list stuff. Temp list is the list that displays events or tutorials
    notifTempListEditItem: '',
    notifTempListIds: [],
    notifTempListId: '#notifications-temp-list',
    notifTempListWrapperId: '#notifications-temp-list-wrapper',
    NotifTempList: function (option) {
      if (option === 'remove') {
        $(this.notifTempListWrapperId).children().remove();
      } else {
        $(this.notifTempListId)
          .find('option[value="' + this.notifTempListEditItem + '"]')
          .prop('selected', true);
        $(this.notifTempListId).material_select();
        this.notifTempListIds = $(this.notifTempListId).val().split(/\,/gm);
      }
    },
    // Notifications title text stuff
    notifTitleText: '',
    notifTitleInputId: '#create-notifications-title',
    NotifTitleText: function (text) {
      this.SetTextViewText(this.notifTitleInputId, 'notifTitleText', text);
    },
    // Notifications tab vendor list stuff
    notifVendorGMSVenId: -1,
    notifVendorEditItem: { id: undefined, title: '' },
    notifVendorListId: '#notifications-vendor-list',
    NotifVendorList: function (vendor) {
      this.SetDropdownSelection(this.notifVendorListId, 'notifVendorEditItem', vendor);
    },
    GetNotification: function (option) {
      switch (option) {
        case 'original':
          return {
            Title: this.notifEditItem.title,
            Body: this.notifEditItem.body,
            GMSVenID: this.notifEditItem.gmsVenId,
            StartDate: this.notifEditItem.startTime,
            EndDate: this.notifEditItem.endTime,
            NotificationType: this.notifEditItem.notificationType,
            NotificationTypeId: this.notifEditItem.notificationTypeId,
            Target: this.notifEditItem.target,
            Edited: false,
          };
        case 'edited':
          return {
            NotifId: this.notifEditItem.notifId,
            Title: this.notifTitleText,
            Body: this.notifIntroText,
            GMSVenID: this.notifVendorEditItem.id ? this.notifVendorEditItem.id : this.notifVendorGMSVenId,
            StartDate: this.NotifStartDate('get'),
            EndDate: this.NotifEndDate('get'),
            NotificationType: this.notifTempListIds[0],
            NotificationTypeId: this.notifTempListIds[1],
            Target: this.notifProjName,
            Edited: this.notifEditItem ? true : false,
          };
        default:
          return {};
      }
    },

    // Tutorial stuff
    tutorials: [],
    tutorialTitleText: '',
    tutorialTitleTextId: '#tutorial-title',
    TutorialTitle: function (text) {
      this.SetTextViewText(this.tutorialTitleTextId, 'tutorialTitleText', text);
    },
    tutorialDeleteListId: '#tutorial-delete-list',
    tutorialEditItem: false,
    tutorialEditListItem: '',
    tutorialEditListId: '#tutorial-edit-list',
    TutorialEditList: function (option) {
      this.SetDropdownSelection(this.tutorialEditListId, 'tutorialEditListItem', option);
    },
    tutorialGroupText: '',
    tutorialGroupsListId: '#tutorial-groups-list',
    TutorialGroupList: function (group) {
      this.SetDropdownSelection(this.tutorialGroupsListId, 'tutorialGroupText', group);
      this.TutorialGroup(group === this.emptyOption ? null : group);
    },
    tutorialGroupTextId: '#tutorial-group',
    TutorialGroup: function (text) {
      this.SetTextViewText(this.tutorialGroupTextId, 'tutorialGroupText', text);
    },
    tutorialIntroText: '',
    tutorialIntroTextId: '#tutorial-intro',
    TutorialIntro: function (text) {
      this.SetTextViewText(this.tutorialIntroTextId, 'tutorialIntroText', text);
    },
    tutorialVideoLink: '',
    tutorialVideoLinkId: '#tutorial-video-link',
    TutorialVideo: function (text) {
      this.SetTextViewText(this.tutorialVideoLinkId, 'tutorialVideoLink', text);
    },
    GetTutorial: function (option) {
      switch (option) {
        case 'original':
          return {
            Intro: this.tutorialEditItem.intro,
            LastEdit: this.tutorialEditItem.lastEdit,
            Title: this.tutorialEditItem.title,
            TutorialGroup: this.tutorialEditItem.tutorialGroup,
            TutorialId: this.tutorialEditItem.tutorialId,
            VideoLink: this.tutorialEditItem.videoLink,
          };
        case 'edited':
          return {
            TutorialId: this.tutorialEditItem.tutorialId,
            Intro: this.tutorialIntroText,
            Title: this.tutorialTitleText,
            TutorialGroup: this.tutorialGroupText,
            TutorialVideoLink: this.tutorialVideoLink,
            VideoLink: this.tutorialVideoLink,
          };
        default:
          return {};
      }
    },

    // Util functions and properties
    adminModalId: '#admin-modal',
    adminModalTabListLiId: '#admin-modal-tab-list li',
    emptyDate: '01/01/0001',
    emptyOption: '-1',
    emptyTime: '01:01:01',
    dontSelect: -2,
    tabListId: '#admin-modal-tab-list',
    that: this,
    // Date Stuff
    datePickers: {},
    timePickers: {},
    RadioVal: function (prefix, val) {
      if (val) {
        $(this[prefix + 'RadioAllId']).prop('checked', val === this.allApps);
        $(this[prefix + 'RadioAppId']).prop('checked', val === this.appName);
      }
      return (this[prefix + 'ProjName'] = val);
    },
    SetDatePicker: function (id, dateField, timeField, date) {
      if (date === 'clear') {
        $(this.datePickers[id]).pickadate('picker').clear();
      } else if (date === 'get') {
        return this[dateField] + ' ' + this[timeField];
      } else {
        $(this.datePickers[id]).pickadate('picker').set('select', date);
      }
      this[dateField] = $(this.datePickers[id]).val();
    },
    SetDropdownSelection: function (id, field, option) {
      if (option) {
        var children = $(id).children();
        var childVal =
          option === '-1'
            ? option
            : children
                .toArray()
                .filter(function (x) {
                  return x.attributes.value.value === option;
                })
                .map(function (x) {
                  return x.attributes.value.value;
                })[0];
        $(id)
          .find('option[value="' + childVal + '"]')
          .prop('selected', true);
        $(id).material_select();
        $(id).trigger('change');
        this[field] = this.that.getIdAndOption(children, id);
      }
    },
    SetTextViewText: function (id, field, text) {
      if (text) {
        $(id).val(text);
        this[field] = $(id).val();
      } else {
        $(id).val('');
        $(id).next().removeClass('active');
      }
    },
    SetTimePicker: function (id, field, time) {
      time = time === 'clear' ? ' ' : time;
      $(this.timePickers[id]).pickatime('picker').val(time);
      this[field] = $(this.timePickers[id]).val();
    },
  };

  /**
   * Function to create a materialize list of vendors in the Admin modal.
   * @param {any} vendors A list of vendors with a field Key as the gmsvenid and Value as the vendor name.
   */
  this.createAdminVendorList = function (vendors) {
    var options = [];
    var tempList = $(this.params.notifVendorListId);

    vendors = vendors.sort(function (a, b) {
      return a.Value < b.Value ? -1 : a.Value > b.Value ? 1 : 0;
    });

    $(tempList).children().remove();
    options.push($('<option value="-1">All Vendors</option>'));

    $.each(vendors, function (key, val) {
      options.push($('<option value="' + val.Key + '">' + val.Value + '</option>'));
    });

    $.each(options, function (key, val) {
      $(tempList).append($(val));
    });

    $(tempList).material_select();
  };

  /**
   * Function that saves an event created by an admin.
   */
  this.createEvent = function () {
    if (this.params.eventTitleText != '') {
      var file = new FormData();
      file.append('DlEvent', JSON.stringify(this.params.GetEvent('edited')));
      var fileVal = this.params.fileFormData.values ? this.params.fileFormData.values().next().value : null;
      file.append('file', fileVal);

      $.ajax({
        context: this,
        url: '/Home/CreateDlEvent',
        type: 'POST',
        dataType: 'json',
        data: file,
        contentType: false,
        processData: false,
        success: function (x) {
          alert('Successfully created event!');

          if (this.params.fileUploadForm) {
            $(this.params.fileUploadFormId).reset();
          }
          this.params.EventBodyText('');
          this.params.EventTitleText('');
          this.getEventList();
          this.clearEventFields();
          this.dlEventsManager.getDlEvents();
        },
        error: function (x) {
          alert("Something went wrong. Event wasn't created!");
        },
      });
    }
  };

  /**
   * Function to call the createEvent function after doing some work previous to the call.
   */
  this.createEventCall = function (e) {
    this.createEvent(e);
  };

  /**
   * Function to create a list of items for the Admin modal that is used to edit/delete things
   * like notifications,tutorials, and events.
   * @param {any} list A list of items
   * @param {any} id The id of the list like '#dl-event-delete-list' or '#notifications-edit-list'.
   * @param {any} itemId The field name that acts as the id for each item like gmsvenid or id.
   * @param {any} title The field name that acts as the name for the item like Title, Name, etc...
   */
  this.createList = function (list, id, itemId, title) {
    var options = [];
    var tempList = $(id);

    list = list.sort(function (a, b) {
      return a[title].toLowerCase() < b[title].toLowerCase() ? -1 : a[title].toLowerCase() > b[title].toLowerCase() ? 1 : 0;
    });

    $(tempList).children().remove();
    options.push($('<option value="-1">Select Option</option>'));

    $.each(list, function (key, val) {
      options.push($('<option value="' + val[itemId] + '">' + val[title] + '</option>'));
    });

    $.each(options, function (key, val) {
      $(tempList).append($(val));
    });

    $(tempList).material_select();

    return $(tempList);
  };

  /**
   * Function that saves a notification created by an admin.
   * */
  this.createNotification = function () {
    if (this.params.notifTitleText != '' && this.params.notifTempListIds.length > 1) {
      $.ajax({
        context: this,
        url: '/Home/CreateNotification',
        async: false,
        dataType: 'json',
        type: 'POST',
        data: this.params.GetNotification('edited'),
        success: function (x) {
          alert('Successfully created Notification!');
          this.notifManager.getNotificationsCount();
          this.clearNotifFields();
          this.getNotificationsList();
        },
        error: function (x) {
          alert("Something went wrong. Notification wasn't created!");
        },
      });
    }
  };

  /**
   * Function to call the createNotification function after doing some work previous to the call.
   */
  this.createNotificationCall = function (e) {
    this.createNotification(e);
    this.getNotificationsList();
  };

  /**
   * Function that saves a notification created by an admin.
   * */
  this.createTutorial = function () {
    if (this.params.tutorialTitleText.length > 0 && this.params.tutorialGroupText.length > 0) {
      $.ajax({
        context: this,
        url: '/Home/CreateTutorial',
        async: false,
        dataType: 'json',
        type: 'POST',
        data: this.params.GetTutorial('edited'),
        success: function (x) {
          alert('Successfully created tutorial!');
          this.getTutorialList();
          this.getTutorialGroups();
          this.tutorialsManager.getTutorials();
        },
        error: function (x) {
          alert("Something went wrong. Tutorial wasn't created!");
        },
      });
    } else {
      alert('Tutorial title and Tutorial Group must contain a value.');
    }
  };

  /**
   * Function to call the createTutorial function after doing some work previous to the call.
   */
  this.createTutorialCall = function (e) {
    this.createTutorial(e);
    this.getTutorialList();
  };

  /**
   * Function that deletes a forecast event.
   * @param {any} event Must have a field called id and title(title is optional).
   */
  this.deleteEvent = function (event) {
    if (event.id == '-1') return;
    var id = parseInt(event.id);
    $.ajax({
      context: this,
      url: '/Home/DeleteDlEvent',
      async: false,
      dataType: 'json',
      type: 'POST',
      data: {
        dlEvent: this.params.events.filter(function (x, y) {
          return x.eventId === id;
        })[0],
      },
      success: function (x) {
        alert('Deleted ' + event.title + '!');
        this.getEventList();
      },
      error: function (x) {
        alert('Could not delete ' + event.title + '!');
      },
    });
  };

  /**
   * Function to call the deleteEvent function after doing some work previous to the call.
   */
  this.deleteEventCall = function (e) {
    var eventId = '#dl-event-delete-list';
    var result = this.getIdAndOption($(eventId).children(), eventId);
    this.deleteEvent(result);
  };

  /**
   * Function that deletes a forecast tutorial.
   * @param {any} tutorial Must have a field called id and title(title is optional).
   */
  this.deleteTutorial = function (tutorial) {
    if (tutorial.id == '-1') return;
    $.ajax({
      context: this,
      url: '/Home/DeleteTutorial',
      async: false,
      dataType: 'json',
      type: 'POST',
      data: { id: tutorial.id },
      success: function (x) {
        alert('Deleted ' + tutorial.title + '!');
        this.getTutorialList();
      },
      error: function (x) {
        alert('Could not delete ' + tutorial.title + '!');
      },
    });
  };

  /**
   * Function to call the deleteTutorial function after doing some work previous to the call.
   */
  this.deleteTutorialCall = function (e) {
    var hotToId = '#tutorial-delete-list';
    var result = this.getIdAndOption($(hotToId).children(), hotToId);
    this.deleteTutorial(result);
  };

  /**
   * Function that deletes a forecast notification.
   * @param {any} notif Must have a field called id and title(title is optional).
   */
  this.deleteNotification = function (notif) {
    if (parseInt(notif.id) === -1) return;
    $.ajax({
      context: this,
      url: '/Home/DeleteNotification',
      async: false,
      dataType: 'json',
      type: 'POST',
      data: { id: notif.id },
      success: function (x) {
        alert('Deleted ' + notif.title + '!');
        this.getNotificationsList();
        this.notifManager.getNotificationsCount();
      },
      error: function (x) {
        alert('Could not delete ' + notif.title + '!');
      },
    });
  };

  /**
   * Function to call the deleteNotification function after doing some work previous to the call.
   */
  this.deleteNotificationCall = function (e) {
    var notifId = '#notifications-delete-list';
    var result = this.getIdAndOption($(notifId).children(), notifId);
    this.deleteNotification(result);
  };

  this.destroy = function () {
    const children = $('#notifications-temp-list-wrapper').children();
    if (children) {
      $(children).remove();
    }
    $(this.params.adminModalId).modal('destroy');
  };

  /**
   * Function that saves an edited event created by an admin.
   * */
  this.editEvent = function (e) {
    if (this.params.eventTitleText != '') {
      var file = new FormData();
      file.append('original', JSON.stringify(this.params.GetEvent('original')));
      file.append('edited', JSON.stringify(this.params.GetEvent('edited')));
      var fileVal = this.params.fileFormData.values ? this.params.fileFormData.values().next().value : null;
      file.append('file', fileVal);

      $.ajax({
        context: this,
        url: '/Home/UpdateDlEvent',
        async: false,
        dataType: 'json',
        type: 'POST',
        data: file,
        contentType: false,
        processData: false,
        success: function (x) {
          alert('Successfully created Event!');
          this.clearEventFields();
          this.getEventList();
        },
        error: function (x) {
          alert("Something went wrong. Event wasn't created!");
        },
      });
    }
  };

  /**
   * Function to call the editNotification function after doing some work previous to the call.
   */
  this.editEventCall = function (e) {
    this.editEvent(e);
  };

  /**
   * Function that saves an edited notification created by an admin.
   * */
  this.editNotification = function (e) {
    if (this.params.notifTitleText != '' && this.params.notifTempListIds.length > 1) {
      $.ajax({
        context: this,
        url: '/Home/UpdateNotification',
        async: false,
        dataType: 'json',
        type: 'POST',
        data: {
          original: this.params.notifEditItem,
          edit: this.params.GetNotification('edited'),
        },
        success: function (x) {
          alert('Successfully created Notification!');
          this.notifManager.getNotificationsCount();
          this.notifManager.getNotifications();
          this.clearNotifFields();
          this.getNotificationsList();
        },
        error: function (x) {
          alert("Something went wrong. Notification wasn't created!");
        },
      });
    }
  };

  /**
   * Function to call the editNotification function after doing some work previous to the call.
   */
  this.editNotificationCall = function (e) {
    this.editNotification(e);
  };

  /**
   * Function that saves an edited tutorial created by an admin.
   * */
  this.editTutorial = function (e) {
    if (this.params.tutorialTitleText != '') {
      $.ajax({
        context: this,
        url: '/Home/UpdateTutorial',
        async: false,
        dataType: 'json',
        type: 'POST',
        data: {
          original: this.params.tutorialEditItem,
          edit: this.params.GetTutorial('edited'),
        },
        success: function (x) {
          alert('Successfully edited Tutorial!');
          this.clearTutorialFields();
          this.getTutorialList();
        },
        error: function (x) {
          alert("Something went wrong. Notification wasn't edited!");
        },
      });
    }
  };

  /**
   * Function to call the editTutorial function after doing some work previous to the call.
   */
  this.editTutorialCall = function (e) {
    this.editTutorial(e);
  };

  /**
   * Function to get a list of object to populate the admin temp list which
   * could be a list of events or tutorials depending on which category was selected.
   * @param {any} id
   * @param {any} title
   */
  this.getAdminTempList = function (id, title, callback) {
    if (id != '-1') {
      $.ajax({
        context: this,
        url: '/Home/GetAdminTempList',
        async: true,
        dataType: 'json',
        type: 'POST',
        data: { id: id },
        success: function (x) {
          this.updateAdminTempList(x, title);
          if (callback) {
            callback();
          }
        },
        error: function (x) {},
      });
    }
  };

  /**
   * Made for the getAdminTempList function to do some work before calling it.
   */
  this.getAdminTempListCall = function (e) {
    var result = this.getIdAndOption(e.currentTarget.children, this.params.notifCategoryListId);
    this.params.notifCategoryListItem = result;
    this.getAdminTempList(result.id, result.title);
  };

  /**
   * Function that gets a list of events for the delete list in the admin modal
   * Events tab.
   * */
  this.getEventList = function () {
    $.ajax({
      context: this,
      url: '/Home/GetDlEvents',
      async: true,
      dataType: 'json',
      type: 'POST',
      success: function (x) {
        this.params.events = Object.values(x).map(function (ev) {
          return {
            eventId: ev.EventId,
            title: ev.Title,
            body: ev.Body,
            fileId: ev.FileId,
            startTime: ev.StartTime,
            endTime: ev.EndTime,
            lastEdit: ev.LastEdit,
            target: ev.Target,
          };
        });
        this.createList(x, this.params.eventEditListId, 'EventId', 'Title');
        this.createList(x, this.params.eventDeleteListId, 'EventId', 'Title');
      },
      error: function (x) {},
    });
  };

  /**
   * Function that gets an object with the fields id and title
   * from a list of options such as options from a materialize list
   * where the option name matches the elementId
   * @param {any} optionList List of <option>'s.
   * @param {any} elementId The value that you are looking for.
   */
  this.getIdAndOption = function (optionList, elementId) {
    var value = $(elementId).val();
    var name = 'Option';
    $.each(optionList, function (key, val) {
      if (value == val.value) {
        name = val.innerText;
      }
    });
    return { id: value, title: name };
  };

  /**
   * Function to get a list of notification categories like tutorials, and Events
   * */
  this.getNotificationCategories = function () {
    $.ajax({
      context: this,
      url: '/Home/GetNotificationCategories',
      async: true,
      dataType: 'json',
      type: 'POST',
      success: function (x) {
        this.params.notifCategories = [].slice.call(x).map(function (x) {
          return {
            ncid: x.NCID,
            notifTypeName: x.NotifTypeName,
          };
        });
        this.updateAdminCategoryList(x, 'NotifTypeName');
      },
      error: function (x) {},
    });
  };

  /**
   * Function that gets a list of notifications for the delete list in the admin modal
   * Notifications tab.
   * */
  this.getNotificationsList = function () {
    $.ajax({
      context: this,
      url: '/Home/GetNotificationsList',
      dataType: 'json',
      type: 'POST',
      async: true,
      success: function (x) {
        this.params.notifications = [].slice.call(x).map(function (x2) {
          return {
            notifId: x2.NotifId,
            title: x2.Title,
            body: x2.Body,
            notificationType: x2.NotificationType,
            notificationTypeId: x2.NotificationTypeId,
            tableName: x2.TableName,
            gmsVenId: x2.GMSVenID,
            startTime: x2.StartTime,
            endTime: x2.EndTime,
            target: x2.Target,
          };
        });
        this.createList(x, this.params.notifEditListId, 'NotifId', 'Title');
        this.createList(x, this.params.notifDeleteListId, 'NotifId', 'Title');
      },
      error: function (x) {},
    });
  };

  /**
   * Function that gets a list of tutorials for the delete list in the admin modal
   * Tutorials tab.*/
  this.getTutorialGroups = function () {
    $.ajax({
      context: this,
      url: '/Home/GetTutorialGroups',
      dataType: 'json',
      type: 'POST',
      async: true,
      success: function (x) {
        this.params.tutorialGroups = Object.values(x).map(function (g) {
          return {
            value: g,
          };
        });
        this.createList(this.params.tutorialGroups, this.params.tutorialGroupsListId, 'value', 'value');
      },
      error: function (x) {},
    });
  };

  /**
   * Function that gets a list of tutorials for the delete list in the admin modal
   * Tutorials tab.*/
  this.getTutorialList = function () {
    $.ajax({
      context: this,
      url: '/Home/GetTutorials',
      dataType: 'json',
      type: 'POST',
      async: true,
      success: function (x) {
        this.params.tutorials = Object.values(x).map(function (tut) {
          return {
            intro: tut.Intro,
            lastEdit: tut.LastEdit,
            title: tut.Title,
            tutorialGroup: tut.TutorialGroup,
            tutorialId: tut.TutorialId,
            videoLink: tut.VideoLink,
          };
        });
        this.createList(x, this.params.tutorialEditListId, 'TutorialId', 'Title');
        this.createList(x, this.params.tutorialDeleteListId, 'TutorialId', 'Title');
      },
      error: function (x) {},
    });
  };

  /**
   * Function that gets the vendors for the vendor list in the admin modal.
   * */
  this.getVendorList = function () {
    $.ajax({
      context: this,
      url: '/Home/GetVendorList',
      async: true,
      dataType: 'json',
      type: 'POST',
      success: function (x) {
        this.createAdminVendorList(x);
      },
      error: function (x) {},
    });
  };

  /**
   * Clears the filds in the Admin event panel
   */
  this.clearEventFields = function () {
    this.setSaveButtons('event');
    var p = this.params;
    p.eventEditItem = false;
    p.EventTitleText('');
    p.EventBodyText('');
    p.EventStartDate('clear');
    p.EventStartTime('clear');
    p.EventEndDate('clear');
    p.EventEndTime('clear');
    p.EventEditList(p.emptyOption);
    p.EventFileTitle('');
    p.EventRadioButton(p.appName);
    Materialize.updateTextFields();
  };

  /**
   * Initialized the fields with the values of a selected event to edit.
   */
  this.initEventEdit = function (ev) {
    this.setEventFileName(ev);
    this.setEditButtons('event');
    this.params.eventEditItem = ev;
    var p = this.params;
    var startDateTime = new Date(ev.startTime);
    var endDateTime = new Date(ev.endTime);

    p.EventTitleText(ev.title);
    p.EventBodyText(ev.body);
    p.EventStartDate(startDateTime.toLocaleDateString());
    p.EventStartTime(startDateTime.getHours() + ':' + startDateTime.getMinutes() + ':' + startDateTime.getSeconds());
    p.EventEndDate(endDateTime.toLocaleDateString());
    p.EventEndTime(endDateTime.getHours() + ':' + endDateTime.getMinutes() + ':' + endDateTime.getSeconds());
    p.EventRadioButton(ev.target);
    Materialize.updateTextFields();
  };

  /**
   * Clears the filds in the Admin notifications panel
   */
  this.clearNotifFields = function () {
    this.setSaveButtons('notif');
    var p = this.params;
    p.notifEditItem = false;
    p.NotifTitleText('');
    p.NotifIntroText('');
    p.NotifStartDate('clear');
    p.NotifStartTime('clear');
    p.NotifEndDate('clear');
    p.NotifEndTime('clear');
    p.NotifEditList(p.emptyOption);
    p.NotifCategoryList(p.emptyOption);
    p.notifTempListEditItem = p.emptyOption;
    p.NotifTempList('remove');
    this.getAdminTempList(this.params.notifCategoryListItem.id, this.params.notifCategoryListItem.title, p.NotifTempList.bind(this.params));
    p.NotifVendorList(p.emptyOption);
    p.NotifRadioButton(p.appName);
    Materialize.updateTextFields();
  };

  /**
   * Initialized the fields with the values of a selected notification to edit.
   */
  this.initNotifEdit = function (notif) {
    this.setEditButtons('notif');
    this.params.notifEditItem = notif;
    var p = this.params;
    var startDateTime = new Date(notif.startTime);
    var endDateTime = new Date(notif.endTime);
    var notifCatId = this.params.notifCategories
      .filter(function (x) {
        return parseInt(x.ncid) === parseInt(notif.notificationType);
      })
      .map(function (x) {
        return x.ncid + ',' + notif.notificationTypeId;
      });

    p.NotifTitleText(notif.title);
    p.NotifIntroText(notif.body);
    p.NotifStartDate(startDateTime.toLocaleDateString());
    p.NotifStartTime(startDateTime.getHours() + ':' + startDateTime.getMinutes() + ':' + startDateTime.getSeconds());
    p.NotifEndDate(endDateTime.toLocaleDateString());
    p.NotifEndTime(endDateTime.getHours() + ':' + endDateTime.getMinutes() + ':' + endDateTime.getSeconds());
    p.NotifCategoryList(notif.notificationType);
    p.notifTempListEditItem = notifCatId;
    this.getAdminTempList(this.params.notifCategoryListItem.id, this.params.notifCategoryListItem.title, p.NotifTempList.bind(this.params));
    p.NotifVendorList(p.gmsVenId);
    p.NotifRadioButton(notif.target);
    Materialize.updateTextFields();
  };

  /**
   * Clears the filds in the Admin tutorials panel
   */
  this.clearTutorialFields = function () {
    this.setSaveButtons('tutorial');
    var p = this.params;
    p.tutorialEditItem = false;
    p.TutorialTitle('');
    p.TutorialVideo('');
    p.TutorialIntro('');
    p.TutorialGroup('');
    p.TutorialGroupList(p.emptyOption);
    p.TutorialEditList(p.emptyOption);
    Materialize.updateTextFields();
  };

  /**
   * Initialized the fields with the values of a selected tutorials to edit.
   */
  this.initTutorialEdit = function (tutorial) {
    this.setEditButtons('tutorial');
    this.params.tutorialEditItem = tutorial;
    var p = this.params;

    p.TutorialTitle(tutorial.title);
    p.TutorialIntro(tutorial.intro);
    p.TutorialVideo(tutorial.videoLink);
    p.TutorialGroup(tutorial.tutorialGroup);
    p.TutorialGroupList(tutorial.tutorialGroup);
    p.TutorialEditList(p.gmsVenId);
    Materialize.updateTextFields();
  };

  // Updates the event title
  this.onEventTitleChange = function (e) {
    this.params.eventTitleText = $(this.params.eventTitleInputId).val();
  };

  this.onEventBodyChange = function (e) {
    this.params.eventBodyText = $(this.params.eventBodyTextId).val();
  };

  this.onEventStartDateChange = function (e) {
    this.params.eventStartDate = $(this.params.eventStartDateId).val();
  };

  this.onEventEndDateChange = function (e) {
    this.params.eventEndDate = $(this.params.eventEndDateId).val();
  };

  this.onEventStartTimeChange = function (e) {
    this.params.eventStartTime = $(this.params.eventStartTimeId).val();
  };

  this.onEventEndTimeChange = function (e) {
    this.params.eventEndTime = $(this.params.eventEndTimeId).val();
  };

  this.onEventRadioAllChange = function (e) {
    this.params.EventRadioButton(this.params.allApps);
  };

  this.onEventRadioAppChange = function (e) {
    this.params.EventRadioButton(this.params.appName);
  };

  // Update the notification intro field
  this.onNotifIntroChange = function (e) {
    this.params.notifIntroText = $(this.params.notifIntroInputId).val();
  };

  this.onNotifTempListChange = function (e) {
    this.params.notifTempListIds = $(this.params.notifTempListId).val().split(/\,/gm);
  };

  this.onNotifTitleChange = function (e) {
    this.params.notifTitleText = $(this.params.notifTitleInputId).val();
  };

  this.onVendorIdChange = function (e) {
    this.params.notifVendorGMSVenId = $(this.params.notifVendorListId).val();
  };

  this.onNotifStartDateChange = function (e) {
    this.params.notifStartDate = $(this.params.notifStartDateId).val();
  };

  this.onNotifEndDateChange = function (e) {
    this.params.notifEndDate = $(this.params.notifEndDateId).val();
  };

  this.onNotifStartTimeChange = function (e) {
    this.params.notifStartTime = $(this.params.notifStartTimeId).val();
  };

  this.onNotifEndTimeChange = function (e) {
    this.params.notifEndTime = $(this.params.notifEndTimeId).val();
  };

  // Update the notification files with the current edit notification
  this.onNotifEditListChange = function (e) {
    this.params.notifEditListItem = $(this.params.notifEditListId).val();
    if (this.params.notifEditListItem !== '-1') {
      var that = this;
      var id = this.params.notifications.filter(function (x) {
        return parseInt(that.params.notifEditListItem) === parseInt(x.notifId);
      });
      this.initNotifEdit(id[0]);
    } else {
      if (!this.params.notifEditItem) return;
      this.clearNotifFields();
    }
  };

  this.onNotifEditCanceled = function (e) {
    this.params.NotifEditList('-1');
  };

  this.onNotifRadioAllChange = function (e) {
    this.params.NotifRadioButton(this.params.allApps);
  };

  this.onNotifRadioAppChange = function (e) {
    this.params.NotifRadioButton(this.params.appName);
  };

  this.onEventEditCanceled = function (e) {
    this.params.EventEditList('-1');
  };

  this.onTutorialEditCanceled = function (e) {
    this.params.TutorialEditList('-1');
  };

  this.onEventEditListChange = function (e) {
    this.params.eventEditListItem = $(this.params.eventEditListId).val();
    if (this.params.eventEditListItem !== '-1') {
      var that = this;
      var id = this.params.events.filter(function (x) {
        return parseInt(that.params.eventEditListItem) === parseInt(x.eventId);
      });
      this.initEventEdit(id[0]);
    } else {
      if (!this.params.eventEditItem) return;
      this.clearEventFields();
    }
  };

  this.onFileBrowseChange = function (e) {
    this.params.fileFormData = new FormData($('#fileUploadForm')[0]);
    this.params.fileName = this.params.fileFormData.values().next().value.name;
    this.params.EventFileTitle(this.params.fileName);
  };

  this.onFileBrowseClick = function (e) {
    $(this.params.fileBrowseId).click();
  };

  this.onTutorialEditListChange = function (e) {
    this.params.tutorialEditListItem = $(this.params.tutorialEditListId).val();
    if (this.params.tutorialEditListItem !== '-1') {
      var that = this;
      var id = this.params.tutorials.filter(function (x) {
        return parseInt(that.params.tutorialEditListItem) === parseInt(x.tutorialId);
      });
      this.initTutorialEdit(id[0]);
    } else {
      if (!this.params.tutorialEditItem) return;
      this.clearTutorialFields();
    }
  };

  this.onTutorialGroupChange = function (e) {
    this.params.tutorialGroupText = $(this.params.tutorialGroupTextId).val();
  };

  this.onTutorialGroupListChange = function (e) {
    this.params.TutorialGroup($(this.params.tutorialGroupsListId).val());
    Materialize.updateTextFields();
  };

  this.onTutorialIntroChange = function (e) {
    this.params.tutorialIntroText = $(this.params.tutorialIntroTextId).val();
  };

  this.onTutorialTitleChange = function (e) {
    this.params.tutorialTitleText = $(this.params.tutorialTitleTextId).val();
  };

  this.onTutorialVideoLinkChange = function (e) {
    this.params.tutorialVideoLink = $(this.params.tutorialVideoLinkId).val();
  };

  this.open = function () {
    $(this.params.adminModalId).modal('open');
  };

  this.setEditButtons = function (tab) {
    switch (tab) {
      case 'event':
        $(this.params.adminEventEditId).css('display', 'block');
        $(this.params.adminEventCancelId).css('display', 'block');
        $(this.params.adminEventCreateId).css('display', 'none');
        break;
      case 'notif':
        $(this.params.adminNotifEditId).css('display', 'block');
        $(this.params.adminNotifCancelId).css('display', 'block');
        $(this.params.adminNotifCreateId).css('display', 'none');
        break;
      case 'tutorial':
        $(this.params.adminTutorialEditId).css('display', 'block');
        $(this.params.adminTutorialCancelId).css('display', 'block');
        $(this.params.adminTutorialCreateId).css('display', 'none');
        break;
      default:
        return;
    }
  };

  this.setEventFileName = function (ev) {
    $.ajax({
      context: this,
      url: '/Home/GetDlEventFile',
      async: false,
      dataType: 'json',
      type: 'POST',
      data: { fileId: ev.fileId },
      success: function (x) {
        this.params.eventFile = x;
        this.params.EventFileTitle(x.Name);
      },
      error: function (x) {},
    });
  };

  this.setSaveButtons = function (tab) {
    switch (tab) {
      case 'event':
        $(this.params.adminEventEditId).css('display', 'none');
        $(this.params.adminEventCancelId).css('display', 'none');
        $(this.params.adminEventCreateId).css('display', 'block');
        break;
      case 'notif':
        $(this.params.adminNotifEditId).css('display', 'none');
        $(this.params.adminNotifCancelId).css('display', 'none');
        $(this.params.adminNotifCreateId).css('display', 'block');
        break;
      case 'tutorial':
        $(this.params.adminTutorialEditId).css('display', 'none');
        $(this.params.adminTutorialCancelId).css('display', 'none');
        $(this.params.adminTutorialCreateId).css('display', 'block');
        break;
      default:
        return;
    }
  };

  this.switchTab = function (e) {
    var text = e.currentTarget.text;

    if (text === 'Notifications') {
      this.getNotificationCategories();
      this.getVendorList();
      this.getNotificationsList();
    } else if (text === 'Tutorials') {
      this.getTutorialGroups();
      this.getTutorialList();
    } else if (text === 'Events') {
      this.getEventList();
    }
  };

  /**
   * Function to update the category list in the admin modal.
   * @param {any} list List of objects with NCID, NotifTypeName fields.
   */
  this.updateAdminCategoryList = function (list, columnName) {
    var options = [];
    var that = this;

    list = list.sort(function (x, y) {
      return x[columnName].toLowerCase() < y[columnName].toLowerCase() ? -1 : x[columnName].toLowerCase() > y[columnName].toLowerCase() ? 1 : 0;
    });

    $(this.params.notifCategoryListId).material_select('destroy');
    options.push($('<option value="-1">Select An Option</option>'));

    $.each(list, function (key, val) {
      options.push($('<option value="' + val.NCID + '">' + val.NotifTypeName.replace(/\_|\-/gm, ' ') + '</option>'));
    });

    $(this.params.notifCategoryListId).children().remove();

    $.each(options, function (key, val) {
      $(that.params.notifCategoryListId).append($(val));
    });

    $(this.params.notifCategoryListId).material_select();
  };

  /**
   * Function to update/create the admin temp list which can be a list of events, tutorial's, etc...
   * @param {any} itemList list of events or tutorial's
   * @param {any} labelTitle The name of the category you are creating like Events or Tutorials
   */
  this.updateAdminTempList = function (itemList, labelTitle) {
    var options = [];
    var wrapper = $('#notifications-temp-list-wrapper');

    itemList = itemList.sort(function (x, y) {
      return x['Title'].toLowerCase() < y['Title'].toLowerCase() ? -1 : x['Title'].toLowerCase() > y['Title'].toLowerCase() ? 1 : 0;
    });

    // Create and bind event
    var tempList = $('<select id="notifications-temp-list"></select>');
    $(tempList).on('change', this.onNotifTempListChange.bind(this));

    var label = $('<label id="notifications-temp-list-label">' + 'Select ' + labelTitle + '</label>');

    $(wrapper).children().remove();
    options.push($('<option value="-1">Select An Option</option>'));

    $.each(itemList, function (key, val) {
      options.push($('<option value="' + val.NotifType + ',' + val.NotifTypeId + '">' + val.Title + '</option>'));
    });

    $.each(options, function (key, val) {
      $(tempList).append($(val));
    });

    $(wrapper).append(tempList);
    $(wrapper).append(label);
    $(tempList).material_select();
  };

  this.init = function () {
    $(this.params.tabListId).tabs();
    var that = this;

    Object.values(
      $('.datepicker').pickadate({
        selectMonths: true, // Creates a dropdown to control month
        selectYears: 15, // Creates a dropdown of 15 years to control year,
        today: 'Today',
        clear: 'Clear',
        close: 'Ok',
        format: 'mm-dd-yyyy',
        closeOnSelect: true, // Close upon selecting a date,
        container: undefined, // ex. 'body' will append picker to body
      })
    )
      .filter(function (x) {
        return x.id ? true : false;
      })
      .forEach(function (x, i) {
        var prop = '#' + x.id;
        that.params.datePickers[prop] = x;
      });

    Object.values(
      $('.timepicker').pickatime({
        default: 'now', // Set default time: 'now', '1:30AM', '16:30'
        fromnow: 0, // set default time to * milliseconds from now (using with default = 'now')
        twelvehour: false, // Use AM/PM or 24-hour format
        donetext: 'OK', // text for done-button
        cleartext: 'Clear', // text for clear-button
        canceltext: 'Cancel', // Text for cancel-button,
        container: undefined, // ex. 'body' will append picker to body
        autoclose: true,
        ampmclickable: true,
      })
    )
      .filter(function (x) {
        return x.id ? true : false;
      })
      .forEach(function (x, i) {
        var prop = '#' + x.id;
        that.params.timePickers[prop] = x;
      });

    $('input' + this.params.eventTitleInputId).characterCounter();
    $(this.params.notifCategoryListId).material_select();
    this.getNotificationCategories();
    this.getVendorList();
    this.getNotificationsList();
    this.getTutorialGroups();
    $(this.params.adminModalId).modal({ dismissible: false });
  };

  this.init();

  // Event listeners for Tutorials tab
  $(this.params.tutorialVideoLinkId).on('keyup', this.onTutorialVideoLinkChange.bind(this));
  $(this.params.tutorialGroupTextId).on('keyup', this.onTutorialGroupChange.bind(this));
  $(this.params.tutorialGroupsListId).on('change', this.onTutorialGroupListChange.bind(this));
  $(this.params.tutorialIntroTextId).on('keyup', this.onTutorialIntroChange.bind(this));
  $(this.params.tutorialTitleTextId).on('keyup', this.onTutorialTitleChange.bind(this));
  $(this.params.tutorialEditListId).on('change', this.onTutorialEditListChange.bind(this));
  // Tutorial Edit/Cancel clicks
  $(this.params.adminTutorialCancelId).on('click', this.onTutorialEditCanceled.bind(this));
  $(this.params.adminTutorialEditId).on('click', this.editTutorialCall.bind(this));
  // Event that gets fired when a tutorial to edit in the admin modal is selected
  $(this.params.tutorialEditListId).on('change', this.onTutorialEditListChange.bind(this));
  // Event that creates a forecast tutorial
  $(this.params.adminTutorialCreateId).on('click', this.createTutorialCall.bind(this));
  // Event for deleting a tutorial in the admin modal
  $(this.params.adminTutorialDeleteId).on('click', this.deleteTutorialCall.bind(this));

  // Event listeners for Events tab
  $(this.params.eventTitleInputId).on('keyup', this.onEventTitleChange.bind(this));
  $(this.params.eventBodyTextId).on('keyup', this.onEventBodyChange.bind(this));
  $(this.params.fileUploadTriggerId).on('click', this.onFileBrowseClick.bind(this));
  $(this.params.fileBrowseId).on('change', this.onFileBrowseChange.bind(this));
  $(this.params.eventStartDateId).on('change', this.onEventStartDateChange.bind(this));
  $(this.params.eventEndDateId).on('change', this.onEventEndDateChange.bind(this));
  $(this.params.eventStartTimeId).on('change', this.onEventStartTimeChange.bind(this));
  $(this.params.eventEndTimeId).on('change', this.onEventEndTimeChange.bind(this));
  // Event Edit/Cancel clicks
  $(this.params.adminEventCancelId).on('click', this.onEventEditCanceled.bind(this));
  $(this.params.adminEventEditId).on('click', this.editEventCall.bind(this));
  // Event that gets fired when a event to edit in the admin modal is selected
  $(this.params.eventEditListId).on('change', this.onEventEditListChange.bind(this));
  // Event that creates an event from the admin modal
  $(this.params.adminEventCreateId).on('click', this.createEventCall.bind(this));
  // Event for deleting an event from the admin modal
  $(this.params.adminEventDeleteId).on('click', this.deleteEventCall.bind(this));
  // Event for DlEvent project radio buttons
  $(this.params.eventRadioAllId).on('change', this.onEventRadioAllChange.bind(this));
  $(this.params.eventRadioAppId).on('change', this.onEventRadioAppChange.bind(this));

  // Event listeners for Notifications tab
  $(this.params.notifTitleInputId).on('keyup', this.onNotifTitleChange.bind(this));
  $(this.params.notifIntroInputId).on('keyup', this.onNotifIntroChange.bind(this));
  $(this.params.notifVendorListId).on('change', this.onVendorIdChange.bind(this));
  $(this.params.notifStartDateId).on('change', this.onNotifStartDateChange.bind(this));
  $(this.params.notifEndDateId).on('change', this.onNotifEndDateChange.bind(this));
  $(this.params.notifStartTimeId).on('change', this.onNotifStartTimeChange.bind(this));
  $(this.params.notifEndTimeId).on('change', this.onNotifEndTimeChange.bind(this));
  // Notification Edit/Cancel clicks
  $(this.params.adminNotifCancelId).on('click', this.onNotifEditCanceled.bind(this));
  $(this.params.adminNotifEditId).on('click', this.editNotificationCall.bind(this));
  // Event that gets the list for the selected category in the admin modal
  $(this.params.notifCategoryListId).on('change', this.getAdminTempListCall.bind(this));
  // Event that gets fired when a notification to edit in the admin modal is selected
  $(this.params.notifEditListId).on('change', this.onNotifEditListChange.bind(this));
  // Event for creating the notifications list
  $(this.params.adminNotifCreateId).on('click', this.createNotificationCall.bind(this));
  // Event for deleting a notification in the admin modal
  $(this.params.adminNotifDeleteId).on('click', this.deleteNotificationCall.bind(this));

  // Event that is fired when a tab is switched in the admin modal
  $(this.params.adminModalTabListLiId).on('click', 'a', this.switchTab.bind(this));
  // Event for Notifications project radio buttons
  $(this.params.notifRadioAllId).on('change', this.onNotifRadioAllChange.bind(this));
  $(this.params.notifRadioAppId).on('change', this.onNotifRadioAppChange.bind(this));
};

var DlEventsManager = function () {
  this.p = {
    localId: '#dl-event',
    initialized: false,
    isModalOpen: false,
    modal: {},
    modalId: '#dl-event-modal',
    dlEventGroups: {},
    dlEventList: [],
    sideNavTrigger: undefined,
    dlEventFile: {},
    dlEvents: [],
    dlEventBodyId: '#dl-event-modal-body',
    dlEventFileContentId: '#dl-event-file-content',
    dlEventTitleId: '#dl-event-modal-title',
    sideNavCollapsibleId: '#dl-event-sidenav .collapsible',
    sideNavId: '#dl-event-sidenav',
    sideNavToolTipId: '#dl-event-sidenav-trigger.tooltipped',
    sideNavTriggerId: '#dl-event-sidenav-trigger',
  };

  /**
   * Function that initializes the dlEvents sidenav and the collapsibles inside of it.
   */
  this.initSideNav = function () {
    var that = this;
    $(this.p.sideNavTriggerId).sideNav({
      menuWidth: '25rem', // Default is 300
      edge: 'left', // Choose the horizontal origin
      closeOnClick: true, // Closes side-nav on <a> clicks, useful for Angular/Meteor
      draggable: true, // Choose whether you can drag to open on touch screens
      onOpen: function (el) {
        var trigger = el[0].nextElementSibling;
        $(trigger).addClass('open');
        $(trigger).find('i').html('navigate_before');
        that.initTooltip('opened');
      }, // A function to be called when sideNav is opened
      onClose: function (el) {
        var trigger = el[0].nextElementSibling;
        $(trigger).removeClass('open');
        $(trigger).find('i').html('navigate_next');
        that.initTooltip('closed');
      }, // A function to be called when sideNav is closed
    });
    this.initTooltip('closed');
    $(this.p.sideNavCollapsibleId).collapsible();
    this.p.sideNavTrigger = $(this.p.sideNavTriggerId);
  };

  /**
   * Initialize tooltip for sidenav trigger button
   *
   * @param option This should indicate the current state of the sidenav.
   * Opened means that the tooltip will display
   * "Close DlEvents" and closed will display "Open DlEvents".
   */
  this.initTooltip = function (option) {
    if (option === 'opened') {
      $(this.p.sideNavToolTipId).tooltip('remove');
      $(this.p.sideNavToolTipId).tooltip({ tooltip: 'Close Events', delay: 50, position: 'right' });
    } else if (option === 'closed') {
      $(this.p.sideNavToolTipId).tooltip('remove');
      $(this.p.sideNavToolTipId).tooltip({ tooltip: 'Open Events', delay: 50, position: 'right' });
    }
  };

  /**
   * This checks to see if the piece of string is HTML or not.
   */
  this.isHTML = function (str) {
    var html = null;

    try {
      html = $(str);
    } catch (error) {
      return false;
    }

    if (html.length === 0) {
      return false;
    } else {
      return html[0].nodeType === 1;
    }
  };

  /**
   * Function that removes all text and elements from the title, body, and file content
   * in the DlEvent modal.
   */
  this.clearDlEventModalContent = function () {
    $(this.p.dlEventTitleId)[0].innerText = '';

    if ($(this.p.dlEventBodyId).children().length > 0) {
      $(this.p.dlEventBodyId).children().remove();
    } else {
      $(this.p.dlEventBodyId)[0].innerText = '';
    }

    if ($(this.p.dlEventFileContentId).children().length > 0) {
      $(this.p.dlEventFileContentId).children().remove();
      $(this.p.dlEventFileContentId).removeClass('active');
    }
  };

  /**
   * Function to close the dlEvents modal.
   */
  this.close = function () {
    this.p.modal.modal('close');
  };

  /**
   * Function to close the dlEvents sidenav.
   */
  this.closeSideNav = function () {
    $(this.p.sideNavTrigger).sideNav('hide');
  };

  /**
   * Function that creates a list of collapsibles for each dlEvent group and a list of <li>'s in
   * each collapsible for each dlEvent inside their respective group.
   */
  this.createSideNavList = function (dlEvents) {
    var eventsLength = $('#dl-event-sidenav').find('li[id*="dl-event-item-"]').length;

    if (eventsLength > 0 && dlEvents.length > 0) {
      $('#dl-event-sidenav').find('li[id*="dl-event-item-"]').remove();
    }

    for (var k = 0; k < dlEvents.length; k++) {
      var id = dlEvents[k].id;
      var title = dlEvents[k].title;

      var eDate = new Date(dlEvents[k].lastEdit);
      var editDate = this.getFormattedDate(eDate);

      var sDate = new Date(dlEvents[k].startTime);
      var startTime = this.getFormattedDate(sDate);

      var item = $(
        '<li class="dl-event-li-item" id="dl-event-item-' +
          id +
          '" data="' +
          id +
          '"><a class="dl-event-title-text" href="#!">' +
          title +
          '</a>' +
          '<div class="dl-event-date">Event Starts: ' +
          startTime +
          '</div>' +
          '<div class="dl-event-date">Last Edit: ' +
          editDate +
          '</div></li>'
      );

      $(item).on('click', this.onSideNavItemClick.bind(this));
      $('ul' + this.p.sideNavId).append($(item));
    }
  };

  this.getFormattedDate = function (date) {
    return date.getMonth() + 1 + '/' + date.getDate() + '/' + date.getYear();
  };

  /**
   * Function that gets forecast dl-events and populates the dl-events modal.
   * */
  this.getDlEvents = function () {
    $.ajax({
      context: this,
      url: '/Home/GetDlEvents',
      async: true,
      dataType: 'json',
      type: 'POST',
      success: function (x) {
        // Get all the dlEvents and cache them for faster access.
        this.p.dlEvents = x.map(function (value, index) {
          return {
            id: value.EventId,
            title: value.Title,
            body: value.Body,
            fileId: value.FileId,
            startTime: value.StartTime,
            endTime: value.EndTime,
            lastEdit: value.LastEdit,
          };
        });

        this.p.dlEvents = this.sortDlEventsByStartDate(this.p.dlEvents);
        var sideNavItemCount = $(this.p.sideNavId).find('li[id*="dl-event-item-"]').length;

        // Here we check to see that we have all the dl-events incase there are new ones
        if (sideNavItemCount === 0 || (sideNavItemCount !== 0 && sideNavItemCount !== this.p.dlEvents.length)) {
          this.createSideNavList(this.p.dlEvents);
          this.initSideNav();
        }
      },
      error: function (x) {},
    });
  };

  /**
   * Function that builds a dl-event iframe to be placed in the dl-events modal.
   *
   * @param {any} link WebViewLink to the file.
   */
  this.getDlEventFile = function (link) {
    var e = '/edit',
      v = '/view',
      pr = '/preview';

    link = link.includes(e) ? link.replace(e, pr) : link.replace(v, pr);

    return $('<embed id="dl-event-file-display" src="' + link + '" style="width: 100%; height: 94%; border: none;"></embed>')[0];
  };

  /**
   * Function that returns a dlEvent based on its ID.
   *
   * @param {number} id The ID of the dl-event.
   */
  this.getDlEventById = function (id) {
    return this.p.dlEvents.filter(function (x, y) {
      return x.id === id;
    })[0];
  };

  /**
   * Initialize and open the dl-events modal.
   *
   * @param id Optionally pass in a dl-event id to open the modal to that dl-event.
   */
  this.open = function (id) {
    this.getDlEvents();

    if (!this.p.initialized) {
      this.init();
    }

    if (id) {
      this.showDlEventById(id);
    }

    if (!this.p.isModalOpen) {
      this.p.modal.modal('open');
    }
  };

  /**
   * Function to open the dl-events sidenav.
   */
  this.openSideNav = function () {
    $(this.p.sideNavTrigger).sideNav('show');
  };

  /**
   * Event listener for dl-event items. When a dl-event is clicked that dl-event is displayed
   * in the modal while the previous dl-event is removed.
   *
   * @param {*} e The dl-event element that was clicked on.
   */
  this.onSideNavItemClick = function (e) {
    var id = parseInt(e.currentTarget.attributes.data.value);

    this.showDlEvent(this.getDlEventById(id));
  };

  /**
   * Function to get and dlEvent from the database and show it on the dlEvent modal.
   *
   * @param {any} dlEvent The dlEvent option object that was clicked on.
   * @param {any} open A boolean of true or ( false or nothing ). To open set to true
   * if not then leave it out or set it to false.
   */
  this.showDlEvent = function (dlEvent, open) {
    if (dlEvent.fileId) {
      $.ajax({
        context: this,
        url: '/Home/GetDlEventFile',
        async: true,
        dataType: 'json',
        type: 'POST',
        data: { fileId: dlEvent.fileId },
        success: function (x) {
          this.p.dlEventFile = x;
          this.setDlEvent(dlEvent, x, open);
        },
        error: function (x) {},
      });
    } else {
      this.setDlEvent(dlEvent, '', open);
    }
  };

  this.showDlEventById = function (id) {
    if (!id) {
      return;
    }

    this.showDlEvent(this.getDlEventById(id), true);
  };

  /**
   * Function to display a DlEvent in the DlEvents modal.
   *
   * @param dlEvent A DlEvent object to set.
   * @param file A file WebViewLink.
   * @param open A boolean of true or false. To open the modal set to true else dont set at all
   */
  this.setDlEvent = function (dlEvent, file, open) {
    this.clearDlEventModalContent();

    $(this.p.dlEventTitleId)[0].innerText = dlEvent.title;

    if (this.isHTML(dlEvent.body)) {
      $(this.p.dlEventBodyId).append($(dlEvent.body));
    } else if (dlEvent.body.length > 0) {
      var paragraph = $('<p>' + dlEvent.body + '</p>');
      $(this.p.dlEventBodyId).append($(paragraph)[0]);
    }

    if (file && file.WebViewLink.length > 0) {
      $(this.p.dlEventFileContentId).addClass('active');
      $(this.p.dlEventFileContentId).append(this.getDlEventFile(file.WebViewLink));
    }

    if (open && open === true) {
      this.open();
      this.showDlEvent(dlEvent);
    }
  };

  /**
   * Sorts a list of dl-events by their titles ignoring case sensitivity.
   *
   * @param dlEvents
   */
  this.sortDlEvents = function (dlEvents) {
    return dlEvents.sort(function (a, b) {
      return a.title.toLowerCase() < b.title.toLowerCase() ? -1 : a.title.toLowerCase() > b.title.toLowerCase() ? 1 : 0;
    });
  };

  /**
   * Sorts a list of dl-events by their start dates.
   *
   * @param dlEvents
   */
  this.sortDlEventsByStartDate = function (dlEvents) {
    return dlEvents.sort(function (a, b) {
      return new Date(b.startTime) - new Date(a.startTime);
    });
  };

  /**
   * Function that initializes the dl-events modal.
   */
  this.init = function () {
    var that = this;
    this.p.modal = $(this.p.modalId).modal({
      complete: function (e) {
        that.closeSideNav();
        that.isModalOpen = false;
      },
      onOpen: function (e) {
        that.isModalOpen = true;
      },
      dismissible: false,
    });
    this.getDlEvents();
    this.p.initialized = true;
  };

  /**
   * Initialize the modal on instantiation.
   */
  this.init();
};

/**
 * Function that gets the offset for left, right, top, bottom coordinates for an element
 * to be placed over another element that was clicked on.
 * @param {any} e The original event that was fired.
 * @param {any} element The element that you want to place. You can pass it in with jquery using $(element).
 * @param {any} align Left or Right align with the element that you clicked on. Currently it'll only
 * check if the element you are placing will overflow to the right of the screen and if so then
 * it will left align it for you instead.
 */
function GetElementXYAsPercent(e, element, align) {
  align = align || 'left';
  var alignLeft = align === 'left';
  // Get window size
  var screenX = $(window).width();
  var screenY = $(window).height();

  var elementWidth = $(element).outerWidth();
  var elementHeight = $(element).outerHeight();
  var parentWidth = $(e.originalEvent.target).outerWidth();
  var parentHeight = $(e.originalEvent.target).outerHeight();

  var xOffset = $(e.originalEvent.target).offset().left;

  var xSide = xOffset + (!alignLeft ? parentWidth : xOffset + elementWidth > screenX ? parentWidth : 0);
  var ySide = $(e.originalEvent.target).offset().top;

  var overFlow = xSide + elementWidth > screenX;
  var right = parseFloat(
    Math.abs((alignLeft ? (overFlow ? 1 : 0) : 1) - (alignLeft ? (overFlow ? xSide : xSide + elementWidth) : xSide) / screenX) * 100
  ).toFixed(2);
  var left = parseFloat((Math.abs(alignLeft ? (overFlow ? xSide - elementWidth : xSide) : xSide - elementWidth) / screenX) * 100).toFixed(2);

  var top = parseFloat((ySide == 0 ? 0 : ySide / screenY) * 100).toFixed(2);
  var bottom = parseFloat(((screenY - (ySide + elementHeight)) / screenY) * 100).toFixed(2);

  return { X: { left: left, right: right }, Y: { top: top, bottom: bottom } };
}

var NotificationsManager = function (em, tm) {
  this.clearAllNotificationsId = '#clear-notifications-list';
  this.dlEventsManager = em;
  this.notifOpen = false;
  this.notifinitialized = false;
  this.notifications = [];
  this.notificationGroups = {};
  this.notifListId = '#notifications-list';
  this.notifTriggerId = '#notifications-nav-trigger';
  this.tutorialsManager = tm;

  this.clearAllNotifications = function () {
    var ids = $(this.notifListId)
      .children()
      .toArray()
      .map(function (x) {
        return x.attributes.notifId.value;
      });
    this.updateViewedNotification(ids, getFormattedTimestamp());
    $(this.notifListId).children().remove();
    this.updateNotificationList([]);
    this.close();
  };

  this.close = function () {
    $(this.notifTriggerId).sideNav('hide');
  };

  /**
   * Function that creates an li items for the notifications ul list.
   * @param {any} item The item object must have the fields:
   * Title -> Title of the item,
   * Body -> Description for the notification item,
   * NotifId -> Notification id,
   * NotifType -> The id for the notification type such as 'event', 'tutorial', etc...,
   * NotifTypeId -> The id for the actual event or tutorial.
   */
  this.createNotificationItem = function (item) {
    var color = 'background-color: ' + (item.NotificationType.toLowerCase() === 'tutorial' ? '#4caf50' : '#039be5');
    var titleDiv = '<div class="title" style="' + color + '">' + item.Title + '</div>';
    var bodyDiv = '<div class="body">' + item.Body + '</div>';
    var icon = '<i class="material-icons waves-effect">clear</i>';
    var li =
      '<li notifId="' +
      item.NotifId +
      '" notifType="' +
      item.NotificationType +
      '"  notifTypeId="' +
      item.NotificationTypeId +
      '" ' +
      'class="notifications-item" > ' +
      icon +
      titleDiv +
      bodyDiv +
      '</li > ';
    return li;
  };

  /**
   * Function that gets all notifications for the current user. After it receives all
   * notifications back from the server it then updates the sidenav list and opens it.
   * */
  this.getNotifications = function () {
    $.ajax({
      context: this,
      url: '/Home/GetNotifications',
      async: true,
      dataType: 'json',
      type: 'POST',
      success: function (x) {
        this.notifications = x.map(function (x, y) {
          return {
            title: x.Title,
            body: x.Body,
            notifId: x.NotifId,
            notifType: x.NotifType,
            notifTypeId: x.NotifTypeId,
          };
        });

        if ($(this.notifListId).children().length !== this.notifications.length) {
          this.updateNotificationList(x);
          Materialize.showStaggeredList(this.notifListId);
        }

        this.updateNotificationBadges(x.length);
      },
      error: function (x) {},
    });
  };

  /**
   * Function to get the count of new notifications and update the
   * notification bubbles.
   * */
  this.getNotificationsCount = function () {
    $.ajax({
      context: this,
      url: '/Home/GetNotificationsCount',
      dataType: 'json',
      type: 'POST',
      async: true,
      success: function (x) {
        if (x > this.notifications.length || x < this.notifications.length) {
          this.getNotifications();
        } else {
          this.updateNotificationBadges(x);
        }
      },
      error: function (x) {},
    });
  };

  /**
   * Function fired by a notification item being clicked.
   */
  this.onNotificationsItemClick = function (e) {
    var notifId = e.currentTarget.attributes.notifid.value;

    if (e.originalEvent.target.localName === 'i' && e.originalEvent.target.innerText === 'clear') {
      this.updateViewedNotification([notifId], getFormattedTimestamp());
    } else {
      var method = RemoveAndUpper(e.currentTarget.attributes.notifType.value, '_');
      var id = parseInt(e.currentTarget.attributes.notiftypeid.value);
      if (method.toLowerCase() === 'tutorial') {
        this.tutorialsManager.showTutorial(id);
      } else if (method.toLowerCase() === 'event') {
        this.close();
        var that = this;
        setTimeout(function () {
          that.dlEventsManager.showDlEventById(id);
        }, 1000);
      }
      this.updateViewedNotification([notifId], getFormattedTimestamp());
    }

    if ($(this.notifListId).children().length < 2) {
      this.updateNotificationList([]);
    }

    $(e.currentTarget).remove();
  };

  /**
   * Function to open the notifications sidenav
   */
  this.open = function () {
    if (!this.notifinitialized) {
      this.init();
    }
    $(this.notifTriggerId).sideNav('show');
  };

  /**
   * Function to update all the notifications bubbles.
   * @param {any} count The number of notifications.
   */
  this.updateNotificationBadges = function (count) {
    var userBadge = 'span#notifications-icon-badge';
    var menuBadge = 'span#notifications-menu-badge';
    $(userBadge).removeClass('invisible');
    $(menuBadge).removeClass('invisible');

    if (count > 0) {
      $(userBadge)[0].innerText = count;
      $(menuBadge)[0].innerText = count;
    } else {
      $(userBadge).addClass('invisible');
      $(menuBadge).addClass('invisible');

      $(userBadge)[0].innerText = count;
      $(menuBadge)[0].innerText = count;
    }
  };

  /**
   * Function to update the notifications list with new notifications or
   * if empty set the background to say no new notifications.
   * @param {any} notifs The list of notifications.
   */
  this.updateNotificationList = function (notifs) {
    $(this.notifListId).children().remove();

    if (notifs.length > 0) {
      notifs = notifs.sort(function (a, b) {
        return a.NotifId < b.NotifId ? 1 : a.NotifId > b.NotifId ? -1 : 0;
      });
    }

    if (notifs.length > 0) {
      var that = this;
      $.each(notifs, function (key, val) {
        var item = that.createNotificationItem(val);
        $(that.notifListId).append(item);
      });
    } else {
      $(this.notifListId).append($('<li class="notifications-empty">No New Notifications</li>'));
    }
  };

  /**
   * Function to set the notification as viewed by the current user.
   * @param {any} notifId the notification id.
   */
  this.updateViewedNotification = function (notifIds, timestamp) {
    notifIds = [].slice.call(notifIds).map(function (x) {
      return parseInt(x);
    });
    $.ajax({
      context: this,
      url: '/Home/UpdateViewedNotification',
      dataType: 'json',
      type: 'POST',
      async: true,
      data: {
        NotifIds: notifIds,
        TimeStamp: timestamp,
      },
      success: function (x) {
        this.getNotificationsCount();
      },
      error: function (x) {},
    });
  };

  /**
   * Function to manage the notifications side nav.
   */
  this.init = function () {
    var that = this;
    $(this.notifTriggerId).sideNav({
      menuWidth: '400px',
      edge: 'right',
      closeOnClick: false,
      onOpen: function (el) {
        Materialize.showStaggeredList(that.notifListId);
        that.notifOpen = true;
      },
      onClose: function (e) {
        that.getNotificationsCount();
        that.getNotifications();
        that.notifOpen = false;
      },
    });
    this.notifinitialized = true;
    this.getNotifications();
  };

  // Event to close and destroy the notification sideNav
  $('.notifications-header').on('click', 'i', this.close.bind(this));
  $(this.clearAllNotificationsId).on('click', this.clearAllNotifications.bind(this));

  // Event for either opening a notification or closing it
  $(this.notifListId).on('click', '.notifications-item', this.onNotificationsItemClick.bind(this));

  /**
   * Initialize upon instantiation
   */
  this.init();
};

function getFormattedDate(date) {
  return date.getMonth() + '/' + date.getDay() + '/' + date.getYear();
}

function getFormattedTimestamp(date) {
  var date = date || new Date();
  var str =
    date.getFullYear() +
    '-' +
    (date.getMonth() + 1) +
    '-' +
    date.getDate() +
    ' ' +
    date.getHours() +
    ':' +
    date.getMinutes() +
    ':' +
    date.getSeconds();

  return str;
}

var TutorialsManager = function () {
  this.collapsibleList;
  this.colorClass = 'teal';
  this.localId = 'tutorial';
  this.initialized = false;
  this.isModalOpen = false;
  this.modal;
  this.modalId = '#tutorial-modal';
  this.tutorials = [];
  this.tutorialGroups = {};
  this.tutorialList = [];
  this.sideNavTrigger;

  /**
   * Function that initializes the tutorials sidenav and the collapsibles inside of it.
   */
  this.initSideNav = function () {
    var that = this;
    $('#tutorial-sidenav-trigger').sideNav({
      menuWidth: '24rem', // Default is 300
      edge: 'left', // Choose the horizontal origin
      closeOnClick: true, // Closes side-nav on <a> clicks, useful for Angular/Meteor
      draggable: true, // Choose whether you can drag to open on touch screens
      onOpen: function (el) {
        var trigger = el[0].nextElementSibling;
        $(trigger).addClass('open');
        $(trigger).find('i').html('navigate_before');
        that.initTooltip('opened');
      }, // A function to be called when sideNav is opened
      onClose: function (el) {
        var trigger = el[0].nextElementSibling;
        $(trigger).removeClass('open');
        $(trigger).find('i').html('navigate_next');
        that.initTooltip('closed');
      }, // A function to be called when sideNav is closed
    });
    this.initTooltip('closed');
    this.collapsibleList = $('#tutorial-sidenav .collapsible').collapsible();
    this.sideNavTrigger = $('#tutorial-sidenav-trigger');
  };

  /**
   * Initialize tooltip for sidenav trigger button
   * @param option This should indicate the current state of the sidenav. Opened means that the tooltip will display
   * "Close Tutorials" and closed will display "Open Tutorials".
   */
  this.initTooltip = function (option) {
    if (option === 'opened') {
      $('#tutorial-sidenav-trigger.tooltipped').tooltip('remove');
      $('#tutorial-sidenav-trigger.tooltipped').tooltip({ tooltip: 'Close Tutorials', delay: 50, position: 'right' });
    } else if (option === 'closed') {
      $('#tutorial-sidenav-trigger.tooltipped').tooltip('remove');
      $('#tutorial-sidenav-trigger.tooltipped').tooltip({ tooltip: 'Open Tutorials', delay: 50, position: 'right' });
    }
  };

  /**
   * Function to close the tutorials modal.
   */
  this.close = function () {
    this.modal.modal('close');
  };

  /**
   * Function to close the tutorials sidenav.
   */
  this.closeSideNav = function () {
    $(this.sideNavTrigger).sideNav('hide');
  };

  /**
   * Function that creates a list of collapsibles for each tutorial group and a list of <li>'s in
   * each collapsible for each tutorial inside their respective group.
   */
  this.createSideNavList = function (tutorialGroups) {
    var tutsLength = $('#tutorial-sidenav').find('li[id*="group-item"]').length;
    var groupLength = Object.keys(tutorialGroups).length;

    if (tutsLength > 0 && groupLength > 0) {
      $('#tutorial-sidenav').find('li[class="no-padding"]').remove();
    }

    var sortedGroupNames = Object.keys(tutorialGroups).sort(function (x, y) {
      return x.toLowerCase() < y.toLowerCase() ? -1 : x.toLowerCase() > y.toLowerCase() ? 1 : 0;
    });

    for (var i = 0; i < groupLength; i++) {
      var groupName = sortedGroupNames[i];
      var gm = groupName.split(' ').join('_');

      var currGroup = $(
        '<li class="no-padding">' +
          '<ul id="tutorial-' +
          gm +
          '-collapsable" class="collapsible collapsible-accordion">' +
          '<li>' +
          '<a class="collapsible-header">' +
          groupName +
          '<i class="material-icons">arrow_drop_down</i></a>' +
          '<div class="collapsible-body">' +
          '<ul id="tutorial-group-' +
          gm +
          '"></ul>' +
          '</div>' +
          '</li>' +
          '</ul>' +
          '</li> '
      );

      var currGroupList = $(currGroup).find('#tutorial-group-' + gm);

      for (var k = 0; k < tutorialGroups[groupName].length; k++) {
        var id = tutorialGroups[groupName][k].tutorialId;
        var title = tutorialGroups[groupName][k].title;
        var lastEdit = new Date(tutorialGroups[groupName][k].lastEdit).toLocaleDateString();
        var item = $(
          '<li id="group-item-' +
            id +
            '" data="' +
            id +
            '" class="dl-tutorial-group-item"><a href="#!">' +
            title +
            '</a><div class="dl-tutorial-date">Last Edit: ' +
            lastEdit +
            '</div></li>'
        );
        $(item).on('click', this.onSideNavItemClick.bind(this));
        $(currGroupList).append(item);
      }

      $('ul#tutorial-sidenav').append(currGroup);
    }
  };

  /**
   * Function that gets forecast tutorials and populates the tutorials modal.
   * */
  this.getTutorials = function () {
    $.ajax({
      context: this,
      url: '/Home/GetTutorials',
      async: true,
      dataType: 'json',
      type: 'POST',
      success: function (x) {
        // Get all the tutorials and cache them for faster access.
        this.tutorials = x.map(function (value, index) {
          return {
            title: value.Title,
            intro: value.Intro,
            tutorialGroup: value.TutorialGroup,
            tutorialId: value.TutorialId,
            videoLink: value.VideoLink,
            lastEdit: value.LastEdit,
          };
        });
        this.tutorials = this.sortTutorials(this.tutorials);
        this.tutorialGroups = this.getGroupLists(this.tutorials);
        // Here we check to see that we have all the tutorials incase there are new ones
        if ($('#tutorial-sidenav').find('li[id*="group-item"]').length <= this.tutorials.length) {
          this.createSideNavList(this.tutorialGroups);
          this.initSideNav();
        }
        this.tutorialList = this.getTutorial(this.tutorials[0]);
        this.updateTutorialModal(this.tutorialList);
      },
      error: function (x) {},
    });
  };

  /**
   * Here we split up the tutorials into their respective groups and return an
   * object with a property corresponding to each group. Each property has an array or
   * tutorial objects.
   * @param tutorials An array of tutorial objects.
   */
  this.getGroupLists = function (tutorials) {
    const groupList = {};
    for (var i = 0; i < tutorials.length; i++) {
      var tut = tutorials[i];
      if (groupList[tut.tutorialGroup]) {
        groupList[tut.tutorialGroup].push(tut);
      } else {
        groupList[tut.tutorialGroup] = [];
        groupList[tut.tutorialGroup].push(tut);
      }
    }

    for (var prop in groupList) {
      groupList[prop] = this.sortTutorials(groupList[prop]);
    }
    return groupList;
  };

  /**
   * Function that builds a tutorial to be placed in the tutorials modal.
   * @param {any} items A single tutorial.
   */
  this.getTutorial = function (item) {
    var tutorialItem = $(
      '<div id="tutorial' +
        item.tutorialId +
        '" class="row">' +
        '<div class="col s12 modal-head tutorial-title"><span class="flow-text">' +
        item.title +
        '</span></div>' +
        '<div class="col s12 vert-split-container">' +
        '<section>' +
        '<div class="col s6 tutorial-tt"><span class="flow-text"><div>' +
        item.intro +
        '</div></span></div>' +
        '<div class="divider"></div>' +
        '<div class="col s6 valign-wrapper tutorial-video-container" > <iframe width="640" height="360" src="' +
        item.videoLink +
        '" frameborder="0" allow="autoplay; encrypted-media" allowfullscreen></iframe></div>' +
        '</section>' +
        '</div>' +
        '</div>'
    );

    return tutorialItem[0];
  };

  /**
   * Function that returns a tutorial based on its ID.
   * @param {number} id The ID of the tutorial.
   */
  this.getTutorialById = function (id) {
    return this.tutorials.filter(function (x, y) {
      return x.tutorialId === id;
    })[0];
  };

  /**
   * Initialize and open the tutorials modal.
   * @param id Optionally pass in a tutorial id to open the modal to that tutorial.
   */
  this.open = function (id) {
    if (!this.initialized) {
      this.init();
    }
    if (id) {
      this.updateTutorialModal(this.getTutorial(this.getTutorialById(id)));
    }
    if (!this.isModalOpen) {
      this.modal.modal('open');
    }
  };

  /**
   * Function to open the tutorials sidenav.
   */
  this.openSideNav = function () {
    $(this.sideNavTrigger).sideNav('show');
  };

  /**
   * Event listener for tutorial items. When a tutorial is clicked that tutorial is displayed
   * in the modal while the previous tutorial is removed.
   * @param {*} e The tutorial element that was clicked on.
   */
  this.onSideNavItemClick = function (e) {
    var id = parseInt(e.currentTarget.attributes.data.value);
    this.showTutorial(id);
  };

  /**
   * Function to show a certain tutorial in the tutorials modal.
   * @param {any} Tutorial Pass in the option object that was clicked.
   */
  this.showTutorial = function (id) {
    var tutorial = this.getTutorialById(id);
    var tut = this.getTutorial(tutorial);
    this.updateTutorialModal(tut);
    if (!this.isModalOpen) {
      this.open();
    }
  };

  /**
   * Sorts a list of tutorials by their titles ignoring case sensitivity.
   * @param tutorials
   */
  this.sortTutorials = function (tutorials) {
    return tutorials.sort(function (a, b) {
      return a.title.toLowerCase() < b.title.toLowerCase() ? -1 : a.title.toLowerCase() > b.title.toLowerCase() ? 1 : 0;
    });
  };

  /**
   * Function to update the tutorials modal by removing all children and repopulating it.
   * @param {any} tutorial A tutorial item as HTML.
   */
  this.updateTutorialModal = function (tutorial) {
    $('#' + this.localId + '-modal-row').modal();
    $('#' + this.localId + '-modal-row')
      .children()
      .remove();

    $('#tutorial-modal-row').append($(tutorial));
  };

  /**
   * Function that initializes the tutorials modal.
   */
  this.init = function () {
    var that = this;
    this.modal = $(this.modalId).modal({
      complete: function (e) {
        that.closeSideNav();
        that.isModalOpen = false;
      },
      onOpen: function (e) {
        that.isModalOpen = true;
      },
      dismissible: false,
    });
    this.getTutorials();
    this.initialized = true;
  };

  /**
   * Initialize the modal on instantiation.
   */
  this.init();
};

/**
 * Replace a character at a given index in a string.
 * @param str The original string
 * @param index The 0-based index of the character to be replaced
 * @param replacement The replacement character or string
 */
function replaceAt(str, index, replacement) {
  return str.substr(0, index) + replacement + str.substr(index + str.length);
}

/**
 * Function to remove a certain repeated character in a string and concatenate
 * each word and capitalize the first character of each word.
 * @param {any} word The string that you want to camel case.
 * @param {any} c The repeated character.
 */
function RemoveAndUpper(word, c) {
  var wordArr = word.split(c);
  var newWord = '';
  $.each(wordArr, function (key, val) {
    newWord += val.charAt(0).toUpperCase() + val.slice(1);
  });

  return newWord;
}

//*********************************************
//*         End Notifications                 *
//*********************************************
