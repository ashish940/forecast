using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Forecast.E2ETests.Global.Models.Dynamic;
using Microsoft.VisualBasic.FileIO;

namespace Forecast.E2ETests.Global.IO.CSV
{
    public interface ICSVReader
    {
        /// <summary>
        /// Get a <see cref="string"/> <see cref="List{T}"/> of column values.
        /// </summary>
        /// <param name="filePath">An absolute <see cref="string"/> path to the file you want to read.</param>
        /// <param name="columnName">The column you want to read.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="string"/>'s.</returns>
        List<string> GetColumnValues(string filePath, string columnName);

        List<ExpandoObject> GetExpandoList(string filePath);

        /// <summary>
        /// Get a <see cref="List{T}"/> of <see cref="T"/> objects from a CSV file.
        /// </summary>
        /// <typeparam name="T">The <see cref="T"/> object to convert each CSV row into.</typeparam>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <returns>A <see cref="LinkedList{T}"/> of <see cref="T"/> objects.</returns>
        List<ExpandoObject> GetExpandoList(string filePath, string delimiter);

        /// <summary>
        /// Gets a <see cref="List{T}"/> of <see cref="ExpandoObject"/>'s.
        /// </summary>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <param name="removeQuotes">True if you want the CSV quites sarounding each value to be removed. False if not.</param>
        /// <returns>A <see cref="LinkedList{T}"/> of <see cref="T"/> objects.</returns>
        List<ExpandoObject> GetExpandoList(string filePath, string delimiter, bool removeQuotes);

        /// <summary>
        /// Gets a <see cref="List{T}"/> of <see cref="ExpandoObject"/>'s.
        /// </summary>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <param name="removeQuotes">True if you want the CSV quites sarounding each value to be removed. False if not.</param>
        /// <param name="lookupColumns">A <see cref="IDictionary{TKey, TValue}"/> that has a <see cref="string"/> key and value that represent column mappings.
        /// You can have a map that contains a key that corresponds to the column name in the CSV and a value that corresponds to the property name
        /// of you <see cref="T"/> object.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="T"/> objects.</returns>
        List<ExpandoObject> GetExpandoList(string filePath, string delimiter, bool removeQuotes, IDictionary<string, string> lookupColumns);

        /// <summary>
        /// Gets a <see cref="List{T}"/> of <see cref="ExpandoObject"/>'s.
        /// </summary>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <param name="removeQuotes">True if you want the CSV quites sarounding each value to be removed. False if not.</param>
        /// <param name="lookupColumns">A <see cref="IDictionary{TKey, TValue}"/> that has a <see cref="string"/> key and value that represent column mappings.
        /// You can have a map that contains a key that corresponds to the column name in the CSV and a value that corresponds to the property name
        /// of you <see cref="T"/> object.</param>
        /// <param name="ignoreCase">Provide true if you want to make the column mapping non-case sensitive. False if it should be case sensitive.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="T"/> objects.</returns>
        List<ExpandoObject> GetExpandoList(string filePath, string delimiter, bool removeQuotes, IDictionary<string, string> lookupColumns, bool ignoreCase);

        /// <summary>
        /// Get a <see cref="List{T}"/> of <see cref="string"/> header names from the CSV file.
        /// </summary>
        /// <param name="filePath">A <see cref="string"/> path to the file you want to read the headers from.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="string"/> header names.</returns>
        List<string> GetHeaders(string filePath);

        /// <summary>
        /// Get a <see cref="List{T}"/> of <see cref="T"/> objects from a CSV file.
        /// </summary>
        /// <typeparam name="T">The <see cref="T"/> object to convert each CSV row into.</typeparam>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="T"/> objects.</returns>
        List<T> GetList<T>(string filePath) where T : new();

        /// <summary>
        /// Get a <see cref="List{T}"/> of <see cref="T"/> objects from a CSV file.
        /// </summary>
        /// <typeparam name="T">The <see cref="T"/> object to convert each CSV row into.</typeparam>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="T"/> objects.</returns>
        List<T> GetList<T>(string filePath, string delimiter) where T : new();

