var Util = (function () {
  function seconds(s) {
    return s ? 1000 * s : 0;
  }

  function minutes(m) {
    return m ? seconds(60) * m : 0;
  }

  function hours(h) {
    return h ? minutes(60) * h : 0;
  }

  function days(d) {
    return d ? hours(24) * d : 0;
  }

  /**
   *
   * @param {any} d
   * @param {any} h
   * @param {any} m
   * @param {any} s
   */
  function getTimeMillis(d, h, m, s) {
    return days(d) + hours(h) + minutes(m) + seconds(s);
  }

  return {
    /**
     * Gets the provided amount of time in milliseconds
     * @param {any} days How many days
     * @param {any} hours How many hours
     * @param {any} minutes How many minutes
     * @param {any} seconds How many seconds
     */
    getTimeMillis: function (days, hours, minutes, seconds) {
      return getTimeMillis(days, hours, minutes, seconds);
    },
  };
})();
