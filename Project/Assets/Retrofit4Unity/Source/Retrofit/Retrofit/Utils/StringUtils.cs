#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
using System;
using System.Collections.Generic;
using System.Text;

namespace Retrofit.Utils
{
    public static class StringUtils
    {
        public static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null) return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!Char.IsWhiteSpace(value[i])) return false;
            }

            return true;
        }
        public static String Join(String separator, IEnumerable<String> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            if (separator == null)
                separator = String.Empty;


            using (IEnumerator<String> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return String.Empty;

                StringBuilder result = new StringBuilder();
                if (en.Current != null)
                {
                    result.Append(en.Current);
                }

                while (en.MoveNext())
                {
                    result.Append(separator);
                    if (en.Current != null)
                    {
                        result.Append(en.Current);
                    }
                }
                return result.ToString();
            }
        }
    }
}