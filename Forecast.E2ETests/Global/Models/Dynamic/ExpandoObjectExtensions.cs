using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json;

namespace Forecast.E2ETests.Global.Models.Dynamic
{
    public static class ExpandoObjectExtensions
    {
        /// <summary>
        /// Deep copy an <see cref="ExpandoObject"/>.
        /// </summary>
        /// <param name="expando">The <see cref="ExpandoObject"/> that you want to copy.</param>
        /// <returns>A new copy of the provided <see cref="ExpandoObject"/>.</returns>
        public static ExpandoObject Copy(this ExpandoObject expando)
        {
            try
            {
                var serialized = JsonConvert.SerializeObject(expando);
                var deserialized = JsonConvert.DeserializeObject<ExpandoObject>(serialized);
                return deserialized;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static bool Remove(this ExpandoObject expando, string key)
        {
            try
            {
                return ((IDictionary<string, object>)expando).Remove(key);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Returns a <see cref="List{T}"/> of <see cref="string"/> keys from the <see cref="ExpandoObject"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ExpandoObject"/> to work on.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="string"/> values from the <see cref="ExpandoObject"/>.</returns>
        public static List<string> Keys(this ExpandoObject obj)
        {
            IDictionary<string, object> localObj = obj;
            List<string> expandoKeys = localObj.Keys.ToList();
            return expandoKeys;
        }

        /// <summary>
        /// Method that adds a property to the expando object or assigns a value to an existing one.
        /// </summary>
        /// 
        /// <param name="expando">The expando object to be modified and returned.</param>
        /// 
        /// <param name="prop">The string name of the property to add or modify.</param>
        /// 
        /// <param name="val">The object value to be assigned to the property.</param>
        /// 
        /// <returns>An ExpandoObject with the new property value.</returns>
        public static ExpandoObject AddOrUpdatePropertyWithValue(this ExpandoObject expando, string prop, object val)
        {
            var tempExpando = (IDictionary<string, object>)expando;
            tempExpando[prop] = val;
            return (ExpandoObject)tempExpando;
        }

        /// <summary>
        /// Returns a <see cref="List{T}"/> of <see cref="string"/> values from the <see cref="ExpandoObject"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ExpandoObject"/> to work on.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="string"/> values from the <see cref="ExpandoObject"/>.</returns>
        public static List<object> Values(this ExpandoObject obj)
        {
            IDictionary<string, object> localObj = obj;
            List<object> expandoValues = localObj.Values.ToList();
            return expandoValues;
        }

        /// <summary>
        /// Extension method to retrive a value from an expando object. If the value is <code>null</code> 
        /// or <seealso cref="DBNull"/> Then the <paramref name="o"/> will be set to it's default value provided
        /// by default(T).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="key"></param>
        /// <param name="o"></param>
        public static T GetValue<T>(this ExpandoObject obj, string key)
        {
            ((IDictionary<string, object>)obj).TryGetValue(key, out object outObj);

            if (outObj == null || outObj is DBNull)
            {
                return default(T);
            }
            else
            {
                if (outObj is T t)
                {
                    return t;
                }
                else
                {
                    try
                    {
                        return (T)Convert.ChangeType(outObj, typeof(T));
                    }
                    catch (InvalidCastException)
                    {
                        return default(T);
                    }
                }
            }
        }

        /// <summary>
        /// Extension method to retrive a value from an expando object. If the value is <code>null</code> 
        /// or <seealso cref="DBNull"/> Then the <paramref name="o"/> will be set to it's default value provided
        /// by default(T).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="key"></param>
        /// <param name="o"></param>
        /// <returns>True if retrieving the value was successful. False if not.</returns>
        public static bool TryGetValue<T>(this ExpandoObject obj, string key, out T o)
        {
            ((IDictionary<string, object>)obj).TryGetValue(key, out object outObj);
            if (outObj == null || outObj is DBNull)
            {
                o = default(T);
                return false;
            }
            else
            {
                if (outObj is T t)
                {
                    o = t;
                    return true;
                }
                else
                {
                    try
                    {
                        o = (T)Convert.ChangeType(outObj, typeof(T));
                        return true;
                    }
                    catch (InvalidCastException)
                    {
                        o = default(T);
                        return false;
                    }
                }
            }
        }
    }
}
