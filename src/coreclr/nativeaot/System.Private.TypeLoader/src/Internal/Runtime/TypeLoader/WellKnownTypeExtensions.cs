// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    public static class WellKnownTypeExtensions
    {
        public static RuntimeTypeHandle GetRuntimeTypeHandle(this WellKnownType wkt)
        {
            switch (wkt)
            {
                case WellKnownType.Void:
                    return typeof(void).TypeHandle;
                case WellKnownType.Boolean:
                    return typeof(bool).TypeHandle;
                case WellKnownType.Char:
                    return typeof(char).TypeHandle;
                case WellKnownType.SByte:
                    return typeof(sbyte).TypeHandle;
                case WellKnownType.Byte:
                    return typeof(byte).TypeHandle;
                case WellKnownType.Int16:
                    return typeof(short).TypeHandle;
                case WellKnownType.UInt16:
                    return typeof(ushort).TypeHandle;
                case WellKnownType.Int32:
                    return typeof(int).TypeHandle;
                case WellKnownType.UInt32:
                    return typeof(uint).TypeHandle;
                case WellKnownType.Int64:
                    return typeof(long).TypeHandle;
                case WellKnownType.UInt64:
                    return typeof(ulong).TypeHandle;
                case WellKnownType.IntPtr:
                    return typeof(IntPtr).TypeHandle;
                case WellKnownType.UIntPtr:
                    return typeof(UIntPtr).TypeHandle;
                case WellKnownType.Single:
                    return typeof(float).TypeHandle;
                case WellKnownType.Double:
                    return typeof(double).TypeHandle;
                case WellKnownType.String:
                    return typeof(string).TypeHandle;
                default:
                    Debug.Assert(false);
                    return default(RuntimeTypeHandle);
            }
        }
    }
}
