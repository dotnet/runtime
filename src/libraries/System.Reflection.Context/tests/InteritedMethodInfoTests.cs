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
    public class InteritedMethodInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new VirtualPropertyInfoCustomReflectionContext();
        private TypeInfo _customTypeInfo;
        private TypeInfo _customTypeInfoToCheckForEquality;
        private PropertyInfo[] _propertyInfos;
        private MethodInfo _inheritedMethodInfo;
        private MethodInfo _inheritedMethodInfoToCheckForEquality;

        public InteritedMethodInfoTests()
        {
            TypeInfo typeInfo = typeof(SecondTestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _customTypeInfoToCheckForEquality = _customReflectionContext.MapType(typeInfo);
            _propertyInfos = _customTypeInfo.BaseType.GetTypeInfo().DeclaredProperties.ToArray();
            _inheritedMethodInfo = _customTypeInfo.GetMethod("get_number");
            _inheritedMethodInfoToCheckForEquality = _customTypeInfoToCheckForEquality.GetMethod("get_number");
        }

        [Fact]
        public void Equals_DifferentObjectSameType_ReturnsTrue()
        {
            Assert.True(_inheritedMethodInfo.Equals(_inheritedMethodInfoToCheckForEquality));
        }

        [Fact]
        public void Equals_DifferentObjectDifferentType_ReturnsFalse()
        {
            TestObject differentObjectDifferentType = new TestObject("a");
            Assert.False(_inheritedMethodInfo.Equals(differentObjectDifferentType));
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            Assert.False(_inheritedMethodInfo.Equals(null));
        }


        [Fact]
        public void GetCustomAttributes_WithType_ReturnsVirtualAttribute()
        {
            object[] attributes = _inheritedMethodInfo.GetCustomAttributes(typeof(TestGetterSetterAttribute), true);
            Assert.Single(attributes);
            Assert.IsType<TestGetterSetterAttribute>(attributes[0]);
        }

        [Fact]
        public void GetHashCode_DifferentObjectSameType_AreEqual()
        {
            Assert.Equal(_inheritedMethodInfoToCheckForEquality.GetHashCode(), _inheritedMethodInfo.GetHashCode());
        }

        [Fact]
        public void ReflectedType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.CustomType, _inheritedMethodInfo.ReflectedType.GetType().FullName);
        }
    }
}
