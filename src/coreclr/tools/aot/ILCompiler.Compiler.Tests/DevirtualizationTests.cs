// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.IL;
using Internal.TypeSystem;

using Xunit;

namespace ILCompiler.Compiler.Tests
{
    public class DevirtualizationTests
    {
        private readonly CompilerTypeSystemContext _context;
        private readonly ModuleDesc _testModule;

        public DevirtualizationTests()
        {
            var target = new TargetDetails(TargetArchitecture.X64, TargetOS.Windows, TargetAbi.CoreRT);
            _context = new CompilerTypeSystemContext(target, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All);

            _context.InputFilePaths = new Dictionary<string, string> {
                { "Test.CoreLib", @"Test.CoreLib.dll" },
                { "ILCompiler.Compiler.Tests.Assets", @"ILCompiler.Compiler.Tests.Assets.dll" },
                };
            _context.ReferenceFilePaths = new Dictionary<string, string>();

            _context.SetSystemModule(_context.GetModuleForSimpleName("Test.CoreLib"));
            _testModule = _context.GetModuleForSimpleName("ILCompiler.Compiler.Tests.Assets");
        }

        private DevirtualizationManager GetDevirtualizationManagerFromScan(MethodDesc method)
        {
            CompilationModuleGroup compilationGroup = new SingleFileCompilationModuleGroup();

            CompilationBuilder builder = new RyuJitCompilationBuilder(_context, compilationGroup);
            IILScanner scanner = builder.GetILScannerBuilder()
                .UseCompilationRoots(new ICompilationRootProvider[] { new SingleMethodRootProvider(method) })
                .ToILScanner();

            return scanner.Scan().GetDevirtualizationManager();
        }

        [Fact]
        public void TestDevirtualizeSimple()
        {
            MetadataType testType = _testModule.GetType("Devirtualization", "DevirtualizeSimple");
            DevirtualizationManager scanDevirt = GetDevirtualizationManagerFromScan(testType.GetMethod("Run", null));

            MethodDesc implMethod = testType.GetNestedType("Derived").GetMethod("Virtual", null);

            // The impl method should be treated as sealed
            Assert.True(scanDevirt.IsEffectivelySealed(implMethod));

            // Even though the metadata based algorithm would say it isn't
            var devirt = new DevirtualizationManager();
            Assert.False(devirt.IsEffectivelySealed(implMethod));
        }

        [Fact]
        public void TestDevirtualizeAbstract()
        {
            MetadataType testType = _testModule.GetType("Devirtualization", "DevirtualizeAbstract");
            DevirtualizationManager scanDevirt = GetDevirtualizationManagerFromScan(testType.GetMethod("Run", null));

            Assert.False(scanDevirt.IsEffectivelySealed(testType.GetNestedType("Abstract")));
        }
    }
}
