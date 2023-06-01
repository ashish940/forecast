using Forecast.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using Vertica.Data.VerticaClient;

namespace Forecast.Models
{
    public class CompareIgnoreCase : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x.Equals(y, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }

    public class IgnoreCaseStringComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class StringListHelper
    {
        public static bool ContainsAllIgnoreCase(this List<string> sl, List<string> strList)
        {
            if(sl.Count < strList.Count)
            {
                foreach(var s in sl)
                {
                    if (!strList.Contains(s, new CompareIgnoreCase()))
                        return false;
                }
            }
            else
            {
                foreach (var s in strList)
                {
                    if (!sl.Contains(s, new CompareIgnoreCase()))
                        return false;
                }
            }

            return true;
        }

        public static string JoinWithWrap(this List<string> list, string join, string wrap)
        {
            var str = "";
            for (var i = 0; i < list.Count(); i++)
            {
                str += (i < (list.Count() - 1)) ? $"{wrap}{list[i]}{wrap}{join}" : $"{wrap}{list[i]}{wrap}";
            }

            return str;
        }

        public static string JoinWithWrap(this List<int> list, string join, string wrap)
        {
            var str = "";
            for (var i = 0; i < list.Count(); i++)
            {
                str += (i < (list.Count() - 1)) ? $"{wrap}{list[i]}{wrap}{join}" : $"{wrap}{list[i]}{wrap}";
            }

            return str;
        }
    }

    public class Util
    {
        public delegate string NoParamFunc();
        public delegate string SingleParamFunc(string s1);
        public delegate string DoubleParamFunc(string s1, string s2);
        public delegate IEnumerable<ExpandoObject> GetExportDataSet(DTParameterModel param, ExportInfo exportInfo);
        public static readonly string VerticaWebConn = ConfigurationManager.ConnectionStrings["VerticaConnectionString"].ConnectionString;
        public static readonly string Qvwebconn = ConfigurationManager.ConnectionStrings["QVWebConnectionString"].ConnectionString;
        public static string FTPVerticaForecastPath = ConfigurationManager.AppSettings.Get("ToolFtpRootDirectory");
        public static string FTPVerticaForecastSuccessPath = $"{FTPVerticaForecastPath}Processed\\";
        public static string FTPVerticaForecastErrorPath = $"{FTPVerticaForecastPath}Error\\";
        public static string FTPVerticaForecastTemplatesPath = $"{FTPVerticaForecastPath}Templates\\";

        public delegate List<object> MapToObject(DataSet ds);
        
        /// <summary>
        /// Executes a query on Vertica.
        /// </summary>
        /// <param name="query">A SQL query to execute on Vertica.</param>
        public static void ExecuteNonQuery(string query)
        {
            _executeNonQuery(query);
        }

