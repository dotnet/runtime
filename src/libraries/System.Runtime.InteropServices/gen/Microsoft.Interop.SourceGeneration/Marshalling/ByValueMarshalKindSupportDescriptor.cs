// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.Interop
{
    public record struct ByValueMarshalKindSupportInfo(ByValueMarshalKindSupport Support, string? details)
    {
        public ByValueMarshalKindSupport GetSupport(TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
        {
            diagnostic = Support switch
            {
                ByValueMarshalKindSupport.Supported => null,
                ByValueMarshalKindSupport.NotRecommended =>
                    new GeneratorDiagnostic.NotRecommended(info, context)
                    {
                        Details = details
                    },
                ByValueMarshalKindSupport.Unnecessary =>
                    new GeneratorDiagnostic.UnnecessaryData(
                           info,
                           context,
                           ImmutableArray.Create(info.ByValueMarshalAttributeLocations.OutLocation))
                    {
                        UnnecessaryDataName = SR.InOutAttributes,
                        UnnecessaryDataDetails = details
                    },
                ByValueMarshalKindSupport.NotSupported =>
                    new GeneratorDiagnostic.NotSupported(info, context)
                    {
                        NotSupportedDetails = details
                    },
                _ => throw new UnreachableException()
            };
            return Support;
        }
    }

    /// <summary>
    /// Provides an implementation of <see cref="IMarshallingGenerator.SupportsByValueMarshalKind(ByValueContentsMarshalKind, TypePositionInfo, StubCodeContext, out GeneratorDiagnostic?)"/> through <see cref="GetSupport(ByValueContentsMarshalKind, TypePositionInfo, StubCodeContext, out GeneratorDiagnostic?)"/>
    /// </summary>
    public record ByValueMarshalKindSupportDescriptor(
        ByValueMarshalKindSupportInfo DefaultSupport,
        ByValueMarshalKindSupportInfo InSupport,
        ByValueMarshalKindSupportInfo OutSupport,
        ByValueMarshalKindSupportInfo InOutSupport)
    {
        /// <summary>
        /// A default <see cref="ByValueMarshalKindSupportDescriptor"/> for by value parameters. [In] is allowed, but unnecessary. Out is not allowed.
        /// </summary>
        public static readonly ByValueMarshalKindSupportDescriptor Default = new ByValueMarshalKindSupportDescriptor(
            DefaultSupport: new(ByValueMarshalKindSupport.Supported, null),
            InSupport: new(ByValueMarshalKindSupport.NotSupported, SR.InAttributeNotSupportedOnByValueParameters),
            OutSupport: new(ByValueMarshalKindSupport.NotSupported, SR.OutAttributeNotSupportedOnByValueParameters),
            InOutSupport: new(ByValueMarshalKindSupport.NotSupported, SR.InOutAttributeNotSupportedOnByValueParameters));

        /// <summary>
        /// A default <see cref="ByValueMarshalKindSupportDescriptor"/> for by value array parameters. Default is allowed, but Not Recommended. [In], [Out], and [In, Out] are allowed
        /// </summary>
        public static readonly ByValueMarshalKindSupportDescriptor ArrayParameter = new ByValueMarshalKindSupportDescriptor(
            DefaultSupport: new(ByValueMarshalKindSupport.NotRecommended, SR.PreferExplicitInOutAttributesOnArrays),
            InSupport: new(ByValueMarshalKindSupport.Supported, null),
            OutSupport: new(ByValueMarshalKindSupport.Supported, null),
            InOutSupport: new(ByValueMarshalKindSupport.Supported, null));

        /// <summary>
        /// Returns the support for the ByValueContentsMarshalKind, and if it is not <see cref="ByValueMarshalKindSupport.Supported"/>, diagnostic is not null
        /// </summary>
        public ByValueMarshalKindSupport GetSupport(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
        {
            if (info.IsByRef)
            {
                // ByRef with ByValue attributes is not allowed
                if (marshalKind != ByValueContentsMarshalKind.Default)
                {
                    diagnostic = new GeneratorDiagnostic.NotSupported(info, context)
                    {
                        NotSupportedDetails = SR.InOutAttributeByRefNotSupported
                    };
                    return ByValueMarshalKindSupport.NotSupported;
                }
                // ByRef with no ByValue attributes is supported
                diagnostic = null;
                return ByValueMarshalKindSupport.Supported;
            }
            // Return can never have In or Out attributes, so can assume valid ByValue attributes
            if (info.ManagedIndex < 0)
            {
                Debug.Assert(marshalKind is ByValueContentsMarshalKind.Default);
                diagnostic = null;
                return ByValueMarshalKindSupport.Supported;
            }

            return marshalKind switch
            {
                ByValueContentsMarshalKind.Default => DefaultSupport.GetSupport(info, context, out diagnostic),
                ByValueContentsMarshalKind.In => InSupport.GetSupport(info, context, out diagnostic),
                ByValueContentsMarshalKind.Out => OutSupport.GetSupport(info, context, out diagnostic),
                ByValueContentsMarshalKind.InOut => InOutSupport.GetSupport(info, context, out diagnostic),
                _ => throw new UnreachableException()
            };
        }
    }
}
