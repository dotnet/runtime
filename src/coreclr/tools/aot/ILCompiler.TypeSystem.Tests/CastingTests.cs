// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class CastingTests
    {
        private TestTypeSystemContext _context;
        private ModuleDesc _testModule;

        public CastingTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;
        }

        [Fact]
        public void TestCastingInHierarchy()
        {
            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);
            TypeDesc stringType = _context.GetWellKnownType(WellKnownType.String);
            TypeDesc intType = _context.GetWellKnownType(WellKnownType.Int32);
            TypeDesc uintType = _context.GetWellKnownType(WellKnownType.UInt32);

            Assert.True(stringType.CanCastTo(objectType));
            Assert.True(objectType.CanCastTo(objectType));
            Assert.True(intType.CanCastTo(objectType));

            Assert.False(objectType.CanCastTo(stringType));
            Assert.False(intType.CanCastTo(uintType));
            Assert.False(uintType.CanCastTo(intType));
        }

        [Fact]
        public void TestInterfaceCasting()
        {
            TypeDesc iFooType = _testModule.GetType("Casting", "IFoo");
            TypeDesc classImplementingIFooType =
                _testModule.GetType("Casting", "ClassImplementingIFoo");
            TypeDesc classImplementingIFooIndirectlyType =
                _testModule.GetType("Casting", "ClassImplementingIFooIndirectly");
            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);

            Assert.True(classImplementingIFooType.CanCastTo(iFooType));
            Assert.True(classImplementingIFooIndirectlyType.CanCastTo(iFooType));
            Assert.True(iFooType.CanCastTo(objectType));

            Assert.False(objectType.CanCastTo(iFooType));
        }

        [Fact]
        public void TestSameSizeArrayTypeCasting()
        {
            TypeDesc intType = _context.GetWellKnownType(WellKnownType.Int32);
            TypeDesc uintType = _context.GetWellKnownType(WellKnownType.UInt32);
            TypeDesc byteType = _context.GetWellKnownType(WellKnownType.Byte);
            TypeDesc sbyteType = _context.GetWellKnownType(WellKnownType.SByte);
            TypeDesc intPtrType = _context.GetWellKnownType(WellKnownType.IntPtr);
            TypeDesc ulongType = _context.GetWellKnownType(WellKnownType.UInt64);

            TypeDesc doubleType = _context.GetWellKnownType(WellKnownType.Double);
            TypeDesc boolType = _context.GetWellKnownType(WellKnownType.Boolean);

            TypeDesc intBasedEnumType = _testModule.GetType("Casting", "IntBasedEnum");
            TypeDesc uintBasedEnumType = _testModule.GetType("Casting", "UIntBasedEnum");
            TypeDesc shortBasedEnumType = _testModule.GetType("Casting", "ShortBasedEnum");

            Assert.True(intType.MakeArrayType().CanCastTo(uintType.MakeArrayType()));
            Assert.True(intType.MakeArrayType().CanCastTo(uintType.MakeArrayType(1)));
            Assert.False(intType.CanCastTo(uintType));

            Assert.True(byteType.MakeArrayType().CanCastTo(sbyteType.MakeArrayType()));
            Assert.False(byteType.CanCastTo(sbyteType));

            Assert.False(intPtrType.MakeArrayType().CanCastTo(ulongType.MakeArrayType()));
            Assert.False(intPtrType.CanCastTo(ulongType));

            // These are same size, but not allowed to cast
            Assert.False(doubleType.MakeArrayType().CanCastTo(ulongType.MakeArrayType()));
            Assert.False(boolType.MakeArrayType().CanCastTo(byteType.MakeArrayType()));

            Assert.True(intBasedEnumType.MakeArrayType().CanCastTo(uintType.MakeArrayType()));
            Assert.True(intBasedEnumType.MakeArrayType().CanCastTo(uintBasedEnumType.MakeArrayType()));
            Assert.False(shortBasedEnumType.MakeArrayType().CanCastTo(intType.MakeArrayType()));
        }

        [Fact]
        public void TestArrayInterfaceCasting()
        {
            TypeDesc intType = _context.GetWellKnownType(WellKnownType.Int32);
            TypeDesc byteType = _context.GetWellKnownType(WellKnownType.Byte);
            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);
            TypeDesc stringType = _context.GetWellKnownType(WellKnownType.String);
            TypeDesc intBasedEnumType = _testModule.GetType("Casting", "IntBasedEnum");
            MetadataType iListType = _context.SystemModule.GetType("System.Collections", "IList");
            MetadataType iListOfTType = _context.SystemModule.GetType("System.Collections.Generic", "IList`1");

            InstantiatedType iListOfIntType = iListOfTType.MakeInstantiatedType(intType);
            InstantiatedType iListOfObjectType = iListOfTType.MakeInstantiatedType(objectType);
            InstantiatedType iListOfStringType = iListOfTType.MakeInstantiatedType(stringType);
            TypeDesc intSzArrayType = intType.MakeArrayType();
            TypeDesc byteSzArrayType = byteType.MakeArrayType();
            TypeDesc objectSzArrayType = objectType.MakeArrayType();
            TypeDesc stringSzArrayType = stringType.MakeArrayType();
            TypeDesc intArrayType = intType.MakeArrayType(1);
            TypeDesc intBasedEnumSzArrayType = intBasedEnumType.MakeArrayType();

            Assert.True(intSzArrayType.CanCastTo(iListOfIntType));
            Assert.True(intSzArrayType.CanCastTo(iListType));

            Assert.False(intArrayType.CanCastTo(iListOfIntType));
            Assert.True(intArrayType.CanCastTo(iListType));

            Assert.True(intBasedEnumSzArrayType.CanCastTo(iListOfIntType));
            Assert.False(byteSzArrayType.CanCastTo(iListOfIntType));

            Assert.True(stringSzArrayType.CanCastTo(iListOfObjectType));
            Assert.True(stringSzArrayType.CanCastTo(iListOfStringType));
            Assert.True(objectSzArrayType.CanCastTo(iListOfObjectType));
            Assert.False(objectSzArrayType.CanCastTo(iListOfStringType));
        }

        [Fact]
        public void TestArrayCasting()
        {
            TypeDesc intType = _context.GetWellKnownType(WellKnownType.Int32);
            TypeDesc stringType = _context.GetWellKnownType(WellKnownType.String);
            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);
            TypeDesc arrayType = _context.GetWellKnownType(WellKnownType.Array);
            TypeDesc intSzArrayType = intType.MakeArrayType();
            TypeDesc intArray1Type = intType.MakeArrayType(1);
            TypeDesc intArray2Type = intType.MakeArrayType(2);
            TypeDesc stringSzArrayType = stringType.MakeArrayType();
            TypeDesc objectSzArrayType = objectType.MakeArrayType();

            Assert.True(intSzArrayType.CanCastTo(intArray1Type));
            Assert.False(intArray1Type.CanCastTo(intSzArrayType));

            Assert.False(intArray1Type.CanCastTo(intArray2Type));

            Assert.True(intSzArrayType.CanCastTo(arrayType));
            Assert.True(intArray1Type.CanCastTo(arrayType));

            Assert.True(stringSzArrayType.CanCastTo(objectSzArrayType));
            Assert.False(intSzArrayType.CanCastTo(objectSzArrayType));
        }

        [Fact]
        public void TestGenericParameterCasting()
        {
            TypeDesc paramWithNoConstraint =
                _testModule.GetType("Casting", "ClassWithNoConstraint`1").Instantiation[0];
            TypeDesc paramWithValueTypeConstraint =
                _testModule.GetType("Casting", "ClassWithValueTypeConstraint`1").Instantiation[0];
            TypeDesc paramWithInterfaceConstraint =
                _testModule.GetType("Casting", "ClassWithInterfaceConstraint`1").Instantiation[0];

            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);
            TypeDesc valueTypeType = _context.GetWellKnownType(WellKnownType.ValueType);
            TypeDesc iFooType = _testModule.GetType("Casting", "IFoo");
            TypeDesc classImplementingIFooType = _testModule.GetType("Casting", "ClassImplementingIFoo");

            Assert.True(paramWithNoConstraint.CanCastTo(objectType));
            Assert.False(paramWithNoConstraint.CanCastTo(valueTypeType));
            Assert.False(paramWithNoConstraint.CanCastTo(iFooType));
            Assert.False(paramWithNoConstraint.CanCastTo(classImplementingIFooType));

            Assert.True(paramWithValueTypeConstraint.CanCastTo(objectType));
            Assert.True(paramWithValueTypeConstraint.CanCastTo(valueTypeType));
            Assert.False(paramWithValueTypeConstraint.CanCastTo(iFooType));
            Assert.False(paramWithValueTypeConstraint.CanCastTo(classImplementingIFooType));

            Assert.True(paramWithInterfaceConstraint.CanCastTo(objectType));
            Assert.False(paramWithInterfaceConstraint.CanCastTo(valueTypeType));
            Assert.True(paramWithInterfaceConstraint.CanCastTo(iFooType));
            Assert.False(paramWithInterfaceConstraint.CanCastTo(classImplementingIFooType));
        }

        [Fact]
        public void TestVariantCasting()
        {
            TypeDesc stringType = _context.GetWellKnownType(WellKnownType.String);
            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);
            TypeDesc exceptionType = _context.GetWellKnownType(WellKnownType.Exception);

            TypeDesc stringSzArrayType = stringType.MakeArrayType();

            MetadataType iEnumerableOfTType =
                _context.SystemModule.GetType("System.Collections.Generic", "IEnumerable`1");
            InstantiatedType iEnumerableOfObjectType = iEnumerableOfTType.MakeInstantiatedType(objectType);
            InstantiatedType iEnumerableOfExceptionType = iEnumerableOfTType.MakeInstantiatedType(exceptionType);

            Assert.True(stringSzArrayType.CanCastTo(iEnumerableOfObjectType));
            Assert.False(stringSzArrayType.CanCastTo(iEnumerableOfExceptionType));

            MetadataType iContravariantOfTType = _testModule.GetType("Casting", "IContravariant`1");
            InstantiatedType iContravariantOfObjectType = iContravariantOfTType.MakeInstantiatedType(objectType);
            InstantiatedType iEnumerableOfStringType = iEnumerableOfTType.MakeInstantiatedType(stringType);

            Assert.True(iContravariantOfObjectType.CanCastTo(objectType));
            Assert.True(iEnumerableOfStringType.CanCastTo(objectType));
        }

        [Fact]
        public void TestNullableCasting()
        {
            TypeDesc intType = _context.GetWellKnownType(WellKnownType.Int32);
            MetadataType nullableType = (MetadataType)_context.GetWellKnownType(WellKnownType.Nullable);
            TypeDesc nullableOfIntType = nullableType.MakeInstantiatedType(intType);

            Assert.True(intType.CanCastTo(nullableOfIntType));
        }

        [Fact]
        public void TestGenericParameterArrayCasting()
        {
            TypeDesc baseArrayType = _testModule.GetType("Casting", "Base").MakeArrayType();
            TypeDesc iFooArrayType = _testModule.GetType("Casting", "IFoo").MakeArrayType();

            TypeDesc paramArrayWithBaseClassConstraint =
                _testModule.GetType("Casting", "ClassWithBaseClassConstraint`1").Instantiation[0].MakeArrayType();
            TypeDesc paramArrayWithInterfaceConstraint =
                _testModule.GetType("Casting", "ClassWithInterfaceConstraint`1").Instantiation[0].MakeArrayType();

            Assert.True(paramArrayWithBaseClassConstraint.CanCastTo(baseArrayType));
            Assert.False(paramArrayWithInterfaceConstraint.CanCastTo(iFooArrayType));
        }

        [Fact]
        public void TestRecursiveCanCast()
        {
            // Tests the stack overflow protection in CanCastTo

            TypeDesc classWithRecursiveImplementation = _testModule.GetType("Casting", "ClassWithRecursiveImplementation");
            MetadataType iContravariantOfTType = (MetadataType)_testModule.GetType("Casting", "IContravariant`1");

            TypeDesc testType = iContravariantOfTType.MakeInstantiatedType(classWithRecursiveImplementation);

            Assert.False(classWithRecursiveImplementation.CanCastTo(testType));
        }
    }
}
