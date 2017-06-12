// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;


namespace System.Data.Common
{
    internal partial class DbConnectionOptions
    {
        // instances of this class are intended to be immutable, i.e readonly
        // used by pooling classes so it is much easier to verify correctness
        // when not worried about the class being modified during execution

        public DbConnectionOptions(string connectionString, Dictionary<string, string> synonyms)
        {
            _parsetable = new Dictionary<string, string>();
            _usersConnectionString = ((null != connectionString) ? connectionString : "");

            // first pass on parsing, initial syntax check
            if (0 < _usersConnectionString.Length)
            {
                _keyChain = ParseInternal(_parsetable, _usersConnectionString, true, synonyms, false);
                _hasPasswordKeyword = (_parsetable.ContainsKey(KEY.Password) || _parsetable.ContainsKey(SYNONYM.Pwd));
            }
        }

        protected DbConnectionOptions(DbConnectionOptions connectionOptions)
        { // Clone used by SqlConnectionString
            _usersConnectionString = connectionOptions._usersConnectionString;
            _hasPasswordKeyword = connectionOptions._hasPasswordKeyword;
            _parsetable = connectionOptions._parsetable;
            _keyChain = connectionOptions._keyChain;
        }

        public bool IsEmpty => _keyChain == null;

        internal bool TryGetParsetableValue(string key, out string value) => _parsetable.TryGetValue(key, out value);

        // same as Boolean, but with SSPI thrown in as valid yes
        public bool ConvertValueToIntegratedSecurity()
        {
            string value;
            return _parsetable.TryGetValue(KEY.Integrated_Security, out value) ?
                ConvertValueToIntegratedSecurityInternal(value) :
                false;
        }

        internal bool ConvertValueToIntegratedSecurityInternal(string stringValue)
        {
            if (CompareInsensitiveInvariant(stringValue, "sspi") || CompareInsensitiveInvariant(stringValue, "true") || CompareInsensitiveInvariant(stringValue, "yes"))
                return true;
            else if (CompareInsensitiveInvariant(stringValue, "false") || CompareInsensitiveInvariant(stringValue, "no"))
                return false;
            else
            {
                string tmp = stringValue.Trim();  // Remove leading & trailing whitespace.
                if (CompareInsensitiveInvariant(tmp, "sspi") || CompareInsensitiveInvariant(tmp, "true") || CompareInsensitiveInvariant(tmp, "yes"))
                    return true;
                else if (CompareInsensitiveInvariant(tmp, "false") || CompareInsensitiveInvariant(tmp, "no"))
                    return false;
                else
                {
                    throw ADP.InvalidConnectionOptionValue(KEY.Integrated_Security);
                }
            }
        }

        public int ConvertValueToInt32(string keyName, int defaultValue)
        {
            string value;
            return _parsetable.TryGetValue(keyName, out value) ?
                ConvertToInt32Internal(keyName, value) :
                defaultValue;
        }

        internal static int ConvertToInt32Internal(string keyname, string stringValue)
        {
            try
            {
                return System.Int32.Parse(stringValue, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                throw ADP.InvalidConnectionOptionValue(keyname, e);
            }
            catch (OverflowException e)
            {
                throw ADP.InvalidConnectionOptionValue(keyname, e);
            }
        }

        public string ConvertValueToString(string keyName, string defaultValue)
        {
            string value;
            return _parsetable.TryGetValue(keyName, out value) && value != null ? value : defaultValue;
        }

        public bool ContainsKey(string keyword)
        {
            return _parsetable.ContainsKey(keyword);
        }
    }
}