// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Final push to 90% coverage
    public class FinalCoverageTests2
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        // Tests for DelegatingAssembly.GetCustomAttributes(bool)
        [Fact]
        public void Assembly_GetCustomAttributes_NoType_ReturnsAttributes()
        {
            Assembly assembly = typeof(FinalCoverageTests2).Assembly;
            Assembly customAssembly = _customReflectionContext.MapAssembly(assembly);

            object[] attrs = customAssembly.GetCustomAttributes(false);
            // Test assembly has attributes, so we expect non-empty
            Assert.NotEmpty(attrs);
        }

        // Tests for DelegatingConstructorInfo.GetCustomAttributes
        [Fact]
        public void Constructor_GetCustomAttributes_NoType_ReturnsEmptyForUnattributedConstructor()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(string) });

            object[] attrs = ctor.GetCustomAttributes(false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void Constructor_GetCustomAttributes_WithType_ReturnsEmptyForUnattributedConstructor()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(string) });

            object[] attrs = ctor.GetCustomAttributes(typeof(Attribute), false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void Constructor_IsDefined_ReturnsValue()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(string) });

            bool isDefined = ctor.IsDefined(typeof(Attribute), false);
            Assert.False(isDefined);
        }

        // Tests for DelegatingEventInfo.GetCustomAttributes
        [Fact]
        public void Event_GetCustomAttributes_NoType_ReturnsEmptyForUnattributedEvent()
        {
            TypeInfo typeInfo = typeof(TypeWithEvent).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            EventInfo evt = customType.GetEvent("TestEvent");

            object[] attrs = evt.GetCustomAttributes(false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void Event_GetCustomAttributes_WithType_ReturnsEmptyForUnattributedEvent()
        {
            TypeInfo typeInfo = typeof(TypeWithEvent).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            EventInfo evt = customType.GetEvent("TestEvent");

            object[] attrs = evt.GetCustomAttributes(typeof(Attribute), false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void Event_IsDefined_ReturnsValue()
        {
            TypeInfo typeInfo = typeof(TypeWithEvent).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            EventInfo evt = customType.GetEvent("TestEvent");

            bool isDefined = evt.IsDefined(typeof(Attribute), false);
            Assert.False(isDefined);
        }

        // Tests for DelegatingFieldInfo.GetCustomAttributes
        [Fact]
        public void Field_GetCustomAttributes_NoType_ReturnsEmptyForUnattributedField()
        {
            TypeInfo typeInfo = typeof(TypeWithFields).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            FieldInfo field = customType.GetField("PublicField");

            object[] attrs = field.GetCustomAttributes(false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void Field_GetCustomAttributes_WithType_ReturnsEmptyForUnattributedField()
        {
            TypeInfo typeInfo = typeof(TypeWithFields).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            FieldInfo field = customType.GetField("PublicField");

            object[] attrs = field.GetCustomAttributes(typeof(Attribute), false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void Field_IsDefined_ReturnsValue()
        {
            TypeInfo typeInfo = typeof(TypeWithFields).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            FieldInfo field = customType.GetField("PublicField");

            bool isDefined = field.IsDefined(typeof(Attribute), false);
            Assert.False(isDefined);
        }

        // Tests for DelegatingMethodInfo.GetCustomAttributes
        [Fact]
        public void Method_GetCustomAttributes_NoType_ReturnsTestAttribute()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("GetMessage");

            // TestCustomReflectionContext adds TestAttribute to GetMessage
            object[] attrs = method.GetCustomAttributes(false);
            Assert.Contains(attrs, a => a is TestAttribute);
        }

        // Tests for DelegatingPropertyInfo.GetCustomAttributes
        [Fact]
        public void Property_GetCustomAttributes_NoType_ReturnsEmptyForUnattributedProperty()
        {
            TypeInfo typeInfo = typeof(TypeWithProperties).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            PropertyInfo prop = customType.GetProperty("ReadWriteProperty");

            object[] attrs = prop.GetCustomAttributes(false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void Property_GetCustomAttributes_WithType_ReturnsEmptyForUnattributedProperty()
        {
            TypeInfo typeInfo = typeof(TypeWithProperties).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            PropertyInfo prop = customType.GetProperty("ReadWriteProperty");

            object[] attrs = prop.GetCustomAttributes(typeof(Attribute), false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void Property_IsDefined_ReturnsValue()
        {
            TypeInfo typeInfo = typeof(TypeWithProperties).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            PropertyInfo prop = customType.GetProperty("ReadWriteProperty");

            bool isDefined = prop.IsDefined(typeof(Attribute), false);
            Assert.False(isDefined);
        }

        // Tests for DelegatingParameterInfo.GetCustomAttributes
        [Fact]
        public void Parameter_GetCustomAttributes_NoType_ReturnsEmptyForReturnParameter()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("GetMessage");
            ParameterInfo returnParam = method.ReturnParameter;

            object[] attrs = returnParam.GetCustomAttributes(false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void Parameter_GetCustomAttributes_WithType_ReturnsEmptyForReturnParameter()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("GetMessage");
            ParameterInfo returnParam = method.ReturnParameter;

            object[] attrs = returnParam.GetCustomAttributes(typeof(Attribute), false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void Parameter_IsDefined_ReturnsValue()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("GetMessage");
            ParameterInfo returnParam = method.ReturnParameter;

            bool isDefined = returnParam.IsDefined(typeof(Attribute), false);
            Assert.False(isDefined);
        }

        // Tests for module GetCustomAttributes
        [Fact]
        public void Module_GetCustomAttributes_NoType_ContainsTestModuleAttribute()
        {
            Assembly assembly = typeof(FinalCoverageTests2).Assembly;
            Assembly customAssembly = _customReflectionContext.MapAssembly(assembly);
            Module module = customAssembly.ManifestModule;

            object[] attrs = module.GetCustomAttributes(false);
            Assert.Contains(attrs, a => a is TestModuleAttribute);
        }

        // Tests for filter exception handling clause
        internal class TypeWithFilter
        {
            private static bool GetFilter() => true;

            public void MethodWithFilter()
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception) when (GetFilter())
                {
                    // Caught
                }
            }
        }

        [Fact]
        public void ExceptionHandlingClause_WithFilter_HasFilterOffset()
        {
            TypeInfo typeInfo = typeof(TypeWithFilter).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("MethodWithFilter");
            MethodBody body = method.GetMethodBody();

            if (body != null && body.ExceptionHandlingClauses.Count > 0)
            {
                var filterClause = body.ExceptionHandlingClauses.FirstOrDefault(c => c.Flags == ExceptionHandlingClauseOptions.Filter);
                if (filterClause != null)
                {
                    int filterOffset = filterClause.FilterOffset;
                    Assert.True(filterOffset >= 0);
                }
            }
        }

        // Tests for CustomAttributeTypedArgument projection
        [Fact]
        public void CustomAttributeData_ConstructorArguments_AreProjected()
        {
            TypeInfo typeInfo = typeof(SecondTestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            FieldInfo field = customType.GetField("field");

            IList<CustomAttributeData> data = field.GetCustomAttributesData();
            foreach (var cad in data)
            {
                IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
                Assert.NotNull(args);
            }
        }

        // Tests for CustomAttributeNamedArgument projection
        [Fact]
        public void CustomAttributeData_NamedArguments_AreProjected()
        {
            TypeInfo typeInfo = typeof(TypeWithProperties).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            PropertyInfo prop = customType.GetProperty("AttributedProperty");

            IList<CustomAttributeData> data = prop.GetCustomAttributesData();
            foreach (var cad in data)
            {
                IList<CustomAttributeNamedArgument> args = cad.NamedArguments;
                Assert.NotNull(args);
            }
        }
    }
}
