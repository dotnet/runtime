// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using ILLink.Shared.TypeSystemProxy;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using TypeSystemWellKnownType = Internal.TypeSystem.WellKnownType;
using ILLinkSharedWellKnownType = ILLink.Shared.TypeSystemProxy.WellKnownType;

#nullable enable

namespace ILCompiler.Dataflow
{
    static class TypeExtensions
    {
        public static bool IsTypeOf(this TypeDesc type, string ns, string name)
        {
            return type is MetadataType mdType && mdType.Name == name && mdType.Namespace == ns;
        }

        public static bool IsTypeOf(this TypeDesc type, string fullTypeName)
        {
            if (type is not MetadataType metadataType)
                return false;

            var name = fullTypeName.AsSpan();
            if (metadataType.Name.Length + 1 > name.Length)
                return false;

            if (!name.Slice(name.Length - metadataType.Name.Length).Equals(metadataType.Name.AsSpan(), StringComparison.Ordinal))
                return false;

            if (name[name.Length - metadataType.Name.Length - 1] != '.')
                return false;

            return name.Slice(0, name.Length - metadataType.Name.Length - 1).Equals(metadataType.Namespace, StringComparison.Ordinal);
        }

        public static bool IsTypeOf(this TypeDesc type, ILLinkSharedWellKnownType wellKnownType) =>
            wellKnownType switch
            {
                ILLinkSharedWellKnownType.System_String => type.IsWellKnownType(TypeSystemWellKnownType.String),
                ILLinkSharedWellKnownType.System_Object => type.IsWellKnownType(TypeSystemWellKnownType.Object),
                ILLinkSharedWellKnownType.System_Void => type.IsWellKnownType(TypeSystemWellKnownType.Void),
                _ => wellKnownType == WellKnownTypeExtensions.GetWellKnownType((type as MetadataType)?.Namespace ?? string.Empty, ((type as MetadataType)?.Name) ?? string.Empty)
            };

        public static bool IsDeclaredOnType(this MethodDesc method, string fullTypeName)
        {
            return method.OwningType.IsTypeOf(fullTypeName);
        }

        public static bool HasParameterOfType(this MethodDesc method, int index, string fullTypeName)
        {
            return index < method.Signature.Length && method.Signature[index].IsTypeOf(fullTypeName);
        }
    }
}
