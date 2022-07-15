// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Data.Common
{
    internal class DbConnectionOptions
    {
        // instances of this class are intended to be immutable, i.e readonly
        // used by pooling classes so it is much easier to verify correctness
        // when not worried about the class being modified during execution

#if DEBUG
        /*private const string ConnectionStringPatternV1 =
             "[\\s;]*"
            +"(?<key>([^=\\s]|\\s+[^=\\s]|\\s+==|==)+)"
            +   "\\s*=(?!=)\\s*"
            +"(?<value>("
            +   "(" + "\"" + "([^\"]|\"\")*" + "\"" + ")"
            +   "|"
            +   "(" + "'" + "([^']|'')*" + "'" + ")"
            +   "|"
            +   "(" + "(?![\"'])" + "([^\\s;]|\\s+[^\\s;])*" + "(?<![\"'])" + ")"
            + "))"
            + "[\\s;]*"
        ;*/
        private const string ConnectionStringPattern =                  // may not contain embedded null except trailing last value
            "([\\s;]*"                                                  // leading whitespace and extra semicolons
            + "(?![\\s;])"                                              // key does not start with space or semicolon
            + "(?<key>([^=\\s\\p{Cc}]|\\s+[^=\\s\\p{Cc}]|\\s+==|==)+)"  // allow any visible character for keyname except '=' which must quoted as '=='
            + "\\s*=(?!=)\\s*"                                          // the equal sign divides the key and value parts
            + "(?<value>"
            + "(\"([^\"\u0000]|\"\")*\")"                              // double quoted string, " must be quoted as ""
            + "|"
            + "('([^'\u0000]|'')*')"                                   // single quoted string, ' must be quoted as ''
            + "|"
            + "((?![\"'\\s])"                                          // unquoted value must not start with " or ' or space, would also like = but too late to change
            + "([^;\\s\\p{Cc}]|\\s+[^;\\s\\p{Cc}])*"                  // control characters must be quoted
            + "(?<![\"']))"                                            // unquoted value must not stop with " or '
            + ")(\\s*)(;|[\u0000\\s]*$)"                                // whitespace after value up to semicolon or end-of-line
            + ")*"                                                      // repeat the key-value pair
            + "[\\s;]*[\u0000\\s]*"                                     // traling whitespace/semicolons (DataSourceLocator), embedded nulls are allowed only in the end
        ;

        private const string ConnectionStringPatternOdbc =              // may not contain embedded null except trailing last value
            "([\\s;]*"                                                  // leading whitespace and extra semicolons
            + "(?![\\s;])"                                              // key does not start with space or semicolon
            + "(?<key>([^=\\s\\p{Cc}]|\\s+[^=\\s\\p{Cc}])+)"            // allow any visible character for keyname except '='
            + "\\s*=\\s*"                                               // the equal sign divides the key and value parts
            + "(?<value>"
            + "(\\{([^\\}\u0000]|\\}\\})*\\})"                         // quoted string, starts with { and ends with }
            + "|"
            + "((?![\\{\\s])"                                          // unquoted value must not start with { or space, would also like = but too late to change
            + "([^;\\s\\p{Cc}]|\\s+[^;\\s\\p{Cc}])*"                  // control characters must be quoted

            + ")" // although the spec does not allow {}
                  // embedded within a value, the retail code does.
                  // +  "(?<![\\}]))"                                            // unquoted value must not stop with }

            + ")(\\s*)(;|[\u0000\\s]*$)"                               // whitespace after value up to semicolon or end-of-line
            + ")*"                                                      // repeat the key-value pair
            + "[\\s;]*[\u0000\\s]*"                                     // traling whitespace/semicolons (DataSourceLocator), embedded nulls are allowed only in the end
        ;

        private static readonly Regex ConnectionStringRegex = new Regex(ConnectionStringPattern, RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static readonly Regex ConnectionStringRegexOdbc = new Regex(ConnectionStringPatternOdbc, RegexOptions.ExplicitCapture | RegexOptions.Compiled);
#endif
        private static readonly Regex ConnectionStringValidKeyRegex = new Regex("^(?![;\\s])[^\\p{Cc}]+(?<!\\s)$", RegexOptions.Compiled); // key not allowed to start with semi-colon or space or contain non-visible characters or end with space

        internal const string DataDirectory = "|datadirectory|";

        private static readonly Regex ConnectionStringQuoteValueRegex = new Regex("^[^\"'=;\\s\\p{Cc}]*$", RegexOptions.Compiled); // generally do not quote the value if it matches the pattern
        private static readonly Regex ConnectionStringQuoteOdbcValueRegex = new Regex("^\\{([^\\}\u0000]|\\}\\})*\\}$", RegexOptions.ExplicitCapture | RegexOptions.Compiled); // do not quote odbc value if it matches this pattern

        // connection string common keywords
        private static class KEY
        {
            internal const string Password = "password";
            internal const string Persist_Security_Info = "persist security info";
            internal const string User_ID = "user id";
        };

        // known connection string common synonyms
        private static class SYNONYM
        {
            internal const string Pwd = "pwd";
            internal const string UID = "uid";
        };

        private readonly string _usersConnectionString;
        private readonly Hashtable _parsetable;
        internal readonly NameValuePair? KeyChain;
        internal readonly bool HasPasswordKeyword;
        internal readonly bool HasUserIdKeyword;

        // differences between OleDb and Odbc
        // ODBC:
        //     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/odbc/htm/odbcsqldriverconnect.asp
        //     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/odbcsql/od_odbc_d_4x4k.asp
        //     do not support == -> = in keywords
        //     first key-value pair wins
        //     quote values using \{ and \}, only driver= and pwd= appear to generically allow quoting
        //     do not strip quotes from value, or add quotes except for driver keyword
        // OLEDB:
        //     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/oledb/htm/oledbconnectionstringsyntax.asp
        //     support == -> = in keywords
        //     last key-value pair wins
        //     quote values using \" or \'
        //     strip quotes from value
        internal readonly bool UseOdbcRules;

        // called by derived classes that may cache based on connectionString
        public DbConnectionOptions(string? connectionString)
            : this(connectionString, null, false)
        {
        }

        // synonyms hashtable is meant to be read-only translation of parsed string
        // keywords/synonyms to a known keyword string
        public DbConnectionOptions(string? connectionString, Hashtable? synonyms, bool useOdbcRules)
        {
            UseOdbcRules = useOdbcRules;
            _parsetable = new Hashtable();
            _usersConnectionString = connectionString ?? "";

            // first pass on parsing, initial syntax check
            if (0 < _usersConnectionString.Length)
            {
                KeyChain = ParseInternal(_parsetable, _usersConnectionString, true, synonyms, UseOdbcRules);
                HasPasswordKeyword = (_parsetable.ContainsKey(KEY.Password) || _parsetable.ContainsKey(SYNONYM.Pwd));
                HasUserIdKeyword = (_parsetable.ContainsKey(KEY.User_ID) || _parsetable.ContainsKey(SYNONYM.UID));
            }
        }

        public string UsersConnectionString(bool hidePassword)
        {
            return UsersConnectionString(hidePassword, false);
        }

        private string UsersConnectionString(bool hidePassword, bool forceHidePassword)
        {
            string connectionString = _usersConnectionString;
            if (HasPasswordKeyword && (forceHidePassword || (hidePassword && !HasPersistablePassword)))
            {
                ReplacePasswordPwd(out connectionString, false);
            }
            return connectionString ?? "";
        }

        internal bool HasPersistablePassword
        {
            get
            {
                if (HasPasswordKeyword)
                {
                    return ConvertValueToBoolean(KEY.Persist_Security_Info, false);
                }
                return true; // no password means persistable password so we don't have to munge
            }
        }

        public bool IsEmpty
        {
            get { return (null == KeyChain); }
        }

        public string? this[string keyword]
        {
            get { return (string?)_parsetable[keyword]; }
        }

        internal static void AppendKeyValuePairBuilder(StringBuilder builder, string keyName, string? keyValue, bool useOdbcRules)
        {
            ADP.CheckArgumentNull(builder, "builder");
            ADP.CheckArgumentLength(keyName, "keyName");

            if ((null == keyName) || !ConnectionStringValidKeyRegex.IsMatch(keyName))
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
            { // else <keyword>=;
                if (useOdbcRules)
                {
                    if ((0 < keyValue.Length) &&
                        (('{' == keyValue[0]) || (0 <= keyValue.IndexOf(';')) || (string.Equals(DbConnectionStringKeywords.Driver, keyName, StringComparison.OrdinalIgnoreCase))) &&
                        !ConnectionStringQuoteOdbcValueRegex.IsMatch(keyValue))
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
                else if (ConnectionStringQuoteValueRegex.IsMatch(keyValue))
                {
                    // <value> -> <value>
                    builder.Append(keyValue);
                }
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

        public bool ConvertValueToBoolean(string keyName, bool defaultValue)
        {
            object? value = _parsetable[keyName];
            if (null == value)
            {
                return defaultValue;
            }
            return ConvertValueToBooleanInternal(keyName, (string)value);
        }

        internal static bool ConvertValueToBooleanInternal(string keyName, string stringValue)
        {
            if (CompareInsensitiveInvariant(stringValue, "true") || CompareInsensitiveInvariant(stringValue, "yes"))
                return true;
            else if (CompareInsensitiveInvariant(stringValue, "false") || CompareInsensitiveInvariant(stringValue, "no"))
                return false;
            else
            {
                string tmp = stringValue.Trim();  // Remove leading & trailing white space.
                if (CompareInsensitiveInvariant(tmp, "true") || CompareInsensitiveInvariant(tmp, "yes"))
                    return true;
                else if (CompareInsensitiveInvariant(tmp, "false") || CompareInsensitiveInvariant(tmp, "no"))
                    return false;
                else
                {
                    throw ADP.InvalidConnectionOptionValue(keyName);
                }
            }
        }

        public int ConvertValueToInt32(string keyName, int defaultValue)
        {
            object? value = _parsetable[keyName];
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

        public string? ConvertValueToString(string keyName, string? defaultValue)
        {
            return (string?)_parsetable[keyName] ?? defaultValue;
        }

        private static bool CompareInsensitiveInvariant(string strvalue, string strconst)
        {
            return (0 == StringComparer.OrdinalIgnoreCase.Compare(strvalue, strconst));
        }

        public bool ContainsKey(string keyword)
        {
            return _parsetable.ContainsKey(keyword);
        }

        protected internal virtual string Expand()
        {
            return _usersConnectionString;
        }

        // SxS notes:
        // * this method queries "DataDirectory" value from the current AppDomain.
        //   This string is used for to replace "!DataDirectory!" values in the connection string, it is not considered as an "exposed resource".
        // * This method uses GetFullPath to validate that root path is valid, the result is not exposed out.
        internal static string? ExpandDataDirectory(string keyword, string? value, ref string? datadir)
        {
            string? fullPath = null;
            if ((null != value) && value.StartsWith(DataDirectory, StringComparison.OrdinalIgnoreCase))
            {
                string? rootFolderPath = datadir;
                if (null == rootFolderPath)
                {
                    // find the replacement path
                    object? rootFolderObject = AppDomain.CurrentDomain.GetData("DataDirectory");
                    rootFolderPath = (rootFolderObject as string);
                    if ((null != rootFolderObject) && (null == rootFolderPath))
                    {
                        throw ADP.InvalidDataDirectory();
                    }
                    else if (ADP.IsEmpty(rootFolderPath))
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

        internal string? ExpandDataDirectories(ref string? filename, ref int position)
        {
            string? value;
            StringBuilder builder = new StringBuilder(_usersConnectionString.Length);
            string? datadir = null;

            int copyPosition = 0;
            bool expanded = false;

            for (NameValuePair? current = KeyChain; null != current; current = current.Next)
            {
                value = current.Value;

                // remove duplicate keyswords from connectionstring
                //if ((object)this[current.Name] != (object)value) {
                //    expanded = true;
                //    copyPosition += current.Length;
                //    continue;
                //}

                // There is a set of keywords we explictly do NOT want to expand |DataDirectory| on
                if (UseOdbcRules)
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
                if (UseOdbcRules || (DbConnectionOptionKeywords.FileName != current.Name))
                {
                    if (value != current.Value)
                    {
                        expanded = true;
                        AppendKeyValuePairBuilder(builder, current.Name, value, UseOdbcRules);
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

        [System.Diagnostics.Conditional("DEBUG")]
        private static void DebugTraceKeyValuePair(string keyname, string? keyvalue, Hashtable? synonyms)
        {
            Debug.Assert(keyname == keyname.ToLowerInvariant(), "missing ToLower");

            string? realkeyname = ((null != synonyms) ? (string)synonyms[keyname]! : keyname);
            if ((KEY.Password != realkeyname) && (SYNONYM.Pwd != realkeyname))
            { // don't trace passwords ever!
                if (null != keyvalue)
                {
                }
                else
                {
                }
            }
        }

        private static string GetKeyName(StringBuilder buffer)
        {
            int count = buffer.Length;
            while ((0 < count) && char.IsWhiteSpace(buffer[count - 1]))
            {
                count--; // trailing whitespace
            }
            return buffer.ToString(0, count).ToLowerInvariant();
        }

        private static string GetKeyValue(StringBuilder buffer, bool trimWhitespace)
        {
            int count = buffer.Length;
            int index = 0;
            if (trimWhitespace)
            {
                while ((index < count) && char.IsWhiteSpace(buffer[index]))
                {
                    index++; // leading whitespace
                }
                while ((0 < count) && char.IsWhiteSpace(buffer[count - 1]))
                {
                    count--; // trailing whitespace
                }
            }
            return buffer.ToString(index, count - index);
        }

        // transistion states used for parsing
        private enum ParserState
        {
            NothingYet = 1,   //start point
            Key,
            KeyEqual,
            KeyEnd,
            UnquotedValue,
            DoubleQuoteValue,
            DoubleQuoteValueQuote,
            SingleQuoteValue,
            SingleQuoteValueQuote,
            BraceQuoteValue,
            BraceQuoteValueQuote,
            QuotedValueEnd,
            NullTermination,
        };

        internal static int GetKeyValuePair(string connectionString, int currentPosition, StringBuilder buffer, bool useOdbcRules, out string? keyname, out string? keyvalue)
        {
            int startposition = currentPosition;

            buffer.Length = 0;
            keyname = null;
            keyvalue = null;

            char currentChar = '\0';

            ParserState parserState = ParserState.NothingYet;
            int length = connectionString.Length;
            for (; currentPosition < length; ++currentPosition)
            {
                currentChar = connectionString[currentPosition];

                switch (parserState)
                {
                    case ParserState.NothingYet: // [\\s;]*
                        if ((';' == currentChar) || char.IsWhiteSpace(currentChar))
                        {
                            continue;
                        }
                        if ('\0' == currentChar)
                        { parserState = ParserState.NullTermination; continue; }
                        if (char.IsControl(currentChar))
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        startposition = currentPosition;
                        if ('=' != currentChar)
                        {
                            parserState = ParserState.Key;
                            break;
                        }
                        else
                        {
                            parserState = ParserState.KeyEqual;
                            continue;
                        }

                    case ParserState.Key: // (?<key>([^=\\s\\p{Cc}]|\\s+[^=\\s\\p{Cc}]|\\s+==|==)+)
                        if ('=' == currentChar)
                        { parserState = ParserState.KeyEqual; continue; }
                        if (char.IsWhiteSpace(currentChar))
                        { break; }
                        if (char.IsControl(currentChar))
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.KeyEqual: // \\s*=(?!=)\\s*
                        if (!useOdbcRules && '=' == currentChar)
                        { parserState = ParserState.Key; break; }
                        keyname = GetKeyName(buffer);
                        if (ADP.IsEmpty(keyname))
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        buffer.Length = 0;
                        parserState = ParserState.KeyEnd;
                        goto case ParserState.KeyEnd;

                    case ParserState.KeyEnd:
                        if (char.IsWhiteSpace(currentChar))
                        { continue; }
                        if (useOdbcRules)
                        {
                            if ('{' == currentChar)
                            { parserState = ParserState.BraceQuoteValue; break; }
                        }
                        else
                        {
                            if ('\'' == currentChar)
                            { parserState = ParserState.SingleQuoteValue; continue; }
                            if ('"' == currentChar)
                            { parserState = ParserState.DoubleQuoteValue; continue; }
                        }
                        if (';' == currentChar)
                        { goto ParserExit; }
                        if ('\0' == currentChar)
                        { goto ParserExit; }
                        if (char.IsControl(currentChar))
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        parserState = ParserState.UnquotedValue;
                        break;

                    case ParserState.UnquotedValue: // "((?![\"'\\s])" + "([^;\\s\\p{Cc}]|\\s+[^;\\s\\p{Cc}])*" + "(?<![\"']))"
                        if (char.IsWhiteSpace(currentChar))
                        { break; }
                        if (char.IsControl(currentChar) || ';' == currentChar)
                        { goto ParserExit; }
                        break;

                    case ParserState.DoubleQuoteValue: // "(\"([^\"\u0000]|\"\")*\")"
                        if ('"' == currentChar)
                        { parserState = ParserState.DoubleQuoteValueQuote; continue; }
                        if ('\0' == currentChar)
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.DoubleQuoteValueQuote:
                        if ('"' == currentChar)
                        { parserState = ParserState.DoubleQuoteValue; break; }
                        keyvalue = GetKeyValue(buffer, false);
                        parserState = ParserState.QuotedValueEnd;
                        goto case ParserState.QuotedValueEnd;

                    case ParserState.SingleQuoteValue: // "('([^'\u0000]|'')*')"
                        if ('\'' == currentChar)
                        { parserState = ParserState.SingleQuoteValueQuote; continue; }
                        if ('\0' == currentChar)
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.SingleQuoteValueQuote:
                        if ('\'' == currentChar)
                        { parserState = ParserState.SingleQuoteValue; break; }
                        keyvalue = GetKeyValue(buffer, false);
                        parserState = ParserState.QuotedValueEnd;
                        goto case ParserState.QuotedValueEnd;

                    case ParserState.BraceQuoteValue: // "(\\{([^\\}\u0000]|\\}\\})*\\})"
                        if ('}' == currentChar)
                        { parserState = ParserState.BraceQuoteValueQuote; break; }
                        if ('\0' == currentChar)
                        { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.BraceQuoteValueQuote:
                        if ('}' == currentChar)
                        { parserState = ParserState.BraceQuoteValue; break; }
                        keyvalue = GetKeyValue(buffer, false);
                        parserState = ParserState.QuotedValueEnd;
                        goto case ParserState.QuotedValueEnd;

                    case ParserState.QuotedValueEnd:
                        if (char.IsWhiteSpace(currentChar))
                        { continue; }
                        if (';' == currentChar)
                        { goto ParserExit; }
                        if ('\0' == currentChar)
                        { parserState = ParserState.NullTermination; continue; }
                        throw ADP.ConnectionStringSyntax(startposition);  // unbalanced single quote

                    case ParserState.NullTermination: // [\\s;\u0000]*
                        if ('\0' == currentChar)
                        { continue; }
                        if (char.IsWhiteSpace(currentChar))
                        { continue; }
                        throw ADP.ConnectionStringSyntax(currentPosition);

                    default:
                        throw ADP.InternalError(ADP.InternalErrorCode.InvalidParserState1);
                }
                buffer.Append(currentChar);
            }
        ParserExit:
            switch (parserState)
            {
                case ParserState.Key:
                case ParserState.DoubleQuoteValue:
                case ParserState.SingleQuoteValue:
                case ParserState.BraceQuoteValue:
                    // keyword not found/unbalanced double/single quote
                    throw ADP.ConnectionStringSyntax(startposition);

                case ParserState.KeyEqual:
                    // equal sign at end of line
                    keyname = GetKeyName(buffer);
                    if (ADP.IsEmpty(keyname))
                    { throw ADP.ConnectionStringSyntax(startposition); }
                    break;

                case ParserState.UnquotedValue:
                    // unquoted value at end of line
                    keyvalue = GetKeyValue(buffer, true);

                    char tmpChar = keyvalue[keyvalue.Length - 1];
                    if (!useOdbcRules && (('\'' == tmpChar) || ('"' == tmpChar)))
                    {
                        throw ADP.ConnectionStringSyntax(startposition);    // unquoted value must not end in quote, except for odbc
                    }
                    break;

                case ParserState.DoubleQuoteValueQuote:
                case ParserState.SingleQuoteValueQuote:
                case ParserState.BraceQuoteValueQuote:
                case ParserState.QuotedValueEnd:
                    // quoted value at end of line
                    keyvalue = GetKeyValue(buffer, false);
                    break;

                case ParserState.NothingYet:
                case ParserState.KeyEnd:
                case ParserState.NullTermination:
                    // do nothing
                    break;

                default:
                    throw ADP.InternalError(ADP.InternalErrorCode.InvalidParserState2);
            }
            if ((';' == currentChar) && (currentPosition < connectionString.Length))
            {
                currentPosition++;
            }
            return currentPosition;
        }

        private static bool IsValueValidInternal(string? keyvalue)
        {
            if (null != keyvalue)
            {
                return (-1 == keyvalue.IndexOf('\u0000'));
            }
            return true;
        }

        private static bool IsKeyNameValid([NotNullWhen(true)] string? keyname)
        {
            if (null != keyname)
            {
#if DEBUG
                bool compValue = ConnectionStringValidKeyRegex.IsMatch(keyname);
                Debug.Assert(((0 < keyname.Length) && (';' != keyname[0]) && !char.IsWhiteSpace(keyname[0]) && (-1 == keyname.IndexOf('\u0000'))) == compValue, "IsValueValid mismatch with regex");
#endif
                return ((0 < keyname.Length) && (';' != keyname[0]) && !char.IsWhiteSpace(keyname[0]) && (-1 == keyname.IndexOf('\u0000')));
            }
            return false;
        }

#if DEBUG
        private static Hashtable SplitConnectionString(string connectionString, Hashtable? synonyms, bool firstKey)
        {
            Hashtable parsetable = new Hashtable();
            Regex parser = (firstKey ? ConnectionStringRegexOdbc : ConnectionStringRegex);

            const int KeyIndex = 1, ValueIndex = 2;
            Debug.Assert(KeyIndex == parser.GroupNumberFromName("key"), "wrong key index");
            Debug.Assert(ValueIndex == parser.GroupNumberFromName("value"), "wrong value index");

            if (null != connectionString)
            {
                Match match = parser.Match(connectionString);
                if (!match.Success || (match.Length != connectionString.Length))
                {
                    throw ADP.ConnectionStringSyntax(match.Length);
                }
                int indexValue = 0;
                CaptureCollection keyvalues = match.Groups[ValueIndex].Captures;
                foreach (Capture keypair in match.Groups[KeyIndex].Captures)
                {
                    string keyname = (firstKey ? keypair.Value : keypair.Value.Replace("==", "=")).ToLowerInvariant();
                    string? keyvalue = keyvalues[indexValue++].Value;
                    if (0 < keyvalue.Length)
                    {
                        if (!firstKey)
                        {
                            switch (keyvalue[0])
                            {
                                case '\"':
                                    keyvalue = keyvalue.Substring(1, keyvalue.Length - 2).Replace("\"\"", "\"");
                                    break;
                                case '\'':
                                    keyvalue = keyvalue.Substring(1, keyvalue.Length - 2).Replace("\'\'", "\'");
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        keyvalue = null;
                    }
                    DebugTraceKeyValuePair(keyname, keyvalue, synonyms);

                    string? realkeyname = ((null != synonyms) ? (string)synonyms[keyname]! : keyname);
                    if (!IsKeyNameValid(realkeyname))
                    {
                        throw ADP.KeywordNotSupported(keyname);
                    }
                    if (!firstKey || !parsetable.ContainsKey(realkeyname))
                    {
                        parsetable[realkeyname] = keyvalue; // last key-value pair wins (or first)
                    }
                }
            }
            return parsetable;
        }

        private static void ParseComparison(Hashtable parsetable, string connectionString, Hashtable? synonyms, bool firstKey, Exception? e)
        {
            try
            {
                Hashtable parsedvalues = SplitConnectionString(connectionString, synonyms, firstKey);
                foreach (DictionaryEntry entry in parsedvalues)
                {
                    string keyname = (string)entry.Key;
                    string? value1 = (string?)entry.Value;
                    string? value2 = (string?)parsetable[keyname];
                    Debug.Assert(parsetable.Contains(keyname), $"ParseInternal code vs. regex mismatch keyname <{keyname}>");
                    Debug.Assert(value1 == value2, $"ParseInternal code vs. regex mismatch keyvalue <{value1}> <{value2}>");
                }

            }
            catch (ArgumentException f)
            {
                if (null != e)
                {
                    string msg1 = e.Message;
                    string msg2 = f.Message;

                    const string KeywordNotSupportedMessagePrefix = "Keyword not supported:";
                    const string WrongFormatMessagePrefix = "Format of the initialization string";
                    bool isEquivalent = (msg1 == msg2);
                    if (!isEquivalent)
                    {
                        // we also accept cases were Regex parser (debug only) reports "wrong format" and
                        // retail parsing code reports format exception in different location or "keyword not supported"
                        if (msg2.StartsWith(WrongFormatMessagePrefix, StringComparison.Ordinal))
                        {
                            if (msg1.StartsWith(KeywordNotSupportedMessagePrefix, StringComparison.Ordinal) || msg1.StartsWith(WrongFormatMessagePrefix, StringComparison.Ordinal))
                            {
                                isEquivalent = true;
                            }
                        }
                    }
                    Debug.Assert(isEquivalent, $"ParseInternal code vs regex message mismatch: <{msg1}> <{msg2}>");
                }
                else
                {
                    Debug.Assert(false, $"ParseInternal code vs regex throw mismatch {f.Message}");
                }
                e = null;
            }
            if (null != e)
            {
                Debug.Assert(false, "ParseInternal code threw exception vs regex mismatch");
            }
        }
#endif
        private static NameValuePair? ParseInternal(Hashtable parsetable, string connectionString, bool buildChain, Hashtable? synonyms, bool firstKey)
        {
            Debug.Assert(null != connectionString, "null connectionstring");
            StringBuilder buffer = new StringBuilder();
            NameValuePair? localKeychain = null, keychain = null;
#if DEBUG
            try
            {
#endif
                int nextStartPosition = 0;
                int endPosition = connectionString.Length;
                while (nextStartPosition < endPosition)
                {
                    int startPosition = nextStartPosition;

                    string? keyname, keyvalue;
                    nextStartPosition = GetKeyValuePair(connectionString, startPosition, buffer, firstKey, out keyname, out keyvalue);
                    if (ADP.IsEmpty(keyname))
                    {
                        // if (nextStartPosition != endPosition) { throw; }
                        break;
                    }
#if DEBUG
                    DebugTraceKeyValuePair(keyname, keyvalue, synonyms);

                    Debug.Assert(IsKeyNameValid(keyname), "ParseFailure, invalid keyname");
                    Debug.Assert(IsValueValidInternal(keyvalue), "parse failure, invalid keyvalue");
#endif
                    string? realkeyname = ((null != synonyms) ? (string)synonyms[keyname]! : keyname);
                    if (!IsKeyNameValid(realkeyname))
                    {
                        throw ADP.KeywordNotSupported(keyname);
                    }
                    if (!firstKey || !parsetable.Contains(realkeyname))
                    {
                        parsetable[realkeyname] = keyvalue; // last key-value pair wins (or first)
                    }

                    if (null != localKeychain)
                    {
                        localKeychain = localKeychain.Next = new NameValuePair(realkeyname, keyvalue, nextStartPosition - startPosition);
                    }
                    else if (buildChain)
                    { // first time only - don't contain modified chain from UDL file
                        keychain = localKeychain = new NameValuePair(realkeyname, keyvalue, nextStartPosition - startPosition);
                    }
                }
#if DEBUG
            }
            catch (ArgumentException e)
            {
                ParseComparison(parsetable, connectionString, synonyms, firstKey, e);
                throw;
            }
            ParseComparison(parsetable, connectionString, synonyms, firstKey, null);
#endif
            return keychain;
        }

        internal NameValuePair? ReplacePasswordPwd(out string constr, bool fakePassword)
        {
            bool expanded = false;
            int copyPosition = 0;
            NameValuePair? head = null, tail = null, next = null;
            StringBuilder builder = new StringBuilder(_usersConnectionString.Length);
            for (NameValuePair? current = KeyChain; null != current; current = current.Next)
            {
                if ((KEY.Password != current.Name) && (SYNONYM.Pwd != current.Name))
                {
                    builder.Append(_usersConnectionString, copyPosition, current.Length);
                    if (fakePassword)
                    {
                        next = new NameValuePair(current.Name, current.Value, current.Length);
                    }
                }
                else if (fakePassword)
                { // replace user password/pwd value with *
                    const string equalstar = "=*;";
                    builder.Append(current.Name).Append(equalstar);
                    next = new NameValuePair(current.Name, "*", current.Name.Length + equalstar.Length);
                    expanded = true;
                }
                else
                { // drop the password/pwd completely in returning for user
                    expanded = true;
                }

                if (fakePassword)
                {
                    if (null != tail)
                    {
                        tail = tail.Next = next;
                    }
                    else
                    {
                        tail = head = next;
                    }
                }
                copyPosition += current.Length;
            }
            Debug.Assert(expanded, "password/pwd was not removed");
            constr = builder.ToString();
            return head;
        }
    }
}
