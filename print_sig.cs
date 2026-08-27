using System;
using System.Collections.Generic;
using ILCompiler;
using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

class Program
{
    static void Main()
    {
        var targetDetails = new TargetDetails(TargetArchitecture.Wasm32, TargetOS.Linux, TargetAbi.NativeAot);
        var context = new CompilerTypeSystemContext(targetDetails, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All);

        context.InputFilePaths = new Dictionary<string, string> {
            { "Test.CoreLib", @"artifacts/bin/Test.CoreLib/x64/Debug/Test.CoreLib.dll" },
            { "ILCompiler.Compiler.Tests.Assets", @"artifacts/bin/ILCompiler.Compiler.Tests.Assets/x64/Debug/ILCompiler.Compiler.Tests.Assets.dll" },
            };
        context.ReferenceFilePaths = new Dictionary<string, string>();

        context.SetSystemModule(context.GetModuleForSimpleName("Test.CoreLib"));
        EcmaModule testModule = context.GetModuleForSimpleName("ILCompiler.Compiler.Tests.Assets");
        MetadataType testType = testModule
            .GetType("ILCompiler.Compiler.Tests.Assets"u8, "DependencyGraph"u8)
            .GetNestedType("PInvokeCctorDependencyTest"u8);
        MethodDesc method = testType.GetMethod("Entrypoint"u8, null);

        var sig = Internal.JitInterface.WasmLowering.GetSignature(method).FuncType;
        Console.WriteLine($"Method: {method.Name}");
        Console.WriteLine($"Length: {sig.Params.Types.Length}");
        for(int i = 0; i < sig.Params.Types.Length; i++) {
            Console.WriteLine($"Param {i}: {sig.Params.Types[i]}");
        }
    }
}
