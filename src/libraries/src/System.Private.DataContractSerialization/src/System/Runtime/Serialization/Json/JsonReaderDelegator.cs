// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Runtime.Serialization;
using System.Globalization;

namespace System.Runtime.Serialization.Json
{
    internal class JsonReaderDelegator : XmlReaderDelegator
    {
        private DateTimeFormat _dateTimeFormat;
        private DateTimeArrayJsonHelperWithString _dateTimeArrayHelper;

        public JsonReaderDelegator(XmlReader reader)
            : base(reader)
        {
        }

        public JsonReaderDelegator(XmlReader reader, DateTimeFormat dateTimeFormat)
            : this(reader)
        {
            _dateTimeFormat = dateTimeFormat;
        }

        internal XmlDictionaryReaderQuotas ReaderQuotas
        {
            get
            {
                if (this.dictionaryReader == null)
                {
                    return null;
                }
                else
                {
                    return dictionaryReader.Quotas;
                }
            }
        }

        private DateTimeArrayJsonHelperWithString DateTimeArrayHelper
        {
            get
            {
                if (_dateTimeArrayHelper == null)
                {
                    _dateTimeArrayHelper = new DateTimeArrayJsonHelperWithString(_dateTimeFormat);
                }
                return _dateTimeArrayHelper;
            }
        }

        internal static XmlQualifiedName ParseQualifiedName(string qname)
        {
            string name, ns;
            if (string.IsNullOrEmpty(qname))
            {
                name = ns = string.Empty;
            }
            else
            {
                qname = qname.Trim();
                int colon = qname.IndexOf(':');
                if (colon >= 0)
                {
                    name = qname.Substring(0, colon);
                    ns = qname.Substring(colon + 1);
                }
                else
                {
                    name = qname;
                    ns = string.Empty;
                }
            }
            return new XmlQualifiedName(name, ns);
        }

        internal override char ReadContentAsChar()
        {
            return XmlConvert.ToChar(ReadContentAsString());
        }

        internal override XmlQualifiedName ReadContentAsQName()
        {
            return ParseQualifiedName(ReadContentAsString());
        }

        internal override char ReadElementContentAsChar()
        {
            return XmlConvert.ToChar(ReadElementContentAsString());
        }

        public override byte[] ReadContentAsBase64()
        {
            if (isEndOfEmptyElement)
                return Array.Empty<byte>();

            byte[] buffer;

            if (dictionaryReader == null)
            {
                XmlDictionaryReader tempDictionaryReader = XmlDictionaryReader.CreateDictionaryReader(reader);
                buffer = ByteArrayHelperWithString.Instance.ReadArray(tempDictionaryReader, JsonGlobals.itemString, string.Empty, tempDictionaryReader.Quotas.MaxArrayLength);
            }
            else
            {
                buffer = ByteArrayHelperWithString.Instance.ReadArray(dictionaryReader, JsonGlobals.itemString, string.Empty, dictionaryReader.Quotas.MaxArrayLength);
            }
            return buffer;
        }

        internal override byte[] ReadElementContentAsBase64()
        {
            if (isEndOfEmptyElement)
            {
                throw new XmlException(SR.Format(SR.XmlStartElementExpected, "EndElement"));
            }

            bool isEmptyElement = reader.IsStartElement() && reader.IsEmptyElement;
            byte[] buffer;

            if (isEmptyElement)
            {
                reader.Read();
                buffer = Array.Empty<byte>();
            }
            else
            {
                reader.ReadStartElement();
                buffer = ReadContentAsBase64();
                reader.ReadEndElement();
            }

            return buffer;
        }

        internal override DateTime ReadContentAsDateTime()
        {
            return ParseJsonDate(ReadContentAsString(), _dateTimeFormat);
        }

        internal static DateTime ParseJsonDate(string originalDateTimeValue, DateTimeFormat dateTimeFormat)
        {
            if (dateTimeFormat == null)
            {
                return ParseJsonDateInDefaultFormat(originalDateTimeValue);
            }
            else
            {
                return DateTime.ParseExact(originalDateTimeValue, dateTimeFormat.FormatString, dateTimeFormat.FormatProvider, dateTimeFormat.DateTimeStyles);
            }
        }

