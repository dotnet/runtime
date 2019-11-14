// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data.SqlTypes;
using System.Globalization;

using Microsoft.SqlServer.Server;

namespace Microsoft.Samples.SqlServer
{
    [Serializable]
    [Microsoft.SqlServer.Server.SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = 8000)]
    public class Utf8String : INullable, IComparable, Microsoft.SqlServer.Server.IBinarySerialize
    {
        #region conversion to/from Unicode strings
        /// <summary>
        /// Parse the given string and return a utf8 representation for it.
        /// </summary>
        /// <param name="sqlString"></param>
        /// <returns></returns>
        public static Utf8String Parse(SqlString sqlString)
        {
            if (sqlString.IsNull)
                return Utf8String.Null;

            return new Utf8String(sqlString.Value);
        }

        /// <summary>
        /// Get/Set the utf8 bytes for this string.
        /// </summary>
        public SqlBinary Utf8Bytes
        {
            get
            {
                if (this.IsNull)
                    return SqlBinary.Null;

                if (this.m_Bytes != null)
                    return this.m_Bytes;

                if (this.m_String != null)
                {
                    this.m_Bytes = System.Text.Encoding.UTF8.GetBytes(this.m_String);
                    return new SqlBinary(this.m_Bytes);
                }

                throw new NotSupportedException("cannot return bytes for empty instance");
            }
            set
            {
                if (value.IsNull)
                {
                    this.m_Bytes = null;
                    this.m_String = null;
                }
                else
                {
                    this.m_Bytes = value.Value;
                    this.m_String = null;
                }
            }
        }

        /// <summary>
        /// Return a unicode string for this type.
        /// </summary>
        [Microsoft.SqlServer.Server.SqlMethod(IsDeterministic = true, IsPrecise = true, DataAccess = Microsoft.SqlServer.Server.DataAccessKind.None, SystemDataAccess = Microsoft.SqlServer.Server.SystemDataAccessKind.None)]
        public override string ToString()
        {
            if (this.IsNull)
                return null;

            if (this.m_String != null)
                return this.m_String;

            if (this.m_Bytes != null)
            {
                this.m_String = System.Text.Encoding.UTF8.GetString(this.m_Bytes);
                return this.m_String;
            }

            throw new NotSupportedException("dont know how to return string from empty instance");
        }

        /// <summary>
        /// Return a SqlStr
        /// </summary>
        public SqlString ToSqlString()
        {
            if (this.IsNull)
                return SqlString.Null;

            return new SqlString(this.ToString());
        }

        private SqlString GetSortKeyUsingCultureInternal(CultureInfo culture, bool ignoreCase,
            bool ignoreNonSpace, bool ignoreWidth)
        {
            if (this.IsNull)
                return SqlString.Null;

            SqlCompareOptions compareOptions = SqlCompareOptions.None;
            if (ignoreCase)
                compareOptions = compareOptions | SqlCompareOptions.IgnoreCase;

            if (ignoreNonSpace)
                compareOptions = compareOptions | SqlCompareOptions.IgnoreNonSpace;

            if (ignoreWidth)
                compareOptions = compareOptions | SqlCompareOptions.IgnoreWidth;

            return new SqlString(this.ToString(), culture.LCID, compareOptions);
        }

        [Microsoft.SqlServer.Server.SqlMethod(IsDeterministic = true, IsPrecise = true)]
        public SqlString GetSortKeyUsingCulture(string cultureName, bool ignoreCase,
            bool ignoreNonSpace, bool ignoreWidth)
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture(cultureName);
            if (culture == null)
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Culture {0} not recognized.", cultureName));

            return this.GetSortKeyUsingCultureInternal(culture, ignoreCase,
                ignoreNonSpace, ignoreWidth);
        }

        [Microsoft.SqlServer.Server.SqlMethod(IsDeterministic = false)]
        public SqlString GetSortKey(bool ignoreCase, bool ignoreNonSpace, bool ignoreWidth)
        {
            return this.GetSortKeyUsingCultureInternal(CultureInfo.CurrentCulture,
                ignoreCase, ignoreNonSpace, ignoreWidth);
        }

        #endregion

        #region comparison operators
        public override bool Equals(object obj)
        {
            return this.CompareTo(obj) == 0;
        }

        public static bool operator ==(Utf8String utf8String, Utf8String other)
        {
            return utf8String.Equals(other);
        }

        public static bool operator !=(Utf8String utf8String, Utf8String other)
        {
            return !(utf8String == other);
        }

        public static bool operator <(Utf8String utf8String, Utf8String other)
        {
            return (utf8String.CompareTo(other) < 0);
        }

        public static bool operator >(Utf8String utf8String, Utf8String other)
        {
            return (utf8String.CompareTo(other) > 0);
        }

        private int CompareUsingCultureInternal(Utf8String other, CultureInfo culture, bool ignoreCase,
            bool ignoreNonSpace, bool ignoreWidth)
        {
            // By definition
            if (other == null)
                return 1;

            if (this.IsNull)
                if (other.IsNull)
                    return 0;
                else
                    return -1;

            if (other.IsNull)
                return 1;

            return this.GetSortKeyUsingCultureInternal(culture, ignoreCase, ignoreNonSpace,
                ignoreWidth).CompareTo(other.GetSortKeyUsingCultureInternal(culture, ignoreCase,
                ignoreNonSpace, ignoreWidth));
        }

        public int CompareUsingCulture(Utf8String other, string cultureName, bool ignoreCase,
            bool ignoreNonSpace, bool ignoreWidth)
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture(cultureName);
            if (culture == null)
                throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Culture {0} not recognized.", cultureName));

            return this.CompareUsingCultureInternal(other, culture, ignoreCase,
                ignoreNonSpace, ignoreWidth);
        }

        public int Compare(Utf8String other, bool ignoreCase,
            bool ignoreNonSpace, bool ignoreWidth)
        {
            return this.CompareUsingCultureInternal(other, CultureInfo.CurrentCulture, ignoreCase,
                ignoreNonSpace, ignoreWidth);
        }

        public override int GetHashCode()
        {
            if (this.IsNull)
                return 0;

            return this.ToString().GetHashCode();
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1; //by definition

            Utf8String s = obj as Utf8String;

            if (s == null)
                throw new ArgumentException("the argument to compare is not a Utf8String");

            if (this.IsNull)
            {
                if (s.IsNull)
                    return 0;

                return -1;
            }

            if (s.IsNull)
                return 1;

            return this.ToString().CompareTo(s.ToString());
        }

        #endregion

        #region private state and constructors
        private string m_String;

        private byte[] m_Bytes;

        public Utf8String(string value)
        {
            this.m_String = value;
        }

        public Utf8String(byte[] bytes)
        {
            this.m_Bytes = bytes;
        }
        #endregion

        #region UserDefinedType boilerplate code

        public bool IsNull
        {
            get
            {
                return this.m_String == null && this.m_Bytes == null;
            }
        }

        public static Utf8String Null
        {
            get
            {
                Utf8String str = new Utf8String((string)null);

                return str;
            }
        }

        public Utf8String()
        {
        }
        #endregion

        #region IBinarySerialize Members
        public void Write(System.IO.BinaryWriter w)
        {
            byte header = (byte)(this.IsNull ? 1 : 0);

            w.Write(header);
            if (header == 1)
                return;

            byte[] bytes = this.Utf8Bytes.Value;

            w.Write(bytes.Length);
            w.Write(bytes);
        }

        public void Read(System.IO.BinaryReader r)
        {
            byte header = r.ReadByte();

            if ((header & 1) > 0)
            {
                this.m_Bytes = null;
                return;
            }

            int length = r.ReadInt32();

            this.m_Bytes = r.ReadBytes(length);
        }
        #endregion
    }
}