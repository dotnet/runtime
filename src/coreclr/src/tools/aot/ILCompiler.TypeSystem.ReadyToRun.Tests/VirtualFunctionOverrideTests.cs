// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Internal.TypeSystem;

using Xunit;


namespace TypeSystemTests
{
    public class VirtualFunctionOverrideTests
    {
        TestTypeSystemContext _context;
        ModuleDesc _testModule;
        DefType _stringType;
        DefType _voidType;

        public VirtualFunctionOverrideTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;

            _stringType = _context.GetWellKnownType(WellKnownType.String);
            _voidType = _context.GetWellKnownType(WellKnownType.Void);
        }

        [Fact]
        public void TestGenericMethodInterfaceMethodImplOverride()
        {
            //
            // Ensure MethodImpl based overriding works for MethodSpecs
            //

            MetadataType interfaceType = _testModule.GetType("VirtualFunctionOverride", "IIFaceWithGenericMethod");
            MethodDesc interfaceMethod = null;

            foreach(MethodDesc m in interfaceType.GetMethods())
            {
                if (m.Name == "GenMethod")
                {
                    interfaceMethod = m;
                    break;
                }
            }
            Assert.NotNull(interfaceMethod);

            MetadataType objectType = _testModule.GetType("VirtualFunctionOverride", "HasMethodInterfaceOverrideOfGenericMethod");
            MethodDesc expectedVirtualMethod = null;
            foreach (MethodDesc m in objectType.GetMethods())
            {
                if (m.Name.Contains("GenMethod"))
                {
                    expectedVirtualMethod = m;
                    break;
                }
            }
            Assert.NotNull(expectedVirtualMethod);

            Assert.Equal(expectedVirtualMethod, objectType.ResolveInterfaceMethodToVirtualMethodOnType(interfaceMethod));
        }

        [Fact]
        public void TestVirtualDispatchOnGenericType()
        {
            // Verifies that virtual dispatch to a non-generic method on a generic instantiation works
            DefType objectType = _context.GetWellKnownType(WellKnownType.Object);
            MethodSignature toStringSig = new MethodSignature(MethodSignatureFlags.None, 0, _stringType, Array.Empty<TypeDesc>());
            MethodDesc objectToString = objectType.GetMethod("ToString", toStringSig);
            Assert.NotNull(objectToString);
            MetadataType openTestType = _testModule.GetType("VirtualFunctionOverride", "SimpleGeneric`1");
            InstantiatedType testInstance = openTestType.MakeInstantiatedType(objectType);
            MethodDesc targetOnInstance = testInstance.GetMethod("ToString", toStringSig);

            MethodDesc targetMethod = testInstance.FindVirtualFunctionTargetMethodOnObjectType(objectToString);
            Assert.Equal(targetOnInstance, targetMethod);        
        }

        [Fact]
        public void TestVirtualDispatchOnGenericTypeWithOverload()
        {
            MetadataType openDerived = _testModule.GetType("VirtualFunctionOverride", "DerivedGenericWithOverload`1");
            MetadataType derivedInstance = openDerived.MakeInstantiatedType(_stringType);
            MetadataType baseInstance = (MetadataType)derivedInstance.BaseType;

            MethodDesc baseNongenericOverload = baseInstance.GetMethod("MyMethod", new MethodSignature(MethodSignatureFlags.None, 0, _voidType, new TypeDesc[] { _stringType }));
            MethodDesc derivedNongenericOverload = derivedInstance.GetMethod("MyMethod", new MethodSignature(MethodSignatureFlags.None, 0, _voidType, new TypeDesc[] { _stringType }));
            MethodDesc nongenericTargetOverload = derivedInstance.FindVirtualFunctionTargetMethodOnObjectType(baseNongenericOverload);
            Assert.Equal(derivedNongenericOverload, nongenericTargetOverload);

            MethodDesc baseGenericOverload = baseInstance.GetMethod("MyMethod", new MethodSignature(MethodSignatureFlags.None, 0, _voidType, new TypeDesc[] { _context.GetSignatureVariable(0, false) }));
            MethodDesc derivedGenericOverload = derivedInstance.GetMethod("MyMethod", new MethodSignature(MethodSignatureFlags.None, 0, _voidType, new TypeDesc[] { _context.GetSignatureVariable(0, false) }));
            MethodDesc genericTargetOverload = derivedInstance.FindVirtualFunctionTargetMethodOnObjectType(baseGenericOverload);
            Assert.Equal(derivedGenericOverload, genericTargetOverload);
        }

        [Fact]
        public void TestFinalizeOverrideChecking()
        {
            MetadataType classWithFinalizer = _testModule.GetType("VirtualFunctionOverride", "ClassWithFinalizer");
            DefType objectType = _testModule.Context.GetWellKnownType(WellKnownType.Object);
            MethodDesc finalizeMethod = objectType.GetMethod("Finalize", new MethodSignature(MethodSignatureFlags.None, 0, _voidType, new TypeDesc[] { }));

            MethodDesc actualFinalizer = classWithFinalizer.FindVirtualFunctionTargetMethodOnObjectType(finalizeMethod);
            Assert.NotNull(actualFinalizer);
            Assert.NotEqual(actualFinalizer, finalizeMethod);
        }

        [Fact]
        public void TestExplicitOverride()
        {
            //
            // Test that explicit virtual method overriding works.
            //

            var ilModule = _context.GetModuleForSimpleName("ILTestAssembly");
            var explicitOverrideClass = ilModule.GetType("VirtualFunctionOverride", "ExplicitOverride");

            var myGetHashCodeMethod = explicitOverrideClass.GetMethod("MyGetHashCode", null);

            var objectGetHashCodeMethod = _context.GetWellKnownType(WellKnownType.Object).GetMethod("GetHashCode", null);

            var foundOverride = explicitOverrideClass.FindVirtualFunctionTargetMethodOnObjectType(objectGetHashCodeMethod);

            Assert.Equal(myGetHashCodeMethod, foundOverride);
        }

        [Fact]
        public void TestFindBaseUnificationGroup()
        {
            var algo = new MetadataVirtualMethodAlgorithm();
            var ilModule = _context.GetModuleForSimpleName("ILTestAssembly");
            MetadataType myDerived2Type = ilModule.GetType("VirtualFunctionOverride", "MyDerived2");
            Assert.NotNull(myDerived2Type);
            MethodDesc method = myDerived2Type.GetMethod("get_foo", null);
            Assert.NotNull(method);

            MethodDesc virtualMethod = algo.FindVirtualFunctionTargetMethodOnObjectType(method, myDerived2Type);
            Assert.NotNull(virtualMethod);
            Assert.Equal(method, virtualMethod);
        }
    }
}
