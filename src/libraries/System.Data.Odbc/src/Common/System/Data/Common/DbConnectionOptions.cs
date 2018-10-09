// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;

namespace System.Data.Common
{
    internal partial class DbConnectionOptions
    {
        // instances of this class are intended to be immutable, i.e readonly
        // used by pooling classes so it is much easier to verify correctness
        // when not worried about the class being modified during execution

        internal readonly bool _hasUserIdKeyword;

        // differences between OleDb and Odbc
        // ODBC:
        //     https://docs.microsoft.com/en-us/sql/odbc/reference/syntax/sqldriverconnect-function
        //     do not support == -> = in keywords
        //     first key-value pair wins
        //     quote values using \{ and \}, only driver= and pwd= appear to generically allow quoting
        //     do not strip quotes from value, or add quotes except for driver keyword
        // OLEDB:
        //     https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/connection-string-syntax#oledb-connection-string-syntax
        //     support == -> = in keywords
        //     last key-value pair wins
        //     quote values using \" or \'
        //     strip quotes from value
        internal readonly bool _useOdbcRules;

        // called by derived classes that may cache based on connectionString
        public DbConnectionOptions(string connectionString)
            : this(connectionString, null, false)
        {
        }

        // synonyms hashtable is meant to be read-only translation of parsed string
        // keywords/synonyms to a known keyword string
        public DbConnectionOptions(string connectionString, Dictionary<string, string> synonyms, bool useOdbcRules)
        {
            _useOdbcRules = useOdbcRules;
            _parsetable = new Dictionary<string, string>();
            _usersConnectionString = ((null != connectionString) ? connectionString : "");

            // first pass on parsing, initial syntax check
            if (0 < _usersConnectionString.Length)
            {
                _keyChain = ParseInternal(_parsetable, _usersConnectionString, true, synonyms, _useOdbcRules);
                _hasPasswordKeyword = (_parsetable.ContainsKey(KEY.Password) || _parsetable.ContainsKey(SYNONYM.Pwd));
                _hasUserIdKeyword = (_parsetable.ContainsKey(KEY.User_ID) || _parsetable.ContainsKey(SYNONYM.UID));
            }
        }

        protected DbConnectionOptions(DbConnectionOptions connectionOptions)
        { // Clone used by SqlConnectionString
            _usersConnectionString = connectionOptions._usersConnectionString;
            _hasPasswordKeyword = connectionOptions._hasPasswordKeyword;
            _hasUserIdKeyword = connectionOptions._hasUserIdKeyword;
            _useOdbcRules = connectionOptions._useOdbcRules;
            _parsetable = connectionOptions._parsetable;
            _keyChain = connectionOptions._keyChain;
        }

        internal string UsersConnectionStringForTrace()
        {
            return UsersConnectionString(true, true);
        }

        internal bool HasBlankPassword
        {
            get
            {
                if (!ConvertValueToIntegratedSecurity())
                {
                    if (_parsetable.ContainsKey(KEY.Password))
                    {
                        return string.IsNullOrEmpty((string)_parsetable[KEY.Password]);
                    }
                    else
                    if (_parsetable.ContainsKey(SYNONYM.Pwd))
                    {
                        return string.IsNullOrEmpty((string)_parsetable[SYNONYM.Pwd]); // MDAC 83097
                    }
                    else
                    {
                        return ((_parsetable.ContainsKey(KEY.User_ID) && !string.IsNullOrEmpty((string)_parsetable[KEY.User_ID])) || (_parsetable.ContainsKey(SYNONYM.UID) && !string.IsNullOrEmpty((string)_parsetable[SYNONYM.UID])));
                    }
                }
                return false;
            }
        }

        public bool IsEmpty
        {
            get { return (null == _keyChain); }
        }

        internal Dictionary<string, string> Parsetable
        {
            get { return _parsetable; }
        }

        public ICollection Keys
        {
            get { return _parsetable.Keys; }
        }

        public string this[string keyword]
        {
            get { return (string)_parsetable[keyword]; }
        }

