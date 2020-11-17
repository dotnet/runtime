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

            _iNonGenType = _testModule.GetType("GenericConstraints", "INonGen");
            _iGenType = _testModule.GetType("GenericConstraints", "IGen`1");
            _arg1Type = _testModule.GetType("GenericConstraints", "Arg1");
            _arg2Type = _testModule.GetType("GenericConstraints", "Arg2`1");
            _arg3Type = _testModule.GetType("GenericConstraints", "Arg3`1");
            _structArgWithDefaultCtorType = _testModule.GetType("GenericConstraints", "StructArgWithDefaultCtor");
            _structArgWithoutDefaultCtorType = _testModule.GetType("GenericConstraints", "StructArgWithoutDefaultCtor");
            _classArgWithDefaultCtorType = _testModule.GetType("GenericConstraints", "ClassArgWithDefaultCtor");
            _classArgWithPrivateDefaultCtorType = _testModule.GetType("GenericConstraints", "ClassArgWithPrivateDefaultCtor");
            _abstractClassArgWithDefaultCtorType = _testModule.GetType("GenericConstraints", "AbstractClassArgWithDefaultCtor");
            _classArgWithoutDefaultCtorType = _testModule.GetType("GenericConstraints", "ClassArgWithoutDefaultCtor");

            _referenceTypeConstraintType = _testModule.GetType("GenericConstraints", "ReferenceTypeConstraint`1");
            _defaultConstructorConstraintType = _testModule.GetType("GenericConstraints", "DefaultConstructorConstraint`1");
            _notNullableValueTypeConstraintType = _testModule.GetType("GenericConstraints", "NotNullableValueTypeConstraint`1");
            _simpleTypeConstraintType = _testModule.GetType("GenericConstraints", "SimpleTypeConstraint`1");
            _doubleSimpleTypeConstraintType = _testModule.GetType("GenericConstraints", "DoubleSimpleTypeConstraint`1");
            _simpleGenericConstraintType = _testModule.GetType("GenericConstraints", "SimpleGenericConstraint`2");
            _complexGenericConstraint1Type = _testModule.GetType("GenericConstraints", "ComplexGenericConstraint1`2");
            _complexGenericConstraint2Type = _testModule.GetType("GenericConstraints", "ComplexGenericConstraint2`2");
            _complexGenericConstraint3Type = _testModule.GetType("GenericConstraints", "ComplexGenericConstraint3`2");
            _complexGenericConstraint4Type = _testModule.GetType("GenericConstraints", "ComplexGenericConstraint4`2");
            _multipleConstraintsType = _testModule.GetType("GenericConstraints", "MultipleConstraints`2");

            _genericMethodsType = _testModule.GetType("GenericConstraints", "GenericMethods");
            _simpleGenericConstraintMethod = _genericMethodsType.GetMethod("SimpleGenericConstraintMethod", null);
            _complexGenericConstraintMethod = _genericMethodsType.GetMethod("ComplexGenericConstraintMethod", null);
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
            foreach(var genType in new MetadataType[] { _simpleTypeConstraintType , _doubleSimpleTypeConstraintType })
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
    }
}
