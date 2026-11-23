// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization.Json
{
    internal sealed class JsonReaderDelegator : XmlReaderDelegator
    {
        private readonly DateTimeFormat? _dateTimeFormat;
        private DateTimeArrayJsonHelperWithString? _dateTimeArrayHelper;

        public JsonReaderDelegator(XmlReader reader)
            : base(reader)
        {
        }

        public JsonReaderDelegator(XmlReader reader, DateTimeFormat? dateTimeFormat)
            : this(reader)
        {
            _dateTimeFormat = dateTimeFormat;
        }

        internal XmlDictionaryReaderQuotas? ReaderQuotas => dictionaryReader?.Quotas;

        internal override async ValueTask<object> ReadReferencePrimitiveContentAsync<TPrimitiveKind>(CancellationToken cancellationToken)
        {
            if (typeof(TPrimitiveKind) == typeof(ReflectionPlan.QualifiedValue))
            {
                return ParseQualifiedName(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
            }

            return await base.ReadReferencePrimitiveContentAsync<TPrimitiveKind>(cancellationToken).ConfigureAwait(false);
        }

        internal override ValueTask<T> ReadPrimitiveContentAsync<T>(CancellationToken cancellationToken)
        {
            return typeof(T) == typeof(char) || typeof(T) == typeof(DateTime) || typeof(T) == typeof(ulong)
                ? ReadJsonPrimitiveContentAsync<T>(cancellationToken)
                : base.ReadPrimitiveContentAsync<T>(cancellationToken);
        }

        private async ValueTask<T> ReadJsonPrimitiveContentAsync<T>(CancellationToken cancellationToken) where T : struct
        {
            if (typeof(T) == typeof(char))
            {
                return (T)(object)XmlConvert.ToChar(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
            }
            if (typeof(T) == typeof(DateTime))
            {
                return (T)(object)ParseJsonDate(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false), _dateTimeFormat);
            }
            if (typeof(T) == typeof(ulong))
            {
                string value = await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    return (T)(object)ulong.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                }
                catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
                {
                    throw XmlExceptionHelper.CreateConversionException(value, "UInt64", exception);
                }
            }

            return await base.ReadPrimitiveContentAsync<T>(cancellationToken).ConfigureAwait(false);
        }

        internal override async ValueTask<object> ReadContentAsAnyTypeAsync(Type valueType, CancellationToken cancellationToken)
        {
            if (valueType == typeof(char))
            {
                return XmlConvert.ToChar(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
            }
            if (valueType == typeof(DateTime))
            {
                return ParseJsonDate(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false), _dateTimeFormat);
            }
            if (valueType == typeof(XmlQualifiedName))
            {
                return ParseQualifiedName(await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false));
            }
            if (valueType == typeof(ulong))
            {
                string value = await ReadContentAsStringAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    return ulong.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                }
                catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
                {
                    throw XmlExceptionHelper.CreateConversionException(value, "UInt64", exception);
                }
            }

            return await base.ReadContentAsAnyTypeAsync(valueType, cancellationToken).ConfigureAwait(false);
        }

        internal override async ValueTask<byte[]> ReadContentAsBase64Async(CancellationToken cancellationToken)
        {
            int quota = dictionaryReader?.Quotas.MaxArrayLength ?? XmlDictionaryReaderQuotas.Max.MaxArrayLength;
            byte[] bytes = new byte[Math.Min(32, quota)];
            int count = 0;
            while (await IsStartElementAsync(JsonGlobals.itemDictionaryString, XmlDictionaryString.Empty, cancellationToken).ConfigureAwait(false))
            {
                if (count == quota)
                {
                    XmlExceptionHelper.ThrowMaxArrayLengthExceeded(dictionaryReader ?? XmlDictionaryReader.CreateDictionaryReader(reader), quota);
                }
                if (count == bytes.Length)
                {
                    Array.Resize(ref bytes, Math.Min(quota, bytes.Length <= quota / 2 ? Math.Max(1, bytes.Length * 2) : quota));
                }
                bytes[count++] = await ReadPrimitiveElementAsync<byte>(cancellationToken).ConfigureAwait(false);
            }

            return XmlObjectSerializerReadContext.TrimArraySize(bytes, count);
        }

        private DateTimeArrayJsonHelperWithString DateTimeArrayHelper =>
            _dateTimeArrayHelper ??= new DateTimeArrayJsonHelperWithString(_dateTimeFormat);

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

        internal static DateTime ParseJsonDate(string originalDateTimeValue, DateTimeFormat? dateTimeFormat)
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

            ReadOnlySpan<char> ticksvalue = dateTimeValue.AsSpan(6, dateTimeValue.Length - 8);
            long millisecondsSinceUnixEpoch;
            DateTimeKind dateTimeKind = DateTimeKind.Utc;
            int indexOfTimeZoneOffset = ticksvalue.Slice(1).IndexOf('+');

            if (indexOfTimeZoneOffset == -1)
            {
                indexOfTimeZoneOffset = ticksvalue.Slice(1).IndexOf('-');
            }

            if (indexOfTimeZoneOffset >= 0)
            {
                dateTimeKind = DateTimeKind.Local;
                ticksvalue = ticksvalue.Slice(0, indexOfTimeZoneOffset + 1); // +1 for Slice above
            }

            try
            {
                millisecondsSinceUnixEpoch = long.Parse(ticksvalue, CultureInfo.InvariantCulture);
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
            {
                throw XmlExceptionHelper.CreateConversionException(ticksvalue.ToString(), "Int64", exception);
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
                throw XmlExceptionHelper.CreateConversionException(ticksvalue.ToString(), "DateTime", exception);
            }
        }

        internal override DateTime ReadElementContentAsDateTime()
        {
            return ParseJsonDate(ReadElementContentAsString(), _dateTimeFormat);
        }

        internal override bool TryReadDateTimeArray(XmlObjectSerializerReadContext context,
        XmlDictionaryString itemName, XmlDictionaryString itemNamespace,
            int arrayLength, [NotNullWhen(true)] out DateTime[]? array)
        {
            return TryReadJsonDateTimeArray(context, itemName, itemNamespace, arrayLength, out array);
        }

        internal bool TryReadJsonDateTimeArray(XmlObjectSerializerReadContext context,
            XmlDictionaryString itemName, XmlDictionaryString itemNamespace,
            int arrayLength, [NotNullWhen(true)] out DateTime[]? array)
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

        private sealed class DateTimeArrayJsonHelperWithString : ArrayHelper<string, DateTime>
        {
            private readonly DateTimeFormat? _dateTimeFormat;

            public DateTimeArrayJsonHelperWithString(DateTimeFormat? dateTimeFormat)
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

            if (string.IsNullOrEmpty(value))
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

            if (string.IsNullOrEmpty(value))
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
