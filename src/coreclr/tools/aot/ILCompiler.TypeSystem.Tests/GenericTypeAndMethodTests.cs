// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Internal.TypeSystem;
using Xunit;

namespace TypeSystemTests
{
    public class GenericMethodTests
    {
        private TestTypeSystemContext _context;
        private ModuleDesc _testModule;

        public GenericMethodTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.Unknown);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;
        }

        /// <summary>
        /// Testing proper instantiation of types and methods involving generic parameters in their signature.
        /// </summary>
        [Fact]
        public void TestInstantiation()
        {
            MetadataType t = _testModule.GetType("GenericTypes", "GenericClass`1");

            // Verify that we get just type definitions.
            Assert.NotNull(t);
            Assert.True(t.IsTypeDefinition);
            Assert.Equal(1, t.Instantiation.Length);
            Assert.True(t.Instantiation[0].IsTypeDefinition);

            // Verify that we got a method definition
            MethodDesc fooMethod = t.GetMethods().First(m => m.Name == "Foo");
            Assert.True(fooMethod.IsTypicalMethodDefinition);

            // Verify that instantiating a method definition has no effect
            MethodDesc instantiatedMethod = fooMethod.InstantiateSignature(new Instantiation(_context.GetWellKnownType(WellKnownType.Int32)), Instantiation.Empty);
            Assert.Same(fooMethod, instantiatedMethod);

            MetadataType instantiatedType = t.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));

            // Verify properties of the instantiated type
            Assert.NotNull(instantiatedType);
            Assert.False(instantiatedType.IsTypeDefinition);
            Assert.Equal(1, instantiatedType.Instantiation.Length);
            Assert.Equal(_context.GetWellKnownType(WellKnownType.Int32), instantiatedType.Instantiation[0]);

            // Verify that we get an instantiated method with the proper signature
            MethodDesc fooInstantiatedMethod = instantiatedType.GetMethods().First(m => m.Name == "Foo");
            Assert.False(fooInstantiatedMethod.IsTypicalMethodDefinition);
            Assert.Equal(_context.GetWellKnownType(WellKnownType.Int32), fooInstantiatedMethod.Signature.ReturnType);
            Assert.Same(fooInstantiatedMethod.GetTypicalMethodDefinition(), fooMethod);
            // This is not a generic method, so they should be the same
            Assert.Same(fooInstantiatedMethod.GetMethodDefinition(), fooInstantiatedMethod);

            // Verify that instantiating a type definition has no effect
            TypeDesc newType = t.InstantiateSignature(new Instantiation(_context.GetWellKnownType(WellKnownType.Int32)), Instantiation.Empty);
            Assert.NotNull(newType);
            Assert.Same(newType, t);
        }

        [Fact]
        public void TestMethodAttributes()
        {
            MetadataType tGen = _testModule.GetType("GenericTypes", "GenericClass`1");
            MetadataType tDerivedGen = _testModule.GetType("GenericTypes", "DerivedGenericClass`1");
            InstantiatedType genOfInt = tGen.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));
            InstantiatedType derivedGenOfInt = tDerivedGen.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));
            MethodDesc fooInstantiatedMethod = genOfInt.GetMethods().First(m => m.Name == "Foo");
            MethodDesc barInstantiatedMethod = genOfInt.GetMethods().First(m => m.Name == "Bar");
            MethodDesc fooDerivedInstantiatedMethod = derivedGenOfInt.GetMethods().First(m => m.Name == "Foo");

            Assert.False(barInstantiatedMethod.IsVirtual);
            Assert.False(barInstantiatedMethod.IsNewSlot);
            Assert.False(barInstantiatedMethod.IsFinal);
            Assert.False(barInstantiatedMethod.IsAbstract);

            Assert.True(fooInstantiatedMethod.IsVirtual);
            Assert.True(fooInstantiatedMethod.IsNewSlot);
            Assert.False(fooInstantiatedMethod.IsFinal);
            Assert.True(fooInstantiatedMethod.IsAbstract);

            Assert.True(fooDerivedInstantiatedMethod.IsVirtual);
            Assert.False(fooDerivedInstantiatedMethod.IsNewSlot);
            Assert.True(fooDerivedInstantiatedMethod.IsFinal);
            Assert.False(fooDerivedInstantiatedMethod.IsAbstract);
        }

        [Fact]
        public void TestFinalize()
        {
            MetadataType tGen = _testModule.GetType("GenericTypes", "GenericClass`1");
            MetadataType tDerivedGen = _testModule.GetType("GenericTypes", "DerivedGenericClass`1");
            InstantiatedType genOfInt = tGen.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));
            InstantiatedType derivedGenOfInt = tDerivedGen.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));
            MethodDesc finalizeInstantiatedMethod = genOfInt.GetMethods().First(m => m.Name == "Finalize");

            Assert.Equal(finalizeInstantiatedMethod, genOfInt.GetFinalizer());
            Assert.Equal(finalizeInstantiatedMethod, derivedGenOfInt.GetFinalizer());
        }

        /// <summary>
        /// Testing lookup up of a method in an instantiated type.
        /// </summary>
        [Fact]
        public void TestMethodLookup()
        {
            MetadataType t = _testModule.GetType("GenericTypes", "GenericClass`1").MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));

            MethodSignature sig = new MethodSignature(MethodSignatureFlags.None, 0, _context.GetSignatureVariable(0, false), System.Array.Empty<TypeDesc>());
            MethodDesc fooMethod = t.GetMethod("Foo", sig);
            Assert.NotNull(fooMethod);
        }

        [Fact]
        public void TestConstructedTypeAdjustment()
        {
            TypeDesc intType = _context.GetWellKnownType(WellKnownType.Int32);
            TypeDesc stringType = _context.GetWellKnownType(WellKnownType.String);
            TypeDesc charType = _context.GetWellKnownType(WellKnownType.Char);
            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);

            MetadataType genericOpenType = _testModule.GetType("GenericTypes", "TwoParamGenericClass`2");

            InstantiatedType genericOfCharObject = genericOpenType.MakeInstantiatedType(charType, objectType);
            InstantiatedType genericOfCharString = genericOpenType.MakeInstantiatedType(charType, stringType);
            InstantiatedType genericOfIntString = genericOpenType.MakeInstantiatedType(intType, stringType);
            InstantiatedType genericOfIntObject = genericOpenType.MakeInstantiatedType(intType, objectType);

            Assert.True(genericOfCharObject.IsConstructedOverType(new TypeDesc[] { charType }));
            Assert.True(genericOfCharObject.IsConstructedOverType(new TypeDesc[] { objectType }));
            Assert.False(genericOfCharObject.IsConstructedOverType(new TypeDesc[] { intType }));
            Assert.False(genericOfCharObject.IsConstructedOverType(new TypeDesc[] { stringType }));
            Assert.False(genericOfCharObject.IsConstructedOverType(new TypeDesc[] { genericOpenType }));

            Assert.True(genericOfCharString.IsConstructedOverType(new TypeDesc[] { charType }));
            Assert.False(genericOfCharString.IsConstructedOverType(new TypeDesc[] { objectType }));
            Assert.False(genericOfCharString.IsConstructedOverType(new TypeDesc[] { intType }));
            Assert.True(genericOfCharString.IsConstructedOverType(new TypeDesc[] { stringType }));

            // Test direct replacement
            TypeDesc testDirectReplaceAllTypes = genericOfCharObject.ReplaceTypesInConstructionOfType(new TypeDesc[] { charType, objectType }, new TypeDesc[] { intType, stringType });
            Assert.Equal(genericOfIntString, testDirectReplaceAllTypes);

            // Test direct replacement where not all types are replaced
            TypeDesc testDirectReplaceFirstType = genericOfCharObject.ReplaceTypesInConstructionOfType(new TypeDesc[] { charType }, new TypeDesc[] { intType });
            Assert.Equal(genericOfIntObject, testDirectReplaceFirstType);

            TypeDesc testDirectReplaceSecondType = genericOfCharObject.ReplaceTypesInConstructionOfType(new TypeDesc[] { objectType }, new TypeDesc[] { stringType });
            Assert.Equal(genericOfCharString, testDirectReplaceSecondType);

            // Test Arrays
            TypeDesc arrayChar = _context.GetArrayType(charType);
            Assert.False(arrayChar.IsMdArray);
            Assert.True(arrayChar.IsSzArray);
            Assert.True(arrayChar.IsArray);

            TypeDesc arrayInt = _context.GetArrayType(intType);
            Assert.False(arrayInt.IsMdArray);
            Assert.True(arrayInt.IsSzArray);
            Assert.True(arrayInt.IsArray);

            InstantiatedType genericOfCharArrayObject = genericOpenType.MakeInstantiatedType(arrayChar, objectType);
            InstantiatedType genericOfIntArrayObject = genericOpenType.MakeInstantiatedType(arrayInt, objectType);
            TypeDesc testReplaceTypeInArrayInGeneric = genericOfCharArrayObject.ReplaceTypesInConstructionOfType(new TypeDesc[] { charType }, new TypeDesc[] { intType });
            Assert.Equal(genericOfIntArrayObject, testReplaceTypeInArrayInGeneric);

            // Test multidimensional arrays
            TypeDesc mdArrayChar = _context.GetArrayType(charType, 3);
            Assert.True(mdArrayChar.IsMdArray);
            Assert.False(mdArrayChar.IsSzArray);
            Assert.True(mdArrayChar.IsArray);

            TypeDesc mdArrayInt = _context.GetArrayType(intType, 3);
            Assert.True(mdArrayInt.IsMdArray);
            Assert.False(mdArrayInt.IsSzArray);
            Assert.True(mdArrayInt.IsArray);

            InstantiatedType genericOfCharMdArrayObject = genericOpenType.MakeInstantiatedType(mdArrayChar, objectType);
            InstantiatedType genericOfIntMdArrayObject = genericOpenType.MakeInstantiatedType(mdArrayInt, objectType);
            TypeDesc testReplaceTypeInMdArrayInGeneric = genericOfCharMdArrayObject.ReplaceTypesInConstructionOfType(new TypeDesc[] { charType }, new TypeDesc[] { intType });
            Assert.Equal(genericOfIntMdArrayObject, testReplaceTypeInMdArrayInGeneric);

            // Test pointers
            TypeDesc charPointer = _context.GetPointerType(charType);
            TypeDesc intPointer = _context.GetPointerType(intType);
            TypeDesc testReplaceTypeInPointer = charPointer.ReplaceTypesInConstructionOfType(new TypeDesc[] { charType }, new TypeDesc[] { intType });
            Assert.Equal(intPointer, testReplaceTypeInPointer);

            Assert.True(charPointer.IsConstructedOverType(new TypeDesc[] { charType }));
            Assert.False(charPointer.IsConstructedOverType(new TypeDesc[] { intType }));

            // Test byref
            TypeDesc charByRef = _context.GetByRefType(charType);
            TypeDesc intByRef = _context.GetByRefType(intType);
            TypeDesc testReplaceTypeInByRef = charByRef.ReplaceTypesInConstructionOfType(new TypeDesc[] { charType }, new TypeDesc[] { intType });
            Assert.Equal(intByRef, testReplaceTypeInByRef);

            Assert.True(charByRef.IsConstructedOverType(new TypeDesc[] { charType }));
            Assert.False(charByRef.IsConstructedOverType(new TypeDesc[] { intType }));

            // Test replace type entirely
            TypeDesc testReplaceTypeEntirely = charByRef.ReplaceTypesInConstructionOfType(new TypeDesc[] { charByRef }, new TypeDesc[] { intByRef });
            Assert.Equal(intByRef, testReplaceTypeEntirely);
            Assert.True(charByRef.IsConstructedOverType(new TypeDesc[] { charByRef }));
        }

        [Fact]
        public void TestConstructedMethodAdjustment()
        {
            TypeDesc intType = _context.GetWellKnownType(WellKnownType.Int32);
            TypeDesc stringType = _context.GetWellKnownType(WellKnownType.String);
            TypeDesc charType = _context.GetWellKnownType(WellKnownType.Char);
            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);

            MetadataType genericOpenType = _testModule.GetType("GenericTypes", "TwoParamGenericClass`2");
            MetadataType nonGenericType = _testModule.GetType("GenericTypes", "NonGenericClass");

            MethodDesc genericOnNonGeneric = nonGenericType.GetMethod("GenericFunction", null);

            InstantiatedType genericIntString = genericOpenType.MakeInstantiatedType(intType, stringType);
            InstantiatedType genericCharString = genericOpenType.MakeInstantiatedType(charType, stringType);
            InstantiatedType genericCharObject = genericOpenType.MakeInstantiatedType(charType, objectType);

            MethodDesc nonGenericOnGenericIntString = genericIntString.GetMethod("NonGenericFunction", null);
            MethodDesc nonGenericOnGenericCharString = genericCharString.GetMethod("NonGenericFunction", null);
            MethodDesc nonGenericOnGenericCharObject = genericCharObject.GetMethod("NonGenericFunction", null);

            MethodDesc genericIntStringOnGenericIntString = genericIntString.GetMethod("GenericFunction", null).MakeInstantiatedMethod(intType, stringType);
            MethodDesc genericCharStringOnGenericCharString = genericCharString.GetMethod("GenericFunction", null).MakeInstantiatedMethod(charType, stringType);
            MethodDesc genericCharObjectOnGenericCharObject = genericCharObject.GetMethod("GenericFunction", null).MakeInstantiatedMethod(charType, objectType);

            MethodDesc genericIntStringOnNonGeneric = genericOnNonGeneric.MakeInstantiatedMethod(intType, stringType);
            MethodDesc genericCharStringOnNonGeneric = genericOnNonGeneric.MakeInstantiatedMethod(charType, stringType);
            MethodDesc genericCharObjectOnNonGeneric = genericOnNonGeneric.MakeInstantiatedMethod(charType, objectType);

            // Test complete replacement
            MethodDesc testDirectReplacementNonGenericOnGeneric = nonGenericOnGenericIntString.ReplaceTypesInConstructionOfMethod(new TypeDesc[] { intType, stringType }, new TypeDesc[] { charType, objectType });
            Assert.Equal(nonGenericOnGenericCharObject, testDirectReplacementNonGenericOnGeneric);
            MethodDesc testDirectReplacementGenericOnGeneric = genericIntStringOnGenericIntString.ReplaceTypesInConstructionOfMethod(new TypeDesc[] { intType, stringType }, new TypeDesc[] { charType, objectType });
            Assert.Equal(genericCharObjectOnGenericCharObject, testDirectReplacementGenericOnGeneric);
            MethodDesc testDirectReplacementGenericOnNonGeneric = genericIntStringOnNonGeneric.ReplaceTypesInConstructionOfMethod(new TypeDesc[] { intType, stringType }, new TypeDesc[] { charType, objectType });
            Assert.Equal(genericCharObjectOnNonGeneric, testDirectReplacementGenericOnNonGeneric);

            // Test replace first type in instantiation
            MethodDesc testPartialReplacementNonGenericOnGeneric = nonGenericOnGenericIntString.ReplaceTypesInConstructionOfMethod(new TypeDesc[] { intType }, new TypeDesc[] { charType });
            Assert.Equal(nonGenericOnGenericCharString, testPartialReplacementNonGenericOnGeneric);
            MethodDesc testPartialReplacementGenericOnGeneric = genericIntStringOnGenericIntString.ReplaceTypesInConstructionOfMethod(new TypeDesc[] { intType }, new TypeDesc[] { charType });
            Assert.Equal(genericCharStringOnGenericCharString, testPartialReplacementGenericOnGeneric);
            MethodDesc testPartialReplacementGenericOnNonGeneric = genericIntStringOnNonGeneric.ReplaceTypesInConstructionOfMethod(new TypeDesc[] { intType }, new TypeDesc[] { charType });
            Assert.Equal(genericCharStringOnNonGeneric, testPartialReplacementGenericOnNonGeneric);

            // Test ArrayMethod case
            ArrayType mdArrayChar = _context.GetArrayType(charType, 3);
            ArrayType mdArrayInt = _context.GetArrayType(intType, 3);

            MethodDesc getMethodOnMDIntArray = mdArrayInt.GetArrayMethod(ArrayMethodKind.Get);
            MethodDesc getMethodOnMDCharArray = mdArrayChar.GetArrayMethod(ArrayMethodKind.Get);

            MethodDesc testArrayMethodCase = getMethodOnMDIntArray.ReplaceTypesInConstructionOfMethod(new TypeDesc[] { intType }, new TypeDesc[] { charType });
            Assert.Equal(getMethodOnMDCharArray, testArrayMethodCase);
        }
    }
}
