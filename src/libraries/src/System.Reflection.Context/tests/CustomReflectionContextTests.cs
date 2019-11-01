// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class CustomReflectionContextTests
    {
        [Fact]
        public void Ctor_Null_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => new FaultyTestCustomReflectionContext());
        }

        [Fact]
        public void MapAssembly_Null_Throws()
        {
            var customReflectionContext = new TestCustomReflectionContext();
            AssertExtensions.Throws<ArgumentNullException>("assembly", () => customReflectionContext.MapAssembly(null));
        }

        [Fact]
        public void MapType_Null_Throws()
        {
            var customReflectionContext = new TestCustomReflectionContext();
            AssertExtensions.Throws<ArgumentNullException>("type", () => customReflectionContext.MapType(null));
        }

        [Fact]
        public void MapType_MemberAttributes_Success()
        {
            var customReflectionContext = new TestCustomReflectionContext();
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();

            TypeInfo customTypeInfo = customReflectionContext.MapType(typeInfo);
            MemberInfo customMemberInfo = customTypeInfo.GetDeclaredMethod("GetMessage");
            IEnumerable<Attribute> results = customMemberInfo.GetCustomAttributes();

            Assert.Single(results);
            Assert.IsType<TestAttribute>(results.First());
        }

        [Fact]
        public void MapType_ParameterAttributes_Success()
        {
            var customReflectionContext = new TestCustomReflectionContext();
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();

            TypeInfo customTypeInfo = customReflectionContext.MapType(typeInfo);
            ConstructorInfo ctor = customTypeInfo.GetConstructor(new Type[] { typeof(string) });
            ParameterInfo[] parameters = ctor.GetParameters();

            Assert.Single(parameters);
            IEnumerable<Attribute> results = parameters[0].GetCustomAttributes();

            Assert.Single(results);
            Assert.IsType<TestAttribute>(results.First());
        }

        [Fact]
        public void MapType_Interface_Throws()
        {
            var customReflectionContext = new TestCustomReflectionContext();
            TypeInfo typeInfo = typeof(ICloneable).GetTypeInfo();

            TypeInfo customTypeInfo = customReflectionContext.MapType(typeInfo);
            MethodInfo customMemberInfo = customTypeInfo.GetDeclaredMethod("Clone");

            IEnumerable<Attribute> results = customMemberInfo.GetCustomAttributes();
            Assert.Empty(results);

            IEnumerable<PropertyInfo> props = customTypeInfo.DeclaredProperties;
            Assert.Empty(props);
        }
    }
}
