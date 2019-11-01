// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml
{
    /* These are the tokens used by the Yukon BinaryXml protocol */
    internal enum BinXmlToken
    {
        Error = 0,
        NotImpl = -2,
        EOF = -1,
        XmlDecl = 0xFE,
        Encoding = 0xFD,
        DocType = 0xFC,
        System = 0xFB,
        Public = 0xFA,
        Subset = 0xF9,
        Element = 0xF8,
        EndElem = 0xF7,
        Attr = 0xF6,
        EndAttrs = 0xF5,
        PI = 0xF4,
        Comment = 0xF3,
        CData = 0xF2,
        EndCData = 0xF1,
        Name = 0xF0,
        QName = 0xEF,
        XmlText = 0xED,
        Nest = 0xEC,
        EndNest = 0xEB,
        Extn = 0xEA,
        NmFlush = 0xE9,
        SQL_BIT = 0x06,
        SQL_TINYINT = 0x07,
        SQL_SMALLINT = 0x1,
        SQL_INT = 0x02,
        SQL_BIGINT = 0x08,
        SQL_REAL = 0x03,
        SQL_FLOAT = 0x04,
        SQL_MONEY = 0x05,
        SQL_SMALLMONEY = 0x14,
        SQL_DATETIME = 0x12,
        SQL_SMALLDATETIME = 0x13,
        SQL_DECIMAL = 0x0A,
        SQL_NUMERIC = 0x0B,
        SQL_UUID = 0x09,
        SQL_VARBINARY = 0x0F,
        SQL_BINARY = 0x0C,
        SQL_IMAGE = 0x17,
        SQL_CHAR = 0x0D,
        SQL_VARCHAR = 0x10,
        SQL_TEXT = 0x16,
        SQL_NVARCHAR = 0x11,
        SQL_NCHAR = 0x0E,
        SQL_NTEXT = 0x18,
        SQL_UDT = 0x1B,
        XSD_KATMAI_DATE = 0x7F,
        XSD_KATMAI_DATETIME = 0x7E,
        XSD_KATMAI_TIME = 0x7D,
        XSD_KATMAI_DATEOFFSET = 0x7C,
        XSD_KATMAI_DATETIMEOFFSET = 0x7B,
        XSD_KATMAI_TIMEOFFSET = 0x7A,
        XSD_BOOLEAN = 0x86,
        XSD_TIME = 0x81,
        XSD_DATETIME = 0x82,
        XSD_DATE = 0x83,
        XSD_BINHEX = 0x84,
        XSD_BASE64 = 0x85,
        XSD_DECIMAL = 0x87,
        XSD_BYTE = 0x88,
        XSD_UNSIGNEDSHORT = 0x89,
        XSD_UNSIGNEDINT = 0x8A,
        XSD_UNSIGNEDLONG = 0x8B,
        XSD_QNAME = 0x8C,
    }
}