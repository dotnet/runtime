using System;
using System.Collections.Generic;
using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.Wasm;
using Internal.IL;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Xunit;

namespace ILCompiler.Compiler.Tests
{
    public class DependencyGraphTests2
    {
        [Fact]
        public void PrintSig()
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

            var sig = Internal.JitInterface.WasmLowering.GetSignature(method).FuncType;
            Console.WriteLine($"\n\n\nMethod: {method.Name}");
            Console.WriteLine($"Length: {sig.Params.Types.Length}");
            for(int i = 0; i < sig.Params.Types.Length; i++) {
                Console.WriteLine($"Param {i}: {sig.Params.Types[i]}");
            }
            Console.WriteLine("\n\n\n");
            throw new Exception("Force output");
        }
    }
}
