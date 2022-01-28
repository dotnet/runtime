// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;                   // Debug services
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

namespace System.Data.Odbc
{
    internal sealed class CNativeBuffer : System.Data.ProviderBase.DbBuffer
    {
        internal CNativeBuffer(int initialSize) : base(initialSize)
        {
        }

        internal short ShortLength
        {
            get
            {
                return checked((short)Length);
            }
        }

        internal object MarshalToManaged(int offset, ODBC32.SQL_C sqlctype, int cb)
        {
            object value;
            switch (sqlctype)
            {
                case ODBC32.SQL_C.WCHAR:
                    //Note: We always bind as unicode
                    if (cb == ODBC32.SQL_NTS)
                    {
                        value = PtrToStringUni(offset);
                        break;
                    }
                    Debug.Assert((cb > 0), "Character count negative ");
                    Debug.Assert((Length >= cb), "Native buffer too small ");
                    cb = Math.Min(cb / 2, (Length - 2) / 2);
                    value = PtrToStringUni(offset, cb);
                    break;

                case ODBC32.SQL_C.CHAR:
                case ODBC32.SQL_C.BINARY:
                    Debug.Assert((cb > 0), "Character count negative ");
                    Debug.Assert((Length >= cb), "Native buffer too small ");
                    cb = Math.Min(cb, Length);
                    value = ReadBytes(offset, cb);
                    break;

                case ODBC32.SQL_C.SSHORT:
                    value = ReadInt16(offset);
                    break;

                case ODBC32.SQL_C.SLONG:
                    value = ReadInt32(offset);
                    break;

                case ODBC32.SQL_C.SBIGINT:
                    value = ReadInt64(offset);
                    break;

                case ODBC32.SQL_C.BIT:
                    byte b = ReadByte(offset);
                    value = (b != 0x00);
                    break;

                case ODBC32.SQL_C.REAL:
                    value = ReadSingle(offset);
                    break;

                case ODBC32.SQL_C.DOUBLE:
                    value = ReadDouble(offset);
                    break;

                case ODBC32.SQL_C.UTINYINT:
                    value = ReadByte(offset);
                    break;

                case ODBC32.SQL_C.GUID:
                    value = ReadGuid(offset);
                    break;

                case ODBC32.SQL_C.TYPE_TIMESTAMP:
                    //So we are mapping this ourselves.
                    //typedef struct tagTIMESTAMP_STRUCT
                    //{
                    //      SQLSMALLINT    year;
                    //      SQLUSMALLINT   month;
                    //      SQLUSMALLINT   day;
                    //      SQLUSMALLINT   hour;
                    //      SQLUSMALLINT   minute;
                    //      SQLUSMALLINT   second;
                    //      SQLUINTEGER    fraction;    (billoniths of a second)
                    //}

                    value = ReadDateTime(offset);
                    break;

                // Note: System does not provide a date-only type
                case ODBC32.SQL_C.TYPE_DATE:
                    //  typedef struct tagDATE_STRUCT
                    //  {
                    //      SQLSMALLINT    year;
                    //      SQLUSMALLINT   month;
                    //      SQLUSMALLINT   day;
                    //  } DATE_STRUCT;

                    value = ReadDate(offset);
                    break;

                // Note: System does not provide a date-only type
                case ODBC32.SQL_C.TYPE_TIME:
                    //  typedef struct tagTIME_STRUCT
                    //  {
                    //      SQLUSMALLINT   hour;
                    //      SQLUSMALLINT   minute;
                    //      SQLUSMALLINT   second;
                    //  } TIME_STRUCT;

                    value = ReadTime(offset);
                    break;

                case ODBC32.SQL_C.NUMERIC:
                    //Note: Unfortunatly the ODBC NUMERIC structure and the URT DECIMAL structure do not
                    //align, so we can't so do the typical "PtrToStructure" call (below) like other types
                    //We actually have to go through the pain of pulling our raw bytes and building the decimal
                    //  Marshal.PtrToStructure(buffer, typeof(decimal));

                    //So we are mapping this ourselves
                    //typedef struct tagSQL_NUMERIC_STRUCT
                    //{
                    //  SQLCHAR     precision;
                    //  SQLSCHAR    scale;
                    //  SQLCHAR     sign;   /* 1 if positive, 0 if negative */
                    //  SQLCHAR     val[SQL_MAX_NUMERIC_LEN];
                    //} SQL_NUMERIC_STRUCT;

                    value = ReadNumeric(offset);
                    break;

                default:
                    Debug.Fail("UnknownSQLCType");
                    value = null;
                    break;
            };
            return value;
        }

