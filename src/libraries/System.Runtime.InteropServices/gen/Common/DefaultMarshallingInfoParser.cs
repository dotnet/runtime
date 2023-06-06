// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal static class DefaultMarshallingInfoParser
    {
        public static MarshallingInfoParser Create(StubEnvironment env, IGeneratorDiagnostics diagnostics, IMethodSymbol method, InteropAttributeCompilationData interopAttributeData, AttributeData unparsedAttributeData)
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
                    new MarshalAsAttributeParser(env.Compilation, diagnostics, defaultInfo),
                    new MarshalUsingAttributeParser(env.Compilation, diagnostics));

            return new MarshallingInfoParser(
                diagnostics,
                new MethodSignatureElementInfoProvider(env.Compilation, diagnostics, method, useSiteAttributeParsers),
                useSiteAttributeParsers,
                ImmutableArray.Create<IMarshallingInfoAttributeParser>(
                    new MarshalAsAttributeParser(env.Compilation, diagnostics, defaultInfo),
                    new MarshalUsingAttributeParser(env.Compilation, diagnostics),
                    new NativeMarshallingAttributeParser(env.Compilation, diagnostics),
                    new ComInterfaceMarshallingInfoProvider(env.Compilation)),
                ImmutableArray.Create<ITypeBasedMarshallingInfoProvider>(
                    new SafeHandleMarshallingInfoProvider(env.Compilation, method.ContainingType),
                    new ArrayMarshallingInfoProvider(env.Compilation),
                    new CharMarshallingInfoProvider(defaultInfo),
                    new StringMarshallingInfoProvider(env.Compilation, diagnostics, unparsedAttributeData, defaultInfo),
                    new BooleanMarshallingInfoProvider(),
                    new BlittableTypeMarshallingInfoProvider(env.Compilation)));
        }
    }
}
