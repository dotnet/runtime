// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public enum HandleKind : byte
    {
        ModuleDefinition = (byte)HandleType.Module,
        TypeReference = (byte)HandleType.TypeRef,
        TypeDefinition = (byte)HandleType.TypeDef,
        FieldDefinition = (byte)HandleType.FieldDef,
        MethodDefinition = (byte)HandleType.MethodDef,
        Parameter = (byte)HandleType.ParamDef,
        InterfaceImplementation = (byte)HandleType.InterfaceImpl,
        MemberReference = (byte)HandleType.MemberRef,
        Constant = (byte)HandleType.Constant,
        CustomAttribute = (byte)HandleType.CustomAttribute,
        DeclarativeSecurityAttribute = (byte)HandleType.DeclSecurity,
        StandaloneSignature = (byte)HandleType.Signature,
        EventDefinition = (byte)HandleType.Event,
        PropertyDefinition = (byte)HandleType.Property,
        MethodImplementation = (byte)HandleType.MethodImpl,
        ModuleReference = (byte)HandleType.ModuleRef,
        TypeSpecification = (byte)HandleType.TypeSpec,
        AssemblyDefinition = (byte)HandleType.Assembly,
        AssemblyFile = (byte)HandleType.File,
        AssemblyReference = (byte)HandleType.AssemblyRef,
        ExportedType = (byte)HandleType.ExportedType,
        GenericParameter = (byte)HandleType.GenericParam,
        MethodSpecification = (byte)HandleType.MethodSpec,
        GenericParameterConstraint = (byte)HandleType.GenericParamConstraint,
        ManifestResource = (byte)HandleType.ManifestResource,

        NamespaceDefinition = (byte)HandleType.Namespace,
        UserString = (byte)HandleType.UserString,
        String = (byte)HandleType.String,
        Blob = (byte)HandleType.Blob,
        Guid = (byte)HandleType.Guid,

        // note that the highest bit is reserved for virtual bit on Handle
    }

    internal static class HandleKindExtensions
    {
        internal static bool IsHeapHandle(this HandleKind kind)
        {
            return kind >= HandleKind.NamespaceDefinition;
        }
    }
}