        // if sizeorprecision applies only for wchar and numeric values
        // for wchar the a value of null means take the value's size
        //
        internal void MarshalToNative(int offset, object value, ODBC32.SQL_C sqlctype, int sizeorprecision, int valueOffset)
        {
            switch (sqlctype)
            {
                case ODBC32.SQL_C.WCHAR:
                    {
                        //Note: We always bind as unicode
                        //Note: StructureToPtr fails indicating string it a non-blittable type
                        //and there is no MarshalStringTo* that moves to an existing buffer,
                        //they all alloc and return a new one, not at all what we want...

                        //So we have to copy the raw bytes of the string ourself?!

                        char[] rgChars;
                        int length;
                        Debug.Assert(value is string || value is char[], "Only string or char[] can be marshaled to WCHAR");

                        if (value is string)
                        {
                            length = Math.Max(0, ((string)value).Length - valueOffset);

                            if ((sizeorprecision > 0) && (sizeorprecision < length))
                            {
                                length = sizeorprecision;
                            }

                            rgChars = ((string)value).ToCharArray(valueOffset, length);
                            Debug.Assert(rgChars.Length < (base.Length - valueOffset), "attempting to extend parameter buffer!");

                            WriteCharArray(offset, rgChars, 0, rgChars.Length);
                            WriteInt16(offset + (rgChars.Length * 2), 0); // Add the null terminator
                        }
                        else
                        {
                            length = Math.Max(0, ((char[])value).Length - valueOffset);
                            if ((sizeorprecision > 0) && (sizeorprecision < length))
                            {
                                length = sizeorprecision;
                            }
                            rgChars = (char[])value;
                            Debug.Assert(rgChars.Length < (base.Length - valueOffset), "attempting to extend parameter buffer!");

                            WriteCharArray(offset, rgChars, valueOffset, length);
                            WriteInt16(offset + (rgChars.Length * 2), 0); // Add the null terminator
                        }
                        break;
                    }

                case ODBC32.SQL_C.BINARY:
                case ODBC32.SQL_C.CHAR:
                    {
                        byte[] rgBytes = (byte[])value;
                        int length = rgBytes.Length;

                        Debug.Assert((valueOffset <= length), "Offset out of Range");

                        // reduce length by the valueOffset
                        //
                        length -= valueOffset;

                        // reduce length to be no more than size (if size is given)
                        //
                        if ((sizeorprecision > 0) && (sizeorprecision < length))
                        {
                            length = sizeorprecision;
                        }

                        //AdjustSize(rgBytes.Length+1);
                        //buffer = DangerousAllocateAndGetHandle();      // Realloc may have changed buffer address
                        Debug.Assert(length < (base.Length - valueOffset), "attempting to extend parameter buffer!");

                        WriteBytes(offset, rgBytes, valueOffset, length);
                        break;
                    }

                case ODBC32.SQL_C.UTINYINT:
                    WriteByte(offset, (byte)value);
                    break;

                case ODBC32.SQL_C.SSHORT:   //Int16
                    WriteInt16(offset, (short)value);
                    break;

                case ODBC32.SQL_C.SLONG:    //Int32
                    WriteInt32(offset, (int)value);
                    break;

                case ODBC32.SQL_C.REAL:     //float
                    WriteSingle(offset, (float)value);
                    break;

                case ODBC32.SQL_C.SBIGINT:  //Int64
                    WriteInt64(offset, (long)value);
                    break;

                case ODBC32.SQL_C.DOUBLE:   //Double
                    WriteDouble(offset, (double)value);
                    break;

                case ODBC32.SQL_C.GUID:     //Guid
                    WriteGuid(offset, (Guid)value);
                    break;

                case ODBC32.SQL_C.BIT:
                    WriteByte(offset, (byte)(((bool)value) ? 1 : 0));
                    break;

                case ODBC32.SQL_C.TYPE_TIMESTAMP:
                    {
                        //typedef struct tagTIMESTAMP_STRUCT
                        //{
                        //      SQLSMALLINT    year;
                        //      SQLUSMALLINT   month;
                        //      SQLUSMALLINT   day;
                        //      SQLUSMALLINT   hour;
                        //      SQLUSMALLINT   minute;
                        //      SQLUSMALLINT   second;
                        //      SQLUINTEGER    fraction;    (billoniths of a second)
                        //}

                        //We have to map this ourselves, due to the different structures between
                        //ODBC TIMESTAMP and URT DateTime, (ie: can't use StructureToPtr)

                        WriteODBCDateTime(offset, (DateTime)value);
                        break;
                    }

                // Note: System does not provide a date-only type
                case ODBC32.SQL_C.TYPE_DATE:
                    {
                        //  typedef struct tagDATE_STRUCT
                        //  {
                        //      SQLSMALLINT    year;
                        //      SQLUSMALLINT   month;
                        //      SQLUSMALLINT   day;
                        //  } DATE_STRUCT;

                        WriteDate(offset, (DateTime)value);
                        break;
                    }

                // Note: System does not provide a date-only type
                case ODBC32.SQL_C.TYPE_TIME:
                    {
                        //  typedef struct tagTIME_STRUCT
                        //  {
                        //      SQLUSMALLINT   hour;
                        //      SQLUSMALLINT   minute;
                        //      SQLUSMALLINT   second;
                        //  } TIME_STRUCT;

                        WriteTime(offset, (TimeSpan)value);
                        break;
                    }

                case ODBC32.SQL_C.NUMERIC:
                    {
                        WriteNumeric(offset, (decimal)value, checked((byte)sizeorprecision));
                        break;
                    }

                default:
                    Debug.Fail("UnknownSQLCType");
                    break;
            }
        }

