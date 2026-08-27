// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.Wasm;
using Internal.IL;
using Internal.Text;
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
            foreach (var type in testModule.GetType("ILCompiler.Compiler.Tests.Assets"u8, "DependencyGraph"u8).GetNestedTypes())
            {
                foundSomethingToCheck = true;
                yield return new object[] { type.GetMethod("Entrypoint"u8, null) };
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
            CompilerGeneratedState compilerGeneratedState = new CompilerGeneratedState(ilProvider, Logger.Null, disableGeneratedCodeHeuristics: true);

            UsageBasedMetadataManager metadataManager = new UsageBasedMetadataManager(compilationGroup, context,
                new FullyBlockedMetadataBlockingPolicy(), new FullyBlockedManifestResourceBlockingPolicy(),
                null, new NoStackTraceEmissionPolicy(), new NoDynamicInvokeThunkGenerationPolicy(),
                new ILLink.Shared.TrimAnalysis.FlowAnnotations(Logger.Null, ilProvider, compilerGeneratedState), UsageBasedMetadataGenerationOptions.None,
                default, Logger.Null, new Dictionary<string, bool>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());

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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TentativeMethodNodeEmitsWasmCall(bool useRealBody)
        {
            var targetDetails = new TargetDetails(TargetArchitecture.Wasm32, TargetOS.Linux, TargetAbi.NativeAot);
            var context = new CompilerTypeSystemContext(targetDetails, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All);

            context.InputFilePaths = new Dictionary<string, string> {
                { "Test.CoreLib", @"Test.CoreLib.dll" },
                { "ILCompiler.Compiler.Tests.Assets", @"ILCompiler.Compiler.Tests.Assets.dll" },
                };
            context.ReferenceFilePaths = new Dictionary<string, string>();

            context.SetSystemModule(context.GetModuleForSimpleName("Test.CoreLib"));
            EcmaModule testModule = context.GetModuleForSimpleName("ILCompiler.Compiler.Tests.Assets");
            MetadataType testType = testModule
                .GetType("ILCompiler.Compiler.Tests.Assets"u8, "DependencyGraph"u8)
                .GetNestedType("PInvokeCctorDependencyTest"u8);
            MethodDesc method = testType.GetMethod("Entrypoint"u8, null);

            var realBody = new MethodCodeNode(method);
            ISymbolNode callTarget = useRealBody ?
                realBody :
                new ExternFunctionSymbolNode(new Utf8String("ThrowBodyRemoved"));
            var node = new TestTentativeMethodNode(realBody, callTarget);
            var emitter = new WasmEmitter(null, relocsOnly: false);

            node.EmitCode(ref emitter);
            Assert.IsAssignableFrom<IMethodCodeNodeWithTypeSignature>(node);

            byte[] expected = useRealBody ?
                [0x0C, 0x00, 0x20, 0x00, 0x20, 0x01, 0x12, 0x80, 0x80, 0x80, 0x80, 0x00, 0x0B] :
                [0x0B, 0x00, 0x20, 0x00, 0x10, 0x80, 0x80, 0x80, 0x80, 0x00, 0x00, 0x0B];
            ObjectNode.ObjectData objectData = emitter.Encode(node);
            Assert.Equal(expected, objectData.Data);
            Assert.Equal(1, objectData.Alignment);
            Assert.Same(node, Assert.Single(objectData.DefinedSymbols));

            Relocation relocation = Assert.Single(objectData.Relocs);
            Assert.Equal(RelocType.WASM_FUNCTION_INDEX_LEB, relocation.RelocType);
            Assert.Equal(useRealBody ? 7 : 5, relocation.Offset);
            Assert.Same(callTarget, relocation.Target);
        }

        private static MethodDesc GetMethodFromAttribute(CustomAttributeValue attr)
        {
            if (attr.NamedArguments.Length > 0)
                throw new NotImplementedException(); // TODO: parse sig and instantiation

            return ((TypeDesc)attr.FixedArguments[0].Value).GetMethod(Encoding.UTF8.GetBytes((string)attr.FixedArguments[1].Value), null);
        }

        private sealed class TestTentativeMethodNode : TentativeMethodNode
        {
            private readonly ISymbolNode _target;

            public TestTentativeMethodNode(IMethodBodyNode methodNode, ISymbolNode target)
                : base(methodNode)
            {
                _target = target;
            }

            protected override ISymbolNode GetTarget(NodeFactory factory) => _target;

            public void EmitCode(ref WasmEmitter emitter)
            {
                base.EmitCode(null, ref emitter, relocsOnly: false);
            }
        }
    }
}
