// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal static class DownlevelLibraryImportGeneratorHelpers
    {
        public static MarshallingInfoParser CreateMarshallingInfoParser(StubEnvironment env, GeneratorDiagnosticsBag diagnostics, IMethodSymbol method, InteropAttributeCompilationData interopAttributeData)
        {
            // Compute the current default string encoding value.
            CharEncoding defaultEncoding = CharEncoding.Undefined;
            if (interopAttributeData.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling))
            {
                defaultEncoding = interopAttributeData.StringMarshalling switch
                {
                    StringMarshalling.Utf16 => CharEncoding.Utf16,
                    StringMarshalling.Utf8 => CharEncoding.Utf8,
                    StringMarshalling.Custom => CharEncoding.Custom,
                    _ => CharEncoding.Undefined, // [Compat] Do not assume a specific value
                };
            }
            else if (interopAttributeData.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshallingCustomType))
            {
                defaultEncoding = CharEncoding.Custom;
            }

            var defaultInfo = new DefaultMarshallingInfo(defaultEncoding, interopAttributeData.StringMarshallingCustomType);

            var useSiteAttributeParsers = ImmutableArray.Create<IUseSiteAttributeParser>(
                    new MarshalAsAttributeParser(diagnostics, defaultInfo));

            return new MarshallingInfoParser(
                diagnostics,
                new MethodSignatureElementInfoProvider(env.Compilation, diagnostics, method, useSiteAttributeParsers),
                useSiteAttributeParsers,
                ImmutableArray.Create<IMarshallingInfoAttributeParser>(new MarshalAsAttributeParser(diagnostics, defaultInfo)),
                ImmutableArray.Create<ITypeBasedMarshallingInfoProvider>(
                    new CharMarshallingInfoProvider(defaultInfo),
                    new BooleanMarshallingInfoProvider(),
                    new BlittableTypeMarshallingInfoProvider(env.Compilation)));
        }

        public static IMarshallingGeneratorResolver GeneratorResolver { get; } = new ByValueContentsMarshalKindValidator(new CompositeMarshallingGeneratorResolver(
            [
                new BlittableMarshallerResolver(runtimeMarshallingDisabled: false),
                new MarshalAsMarshallingGeneratorResolver(new InteropGenerationOptions(UseMarshalType: true)),
                new NoMarshallingInfoErrorResolver(TypeNames.LibraryImportAttribute_ShortName),
                new CharMarshallingGeneratorResolver(useBlittableMarshallerForUtf16: false, TypeNames.LibraryImportAttribute_ShortName),
                new ForwarderResolver()
            ]));
    }
}
