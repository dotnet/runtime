// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class InheritedMethodInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new VirtualPropertyInfoCustomReflectionContext();
        // Points to a PropertyInfo instance created by reflection. This doesn't work in a reflection-only context.
        // Fully functional virtual property with getter and setter.
        // Test data
        private readonly SecondTestObject _testObject = new SecondTestObject("Age");
        private TypeInfo _customerTypeInfo;

        public InheritedMethodInfoTests()
        {
            TypeInfo typeInfo = typeof(SecondTestObject).GetTypeInfo();
            _customerTypeInfo = _customReflectionContext.MapType(typeInfo);
        }

        [Fact]
        public void GetBaseMethod()
        {
            MethodInfo[] methodInfos = _customerTypeInfo.GetMethods();
            MethodInfo[] testObjectMethodInfos = typeof(SecondTestObject).GetMethods();
            var customAttributes = _customerTypeInfo.GetCustomAttributes(true);
            var customAttributes2 = _customerTypeInfo.GetCustomAttributes(false);
            var otherTest = _customerTypeInfo.GetProperties(BindingFlags.Public);

            Assert.NotEmpty(otherTest);
            Assert.NotEmpty(customAttributes);
            Assert.NotNull(methodInfos);
        }
    }
}
