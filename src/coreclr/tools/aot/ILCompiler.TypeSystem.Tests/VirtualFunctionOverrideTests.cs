// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Xunit;
using Xunit.Abstractions;


namespace TypeSystemTests
{
    public class VirtualFunctionOverrideTests
    {
        private TestTypeSystemContext _context;
        private ModuleDesc _testModule;
        private DefType _stringType;
        private DefType _voidType;
        private ITestOutputHelper _logger;

        public VirtualFunctionOverrideTests(ITestOutputHelper logger)
        {
            _logger = logger;
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
                if (m.GetName() == "GenMethod")
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
                if (m.GetName().Contains("GenMethod"))
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
            MethodDesc objectToString = objectType.GetMethod("ToString"u8, toStringSig);
            Assert.NotNull(objectToString);
            MetadataType openTestType = _testModule.GetType("VirtualFunctionOverride", "SimpleGeneric`1");
            InstantiatedType testInstance = openTestType.MakeInstantiatedType(objectType);
            MethodDesc targetOnInstance = testInstance.GetMethod("ToString"u8, toStringSig);

            MethodDesc targetMethod = testInstance.FindVirtualFunctionTargetMethodOnObjectType(objectToString);
            Assert.Equal(targetOnInstance, targetMethod);
        }

        [Fact]
        public void TestVirtualDispatchOnGenericTypeWithOverload()
        {
            MetadataType openDerived = _testModule.GetType("VirtualFunctionOverride", "DerivedGenericWithOverload`1");
            MetadataType derivedInstance = openDerived.MakeInstantiatedType(_stringType);
            MetadataType baseInstance = (MetadataType)derivedInstance.BaseType;

            MethodDesc baseNongenericOverload = baseInstance.GetMethod("MyMethod"u8, new MethodSignature(MethodSignatureFlags.None, 0, _voidType, new TypeDesc[] { _stringType }));
            MethodDesc derivedNongenericOverload = derivedInstance.GetMethod("MyMethod"u8, new MethodSignature(MethodSignatureFlags.None, 0, _voidType, new TypeDesc[] { _stringType }));
            MethodDesc nongenericTargetOverload = derivedInstance.FindVirtualFunctionTargetMethodOnObjectType(baseNongenericOverload);
            Assert.Equal(derivedNongenericOverload, nongenericTargetOverload);

            MethodDesc baseGenericOverload = baseInstance.GetMethod("MyMethod"u8, new MethodSignature(MethodSignatureFlags.None, 0, _voidType, new TypeDesc[] { _context.GetSignatureVariable(0, false) }));
            MethodDesc derivedGenericOverload = derivedInstance.GetMethod("MyMethod"u8, new MethodSignature(MethodSignatureFlags.None, 0, _voidType, new TypeDesc[] { _context.GetSignatureVariable(0, false) }));
            MethodDesc genericTargetOverload = derivedInstance.FindVirtualFunctionTargetMethodOnObjectType(baseGenericOverload);
            Assert.Equal(derivedGenericOverload, genericTargetOverload);
        }

        [Fact]
        public void TestFinalizeOverrideChecking()
        {
            MetadataType classWithFinalizer = _testModule.GetType("VirtualFunctionOverride", "ClassWithFinalizer");
            DefType objectType = _testModule.Context.GetWellKnownType(WellKnownType.Object);
            MethodDesc finalizeMethod = objectType.GetMethod("Finalize"u8, new MethodSignature(MethodSignatureFlags.None, 0, _voidType, Array.Empty<TypeDesc>()));

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

            var myGetHashCodeMethod = explicitOverrideClass.GetMethod("MyGetHashCode"u8, null);

            var objectGetHashCodeMethod = _context.GetWellKnownType(WellKnownType.Object).GetMethod("GetHashCode"u8, null);

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
            MethodDesc method = myDerived2Type.GetMethod("get_foo"u8, null);
            Assert.NotNull(method);

            MethodDesc virtualMethod = algo.FindVirtualFunctionTargetMethodOnObjectType(method, myDerived2Type);
            Assert.NotNull(virtualMethod);
            Assert.Equal(method, virtualMethod);
        }

