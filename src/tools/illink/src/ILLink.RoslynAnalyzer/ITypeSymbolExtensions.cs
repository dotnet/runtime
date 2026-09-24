// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
    internal static class ITypeSymbolExtensions
    {
        private const int MaxCachedTypeClassificationsPerAssembly = 32768;
        private static readonly ConditionalWeakTable<IAssemblySymbol, HierarchyFlagsCache> s_hierarchyCaches = new();

        [Flags]
        private enum HierarchyFlags
        {
            IsSystemType = 0x01,
            IsSystemReflectionIReflect = 0x02,
        }

        private sealed class CachedHierarchyFlags
        {
            public CachedHierarchyFlags(HierarchyFlags flags) => Flags = flags;

            public HierarchyFlags Flags { get; }
        }

        private sealed class HierarchyFlagsCache
        {
            // Weak keys avoid retaining symbols or compilations after analysis.
            private readonly ConditionalWeakTable<INamedTypeSymbol, CachedHierarchyFlags> _flagsByType = new();
            private readonly object _gate = new();
            private int _cachedTypes;

            public HierarchyFlags GetFlags(INamedTypeSymbol type)
            {
                if (_flagsByType.TryGetValue(type, out CachedHierarchyFlags? cached))
                    return cached.Flags;

                HierarchyFlags flags = ComputeFlags(type);
                lock (_gate)
                {
                    if (_flagsByType.TryGetValue(type, out cached))
                        return cached.Flags;

                    if (_cachedTypes < MaxCachedTypeClassificationsPerAssembly)
                    {
                        _flagsByType.Add(type, new CachedHierarchyFlags(flags));
                        _cachedTypes++;
                    }
                }

                return flags;
            }
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
            if (type.ContainingAssembly is not IAssemblySymbol assembly)
                return ComputeFlags(type);

            return s_hierarchyCaches.GetValue(assembly, static _ => new HierarchyFlagsCache()).GetFlags(type);
        }

        private static HierarchyFlags ComputeFlags(INamedTypeSymbol type)
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
            return symbol.ContainingNamespace?.GetDisplayName() == @namespace && symbol.MetadataName == name;
        }

        public static bool IsTypeOf(this ITypeSymbol symbol, WellKnownType wellKnownType)
        {
            return wellKnownType switch
            {
                WellKnownType.System_Type =>
                    symbol.MetadataName == "Type" && IsSystemNamespace(symbol.ContainingNamespace),
                WellKnownType.System_Reflection_IReflect =>
                    symbol.MetadataName == "IReflect" && symbol.ContainingNamespace is { Name: "Reflection" } reflection &&
                    IsSystemNamespace(reflection.ContainingNamespace),
                _ => symbol.TryGetWellKnownType() == wellKnownType
            };
        }

        private static bool IsSystemNamespace(INamespaceSymbol? @namespace) =>
            @namespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true };

        public static WellKnownType? TryGetWellKnownType(this ITypeSymbol symbol)
        {
            return symbol.SpecialType switch
            {
                SpecialType.System_String => WellKnownType.System_String,
                SpecialType.System_Nullable_T => WellKnownType.System_Nullable_T,
                SpecialType.System_Array => WellKnownType.System_Array,
                SpecialType.System_Object => WellKnownType.System_Object,
                _ => WellKnownTypeExtensions.GetWellKnownType(symbol.ContainingNamespace?.GetDisplayName() ?? "", symbol.MetadataName)
            };
        }
    }
}
