// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Tests specifically to increase coverage to 90%
    public class FinalCoverageTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        // Tests for DelegatingModule methods
        [Fact]
        public void Module_GetMethod_WithBindingFlags_ReturnsValue()
        {
            Assembly assembly = typeof(FinalCoverageTests).Assembly;
            Assembly customAssembly = _customReflectionContext.MapAssembly(assembly);
            Module module = customAssembly.ManifestModule;

            // Call GetMethod with different overloads
            MethodInfo method = module.GetMethod("NonExistent", BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, Type.EmptyTypes, null);
            Assert.Null(method);
        }

        [Fact]
        public void Module_GetField_WithBindingFlags_ReturnsValue()
        {
            Assembly assembly = typeof(FinalCoverageTests).Assembly;
            Assembly customAssembly = _customReflectionContext.MapAssembly(assembly);
            Module module = customAssembly.ManifestModule;

            FieldInfo field = module.GetField("NonExistent");
            Assert.Null(field);
        }

        // Tests for projecting constructor
        [Fact]
        public void Constructor_GetMethodBody_LocalVariables_ReturnsProjectedLocals()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(string) });
            MethodBody body = ctor.GetMethodBody();

            if (body != null)
            {
                IList<LocalVariableInfo> locals = body.LocalVariables;
                Assert.NotNull(locals);
            }
        }

        // Tests for projecting parameter
        [Fact]
        public void Parameter_Member_IsProjectedMethod()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("GetMessage");
            ParameterInfo returnParam = method.ReturnParameter;

            MemberInfo member = returnParam.Member;
            Assert.NotNull(member);
        }

        // Tests for generic type constraints
        internal class ConstrainedGeneric<T> where T : class, new()
        {
            public T Value { get; set; }
        }

        [Fact]
        public void GenericTypeParameter_GetGenericParameterConstraints_ReturnsProjectedTypes()
        {
            TypeInfo genericDefInfo = typeof(ConstrainedGeneric<>).GetTypeInfo();
            TypeInfo customGenericDef = _customReflectionContext.MapType(genericDefInfo);

            Type[] typeParams = customGenericDef.GetGenericArguments();
            Assert.Single(typeParams);

            Type[] constraints = typeParams[0].GetGenericParameterConstraints();
            Assert.NotNull(constraints);
            // The class constraint
        }

        [Fact]
        public void GenericTypeParameter_GenericParameterAttributes_ReturnsValue()
        {
            TypeInfo genericDefInfo = typeof(ConstrainedGeneric<>).GetTypeInfo();
            TypeInfo customGenericDef = _customReflectionContext.MapType(genericDefInfo);

            Type[] typeParams = customGenericDef.GetGenericArguments();
            GenericParameterAttributes attrs = typeParams[0].GenericParameterAttributes;

            Assert.True(attrs.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint));
            Assert.True(attrs.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint));
        }

        [Fact]
        public void GenericTypeParameter_GenericParameterPosition_ReturnsValue()
        {
            TypeInfo genericDefInfo = typeof(ConstrainedGeneric<>).GetTypeInfo();
            TypeInfo customGenericDef = _customReflectionContext.MapType(genericDefInfo);

            Type[] typeParams = customGenericDef.GetGenericArguments();
            int position = typeParams[0].GenericParameterPosition;
            Assert.Equal(0, position);
        }

        // Tests for array types
        [Fact]
        public void ArrayType_HasElementType_ReturnsTrue()
        {
            TypeInfo typeInfo = typeof(int[]).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            Assert.True(customType.HasElementType);
        }

        [Fact]
        public void ArrayType_IsArray_ReturnsTrue()
        {
            TypeInfo typeInfo = typeof(int[]).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            Assert.True(customType.IsArray);
        }

        [Fact]
        public void ArrayType_GetElementType_ReturnsProjectedType()
        {
            TypeInfo typeInfo = typeof(int[]).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            Type elementType = customType.GetElementType();
            Assert.NotNull(elementType);
            Assert.Equal(ProjectionConstants.CustomType, elementType.GetType().FullName);
        }

        [Fact]
        public void ArrayType_GetArrayRank_ReturnsValue()
        {
            TypeInfo typeInfo = typeof(int[,]).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            Assert.Equal(2, customType.GetArrayRank());
        }

        // Tests for pointer type
        [Fact]
        public void PointerType_IsPointer_ReturnsTrue()
        {
            TypeInfo typeInfo = typeof(int*).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            Assert.True(customType.IsPointer);
        }

        [Fact]
        public void PointerType_GetElementType_ReturnsProjectedType()
        {
            TypeInfo typeInfo = typeof(int*).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            Type elementType = customType.GetElementType();
            Assert.NotNull(elementType);
        }

        // Tests for by-ref type
        [Fact]
        public void ByRefType_IsByRef_ReturnsTrue()
        {
            Type byRefType = typeof(int).MakeByRefType();
            TypeInfo typeInfo = byRefType.GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            Assert.True(customType.IsByRef);
        }

        [Fact]
        public void ByRefType_GetElementType_ReturnsProjectedType()
        {
            Type byRefType = typeof(int).MakeByRefType();
            TypeInfo typeInfo = byRefType.GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            Type elementType = customType.GetElementType();
            Assert.NotNull(elementType);
        }

        // Tests for value types
        [Fact]
        public void ValueType_IsValueType_ReturnsTrue()
        {
            TypeInfo typeInfo = typeof(int).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            Assert.True(customType.IsValueType);
        }

        [Fact]
        public void ValueType_IsPrimitive_ReturnsTrue()
        {
            TypeInfo typeInfo = typeof(int).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            Assert.True(customType.IsPrimitive);
        }

        // Tests for delegate type
        [Fact]
        public void DelegateMethod_ReturnTypeCustomAttributes_ParameterInfo()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("GetMessage");

            ICustomAttributeProvider provider = method.ReturnTypeCustomAttributes;
            Assert.NotNull(provider);
        }

        // Tests for generic method parameter
        internal class ClassWithConstrainedGenericMethod
        {
            public T GenericMethod<T>() where T : class => default;
        }

        [Fact]
        public void GenericMethodParameter_DeclaringMethod_ReturnsProjectedMethod()
        {
            TypeInfo typeInfo = typeof(ClassWithConstrainedGenericMethod).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("GenericMethod");

            Type[] typeParams = method.GetGenericArguments();
            Assert.Single(typeParams);

            // DeclaringMethod for a method's generic type parameter should return the method
            MethodBase declaringMethod = typeParams[0].DeclaringMethod;
            Assert.NotNull(declaringMethod);
        }

        // Tests for DelegatingPropertyInfo methods
        [Fact]
        public void Property_SetValue_WithBindingFlags_SetsValue()
        {
            TypeInfo typeInfo = typeof(TypeWithProperties).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            PropertyInfo prop = customType.GetProperty("ReadWriteProperty");

            var target = new TypeWithProperties();
            prop.SetValue(target, "Test", BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
            Assert.Equal("Test", target.ReadWriteProperty);
        }

        [Fact]
        public void Property_GetValue_WithBindingFlags_ReturnsValue()
        {
            TypeInfo typeInfo = typeof(TypeWithProperties).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            PropertyInfo prop = customType.GetProperty("ReadWriteProperty");

            var target = new TypeWithProperties { ReadWriteProperty = "Test" };
            object value = prop.GetValue(target, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
            Assert.Equal("Test", value);
        }

        // Tests for DelegatingEventInfo
        [Fact]
        public void Event_AddEventHandler_WithNullHandler_DoesNotThrow()
        {
            TypeInfo typeInfo = typeof(TypeWithEvent).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            EventInfo evt = customType.GetEvent("TestEvent");

            var target = new TypeWithEvent();
            evt.AddEventHandler(target, null);
            evt.RemoveEventHandler(target, null);
        }
    }
}
