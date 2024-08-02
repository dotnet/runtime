// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data
{
    /// <summary>
    /// Specifies SQL Server-specific data type of a field, property, for use in a <see cref="T:Microsoft.Data.SqlClient.SqlParameter" />.
    /// </summary>
    public enum SqlDbType
    {
        /// <summary>
        /// <see cref="long" />. A 64-bit signed integer.
        /// </summary>
        BigInt = 0,

        /// <summary>
        /// <see cref="Array"/> of type <see cref="byte"/>. A fixed-length stream of binary data ranging between 1 and 8,000 bytes.
        /// </summary>
        Binary = 1,

        /// <summary>
        /// <see cref="bool"/>. An unsigned numeric value that can be 0, 1, or <see langword="null"/>
        /// </summary>
        Bit = 2,

        /// <summary>
        /// <see cref="string"/>. A fixed-length stream of non-Unicode characters ranging between 1 and 8,000 characters.
        /// </summary>
        Char = 3,

        /// <summary>
        /// <see cref="System.DateTime"/>. Date and time data ranging in value from January 1, 1753 to December 31, 9999 to an accuracy of 3.33 milliseconds
        /// </summary>
        DateTime = 4,

        /// <summary>
        /// <see cref="decimal"/>. A fixed precision and scale numeric value between -10 <sup>38</sup> -1 and 10 <sup>38</sup> -1.
        /// </summary>
        Decimal = 5,

        /// <summary>
        /// <see cref="double"/>. A floating point number within the range of -1.79E +308 through 1.79E +308.
        /// </summary>
        Float = 6,

        /// <summary>
        /// <see cref="Array"/> of type <see cref="byte"/>. A variable-length stream of binary data ranging from 0 to 2 <sup>31</sup> -1
        /// (or 2,147,483,647) bytes.
        /// </summary>
        Image = 7,

        /// <summary>
        /// <see cref="int"/>. A 32-bit signed integer.
        /// </summary>
        Int = 8,

        /// <summary>
        /// <see cref="decimal"/>. A currency value ranging from -2 <sup>63</sup> (or -9,223,372,036,854,775,808) to 2 <sup>63</sup>
        /// -1 (or +9,223,372,036,854,775,807) with an accuracy to a ten-thousandth of a currency unit.
        /// </summary>
        Money = 9,

        /// <summary>
        /// <see cref="string"/>. A fixed-length stream of Unicode characters ranging between 1 and 4,000 characters.
        /// </summary>
        NChar = 10,

        /// <summary>
        /// <see cref="string"/>. A variable-length stream of Unicode data with a maximum length of 2 <sup>30</sup> - 1
        /// (or 1,073,741,823) characters.
        /// </summary>
        NText = 11,

        /// <summary>
        /// <see cref="string"/>. A variable-length stream of Unicode characters ranging between 1 and 4,000 characters. Implicit
        /// conversion fails if the string is greater than 4,000 characters. Explicitly set the object when working with strings
        /// longer than 4,000 characters. Use <see cref="F:System.Data.SqlDbType.NVarChar"/> when the database column is
        /// <see langword="nvarchar(max)"/>.
        /// </summary>
        NVarChar = 12,

        /// <summary>
        /// <see cref="float"/>. A floating point number within the range of -3.40E +38 through 3.40E +38.
        /// </summary>
        Real = 13,

        /// <summary>
        /// <see cref="Guid"/>. A globally unique identifier (or GUID).
        /// </summary>
        UniqueIdentifier = 14,

        /// <summary>
        /// <see cref="System.DateTime"/>. Date and time data ranging in value from January 1, 1900 to June 6, 2079 to an accuracy of one minute.
        /// </summary>
        SmallDateTime = 15,

        /// <summary>
        /// <see cref="short"/>. A 16-bit signed integer.
        /// </summary>
        SmallInt = 16,

        /// <summary>
        /// <see cref="decimal"/>. A currency value ranging from -214,748.3648 to +214,748.3647 with an accuracy to a ten-thousandth of a currency unit.
        /// </summary>
        SmallMoney = 17,

        /// <summary>
        /// <see cref="string"/>. A variable-length stream of non-Unicode data with a maximum length of 2 <sup>31</sup> -1 (or 2,147,483,647) characters.
        /// </summary>
        Text = 18,

        /// <summary>
        /// <see cref="System.Array"/> of type <see cref="byte"/>. Automatically generated binary numbers, which are guaranteed to be unique within a database. <see langword="timestamp"/> is used typically as a mechanism for version-stamping table rows. The storage size is 8 bytes.
        /// </summary>
        Timestamp = 19,

        /// <summary>
        /// <see cref="byte"/>. An 8-bit unsigned integer.
        /// </summary>
        TinyInt = 20,

        /// <summary>
        /// <see cref="System.Array"/> of type <see cref="byte"/>. A variable-length stream of binary data ranging between 1 and 8,000 bytes.
        /// Implicit conversion fails if the byte array is greater than 8,000 bytes. Explicitly set the object when working with byte arrays
        /// larger than 8,000 bytes.
        /// </summary>
        VarBinary = 21,

        /// <summary>
        /// <see cref="string"/>. A variable-length stream of non-Unicode characters ranging between 1 and 8,000 characters.
        /// Use <see cref="F:System.Data.SqlDbType.VarChar"/> when the database column is <see langword="varchar(max)"/>.
        /// </summary>
        VarChar = 22,

        /// <summary>
        /// <see cref="object"/>. A special data type that can contain numeric, string, binary, or date data as well as the SQL Server values Empty and Null,
        /// which is assumed if no other type is declared.
        /// </summary>
        Variant = 23,

        /// <summary>
        /// An XML value. Obtain the XML as a string using the <see cref="M:Microsoft.Data.SqlClient.SqlDataReader.GetValue(System.Int32)"/> method or
        /// <see cref="P:System.Data.SqlTypes.SqlXml.Value"/> property, or as an <see cref="T:System.Xml.XmlReader"/> by calling the
        /// <see cref="M:System.Data.SqlTypes.SqlXml.CreateReader"/> method.
        /// </summary>
        Xml = 25,

        /// <summary>
        /// A SQL Server user-defined type (UDT).
        /// </summary>
        Udt = 29,

        /// <summary>
        /// A special data type for specifying structured data contained in table-valued parameters.
        /// </summary>
        Structured = 30,

        /// <summary>
        /// Date data ranging in value from January 1,1 AD through December 31, 9999 AD.
        /// </summary>
        Date = 31,

        /// <summary>
        /// Time data based on a 24-hour clock. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds.
        /// Corresponds to a SQL Server <see langword="time"/> value.
        /// </summary>
        Time = 32,

        /// <summary>
        /// Date and time data. Date value range is from January 1,1 AD through December 31, 9999 AD.
        /// Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds.
        /// </summary>
        DateTime2 = 33,

        /// <summary>
        /// Date and time data with time zone awareness. Date value range is from January 1,1 AD through December 31, 9999 AD.
        /// Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds. Time zone value range
        /// is -14:00 through +14:00.
        /// </summary>
        DateTimeOffset = 34,

        /// <summary>
        /// A JSON value.
        /// </summary>
        Json = 35,
    }
}
