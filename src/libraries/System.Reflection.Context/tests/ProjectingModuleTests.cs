// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class ProjectingModuleTests
    {
        private readonly Assembly _customAssembly;
        private Module _customModule;

        public ProjectingModuleTests()
        {
            var customReflectionContext = new TestCustomReflectionContext();
            Assembly assembly = typeof(SecondTestObject).Assembly;

            _customAssembly = customReflectionContext.MapAssembly(assembly);
            _customModule = _customAssembly.GetModule("System.Reflection.Context.Tests.dll");
        }

        [Fact]
        public void GetAssembly_ReturnsCorrectAssemblyName()
        {
            Assert.Equal(_customModule.Assembly.FullName, typeof(ProjectingModuleTests).Assembly.FullName);
        }

        [Fact]
        public void GetCustomAttribute_WithType_ReturnsTestModuleAttribute()
        {
            Assert.Equal(typeof(TestModuleAttribute), _customModule.GetCustomAttribute(typeof(TestModuleAttribute)).GetType());
        }

        [Fact]
        public void GetCustomAttributesData_ReturnsTestModuleAttribute()
        {
            List<CustomAttributeData> customAttributesData = _customModule.GetCustomAttributesData().ToList();
            var testModuleAttribute = customAttributesData.FirstOrDefault(x => x.AttributeType.Name == "TestModuleAttribute");
            Assert.NotNull(testModuleAttribute);
        }

        [Fact]
        public void IsDefined_TestModuleAttribute_ReturnsTrue()
        {
            Assert.True(_customModule.IsDefined(typeof(TestModuleAttribute)));
        }

        [Fact]
        public void IsDefined_NonModuleAttribute_ReturnsFalse()
        {
            Assert.False(_customModule.IsDefined(typeof(TestAttribute)));
        }
    }
}
