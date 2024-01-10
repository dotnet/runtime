// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Factory class for creating a <see cref="MarshallingInfoParser"/> instance that can parse marshalling information supported by .NET interop source generators.
    /// </summary>
    public static class DefaultMarshallingInfoParser
    {
        /// <summary>
        /// Creates a <see cref="MarshallingInfoParser"/> instance for parsing marshalling information from attributes
        /// with support for the marshalling support that is common to all SDK-provided source generators.
        /// </summary>
        /// <remarks>
        /// Includes support for:
        /// <list type="bullet">
        /// <item>System.Runtime.InteropServices.MarshalAsAttribute</item>
        /// <item>System.Runtime.InteropServices.Marshalling.MarshalUsingAttribute</item>
        /// <item>System.Runtime.InteropServices.Marshallling.NativeMarshallingAttribute-attributed types</item>
        /// <item>System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute-attributed types</item>
        /// <item>System.Runtime.InteropServices.SafeHandle-derived types</item>
        /// <item>Arrays</item>
        /// <item>System.String</item>
        /// <item>Primitive types</item>
        /// <item>User-defined unmanaged types.</item>
        /// </list>
        /// </remarks>
        public static MarshallingInfoParser Create(StubEnvironment env, GeneratorDiagnosticsBag diagnostics, IMethodSymbol method, InteropAttributeCompilationData interopAttributeData, AttributeData unparsedAttributeData)
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
                    new MarshalAsAttributeParser(diagnostics, defaultInfo),
                    new MarshalUsingAttributeParser(env.Compilation, diagnostics));

            return new MarshallingInfoParser(
                diagnostics,
                new MethodSignatureElementInfoProvider(env.Compilation, diagnostics, method, useSiteAttributeParsers),
                useSiteAttributeParsers,
                ImmutableArray.Create<IMarshallingInfoAttributeParser>(
                    new MarshalAsWithCustomMarshallersParser(env.Compilation, diagnostics, new MarshalAsAttributeParser(diagnostics, defaultInfo)),
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
