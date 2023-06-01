var ExceptionsTabModule = (function (args) {
  /**
   * Private variables for the Exceptions tab module.
   */
  var _gmsVenId = args.gmsVenId;
  var _ipoTableId = '#' + args.tableId;
  var _overlappingIPOTable = Object.create({});
  var _itemPatchesToRemove = [];
  var _isForecastTableStateDirty = false;
  var _window = args.context;

  var _ipoTableParams = {
    customOrder: [],
    isMM: _window.isMerchandisingManager,
    isMD: _window.isMerchandisingDirector,
  };

  var CreateOverlappingIPOTable = function () {
    _overlappingIPOTable = $(_ipoTableId).DataTable({
      processing: true,
      serverSide: true,
      deferRender: true,
      preDrawCallback: function (settings) {
        if (DEBUG) console.log('IPO Table draw() called');
      },
      stateSave: true,
      stateSaveCallback: function (settings, data) {
        _ipoTableParams = data;
        var order = data.order;
        var isArray = Array.isArray(order[0]);
        if (!isArray) {
          order = [order];
        }
        _ipoTableParams.customOrder = order.map(function (d) {
          var index = d[0];
          var dir = d[1];
          var name = settings.aoColumns[index].data;
          return { name: name, dir: dir, index: index };
        });
        _ipoTableParams.isMM = _window.isMerchandisingManager;
        _ipoTableParams.isMD = _window.isMerchandisingDirector;
        localStorage.setItem('DL_IPO_State', JSON.stringify(_ipoTableParams));
      },
      stateLoadCallback: function (settings, callback) {
        try {
          var tempState = JSON.parse(localStorage.getItem('DL_IPO_State'));
          return tempState;
        } catch (e) {
          return JSON.parse(localStorage.getItem('DataTables_IPOTable_/'));
        }
      },
      table: '#IPOTable',
      autoWidth: false,
      scrollX: true,
      scrollY: 500,
      paging: true,
      pagingType: 'full_numbers',
      lengthChange: true,
      orderCellsTop: true,
      colReorder: false,
      order: [0, 'asc'],
      lengthMenu: [20, 50, 100, 500, 1000],
      filter: true,
      initComplete: function () {
        if (DEBUG) console.log('IPO Table Init Complete');
      },
      language: {
        zeroRecords: 'There are no records that match your currently selected filters.',
      },
      rowCallback: function (row, data, index) {},
      dom: 'Brtip',
      select: isSelectionEnabled(),
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
            text: 'Remove Selected Claims',
            action: function () {
              if (_gmsVenId !== 0) {
                _itemPatchesToRemove = [];
                $.each(GetOverlappingIPOTable().rows({ selected: true }).data(), function (i, row) {
                  _itemPatchesToRemove.push({ ItemID: row.ItemID, ItemDesc: row.ItemDesc, Patch: row.Patch });
                });

                if (_itemPatchesToRemove.length === 0) {
                  alert(
                    "You have not selected any rows to remove a claim(s) from. Please select a row by clicking on a checkbox in the 'Remove Claims' column."
                  );
                } else {
                  removeItemPatchClaims(_itemPatchesToRemove);
                }
              } else {
                alert('You do not have access to this button.');
              }
            },
          },
          {
            text: 'Refresh Table Data',
            action: function () {
              if (!_thisExceptionObject.isEmpty() && !_thisExceptionObject.ipoOverlapTable.isEmpty()) {
                _thisExceptionObject.ipoOverlapTable.draw();
              }
            },
          },
        ].map(function (x) {
          if (_gmsVenId !== 0) {
            return x;
          } else {
            if (x.text !== 'Remove Selected Claims') {
              return x;
            }
          }
        }),
      },
      ajax: {
        url: '/Home/GetOverlappingClaimsTable',
        type: 'POST',
        datatype: 'json',
        async: true,
        dataSrc: function (x) {
          return x.data;
        },
        beforeSend: function () {
          ShowProcessingLoader();
        },
        complete: function () {
          if (DEBUG) console.log('IPO Table Completed');
          HideProcessingLoader();
        },
        data: function (e) {
          if (e) {
            e.isMM = _window.isMerchandisingManager;
            e.isMD = _window.isMerchandisingDirector;
            return e;
          }
        },
      },
      error: function (xhr, status, error) {
        if (DEBUG) console.log('IPO Table Error: ', err.Message);
      },
      columns: [
        { sName: 'VendorDesc', data: 'VendorDesc', searchable: true, orderable: true, className: 'dt-center', visible: true },
        { sName: 'RequestingOwners', data: 'RequestingOwners', searchable: true, orderable: false, className: 'dt-center', visible: true },
        { sName: 'ItemID', data: 'ItemID', searchable: true, orderable: true, className: 'dt-center', visible: true },
        { sName: 'ItemDesc', data: 'ItemDesc', searchable: true, orderable: true, className: 'dt-center', visible: true },
        { sName: 'Patch', data: 'Patch', searchable: true, orderable: true, className: 'dt-center', visible: true },
        { sName: 'MM', data: 'MM', searchable: true, orderable: true, className: 'dt-center', visible: true },
        { sName: 'MD', data: 'MD', searchable: true, orderable: true, className: 'dt-center', visible: true },
        {
          sName: 'RemoveClaim',
          type: 'checkbox',
          label: 'checkbox',
          searchable: false,
          orderable: false,
          className: 'select-checkbox' + getIsCheckBoxDisabledClass(),
          defaultContent: '',
        },
      ],
    });

    function getIsCheckBoxDisabledClass() {
      if (_gmsVenId !== 0) {
        return '';
      } else {
        return ' disabled';
      }
    }

    function isSelectionEnabled() {
      if (_gmsVenId !== 0) {
        return {
          style: 'multi',
          selector: 'td:last-child',
        };
      } else {
        return false;
      }
    }
  };

  function GetOverlappingIPOTable() {
    return _overlappingIPOTable;
  }

  // Proves convenient for checking empty objecs.
  if (!Object.prototype.isEmpty) {
    Object.defineProperty(Object.prototype, 'isEmpty', {
      value: function () {
        for (var k in this) {
          if (this.hasOwnProperty(k)) {
            return false;
          }
        }
        return true;
      },
      enumerable: false,
    });
  }

  /**
   * Calls to remove a vendor's claim on a list of item/patch combos.
   * @param {any} itemPatches A list of itemId/patch's comboes to remove claim from.
   */
  function removeItemPatchClaims(itemPatches) {
    if (_gmsVenId !== 0) {
      $.ajax({
        url: '/Home/RemoveItemPatchOwnershipClaims',
        async: true,
        dataType: 'json',
        method: 'POST',
        beforeSend: function (jqXHR, settings) {
          ShowProcessingLoader();
        },
        data: {
          itemPatches: itemPatches,
        },
        success: function (x) {
          HideProcessingLoader();
          if (x.success) {
            alert(x.message);
            HideProcessingLoader();
          } else {
            alert(x.message);
            if (x.fileName && x.fileName.length > 0) {
              window.location.href = '/Home/GetFile?filePath=' + x.fileName;
            } else {
              HideProcessingLoader();
            }
          }
          if (_overlappingIPOTable && !_overlappingIPOTable.isEmpty()) {
            _overlappingIPOTable.draw();
          }
          if (typeof x.isPrefreeze !== undefined && !x.isPrefreeze) {
            _isForecastTableStateDirty = !x.isPrefreeze;
          }
        },
        error: function (jqXHR, textStatus, errorThrown) {
          HideProcessingLoader();
        },
      });
    }
  }

  var _thisExceptionObject = {
    createOverlappingIPOTable: function () {
      var oTable = Object.assign({}, GetOverlappingIPOTable());
      if (!oTable || oTable.isEmpty()) {
        CreateOverlappingIPOTable();
      }
    },
    id: _gmsVenId,
    ipoOverlapTable: {
      table: GetOverlappingIPOTable,
      columnAdjustDraw: function () {
        if (_overlappingIPOTable) {
          _overlappingIPOTable.columns.adjust().draw();
        }
      },
      draw: function (createIfNull) {
        if (!_overlappingIPOTable.isEmpty()) {
          _overlappingIPOTable.draw();
        } else if (createIfNull) {
          _thisExceptionObject.createOverlappingIPOTable();
        }
      },
      isVisible: function () {
        var isIPOTableVisible = $('#ipo_table_wrapper').is(':visible');
        return isIPOTableVisible;
      },
      params: function () {
        return _ipoTableParams;
      },
    },
    isForecastTableDirty: function () {
      return _isForecastTableStateDirty;
    },
    setForecastTableStateClean: function () {
      _isForecastTableStateDirty = false;
    },
  };

  return _thisExceptionObject;
})({ context: this, gmsVenId: parseInt(document.getElementById('GMSVenID').value), tableId: 'ipo_table' });
