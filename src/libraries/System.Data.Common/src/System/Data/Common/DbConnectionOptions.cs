// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System.Data.Common
{
    internal sealed partial class DbConnectionOptions
    {
        // instances of this class are intended to be immutable, i.e readonly
        // used by pooling classes so it is easier to verify correctness
        // without the risk of the objects of the class being modified during execution.


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
        internal readonly bool _hasUserIdKeyword;

        // synonyms hashtable is meant to be read-only translation of parsed string
        // keywords/synonyms to a known keyword string
        public DbConnectionOptions(string? connectionString, Dictionary<string, string>? synonyms, bool useOdbcRules)
        {
            _useOdbcRules = useOdbcRules;
            _parsetable = new Dictionary<string, string?>();
            _usersConnectionString = ((null != connectionString) ? connectionString : "");

            // first pass on parsing, initial syntax check
            if (0 < _usersConnectionString.Length)
            {
                _keyChain = ParseInternal(_parsetable, _usersConnectionString, true, synonyms, _useOdbcRules);
                _hasPasswordKeyword = (_parsetable.ContainsKey(KEY.Password) || _parsetable.ContainsKey(SYNONYM.Pwd));
                _hasUserIdKeyword = (_parsetable.ContainsKey(KEY.User_ID) || _parsetable.ContainsKey(SYNONYM.UID));
            }
        }

        internal Dictionary<string, string?> Parsetable => _parsetable;

        public string? this[string keyword] => _parsetable[keyword];

        internal static void AppendKeyValuePairBuilder(StringBuilder builder, string keyName, string? keyValue, bool useOdbcRules)
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
                builder.Append(';');
            }

            if (useOdbcRules)
            {
                builder.Append(keyName);
            }
            else
            {
                builder.Append(keyName.Replace("=", "=="));
            }
            builder.Append('=');

            if (null != keyValue)
            {
                // else <keyword>=;
                if (useOdbcRules)
                {
                    if ((0 < keyValue.Length) &&
                        // string.Contains(char) is .NetCore2.1+ specific
                        (('{' == keyValue[0]) || (0 <= keyValue.IndexOf(';')) || string.Equals(DbConnectionStringKeywords.Driver, keyName, StringComparison.OrdinalIgnoreCase)) &&
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
                else if ((keyValue.Contains('\"')) && (!keyValue.Contains('\'')))
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

        internal string Expand() => _usersConnectionString;

        internal string ExpandKeyword(string keyword, string replacementValue)
        {
            // preserve duplicates, updated keyword value with replacement value
            // if keyword not specified, append to end of the string
            bool expanded = false;
            int copyPosition = 0;

            var builder = new StringBuilder(_usersConnectionString.Length);
            for (NameValuePair? current = _keyChain; null != current; current = current.Next)
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
                Debug.Assert(!_useOdbcRules, "ExpandKeyword not ready for Odbc");
                AppendKeyValuePairBuilder(builder, keyword, replacementValue, _useOdbcRules);
            }
            return builder.ToString();
        }

        [Conditional("DEBUG")]
        static partial void DebugTraceKeyValuePair(string keyname, string? keyvalue, Dictionary<string, string>? synonyms)
        {
            Debug.Assert(keyname == keyname.ToLowerInvariant(), "missing ToLower");

            string realkeyname = ((null != synonyms) ? (string)synonyms[keyname] : keyname);
            if ((KEY.Password != realkeyname) && (SYNONYM.Pwd != realkeyname))
            {
                // don't trace passwords ever!
                if (null != keyvalue)
                {
                    DataCommonEventSource.Log.Trace("<comm.DbConnectionOptions|INFO|ADV> KeyName='{0}', KeyValue='{1}'", keyname, keyvalue);
                }
                else
                {
                    DataCommonEventSource.Log.Trace("<comm.DbConnectionOptions|INFO|ADV> KeyName='{0}'", keyname);
                }
            }
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
