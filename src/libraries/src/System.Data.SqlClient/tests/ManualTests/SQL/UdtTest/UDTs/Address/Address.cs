// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Data.SqlTypes;

using Microsoft.SqlServer.Server;

[Serializable]
[SqlUserDefinedType(Format.UserDefined, IsByteOrdered = false, MaxByteSize = 500)]
public class Address : INullable, IBinarySerialize
{
    public static Address Null { get { return new Address(true); } }

    //******************************************************
    // Constructors
    //******************************************************

    // Constructor for a null value: Address.Null
    // fNull is not used but needed because compiler doesn't let
    // struct have parameterless constructors
    private Address(bool fNull)
    {
        m_fNotNull = false;
        m_firstline = SqlString.Null;
        m_secondline = SqlString.Null;
    }

    public Address(SqlString line1, SqlString line2)
    {
        m_firstline = line1;
        m_secondline = line2;
        m_fNotNull = true;
    }

    public Address()
    {
        m_fNotNull = false;
        m_firstline = SqlString.Null;
        m_secondline = SqlString.Null;
    }

    //******************************************************
    // INullable interface
    //******************************************************

    // INullable
    public bool IsNull
    {
        get { return !m_fNotNull; }
    }

    //******************************************************
    // Common static and instance methods for SQL UDTs
    //******************************************************
    public const int MaxByteSize = 500;
    public const bool IsFixedLength = false;
    public const bool IsByteOrdered = true;

    public void Read(BinaryReader r)
    {
        m_firstline = new SqlString(r.ReadString());
        m_secondline = new SqlString(r.ReadString());
        m_fNotNull = BitConverter.ToBoolean(r.ReadBytes(1), 0);
    }

    public void Write(BinaryWriter w)
    {
        w.Write(m_firstline.ToString());
        w.Write(m_secondline.ToString());
        w.Write(m_fNotNull);
    }

    public void FillFromBytes(SqlBytes value)
    {
        if (value.IsNull)
        {
            m_fNotNull = false;
            m_firstline = SqlString.Null;
            m_secondline = SqlString.Null;
            return;
        }

        System.Text.UnicodeEncoding e = new System.Text.UnicodeEncoding();
        string str = e.GetString(value.Buffer);

        string[] twolines = new string[2];
        char[] seperator = { '|' };
        twolines = str.Split(seperator);

        m_firstline = twolines[0];
        m_secondline = twolines.Length > 1 ? twolines[1] : SqlString.Null;
        m_fNotNull = true;

        return;
    }

    public void FillBytes(SqlBytes value)
    {
        if (IsNull)
        {
            if (value.IsNull)
                return;
            else
            {
                value.SetNull();
                return;
            }
        }

        SqlString str;
        if ((object)m_secondline == null || m_secondline.IsNull)
            str = m_firstline;
        else
        {
            str = string.Concat(m_firstline, "|");
            str = string.Concat(str, m_secondline);
        }

        byte[] stringData = str.GetUnicodeBytes();
        int i;
        for (i = 0; i < stringData.Length; i++)
            value[i] = stringData[i];
        value.SetLength(i);

        return;
    }

    public override string ToString()
    {
        if (IsNull)
            return "Null";
        else
            return Value.ToString();
    }

    public static Address Parse(SqlString s)
    {
        if (s.IsNull)
            return Address.Null;

        string str = s.ToString();
        string[] twolines = new string[2];

        // using || to indicate the separation between
        // address line 1 and 2, assume it won't appear
        // in any address
        char[] seperator = { '|', '|' };
        twolines = str.Split(seperator);

        if (twolines.Length == 2)
            return new Address(twolines[0], twolines[1]);
        else
            return new Address(twolines[0], SqlString.Null);
    }

    //******************************************************
    // Address Specific Methods
    //******************************************************
    public SqlString GetFirstLine()
    {
        return m_firstline;
    }

    public SqlString GetSecondLine()
    {
        return m_secondline;
    }

    public SqlString Value
    {
        get
        {
            if (m_fNotNull)
            {
                if (m_secondline.IsNull)
                    return m_firstline;
                else
                    return string.Concat(m_firstline, m_secondline);
            }
            else
                throw new SqlNullValueException();
        }
    }

    //******************************************************
    // Address Private Members
    //******************************************************
    private SqlString m_firstline;
    private SqlString m_secondline;
    private bool m_fNotNull; //false if null, default ctor makes it null
}