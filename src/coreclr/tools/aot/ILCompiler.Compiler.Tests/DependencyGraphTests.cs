// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using ILCompiler.Dataflow;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

using CustomAttributeValue = System.Reflection.Metadata.CustomAttributeValue<Internal.TypeSystem.TypeDesc>;

namespace ILCompiler.Compiler.Tests
{
    //
    // This test uses IL scanner to scan a dependency graph, starting with a
    // single method from the test assembly.
    // It then checks various invariants about the resulting dependency graph.
    // The test method declares these invariants using custom attributes.
    //
    // The invariants to check for are:
    // * Whether an EEType was/was not generated
    // * Whether a method body was/was not generated
    // * Etc.
    //
    // The most valuable tests are the ones that check that something was not
    // generated. These let us create unit tests for size on disk regressions.
    //

    public class DependencyGraphTests
    {
        public static IEnumerable<object[]> GetTestMethods()
        {
            var target = new TargetDetails(TargetArchitecture.X64, TargetOS.Windows, TargetAbi.NativeAot);
            var context = new CompilerTypeSystemContext(target, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All);

            context.InputFilePaths = new Dictionary<string, string> {
                { "Test.CoreLib", @"Test.CoreLib.dll" },
                { "ILCompiler.Compiler.Tests.Assets", @"ILCompiler.Compiler.Tests.Assets.dll" },
                };
            context.ReferenceFilePaths = new Dictionary<string, string>();

            context.SetSystemModule(context.GetModuleForSimpleName("Test.CoreLib"));
            var testModule = context.GetModuleForSimpleName("ILCompiler.Compiler.Tests.Assets");

            bool foundSomethingToCheck = false;
            foreach (var type in testModule.GetType("ILCompiler.Compiler.Tests.Assets", "DependencyGraph").GetNestedTypes())
            {
                foundSomethingToCheck = true;
                yield return new object[] { type.GetMethod("Entrypoint", null) };
            }

            Assert.True(foundSomethingToCheck, "No methods to check?");
        }

        [Theory]
        [MemberData(nameof(GetTestMethods))]
        public void TestDependencyGraphInvariants(EcmaMethod method)
        {
            //
            // Scan the input method
            //

            var context = (CompilerTypeSystemContext)method.Context;
            CompilationModuleGroup compilationGroup = new SingleFileCompilationModuleGroup();

            NativeAotILProvider ilProvider = new NativeAotILProvider();
            CompilerGeneratedState compilerGeneratedState = new CompilerGeneratedState(ilProvider, Logger.Null);

            UsageBasedMetadataManager metadataManager = new UsageBasedMetadataManager(compilationGroup, context,
                new FullyBlockedMetadataBlockingPolicy(), new FullyBlockedManifestResourceBlockingPolicy(),
                null, new NoStackTraceEmissionPolicy(), new NoDynamicInvokeThunkGenerationPolicy(),
                new ILLink.Shared.TrimAnalysis.FlowAnnotations(Logger.Null, ilProvider, compilerGeneratedState), UsageBasedMetadataGenerationOptions.None,
                Logger.Null, Array.Empty<KeyValuePair<string, bool>>(), Array.Empty<string>(), Array.Empty<string>());

            CompilationBuilder builder = new RyuJitCompilationBuilder(context, compilationGroup)
                .UseILProvider(ilProvider);

            IILScanner scanner = builder.GetILScannerBuilder()
                .UseCompilationRoots(new ICompilationRootProvider[] { new SingleMethodRootProvider(method) })
                .UseMetadataManager(metadataManager)
                .ToILScanner();

            ILScanResults results = scanner.Scan();

            //
            // Check invariants
            //

            const string assetsNamespace = "ILCompiler.Compiler.Tests.Assets";
            bool foundSomethingToCheck = false;

            foreach (var attr in method.GetDecodedCustomAttributes(assetsNamespace, "GeneratesConstructedEETypeAttribute"))
            {
                foundSomethingToCheck = true;
                Assert.Contains((TypeDesc)attr.FixedArguments[0].Value, results.ConstructedEETypes);
            }

            foreach (var attr in method.GetDecodedCustomAttributes(assetsNamespace, "NoConstructedEETypeAttribute"))
            {
                foundSomethingToCheck = true;
                Assert.DoesNotContain((TypeDesc)attr.FixedArguments[0].Value, results.ConstructedEETypes);
            }

            foreach (var attr in method.GetDecodedCustomAttributes(assetsNamespace, "GeneratesMethodBodyAttribute"))
            {
                foundSomethingToCheck = true;
                MethodDesc methodToCheck = GetMethodFromAttribute(attr);
                Assert.Contains(methodToCheck.GetCanonMethodTarget(CanonicalFormKind.Specific), results.CompiledMethodBodies);
            }

            foreach (var attr in method.GetDecodedCustomAttributes(assetsNamespace, "NoMethodBodyAttribute"))
            {
                foundSomethingToCheck = true;
                MethodDesc methodToCheck = GetMethodFromAttribute(attr);
                Assert.DoesNotContain(methodToCheck.GetCanonMethodTarget(CanonicalFormKind.Specific), results.CompiledMethodBodies);
            }

            //
            // Make sure we checked something
            //

            Assert.True(foundSomethingToCheck, "No invariants to check?");
        }

        private static MethodDesc GetMethodFromAttribute(CustomAttributeValue attr)
        {
            if (attr.NamedArguments.Length > 0)
                throw new NotImplementedException(); // TODO: parse sig and instantiation

            return ((TypeDesc)attr.FixedArguments[0].Value).GetMethod((string)attr.FixedArguments[1].Value, null);
        }
    }
}