        [Fact]
        public void TestGenericsOverrideOfSpecificMethodWhereSubstitutionsAreNecessaryToComputeTheRightTargetToOverride()
        {
            var algo = new MetadataVirtualMethodAlgorithm();
            var ilModule = _context.GetModuleForSimpleName("ILTestAssembly");
            MetadataType myDerivedType = ilModule.GetType("MethodImplOverride1", "Derived");
            MetadataType baseType = ilModule.GetType("MethodImplOverride1", "Base`2").MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32), _context.GetWellKnownType(WellKnownType.Int32));
            var csModule = _context.GetModuleForSimpleName("CoreTestAssembly");
            var myGenericType = csModule.GetType("GenericTypes", "GenericClass`1");
            var myGenericTypeInstantiatedOverBang0 = myGenericType.MakeInstantiatedType(_context.GetSignatureVariable(0, false));
            var myGenericTypeInstantiatedOverBang0ByRef = myGenericTypeInstantiatedOverBang0.MakeByRefType();
            var myGenericTypeInstantiatedOverBang1 = myGenericType.MakeInstantiatedType(_context.GetSignatureVariable(1, false));
            var myGenericTypeInstantiatedOverBang1ByRef = myGenericTypeInstantiatedOverBang1.MakeByRefType();
            var stringType = _context.GetWellKnownType(WellKnownType.String);

            MethodSignature sigBang0Bang1 = new MethodSignature(0, 0, stringType, new TypeDesc[] { myGenericTypeInstantiatedOverBang0ByRef, myGenericTypeInstantiatedOverBang1ByRef });
            MethodDesc baseMethod0_1 = baseType.GetMethod("Method"u8, sigBang0Bang1);

            MethodDesc virtualMethodBang0Bang1 = algo.FindVirtualFunctionTargetMethodOnObjectType(baseMethod0_1, myDerivedType);
            Assert.Equal(virtualMethodBang0Bang1.OwningType, myDerivedType);

            MethodSignature sigBang1Bang0 = new MethodSignature(0, 0, stringType, new TypeDesc[] { myGenericTypeInstantiatedOverBang1ByRef, myGenericTypeInstantiatedOverBang0ByRef });
            MethodDesc baseMethod1_0 = baseType.GetMethod("Method"u8, sigBang1Bang0);

