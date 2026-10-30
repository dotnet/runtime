// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class ConstraintsValidationTest
    {
        private TestTypeSystemContext _context;
        private ModuleDesc _testModule;

        private MetadataType _iNonGenType;
        private MetadataType _iGenType;
        private MetadataType _arg1Type;
        private MetadataType _arg2Type;
        private MetadataType _arg3Type;
        private MetadataType _structArgWithDefaultCtorType;
        private MetadataType _structArgWithoutDefaultCtorType;
        private MetadataType _classArgWithDefaultCtorType;
        private MetadataType _classArgWithPrivateDefaultCtorType;
        private MetadataType _abstractClassArgWithDefaultCtorType;
        private MetadataType _classArgWithoutDefaultCtorType;
        private MetadataType _referenceTypeConstraintType;
        private MetadataType _defaultConstructorConstraintType;
        private MetadataType _notNullableValueTypeConstraintType;
        private MetadataType _simpleTypeConstraintType;
        private MetadataType _doubleSimpleTypeConstraintType;
        private MetadataType _simpleGenericConstraintType;
        private MetadataType _complexGenericConstraint1Type;
        private MetadataType _complexGenericConstraint2Type;
        private MetadataType _complexGenericConstraint3Type;
        private MetadataType _complexGenericConstraint4Type;
        private MetadataType _multipleConstraintsType;

        private MetadataType _genericMethodsType;
        private MethodDesc _simpleGenericConstraintMethod;
        private MethodDesc _complexGenericConstraintMethod;

        public ConstraintsValidationTest()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.Unknown);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;

            _iNonGenType = _testModule.GetType("GenericConstraints"u8, "INonGen"u8);
            _iGenType = _testModule.GetType("GenericConstraints"u8, "IGen`1"u8);
            _arg1Type = _testModule.GetType("GenericConstraints"u8, "Arg1"u8);
            _arg2Type = _testModule.GetType("GenericConstraints"u8, "Arg2`1"u8);
            _arg3Type = _testModule.GetType("GenericConstraints"u8, "Arg3`1"u8);
            _structArgWithDefaultCtorType = _testModule.GetType("GenericConstraints"u8, "StructArgWithDefaultCtor"u8);
            _structArgWithoutDefaultCtorType = _testModule.GetType("GenericConstraints"u8, "StructArgWithoutDefaultCtor"u8);
            _classArgWithDefaultCtorType = _testModule.GetType("GenericConstraints"u8, "ClassArgWithDefaultCtor"u8);
            _classArgWithPrivateDefaultCtorType = _testModule.GetType("GenericConstraints"u8, "ClassArgWithPrivateDefaultCtor"u8);
            _abstractClassArgWithDefaultCtorType = _testModule.GetType("GenericConstraints"u8, "AbstractClassArgWithDefaultCtor"u8);
            _classArgWithoutDefaultCtorType = _testModule.GetType("GenericConstraints"u8, "ClassArgWithoutDefaultCtor"u8);

            _referenceTypeConstraintType = _testModule.GetType("GenericConstraints"u8, "ReferenceTypeConstraint`1"u8);
            _defaultConstructorConstraintType = _testModule.GetType("GenericConstraints"u8, "DefaultConstructorConstraint`1"u8);
            _notNullableValueTypeConstraintType = _testModule.GetType("GenericConstraints"u8, "NotNullableValueTypeConstraint`1"u8);
            _simpleTypeConstraintType = _testModule.GetType("GenericConstraints"u8, "SimpleTypeConstraint`1"u8);
            _doubleSimpleTypeConstraintType = _testModule.GetType("GenericConstraints"u8, "DoubleSimpleTypeConstraint`1"u8);
            _simpleGenericConstraintType = _testModule.GetType("GenericConstraints"u8, "SimpleGenericConstraint`2"u8);
            _complexGenericConstraint1Type = _testModule.GetType("GenericConstraints"u8, "ComplexGenericConstraint1`2"u8);
            _complexGenericConstraint2Type = _testModule.GetType("GenericConstraints"u8, "ComplexGenericConstraint2`2"u8);
            _complexGenericConstraint3Type = _testModule.GetType("GenericConstraints"u8, "ComplexGenericConstraint3`2"u8);
            _complexGenericConstraint4Type = _testModule.GetType("GenericConstraints"u8, "ComplexGenericConstraint4`2"u8);
            _multipleConstraintsType = _testModule.GetType("GenericConstraints"u8, "MultipleConstraints`2"u8);

            _genericMethodsType = _testModule.GetType("GenericConstraints"u8, "GenericMethods"u8);
            _simpleGenericConstraintMethod = _genericMethodsType.GetMethod("SimpleGenericConstraintMethod"u8, null);
            _complexGenericConstraintMethod = _genericMethodsType.GetMethod("ComplexGenericConstraintMethod"u8, null);
        }

        [Fact]
        public void TestTypeConstraints()
        {
            TypeDesc instantiatedType;
            MethodDesc instantiatedMethod;

            MetadataType arg2OfInt = _arg2Type.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));
            MetadataType arg2OfBool = _arg2Type.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Boolean));
            MetadataType arg2OfObject = _arg2Type.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Object));

            // ReferenceTypeConstraint
            {
                instantiatedType = _referenceTypeConstraintType.MakeInstantiatedType(_arg1Type);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _referenceTypeConstraintType.MakeInstantiatedType(_iNonGenType);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _referenceTypeConstraintType.MakeInstantiatedType(_structArgWithDefaultCtorType);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _referenceTypeConstraintType.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));
                Assert.False(instantiatedType.CheckConstraints());
            }

            // DefaultConstructorConstraint
            {
                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_arg1Type);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_classArgWithDefaultCtorType);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_classArgWithPrivateDefaultCtorType);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_abstractClassArgWithDefaultCtorType);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_classArgWithoutDefaultCtorType);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_structArgWithDefaultCtorType);
                Assert.True(instantiatedType.CheckConstraints());

                // Structs always have implicit default constructors
                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_structArgWithoutDefaultCtorType);
                Assert.True(instantiatedType.CheckConstraints());
            }

            // NotNullableValueTypeConstraint
            {
                instantiatedType = _notNullableValueTypeConstraintType.MakeInstantiatedType(_arg1Type);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _notNullableValueTypeConstraintType.MakeInstantiatedType(_structArgWithDefaultCtorType);
                Assert.True(instantiatedType.CheckConstraints());

                MetadataType nullable = (MetadataType)_context.GetWellKnownType(WellKnownType.Nullable);
                MetadataType nullableOfInt = nullable.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));

                instantiatedType = _notNullableValueTypeConstraintType.MakeInstantiatedType(nullableOfInt);
                Assert.False(instantiatedType.CheckConstraints());
            }

            // Special constraints instantiated with generic parameter
            {
                instantiatedType = _referenceTypeConstraintType.MakeInstantiatedType(_referenceTypeConstraintType.Instantiation[0]);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_defaultConstructorConstraintType.Instantiation[0]);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _notNullableValueTypeConstraintType.MakeInstantiatedType(_notNullableValueTypeConstraintType.Instantiation[0]);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_notNullableValueTypeConstraintType.Instantiation[0]);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _referenceTypeConstraintType.MakeInstantiatedType(_arg2Type.Instantiation[0]);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_arg2Type.Instantiation[0]);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _notNullableValueTypeConstraintType.MakeInstantiatedType(_arg2Type.Instantiation[0]);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _referenceTypeConstraintType.MakeInstantiatedType(_simpleTypeConstraintType.Instantiation[0]);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(_simpleTypeConstraintType.Instantiation[0]);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _notNullableValueTypeConstraintType.MakeInstantiatedType(_simpleTypeConstraintType.Instantiation[0]);
                Assert.False(instantiatedType.CheckConstraints());
            }

            // SimpleTypeConstraint and DoubleSimpleTypeConstraint
            foreach(var genType in new MetadataType[] { _simpleTypeConstraintType, _doubleSimpleTypeConstraintType })
            {
                instantiatedType = genType.MakeInstantiatedType(_arg1Type);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = genType.MakeInstantiatedType(_iNonGenType);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = genType.MakeInstantiatedType(_classArgWithDefaultCtorType);
                Assert.False(instantiatedType.CheckConstraints());
            }

            // SimpleGenericConstraint
            {
                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_arg1Type, _arg1Type);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_arg1Type, _iNonGenType);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_classArgWithDefaultCtorType, _classArgWithoutDefaultCtorType);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_arg1Type, _context.GetWellKnownType(WellKnownType.Object));
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_structArgWithDefaultCtorType, _context.GetWellKnownType(WellKnownType.ValueType));
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_arg1Type, _context.GetWellKnownType(WellKnownType.ValueType));
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.UInt16), _context.GetWellKnownType(WellKnownType.UInt32));
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.UInt16), _context.GetWellKnownType(WellKnownType.ValueType));
                Assert.True(instantiatedType.CheckConstraints());
            }

            // ComplexGenericConstraint1
            {
                instantiatedType = _complexGenericConstraint1Type.MakeInstantiatedType(_arg1Type, _arg1Type /* uninteresting */);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _complexGenericConstraint1Type.MakeInstantiatedType(arg2OfInt, _arg1Type /* uninteresting */);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _complexGenericConstraint1Type.MakeInstantiatedType(arg2OfBool, _arg1Type /* uninteresting */);
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _complexGenericConstraint1Type.MakeInstantiatedType(arg2OfObject, _arg1Type /* uninteresting */);
                Assert.False(instantiatedType.CheckConstraints());
            }

            // ComplexGenericConstraint2
            {
                MetadataType arg2OfArg2OfInt = _arg2Type.MakeInstantiatedType(arg2OfInt);
                MetadataType arg2OfArg2OfBool = _arg2Type.MakeInstantiatedType(arg2OfBool);
                MetadataType arg2OfArg2OfObject = _arg2Type.MakeInstantiatedType(arg2OfObject);

                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(_arg1Type, _context.GetWellKnownType(WellKnownType.Int32));
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfInt, _context.GetWellKnownType(WellKnownType.Int32));
                Assert.True(instantiatedType.CheckConstraints());
                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfBool, _context.GetWellKnownType(WellKnownType.Int32));
                Assert.False(instantiatedType.CheckConstraints());
                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfObject, _context.GetWellKnownType(WellKnownType.Int32));
                Assert.False(instantiatedType.CheckConstraints());

                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfInt, _context.GetWellKnownType(WellKnownType.Object));
                Assert.False(instantiatedType.CheckConstraints());
                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfBool, _context.GetWellKnownType(WellKnownType.Object));
                Assert.False(instantiatedType.CheckConstraints());
                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfObject, _context.GetWellKnownType(WellKnownType.Object));
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfInt, _context.GetWellKnownType(WellKnownType.Boolean));
                Assert.False(instantiatedType.CheckConstraints());
                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfBool, _context.GetWellKnownType(WellKnownType.Boolean));
                Assert.True(instantiatedType.CheckConstraints());
                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfObject, _context.GetWellKnownType(WellKnownType.Boolean));
                Assert.False(instantiatedType.CheckConstraints());
            }

            // ComplexGenericConstraint3
            {
                MetadataType igenOfObject = _iGenType.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Object));

                instantiatedType = _complexGenericConstraint3Type.MakeInstantiatedType(igenOfObject, _context.GetWellKnownType(WellKnownType.Object));
                Assert.True(instantiatedType.CheckConstraints());

                // Variance-compatible instantiation argument
                instantiatedType = _complexGenericConstraint3Type.MakeInstantiatedType(igenOfObject, _context.GetWellKnownType(WellKnownType.String));
                Assert.True(instantiatedType.CheckConstraints());

                // Type that implements the interface
                var arg3OfObject = _arg3Type.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Object));
                instantiatedType = _complexGenericConstraint3Type.MakeInstantiatedType(arg3OfObject, _context.GetWellKnownType(WellKnownType.Object));
                Assert.True(instantiatedType.CheckConstraints());

                // Type that implements a variant compatible interface
                instantiatedType = _complexGenericConstraint3Type.MakeInstantiatedType(arg3OfObject, _context.GetWellKnownType(WellKnownType.String));
                Assert.True(instantiatedType.CheckConstraints());
            }

            // Constraints requiring InstantiationContext
            {
                // Instantiate type / method with own generic parameters
                instantiatedType = _complexGenericConstraint3Type.MakeInstantiatedType(_complexGenericConstraint3Type.Instantiation[0], _complexGenericConstraint3Type.Instantiation[1]);
                Assert.True(instantiatedType.CheckConstraints(new InstantiationContext(instantiatedType.Instantiation, default(Instantiation))));

                instantiatedType = _complexGenericConstraint4Type.MakeInstantiatedType(_complexGenericConstraint4Type.Instantiation[0], _complexGenericConstraint4Type.Instantiation[1]);
                Assert.True(instantiatedType.CheckConstraints(new InstantiationContext(instantiatedType.Instantiation, default(Instantiation))));

                instantiatedMethod = _simpleGenericConstraintMethod.MakeInstantiatedMethod(_simpleGenericConstraintMethod.Instantiation);
                Assert.True(instantiatedMethod.CheckConstraints(new InstantiationContext(default(Instantiation), instantiatedMethod.Instantiation)));

                instantiatedMethod = _complexGenericConstraintMethod.MakeInstantiatedMethod(_complexGenericConstraintMethod.Instantiation);
                Assert.True(instantiatedMethod.CheckConstraints(new InstantiationContext(default(Instantiation), instantiatedMethod.Instantiation)));

                // Instantiate type with generic parameters of method
                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_simpleGenericConstraintMethod.Instantiation);
                Assert.True(instantiatedType.CheckConstraints(new InstantiationContext(default(Instantiation), _simpleGenericConstraintMethod.Instantiation)));

                instantiatedType = _complexGenericConstraint4Type.MakeInstantiatedType(_complexGenericConstraintMethod.Instantiation);
                Assert.True(instantiatedType.CheckConstraints(new InstantiationContext(default(Instantiation), _complexGenericConstraintMethod.Instantiation)));

                // Instantiate method with generic parameters of type
                instantiatedMethod = _simpleGenericConstraintMethod.MakeInstantiatedMethod(_simpleGenericConstraintType.Instantiation);
                Assert.True(instantiatedMethod.CheckConstraints(new InstantiationContext(_simpleGenericConstraintType.Instantiation, default(Instantiation))));

                instantiatedMethod = _complexGenericConstraintMethod.MakeInstantiatedMethod(_complexGenericConstraint4Type.Instantiation);
                Assert.True(instantiatedMethod.CheckConstraints(new InstantiationContext(_complexGenericConstraint4Type.Instantiation, default(Instantiation))));
            }

            // MultipleConstraints
            {
                // Violate the class constraint
                instantiatedType = _multipleConstraintsType.MakeInstantiatedType(_structArgWithDefaultCtorType, _context.GetWellKnownType(WellKnownType.Object));
                Assert.False(instantiatedType.CheckConstraints());

                // Violate the new() constraint
                instantiatedType = _multipleConstraintsType.MakeInstantiatedType(_classArgWithoutDefaultCtorType, _context.GetWellKnownType(WellKnownType.Object));
                Assert.False(instantiatedType.CheckConstraints());

                // Violate the IGen<U> constraint
                instantiatedType = _multipleConstraintsType.MakeInstantiatedType(_arg1Type, _context.GetWellKnownType(WellKnownType.Object));
                Assert.False(instantiatedType.CheckConstraints());

                // Satisfy all constraints
                instantiatedType = _multipleConstraintsType.MakeInstantiatedType(_classArgWithDefaultCtorType, _context.GetWellKnownType(WellKnownType.Object));
                Assert.True(instantiatedType.CheckConstraints());
            }

            // InvalidInstantiationArgs
            {
                var pointer = _context.GetWellKnownType(WellKnownType.Int16).MakePointerType();
                var byref = _context.GetWellKnownType(WellKnownType.Int16).MakeByRefType();

                Assert.False(_iGenType.Instantiation.CheckValidInstantiationArguments());

                instantiatedType = _iGenType.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Void));
                Assert.False(instantiatedType.Instantiation.CheckValidInstantiationArguments());

                instantiatedType = _iGenType.MakeInstantiatedType(pointer);
                Assert.False(instantiatedType.Instantiation.CheckValidInstantiationArguments());

                instantiatedType = _iGenType.MakeInstantiatedType(byref);
                Assert.False(instantiatedType.Instantiation.CheckValidInstantiationArguments());

                instantiatedType = _iGenType.MakeInstantiatedType(byref);
                instantiatedType = _iGenType.MakeInstantiatedType(instantiatedType);
                Assert.False(instantiatedType.Instantiation.CheckValidInstantiationArguments());
            }
        }

        [Fact]
        public void TestCanonicalTypeConstraints()
        {
            TypeDesc canon = _context.CanonType;
            TypeDesc universalCanon = _context.UniversalCanonType;
            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);
            TypeDesc stringType = _context.GetWellKnownType(WellKnownType.String);
            TypeDesc intType = _context.GetWellKnownType(WellKnownType.Int32);

            MetadataType nonVariantInterfaceConstraintType = _testModule.GetType("GenericConstraints"u8, "NonVariantInterfaceConstraint`2"u8);
            MetadataType nonVariantGenImplType = _testModule.GetType("GenericConstraints"u8, "NonVariantGenImpl`1"u8);

            TypeDesc instantiatedType;

            // __Canon satisfies special constraints: class, new()
            {
                instantiatedType = _referenceTypeConstraintType.MakeInstantiatedType(canon);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(canon);
                Assert.True(instantiatedType.CheckConstraints());

                // __Canon should NOT satisfy struct constraint
                instantiatedType = _notNullableValueTypeConstraintType.MakeInstantiatedType(canon);
                Assert.False(instantiatedType.CheckConstraints());
            }

            // __UniversalCanon satisfies all special constraints
            {
                instantiatedType = _referenceTypeConstraintType.MakeInstantiatedType(universalCanon);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _defaultConstructorConstraintType.MakeInstantiatedType(universalCanon);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _notNullableValueTypeConstraintType.MakeInstantiatedType(universalCanon);
                Assert.True(instantiatedType.CheckConstraints());
            }

            // __Canon as instantiation param satisfies type constraints (wildcard — runtime will validate)
            {
                // NonVariantInterfaceConstraint<__Canon, object>: __Canon is wildcard, passes any type constraint
                instantiatedType = nonVariantInterfaceConstraintType.MakeInstantiatedType(canon, objectType);
                Assert.True(instantiatedType.CheckConstraints());

                // ComplexGenericConstraint3<__Canon, object>: where T : IGen<U>, T=__Canon, U=object
                instantiatedType = _complexGenericConstraint3Type.MakeInstantiatedType(canon, objectType);
                Assert.True(instantiatedType.CheckConstraints());
            }

            // Invariant interface constraint with __Canon in the constraint's type args
            // NonVariantGenImpl<string> : INonVariantGen<string>
            // NonVariantInterfaceConstraint<T, U> where T : INonVariantGen<U>
            // Check: NonVariantInterfaceConstraint<NonVariantGenImpl<string>, __Canon>
            //   constraint becomes INonVariantGen<__Canon>, concrete implements INonVariantGen<string>
            //   string is a ref type, __Canon matches ref types
            {
                TypeDesc nonVariantGenImplOfString = nonVariantGenImplType.MakeInstantiatedType(stringType);
                instantiatedType = nonVariantInterfaceConstraintType.MakeInstantiatedType(nonVariantGenImplOfString, canon);
                Assert.True(instantiatedType.CheckConstraints());

                // With int (value type) — __Canon should NOT match value types
                TypeDesc nonVariantGenImplOfInt = nonVariantGenImplType.MakeInstantiatedType(intType);
                instantiatedType = nonVariantInterfaceConstraintType.MakeInstantiatedType(nonVariantGenImplOfInt, canon);
                Assert.False(instantiatedType.CheckConstraints());
            }

            // Variant interface constraint with __Canon in the constraint's type args
            // ComplexGenericConstraint3<T, U> where T : IGen<U>  (IGen<in T> is contravariant)
            // Arg3<object> : IGen<object>
            // ComplexGenericConstraint3<Arg3<object>, __Canon>
            //   constraint: IGen<__Canon>. Arg3<object> implements IGen<object>.
            //   __Canon matches object (ref type) in invariant arg position of IGen
            {
                TypeDesc arg3OfObject = _arg3Type.MakeInstantiatedType(objectType);
                instantiatedType = _complexGenericConstraint3Type.MakeInstantiatedType(arg3OfObject, canon);
                Assert.True(instantiatedType.CheckConstraints());
            }

            // Base type constraint with __Canon
            // ComplexGenericConstraint2<T, U> where T : Arg2<Arg2<U>>
            // Arg2<Arg2<__Canon>> — constraint has canonical subtype
            // T=Arg2<Arg2<string>> should match because Arg2<string> canonicalizes to Arg2<__Canon>
            {
                TypeDesc arg2OfString = _arg2Type.MakeInstantiatedType(stringType);
                TypeDesc arg2OfArg2OfString = _arg2Type.MakeInstantiatedType(arg2OfString);
                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfString, canon);
                Assert.True(instantiatedType.CheckConstraints());

                // Value type should not match __Canon in base type constraint
                TypeDesc arg2OfInt = _arg2Type.MakeInstantiatedType(intType);
                TypeDesc arg2OfArg2OfInt = _arg2Type.MakeInstantiatedType(arg2OfInt);
                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfInt, canon);
                Assert.False(instantiatedType.CheckConstraints());
            }

            // Parameterized canonical types (e.g., __Canon[] as type arg in constraint)
            // ComplexGenericConstraint3<T, U> where T : IGen<U>  (IGen<in T>)
            // T=IGen<int[]>, U=int[] : IGen<int[]> implements IGen<int[]>, passes normally.
            // Canonicalized: T becomes __Canon (ref type), U=int[] stays.
            // Check: __Canon satisfies IGen<int[]>? __Canon is wildcard → true.
            {
                TypeDesc intArray = intType.MakeArrayType();
                instantiatedType = _complexGenericConstraint3Type.MakeInstantiatedType(canon, intArray);
                Assert.True(instantiatedType.CheckConstraints());
            }

            // Variance + __Canon interaction: MultipleConstraints<T, U> where T : class, IGen<U>, new()
            // MultipleConstraints<ClassArgWithDefaultCtor, __Canon>
            //   ClassArgWithDefaultCtor : IGen<object>
            //   constraint: IGen<__Canon>, __Canon matches object
            {
                instantiatedType = _multipleConstraintsType.MakeInstantiatedType(_classArgWithDefaultCtorType, canon);
                Assert.True(instantiatedType.CheckConstraints());
            }

            // Interface type used directly as instantiation param
            // ComplexGenericConstraint3<IGen<string>, __Canon>: IGen<string> satisfies IGen<__Canon>
            {
                TypeDesc igenOfString = _iGenType.MakeInstantiatedType(stringType);
                instantiatedType = _complexGenericConstraint3Type.MakeInstantiatedType(igenOfString, canon);
                Assert.True(instantiatedType.CheckConstraints());
            }

            // __UniversalCanon as instantiation param should pass all type constraints
            {
                instantiatedType = nonVariantInterfaceConstraintType.MakeInstantiatedType(universalCanon, objectType);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _complexGenericConstraint3Type.MakeInstantiatedType(universalCanon, intType);
                Assert.True(instantiatedType.CheckConstraints());
            }

            // __Canon / __UniversalCanon as the constraint type itself
            // SimpleGenericConstraint<T, U> where T : U
            // T=Arg1, U=__Canon → constraint type is __Canon, a ref type should satisfy it
            {
                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_arg1Type, canon);
                Assert.True(instantiatedType.CheckConstraints());

                // Value type should not satisfy __Canon constraint
                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_structArgWithDefaultCtorType, canon);
                Assert.False(instantiatedType.CheckConstraints());

                // Any type satisfies __UniversalCanon constraint
                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_arg1Type, universalCanon);
                Assert.True(instantiatedType.CheckConstraints());

                instantiatedType = _simpleGenericConstraintType.MakeInstantiatedType(_structArgWithDefaultCtorType, universalCanon);
                Assert.True(instantiatedType.CheckConstraints());
            }

            // Nested __UniversalCanon under invariant generic shape
            // ComplexGenericConstraint2<T, U> where T : Arg2<Arg2<U>>
            // T=Arg2<Arg2<int>>, U=__UniversalCanon → constraint is Arg2<Arg2<__UniversalCanon>>
            {
                TypeDesc arg2OfInt = _arg2Type.MakeInstantiatedType(intType);
                TypeDesc arg2OfArg2OfInt = _arg2Type.MakeInstantiatedType(arg2OfInt);
                instantiatedType = _complexGenericConstraint2Type.MakeInstantiatedType(arg2OfArg2OfInt, universalCanon);
                Assert.True(instantiatedType.CheckConstraints());
            }

            // Array type args with __Canon in invariant position
            // NonVariantInterfaceConstraint<T, U> where T : INonVariantGen<U>
            // T=NonVariantGenImpl<string[]>, U=__Canon[] → constraint is INonVariantGen<__Canon[]>
            // NonVariantGenImpl<string[]> implements INonVariantGen<string[]>
            // string[] is a ref type, so string[] should be compatible with __Canon[]
            {
                TypeDesc stringArray = stringType.MakeArrayType();
                TypeDesc canonArray = canon.MakeArrayType();
                TypeDesc nonVariantGenImplOfStringArray = nonVariantGenImplType.MakeInstantiatedType(stringArray);
                instantiatedType = nonVariantInterfaceConstraintType.MakeInstantiatedType(nonVariantGenImplOfStringArray, canonArray);
                Assert.True(instantiatedType.CheckConstraints());
            }
        }
    }
}
