// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class HasCustomAttributeTag
    {
        internal const int NumberOfBits = 5;
        internal const int LargeRowSize = 0x00000001 << (16 - NumberOfBits);
        internal const uint MethodDef = 0x00000000;
        internal const uint Field = 0x00000001;
        internal const uint TypeRef = 0x00000002;
        internal const uint TypeDef = 0x00000003;
        internal const uint Param = 0x00000004;
        internal const uint InterfaceImpl = 0x00000005;
        internal const uint MemberRef = 0x00000006;
        internal const uint Module = 0x00000007;
        internal const uint DeclSecurity = 0x00000008;
        internal const uint Property = 0x00000009;
        internal const uint Event = 0x0000000A;
        internal const uint StandAloneSig = 0x0000000B;
        internal const uint ModuleRef = 0x0000000C;
        internal const uint TypeSpec = 0x0000000D;
        internal const uint Assembly = 0x0000000E;
        internal const uint AssemblyRef = 0x0000000F;
        internal const uint File = 0x00000010;
        internal const uint ExportedType = 0x00000011;
        internal const uint ManifestResource = 0x00000012;
        internal const uint GenericParam = 0x00000013;
        internal const uint GenericParamConstraint = 0x00000014;
        internal const uint MethodSpec = 0x00000015;
        internal const uint TagMask = 0x0000001F;

        // Arbitrary value not equal to any of the token types in the array. This includes 0 which is TokenTypeIds.Module.
        internal const uint InvalidTokenType = uint.MaxValue;

        internal static uint[] TagToTokenTypeArray =
        {
            TokenTypeIds.MethodDef,
            TokenTypeIds.FieldDef,
            TokenTypeIds.TypeRef,
            TokenTypeIds.TypeDef,
            TokenTypeIds.ParamDef,
            TokenTypeIds.InterfaceImpl,
            TokenTypeIds.MemberRef,
            TokenTypeIds.Module,
            TokenTypeIds.DeclSecurity,
            TokenTypeIds.Property,
            TokenTypeIds.Event,
            TokenTypeIds.Signature,
            TokenTypeIds.ModuleRef,
            TokenTypeIds.TypeSpec,
            TokenTypeIds.Assembly,
            TokenTypeIds.AssemblyRef,
            TokenTypeIds.File,
            TokenTypeIds.ExportedType,
            TokenTypeIds.ManifestResource,
            TokenTypeIds.GenericParam,
            TokenTypeIds.GenericParamConstraint,
            TokenTypeIds.MethodSpec,

            InvalidTokenType,
            InvalidTokenType,
            InvalidTokenType,
            InvalidTokenType,
            InvalidTokenType,
            InvalidTokenType,
            InvalidTokenType,
            InvalidTokenType,
            InvalidTokenType,
            InvalidTokenType
        };

        internal const TableMask TablesReferenced =
          TableMask.MethodDef
          | TableMask.Field
          | TableMask.TypeRef
          | TableMask.TypeDef
          | TableMask.Param
          | TableMask.InterfaceImpl
          | TableMask.MemberRef
          | TableMask.Module
          | TableMask.DeclSecurity
          | TableMask.Property
          | TableMask.Event
          | TableMask.StandAloneSig
          | TableMask.ModuleRef
          | TableMask.TypeSpec
          | TableMask.Assembly
          | TableMask.AssemblyRef
          | TableMask.File
          | TableMask.ExportedType
          | TableMask.ManifestResource
          | TableMask.GenericParam
          | TableMask.GenericParamConstraint
          | TableMask.MethodSpec;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EntityHandle ConvertToHandle(uint hasCustomAttribute)
        {
            uint tokenType = TagToTokenTypeArray[hasCustomAttribute & TagMask];
            uint rowId = (hasCustomAttribute >> NumberOfBits);

            if (tokenType == InvalidTokenType || ((rowId & ~TokenTypeIds.RIDMask) != 0))
            {
                Handle.ThrowInvalidCodedIndex();
            }

            return new EntityHandle(tokenType | rowId);
        }

        internal static uint ConvertToTag(EntityHandle handle)
        {
            uint tokenType = handle.Type;
            uint rowId = (uint)handle.RowId;
            switch (tokenType >> TokenTypeIds.RowIdBitCount)
            {
                case TokenTypeIds.MethodDef >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | MethodDef;
                case TokenTypeIds.FieldDef >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | Field;
                case TokenTypeIds.TypeRef >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | TypeRef;
                case TokenTypeIds.TypeDef >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | TypeDef;
                case TokenTypeIds.ParamDef >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | Param;
                case TokenTypeIds.InterfaceImpl >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | InterfaceImpl;
                case TokenTypeIds.MemberRef >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | MemberRef;
                case TokenTypeIds.Module >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | Module;
                case TokenTypeIds.DeclSecurity >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | DeclSecurity;
                case TokenTypeIds.Property >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | Property;
                case TokenTypeIds.Event >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | Event;
                case TokenTypeIds.Signature >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | StandAloneSig;
                case TokenTypeIds.ModuleRef >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | ModuleRef;
                case TokenTypeIds.TypeSpec >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | TypeSpec;
                case TokenTypeIds.Assembly >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | Assembly;
                case TokenTypeIds.AssemblyRef >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | AssemblyRef;
                case TokenTypeIds.File >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | File;
                case TokenTypeIds.ExportedType >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | ExportedType;
                case TokenTypeIds.ManifestResource >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | ManifestResource;
                case TokenTypeIds.GenericParam >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | GenericParam;
                case TokenTypeIds.GenericParamConstraint >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | GenericParamConstraint;
                case TokenTypeIds.MethodSpec >> TokenTypeIds.RowIdBitCount:
                    return rowId << NumberOfBits | MethodSpec;
            }

            return 0;
        }
    }
}