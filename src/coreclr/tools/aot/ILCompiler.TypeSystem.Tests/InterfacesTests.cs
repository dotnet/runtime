// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class InterfacesTests
    {
        TestTypeSystemContext _context;
        ModuleDesc _testModule;

        public InterfacesTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;
        }

        [Fact]
        public void TestMultidimensionalArrays()
        {
            DefType systemArrayType = _context.GetWellKnownType(WellKnownType.Array);
            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);

            ArrayType objectMDArray = _context.GetArrayType(objectType, 2);

            // MD array should have the same set of interfaces as System.Array
            Assert.Equal(systemArrayType.RuntimeInterfaces, objectMDArray.RuntimeInterfaces);
        }

        [Fact]
        public void TestSingleDimensionalArrays()
        {
            DefType systemArrayType = _context.GetWellKnownType(WellKnownType.Array);
            MetadataType systemIListOfTType = _testModule.GetType("System.Collections.Generic", "IList`1");

            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);

            ArrayType objectArray = _context.GetArrayType(objectType);

            // The set of interfaces on an array shall start with the same set that exists on System.Array
            for (int i = 0; i < systemArrayType.RuntimeInterfaces.Length; i++)
            {
                Assert.Equal(systemArrayType.RuntimeInterfaces[i], objectArray.RuntimeInterfaces[i]);
            }

            // The set of interfaces on an array of type T shall include IList<T>
            TypeDesc ilistOfObject = systemIListOfTType.MakeInstantiatedType(objectType);
            Assert.Contains(ilistOfObject, objectArray.RuntimeInterfaces);
        }

        [Fact]
        public void TestNoInterface()
        {
            MetadataType noInterfacesType = _testModule.GetType("InterfaceArrangements", "NoInterfaces");
            Assert.Empty(noInterfacesType.RuntimeInterfaces);
        }


        [Fact]
        public void TestOneInterface()
        {
            MetadataType oneInterfacesType = _testModule.GetType("InterfaceArrangements", "OneInterface");
            MetadataType i1Type = _testModule.GetType("InterfaceArrangements", "I1");

            Assert.Equal(new DefType[] { i1Type }, oneInterfacesType.RuntimeInterfaces);
        }

        [Fact]
        public void TestOverlappingInterfacesAtDerivation()
        {
            // This test tests that an explicit interface implementation on a type does not cause the
            // set of runtime interfaces to get extra duplication
            MetadataType derivedFromMidType = _testModule.GetType("InterfaceArrangements", "DerivedFromMid");
            MetadataType igen1Type = _testModule.GetType("InterfaceArrangements", "IGen1`1");
            TypeDesc stringType = _testModule.Context.GetWellKnownType(WellKnownType.String);
            DefType igen1OfString = igen1Type.MakeInstantiatedType(stringType);
            MetadataType i1Type = _testModule.GetType("InterfaceArrangements", "I1");

            Assert.Equal(new DefType[] { igen1OfString, i1Type, igen1OfString }, derivedFromMidType.RuntimeInterfaces);
        }

        [Fact]
        public void TestOverlappingGenericInterfaces()
        {
            // This test tests that the set of interfaces implemented on a generic type definition
            // has the same arrangement regardless of instantiation
            MetadataType midType = _testModule.GetType("InterfaceArrangements", "Mid`2");
            MetadataType igen1Type = _testModule.GetType("InterfaceArrangements", "IGen1`1");
            TypeDesc stringType = _testModule.Context.GetWellKnownType(WellKnownType.String);
            TypeDesc objectType = _testModule.Context.GetWellKnownType(WellKnownType.Object);
            DefType igen1OfString = igen1Type.MakeInstantiatedType(stringType);
            DefType igen1OfObject = igen1Type.MakeInstantiatedType(objectType);
            MetadataType i1Type = _testModule.GetType("InterfaceArrangements", "I1");

            TypeDesc mid_string_string = midType.MakeInstantiatedType(stringType, stringType);
            TypeDesc mid_string_object = midType.MakeInstantiatedType(stringType, objectType);
            TypeDesc mid_object_string = midType.MakeInstantiatedType(objectType, stringType);
            TypeDesc mid_object_object = midType.MakeInstantiatedType(objectType, objectType);

            Assert.Equal(new DefType[] { igen1OfString, i1Type, igen1OfString }, mid_string_string.RuntimeInterfaces);
            Assert.Equal(new DefType[] { igen1OfString, i1Type, igen1OfObject }, mid_string_object.RuntimeInterfaces);
            Assert.Equal(new DefType[] { igen1OfObject, i1Type, igen1OfString }, mid_object_string.RuntimeInterfaces);
            Assert.Equal(new DefType[] { igen1OfObject, i1Type, igen1OfObject }, mid_object_object.RuntimeInterfaces);
        }

        [Fact]
        public void TestInterfaceRequiresImplementation()
        {
            MetadataType i1Type = _testModule.GetType("InterfaceArrangements", "I1");
            MetadataType i2Type = _testModule.GetType("InterfaceArrangements", "I2");

            Assert.Empty(i1Type.RuntimeInterfaces);
            Assert.Equal(i1Type.ExplicitlyImplementedInterfaces, i1Type.RuntimeInterfaces);

            Assert.Equal(new DefType[] { i1Type }, i2Type.RuntimeInterfaces);
            Assert.Equal(i2Type.ExplicitlyImplementedInterfaces, i2Type.RuntimeInterfaces);
        }

        [Fact]
        public void TestPointerArrayInterfaces()
        {
            MetadataType systemArrayType = _testModule.GetType("System", "Array");
            TypeDesc intType = _context.GetWellKnownType(WellKnownType.Int32);
            TypeDesc intPointerType = _context.GetPointerType(intType);

            ArrayType intPointerArray = _context.GetArrayType(intPointerType);

            // Pointer arrays should have the same set of interfaces as System.Array
            Assert.Equal(systemArrayType.RuntimeInterfaces, intPointerArray.RuntimeInterfaces);
        }

        [Fact]
        public void TestInterafaceMethodResolution()
        {
            MetadataType fooType = _testModule.GetType("InterfaceArrangements", "Foo");
            MetadataType derivedType = _testModule.GetType("InterfaceArrangements", "DerivedFromFoo");
            MetadataType superDerivedType = _testModule.GetType("InterfaceArrangements", "SuperDerivedFromFoo");

            MetadataType ifooOfInt = _testModule.GetType("InterfaceArrangements", "IFoo`1").MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));
            MetadataType ifooOfString = _testModule.GetType("InterfaceArrangements", "IFoo`1").MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.String));

            MethodDesc ifooOfIntMethod = ifooOfInt.GetMethods().Where(m => m.Name == "IMethod").Single();
            MethodDesc ifooOfStringMethod = ifooOfString.GetMethods().Where(m => m.Name == "IMethod").Single();

            MethodDesc result;

            // Resolve on type Foo
            {
                result = fooType.ResolveInterfaceMethodTarget(ifooOfIntMethod);
                Assert.NotNull(result);
                Assert.Equal(result.OwningType, fooType);

                result = fooType.ResolveInterfaceMethodTarget(ifooOfStringMethod);
                Assert.NotNull(result);
                Assert.Equal(result.OwningType, fooType);
            }

            // Resolve on type DerivedFromFoo
            {
                result = derivedType.ResolveInterfaceMethodTarget(ifooOfIntMethod);
                Assert.NotNull(result);
                Assert.Equal(result.OwningType, fooType);

                result = derivedType.ResolveInterfaceMethodTarget(ifooOfStringMethod);
                Assert.NotNull(result);
                Assert.Equal(result.OwningType, derivedType);
            }


            // Resolve on type SuperDerivedFromFoo
            {
                result = superDerivedType.ResolveInterfaceMethodTarget(ifooOfIntMethod);
                Assert.NotNull(result);
                Assert.Equal(result.OwningType, superDerivedType);

                result = superDerivedType.ResolveInterfaceMethodTarget(ifooOfStringMethod);
                Assert.NotNull(result);
                Assert.Equal(result.OwningType, derivedType);
            }
        }
    }
}
