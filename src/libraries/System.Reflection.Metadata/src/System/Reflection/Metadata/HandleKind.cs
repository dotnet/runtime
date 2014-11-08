// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public enum HandleKind : byte
    {
        ModuleDefinition = (byte)(TokenTypeIds.Module >> TokenTypeIds.RowIdBitCount),
        TypeReference = (byte)(TokenTypeIds.TypeRef >> TokenTypeIds.RowIdBitCount),
        TypeDefinition = (byte)(TokenTypeIds.TypeDef >> TokenTypeIds.RowIdBitCount),
        FieldDefinition = (byte)(TokenTypeIds.FieldDef >> TokenTypeIds.RowIdBitCount),
        MethodDefinition = (byte)(TokenTypeIds.MethodDef >> TokenTypeIds.RowIdBitCount),
        Parameter = (byte)(TokenTypeIds.ParamDef >> TokenTypeIds.RowIdBitCount),
        InterfaceImplementation = (byte)(TokenTypeIds.InterfaceImpl >> TokenTypeIds.RowIdBitCount),
        MemberReference = (byte)(TokenTypeIds.MemberRef >> TokenTypeIds.RowIdBitCount),
        Constant = (byte)(TokenTypeIds.Constant >> TokenTypeIds.RowIdBitCount),
        CustomAttribute = (byte)(TokenTypeIds.CustomAttribute >> TokenTypeIds.RowIdBitCount),
        DeclarativeSecurityAttribute = (byte)(TokenTypeIds.DeclSecurity >> TokenTypeIds.RowIdBitCount),
        StandaloneSignature = (byte)(TokenTypeIds.Signature >> TokenTypeIds.RowIdBitCount),
        EventDefinition = (byte)(TokenTypeIds.Event >> TokenTypeIds.RowIdBitCount),
        PropertyDefinition = (byte)(TokenTypeIds.Property >> TokenTypeIds.RowIdBitCount),
        MethodImplementation = (byte)(TokenTypeIds.MethodImpl >> TokenTypeIds.RowIdBitCount),
        ModuleReference = (byte)(TokenTypeIds.ModuleRef >> TokenTypeIds.RowIdBitCount),
        TypeSpecification = (byte)(TokenTypeIds.TypeSpec >> TokenTypeIds.RowIdBitCount),
        AssemblyDefinition = (byte)(TokenTypeIds.Assembly >> TokenTypeIds.RowIdBitCount),
        AssemblyFile = (byte)(TokenTypeIds.File >> TokenTypeIds.RowIdBitCount),
        AssemblyReference = (byte)(TokenTypeIds.AssemblyRef >> TokenTypeIds.RowIdBitCount),
        ExportedType = (byte)(TokenTypeIds.ExportedType >> TokenTypeIds.RowIdBitCount),
        GenericParameter = (byte)(TokenTypeIds.GenericParam >> TokenTypeIds.RowIdBitCount),
        MethodSpecification = (byte)(TokenTypeIds.MethodSpec >> TokenTypeIds.RowIdBitCount),
        GenericParameterConstraint = (byte)(TokenTypeIds.GenericParamConstraint >> TokenTypeIds.RowIdBitCount),
        ManifestResource = (byte)(TokenTypeIds.ManifestResource >> TokenTypeIds.RowIdBitCount),

        NamespaceDefinition = (byte)(TokenTypeIds.Namespace >> TokenTypeIds.RowIdBitCount),
        UserString = (byte)(TokenTypeIds.UserString >> TokenTypeIds.RowIdBitCount),
        String = (byte)(TokenTypeIds.String >> TokenTypeIds.RowIdBitCount),
        Blob = (byte)(TokenTypeIds.Blob >> TokenTypeIds.RowIdBitCount),
        Guid = (byte)(TokenTypeIds.Guid >> TokenTypeIds.RowIdBitCount),
    }
}