            MethodDesc virtualMethodBang1Bang0 = algo.FindVirtualFunctionTargetMethodOnObjectType(baseMethod1_0, myDerivedType);
            Assert.Equal(virtualMethodBang1Bang0.OwningType, baseType);
        }

        [Fact]
        public void TestGenericsClassOverrideOfMethodWhereMethodHasBeenMovedFromTheTypeWhichPreviouslyDeclaredTheTypeToItsBaseType()
        {
            var algo = new MetadataVirtualMethodAlgorithm();
            var csModule = _context.GetModuleForSimpleName("CoreTestAssembly");
            var ilModule = _context.GetModuleForSimpleName("ILTestAssembly");
            var doubleType = _context.GetWellKnownType(WellKnownType.Double);
            var objectType = _context.GetWellKnownType(WellKnownType.Object);
            var stringType = _context.GetWellKnownType(WellKnownType.String);
            var intType = _context.GetWellKnownType(WellKnownType.Int32);
            var myGenericType = csModule.GetType("GenericTypes", "GenericClass`1");
            var genericTypeOfInt = myGenericType.MakeInstantiatedType(intType);
            var doubleArrayType = doubleType.MakeArrayType();

            MetadataType myDerivedType = ilModule.GetType("MethodImplOverride1", "DerivedGenericsShape`1").MakeInstantiatedType(doubleType);
            MetadataType baseType = ilModule.GetType("MethodImplOverride1", "BaseTestGenericsShape`4").MakeInstantiatedType(objectType, stringType, genericTypeOfInt, doubleArrayType);

            var bang0Type = _context.GetSignatureVariable(0, false);
            var bang1Type = _context.GetSignatureVariable(1, false);
            var bang2Type = _context.GetSignatureVariable(2, false);

            MethodSignature sigBang0Bang1 = new MethodSignature(0, 0, stringType, new TypeDesc[] { bang0Type, bang1Type });
            MethodDesc baseMethod0_1 = baseType.GetMethod("Method"u8, sigBang0Bang1);

            MethodDesc virtualMethodBang0Bang1 = algo.FindVirtualFunctionTargetMethodOnObjectType(baseMethod0_1, myDerivedType);
            Assert.Equal(virtualMethodBang0Bang1.OwningType, baseType);

            MethodDesc baseMethod2_3 = null;
            // BaseMethod(!2,!3) has custom modifiers in its signature, and thus the sig is difficult to write up by hand. Just search for
            // it in an ad hoc manner
            foreach (MethodDesc method in baseType.GetMethods())
            {
                if (method.GetName() != "Method")
                    continue;

                if (method.GetTypicalMethodDefinition().Signature[0] == bang2Type)
                {
                    baseMethod2_3 = method;
                    break;
                }
            }

            MethodDesc virtualMethodBang1Bang0 = algo.FindVirtualFunctionTargetMethodOnObjectType(baseMethod2_3, myDerivedType);
            Assert.Equal(virtualMethodBang1Bang0.OwningType, myDerivedType);
        }

        private void ResolveInterfaceDispatch_ForMultiGenericTest(MetadataType type, out MethodDesc md1, out MethodDesc md2)
        {
            var algo = new MetadataVirtualMethodAlgorithm();
            var stringType = _context.GetWellKnownType(WellKnownType.String);

            var bang0Type = _context.GetSignatureVariable(0, false);
            var bang1Type = _context.GetSignatureVariable(1, false);
            MethodSignature sigBang0Bang1 = new MethodSignature(0, 0, stringType, new TypeDesc[] { bang0Type, bang1Type });
            MethodSignature sigBang1Bang0 = new MethodSignature(0, 0, stringType, new TypeDesc[] { bang1Type, bang0Type });

            var iMultiGeneric = type.ExplicitlyImplementedInterfaces.First();
            var method0_1 = iMultiGeneric.GetMethod("Func"u8, sigBang0Bang1);
            Assert.NotNull(method0_1);
            var method1_0 = iMultiGeneric.GetMethod("Func"u8, sigBang1Bang0);
            Assert.NotNull(method1_0);

            md1 = algo.ResolveInterfaceMethodToVirtualMethodOnType(method0_1, type);
            md2 = algo.ResolveInterfaceMethodToVirtualMethodOnType(method1_0, type);
            Assert.NotNull(md1);
            Assert.NotNull(md2);
        }

        [Fact]
        public void TestExactMethodImplGenericDeclResolutionOnInterfaces()
        {
            var ilModule = _context.GetModuleForSimpleName("ILTestAssembly");
            var intType = _context.GetWellKnownType(WellKnownType.Int32);
            var bang0Type = _context.GetSignatureVariable(0, false);
            var bang1Type = _context.GetSignatureVariable(1, false);

            var implementorType = ilModule.GetType("MethodImplOverride1", "Implementor`2").MakeInstantiatedType(intType, intType);
            var partialIntImplementorType = ilModule.GetType("MethodImplOverride1", "PartialIntImplementor`1").MakeInstantiatedType(intType);
            var intImplementorType = ilModule.GetType("MethodImplOverride1", "IntImplementor");

            ResolveInterfaceDispatch_ForMultiGenericTest(implementorType, out var md1, out var md2);
            Assert.Equal(bang0Type, md1.GetTypicalMethodDefinition().Signature[0]);
            Assert.Equal(bang1Type, md1.GetTypicalMethodDefinition().Signature[1]);
            Assert.Equal(bang1Type, md2.GetTypicalMethodDefinition().Signature[0]);
            Assert.Equal(bang0Type, md2.GetTypicalMethodDefinition().Signature[1]);

            ResolveInterfaceDispatch_ForMultiGenericTest(partialIntImplementorType, out md1, out md2);
            Assert.Equal(bang0Type, md1.GetTypicalMethodDefinition().Signature[0]);
            Assert.Equal(intType, md1.GetTypicalMethodDefinition().Signature[1]);
            Assert.Equal(intType, md2.GetTypicalMethodDefinition().Signature[0]);
            Assert.Equal(bang0Type, md2.GetTypicalMethodDefinition().Signature[1]);

            ResolveInterfaceDispatch_ForMultiGenericTest(intImplementorType, out md1, out md2);
            Assert.Contains("!0,!1", md1.GetName());
            Assert.Contains("!1,!0", md2.GetName());
        }

        [Fact]
        public void TestFunctionPointerOverloads()
        {
            MetadataType baseClass = _testModule.GetType("VirtualFunctionOverride", "FunctionPointerOverloadBase");
            MetadataType derivedClass = _testModule.GetType("VirtualFunctionOverride", "FunctionPointerOverloadDerived");

            var resolvedMethods = new List<MethodDesc>();
            foreach (MethodDesc baseMethod in baseClass.GetVirtualMethods())
                resolvedMethods.Add(derivedClass.FindVirtualFunctionTargetMethodOnObjectType(baseMethod));

            var expectedMethods = new List<MethodDesc>();
            foreach (MethodDesc derivedMethod in derivedClass.GetVirtualMethods())
                expectedMethods.Add(derivedMethod);

            Assert.Equal(expectedMethods, resolvedMethods);

            Assert.Equal(expectedMethods[0].Signature[0], expectedMethods[1].Signature[0]);
            Assert.NotEqual(expectedMethods[0].Signature[0], expectedMethods[3].Signature[0]);
        }

        [Theory]
        // ScenarioA_Test1
        [InlineData("ScenarioA_Test1_1", "T3", "A.T3_1", "A.T1", "Func1")]
        [InlineData("ScenarioA_Test1_2", "T2", "A.T3_1", "A.T2Exp", "Func2")]
        [InlineData("ScenarioA_Test1_3", "T3", "A.T3_1", "A.T3_1", "Func3")]

        // ScenarioA_Test2
        [InlineData("ScenarioA_Test2_1", "T3", "A.T3_2", "A.T1", "Func1")]
        [InlineData("ScenarioA_Test2_2", "T3", "A.T3_2", "A.T2Exp", "Func2")]
        [InlineData("ScenarioA_Test2_3", "T3", "A.T3_2", "A.T3_2", "Func3")]

        // ScenarioA_Test3
        [InlineData("ScenarioA_Test3_1", "T3", "A.T3_3", "A.T1", "Func1")]
        [InlineData("ScenarioA_Test3_2", "T2", "A.T3_3", "A.T2Exp", "Func2")]
        [InlineData("ScenarioA_Test3_3", "T3", "A.T3_3", "A.T3_3", "Func1")]

        // ScenarioA_Test4
        [InlineData("ScenarioA_Test4_1", "T3", "A.T3_4", "A.T1", "Func1")]
        [InlineData("ScenarioA_Test4_2", "T3", "A.T3_4", "A.T2Exp", "Func2")]
        [InlineData("ScenarioA_Test4_3", "T3", "A.T3_4", "A.T3_4", "Func2")]

        // ScenarioA_Test5
        [InlineData("ScenarioA_Test5_1", "T3", "A.T3_5", "A.T1", "Func1")]
        [InlineData("ScenarioA_Test5_2", "T3", "A.T3_5", "A.T2Imp", "Func1")]
        [InlineData("ScenarioA_Test5_3", "T3", "A.T3_5", "A.T3_5", "Func3")]

        // ScenarioA_Test6
        [InlineData("ScenarioA_Test6_1", "T3", "A.T3_6", "A.T1", "Func1")]
        [InlineData("ScenarioA_Test6_2", "T3", "A.T3_6", "A.T2Imp", "Func1")]
        [InlineData("ScenarioA_Test6_3", "T3", "A.T3_6", "A.T3_6", "Func3")]

        // ScenarioA_Test7
        [InlineData("ScenarioA_Test7_1", "T3", "A.T3_7", "A.T1", "Func1")]
        [InlineData("ScenarioA_Test7_2", "T3", "A.T3_7", "A.T2Imp", "Func1")]
        [InlineData("ScenarioA_Test7_3", "T3", "A.T3_7", "A.T3_7", "Func1")]

        // ScenarioB_Test1
        [InlineData("ScenarioB_Test_1_1", "T3", "B.T3_1", "B.T1", "Func1")]
        [InlineData("ScenarioB_Test_1_2", "T2", "B.T3_1", "B.T2", "Func2")]
        [InlineData("ScenarioB_Test_1_3", "T3", "B.T3_1", "B.T3_1", "Func3")] // Change from IL

        // ScenarioB_Test2
        [InlineData("ScenarioB_Test_2_1", "T3", "B.T3_2", "B.T1", "Func1")]
        [InlineData("ScenarioB_Test_2_2", "T3", "B.T3_2", "B.T2", "Func2")]
        [InlineData("ScenarioB_Test_2_3", "T3", "B.T3_2", "B.T3_2", "Func3")] // Change from IL

        // ScenarioB_Test3
        [InlineData("ScenarioB_Test_3_1", "T3", "B.T3_3", "B.T1", "Func1")]
        [InlineData("ScenarioB_Test_3_2", "T2", "B.T3_3", "B.T2", "Func2")]
        [InlineData("ScenarioB_Test_3_3", "T3", "B.T3_3", "B.T3_3", "Func1")]

        // ScenarioB_Test4
        [InlineData("ScenarioB_Test_4_1", "T3", "B.T3_4", "B.T1", "Func1")]
        [InlineData("ScenarioB_Test_4_2", "T3", "B.T3_4", "B.T2", "Func2")]
        [InlineData("ScenarioB_Test_4_3", "T3", "B.T3_4", "B.T3_4", "Func2")] // Change from IL

        // ScenarioC_Test1
        [InlineData("ScenarioC_Test_1_1", "T3", "C.T3_1", "C.T1", "Func1")]
        [InlineData("ScenarioC_Test_1_2", "T3", "C.T3_1", "C.T2", "Func2")]
        [InlineData("ScenarioC_Test_1_3", "T3", "C.T3_1", "C.T3_1", "Func3")]

        // ScenarioC_Test2
        [InlineData("ScenarioC_Test_2_1", "T3", "C.T3_2", "C.T1", "Func1")]
        [InlineData("ScenarioC_Test_2_2", "T3", "C.T3_2", "C.T2", "Func2")]
        [InlineData("ScenarioC_Test_2_3", "T3", "C.T3_2", "C.T3_2", "Func3")] // Change from IL

        // ScenarioC_Test3
        [InlineData("ScenarioC_Test_3_1", "T3", "C.T3_3", "C.T1", "Func1")]
        [InlineData("ScenarioC_Test_3_2", "T2", "C.T3_3", "C.T2", "Func2")]
        [InlineData("ScenarioC_Test_3_3", "T3", "C.T3_3", "C.T3_3", "Func1")]

        // ScenarioC_Test4
        [InlineData("ScenarioC_Test_4_1", "T3", "C.T3_4", "C.T1", "Func1")]
        [InlineData("ScenarioC_Test_4_2", "T3", "C.T3_4", "C.T2", "Func2")]
        [InlineData("ScenarioC_Test_4_3", "T3", "C.T3_4", "C.T3_4", "Func2")] // Change from IL
        [InlineData("ScenarioC_Test_4_4", "T3", "C.T3_4", "C.T3_4", "Func2")]

        // ScenarioD_Test1
        [InlineData("ScenarioD_Test_1_1", "T3", "D.T3_1", "D.T1", "Func1")]
        [InlineData("ScenarioD_Test_1_2", "T3", "D.T3_1", "D.T2", "Func1")]
        [InlineData("ScenarioD_Test_1_3", "T3", "D.T3_1", "D.T3_1", "Func3")] // Change from IL
        [InlineData("ScenarioD_Test_1_4", "T3", "D.T3_1", "D.T3_1", "Func3")]

        // ScenarioD_Test2
        [InlineData("ScenarioD_Test_2_1", "T3", "D.T3_2", "D.T1", "Func1")]
        [InlineData("ScenarioD_Test_2_2", "T3", "D.T3_2", "D.T2", "Func1")]
        [InlineData("ScenarioD_Test_2_3", "T3", "D.T3_2", "D.T3_2", "Func3")] // Change from IL
        // DUPLICATE?!?! [InlineData("T3", "D.T3_1", "D.T3_1", "Func3")]

        // ScenarioD_Test3
        [InlineData("ScenarioD_Test_3_1", "T3", "D.T3_3", "D.T1", "Func1")]
        [InlineData("ScenarioD_Test_3_2", "T3", "D.T3_3", "D.T2", "Func1")]
        [InlineData("ScenarioD_Test_3_3", "T3", "D.T3_3", "D.T3_3", "Func1")]

        // ScenarioE_Test1
        [InlineData("ScenarioE_Test_1_1", "T3", "E.T3_1", "E.T1", "Func1")]
        [InlineData("ScenarioE_Test_1_2", "T3", "E.T3_1", "E.T2", "Func1")]
        [InlineData("ScenarioE_Test_1_3", "T3", "E.T3_1", "E.T3_1", "Func3")]

        // ScenarioE_Test2
        [InlineData("ScenarioE_Test_2_1", "T3", "E.T3_2", "E.T1", "Func1")]
        [InlineData("ScenarioE_Test_2_2", "T3", "E.T3_2", "E.T2", "Func1")]
        [InlineData("ScenarioE_Test_2_3", "T3", "E.T3_2", "E.T3_2", "Func3")]
        // DUPLICATE?!?!?        [InlineData("T3", "E.T3_1", "E.T3_1", "Func3")]

        // ScenarioE_Test3
        [InlineData("ScenarioE_Test_3_1", "T3", "E.T3_3", "E.T1", "Func1")]
        [InlineData("ScenarioE_Test_3_2", "T3", "E.T3_3", "E.T2", "Func1")]
        [InlineData("ScenarioE_Test_3_3", "T3", "E.T3_3", "E.T3_3", "Func1")]
        public void TestPreserveBaseOverridesBehavior(string exactScenarioName, string stringToExpect, string typeToConstruct, string typeOfMethodToCallOn, string methodName)
        {
            this._logger.WriteLine(exactScenarioName);
            var ilModule = _context.GetModuleForSimpleName("ILTestAssembly");
            (string typeToConstructNamespace, string typeToConstructTypeName) = SplitIntoNameAndNamespace(typeToConstruct);
            var constructedType = ilModule.GetType(typeToConstructNamespace, typeToConstructTypeName);

            (string typeToCallNamespace, string typeToCallTypeName) = SplitIntoNameAndNamespace(typeOfMethodToCallOn);
            var typeToCall = ilModule.GetType(typeToCallNamespace, typeToCallTypeName);

            MethodDesc callMethod = typeToCall.GetMethod(Encoding.UTF8.GetBytes(methodName), null);
            Assert.NotNull(callMethod);

            MethodDesc resolvedMethod = constructedType.FindVirtualFunctionTargetMethodOnObjectType(callMethod);

            var methodIL = Internal.IL.EcmaMethodIL.Create((EcmaMethod)resolvedMethod);
            ILReader reader = new ILReader(methodIL.GetILBytes());
            Assert.Equal(ILOpcode.ldstr, reader.ReadILOpcode());

            int userStringToken = reader.ReadILToken();
            string stringLoadedAsFirstILOpcodeInResolvedMethod = (string)methodIL.GetObject(userStringToken);
            Assert.Equal(stringToExpect, stringLoadedAsFirstILOpcodeInResolvedMethod);
        }


        [Theory]
        [InlineData("Base", "BaseCovariant")]
        [InlineData("Impl", "ImplCovariant")]
        [InlineData("SubImpl", "SubImplCovariant")]
        [InlineData("SubImpl_OverrideViaNameSig", "SubImplCovariant2")]
        [InlineData("SubImpl_OverrideViaNameSig_OverriddenViaMethodImpl", "SubSubImplCovariant")]
        public void TestSubImplCovariant(string exactScenarioName, string typeToConstruct)
        {
            this._logger.WriteLine(exactScenarioName);

            MetadataType derivedClass = _testModule.GetType("VirtualFunctionOverride", typeToConstruct);
            MetadataType baseClass = derivedClass;

            while (baseClass != baseClass.Context.GetWellKnownType(WellKnownType.Object))
            {
                this._logger.WriteLine("-----");
                this._logger.WriteLine(baseClass.ToString());
                MethodDesc callMethod = baseClass.GetMethod("FromType"u8, null);
                this._logger.WriteLine(callMethod.ToString());
                Assert.NotNull(callMethod);

                MethodDesc resolvedMethod = derivedClass.FindVirtualFunctionTargetMethodOnObjectType(callMethod);
                this._logger.WriteLine(resolvedMethod.ToString());

                Assert.Equal(typeToConstruct, ((EcmaType)((EcmaMethod)resolvedMethod).OwningType).GetName());

                baseClass = (MetadataType)baseClass.BaseType;
            }
        }

        private static (string _namespace, string type) SplitIntoNameAndNamespace(string typeName)
        {
            int namespaceIndextypeName = typeName.LastIndexOf('.');
            Assert.True(namespaceIndextypeName > 0);
            string typeNameNamespace = typeName.Substring(0, namespaceIndextypeName);
            string typeNameTypeName = typeName.Substring(namespaceIndextypeName + 1);
            Assert.True(typeNameTypeName.Length > 0);
            return (typeNameNamespace, typeNameTypeName);
        }
    }
}