        /// <summary>
        /// Gets a string timestamp from a <seealso cref="DateTime"/> object.
        /// </summary>
        /// <param name="value">A <seealso cref="DateTime"/> object that represents the time you want to display as text.
        /// Don't provide a <seealso cref="DateTime"/> if you want a timestamp of now.</param>
        /// <returns></returns>
        public static string GetTimestamp(DateTime value = new DateTime())
        {
            value = (value == new DateTime()) ? DateTime.Now : value;
            return value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Gets a string timestamp from a <seealso cref="DateTime"/> object.
        /// </summary>
        /// <param name="value">A <seealso cref="DateTime"/> object that represents the time you want to display as text.
        /// Don't provide a <seealso cref="DateTime"/> if you want a timestamp of now.</param>
        /// <returns></returns>
        public static string GetTime(long millis)
        {
            var t = TimeSpan.FromMilliseconds(millis);
            var time = t.ToString().Substring(0, 8);
            return time;
        }

        /// <summary>
        /// Executes the provided string and does not return anything.
        /// </summary>
        /// <param name="query">A string SQL query to be executed in Vertica.</param>
        private static void _executeNonQuery(string query)
        {
            VerticaConnection connection = new VerticaConnection(VerticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = query;
                command.ExecuteNonQuery();
                connection.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }

    public static class CSVHelper
    {
        public static List<T> GetList<T>(string filePath, IDictionary<string, string> lookupColumns = null) where T : new()
        {
            var tList = new List<T>();
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    var columnNames = reader.ReadLine().Split(',');
                    while (!reader.EndOfStream)
                    {
                        // Get an instance of T which is the object you provide as T.
                        var tItem = new T();
                        var data = reader.ReadLine().Split(',');

                        for (int j = 0; j < columnNames.Length; j++)
                        {
                            var columnName = columnNames[j];
                            if (lookupColumns != null)
                            {
                                lookupColumns.TryGetValue(columnName, out string newColumnName);
                                columnName = newColumnName;
                            }
                            PropertyInfo property = tItem.GetType().GetProperty(columnName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                            if (property != null && property.CanWrite)
                            {
                                var converter = TypeDescriptor.GetConverter(property.PropertyType);
                                var value = converter.ConvertFromString(data[j]);
                                if (value != null)
                                {
                                    property.SetValue(tItem, value, null);
                                }
                            }
                        }

                        tList.Add(tItem);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return tList;
        }

        public static List<ExpandoObject> GetExpandoList(string filePath, IDictionary<string, string> lookupColumns = null)
        {
            var expandoList = new List<ExpandoObject>();
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    var columnNames = reader.ReadLine().Split(',');
                    while (!reader.EndOfStream)
                    {
                        dynamic expando = new ExpandoObject();
                        var data = reader.ReadLine().Split(',');

                        // Loop through all the column names and assign 
                        for (var i = 0; i < columnNames.Length; i++)
                        {
                            var columnName = columnNames[i];
                            if (lookupColumns != null)
                            {
                                lookupColumns.TryGetValue(columnName, out string newColumnName);
                                columnName = newColumnName;
                            }
                            var val = data[i];
                            expando = ExpandoUtil.AddPropertyWithValue(expando, columnName, val ?? "");
                        }

                        expandoList.Add(expando);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return expandoList;
        }

        public static List<string> GetHeaders(string filePath)
        {
            var headers = new List<string>();
            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    headers = reader.ReadLine().Split(',').ToList();
                    reader.Close();
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return headers;
        }
    }

    public static class ExpandoUtil
    {
        #region Dynamic Object Creation

        /// <summary>
        /// Creates an instance of an <seealso cref="ExpandoObject"/> with properties from a string
        /// list of property names. The property names will appear in the order they were provided in the string list.
        /// </summary>
        /// <param name="propertyNames">A <see cref="List{string}"/> of property names in the order you want them to appaear in the 
        /// <see cref="ExpandoObject"/>.</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ExpandoObject CreateExpandoObject(List<string> propertyNames, Type type)
        {
            dynamic expando = new ExpandoObject();

            foreach (var prop in propertyNames)
            {
                var defaultValue = type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
                expando = AddPropertyWithValue(expando, prop, defaultValue);
            }

            return expando;
        }

        /// <summary>
        /// Method that adds a property to the expando object or assigns a value to an existing one.
        /// </summary>
        /// 
        /// <param name="expando">The expando object to be modified and returned.</param>
        /// <param name="prop">The string name of the property to add or modify.</param>
        /// <param name="val">The object value to be assigned to the property.</param>
        /// <returns>An ExpandoObject with the new property value.</returns>
        public static ExpandoObject AddPropertyWithValue(ExpandoObject expando, string prop, object val)
        {
            var tempExpando = (IDictionary<string, object>)expando;
            tempExpando[prop] = val;
            return (ExpandoObject)tempExpando;
        }

        /// <summary>
        /// Method that builds a single <seealso cref="ExpandoObject"/> from a row in a datareader.
        /// </summary>
        /// 
        /// <param name="reader">A populated data reader. By populated I mean the data reader should have already
        /// called reader.NextResult() before calling this method. You should not call this method if you haven't 
        /// called NextResult() because this method is designed to work inside a loop that is looping through all
        /// the data reader results.</param>
        /// 
        /// <param name="columnNames">An optional list of field names from the data reader. If the array is null then 
        /// they will be populated from the data reader itself. For faster performance it's better to get the list of 
        /// names from the data reader prior to calling this method and you should only call the data reader GetNames()
        /// method once for the entire operation to get the performance benefit.</param>
        /// 
        /// <returns>An ExpandoObject populated with a list of column names as properties and their corresponding
        /// values from the data reader.</returns>
        public static ExpandoObject BuildExpandoFromRow(IDataReader reader, string[] columnNames = null)
        {
            // Get the field names if the list is null
            columnNames = columnNames ?? Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
            dynamic expando = new ExpandoObject();

            // Loop through all the column names and assign 
            for (var i = 0; i < columnNames.Length; i++)
            {
                var val = reader.GetValue(i);
                expando = AddPropertyWithValue(expando, columnNames[i], val is DBNull ? "" : val);
            }

            return expando;
        }

        /// <summary>
        /// Creates a single <seealso cref="ExpandoObject"/> from a given strng property name and an 
        /// <seealso cref="object"/> value.
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static ExpandoObject CreateExpandoObject(string prop, object val)
        {
            dynamic expando = new ExpandoObject();
            expando = AddPropertyWithValue(expando, prop, val is null ? "" : val);
            return expando;
        }

        /// <summary>
        /// Method that builds a single <seealso cref="IDictionary"/> object from an <seealso cref="ExpandoObject"/>.
        /// </summary>
        /// 
        /// <param name="expando">An ExpandoObject to build the Dictionary from.</param>
        /// <returns>An Dictionary object.</returns>
        public static Dictionary<string, object> ExpandoToDictionary(ExpandoObject expando)
        {
            var dict = (IDictionary<string, object>)expando;
            // Loop through all the column names and assign the value to its corresponding key.
            return dict.ToDictionary(keyValue => keyValue.Key, keyValue => keyValue.Value);
        }

        /// <summary>
        /// Used for selecting a single column value from the first row of the table. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnName"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static T GetDbValue<T>(string columnName, string query)
        {
            try
            {
                var result = GetExpandoList(query);
                if (result.Count > 0)
                {
                    result.First().TryGetValue(columnName, out T resultValue);
                    return resultValue;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return default(T);
        }

        /// <summary>
        /// Method that takes a data reader and builds a list of objects. Each object in the list represents
        /// a record from the database selection result. Each object has a field that corresponds to a 
        /// column name from the selection query. Currently all data is converted to a string but 
        /// could possibly be modified for data types later on if the need for it arises.
        /// </summary>
        /// 
        /// <param name="reader">A <seealso cref="IDataReader"/> object.</param>
        /// <returns>A List of ExpandoObjects.</returns>
        public static IEnumerable<ExpandoObject> MapDataReaderToExpando(IDataReader reader)
        {
            if (reader == null) return null;
            if (reader.IsClosed) return null;
            if (reader.FieldCount == 0) return null;

            var listOfExpandos = new List<ExpandoObject>();
            var columnNames = new string[reader.FieldCount];

            try
            {
                // Get the first result to pull the column names into an array
                reader.NextResult();
                columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();

                // Add a row from the reader to the list
                listOfExpandos.Add(BuildExpandoFromRow(reader, columnNames));

                // Loop through the rest of the reader to build the rest of the rows
                while (reader.NextResult())
                {
                    listOfExpandos.Add(BuildExpandoFromRow(reader, columnNames));
                }
            }
            catch (Exception)
            {
                return null;
            }

            return listOfExpandos;
        }

        /// <summary>
        /// Maps a DataRow to a generic object.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static List<ExpandoObject> ToExpandoList(this IDataReader reader)
        {
            var items = new List<ExpandoObject>();
            var columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();

            try
            {
                while (reader.Read())
                {
                    dynamic expando = new ExpandoObject();

                    // Loop through all the column names and assign 
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        var val = reader.GetValue(i);
                        expando = AddPropertyWithValue(expando, columnNames[i], val);
                    }

                    items.Add(expando);
                }
            }
            catch (Exception e)
            {
                reader.Close();
                throw e;
            }

            return items;
        }

        /// <summary>
        /// Maps a DataRow to a generic object.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static List<ExpandoObject> ToExpandoList(this DataTable table)
        {
            var items = new List<ExpandoObject>();

            try
            {
                var rows = table.Rows;
                foreach (DataRow row in rows)
                {
                    dynamic expando = row.ToExpandoObject();
                    items.Add(expando);
                }
            }
            catch (Exception e)
            {
                table.Dispose();
                throw e;
            }

            return items;
        }

        /// <summary>
        /// Maps a <seealso cref="DataRow"/> to a <seealso cref="ExpandoObject"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataRow"></param>
        /// <returns></returns>
        public static ExpandoObject ToExpandoObject(this DataRow dataRow)
        {
            dynamic expando = new ExpandoObject();

            foreach (DataColumn column in dataRow.Table.Columns)
            {
                var value = dataRow[column];
                var columnName = column.ColumnName;
                // We check to see what datatype the value is and convert it accordingly
                if (value is DBNull)
                {
                    expando = AddPropertyWithValue(expando, columnName, value);
                }
                else if (value.GetType() == typeof(decimal))
                {
                    expando = AddPropertyWithValue(expando, columnName, Convert.ToDecimal(value));
                }
                else if (Int32.TryParse(Convert.ToString(value), out int val))
                {
                    expando = AddPropertyWithValue(expando, columnName, val);
                }
                else
                {
                    expando = AddPropertyWithValue(expando, columnName, Convert.ToString(value));
                }
            }

            return expando;
        }

        #endregion

        #region SQL Helpers

        /// <summary>
        /// Executes a SQL string on vertica and maps the result to a list of <seealso cref="ExpandoObject"/>'s.
        /// The properties of the <seealso cref="ExpandoObject"/>'s will resemble the column names of the SQL
        /// statement and will appear in the same order.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>A list of <seealso cref="ExpandoObject"/>'s.</returns>
        public static List<ExpandoObject> GetExpandoList(string query, VerticaConnection conn = null, bool keepAlive = false)
        {
            VerticaConnection connection = conn ?? new VerticaConnection(Util.VerticaWebConn);
            VerticaDataReader dr = null;
            List<ExpandoObject> items;

            try
            {
                if (conn == null)
                {
                    connection.Open();
                }
                VerticaCommand command = new VerticaCommand(query, connection);
                dr = command.ExecuteReader();

                items = dr.ToExpandoList();

                dr.Dispose();
                if (!keepAlive)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                if (!keepAlive)
                {
                    connection.Close();
                }
                throw e;
            }

            return items;
        }

        /// <summary>
        /// Executes a given SQL string on vertica and maps it to a list of <typeparam name="T"></typeparam> objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns>A list of <typeparam name="T"></typeparam> objects.</returns>
        public static List<List<ExpandoObject>> GetDataAdapterExpandoList(string query)
        {
            VerticaConnection connection = new VerticaConnection(Util.VerticaWebConn);
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            DataSet ds = new DataSet();
            List<List<ExpandoObject>> items = new List<List<ExpandoObject>>();

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = query;
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables.Count; i++)
                {
                    var itemList = ds.Tables[i].ToExpandoList();
                }

                adapter.Dispose();
                connection.Close();
            }
            catch (Exception e)
            {
                adapter.Dispose();
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
                throw e;
            }

            return items;
        }

        #endregion

        #region Helpers

        public static void TryGetValue<T>(this ExpandoObject obj, string key, out T o)
        {
            ((IDictionary<string, object>)obj).TryGetValue(key, out object outObj);
            if (outObj == null || outObj is DBNull)
            {
                o = default(T);
            }
            else
            {
                o = (T)outObj;
            }
        }

        #endregion
    }

    public static class ObjectUtil
    {
        #region SQL Helpers

        /// <summary>
        /// Executes a given SQL string on vertica and maps it to a list of <typeparam name="T"></typeparam> objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns>A list of <typeparam name="T"></typeparam> objects.</returns>
        public static List<T> ExecuteDataReader<T>(string query, VerticaConnection conn = null, bool keepAlive = false) where T : new()
        {
            VerticaConnection connection = conn ?? new VerticaConnection(Util.VerticaWebConn);
            List<T> items;
            VerticaDataReader dr = null;

            try
            {
                if (conn == null)
                {
                    connection.Open();
                }
                VerticaCommand command = new VerticaCommand(query, connection);
                command.CommandText = query;
                dr = command.ExecuteReader();

                items = dr.ToList<T>();

                dr.Dispose();
                if (!keepAlive)
                {
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                if (dr != null || connection.State == ConnectionState.Open)
                {
                    if (!dr.IsClosed)
                    {
                        dr.Close();
                    }
                    if (!keepAlive)
                    {
                        connection.Close();
                    }
                }
                throw e;
            }

            return items;
        }

        /// <summary>
        /// Executes a given SQL string on vertica and maps it to a list of <typeparam name="T"></typeparam> objects.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns>A list of <typeparam name="T"></typeparam> objects.</returns>
        public static List<List<T>> ExecuteDataAdapter<T>(string query) where T : new()
        {
            VerticaConnection connection = new VerticaConnection(Util.VerticaWebConn);
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            DataSet ds = new DataSet();
            List<List<T>> items = new List<List<T>>();

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = query;
                adapter.SelectCommand = command;
                adapter.Fill(ds);

                for (int i = 0; i < ds.Tables.Count; i++)
                {
                    var itemList = ds.Tables[i].ToList<T>();
                    items.Add(itemList);
                }

                adapter.Dispose();
                connection.Close();
            }
            catch (Exception e)
            {
                adapter.Dispose();
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
                throw e;
            }

            return items;
        }

        /// <summary>
        /// Maps a <seealso cref="IDataReader"/> to a list of <typeparam name="T"></typeparam> objects.
        /// </summary>
        /// <returns></returns>
        public static List<T> ToList<T>(this IDataReader reader) where T : new()
        {
            var items = new List<T>();
            var columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();

            try
            {
                while (reader.Read())
                {
                    // Get an instance of T which is the object you provide as T.
                    var item = new T();

                    // Loop through all column names from the data reader.
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        var column = columnNames[i];

                        // Try and get the property name of T that matches the column name from the data reader.
                        PropertyInfo property = item.GetType().GetProperty(column);

                        var currentValue = reader[column];

                        // If property exists and there's a value then assign it to the object property.
                        if (property != null && currentValue != DBNull.Value)
                        {
                            
                            object result = Convert.ChangeType(currentValue, property.PropertyType);
                            property.SetValue(item, result, null);
                        }
                    }
                    items.Add(item);
                }
            }
            catch (Exception e)
            {
                reader.Close();
                throw;
            }

            return items;
        }

        /// <summary>
        /// Maps a <seealso cref="IDataReader"/> to a list of <typeparam name="T"></typeparam> objects.
        /// </summary>
        /// <returns></returns>
        public static List<T> ToList<T>(this DataTable table) where T : new()
        {
            var items = new List<T>();
            var columnNames = Enumerable.Range(0, table.Columns.Count).Select(i => table.Columns[i].GetType().Name).ToArray();

            try
            {
                var rows = table.Rows;
                foreach (DataRow row in rows)
                {
                    // Get an instance of T which is the object you provide as T.
                    var item = row.ToObject<T>();
                    items.Add(item);
                }
            }
            catch (Exception e)
            {
                table.Dispose();
                throw e;
            }

            return items;
        }

        /// <summary>
        /// Maps a DataRow to a generic object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataRow"></param>
        /// <returns></returns>
        public static T ToObject<T>(this DataRow dataRow) where T : new()
        {
            T item = new T();
            foreach (DataColumn column in dataRow.Table.Columns)
            {
                PropertyInfo property = item.GetType().GetProperty(column.ColumnName);

                if (property != null && dataRow[column] != DBNull.Value)
                {
                    object result = Convert.ChangeType(dataRow[column], property.PropertyType);
                    property.SetValue(item, result, null);
                }
            }

            return item;
        }

        /// <summary>
        /// Maps a DataRow to a generic object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataRow"></param>
        /// <returns></returns>
        public static ExpandoObject ToExpandoObject(this object o)
        {
            var properties = o.GetType().GetProperties().ToList();
            dynamic expando = new ExpandoObject();

            foreach (var prop in properties)
            {
                var propertyName = prop.Name;
                var value = prop.GetValue(o);
                // We check to see what datatype the value is and convert it accordingly
                if (value is DBNull)
                {
                    expando = ExpandoUtil.AddPropertyWithValue(expando, propertyName, value);
                }
                else if (value.GetType() == typeof(decimal))
                {
                    expando = ExpandoUtil.AddPropertyWithValue(expando, propertyName, Convert.ToDecimal(value));
                }
                else if (Int32.TryParse(Convert.ToString(value), out int val))
                {
                    expando = ExpandoUtil.AddPropertyWithValue(expando, propertyName, val);
                }
                else
                {
                    expando = ExpandoUtil.AddPropertyWithValue(expando, propertyName, $"\"{Convert.ToString(value)}\"");
                }
            }

            return expando;
        }

        #endregion
    }

    public static class StringExtensions
    {
        /// <summary>
        /// Checks to see if the string is either null, empty, or white space. If it's any of the 
        /// described values then it'll return s false. Otherwise, it'll return as true.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsValid(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(s))
            {
                return false;
            }

            return true;
        }
    }
}