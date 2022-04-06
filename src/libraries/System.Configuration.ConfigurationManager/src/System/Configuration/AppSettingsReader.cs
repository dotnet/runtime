// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Globalization;

namespace System.Configuration
{
    /// <summary>
    ///     The AppSettingsReader class provides a wrapper for System.Configuration.ConfigurationManager.AppSettings
    ///     which provides a single method for reading values from the config file of a particular type.
    /// </summary>
    public class AppSettingsReader
    {
        private readonly NameValueCollection _map;
        private static readonly Type s_stringType = typeof(string);
        private const string NullString = "None";

        public AppSettingsReader()
        {
            _map = System.Configuration.ConfigurationManager.AppSettings;
        }

        /// <summary>
        /// Gets the value for specified key from ConfigurationManager.AppSettings, and returns
        /// an object of the specified type containing the value from the config file.  If the key
        /// isn't in the config file, or if it is not a valid value for the given type, it will
        /// throw an exception with a descriptive message so the user can make the appropriate
        /// change
        /// </summary>
        public object GetValue(string key!!, Type type!!)
        {
            string val = _map[key];

            if (val == null) throw new InvalidOperationException(SR.Format(SR.AppSettingsReaderNoKey, key));

            if (type == s_stringType)
            {
                // It's a string, so we can ALMOST just return the value.  The only
                // tricky point is that if it's the string "(None)", then we want to
                // return null.  And of course we need a way to represent the string
                // (None), so we use ((None)), and so on... so it's a little complicated.
                int NoneNesting = GetNoneNesting(val);
                if (NoneNesting == 0)
                {
                    // val is not of the form ((..((None))..))
                    return val;
                }
                else if (NoneNesting == 1)
                {
                    // val is (None)
                    return null;
                }
                else
                {
                    // val is of the form ((..((None))..))
                    return val.Substring(1, val.Length - 2);
                }
            }
            else
            {
                try
                {
                    return Convert.ChangeType(val, type, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    string displayString = (val.Length == 0) ? SR.AppSettingsReaderEmptyString : val;
                    throw new InvalidOperationException(SR.Format(SR.AppSettingsReaderCantParse, displayString, key, type));
                }
            }
        }

        private static int GetNoneNesting(string val)
        {
            int count = 0;
            int len = val.Length;
            if (len > 1)
            {
                while (val[count] == '(' && val[len - count - 1] == ')')
                {
                    count++;
                }
                if (count > 0 && string.Compare(NullString, 0, val, count, len - 2 * count, StringComparison.Ordinal) != 0)
                {
                    // the stuff between the parens is not "None"
                    count = 0;
                }
            }
            return count;
        }
    }
}
