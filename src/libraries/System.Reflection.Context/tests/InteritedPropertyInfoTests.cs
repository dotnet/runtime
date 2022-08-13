// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class InteritedPropertyInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new VirtualPropertyInfoCustomReflectionContext();
        // Points to a PropertyInfo instance created by reflection. This doesn't work in a reflection-only context.
        // Fully functional virtual property with getter and setter.
        // Test data
        private readonly SecondTestObject _testObject = new SecondTestObject("Age");
        private TypeInfo _customTypeInfo;
        private PropertyInfo[] _propertyInfos;

        public InteritedPropertyInfoTests()
        {
            TypeInfo typeInfo = typeof(SecondTestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _propertyInfos = _customTypeInfo.BaseType.GetTypeInfo().DeclaredProperties.ToArray();
        }

        //[Fact]
        //public void GetBaseMethod()
        //{
        //    //TestPropertyAttribute
        //    MethodInfo[] methodInfos = _customerTypeInfo.GetMethods();
        //    MethodInfo[] testObjectMethodInfos = typeof(SecondTestObject).GetMethods();
        //    var customAttributes = _customerTypeInfo.GetCustomAttributes(true);
        //    var customAttributes2 = _customerTypeInfo.GetCustomAttributes(typeof(TestPropertyAttribute), true);
        //    var otherTest = _customerTypeInfo.GetProperties(BindingFlags.Public);

        //    Assert.NotEmpty(otherTest);
        //    Assert.NotEmpty(customAttributes);
        //    Assert.NotNull(methodInfos);
        //}

        [Fact]
        public void Inherited_GetCustomAttributes_WithType_Test()
        {
            object[] attributes = _propertyInfos.FirstOrDefault(x => x.Name == "number").GetCustomAttributes(typeof(TestPropertyAttribute), true);
            Assert.Single(attributes);
            Assert.IsType<TestPropertyAttribute>(attributes[0]);
        }

        [Fact]
        public void Inherited_GetCustomAttributes_FromMethod_WithType_Test()
        {
            MethodInfo methodInfo = _customTypeInfo.GetMethod("get_number");
            object[] attributes = methodInfo.GetCustomAttributes(typeof(TestPropertyAttribute), true);

            Assert.Single(attributes);
            Assert.IsType<TestGetterSetterAttribute>(attributes[0]);
        }
    }
}
