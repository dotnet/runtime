// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
            + "[\\s;]*[\u0000\\s]*"                                     // trailing whitespace/semicolons (DataSourceLocator), embedded nulls are allowed only in the end
        ;


        private static readonly Regex s_connectionStringRegex = new Regex(ConnectionStringPattern, RegexOptions.ExplicitCapture | RegexOptions.Compiled);
#endif
        private const string ConnectionStringValidKeyPattern = "^(?![;\\s])[^\\p{Cc}]+(?<!\\s)$"; // key not allowed to start with semi-colon or space or contain non-visible characters or end with space
        private const string ConnectionStringValidValuePattern = "^[^\u0000]*$";                    // value not allowed to contain embedded null
        private static readonly Regex s_connectionStringValidKeyRegex = new Regex(ConnectionStringValidKeyPattern, RegexOptions.Compiled);
        private static readonly Regex s_connectionStringValidValueRegex = new Regex(ConnectionStringValidValuePattern, RegexOptions.Compiled);

        // connection string common keywords
        private static class KEY
        {
            internal const string Integrated_Security = "integrated security";
            internal const string Password = "password";
            internal const string Persist_Security_Info = "persist security info";
        };

        // known connection string common synonyms
        private static class SYNONYM
        {
            internal const string Pwd = "pwd";
        };

        private readonly string _usersConnectionString;
        private readonly Dictionary<string, string> _parsetable;
        internal readonly NameValuePair KeyChain;
        internal readonly bool HasPasswordKeyword;

        public DbConnectionOptions(string connectionString, Dictionary<string, string> synonyms)
        {
            _parsetable = new Dictionary<string, string>();
            _usersConnectionString = ((null != connectionString) ? connectionString : "");

            // first pass on parsing, initial syntax check
            if (0 < _usersConnectionString.Length)
            {
                KeyChain = ParseInternal(_parsetable, _usersConnectionString, true, synonyms);
                HasPasswordKeyword = (_parsetable.ContainsKey(KEY.Password) || _parsetable.ContainsKey(SYNONYM.Pwd));
            }
        }

        protected DbConnectionOptions(DbConnectionOptions connectionOptions)
        { // Clone used by SqlConnectionString
            _usersConnectionString = connectionOptions._usersConnectionString;
            HasPasswordKeyword = connectionOptions.HasPasswordKeyword;
            _parsetable = connectionOptions._parsetable;
            KeyChain = connectionOptions.KeyChain;
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
            return ((null != connectionString) ? connectionString : "");
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

        internal bool TryGetParsetableValue(string key, out string value) => _parsetable.TryGetValue(key, out value);


        public bool ConvertValueToBoolean(string keyName, bool defaultValue)
        {
            string value;
            return _parsetable.TryGetValue(keyName, out value) ?
                ConvertValueToBooleanInternal(keyName, value) :
                defaultValue;
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

        private static bool CompareInsensitiveInvariant(string strvalue, string strconst)
        {
            return (0 == StringComparer.OrdinalIgnoreCase.Compare(strvalue, strconst));
        }

        public bool ContainsKey(string keyword)
        {
            return _parsetable.ContainsKey(keyword);
        }


#if DEBUG
        [System.Diagnostics.Conditional("DEBUG")]
        private static void DebugTraceKeyValuePair(string keyname, string keyvalue, Dictionary<string, string> synonyms)
        {
        }
#endif

        private static string GetKeyName(StringBuilder buffer)
        {
            int count = buffer.Length;
            while ((0 < count) && Char.IsWhiteSpace(buffer[count - 1]))
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
                while ((index < count) && Char.IsWhiteSpace(buffer[index]))
                {
                    index++; // leading whitespace
                }
                while ((0 < count) && Char.IsWhiteSpace(buffer[count - 1]))
                {
                    count--; // trailing whitespace
                }
            }
            return buffer.ToString(index, count - index);
        }

        // transition states used for parsing
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

        internal static int GetKeyValuePair(string connectionString, int currentPosition, StringBuilder buffer, out string keyname, out string keyvalue)
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
                        if ((';' == currentChar) || Char.IsWhiteSpace(currentChar))
                        {
                            continue;
                        }
                        if ('\0' == currentChar) { parserState = ParserState.NullTermination; continue; }
                        if (Char.IsControl(currentChar)) { throw ADP.ConnectionStringSyntax(startposition); }
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
                        if ('=' == currentChar) { parserState = ParserState.KeyEqual; continue; }
                        if (Char.IsWhiteSpace(currentChar)) { break; }
                        if (Char.IsControl(currentChar)) { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.KeyEqual: // \\s*=(?!=)\\s*
                        if ('=' == currentChar) { parserState = ParserState.Key; break; }
                        keyname = GetKeyName(buffer);
                        if (string.IsNullOrEmpty(keyname)) { throw ADP.ConnectionStringSyntax(startposition); }
                        buffer.Length = 0;
                        parserState = ParserState.KeyEnd;
                        goto case ParserState.KeyEnd;

                    case ParserState.KeyEnd:
                        if (Char.IsWhiteSpace(currentChar)) { continue; }
                        {
                            if ('\'' == currentChar) { parserState = ParserState.SingleQuoteValue; continue; }
                            if ('"' == currentChar) { parserState = ParserState.DoubleQuoteValue; continue; }
                        }
                        if (';' == currentChar) { goto ParserExit; }
                        if ('\0' == currentChar) { goto ParserExit; }
                        if (Char.IsControl(currentChar)) { throw ADP.ConnectionStringSyntax(startposition); }
                        parserState = ParserState.UnquotedValue;
                        break;

                    case ParserState.UnquotedValue: // "((?![\"'\\s])" + "([^;\\s\\p{Cc}]|\\s+[^;\\s\\p{Cc}])*" + "(?<![\"']))"
                        if (Char.IsWhiteSpace(currentChar)) { break; }
                        if (Char.IsControl(currentChar) || ';' == currentChar) { goto ParserExit; }
                        break;

                    case ParserState.DoubleQuoteValue: // "(\"([^\"\u0000]|\"\")*\")"
                        if ('"' == currentChar) { parserState = ParserState.DoubleQuoteValueQuote; continue; }
                        if ('\0' == currentChar) { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.DoubleQuoteValueQuote:
                        if ('"' == currentChar) { parserState = ParserState.DoubleQuoteValue; break; }
                        keyvalue = GetKeyValue(buffer, false);
                        parserState = ParserState.QuotedValueEnd;
                        goto case ParserState.QuotedValueEnd;

                    case ParserState.SingleQuoteValue: // "('([^'\u0000]|'')*')"
                        if ('\'' == currentChar) { parserState = ParserState.SingleQuoteValueQuote; continue; }
                        if ('\0' == currentChar) { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.SingleQuoteValueQuote:
                        if ('\'' == currentChar) { parserState = ParserState.SingleQuoteValue; break; }
                        keyvalue = GetKeyValue(buffer, false);
                        parserState = ParserState.QuotedValueEnd;
                        goto case ParserState.QuotedValueEnd;

                    case ParserState.BraceQuoteValue: // "(\\{([^\\}\u0000]|\\}\\})*\\})"
                        if ('}' == currentChar) { parserState = ParserState.BraceQuoteValueQuote; break; }
                        if ('\0' == currentChar) { throw ADP.ConnectionStringSyntax(startposition); }
                        break;

                    case ParserState.BraceQuoteValueQuote:
                        if ('}' == currentChar) { parserState = ParserState.BraceQuoteValue; break; }
                        keyvalue = GetKeyValue(buffer, false);
                        parserState = ParserState.QuotedValueEnd;
                        goto case ParserState.QuotedValueEnd;

                    case ParserState.QuotedValueEnd:
                        if (Char.IsWhiteSpace(currentChar)) { continue; }
                        if (';' == currentChar) { goto ParserExit; }
                        if ('\0' == currentChar) { parserState = ParserState.NullTermination; continue; }
                        throw ADP.ConnectionStringSyntax(startposition);  // unbalanced single quote

                    case ParserState.NullTermination: // [\\s;\u0000]*
                        if ('\0' == currentChar) { continue; }
                        if (Char.IsWhiteSpace(currentChar)) { continue; }
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
                    if (string.IsNullOrEmpty(keyname)) { throw ADP.ConnectionStringSyntax(startposition); }
                    break;

                case ParserState.UnquotedValue:
                    // unquoted value at end of line
                    keyvalue = GetKeyValue(buffer, true);

                    char tmpChar = keyvalue[keyvalue.Length - 1];
                    if (('\'' == tmpChar) || ('"' == tmpChar))
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

        private static bool IsValueValidInternal(string keyvalue)
        {
            if (null != keyvalue)
            {
#if DEBUG
                bool compValue = s_connectionStringValidValueRegex.IsMatch(keyvalue);
                Debug.Assert((-1 == keyvalue.IndexOf('\u0000')) == compValue, "IsValueValid mismatch with regex");
#endif
                return (-1 == keyvalue.IndexOf('\u0000'));
            }
            return true;
        }

        private static bool IsKeyNameValid(string keyname)
        {
            if (null != keyname)
            {
#if DEBUG
                bool compValue = s_connectionStringValidKeyRegex.IsMatch(keyname);
                Debug.Assert(((0 < keyname.Length) && (';' != keyname[0]) && !Char.IsWhiteSpace(keyname[0]) && (-1 == keyname.IndexOf('\u0000'))) == compValue, "IsValueValid mismatch with regex");
#endif
                return ((0 < keyname.Length) && (';' != keyname[0]) && !Char.IsWhiteSpace(keyname[0]) && (-1 == keyname.IndexOf('\u0000')));
            }
            return false;
        }

#if DEBUG
        private static Dictionary<string, string> SplitConnectionString(string connectionString, Dictionary<string, string> synonyms)
        {
            var parsetable = new Dictionary<string, string>();
            Regex parser = s_connectionStringRegex;

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
                    string keyname = keypair.Value.Replace("==", "=").ToLowerInvariant();
                    string keyvalue = keyvalues[indexValue++].Value;
                    if (0 < keyvalue.Length)
                    {
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

                    string synonym;
                    string realkeyname = null != synonyms ?
                        (synonyms.TryGetValue(keyname, out synonym) ? synonym : null) :
                        keyname;

                    if (!IsKeyNameValid(realkeyname))
                    {
                        throw ADP.KeywordNotSupported(keyname);
                    }
                    else
                    {
                        parsetable[realkeyname] = keyvalue; // last key-value pair wins (or first)
                    }
                }
            }
            return parsetable;
        }

        private static void ParseComparison(Dictionary<string, string> parsetable, string connectionString, Dictionary<string, string> synonyms, Exception e)
        {
            try
            {
                Dictionary<string, string> parsedvalues = SplitConnectionString(connectionString, synonyms);
                foreach (KeyValuePair<string, string> pair in parsedvalues)
                {
                    string keyname = pair.Key;
                    string value1 = pair.Value;
                    string value2;
                    bool parsetableContainsKey = parsetable.TryGetValue(keyname, out value2);
                    Debug.Assert(parsetableContainsKey, $"{nameof(ParseInternal)} code vs. regex mismatch keyname <{keyname}>");
                    Debug.Assert(value1 == value2, $"{nameof(ParseInternal)} code vs. regex mismatch keyvalue <{value1}> <{value2}>");
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
                        // We also accept cases were Regex parser (debug only) reports "wrong format" and 
                        // retail parsing code reports format exception in different location or "keyword not supported"
                        if (msg2.StartsWith(WrongFormatMessagePrefix, StringComparison.Ordinal))
                        {
                            if (msg1.StartsWith(KeywordNotSupportedMessagePrefix, StringComparison.Ordinal) || msg1.StartsWith(WrongFormatMessagePrefix, StringComparison.Ordinal))
                            {
                                isEquivalent = true;
                            }
                        }
                    }
                    Debug.Assert(isEquivalent, "ParseInternal code vs regex message mismatch: <" + msg1 + "> <" + msg2 + ">");
                }
                else
                {
                    Debug.Assert(false, "ParseInternal code vs regex throw mismatch " + f.Message);
                }
                e = null;
            }
            if (null != e)
            {
                Debug.Assert(false, "ParseInternal code threw exception vs regex mismatch");
            }
        }
#endif

        private static NameValuePair ParseInternal(Dictionary<string, string> parsetable, string connectionString, bool buildChain, Dictionary<string, string> synonyms)
        {
            Debug.Assert(null != connectionString, "null connectionstring");
            StringBuilder buffer = new StringBuilder();
            NameValuePair localKeychain = null, keychain = null;
#if DEBUG
            try
            {
#endif
                int nextStartPosition = 0;
                int endPosition = connectionString.Length;
                while (nextStartPosition < endPosition)
                {
                    int startPosition = nextStartPosition;

                    string keyname, keyvalue;
                    nextStartPosition = GetKeyValuePair(connectionString, startPosition, buffer, out keyname, out keyvalue);
                    if (string.IsNullOrEmpty(keyname))
                    {
                        // if (nextStartPosition != endPosition) { throw; }
                        break;
                    }
#if DEBUG
                    DebugTraceKeyValuePair(keyname, keyvalue, synonyms);

                    Debug.Assert(IsKeyNameValid(keyname), "ParseFailure, invalid keyname");
                    Debug.Assert(IsValueValidInternal(keyvalue), "parse failure, invalid keyvalue");
#endif
                    string synonym;
                    string realkeyname = null != synonyms ?
                        (synonyms.TryGetValue(keyname, out synonym) ? synonym : null) :
                        keyname;

                    if (!IsKeyNameValid(realkeyname))
                    {
                        throw ADP.KeywordNotSupported(keyname);
                    }
                    else
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
                ParseComparison(parsetable, connectionString, synonyms, e);
                throw;
            }
            ParseComparison(parsetable, connectionString, synonyms, null);
#endif
            return keychain;
        }

        internal NameValuePair ReplacePasswordPwd(out string constr, bool fakePassword)
        {
            bool expanded = false;
            int copyPosition = 0;
            NameValuePair head = null, tail = null, next = null;
            StringBuilder builder = new StringBuilder(_usersConnectionString.Length);
            for (NameValuePair current = KeyChain; null != current; current = current.Next)
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
