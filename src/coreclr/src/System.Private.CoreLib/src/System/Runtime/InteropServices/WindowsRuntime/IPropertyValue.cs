// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [ComImport]
    [Guid("4bd682dd-7554-40e9-9a9b-82654ede7e62")]
    [WindowsRuntimeImport]
    internal interface IPropertyValue
    {
        PropertyType Type
        {
            get;
        }

        bool IsNumericScalar
        {
            get;
        }

        Byte GetUInt8();

        Int16 GetInt16();

        UInt16 GetUInt16();

        Int32 GetInt32();

        UInt32 GetUInt32();

        Int64 GetInt64();

        UInt64 GetUInt64();

        Single GetSingle();

        Double GetDouble();

        char GetChar16();

        Boolean GetBoolean();

        String GetString();

        Guid GetGuid();

        DateTimeOffset GetDateTime();

        TimeSpan GetTimeSpan();

        Point GetPoint();

        Size GetSize();

        Rect GetRect();

        Byte[] GetUInt8Array();

        Int16[] GetInt16Array();

        UInt16[] GetUInt16Array();

        Int32[] GetInt32Array();

        UInt32[] GetUInt32Array();

        Int64[] GetInt64Array();

        UInt64[] GetUInt64Array();

        Single[] GetSingleArray();

        Double[] GetDoubleArray();

        char[] GetChar16Array();

        Boolean[] GetBooleanArray();

        String[] GetStringArray();

        object[] GetInspectableArray();

        Guid[] GetGuidArray();

        DateTimeOffset[] GetDateTimeArray();

        TimeSpan[] GetTimeSpanArray();

        Point[] GetPointArray();

        Size[] GetSizeArray();

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
