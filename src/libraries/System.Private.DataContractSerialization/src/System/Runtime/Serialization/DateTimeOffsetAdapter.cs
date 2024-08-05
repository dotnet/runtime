// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Xml;


namespace System.Runtime.Serialization
{
    [DataContract(Name = "DateTimeOffset", Namespace = "http://schemas.datacontract.org/2004/07/System")]
    internal struct DateTimeOffsetAdapter
    {
        private DateTime _utcDateTime;
        private short _offsetMinutes;

        public DateTimeOffsetAdapter(DateTime dateTime, short offsetMinutes)
        {
            _utcDateTime = dateTime;
            _offsetMinutes = offsetMinutes;
        }

        [DataMember(Name = "DateTime", IsRequired = true)]
        public DateTime UtcDateTime
        {
            get { return _utcDateTime; }
            set { _utcDateTime = value; }
        }

        [DataMember(Name = "OffsetMinutes", IsRequired = true)]
        public short OffsetMinutes
        {
            get { return _offsetMinutes; }
            set { _offsetMinutes = value; }
        }

        public static DateTimeOffset GetDateTimeOffset(DateTimeOffsetAdapter value)
        {
            try
            {
                switch (value.UtcDateTime.Kind)
                {
                    case DateTimeKind.Unspecified:
                        return new DateTimeOffset(value.UtcDateTime, new TimeSpan(0, value.OffsetMinutes, 0));

                    //DateTimeKind.Utc and DateTimeKind.Local
                    //Read in deserialized DateTime portion of the DateTimeOffsetAdapter and convert DateTimeKind to Unspecified.
                    //Apply offset information read from OffsetMinutes portion of the DateTimeOffsetAdapter.
                    //Return converted DateTimeoffset object.
                    default:
                        DateTimeOffset deserialized = new DateTimeOffset(value.UtcDateTime);
                        return deserialized.ToOffset(new TimeSpan(0, value.OffsetMinutes, 0));
                }
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "DateTimeOffset", exception);
            }
        }

        public static DateTimeOffsetAdapter GetDateTimeOffsetAdapter(DateTimeOffset value)
        {
            return new DateTimeOffsetAdapter(value.UtcDateTime, (short)value.Offset.TotalMinutes);
        }

#pragma warning disable IDE0060 // https://github.com/dotnet/runtime/issues/76012
        public string ToString(IFormatProvider provider)
        {
            return "DateTime: " + UtcDateTime + ", Offset: " + OffsetMinutes;
        }
#pragma warning restore IDE0060
    }
}
