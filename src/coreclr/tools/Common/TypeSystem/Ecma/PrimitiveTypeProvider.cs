// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

namespace Internal.TypeSystem.Ecma
{
    public static class PrimitiveTypeProvider
    {
        public static TypeDesc GetPrimitiveType(TypeSystemContext context, PrimitiveTypeCode typeCode)
        {
            WellKnownType wkt;

            switch (typeCode)
            {
                case PrimitiveTypeCode.Boolean:
                    wkt = WellKnownType.Boolean;
                    break;
                case PrimitiveTypeCode.Byte:
                    wkt = WellKnownType.Byte;
                    break;
                case PrimitiveTypeCode.Char:
                    wkt = WellKnownType.Char;
                    break;
                case PrimitiveTypeCode.Double:
                    wkt = WellKnownType.Double;
                    break;
                case PrimitiveTypeCode.Int16:
                    wkt = WellKnownType.Int16;
                    break;
                case PrimitiveTypeCode.Int32:
                    wkt = WellKnownType.Int32;
                    break;
                case PrimitiveTypeCode.Int64:
                    wkt = WellKnownType.Int64;
                    break;
                case PrimitiveTypeCode.IntPtr:
                    wkt = WellKnownType.IntPtr;
                    break;
                case PrimitiveTypeCode.Object:
                    wkt = WellKnownType.Object;
                    break;
                case PrimitiveTypeCode.SByte:
                    wkt = WellKnownType.SByte;
                    break;
                case PrimitiveTypeCode.Single:
                    wkt = WellKnownType.Single;
                    break;
                case PrimitiveTypeCode.String:
                    wkt = WellKnownType.String;
                    break;
                case PrimitiveTypeCode.UInt16:
                    wkt = WellKnownType.UInt16;
                    break;
                case PrimitiveTypeCode.UInt32:
                    wkt = WellKnownType.UInt32;
                    break;
                case PrimitiveTypeCode.UInt64:
                    wkt = WellKnownType.UInt64;
                    break;
                case PrimitiveTypeCode.UIntPtr:
                    wkt = WellKnownType.UIntPtr;
                    break;
                case PrimitiveTypeCode.Void:
                    wkt = WellKnownType.Void;
                    break;
                case PrimitiveTypeCode.TypedReference:
                    wkt = WellKnownType.TypedReference;
                    break;
                default:
                    throw new BadImageFormatException();
            }

            return context.GetWellKnownType(wkt);
        }
    }
}
