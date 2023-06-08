// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using Xunit;

using StringMarshalling = System.Runtime.InteropServices.StringMarshalling;
using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.LibraryImportGenerator>;

namespace LibraryImportGenerator.UnitTests
{
    public class CompileFails
    {
        private static string ID(
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string? filePath = null)
            => TestUtils.GetFileLineName(lineNumber, filePath);

        public static IEnumerable<object[]> CodeSnippetsToCompile()
        {
            // Not LibraryImportAttribute
            yield return new object[] { ID(), CodeSnippets.UserDefinedPrefixedAttributes, Array.Empty<DiagnosticResult>() };

            // No explicit marshalling for char or string
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<char>(), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pOut")
            }};
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<string>(), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pOut")
            }};
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<char>(), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(5)
                    .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pOut")
            }};
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<string>(), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(5)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pOut")
            }};

            // No explicit marshalling for bool
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<bool>(), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pOut")
            }};

            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers<bool>(), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(5)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pOut")
            }};


            // Unsupported StringMarshalling configuration
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<char>(StringMarshalling.Utf8), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling char with 'StringMarshalling.Utf8' is not supported. Instead, manually convert the char type to the desired byte representation and pass to the source-generated P/Invoke.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("Marshalling char with 'StringMarshalling.Utf8' is not supported. Instead, manually convert the char type to the desired byte representation and pass to the source-generated P/Invoke.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling char with 'StringMarshalling.Utf8' is not supported. Instead, manually convert the char type to the desired byte representation and pass to the source-generated P/Invoke.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling char with 'StringMarshalling.Utf8' is not supported. Instead, manually convert the char type to the desired byte representation and pass to the source-generated P/Invoke.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(5)
                    .WithArguments("Marshalling char with 'StringMarshalling.Utf8' is not supported. Instead, manually convert the char type to the desired byte representation and pass to the source-generated P/Invoke.", "pOut")
            }};
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<char>(StringMarshalling.Custom), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.InvalidStringMarshallingConfiguration)
                    .WithLocation(0)
                    .WithArguments("Method", "'StringMarshallingCustomType' must be specified when 'StringMarshalling' is set to 'StringMarshalling.Custom'."),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling char with 'StringMarshalling.Custom' is not supported. To use a custom type marshaller, specify 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("Marshalling char with 'StringMarshalling.Custom' is not supported. To use a custom type marshaller, specify 'MarshalUsingAttribute'.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling char with 'StringMarshalling.Custom' is not supported. To use a custom type marshaller, specify 'MarshalUsingAttribute'.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling char with 'StringMarshalling.Custom' is not supported. To use a custom type marshaller, specify 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(5)
                    .WithArguments("Marshalling char with 'StringMarshalling.Custom' is not supported. To use a custom type marshaller, specify 'MarshalUsingAttribute'.", "pOut")
            }};
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiersWithStringMarshalling<string>(StringMarshalling.Custom), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.InvalidStringMarshallingConfiguration)
                    .WithLocation(0)
                    .WithArguments("Method", "'StringMarshallingCustomType' must be specified when 'StringMarshalling' is set to 'StringMarshalling.Custom'."),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupported)
                    .WithLocation(1)
                    .WithArguments("string", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupported)
                    .WithLocation(2)
                    .WithArguments("string", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupported)
                    .WithLocation(3)
                    .WithArguments("string", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupported)
                    .WithLocation(4)
                    .WithArguments("string", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupported)
                    .WithLocation(5)
                    .WithArguments("string", "pOut")
            }};
            yield return new object[] { ID(), CodeSnippets.CustomStringMarshallingParametersAndModifiers<char>(), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling char with 'StringMarshalling.Custom' is not supported. To use a custom type marshaller, specify 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling char with 'StringMarshalling.Custom' is not supported. To use a custom type marshaller, specify 'MarshalUsingAttribute'.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("Marshalling char with 'StringMarshalling.Custom' is not supported. To use a custom type marshaller, specify 'MarshalUsingAttribute'.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling char with 'StringMarshalling.Custom' is not supported. To use a custom type marshaller, specify 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling char with 'StringMarshalling.Custom' is not supported. To use a custom type marshaller, specify 'MarshalUsingAttribute'.", "pOut")
            }};

            // Unsupported UnmanagedType
            yield return new object[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.I1), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments("MarshalAsAttribute", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(1)
                    .WithArguments("MarshalAsAttribute", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(2)
                    .WithArguments("MarshalAsAttribute", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(3)
                    .WithArguments("MarshalAsAttribute", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(4)
                    .WithArguments("MarshalAsAttribute", "pOut")
            }};
            yield return new object[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<char>(UnmanagedType.U1), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments("MarshalAsAttribute", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(1)
                    .WithArguments("MarshalAsAttribute", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(2)
                    .WithArguments("MarshalAsAttribute", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(3)
                    .WithArguments("MarshalAsAttribute", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(4)
                    .WithArguments("MarshalAsAttribute", "pOut")
            }};
            yield return new object[] { ID(), CodeSnippets.MarshalAsParametersAndModifiers<int[]>(UnmanagedType.SafeArray), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(10)
                    .WithArguments("SafeArray", "UnmanagedType"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(11)
                    .WithArguments("SafeArray", "UnmanagedType"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(12)
                    .WithArguments("SafeArray", "UnmanagedType"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(13)
                    .WithArguments("SafeArray", "UnmanagedType"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(14)
                    .WithArguments("SafeArray", "UnmanagedType"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments("MarshalAsAttribute", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(1)
                    .WithArguments("MarshalAsAttribute", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(2)
                    .WithArguments("MarshalAsAttribute", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(3)
                    .WithArguments("MarshalAsAttribute", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(4)
                    .WithArguments("MarshalAsAttribute", "pOut")
            }};

            // Unsupported MarshalAsAttribute usage
            //  * UnmanagedType.CustomMarshaler, MarshalTypeRef, MarshalType, MarshalCookie
            yield return new object[] { ID(), CodeSnippets.MarshalAsCustomMarshalerOnTypes, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(0)
                    .WithArguments("CustomMarshaler", "UnmanagedType"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments("MarshalAsAttribute.MarshalCookie"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments("MarshalAsAttribute.MarshalTypeRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(2)
                    .WithArguments("CustomMarshaler", "UnmanagedType"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationNotSupported)
                    .WithLocation(2)
                    .WithArguments("MarshalAsAttribute.MarshalCookie"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationNotSupported)
                    .WithLocation(2)
                    .WithArguments("MarshalAsAttribute.MarshalTypeRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(4)
                    .WithArguments("CustomMarshaler", "UnmanagedType"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationNotSupported)
                    .WithLocation(4)
                    .WithArguments("MarshalAsAttribute.MarshalCookie"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationNotSupported)
                    .WithLocation(4)
                    .WithArguments("MarshalAsAttribute.MarshalType"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(6)
                    .WithArguments("CustomMarshaler", "UnmanagedType"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationNotSupported)
                    .WithLocation(6)
                    .WithArguments("MarshalAsAttribute.MarshalCookie"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationNotSupported)
                    .WithLocation(6)
                    .WithArguments("MarshalAsAttribute.MarshalType"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnConfigurationNotSupported)
                    .WithLocation(1)
                    .WithArguments("MarshalAsAttribute", "Method1"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(3)
                    .WithArguments("MarshalAsAttribute", "t"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnConfigurationNotSupported)
                    .WithLocation(5)
                    .WithArguments("MarshalAsAttribute", "Method2"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterConfigurationNotSupported)
                    .WithLocation(7)
                    .WithArguments("MarshalAsAttribute", "t")
            }};

            // Unsupported [In, Out] attributes usage
            // Blittable array
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier<int[]>("Out"), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The provided '[In]' and '[Out]' attributes on this parameter are unsupported on this parameter.", "p")
            } };

            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier<int[]>("In, Out"), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The provided '[In]' and '[Out]' attributes on this parameter are unsupported on this parameter.", "p")
            } };

            // By ref with [In, Out] attributes
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("in int", "In"), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The '[In]' and '[Out]' attributes are unsupported on parameters passed by reference. Use the 'in', 'ref', or 'out' keywords instead.", "p")
            } };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("ref int", "In"), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The '[In]' and '[Out]' attributes are unsupported on parameters passed by reference. Use the 'in', 'ref', or 'out' keywords instead.", "p")
            } };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("ref int", "In, Out"), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The '[In]' and '[Out]' attributes are unsupported on parameters passed by reference. Use the 'in', 'ref', or 'out' keywords instead.", "p")
            } };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier("out int", "Out"), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The '[In]' and '[Out]' attributes are unsupported on parameters passed by reference. Use the 'in', 'ref', or 'out' keywords instead.", "p")
            } };

            // By value non-array with [In, Out] attributes
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier<byte>("In"), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The '[In]' attribute is not supported unless the '[Out]' attribute is also used. The behavior of the '[In]' attribute without the '[Out]' attribute is the same as the default behavior.", "p")
            } };
            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier<byte>("Out"), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The provided '[In]' and '[Out]' attributes on this parameter are unsupported on this parameter.", "p")
            } };

            yield return new object[] { ID(), CodeSnippets.ByValueParameterWithModifier<byte>("In, Out"), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The provided '[In]' and '[Out]' attributes on this parameter are unsupported on this parameter.", "p")
            } };

            // LCIDConversion
            yield return new object[] { ID(), CodeSnippets.LCIDConversionAttribute, new[] {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments("LCIDConversionAttribute")
            } };

            // No size information for array marshalling from unmanaged to managed
            //   * return, out, ref
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<byte[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<sbyte[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<short[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<ushort[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<char[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<string[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling string or char without explicit marshalling information is not supported. Specify 'LibraryImportAttribute.StringMarshalling', 'LibraryImportAttribute.StringMarshallingCustomType', 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<int[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<uint[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<long[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<ulong[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<float[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<double[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<bool[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<IntPtr[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };
            yield return new object[] { ID(), CodeSnippets.BasicParametersAndModifiers<UIntPtr[]>(CodeSnippets.DisableRuntimeMarshalling), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pOut"),
            } };

            // Collection with non-integer size param
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<float>(isByRef: false), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "pRef")
            } };
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<double>(isByRef: false), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "pRef")
            } };
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<bool>(isByRef: false), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pRefSize"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "pRef")
            } };
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<float>(isByRef: true), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "pRef")
            } };
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<double>(isByRef: true), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "pRef")
            } };
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParameterWithSizeParam<bool>(isByRef: true), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("Marshalling bool without explicit marshalling information is not supported. Specify either 'MarshalUsingAttribute' or 'MarshalAsAttribute'.", "pRefSize"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "pRef")
            } };

            // Custom type marshalling with invalid members
            CustomStructMarshallingCodeSnippets customStructMarshallingCodeSnippets = new(new CodeSnippets());
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.NonStaticMarshallerEntryPoint, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupported).WithLocation(0).WithArguments("S", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported).WithLocation(10).WithArguments(""),
            } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.ManagedToNativeOnlyOutParameter, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The specified parameter needs to be marshalled from unmanaged to managed, but the marshaller type 'global::Marshaller' does not support it.", "p"),
            } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.ManagedToNativeOnlyReturnValue, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The specified parameter needs to be marshalled from unmanaged to managed, but the marshaller type 'global::Marshaller' does not support it.", "Method"),
            } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.NativeToManagedOnlyInParameter, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The specified parameter needs to be marshalled from managed to unmanaged, but the marshaller type 'global::Marshaller' does not support it.", "p"),
            } };
            yield return new object[] { ID(), customStructMarshallingCodeSnippets.Stateless.StackallocOnlyRefParameter, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The specified parameter needs to be marshalled from managed to unmanaged and unmanaged to managed, but the marshaller type 'global::Marshaller' does not support it.", "p"),
            } };

            // Abstract SafeHandle by reference
            yield return new object[] { ID(), CodeSnippets.BasicParameterWithByRefModifier("ref", "System.Runtime.InteropServices.SafeHandle"), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The specified parameter needs to be marshalled from managed to unmanaged and unmanaged to managed, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::System.Runtime.InteropServices.SafeHandle>' does not support it.", "p"),
            } };

            // SafeHandle array
            yield return new object[] { ID(), CodeSnippets.MarshalAsArrayParametersAndModifiers("Microsoft.Win32.SafeHandles.SafeFileHandle"), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The specified parameter needs to be marshalled from unmanaged to managed, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>' does not support it.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("The specified parameter needs to be marshalled from managed to unmanaged, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>' does not support it.", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(2)
                    .WithArguments("The specified parameter needs to be marshalled from managed to unmanaged, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>' does not support it.", "pIn"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("The specified parameter needs to be marshalled from managed to unmanaged and unmanaged to managed, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>' does not support it.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(5)
                    .WithArguments("The specified parameter needs to be marshalled from unmanaged to managed, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::Microsoft.Win32.SafeHandles.SafeFileHandle>' does not support it.", "pOut"),
            } };

            // SafeHandle with private constructor by ref or out
            yield return new object[] { ID(), CodeSnippets.SafeHandleWithCustomDefaultConstructorAccessibility(privateCtor: true), new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(0)
                    .WithArguments("The specified parameter needs to be marshalled from unmanaged to managed, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::MySafeHandle>' does not support it.", "Method"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("The specified parameter needs to be marshalled from managed to unmanaged and unmanaged to managed, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::MySafeHandle>' does not support it.", "pRef"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(4)
                    .WithArguments("The specified parameter needs to be marshalled from unmanaged to managed, but the marshaller type 'global::System.Runtime.InteropServices.Marshalling.SafeHandleMarshaller<global::MySafeHandle>' does not support it.", "pOut"),
            } };

            // Collection with constant and element size parameter
            yield return new object[] { ID(), CodeSnippets.MarshalUsingCollectionWithConstantAndElementCount, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments(""),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
            } };
            // Collection with null element size parameter name
            yield return new object[] { ID(), CodeSnippets.MarshalUsingCollectionWithNullElementName, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ConfigurationValueNotSupported)
                    .WithLocation(0)
                    .WithArguments("null", "CountElementName"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("Marshalling an array from unmanaged to managed requires either the 'SizeParamIndex' or 'SizeConst' fields to be set on a 'MarshalAsAttribute' or the 'ConstantElementCount' or 'CountElementName' properties to be set on a 'MarshalUsingAttribute'.", "pRef"),
            } };

            // Generic collection marshaller has different arity than collection.
            CustomCollectionMarshallingCodeSnippets customCollectionMarshallingCodeSnippets = new(new CodeSnippets());
            yield return new object[] { ID(), customCollectionMarshallingCodeSnippets.Stateless.GenericCollectionMarshallingArityMismatch, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupported)
                    .WithLocation(0)
                    .WithArguments("TestCollection<int>", "p"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported)
                    .WithLocation(10)
                    .WithArguments(""),
            } };

            yield return new object[] { ID(), CodeSnippets.MarshalAsAndMarshalUsingOnReturnValue, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments(""),
            } };
            yield return new object[] { ID(), CodeSnippets.CustomElementMarshallingDuplicateElementIndirectionDepth, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments(""),
            } };
            yield return new object[] { ID(), CodeSnippets.CustomElementMarshallingUnusedElementIndirectionDepth, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments(""),
            } };
            yield return new object[] { ID(), CodeSnippets.RecursiveCountElementNameOnReturnValue, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments(""),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "Method"),
            } };
            yield return new object[] { ID(), CodeSnippets.RecursiveCountElementNameOnParameter, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments(""),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "arr"),
            } };
            yield return new object[] { ID(), CodeSnippets.MutuallyRecursiveCountElementNameOnParameter, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments(""),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "arr"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported)
                    .WithLocation(2)
                    .WithArguments(""),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "arr2"),
            } };
            yield return new object[] { ID(), CodeSnippets.MutuallyRecursiveSizeParamIndexOnParameter, new[]
            {
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments(""),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(1)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "arr"),
                VerifyCS.Diagnostic(GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported)
                    .WithLocation(2)
                    .WithArguments(""),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                    .WithLocation(3)
                    .WithArguments("The specified collection size parameter for an collection must be an integer type. If the size information is applied to a nested collection, the size parameter must be a collection of one less level of nesting with an integral element.", "arr2"),
            } };
            yield return new object[] { ID(), CodeSnippets.RefReturn("int"), new[]
            {
                DiagnosticResult.CompilerError("CS8795")
                    .WithLocation(0),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnConfigurationNotSupported)
                    .WithLocation(0)
                    .WithArguments("ref return", "Basic.RefReturn()"),
                DiagnosticResult.CompilerError("CS8795")
                    .WithLocation(1),
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnConfigurationNotSupported)
                    .WithLocation(1)
                    .WithArguments("ref return", "Basic.RefReadonlyReturn()"),
            } };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile))]
        public async Task ValidateSnippets(string id, string source, DiagnosticResult[] diagnostics)
        {
            TestUtils.Use(id);
            // Each snippet will contain the expected diagnostic codes in their expected locations for the compile errors.
            // The test case will pass in the expected generator diagnostics.
            await VerifyCS.VerifySourceGeneratorAsync(source, diagnostics);
        }

        public static IEnumerable<object[]> CodeSnippetsToCompile_InvalidCode()
        {
            yield return new[] { ID(), CodeSnippets.RecursiveImplicitlyBlittableStruct };
            yield return new[] { ID(), CodeSnippets.MutuallyRecursiveImplicitlyBlittableStruct };
            yield return new[] { ID(), CodeSnippets.PartialPropertyName };
            yield return new[] { ID(), CodeSnippets.InvalidConstantForModuleName };
            yield return new[] { ID(), CodeSnippets.IncorrectAttributeFieldType };
        }

        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile_InvalidCode))]
        public async Task ValidateSnippets_InvalidCodeGracefulFailure(string id, string source)
        {
            TestUtils.Use(id);
            // Each snippet will contain the expected diagnostic codes in their expected locations for the compile errors.
            // We expect there to be no generator diagnostics or failures.
            await VerifyCS.VerifySourceGeneratorAsync(source);
        }

        [Fact]
        public async Task ValidateDisableRuntimeMarshallingForBlittabilityCheckFromAssemblyReference()
        {
            // Emit the referenced assembly to a stream so we reference it through a metadata reference.
            // Our check for strict blittability doesn't work correctly when using source compilation references.
            // (There are sometimes false-positives.)
            // This causes any diagnostics that depend on strict blittability being correctly calculated to
            // not show up in the IDE experience. However, since they correctly show up when doing builds,
            // either by running the Build command in the IDE or a command line build, we aren't allowing invalid code.
            // This test validates the Build-like experience. In the future, we should update this test to validate the
            // IDE-like experience once we fix that case
            // (If the IDE experience works, then the command-line experience will also work.)
            // This bug is tracked in https://github.com/dotnet/runtime/issues/84739.
            string assemblySource = $$"""
                using System.Runtime.InteropServices.Marshalling;
                {{CodeSnippets.ValidateDisableRuntimeMarshalling.NonBlittableUserDefinedTypeWithNativeType}}
                """;
            Compilation assemblyComp = await TestUtils.CreateCompilation(assemblySource);
            Assert.Empty(assemblyComp.GetDiagnostics());

            var ms = new MemoryStream();
            Assert.True(assemblyComp.Emit(ms).Success);

            string testSource = CodeSnippets.ValidateDisableRuntimeMarshalling.TypeUsage(string.Empty);

            VerifyCS.Test test = new(referenceAncillaryInterop: false)
            {
                TestCode = testSource,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(ms.ToArray()));

            // The errors should indicate the DisableRuntimeMarshalling is required.
            test.ExpectedDiagnostics.Add(
                VerifyCS.Diagnostic(GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails)
                .WithLocation(0)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "Method"));
            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(1)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "p"));
            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(2)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pIn"));
            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(3)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pRef"));
            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails)
                .WithLocation(4)
                .WithArguments("Runtime marshalling must be disabled in this project by applying the 'System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute' to the assembly to enable marshalling this type.", "pOut"));

            await test.RunAsync();
        }

        [Fact]
        public async Task ValidateRequireAllowUnsafeBlocksDiagnostic()
        {
            var test = new AllowUnsafeBlocksTest()
            {
                TestCode = CodeSnippets.TrivialClassDeclarations,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };

            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic("SYSLIB1062"));
            test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("CS0227").WithLocation(0));

            await test.RunAsync();
        }

        class AllowUnsafeBlocksTest : VerifyCS.Test
        {
            public AllowUnsafeBlocksTest()
                    :base(referenceAncillaryInterop: false)
            {
            }

            protected override CompilationOptions CreateCompilationOptions() => ((CSharpCompilationOptions)base.CreateCompilationOptions()).WithAllowUnsafe(false);
        }
    }
}
