// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public abstract record GeneratorDiagnostic
    {
        private GeneratorDiagnostic(TypePositionInfo typePositionInfo, StubCodeContext stubCodeContext, bool isFatal)
        {
            TypePositionInfo = typePositionInfo;
            StubCodeContext = stubCodeContext;
            IsFatal = isFatal;
        }

        /// <summary>
        /// [Optional] Properties to attach to any diagnostic emitted due to this exception.
        /// </summary>
        public ImmutableDictionary<string, string> DiagnosticProperties { get; init; } = ImmutableDictionary<string, string>.Empty;
        public TypePositionInfo TypePositionInfo { get; }
        public StubCodeContext StubCodeContext { get; }
        public bool IsFatal { get; }

        public abstract DiagnosticInfo ToDiagnosticInfo(DiagnosticDescriptor descriptor, Location location, string elementName);

        public sealed record NotSupported(TypePositionInfo TypePositionInfo, StubCodeContext Context) : GeneratorDiagnostic(TypePositionInfo, Context, isFatal: true)
        {
            /// <summary>
            /// [Optional] Specific reason marshalling of the supplied type isn't supported.
            /// </summary>
            public string? NotSupportedDetails { get; init; }

            public override DiagnosticInfo ToDiagnosticInfo(DiagnosticDescriptor descriptor, Location location, string elementName)
            {
                if (NotSupportedDetails is not null)
                {
                    return DiagnosticInfo.Create(descriptor, location, DiagnosticProperties, NotSupportedDetails, elementName);
                }
                return DiagnosticInfo.Create(descriptor, location, DiagnosticProperties, TypePositionInfo.ManagedType.DiagnosticFormattedName, elementName);
            }
        }

        public sealed record UnnecessaryData(TypePositionInfo TypePositionInfo, StubCodeContext StubCodeContext, ImmutableArray<Location> UnnecessaryDataLocations) : GeneratorDiagnostic(TypePositionInfo, StubCodeContext, isFatal: false)
        {
            public required string UnnecessaryDataDetails { get; init; }

            public override DiagnosticInfo ToDiagnosticInfo(DiagnosticDescriptor descriptor, Location location, string elementName)
            {
                return DiagnosticInfo.Create(
                    descriptor,
                    location,
                    UnnecessaryDataLocations,
                    // Add "unnecessary locations" property so the IDE fades the right locations.
                    DiagnosticProperties.Add(WellKnownDiagnosticTags.Unnecessary, $"[{string.Join(",", Enumerable.Range(0, UnnecessaryDataLocations.Length))}]"),
                    UnnecessaryDataDetails,
                    elementName);
            }
        }

        /// <summary>
        /// Provides the default implementation for <see cref="IMarshallingGenerator.SupportsByValueMarshalKind(ByValueContentsMarshalKind, TypePositionInfo, StubCodeContext, out GeneratorDiagnostic?)"/>
        /// </summary>
        public static bool SupportsByValueMarshalKindDefault(ByValueContentsMarshalKind kind, TypePositionInfo info, StubCodeContext context, [NotNullWhen(false)] out GeneratorDiagnostic? diagnostic)
        {
            diagnostic = null;
            switch (kind)
            {
                case ByValueContentsMarshalKind.Default: return true;
                case ByValueContentsMarshalKind.In:
                    diagnostic = new GeneratorDiagnostic.UnnecessaryData(info, context, ImmutableArray.Create(info.ByValueMarshalAttributeLocations.InLocation))
                    { UnnecessaryDataDetails = SR.InAttributeOnlyIsDefault };
                    return false;
                default:
                    diagnostic = DefaultDiagnosticForByValueGeneratorSupport(ByValueMarshalKindSupport.NotSupported, info, context);
                    return false;
            }
        }

        public static bool ByValueParameterSupportsByValueMarshalKindByDefault(
            ByValueContentsMarshalKind marshalKind,
            TypePositionInfo info,
            StubCodeContext context,
            out GeneratorDiagnostic? diagnostic)
        {
            if (marshalKind.HasFlag(ByValueContentsMarshalKind.Out))
            {
                diagnostic = new GeneratorDiagnostic.NotSupported(info, context)
                {
                    NotSupportedDetails = SR.OutAttributeNotSupportedOnByValueParameters
                };
                return true;
            }
            return GeneratorDiagnostic.SupportsByValueMarshalKindDefault(marshalKind, info, context, out diagnostic);
        }
        /// <summary>
        /// Provides an implementation of <see cref="IMarshallingGenerator.SupportsByValueMarshalKind(ByValueContentsMarshalKind, TypePositionInfo, StubCodeContext, out GeneratorDiagnostic?)"/> through <see cref="GetSupport(ByValueContentsMarshalKind, TypePositionInfo, StubCodeContext, out GeneratorDiagnostic?)"/>
        /// </summary>
        public record ByValueMarshalKindSupportManager(
            ByValueMarshalKindSupport InSupport, string? InSupportDetails,
            ByValueMarshalKindSupport OutSupport, string? OutSupportDetails,
            ByValueMarshalKindSupport InOutSupport, string? InOutSupportDetails)
        {
            /// <summary>
            /// A default <see cref="ByValueMarshalKindSupportManager"/> for by value parameters. [In] is allowed, but unnecessary. Out is not allowed.
            /// </summary>
            public static ByValueMarshalKindSupportManager ValueTypeParameterDefault = new ByValueMarshalKindSupportManager(
                InSupport: ByValueMarshalKindSupport.Unnecessary, InSupportDetails: SR.InAttributeOnlyIsDefault,
                OutSupport: ByValueMarshalKindSupport.NotSupported, OutSupportDetails: SR.OutAttributeNotSupportedOnByValueParameters,
                InOutSupport: ByValueMarshalKindSupport.NotSupported, InOutSupportDetails: SR.OutAttributeNotSupportedOnByValueParameters);

            /// <summary>
            /// A default <see cref="ByValueMarshalKindSupportManager"/> for by reference parameters. [In] is allowed, but unnecessary. Out is allowed.
            /// </summary>
            public static ByValueMarshalKindSupportManager ReferenceTypeParameterDefault = new ByValueMarshalKindSupportManager(
                InSupport: ByValueMarshalKindSupport.Unnecessary, InSupportDetails: SR.InAttributeOnlyIsDefault,
                OutSupport: ByValueMarshalKindSupport.Supported, OutSupportDetails: null,
                InOutSupport: ByValueMarshalKindSupport.Supported, InOutSupportDetails: null);

            /// <summary>
            /// A default <see cref="ByValueMarshalKindSupportManager"/> for pinned by reference parameters. [In] is not allowed. [In, Out] is the default and unnecessary. [Out] is allowed.
            /// </summary>
            public static ByValueMarshalKindSupportManager PinnedByReferenceParameterDefault = new ByValueMarshalKindSupportManager(
                InSupport: ByValueMarshalKindSupport.NotSupported, InSupportDetails: SR.InAttributeOnlyNotSupportedOnPinnedParameters,
                OutSupport: ByValueMarshalKindSupport.Supported, OutSupportDetails: null,
                InOutSupport: ByValueMarshalKindSupport.Unnecessary, InOutSupportDetails: SR.PinnedMarshallingIsInOutByDefault);

            /// <summary>
            /// Returns the support for the ByValueContentsMarshalKind, and if it is not <see cref="ByValueMarshalKindSupport.Supported"/>, diagnostic is not null
            /// </summary>
            public ByValueMarshalKindSupport GetSupport(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
            {
                if (info.IsByRef && marshalKind != ByValueContentsMarshalKind.Default)
                {
                    diagnostic = new NotSupported(info, context)
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
                                { UnnecessaryDataDetails = OutSupportDetails },
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
                                { UnnecessaryDataDetails = InSupportDetails },
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
                                { UnnecessaryDataDetails = InOutSupportDetails },
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

        /// <summary>
        /// Gets the default warnings for supported / unsupported / unnecessary ByValueMarshalKindSupport
        /// </summary>
        public static GeneratorDiagnostic DefaultDiagnosticForByValueGeneratorSupport(ByValueMarshalKindSupport support, TypePositionInfo info, StubCodeContext context)
        {
            switch (support)
            {
                case ByValueMarshalKindSupport.Supported:
                    throw new ArgumentException("Supported ByValueMarshalKind will not have a diagnostic");
                case ByValueMarshalKindSupport.NotSupported:
                    return new GeneratorDiagnostic.NotSupported(info, context)
                    {
                        NotSupportedDetails = SR.InOutAttributeMarshalerNotSupported
                    };
                case ByValueMarshalKindSupport.Unnecessary:
                    var locations = ImmutableArray<Location>.Empty;
                    if (info.ByValueMarshalAttributeLocations.InLocation is not null)
                    {
                        locations = locations.Add(info.ByValueMarshalAttributeLocations.InLocation);
                    }
                    if (info.ByValueMarshalAttributeLocations.OutLocation is not null)
                    {
                        locations = locations.Add(info.ByValueMarshalAttributeLocations.OutLocation);
                    }
                    return new GeneratorDiagnostic.UnnecessaryData(info, context, locations)
                    {
                        UnnecessaryDataDetails = SR.InOutAttributes
                    };
                default:
                    throw new UnreachableException("Unexpected ByValueMarshalKindSupport");
            }
        }
    }
}
