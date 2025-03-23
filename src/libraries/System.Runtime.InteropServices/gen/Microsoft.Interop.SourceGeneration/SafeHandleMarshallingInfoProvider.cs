// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Interop
{
    /// <summary>
    /// This class supports generating marshalling info for SafeHandle-derived types.
    /// </summary>
    public sealed class SafeHandleMarshallingInfoProvider(Compilation compilation) : ITypeBasedMarshallingInfoProvider
    {
        private readonly INamedTypeSymbol? _safeHandleType = compilation.GetBestTypeByMetadataName(TypeNames.System_Runtime_InteropServices_SafeHandle);
        private readonly INamedTypeSymbol? _safeHandleMarshallerType = compilation.GetBestTypeByMetadataName(TypeNames.System_Runtime_InteropServices_Marshalling_SafeHandleMarshaller_Metadata);

        public bool CanProvideMarshallingInfoForType(ITypeSymbol type)
        {
            // Check if type derives from SafHandle
            // The SafeHandle type might not be defined if we're using one of the test CoreLib implementations used for NativeAOT.
            if (_safeHandleType is null)
            {
                return false;
            }

            for (ITypeSymbol? currentType = type; currentType is not null; currentType = currentType.BaseType)
            {
                if (currentType.Equals(_safeHandleType, SymbolEqualityComparer.Default))
                {
                    return true;
                }
            }

            return false;
        }

        public MarshallingInfo GetMarshallingInfo(ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            bool hasDefaultConstructor = false;
            if (type is INamedTypeSymbol named && !named.IsAbstract && named.InstanceConstructors.Length > 0)
            {
                foreach (IMethodSymbol ctor in named.InstanceConstructors)
                {
                    if (ctor.Parameters.Length == 0)
                    {
                        hasDefaultConstructor = ctor.DeclaredAccessibility == Accessibility.Public;
                        break;
                    }
                }
            }

            // If we don't have the SafeHandleMarshaller<T> type, then we'll return NoMarshallingInfo,
            // indicating that we don't support marshalling SafeHandles with source-generated marshalling.
            if (_safeHandleMarshallerType is null)
            {
                return NoMarshallingInfo.Instance;
            }

            INamedTypeSymbol entryPointType = _safeHandleMarshallerType.Construct(type);
            if (!ManualTypeMarshallingHelper.TryGetValueMarshallersFromEntryType(
                entryPointType,
                type,
                compilation,
                out CustomTypeMarshallers? marshallers))
            {
                return NoMarshallingInfo.Instance;
            }

            // If the SafeHandle-derived type doesn't have a default constructor or is abstract,
            // we only support managed-to-unmanaged marshalling
            if (!hasDefaultConstructor || type.IsAbstract)
            {
                marshallers = marshallers.Value with
                {
                    Modes = ImmutableDictionary<MarshalMode, CustomTypeMarshallerData>.Empty
                        .Add(
                            MarshalMode.ManagedToUnmanagedIn,
                            marshallers.Value.GetModeOrDefault(MarshalMode.ManagedToUnmanagedIn))
                };
            }

            return new NativeMarshallingAttributeInfo(ManagedTypeInfo.CreateTypeInfoForTypeSymbol(entryPointType), marshallers.Value);
        }
    }
}
