var ForecastDebugger = (function (isDebugging) {
  if (!isDebugging) {
    return;
  }

  var stateTimeName = 'DL_StaleStateTime';
  var lastDownload = new Map();

  return {
    deleteStateTime: function () {
      localStorage.removeItem(stateTimeName);
    },
    getLastDownloadedFiles: function (downloadType) {
      return lastDownload.get(downloadType);
    },
    getStaleStateTime: function () {
      try {
        var time = localStorage.getItem(stateTimeName);
        return time;
      } catch (e) {
        return 'undefined';
      }
    },
    setDownloadedFile: function (downloadType, fileName) {
      lastDownload.set(downloadType, fileName);
    },
    setStaleStateTime: function (time) {
      localStorage.setItem(stateTimeName, time);
    },
  };
})(window.DEBUG);
