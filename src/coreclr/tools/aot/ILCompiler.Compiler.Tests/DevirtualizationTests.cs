// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

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
            var target = new TargetDetails(TargetArchitecture.X64, TargetOS.Windows, TargetAbi.NativeAot);
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
            MetadataType testType = _testModule.GetType("Devirtualization"u8, "DevirtualizeSimple"u8);
            DevirtualizationManager scanDevirt = GetDevirtualizationManagerFromScan(testType.GetMethod("Run"u8, null));

            MethodDesc implMethod = testType.GetNestedType("Derived"u8).GetMethod("Virtual"u8, null);

            // The impl method should be treated as sealed
            Assert.True(scanDevirt.IsEffectivelySealed(implMethod));

            // Even though the metadata based algorithm would say it isn't
            var devirt = new DevirtualizationManager();
            Assert.False(devirt.IsEffectivelySealed(implMethod));
        }

        [Fact]
        public void TestDevirtualizeAbstract()
        {
            MetadataType testType = _testModule.GetType("Devirtualization"u8, "DevirtualizeAbstract"u8);
            DevirtualizationManager scanDevirt = GetDevirtualizationManagerFromScan(testType.GetMethod("Run"u8, null));

            Assert.False(scanDevirt.IsEffectivelySealed(testType.GetNestedType("Abstract"u8)));
        }

        [Fact]
        public void TestVariantInterfaceGenericMethodResolution()
        {
            DefType objectType = _context.GetWellKnownType(WellKnownType.Object);
            MetadataType baseType = _testModule.GetType("GvmVariantInterface"u8, "GvmVariantBase"u8);
            MetadataType derivedType = _testModule.GetType("GvmVariantInterface"u8, "GvmVariantDerived"u8);
            MetadataType implementationType = _testModule.GetType("GvmVariantInterface"u8, "ClassWithVariantGvms"u8);

            MetadataType inVariantType = _testModule.GetType("GvmVariantInterface"u8, "IInVariantGvm`1"u8);
            MetadataType outVariantType = _testModule.GetType("GvmVariantInterface"u8, "IOutVariantGvm`1"u8);

            MethodDesc inObjectMethod = inVariantType.MakeInstantiatedType(objectType).GetMethods().Single(m => m.GetName() == "Func").MakeInstantiatedMethod(objectType);
            MethodDesc inBaseMethod = inVariantType.MakeInstantiatedType(baseType).GetMethods().Single(m => m.GetName() == "Func").MakeInstantiatedMethod(objectType);
            MethodDesc inDerivedMethod = inVariantType.MakeInstantiatedType(derivedType).GetMethods().Single(m => m.GetName() == "Func").MakeInstantiatedMethod(objectType);
            MethodDesc outObjectMethod = outVariantType.MakeInstantiatedType(objectType).GetMethods().Single(m => m.GetName() == "Func").MakeInstantiatedMethod(objectType);
            MethodDesc outBaseMethod = outVariantType.MakeInstantiatedType(baseType).GetMethods().Single(m => m.GetName() == "Func").MakeInstantiatedMethod(objectType);
            MethodDesc outDerivedMethod = outVariantType.MakeInstantiatedType(derivedType).GetMethods().Single(m => m.GetName() == "Func").MakeInstantiatedMethod(objectType);

            MethodImplRecord[] methodImpls = implementationType.FindMethodsImplWithMatchingDeclName(inObjectMethod.Name);
            Assert.NotNull(methodImpls);

            MethodDesc inObjectImpl = methodImpls.Single(m => m.Decl.GetMethodDefinition() == inObjectMethod.GetMethodDefinition()).Body.MakeInstantiatedMethod(objectType);
            MethodDesc inBaseImpl = methodImpls.Single(m => m.Decl.GetMethodDefinition() == inBaseMethod.GetMethodDefinition()).Body.MakeInstantiatedMethod(objectType);
            MethodDesc outDerivedImpl = methodImpls.Single(m => m.Decl.GetMethodDefinition() == outDerivedMethod.GetMethodDefinition()).Body.MakeInstantiatedMethod(objectType);
            MethodDesc outBaseImpl = methodImpls.Single(m => m.Decl.GetMethodDefinition() == outBaseMethod.GetMethodDefinition()).Body.MakeInstantiatedMethod(objectType);

            var devirt = new DevirtualizationManager();
            Assert.Equal(inObjectImpl, devirt.ResolveVirtualMethod(inObjectMethod, implementationType, out _));
            Assert.Equal(inBaseImpl, devirt.ResolveVirtualMethod(inBaseMethod, implementationType, out _));
            Assert.Equal(inObjectImpl, devirt.ResolveVirtualMethod(inDerivedMethod, implementationType, out _));
            Assert.Equal(outDerivedImpl, devirt.ResolveVirtualMethod(outObjectMethod, implementationType, out _));
            Assert.Equal(outBaseImpl, devirt.ResolveVirtualMethod(outBaseMethod, implementationType, out _));
            Assert.Equal(outDerivedImpl, devirt.ResolveVirtualMethod(outDerivedMethod, implementationType, out _));
        }
    }
}