        internal static DateTime ParseJsonDateInDefaultFormat(string originalDateTimeValue)
        {
            // Dates are represented in JSON as "\/Date(number of ticks)\/".
            // The number of ticks is the number of milliseconds since January 1, 1970.

            string dateTimeValue;

            if (!string.IsNullOrEmpty(originalDateTimeValue))
            {
                dateTimeValue = originalDateTimeValue.Trim();
            }
            else
            {
                dateTimeValue = originalDateTimeValue;
            }

            if (string.IsNullOrEmpty(dateTimeValue) ||
                !dateTimeValue.StartsWith(JsonGlobals.DateTimeStartGuardReader, StringComparison.Ordinal) ||
                !dateTimeValue.EndsWith(JsonGlobals.DateTimeEndGuardReader, StringComparison.Ordinal))
            {
                throw new FormatException(SR.Format(SR.JsonInvalidDateTimeString, originalDateTimeValue, JsonGlobals.DateTimeStartGuardWriter, JsonGlobals.DateTimeEndGuardWriter));
            }

            string ticksvalue = dateTimeValue.Substring(6, dateTimeValue.Length - 8);
            long millisecondsSinceUnixEpoch;
            DateTimeKind dateTimeKind = DateTimeKind.Utc;
            int indexOfTimeZoneOffset = ticksvalue.IndexOf('+', 1);

            if (indexOfTimeZoneOffset == -1)
            {
                indexOfTimeZoneOffset = ticksvalue.IndexOf('-', 1);
            }

            if (indexOfTimeZoneOffset != -1)
            {
                dateTimeKind = DateTimeKind.Local;
                ticksvalue = ticksvalue.Substring(0, indexOfTimeZoneOffset);
            }

            try
            {
                millisecondsSinceUnixEpoch = long.Parse(ticksvalue, CultureInfo.InvariantCulture);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(ticksvalue, "Int64", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(ticksvalue, "Int64", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(ticksvalue, "Int64", exception);
            }

            // Convert from # milliseconds since epoch to # of 100-nanosecond units, which is what DateTime understands
            long ticks = millisecondsSinceUnixEpoch * 10000 + JsonGlobals.unixEpochTicks;

            try
            {
                DateTime dateTime = new DateTime(ticks, DateTimeKind.Utc);
                switch (dateTimeKind)
                {
                    case DateTimeKind.Local:
                        return dateTime.ToLocalTime();
                    case DateTimeKind.Unspecified:
                        return DateTime.SpecifyKind(dateTime.ToLocalTime(), DateTimeKind.Unspecified);
                    case DateTimeKind.Utc:
                    default:
                        return dateTime;
                }
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(ticksvalue, "DateTime", exception);
            }
        }

        internal override DateTime ReadElementContentAsDateTime()
        {
            return ParseJsonDate(ReadElementContentAsString(), _dateTimeFormat);
        }

        #if USE_REFEMIT
        public override bool TryReadDateTimeArray(XmlObjectSerializerReadContext context,
#else
        internal override bool TryReadDateTimeArray(XmlObjectSerializerReadContext context,
#endif
        XmlDictionaryString itemName, XmlDictionaryString itemNamespace,
            int arrayLength, out DateTime[] array)
        {
            return TryReadJsonDateTimeArray(context, itemName, itemNamespace, arrayLength, out array);
        }

        internal bool TryReadJsonDateTimeArray(XmlObjectSerializerReadContext context,
            XmlDictionaryString itemName, XmlDictionaryString itemNamespace,
            int arrayLength, out DateTime[] array)
        {
            if ((dictionaryReader == null) || (arrayLength != -1))
            {
                array = null;
                return false;
            }

            array = this.DateTimeArrayHelper.ReadArray(dictionaryReader, XmlDictionaryString.GetString(itemName), XmlDictionaryString.GetString(itemNamespace), GetArrayLengthQuota(context));
            context.IncrementItemCount(array.Length);

            return true;
        }

        private class DateTimeArrayJsonHelperWithString : ArrayHelper<string, DateTime>
        {
            private DateTimeFormat _dateTimeFormat;

            public DateTimeArrayJsonHelperWithString(DateTimeFormat dateTimeFormat)
            {
                _dateTimeFormat = dateTimeFormat;
            }

            protected override int ReadArray(XmlDictionaryReader reader, string localName, string namespaceUri, DateTime[] array, int offset, int count)
            {
                XmlJsonReader.CheckArray(array, offset, count);
                int actual = 0;
                while (actual < count && reader.IsStartElement(JsonGlobals.itemString, string.Empty))
                {
                    array[offset + actual] = JsonReaderDelegator.ParseJsonDate(reader.ReadElementContentAsString(), _dateTimeFormat);
                    actual++;
                }
                return actual;
            }

            protected override void WriteArray(XmlDictionaryWriter writer, string prefix, string localName, string namespaceUri, DateTime[] array, int offset, int count)
            {
                throw NotImplemented.ByDesign;
            }
        }

        // Overridden because base reader relies on XmlConvert.ToUInt64 for conversion to ulong
        internal override ulong ReadContentAsUnsignedLong()
        {
            string value = reader.ReadContentAsString();

            if (value == null || value.Length == 0)
            {
                throw new XmlException(XmlObjectSerializer.TryAddLineInfo(this, SR.Format(SR.XmlInvalidConversion, value, "UInt64")));
            }

            try
            {
                return ulong.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "UInt64", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "UInt64", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "UInt64", exception);
            }
        }

        // Overridden because base reader relies on XmlConvert.ToUInt64 for conversion to ulong
        internal override ulong ReadElementContentAsUnsignedLong()
        {
            if (isEndOfEmptyElement)
            {
                throw new XmlException(SR.Format(SR.XmlStartElementExpected, "EndElement"));
            }

            string value = reader.ReadElementContentAsString();

            if (value == null || value.Length == 0)
            {
                throw new XmlException(XmlObjectSerializer.TryAddLineInfo(this, SR.Format(SR.XmlInvalidConversion, value, "UInt64")));
            }

            try
            {
                return ulong.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "UInt64", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "UInt64", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value, "UInt64", exception);
            }
        }
    }
}
