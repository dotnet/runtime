// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Diagnostics.Contracts;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [ComImport]
    [Guid("4bd682dd-7554-40e9-9a9b-82654ede7e62")]
    [WindowsRuntimeImport]
    internal interface IPropertyValue
    {
        PropertyType Type
        {
            [Pure]
            get;
        }

        bool IsNumericScalar
        {
            [Pure]
            get;
        }

        [Pure]
        Byte GetUInt8();

        [Pure]
        Int16 GetInt16();

        [Pure]
        UInt16 GetUInt16();

        [Pure]
        Int32 GetInt32();

        [Pure]
        UInt32 GetUInt32();

        [Pure]
        Int64 GetInt64();

        [Pure]
        UInt64 GetUInt64();

        [Pure]
        Single GetSingle();

        [Pure]
        Double GetDouble();

        [Pure]
        char GetChar16();

        [Pure]
        Boolean GetBoolean();

        [Pure]
        String GetString();

        [Pure]
        Guid GetGuid();

        [Pure]
        DateTimeOffset GetDateTime();

        [Pure]
        TimeSpan GetTimeSpan();

        [Pure]
        Point GetPoint();

        [Pure]
        Size GetSize();

        [Pure]
        Rect GetRect();

        [Pure]
        Byte[] GetUInt8Array();

        [Pure]
        Int16[] GetInt16Array();

        [Pure]
        UInt16[] GetUInt16Array();

        [Pure]
        Int32[] GetInt32Array();

        [Pure]
        UInt32[] GetUInt32Array();

        [Pure]
        Int64[] GetInt64Array();

        [Pure]
        UInt64[] GetUInt64Array();

        [Pure]
        Single[] GetSingleArray();

        [Pure]
        Double[] GetDoubleArray();

        [Pure]
        char[] GetChar16Array();

        [Pure]
        Boolean[] GetBooleanArray();

        [Pure]
        String[] GetStringArray();

        [Pure]
        object[] GetInspectableArray();

        [Pure]
        Guid[] GetGuidArray();

        [Pure]
        DateTimeOffset[] GetDateTimeArray();

        [Pure]
        TimeSpan[] GetTimeSpanArray();

        [Pure]
        Point[] GetPointArray();

        [Pure]
        Size[] GetSizeArray();

        [Pure]
        Rect[] GetRectArray();
    }

    // Specify size directly instead of fields to avoid warnings
    [StructLayoutAttribute(LayoutKind.Sequential, Size = 8)]
    [WindowsRuntimeImport]
    internal struct Point
    {
        // float X;
        // float Y;        
    }

    // Specify size directly instead of fields to avoid warnings
    [StructLayoutAttribute(LayoutKind.Sequential, Size = 8)]
    [WindowsRuntimeImport]
    internal struct Size
    {
        // float Width;
        // float Height;   
    }

    // Specify size directly instead of fields to avoid warnings
    [StructLayoutAttribute(LayoutKind.Sequential, Size = 16)]
    [WindowsRuntimeImport]
    internal struct Rect
    {
        // float X;
        // float Y;
        // float Width;
        // float Height;        
    }
}
