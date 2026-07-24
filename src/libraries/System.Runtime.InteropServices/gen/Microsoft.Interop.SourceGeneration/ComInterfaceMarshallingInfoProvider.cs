// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// This class supports generating marshalling info for types with the <c>System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute</c> attribute.
    /// </summary>
    public class ComInterfaceMarshallingInfoProvider : IMarshallingInfoAttributeParser
    {
        private readonly Compilation _compilation;

        public ComInterfaceMarshallingInfoProvider(Compilation compilation)
        {
            _compilation = compilation;
        }

        public bool CanParseAttributeType(INamedTypeSymbol attributeType) => attributeType.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute;

        public MarshallingInfo? ParseAttribute(AttributeData attributeData, ITypeSymbol type, int indirectionDepth, UseSiteAttributeProvider useSiteAttributes, GetMarshallingInfoCallback marshallingInfoCallback)
        {
            return CreateComInterfaceMarshallingInfo(_compilation, type);
        }

        public static MarshallingInfo CreateComInterfaceMarshallingInfo(
            Compilation compilation,
            ITypeSymbol interfaceType,
            TypePositionInfo? iidParameterIndexInfo = null)
        {
            INamedTypeSymbol? comInterfaceMarshaller = compilation.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_Marshalling_ComInterfaceMarshaller_Metadata);
            if (comInterfaceMarshaller is null)
                return NoMarshallingInfo.Instance;

            comInterfaceMarshaller = comInterfaceMarshaller.Construct(interfaceType);

            if (ManualTypeMarshallingHelper.HasEntryPointMarshallerAttribute(comInterfaceMarshaller))
            {
                if (ManualTypeMarshallingHelper.TryGetValueMarshallersFromEntryType(comInterfaceMarshaller, interfaceType, compilation, out CustomTypeMarshallers? marshallers))
                {
                    // Filter the available marshallers based on the ComInterfaceOptions declared on the interface.
                    // A [GeneratedComInterface] that only specifies ManagedObjectWrapper (CCW) supports data flow
                    // in the managed-to-unmanaged direction, and one that only specifies ComObjectWrapper (RCW)
                    // supports the unmanaged-to-managed direction. Restricting the CustomTypeMarshallers dictionary
                    // to only the modes that match the supported direction lets the shared resolver machinery
                    // (AttributedMarshallingModelGeneratorResolver) surface a build-time diagnostic when a caller
                    // marshals in an unsupported direction.
                    CustomTypeMarshallers filteredMarshallers = FilterMarshallersByComInterfaceOptions(marshallers.Value, interfaceType);

                    if (iidParameterIndexInfo is not null)
                    {
                        return new IidParameterIndexNativeMarshallingInfo(
                            EntryPointType: ManagedTypeInfo.CreateTypeInfoForTypeSymbol(comInterfaceMarshaller),
                            Marshallers: filteredMarshallers,
                            IidParameterIndexInfo: iidParameterIndexInfo);
                    }

                    return new NativeMarshallingAttributeInfo(
                        EntryPointType: ManagedTypeInfo.CreateTypeInfoForTypeSymbol(comInterfaceMarshaller),
                        Marshallers: filteredMarshallers);
                }
            }

            return NoMarshallingInfo.Instance;
        }

        private static CustomTypeMarshallers FilterMarshallersByComInterfaceOptions(CustomTypeMarshallers marshallers, ITypeSymbol interfaceType)
        {
            GetComInterfaceWrapperSupport(interfaceType, out bool supportsManagedObjectWrapper, out bool supportsComObjectWrapper);

            // Fast path: both wrappers are generated -> no directional restrictions.
            if (supportsManagedObjectWrapper && supportsComObjectWrapper)
            {
                return marshallers;
            }

            // ComInterfaceMarshaller<T> declares [CustomMarshaller(..., MarshalMode.Default, ...)], so
            // TryGetValueMarshallersFromEntryType produces a single Default entry. Expand it into
            // explicit per-direction entries only for the directions the interface actually supports,
            // and drop the Default fallback so ValidateCustomNativeTypeMarshallingSupported can report
            // the unsupported direction.
            if (!marshallers.Modes.TryGetValue(MarshalMode.Default, out CustomTypeMarshallerData defaultData))
            {
                return marshallers;
            }

            ImmutableDictionary<MarshalMode, CustomTypeMarshallerData>.Builder builder =
                ImmutableDictionary.CreateBuilder<MarshalMode, CustomTypeMarshallerData>();

            // Preserve any explicitly-defined non-Default entries.
            foreach (KeyValuePair<MarshalMode, CustomTypeMarshallerData> kvp in marshallers.Modes)
            {
                if (kvp.Key != MarshalMode.Default)
                {
                    builder[kvp.Key] = kvp.Value;
                }
            }

            if (supportsManagedObjectWrapper)
            {
                // CCW: data flowing managed -> unmanaged is supported.
                builder[MarshalMode.ManagedToUnmanagedIn] = defaultData;
                builder[MarshalMode.UnmanagedToManagedOut] = defaultData;
                builder[MarshalMode.ElementIn] = defaultData;
            }

            if (supportsComObjectWrapper)
            {
                // RCW: data flowing unmanaged -> managed is supported.
                builder[MarshalMode.ManagedToUnmanagedOut] = defaultData;
                builder[MarshalMode.UnmanagedToManagedIn] = defaultData;
                builder[MarshalMode.ElementOut] = defaultData;
            }

            return new CustomTypeMarshallers(builder.ToImmutable());
        }

        private static void GetComInterfaceWrapperSupport(ITypeSymbol interfaceType, out bool supportsManagedObjectWrapper, out bool supportsComObjectWrapper)
        {
            // Default when the attribute is absent, Options is not set, or Options is None (0)
            // matches GeneratedComInterfaceCompilationData.Options: both wrappers are generated.
            supportsManagedObjectWrapper = true;
            supportsComObjectWrapper = true;

            foreach (AttributeData attr in interfaceType.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() != TypeNames.GeneratedComInterfaceAttribute)
                {
                    continue;
                }

                foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "Options" && namedArg.Value.Value is int rawOptions && rawOptions != 0)
                    {
                        ComInterfaceOptions options = (ComInterfaceOptions)rawOptions;
                        supportsManagedObjectWrapper = options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper);
                        supportsComObjectWrapper = options.HasFlag(ComInterfaceOptions.ComObjectWrapper);
                    }
                }

                break;
            }
        }
    }
}
