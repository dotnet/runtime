// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Xml.Serialization
{
    internal class XmlSerializationPrimitiveWriter : System.Xml.Serialization.XmlSerializationWriter
    {
        internal void Write_string(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteNullTagLiteral(@"string", @"");
                return;
            }
            TopLevelElement();
            WriteNullableStringLiteral(@"string", @"", ((string)o));
        }

        internal void Write_int(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"int", @"");
                return;
            }
            WriteElementStringRaw(@"int", @"", System.Xml.XmlConvert.ToString((int)((int)o)));
        }

        internal void Write_boolean(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"boolean", @"");
                return;
            }
            WriteElementStringRaw(@"boolean", @"", System.Xml.XmlConvert.ToString((bool)((bool)o)));
        }

        internal void Write_short(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"short", @"");
                return;
            }
            WriteElementStringRaw(@"short", @"", System.Xml.XmlConvert.ToString((short)((short)o)));
        }

        internal void Write_long(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"long", @"");
                return;
            }
            WriteElementStringRaw(@"long", @"", System.Xml.XmlConvert.ToString((long)((long)o)));
        }

        internal void Write_float(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"float", @"");
                return;
            }
            WriteElementStringRaw(@"float", @"", System.Xml.XmlConvert.ToString((float)((float)o)));
        }

        internal void Write_double(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"double", @"");
                return;
            }
            WriteElementStringRaw(@"double", @"", System.Xml.XmlConvert.ToString((double)((double)o)));
        }

        internal void Write_decimal(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"decimal", @"");
                return;
            }
            // Preventing inlining optimization which is causing issue for XmlConvert.ToString(Decimal)
            decimal d = (decimal)o;
            WriteElementStringRaw(@"decimal", @"", System.Xml.XmlConvert.ToString(d));
        }

        internal void Write_dateTime(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"dateTime", @"");
                return;
            }
            WriteElementStringRaw(@"dateTime", @"", FromDateTime(((System.DateTime)o)));
        }

        internal void Write_unsignedByte(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"unsignedByte", @"");
                return;
            }
            WriteElementStringRaw(@"unsignedByte", @"", System.Xml.XmlConvert.ToString((byte)((byte)o)));
        }

        internal void Write_byte(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"byte", @"");
                return;
            }
            WriteElementStringRaw(@"byte", @"", System.Xml.XmlConvert.ToString((sbyte)((sbyte)o)));
        }

        internal void Write_unsignedShort(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"unsignedShort", @"");
                return;
            }
            WriteElementStringRaw(@"unsignedShort", @"", System.Xml.XmlConvert.ToString((ushort)((ushort)o)));
        }

        internal void Write_unsignedInt(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"unsignedInt", @"");
                return;
            }
            WriteElementStringRaw(@"unsignedInt", @"", System.Xml.XmlConvert.ToString((uint)((uint)o)));
        }

        internal void Write_unsignedLong(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"unsignedLong", @"");
                return;
            }
            WriteElementStringRaw(@"unsignedLong", @"", System.Xml.XmlConvert.ToString((ulong)((ulong)o)));
        }

        internal void Write_base64Binary(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteNullTagLiteral(@"base64Binary", @"");
                return;
            }
            TopLevelElement();
            WriteNullableStringLiteralRaw(@"base64Binary", @"", FromByteArrayBase64(((byte[])o)));
        }

        internal void Write_guid(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"guid", @"");
                return;
            }
            // Preventing inlining optimization which is causing issue for XmlConvert.ToString(Guid)
            Guid guid = (Guid)o;
            WriteElementStringRaw(@"guid", @"", System.Xml.XmlConvert.ToString(guid));
        }

        internal void Write_TimeSpan(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"TimeSpan", @"");
                return;
            }
            TimeSpan timeSpan = (TimeSpan)o;
            WriteElementStringRaw(@"TimeSpan", @"", System.Xml.XmlConvert.ToString(timeSpan));
        }

        internal void Write_char(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteEmptyTag(@"char", @"");
                return;
            }
            WriteElementString(@"char", @"", FromChar(((char)o)));
        }

        internal void Write_QName(object o)
        {
            WriteStartDocument();
            if (o == null)
            {
                WriteNullTagLiteral(@"QName", @"");
                return;
            }
            TopLevelElement();
            WriteNullableQualifiedNameLiteral(@"QName", @"", ((global::System.Xml.XmlQualifiedName)o));
        }

        protected override void InitCallbacks()
        {
        }
    }

    internal class XmlSerializationPrimitiveReader : System.Xml.Serialization.XmlSerializationReader
    {
        internal object Read_string()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id1_string && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    if (ReadNull())
                    {
                        o = null;
                    }
                    else
                    {
                        o = Reader.ReadElementString();
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_int()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id3_int && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToInt32(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_boolean()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id4_boolean && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToBoolean(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_short()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id5_short && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToInt16(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_long()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id6_long && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToInt64(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_float()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id7_float && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToSingle(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_double()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id8_double && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToDouble(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_decimal()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id9_decimal && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToDecimal(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_dateTime()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id10_dateTime && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = ToDateTime(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_unsignedByte()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id11_unsignedByte && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToByte(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_byte()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id12_byte && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToSByte(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_unsignedShort()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id13_unsignedShort && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToUInt16(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_unsignedInt()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id14_unsignedInt && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToUInt32(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_unsignedLong()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id15_unsignedLong && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToUInt64(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_base64Binary()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id16_base64Binary && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    if (ReadNull())
                    {
                        o = null;
                    }
                    else
                    {
                        o = ToByteArrayBase64(false);
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_guid()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id17_guid && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = System.Xml.XmlConvert.ToGuid(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_TimeSpan()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id19_TimeSpan && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    if(Reader.IsEmptyElement)
                    {
                        Reader.Skip();
                        o = default(TimeSpan);
                    }
                    else
                    {
                        o = System.Xml.XmlConvert.ToTimeSpan(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_char()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id18_char && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    {
                        o = ToChar(Reader.ReadElementString());
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        internal object Read_QName()
        {
            object o = null;
            Reader.MoveToContent();
            if (Reader.NodeType == System.Xml.XmlNodeType.Element)
            {
                if (((object)Reader.LocalName == (object)_id1_QName && (object)Reader.NamespaceURI == (object)_id2_Item))
                {
                    if (ReadNull())
                    {
                        o = null;
                    }
                    else
                    {
                        o = ReadElementQualifiedName();
                    }
                }
                else
                {
                    throw CreateUnknownNodeException();
                }
            }
            else
            {
                UnknownNode(null);
            }
            return (object)o;
        }

        protected override void InitCallbacks()
        {
        }

        private string _id4_boolean;
        private string _id14_unsignedInt;
        private string _id15_unsignedLong;
        private string _id7_float;
        private string _id10_dateTime;
        private string _id6_long;
        private string _id9_decimal;
        private string _id8_double;
        private string _id17_guid;
        private string _id19_TimeSpan;
        private string _id2_Item;
        private string _id13_unsignedShort;
        private string _id18_char;
        private string _id3_int;
        private string _id12_byte;
        private string _id16_base64Binary;
        private string _id11_unsignedByte;
        private string _id5_short;
        private string _id1_string;
        private string _id1_QName;

        protected override void InitIDs()
        {
            _id4_boolean = Reader.NameTable.Add(@"boolean");
            _id14_unsignedInt = Reader.NameTable.Add(@"unsignedInt");
            _id15_unsignedLong = Reader.NameTable.Add(@"unsignedLong");
            _id7_float = Reader.NameTable.Add(@"float");
            _id10_dateTime = Reader.NameTable.Add(@"dateTime");
            _id6_long = Reader.NameTable.Add(@"long");
            _id9_decimal = Reader.NameTable.Add(@"decimal");
            _id8_double = Reader.NameTable.Add(@"double");
            _id17_guid = Reader.NameTable.Add(@"guid");
            _id19_TimeSpan = Reader.NameTable.Add(@"TimeSpan");
            _id2_Item = Reader.NameTable.Add(@"");
            _id13_unsignedShort = Reader.NameTable.Add(@"unsignedShort");
            _id18_char = Reader.NameTable.Add(@"char");
            _id3_int = Reader.NameTable.Add(@"int");
            _id12_byte = Reader.NameTable.Add(@"byte");
            _id16_base64Binary = Reader.NameTable.Add(@"base64Binary");
            _id11_unsignedByte = Reader.NameTable.Add(@"unsignedByte");
            _id5_short = Reader.NameTable.Add(@"short");
            _id1_string = Reader.NameTable.Add(@"string");
            _id1_QName = Reader.NameTable.Add(@"QName");
        }
    }
}