        internal static void AppendKeyValuePairBuilder(StringBuilder builder, string keyName, string keyValue, bool useOdbcRules)
        {
            ADP.CheckArgumentNull(builder, nameof(builder));
            ADP.CheckArgumentLength(keyName, nameof(keyName));

            if ((null == keyName) || !s_connectionStringValidKeyRegex.IsMatch(keyName))
            {
                throw ADP.InvalidKeyname(keyName);
            }
            if ((null != keyValue) && !IsValueValidInternal(keyValue))
            {
                throw ADP.InvalidValue(keyName);
            }

            if ((0 < builder.Length) && (';' != builder[builder.Length - 1]))
            {
                builder.Append(";");
            }

            if (useOdbcRules)
            {
                builder.Append(keyName);
            }
            else
            {
                builder.Append(keyName.Replace("=", "=="));
            }
            builder.Append("=");

            if (null != keyValue)
            { // else <keyword>=;
                if (useOdbcRules)
                {
                    if ((0 < keyValue.Length) &&
                        // string.Contains(char) is .NetCore2.1+ specific
                        (('{' == keyValue[0]) || (0 <= keyValue.IndexOf(';')) || (0 == string.Compare(DbConnectionStringKeywords.Driver, keyName, StringComparison.OrdinalIgnoreCase))) &&
                        !s_connectionStringQuoteOdbcValueRegex.IsMatch(keyValue))
                    {
                        // always quote Driver value (required for ODBC Version 2.65 and earlier)
                        // always quote values that contain a ';'
                        builder.Append('{').Append(keyValue.Replace("}", "}}")).Append('}');
                    }
                    else
                    {
                        builder.Append(keyValue);
                    }
                }
                else if (s_connectionStringQuoteValueRegex.IsMatch(keyValue))
                {
                    // <value> -> <value>
                    builder.Append(keyValue);
                }
                // string.Contains(char) is .NetCore2.1+ specific
                else if ((-1 != keyValue.IndexOf('\"')) && (-1 == keyValue.IndexOf('\'')))
                {
                    // <val"ue> -> <'val"ue'>
                    builder.Append('\'');
                    builder.Append(keyValue);
                    builder.Append('\'');
                }
                else
                {
                    // <val'ue> -> <"val'ue">
                    // <=value> -> <"=value">
                    // <;value> -> <";value">
                    // < value> -> <" value">
                    // <va lue> -> <"va lue">
                    // <va'"lue> -> <"va'""lue">
                    builder.Append('\"');
                    builder.Append(keyValue.Replace("\"", "\"\""));
                    builder.Append('\"');
                }
            }
        }

        // same as Boolean, but with SSPI thrown in as valid yes
        public bool ConvertValueToIntegratedSecurity()
        {
            object value = _parsetable[KEY.Integrated_Security];
            if (null == value)
            {
                return false;
            }
            return ConvertValueToIntegratedSecurityInternal((string)value);
        }

