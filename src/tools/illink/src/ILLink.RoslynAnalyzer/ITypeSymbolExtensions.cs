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
