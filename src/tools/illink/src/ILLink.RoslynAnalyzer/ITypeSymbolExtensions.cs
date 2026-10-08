// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
    internal static class ITypeSymbolExtensions
    {
        [Flags]
        private enum HierarchyFlags
        {
            IsSystemType = 0x01,
            IsSystemReflectionIReflect = 0x02,
        }

        public static bool IsTypeInterestingForDataflow(this ITypeSymbol type, bool isByRef)
        {
            if (type.SpecialType is SpecialType.System_String && !isByRef)
                return true;

            if (type is not INamedTypeSymbol namedType)
                return false;

            var flags = GetFlags(namedType);
            return IsSystemType(flags) || IsSystemReflectionIReflect(flags);
        }

        private static HierarchyFlags GetFlags(INamedTypeSymbol type)
        {
            HierarchyFlags flags = 0;
            if (type.IsTypeOf(WellKnownType.System_Reflection_IReflect))
            {
                flags |= HierarchyFlags.IsSystemReflectionIReflect;
            }

            ITypeSymbol? baseType = type;
            while (baseType != null)
            {
                if (baseType.IsTypeOf(WellKnownType.System_Type))
                    flags |= HierarchyFlags.IsSystemType;

                foreach (var iface in baseType.Interfaces)
                {
                    if (iface.IsTypeOf(WellKnownType.System_Reflection_IReflect))
                    {
                        flags |= HierarchyFlags.IsSystemReflectionIReflect;
                    }
                }

                baseType = baseType.BaseType;
            }
            return flags;
        }

        private static bool IsSystemType(HierarchyFlags flags) => (flags & HierarchyFlags.IsSystemType) != 0;

        private static bool IsSystemReflectionIReflect(HierarchyFlags flags) => (flags & HierarchyFlags.IsSystemReflectionIReflect) != 0;

        public static bool IsTypeOf(this ITypeSymbol symbol, string @namespace, string name)
        {
            return symbol.MetadataName == name && symbol.ContainingNamespace?.GetDisplayName() == @namespace;
        }

        public static bool IsTypeOf(this ITypeSymbol symbol, WellKnownType wellKnownType)
        {
            switch (symbol.SpecialType)
            {
                case SpecialType.System_String:
                    return wellKnownType == WellKnownType.System_String;
                case SpecialType.System_Nullable_T:
                    return wellKnownType == WellKnownType.System_Nullable_T;
                case SpecialType.System_Array:
                    return wellKnownType == WellKnownType.System_Array;
                case SpecialType.System_Object:
                    return wellKnownType == WellKnownType.System_Object;
            }

            if ((uint)wellKnownType >= (uint)WellKnownType.NextAvailable)
            {
                return false;
            }

            (string @namespace, string name) = wellKnownType.GetNamespaceAndName();
            return symbol.IsTypeOf(@namespace, name);
        }
    }
}
