// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

namespace Microsoft.Interop
{
    /// <summary>
    /// Provides an implementation of <see cref="IMarshallingGenerator.SupportsByValueMarshalKind(ByValueContentsMarshalKind, TypePositionInfo, StubCodeContext, out GeneratorDiagnostic?)"/> through <see cref="GetSupport(ByValueContentsMarshalKind, TypePositionInfo, StubCodeContext, out GeneratorDiagnostic?)"/>
    /// </summary>
    public record ByValueMarshalKindSupportDescriptor(
        ByValueMarshalKindSupport InSupport, string? InSupportDetails,
        ByValueMarshalKindSupport OutSupport, string? OutSupportDetails,
        ByValueMarshalKindSupport InOutSupport, string? InOutSupportDetails)
    {
        /// <summary>
        /// A default <see cref="ByValueMarshalKindSupportDescriptor"/> for by value parameters. [In] is allowed, but unnecessary. Out is not allowed.
        /// </summary>
        public static readonly ByValueMarshalKindSupportDescriptor Default = new ByValueMarshalKindSupportDescriptor(
            InSupport: ByValueMarshalKindSupport.Unnecessary, InSupportDetails: SR.InAttributeOnlyIsDefault,
            OutSupport: ByValueMarshalKindSupport.NotSupported, OutSupportDetails: SR.OutAttributeNotSupportedOnByValueParameters,
            InOutSupport: ByValueMarshalKindSupport.NotSupported, InOutSupportDetails: SR.OutAttributeNotSupportedOnByValueParameters);

        /// <summary>
        /// A default <see cref="ByValueMarshalKindSupportDescriptor"/> for by value array parameters. [In] is allowed, but unnecessary. Out is allowed.
        /// </summary>
        public static readonly ByValueMarshalKindSupportDescriptor ArrayParameter = new ByValueMarshalKindSupportDescriptor(
            InSupport: ByValueMarshalKindSupport.Unnecessary, InSupportDetails: SR.InAttributeOnlyIsDefault,
            OutSupport: ByValueMarshalKindSupport.Supported, OutSupportDetails: null,
            InOutSupport: ByValueMarshalKindSupport.Supported, InOutSupportDetails: null);

        /// <summary>
        /// A default <see cref="ByValueMarshalKindSupportDescriptor"/> for pinned parameters. [In] is allowed, but unnecessary. Out is allowed.
        /// </summary>
        public static readonly ByValueMarshalKindSupportDescriptor PinnedParameter = new ByValueMarshalKindSupportDescriptor(
            InSupport: ByValueMarshalKindSupport.Unnecessary, InSupportDetails: SR.InAttributeOnlyIsDefault,
            OutSupport: ByValueMarshalKindSupport.Supported, OutSupportDetails: null,
            InOutSupport: ByValueMarshalKindSupport.Supported, InOutSupportDetails: null);

        /// <summary>
        /// Returns the support for the ByValueContentsMarshalKind, and if it is not <see cref="ByValueMarshalKindSupport.Supported"/>, diagnostic is not null
        /// </summary>
        public ByValueMarshalKindSupport GetSupport(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
        {
            if (info.IsByRef && marshalKind != ByValueContentsMarshalKind.Default)
            {
                diagnostic = new GeneratorDiagnostic.NotSupported(info, context)
                {
                    NotSupportedDetails = SR.InOutAttributeByRefNotSupported
                };
                return ByValueMarshalKindSupport.NotSupported;
            }
            switch (marshalKind)
            {
                case ByValueContentsMarshalKind.Default:
                    diagnostic = null;
                    return ByValueMarshalKindSupport.Supported;
                case ByValueContentsMarshalKind.Out:
                    diagnostic = OutSupport switch
                    {
                        ByValueMarshalKindSupport.Supported => null,
                        ByValueMarshalKindSupport.Unnecessary
                            => new GeneratorDiagnostic.UnnecessaryData(
                                   info,
                                   context,
                                   ImmutableArray.Create(info.ByValueMarshalAttributeLocations.OutLocation))
                            {
                                UnnecessaryDataName = SR.InOutAttributes,
                                UnnecessaryDataDetails = OutSupportDetails
                            },
                        ByValueMarshalKindSupport.NotSupported
                            => new GeneratorDiagnostic.NotSupported(
                                info,
                                context)
                            { NotSupportedDetails = OutSupportDetails },
                        _ => throw new UnreachableException($"Unexpected {nameof(ByValueMarshalKindSupport)} Variant: {InOutSupport}")
                    };
                    return OutSupport;
                case ByValueContentsMarshalKind.In:
                    diagnostic = InSupport switch
                    {
                        ByValueMarshalKindSupport.Supported => null,
                        ByValueMarshalKindSupport.Unnecessary
                            => new GeneratorDiagnostic.UnnecessaryData(
                                   info,
                                   context,
                                   ImmutableArray.Create(info.ByValueMarshalAttributeLocations.InLocation))
                            {
                                UnnecessaryDataName = SR.InOutAttributes,
                                UnnecessaryDataDetails = InSupportDetails
                            },
                        ByValueMarshalKindSupport.NotSupported
                            => new GeneratorDiagnostic.NotSupported(
                                info,
                                context)
                            { NotSupportedDetails = InSupportDetails },
                        _ => throw new UnreachableException($"Unexpected {nameof(ByValueMarshalKindSupport)} Variant: {InOutSupport}")
                    };
                    return InSupport;
                case ByValueContentsMarshalKind.InOut:
                    diagnostic = InOutSupport switch
                    {
                        ByValueMarshalKindSupport.Supported => null,
                        ByValueMarshalKindSupport.Unnecessary
                            => new GeneratorDiagnostic.UnnecessaryData(
                                   info,
                                   context,
                                   ImmutableArray.Create(
                                       info.ByValueMarshalAttributeLocations.InLocation,
                                       info.ByValueMarshalAttributeLocations.OutLocation))
                            {
                                UnnecessaryDataName = SR.InOutAttributes,
                                UnnecessaryDataDetails = InOutSupportDetails
                            },
                        ByValueMarshalKindSupport.NotSupported
                            => new GeneratorDiagnostic.NotSupported(
                                info,
                                context)
                            { NotSupportedDetails = InOutSupportDetails },
                        _ => throw new UnreachableException($"Unexpected {nameof(ByValueMarshalKindSupport)} Variant: {InOutSupport}")
                    };
                    return InOutSupport;
                default:
                    throw new UnreachableException($"Unexpected {nameof(ByValueContentsMarshalKind)} variant: {marshalKind}");
            }
        }
    }
}
