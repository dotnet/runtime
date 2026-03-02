// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class CustomModuleTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly Module _customModule;

        public CustomModuleTests()
        {
            Assembly assembly = typeof(CustomModuleTests).Assembly;
            Assembly customAssembly = _customReflectionContext.MapAssembly(assembly);
            _customModule = customAssembly.ManifestModule;
        }

        [Fact]
        public void ModuleType_ReturnsCustomModule()
        {
            Assert.Equal(ProjectionConstants.CustomModule, _customModule.GetType().FullName);
        }

        [Fact]
        public void Assembly_ReturnsCustomAssembly()
        {
            Assert.Equal(ProjectionConstants.CustomAssembly, _customModule.Assembly.GetType().FullName);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void FullyQualifiedName_ContainsTestAssemblyName()
        {
            string fqn = _customModule.FullyQualifiedName;
            Assert.NotNull(fqn);
            Assert.Contains("System.Reflection.Context.Tests", fqn);
            Assert.EndsWith(".dll", fqn);
        }

        [Fact]
        public void MetadataToken_ReturnsValue()
        {
            int token = _customModule.MetadataToken;
            Assert.True(token > 0);
        }

        [Fact]
        public void ModuleVersionId_ReturnsNonEmptyGuid()
        {
            Guid mvid = _customModule.ModuleVersionId;
            Guid mvid2 = _customModule.ModuleVersionId;
            Assert.NotEqual(Guid.Empty, mvid);
            Assert.Equal(mvid, mvid2);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void Name_ContainsTestAssemblyName()
        {
            string name = _customModule.Name;
            Assert.NotNull(name);
            Assert.Contains("System.Reflection.Context.Tests", name);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        public void ScopeName_ContainsTestAssemblyName()
        {
            string scopeName = _customModule.ScopeName;
            Assert.NotNull(scopeName);
            Assert.Contains("System.Reflection.Context.Tests", scopeName);
        }

        [Fact]
        public void GetCustomAttributes_WithType_ReturnsAttributes()
        {
            object[] attributes = _customModule.GetCustomAttributes(typeof(TestModuleAttribute), true);
            Assert.Single(attributes);
            Assert.IsType<TestModuleAttribute>(attributes[0]);
        }

        [Fact]
        public void GetCustomAttributes_NoType_ReturnsTestModuleAttribute()
        {
            object[] attributes = _customModule.GetCustomAttributes(false);
            Assert.Contains(attributes, a => a is TestModuleAttribute);
        }

        [Fact]
        public void GetCustomAttributesData_ContainsAttributeData()
        {
            IList<CustomAttributeData> data = _customModule.GetCustomAttributesData();
            Assert.NotEmpty(data);
        }

        [Fact]
        public void IsDefined_ExistingAttribute_ReturnsTrue()
        {
            Assert.True(_customModule.IsDefined(typeof(TestModuleAttribute), true));
        }

        [Fact]
        public void IsDefined_NonExistingAttribute_ReturnsFalse()
        {
            Assert.False(_customModule.IsDefined(typeof(TestAttribute), true));
        }

        [Fact]
        public void GetField_ReturnsProjectedField()
        {
            // Modules typically don't have fields defined directly, but we test the method
            FieldInfo field = _customModule.GetField("NonExistentField", BindingFlags.Public | BindingFlags.Static);
            Assert.Null(field);
        }

        [Fact]
        public void GetFields_ReturnsProjectedFields()
        {
            FieldInfo[] fields = _customModule.GetFields(BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(fields);
            // Modules typically don't have public static fields, so we expect empty
            Assert.Empty(fields);
        }

        [Fact]
        public void GetMethod_ReturnsNull()
        {
            MethodInfo method = _customModule.GetMethod("NonExistentMethod");
            Assert.Null(method);
        }

        [Fact]
        public void GetMethods_ReturnsEmptyForTestModule()
        {
            MethodInfo[] methods = _customModule.GetMethods(BindingFlags.Public | BindingFlags.Static);
            Assert.Empty(methods);
        }

        [Fact]
        public void GetType_ReturnsProjectedType()
        {
            Type type = _customModule.GetType(typeof(TestObject).FullName, false, false);
            Assert.NotNull(type);
            Assert.Equal(ProjectionConstants.CustomType, type.GetType().FullName);
        }

        [Fact]
        public void GetType_CaseSensitive_ReturnsProjectedType()
        {
            Type type = _customModule.GetType(typeof(TestObject).FullName);
            Assert.NotNull(type);
        }

        [Fact]
        public void GetType_IgnoreCase_ReturnsProjectedType()
        {
            Type type = _customModule.GetType(typeof(TestObject).FullName.ToLowerInvariant(), false, true);
            Assert.NotNull(type);
        }

        [Fact]
        public void GetTypes_ReturnsProjectedTypes()
        {
            Type[] types = _customModule.GetTypes();
            Assert.NotEmpty(types);
            Assert.All(types, t => Assert.Equal(ProjectionConstants.CustomType, t.GetType().FullName));
        }

        [Fact]
        public void FindTypes_ReturnsProjectedTypes()
        {
            Type[] types = _customModule.FindTypes((t, o) => t.Name == "TestObject", null);
            Assert.NotEmpty(types);
        }

        [Fact]
        public void IsResource_ReturnsFalse()
        {
            Assert.False(_customModule.IsResource());
        }

        [Fact]
        public void ResolveField_ReturnsProjectedField()
        {
            FieldInfo originalField = typeof(SecondTestObject).GetField("field");
            int token = originalField.MetadataToken;
            FieldInfo resolvedField = _customModule.ResolveField(token);
            Assert.NotNull(resolvedField);
        }

        [Fact]
        public void ResolveMethod_ReturnsProjectedMethod()
        {
            MethodInfo originalMethod = typeof(TestObject).GetMethod("GetMessage");
            int token = originalMethod.MetadataToken;
            MethodBase resolvedMethod = _customModule.ResolveMethod(token);
            Assert.NotNull(resolvedMethod);
        }

        [Fact]
        public void ResolveType_ReturnsProjectedType()
        {
            int token = typeof(TestObject).MetadataToken;
            Type resolvedType = _customModule.ResolveType(token);
            Assert.NotNull(resolvedType);
            Assert.Equal(ProjectionConstants.CustomType, resolvedType.GetType().FullName);
        }

        [Fact]
        public void ResolveMember_ReturnsProjectedMember()
        {
            MethodInfo originalMethod = typeof(TestObject).GetMethod("GetMessage");
            int token = originalMethod.MetadataToken;
            MemberInfo resolvedMember = _customModule.ResolveMember(token);
            Assert.NotNull(resolvedMember);
        }

        [Fact]
        public void ToString_ContainsTestAssemblyName()
        {
            string str = _customModule.ToString();
            Assert.Contains("System.Reflection.Context.Tests", str);
        }

        [Fact]
        public void Equals_SameModule_ReturnsTrue()
        {
            Module sameModule = _customReflectionContext.MapAssembly(typeof(CustomModuleTests).Assembly).ManifestModule;
            Assert.True(_customModule.Equals(sameModule));
        }

        [Fact]
        public void GetHashCode_IsIdempotent()
        {
            int hashCode1 = _customModule.GetHashCode();
            int hashCode2 = _customModule.GetHashCode();
            Assert.Equal(hashCode1, hashCode2);
        }
    }
}
