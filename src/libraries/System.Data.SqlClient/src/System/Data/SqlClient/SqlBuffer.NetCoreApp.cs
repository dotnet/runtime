// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//------------------------------------------------------------------------------

using System.Diagnostics;

namespace System.Data.SqlClient
{
    internal sealed partial class SqlBuffer
    {
        internal void SetToDate(ReadOnlySpan<byte> bytes)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");

            _type = StorageType.Date;
            _value._int32 = GetDateFromByteArray(bytes);
            _isNull = false;
        }

        internal void SetToTime(ReadOnlySpan<byte> bytes, int length, byte scale)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");

            _type = StorageType.Time;
            FillInTimeInfo(ref _value._timeInfo, bytes, length, scale);
            _isNull = false;
        }

        internal void SetToDateTime2(ReadOnlySpan<byte> bytes, int length, byte scale)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");
            _type = StorageType.DateTime2;
            FillInTimeInfo(ref _value._dateTime2Info.timeInfo, bytes, length - 3, scale); // remaining 3 bytes is for date
            _value._dateTime2Info.date = GetDateFromByteArray(bytes.Slice(length - 3)); // 3 bytes for date
            _isNull = false;
        }

        internal void SetToDateTimeOffset(ReadOnlySpan<byte> bytes, int length, byte scale)
        {
            Debug.Assert(IsEmpty, "setting value a second time?");

            _type = StorageType.DateTimeOffset;
            FillInTimeInfo(ref _value._dateTimeOffsetInfo.dateTime2Info.timeInfo, bytes, length - 5, scale); // remaining 5 bytes are for date and offset
            _value._dateTimeOffsetInfo.dateTime2Info.date = GetDateFromByteArray(bytes.Slice(length - 5)); // 3 bytes for date
            _value._dateTimeOffsetInfo.offset = (short)(bytes[length - 2] + (bytes[length - 1] << 8)); // 2 bytes for offset (Int16)
            _isNull = false;
        }

        private static int GetDateFromByteArray(ReadOnlySpan<byte> buf)
        {
            return buf[0] + (buf[1] << 8) + (buf[2] << 16);
        }
    }
}
