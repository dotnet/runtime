// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // Note this is a copy of the PropertyType enumeration from Windows.Foundation.winmd
    internal enum PropertyType
    {
        // WARNING: These values have to match enum Windows.Foundation.PropertyType !!!
        Empty = 0,
        UInt8 = 1,
        Int16 = 2,
        UInt16 = 3,
        Int32 = 4,
        UInt32 = 5,
        Int64 = 6,
        UInt64 = 7,
        Single = 8,
        Double = 9,
        Char16 = 10,
        Boolean = 11,
        String = 12,
        Inspectable = 13,
        DateTime = 14,
        TimeSpan = 15,
        Guid = 16,
        Point = 17,
        Size = 18,
        Rect = 19,

        Other = 20,

        UInt8Array = UInt8 + 1024,
        Int16Array = Int16 + 1024,
        UInt16Array = UInt16 + 1024,
        Int32Array = Int32 + 1024,
        UInt32Array = UInt32 + 1024,
        Int64Array = Int64 + 1024,
        UInt64Array = UInt64 + 1024,
        SingleArray = Single + 1024,
        DoubleArray = Double + 1024,
        Char16Array = Char16 + 1024,
        BooleanArray = Boolean + 1024,
        StringArray = String + 1024,
        InspectableArray = Inspectable + 1024,
        DateTimeArray = DateTime + 1024,
        TimeSpanArray = TimeSpan + 1024,
        GuidArray = Guid + 1024,
        PointArray = Point + 1024,
        SizeArray = Size + 1024,
        RectArray = Rect + 1024,
        OtherArray = Other + 1024,
    }
}