        internal bool ConvertValueToIntegratedSecurityInternal(string stringValue)
        {
            if (CompareInsensitiveInvariant(stringValue, "sspi") || CompareInsensitiveInvariant(stringValue, "true") || CompareInsensitiveInvariant(stringValue, "yes"))
                return true;
            else if (CompareInsensitiveInvariant(stringValue, "false") || CompareInsensitiveInvariant(stringValue, "no"))
                return false;
            else
            {
                string tmp = stringValue.Trim();  // Remove leading & trailing white space.
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
            object value = _parsetable[keyName];
            if (null == value)
            {
                return defaultValue;
            }
            return ConvertToInt32Internal(keyName, (string)value);
        }

        internal static int ConvertToInt32Internal(string keyname, string stringValue)
        {
            try
            {
                return int.Parse(stringValue, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture);
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
            string value = (string)_parsetable[keyName];
            return ((null != value) ? value : defaultValue);
        }

        public bool ContainsKey(string keyword)
        {
            return _parsetable.ContainsKey(keyword);
        }

        // SxS notes:
        // * this method queries "DataDirectory" value from the current AppDomain.
        //   This string is used for to replace "!DataDirectory!" values in the connection string, it is not considered as an "exposed resource".
        // * This method uses GetFullPath to validate that root path is valid, the result is not exposed out.
        internal static string ExpandDataDirectory(string keyword, string value, ref string datadir)
        {
            string fullPath = null;
            if ((null != value) && value.StartsWith(DataDirectory, StringComparison.OrdinalIgnoreCase))
            {
                string rootFolderPath = datadir;
                if (null == rootFolderPath)
                {
                    // find the replacement path
                    object rootFolderObject = AppDomain.CurrentDomain.GetData("DataDirectory");
                    rootFolderPath = (rootFolderObject as string);
                    if ((null != rootFolderObject) && (null == rootFolderPath))
                    {
                        throw ADP.InvalidDataDirectory();
                    }
                    else if (string.IsNullOrEmpty(rootFolderPath))
                    {
                        rootFolderPath = AppDomain.CurrentDomain.BaseDirectory;
                    }
                    if (null == rootFolderPath)
                    {
                        rootFolderPath = "";
                    }
                    // cache the |DataDir| for ExpandDataDirectories
                    datadir = rootFolderPath;
                }

                // We don't know if rootFolderpath ends with '\', and we don't know if the given name starts with onw
                int fileNamePosition = DataDirectory.Length;    // filename starts right after the '|datadirectory|' keyword
                bool rootFolderEndsWith = (0 < rootFolderPath.Length) && rootFolderPath[rootFolderPath.Length - 1] == '\\';
                bool fileNameStartsWith = (fileNamePosition < value.Length) && value[fileNamePosition] == '\\';

                // replace |datadirectory| with root folder path
                if (!rootFolderEndsWith && !fileNameStartsWith)
                {
                    // need to insert '\'
                    fullPath = rootFolderPath + '\\' + value.Substring(fileNamePosition);
                }
                else if (rootFolderEndsWith && fileNameStartsWith)
                {
                    // need to strip one out
                    fullPath = rootFolderPath + value.Substring(fileNamePosition + 1);
                }
                else
                {
                    // simply concatenate the strings
                    fullPath = rootFolderPath + value.Substring(fileNamePosition);
                }

                // verify root folder path is a real path without unexpected "..\"
                if (!ADP.GetFullPath(fullPath).StartsWith(rootFolderPath, StringComparison.Ordinal))
                {
                    throw ADP.InvalidConnectionOptionValue(keyword);
                }
            }
            return fullPath;
        }

        internal string ExpandDataDirectories(ref string filename, ref int position)
        {
            string value = null;
            StringBuilder builder = new StringBuilder(_usersConnectionString.Length);
            string datadir = null;

            int copyPosition = 0;
            bool expanded = false;

            for (NameValuePair current = _keyChain; null != current; current = current.Next)
            {
                value = current.Value;

                // remove duplicate keyswords from connectionstring
                //if ((object)this[current.Name] != (object)value) {
                //    expanded = true;
                //    copyPosition += current.Length;
                //    continue;
                //}

                // There is a set of keywords we explictly do NOT want to expand |DataDirectory| on
                if (_useOdbcRules)
                {
                    switch (current.Name)
                    {
                        case DbConnectionOptionKeywords.Driver:
                        case DbConnectionOptionKeywords.Pwd:
                        case DbConnectionOptionKeywords.UID:
                            break;
                        default:
                            value = ExpandDataDirectory(current.Name, value, ref datadir);
                            break;
                    }
                }
                else
                {
                    switch (current.Name)
                    {
                        case DbConnectionOptionKeywords.Provider:
                        case DbConnectionOptionKeywords.DataProvider:
                        case DbConnectionOptionKeywords.RemoteProvider:
                        case DbConnectionOptionKeywords.ExtendedProperties:
                        case DbConnectionOptionKeywords.UserID:
                        case DbConnectionOptionKeywords.Password:
                        case DbConnectionOptionKeywords.UID:
                        case DbConnectionOptionKeywords.Pwd:
                            break;
                        default:
                            value = ExpandDataDirectory(current.Name, value, ref datadir);
                            break;
                    }
                }
                if (null == value)
                {
                    value = current.Value;
                }
                if (_useOdbcRules || (DbConnectionOptionKeywords.FileName != current.Name))
                {
                    if (value != current.Value)
                    {
                        expanded = true;
                        AppendKeyValuePairBuilder(builder, current.Name, value, _useOdbcRules);
                        builder.Append(';');
                    }
                    else
                    {
                        builder.Append(_usersConnectionString, copyPosition, current.Length);
                    }
                }
                else
                {
                    // strip out 'File Name=myconnection.udl' for OleDb
                    // remembering is value for which UDL file to open
                    // and where to insert the strnig
                    expanded = true;
                    filename = value;
                    position = builder.Length;
                }
                copyPosition += current.Length;
            }

            if (expanded)
            {
                value = builder.ToString();
            }
            else
            {
                value = null;
            }
            return value;
        }

        internal string ExpandKeyword(string keyword, string replacementValue)
        {
            // preserve duplicates, updated keyword value with replacement value
            // if keyword not specified, append to end of the string
            bool expanded = false;
            int copyPosition = 0;

            StringBuilder builder = new StringBuilder(_usersConnectionString.Length);
            for (NameValuePair current = _keyChain; null != current; current = current.Next)
            {
                if ((current.Name == keyword) && (current.Value == this[keyword]))
                {
                    // only replace the parse end-result value instead of all values
                    // so that when duplicate-keywords occur other original values remain in place
                    AppendKeyValuePairBuilder(builder, current.Name, replacementValue, _useOdbcRules);
                    builder.Append(';');
                    expanded = true;
                }
                else
                {
                    builder.Append(_usersConnectionString, copyPosition, current.Length);
                }
                copyPosition += current.Length;
            }

            if (!expanded)
            {
                // 
                Debug.Assert(!_useOdbcRules, "ExpandKeyword not ready for Odbc");
                AppendKeyValuePairBuilder(builder, keyword, replacementValue, _useOdbcRules);
            }
            return builder.ToString();
        }

        internal static void ValidateKeyValuePair(string keyword, string value)
        {
            if ((null == keyword) || !s_connectionStringValidKeyRegex.IsMatch(keyword))
            {
                throw ADP.InvalidKeyname(keyword);
            }
            if ((null != value) && !s_connectionStringValidValueRegex.IsMatch(value))
            {
                throw ADP.InvalidValue(keyword);
            }
        }
    }
}
