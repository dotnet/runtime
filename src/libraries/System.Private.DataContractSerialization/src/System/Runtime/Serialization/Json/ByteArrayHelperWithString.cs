// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal sealed class ByteArrayHelperWithString : ArrayHelper<string, byte>
    {
        public static readonly ByteArrayHelperWithString Instance = new ByteArrayHelperWithString();

        internal void WriteArray(XmlWriter writer, byte[] array, int offset, int count)
        {
            XmlJsonReader.CheckArray(array, offset, count);
            writer.WriteAttributeString(string.Empty, JsonGlobals.typeString, string.Empty, JsonGlobals.arrayString);
            for (int i = 0; i < count; i++)
            {
                writer.WriteStartElement(JsonGlobals.itemString, string.Empty);
                writer.WriteAttributeString(string.Empty, JsonGlobals.typeString, string.Empty, JsonGlobals.numberString);
                writer.WriteValue((int)array[offset + i]);
                writer.WriteEndElement();
            }
        }

        protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, byte[] array, int offset, int count)
        {
            XmlJsonReader.CheckArray(array, offset, count);
            int actual = 0;
            while (actual < count && reader.IsStartElement(JsonGlobals.itemString, string.Empty))
            {
                array[offset + actual] = ToByte(reader.ReadElementContentAsInt());
                actual++;
            }
            return actual;
        }

        protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, byte[] array, int offset, int count)
        {
            WriteArray(writer, array, offset, count);
        }

        private void ThrowConversionException(string value, string type)
        {
            throw new XmlException(SR.Format(SR.XmlInvalidConversion, value, type));
        }

        private byte ToByte(int value)
        {
            if (value < byte.MinValue || value > byte.MaxValue)
            {
                ThrowConversionException(value.ToString(System.Globalization.NumberFormatInfo.CurrentInfo), "Byte");
            }
            return (byte)value;
        }
    }
}
