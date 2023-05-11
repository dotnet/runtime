// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.Interop.UnitTests;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

using VerifyCS = Microsoft.Interop.UnitTests.Verifiers.CSharpSourceGeneratorVerifier<Microsoft.Interop.LibraryImportGenerator>;

namespace LibraryImportGenerator.UnitTests
{
    public class AdditionalAttributesOnStub
    {
        [Fact]
        public async Task SkipLocalsInitAdded()
        {
            string source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                [assembly:DisableRuntimeMarshalling]
                partial class C
                {
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial S Method();
                }

                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }

                struct Native
                {
                }

                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;

                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            await VerifySourceGeneratorAsync(source, "C", "Method", typeof(SkipLocalsInitAttribute).FullName, attributeAdded: true, TestTargetFramework.Net);
        }

        [Fact]
        public async Task SkipLocalsInitNotAddedOnForwardingStub()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial void Method();
                }
                """;
            await VerifySourceGeneratorAsync(source, "C", "Method", typeof(SkipLocalsInitAttribute).FullName, attributeAdded: false, TestTargetFramework.Net);
        }

        [Fact]
        public async Task GeneratedCodeAdded()
        {
            string source = """
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                [assembly:DisableRuntimeMarshalling]
                partial class C
                {
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial S Method();
                }

                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }

                struct Native
                {
                }

                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;

                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            await VerifySourceGeneratorAsync(source, "C", "Method", typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName, attributeAdded: true, TestTargetFramework.Net);
        }

        [Fact]
        public async Task GeneratedCodeNotAddedOnForwardingStub()
        {
            string source = """
                using System.Runtime.InteropServices;
                partial class C
                {
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial void Method();
                }
                """;
            await VerifySourceGeneratorAsync(source, "C", "Method", typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName, attributeAdded: false, TestTargetFramework.Net);
        }

        public static IEnumerable<object[]> GetDownlevelTargetFrameworks()
        {
            yield return new object[] { TestTargetFramework.Net, true };
            yield return new object[] { TestTargetFramework.Net6, true };
            yield return new object[] { TestTargetFramework.Core, false };
            yield return new object[] { TestTargetFramework.Standard, false };
            yield return new object[] { TestTargetFramework.Framework, false };
        }

        [Theory]
        [MemberData(nameof(GetDownlevelTargetFrameworks))]
        [OuterLoop("Uses the network for downlevel ref packs")]
        public async Task SkipLocalsInitOnDownlevelTargetFrameworks(TestTargetFramework targetFramework, bool expectSkipLocalsInit)
        {
            string source = $$"""
                using System.Runtime.InteropServices;
                {{CodeSnippets.LibraryImportAttributeDeclaration}}
                partial class C
                {
                    [LibraryImportAttribute("DoesNotExist")]
                    [return: MarshalAs(UnmanagedType.Bool)]
                    public static partial bool Method();
                }
                """;
            await VerifySourceGeneratorAsync(source, "C", "Method", typeof(SkipLocalsInitAttribute).FullName, attributeAdded: expectSkipLocalsInit, targetFramework);
        }

        [Fact]
        public async Task SkipLocalsInitNotAddedWhenDefinedAtModuleLevel()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                using System.Runtime.CompilerServices;
                [module:SkipLocalsInit]
                partial class C
                {
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial S Method();
                }

                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }

                struct Native
                {
                }

                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;

                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            await VerifySourceGeneratorAsync(source, "C", "Method", typeof(SkipLocalsInitAttribute).FullName, attributeAdded: false, TestTargetFramework.Net);
        }

        [Fact]
        public async Task SkipLocalsInitNotAddedWhenDefinedAtClassLevel()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                using System.Runtime.CompilerServices;
                [SkipLocalsInit]
                partial class C
                {
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial S Method();
                }

                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }

                struct Native
                {
                }

                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;

                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            await VerifySourceGeneratorAsync(source, "C", "Method", typeof(SkipLocalsInitAttribute).FullName, attributeAdded: false, TestTargetFramework.Net);
        }

        [Fact]
        public async Task SkipLocalsInitNotAddedWhenDefinedOnMethodByUser()
        {
            string source = """
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                using System.Runtime.CompilerServices;
                partial class C
                {
                    [SkipLocalsInit]
                    [LibraryImportAttribute("DoesNotExist")]
                    public static partial S Method();
                }

                [NativeMarshalling(typeof(Marshaller))]
                struct S
                {
                }

                struct Native
                {
                }

                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => default;

                    public static S ConvertToManaged(Native n) => default;
                }
                """;
            // Verify that we get no diagnostics from applying the attribute twice.
            await VerifyCS.VerifySourceGeneratorAsync(source);
        }

        private static Task VerifySourceGeneratorAsync(string source, string typeName, string methodName, string? attributeName, bool attributeAdded, TestTargetFramework targetFramework)
        {
            AttributeAddedTest test = new(typeName, methodName, attributeName, attributeAdded, targetFramework)
            {
                TestCode = source,
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck
            };
            return test.RunAsync();
        }

        class AttributeAddedTest : VerifyCS.Test
        {
            private readonly string _typeName;
            private readonly string _methodName;
            private readonly string? _attributeName;
            private readonly bool _expectSkipLocalsInit;

            public AttributeAddedTest(string typeName, string methodName, string? attributeName, bool expectSkipLocalsInitOnMethod, TestTargetFramework targetFramework)
                : base(targetFramework)
            {
                _typeName = typeName;
                _methodName = methodName;
                _attributeName = attributeName;
                _expectSkipLocalsInit = expectSkipLocalsInitOnMethod;
            }

            protected override void VerifyFinalCompilation(Compilation compilation)
            {
                ITypeSymbol c = compilation.GetTypeByMetadataName(_typeName)!;
                IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == _methodName);
                if (_expectSkipLocalsInit)
                {
                    Assert.Contains(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == _attributeName);
                }
                else
                {
                    Assert.DoesNotContain(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == _attributeName);
                }
            }
        }
    }
}
