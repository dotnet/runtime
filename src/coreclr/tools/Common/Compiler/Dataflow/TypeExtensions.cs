// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILLink.Shared.TypeSystemProxy;
using Internal.TypeSystem;

using TypeSystemWellKnownType = Internal.TypeSystem.WellKnownType;
using ILLinkSharedWellKnownType = ILLink.Shared.TypeSystemProxy.WellKnownType;

#nullable enable

namespace ILCompiler.Dataflow
{
    internal static class TypeExtensions
    {
        public static bool IsTypeOf(this TypeDesc type, string ns, string name)
        {
            return type is MetadataType mdType && mdType.Name.StringEquals(name) && mdType.Namespace.StringEquals(ns);
        }

        public static bool IsTypeOf(this TypeDesc type, string fullTypeName)
        {
            if (type is not MetadataType metadataType)
                return false;

            string metadataTypeName = metadataType.GetName();

            var name = fullTypeName.AsSpan();
            if (metadataTypeName.Length + 1 > name.Length)
                return false;

            if (!name.Slice(name.Length - metadataTypeName.Length).Equals(metadataTypeName.AsSpan(), StringComparison.Ordinal))
                return false;

            if (name[name.Length - metadataTypeName.Length - 1] != '.')
                return false;

            return name.Slice(0, name.Length - metadataTypeName.Length - 1).Equals(metadataType.GetNamespace(), StringComparison.Ordinal);
        }

        public static bool IsTypeOf(this TypeDesc type, ILLinkSharedWellKnownType wellKnownType) =>
            wellKnownType switch
            {
                ILLinkSharedWellKnownType.System_String => type.IsWellKnownType(TypeSystemWellKnownType.String),
                ILLinkSharedWellKnownType.System_Object => type.IsWellKnownType(TypeSystemWellKnownType.Object),
                ILLinkSharedWellKnownType.System_Void => type.IsWellKnownType(TypeSystemWellKnownType.Void),
                _ => wellKnownType == WellKnownTypeExtensions.GetWellKnownType((type as MetadataType)?.GetNamespace() ?? string.Empty, ((type as MetadataType)?.GetName()) ?? string.Empty)
            };

        public static bool IsDeclaredOnType(this MethodDesc method, string fullTypeName)
        {
            return method.OwningType.IsTypeOf(fullTypeName);
        }

    }
}