        /// <summary>
        /// Get a <see cref="List{T}"/> of <see cref="T"/> objects from a CSV file.
        /// </summary>
        /// <typeparam name="T">The <see cref="T"/> object to convert each CSV row into.</typeparam>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <param name="lookupColumns">A <see cref="IDictionary{TKey, TValue}"/> that has a <see cref="string"/> key and value that represent column mappings.
        /// You can have a map that contains a key that corresponds to the column name in the CSV and a value that corresponds to the property name
        /// of you <see cref="T"/> object.</param>
        /// <returns>A <see cref="LinkedList{T}"/> of <see cref="T"/> objects.</returns>
        List<T> GetList<T>(string filePath, string delimiter, IDictionary<string, string> lookupColumns) where T : new();

        /// <summary>
        /// Get a <see cref="List{T}"/> of <see cref="T"/> objects from a CSV file.
        /// </summary>
        /// <typeparam name="T">The <see cref="T"/> object to convert each CSV row into.</typeparam>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="lookupColumns">A <see cref="IDictionary{TKey, TValue}"/> that has a <see cref="string"/> key and value that represent column mappings.
        /// You can have a map that contains a key that corresponds to the column name in the CSV and a value that corresponds to the property name
        /// of you <see cref="T"/> object.</param>
        /// <param name="ignoreCase">Provide true if you want to make the column mapping non-case sensitive. False if it should be case sensitive.</param>
        /// <returns>A <see cref="LinkedList{T}"/> of <see cref="T"/> objects.</returns>
        List<T> GetList<T>(string filePath, string delimiter, IDictionary<string, string> lookupColumns, bool ignoreCase) where T : new();
    }