        internal HandleRef PtrOffset(int offset, int length)
        {
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // NOTE: You must have called DangerousAddRef before calling this
            //       method, or you run the risk of allowing Handle Recycling
            //       to occur!
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            Validate(offset, length);

            IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
            return new HandleRef(this, ptr);
        }

        internal void WriteODBCDateTime(int offset, DateTime value)
        {
            short[] buffer = new short[6] {
                unchecked((short)value.Year),
                unchecked((short)value.Month),
                unchecked((short)value.Day),
                unchecked((short)value.Hour),
                unchecked((short)value.Minute),
                unchecked((short)value.Second),
            };
            WriteInt16Array(offset, buffer, 0, 6);
            WriteInt32(offset + 12, value.Millisecond * 1000000); //fraction
        }
    }


    internal sealed class CStringTokenizer
    {
        private readonly StringBuilder _token;
        private readonly string _sqlstatement;
        private readonly char _quote;         // typically the semicolon '"'
        private readonly char _escape;        // typically the same char as the quote
        private readonly int _len;
        private int _idx;

        internal CStringTokenizer(string text, char quote, char escape)
        {
            _token = new StringBuilder();
            _quote = quote;
            _escape = escape;
            _sqlstatement = text;
            if (text != null)
            {
                int iNul = text.IndexOf('\0');
                _len = (0 > iNul) ? text.Length : iNul;
            }
            else
            {
                _len = 0;
            }
        }

        internal int CurrentPosition
        {
            get { return _idx; }
        }

