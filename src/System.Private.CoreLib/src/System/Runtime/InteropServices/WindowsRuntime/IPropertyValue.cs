// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        byte GetUInt8();

        short GetInt16();

        ushort GetUInt16();

        int GetInt32();

        uint GetUInt32();

        long GetInt64();

        ulong GetUInt64();

        float GetSingle();

        double GetDouble();

        char GetChar16();

        bool GetBoolean();

        string GetString();

        Guid GetGuid();

        DateTimeOffset GetDateTime();

        TimeSpan GetTimeSpan();

        Point GetPoint();

        Size GetSize();

        Rect GetRect();

        byte[] GetUInt8Array();

        short[] GetInt16Array();

        ushort[] GetUInt16Array();

        int[] GetInt32Array();

        uint[] GetUInt32Array();

        long[] GetInt64Array();

        ulong[] GetUInt64Array();

        float[] GetSingleArray();

        double[] GetDoubleArray();

        char[] GetChar16Array();

        bool[] GetBooleanArray();

        string[] GetStringArray();

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