    public class CSVReader : ICSVReader
    {
        /// <summary>
        /// Get a <see cref="string"/> <see cref="List{T}"/> of column values.
        /// </summary>
        /// <param name="filePath">An absolute <see cref="string"/> path to the file you want to read.</param>
        /// <param name="columnName">The column you want to read.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="string"/>'s.</returns>
        public List<string> GetColumnValues(string filePath, string columnName)
        {
            try
            {
                var columnValues = new List<string>();
                var delimiter = ",";
                var removeQuotes = true;

                using (var stream = File.OpenRead(filePath))
                {
                    using (var reader = new TextFieldParser(stream)
                    {
                        Delimiters = new string[] { delimiter },
                        HasFieldsEnclosedInQuotes = true
                    })
                    {

                        var columnNames = reader.ReadLine().Split(delimiter.ToCharArray());
                        var columnNameIndex = columnNames.ToList().IndexOf(columnName);

                        while (reader.EndOfData == false)
                        {
                            var commaSplit = removeQuotes ? @"\""?\s*,(?=(?:[^\""]*\""[^\""]*\"")*[^\""]*$)\s*\""?" : @",(?=(?:[^\""]*\""[^\""]*\"")*[^\""]*$)";
                            string[] data = Regex.Split(reader.ReadLine(), commaSplit);
                            columnValues.Add(data[columnNameIndex]);
                        }
                    }

                }

                return columnValues;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a <see cref="List{T}"/> of <see cref="ExpandoObject"/>'s.
        /// </summary>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <returns>A <see cref="LinkedList{T}"/> of <see cref="T"/> objects.</returns>
        public List<ExpandoObject> GetExpandoList(string filePath)
        {
            try
            {
                return GetExpandoList(filePath, ",", true, null, false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a <see cref="List{T}"/> of <see cref="ExpandoObject"/>'s.
        /// </summary>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="T"/> objects.</returns>
        public List<ExpandoObject> GetExpandoList(string filePath, string delimiter)
        {
            try
            {
                return GetExpandoList(filePath, delimiter, true, null, false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a <see cref="List{T}"/> of <see cref="ExpandoObject"/>'s.
        /// </summary>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <param name="removeQuotes">True if you want the CSV quites sarounding each value to be removed. False if not.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="T"/> objects.</returns>
        public List<ExpandoObject> GetExpandoList(string filePath, string delimiter, bool removeQuotes)
        {
            try
            {
                return GetExpandoList(filePath, delimiter, removeQuotes, null, false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a <see cref="List{T}"/> of <see cref="ExpandoObject"/>'s.
        /// </summary>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <param name="removeQuotes">True if you want the CSV quites sarounding each value to be removed. False if not.</param>
        /// <param name="lookupColumns">A <see cref="IDictionary{TKey, TValue}"/> that has a <see cref="string"/> key and value that represent column mappings.
        /// You can have a map that contains a key that corresponds to the column name in the CSV and a value that corresponds to the property name
        /// of you <see cref="T"/> object.</param>
        /// <returns>A <see cref="LinkedList{T}"/> of <see cref="T"/> objects.</returns>
        public List<ExpandoObject> GetExpandoList(string filePath, string delimiter, bool removeQuotes, IDictionary<string, string> lookupColumns)
        {
            try
            {
                return GetExpandoList(filePath, delimiter, removeQuotes, lookupColumns, false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gets a <see cref="List{T}"/> of <see cref="ExpandoObject"/>'s.
        /// </summary>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <param name="removeQuotes">True if you want the CSV quites sarounding each value to be removed. False if not.</param>
        /// <param name="lookupColumns">A <see cref="IDictionary{TKey, TValue}"/> that has a <see cref="string"/> key and value that represent column mappings.
        /// You can have a map that contains a key that corresponds to the column name in the CSV and a value that corresponds to the property name
        /// of you <see cref="T"/> object.</param>
        /// <param name="ignoreCase">Provide true if you want to make the column mapping non-case sensitive. False if it should be case sensitive.</param>
        /// <returns>A <see cref="LinkedList{T}"/> of <see cref="T"/> objects.</returns>
        public List<ExpandoObject> GetExpandoList(string filePath, string delimiter, bool removeQuotes, IDictionary<string, string> lookupColumns, bool ignoreCase)
        {
            var expandoList = new List<ExpandoObject>();
            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    using (var reader = new TextFieldParser(stream)
                    {
                        Delimiters = new string[] { delimiter },
                        HasFieldsEnclosedInQuotes = true
                    })
                    {
                        var columnNames = reader.ReadLine().Split(delimiter.ToCharArray());
                        while (reader.EndOfData == false)
                        {
                            ExpandoObject expando = new ExpandoObject();
                            var commaSplit = removeQuotes ? @"\""?\s*,(?=(?:[^\""]*\""[^\""]*\"")*[^\""]*$)\s*\""?" : @",(?=(?:[^\""]*\""[^\""]*\"")*[^\""]*$)";
                            string[] data = Regex.Split(reader.ReadLine(), commaSplit);

                            // Loop through all the column names And assign 
                            for (int i = 0; i <= columnNames.Length - 1; i++)
                            {
                                string columnName = columnNames[i];

                                if (lookupColumns != null)
                                {
                                    var dictColumnName = columnName;
                                    if (ignoreCase)
                                    {
                                        dictColumnName = lookupColumns.Keys.Where(key => key.ToLower().Equals(dictColumnName.ToLower())).FirstOrDefault();
                                    }
                                    lookupColumns.TryGetValue(dictColumnName, out columnName);
                                }

                                string val = data[i];
                                expando = expando.AddOrUpdatePropertyWithValue(columnName, val ?? "");
                            }

                            expandoList.Add(expando);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return expandoList;
        }

        /// <summary>
        /// Get a <see cref="List{T}"/> of <see cref="string"/> header names from the CSV file.
        /// </summary>
        /// <param name="filePath">A <see cref="string"/> path to the file you want to read the headers from.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="string"/> header names.</returns>
        public List<string> GetHeaders(string filePath)
        {
            var headers = new List<string>();
            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    headers = reader.ReadLine().Split(',').ToList();
                    reader.Close();
                }
            }
            catch (Exception)
            {
                throw;
            }
            return headers;
        }

        /// <summary>
        /// Get a <see cref="List{T}"/> of <see cref="T"/> objects from a CSV file.
        /// </summary>
        /// <typeparam name="T">The <see cref="T"/> object to convert each CSV row into.</typeparam>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="T"/> objects.</returns>
        public List<T> GetList<T>(string filePath) where T : new()
        {
            try
            {
                return GetList<T>(filePath, ",", null, false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get a <see cref="List{T}"/> of <see cref="T"/> objects from a CSV file.
        /// </summary>
        /// <typeparam name="T">The <see cref="T"/> object to convert each CSV row into.</typeparam>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="T"/> objects.</returns>
        public List<T> GetList<T>(string filePath, string delimiter) where T : new()
        {
            try
            {
                return GetList<T>(filePath, delimiter, null, false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get a <see cref="List{T}"/> of <see cref="T"/> objects from a CSV file.
        /// </summary>
        /// <typeparam name="T">The <see cref="T"/> object to convert each CSV row into.</typeparam>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="delimiter">A <see cref="string"/> value for what you want to use as a delimiter.</param>
        /// <param name="lookupColumns">A <see cref="IDictionary{TKey, TValue}"/> that has a <see cref="string"/> key and value that represent column mappings.
        /// You can have a map that contains a key that corresponds to the column name in the CSV and a value that corresponds to the property name
        /// of you <see cref="T"/> object.</param>
        /// <returns>A <see cref="LinkedList{T}"/> of <see cref="T"/> objects.</returns>
        public List<T> GetList<T>(string filePath, string delimiter, IDictionary<string, string> lookupColumns) where T : new()
        {
            try
            {
                return GetList<T>(filePath, delimiter, lookupColumns, false);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get a <see cref="List{T}"/> of <see cref="T"/> objects from a CSV file.
        /// </summary>
        /// <typeparam name="T">The <see cref="T"/> object to convert each CSV row into.</typeparam>
        /// <param name="filePath">The CSV <see cref="string"/> file path to read from.</param>
        /// <param name="lookupColumns">A <see cref="IDictionary{TKey, TValue}"/> that has a <see cref="string"/> key and value that represent column mappings.
        /// You can have a map that contains a key that corresponds to the column name in the CSV and a value that corresponds to the property name
        /// of you <see cref="T"/> object.</param>
        /// <param name="ignoreCase">Provide true if you want to make the column mapping non-case sensitive. False if it should be case sensitive.</param>
        /// <returns>A <see cref="LinkedList{T}"/> of <see cref="T"/> objects.</returns>
        public List<T> GetList<T>(string filePath, string delimiter, IDictionary<string, string> lookupColumns, bool ignoreCase) where T : new()
        {
            var tList = new List<T>();

            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    using (var reader = new TextFieldParser(stream)
                    {
                        Delimiters = new string[] { delimiter },
                        HasFieldsEnclosedInQuotes = true
                    })
                    {
                        var columnNames = reader.ReadLine().Split(delimiter.ToCharArray());
                        while (reader.EndOfData == false)
                        {
                            // Get an instance of T which Is the object you provide as T.
                            T tItem = new T();
                            var data = reader.ReadFields();

                            for (int j = 0; j <= columnNames.Length - 1; j++)
                            {
                                string columnName = columnNames[j];

                                if (lookupColumns != null)
                                {
                                    var dictColumnName = columnName;
                                    if (ignoreCase)
                                    {
                                        dictColumnName = lookupColumns.Keys.Where(key => key.ToLower().Equals(dictColumnName.ToLower())).FirstOrDefault();
                                    }
                                    lookupColumns.TryGetValue(dictColumnName, out columnName);
                                }

                                var bindings = (BindingFlags.Public & BindingFlags.Instance) | BindingFlags.Static;
                                if (ignoreCase)
                                {
                                    bindings = (BindingFlags.Public & BindingFlags.IgnoreCase & BindingFlags.Instance) | BindingFlags.Static;
                                }
                                PropertyInfo property = tItem.GetType().GetProperty(columnName, bindings);

                                if (property != null & property.CanWrite)
                                {
                                    var currentValue = data[j];
                                    var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                                    var isNull = false;
                                    if (currentValue == null || (currentValue is string && currentValue.Equals(string.Empty)))
                                    {
                                        isNull = true;
                                    }

                                    object result = isNull ? null : Convert.ChangeType(currentValue, property.PropertyType);
                                    if (result != null)
                                    {
                                        property.SetValue(tItem, result, null);
                                    }
                                }
                            }
                            tList.Add(tItem);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return tList;
        }
    }
}