        // Returns the next token in the statement, advancing the current index to
        //  the start of the token
        internal string NextToken()
        {
            if (_token.Length != 0)
            {                   // if we've read a token before
                _idx += _token.Length;                  // proceed the internal marker (_idx) behind the token
                _token.Remove(0, _token.Length);        // and start over with a fresh token
            }

            while ((_idx < _len) && char.IsWhiteSpace(_sqlstatement[_idx]))
            {
                // skip whitespace
                _idx++;
            }

            if (_idx == _len)
            {
                // return if string is empty
                return string.Empty;
            }

            int curidx = _idx;                          // start with internal index at current index
            bool endtoken = false;                      //

            // process characters until we reache the end of the token or the end of the string
            //
            while (!endtoken && curidx < _len)
            {
                if (IsValidNameChar(_sqlstatement[curidx]))
                {
                    while ((curidx < _len) && IsValidNameChar(_sqlstatement[curidx]))
                    {
                        _token.Append(_sqlstatement[curidx]);
                        curidx++;
                    }
                }
                else
                {
                    char currentchar = _sqlstatement[curidx];
                    if (currentchar == '[')
                    {
                        curidx = GetTokenFromBracket(curidx);
                    }
                    else if (' ' != _quote && currentchar == _quote)
                    { // if the ODBC driver does not support quoted identifiers it returns a single blank character
                        curidx = GetTokenFromQuote(curidx);
                    }
                    else
                    {
                        // Some other marker like , ; ( or )
                        // could also be * or ?
                        if (!char.IsWhiteSpace(currentchar))
                        {
                            switch (currentchar)
                            {
                                case ',':
                                    // commas are not part of a token so we'll only append them if they are at the beginning
                                    if (curidx == _idx)
                                        _token.Append(currentchar);
                                    break;
                                default:
                                    _token.Append(currentchar);
                                    break;
                            }
                        }
                        break;
                    }
                }
            }

            return (_token.Length > 0) ? _token.ToString() : string.Empty;
        }

        private int GetTokenFromBracket(int curidx)
        {
            Debug.Assert((_sqlstatement[curidx] == '['), "GetTokenFromQuote: character at starting position must be same as quotechar");
            while (curidx < _len)
            {
                _token.Append(_sqlstatement[curidx]);
                curidx++;
                if (_sqlstatement[curidx - 1] == ']')
                    break;
            }
            return curidx;
        }

        // attempts to complete an encapsulated token (e.g. "scott")
        // double quotes are valid part of the token (e.g. "foo""bar")
        //
        private int GetTokenFromQuote(int curidx)
        {
            Debug.Assert(_quote != ' ', "ODBC driver doesn't support quoted identifiers -- GetTokenFromQuote should not be used in this case");
            Debug.Assert((_sqlstatement[curidx] == _quote), "GetTokenFromQuote: character at starting position must be same as quotechar");

            int localidx = curidx;                                  // start with local index at current index
            while (localidx < _len)
            {                               // run to the end of the statement
                _token.Append(_sqlstatement[localidx]);             // append current character to token
                if (_sqlstatement[localidx] == _quote)
                {
                    if (localidx > curidx)
                    {                         // don't care for the first char
                        if (_sqlstatement[localidx - 1] != _escape)
                        { // if it's not escape we look at the following char
                            if (localidx + 1 < _len)
                            {                // do not overrun the end of the string
                                if (_sqlstatement[localidx + 1] != _quote)
                                {
                                    return localidx + 1;              // We've reached the end of the quoted text
                                }
                            }
                        }
                    }
                }
                localidx++;
            }
            return localidx;
        }

        private static bool IsValidNameChar(char ch)
        {
            return (char.IsLetterOrDigit(ch) ||
                    (ch == '_') || (ch == '-') || (ch == '.') ||
                    (ch == '$') || (ch == '#') || (ch == '@') ||
                    (ch == '~') || (ch == '`') || (ch == '%') ||
                    (ch == '^') || (ch == '&') || (ch == '|'));
        }

        // Searches for the token given, starting from the current position
        // If found, positions the currentindex at the
        // beginning of the token if found.
        internal int FindTokenIndex(string tokenString)
        {
            string nextToken;
            while (true)
            {
                nextToken = NextToken();
                if ((_idx == _len) || string.IsNullOrEmpty(nextToken))
                { // fxcop
                    break;
                }
                if (string.Equals(tokenString, nextToken, StringComparison.OrdinalIgnoreCase))
                {
                    return _idx;
                }
            }
            return -1;
        }

        // Skips the whitespace found in the beginning of the string.
        internal bool StartsWith(string tokenString)
        {
            int tempidx = 0;
            while ((tempidx < _len) && char.IsWhiteSpace(_sqlstatement[tempidx]))
            {
                tempidx++;
            }
            if ((_len - tempidx) < tokenString.Length)
            {
                return false;
            }

            if (0 == string.Compare(_sqlstatement, tempidx, tokenString, 0, tokenString.Length, StringComparison.OrdinalIgnoreCase))
            {
                // Reset current position and token
                _idx = 0;
                NextToken();
                return true;
            }
            return false;
        }
    }
}
