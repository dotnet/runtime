// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Linq;
using System.Reflection;

namespace System.Reflection.Tests
{
    public class TypeInfo_FindInterfaces
    {
        [Fact]
        public static void FindInterfaces()
        {
            Type[] interfaces = typeof(ClassWithNoItfImpl).GetTypeInfo().FindInterfaces((Type t, object c) => true, "notused");

            Assert.Equal(0, interfaces.Length);
            interfaces = typeof(ClassWithSingleItfImpl).GetTypeInfo().FindInterfaces((Type t, object c) => true, "notused");
            Assert.Equal(1, interfaces.Length);
            Assert.Equal("IFace1", interfaces[0].Name);

            interfaces = typeof(ClassWithSingleItfImpl).GetTypeInfo().FindInterfaces((Type t, object c) => t.Name.Equals(c), "IFace1");
            Assert.Equal(1, interfaces.Length);
            Assert.Equal("IFace1", interfaces[0].Name);

            interfaces = typeof(ClassWithMultipleItfImpl).GetTypeInfo().FindInterfaces((Type t, object c) => true, "notused");
            Assert.Equal(2, interfaces.Length);
            Assert.True(interfaces.All(m => m.Name.Contains("IFace")));

            interfaces = typeof(ClassWithMultipleItfImpl).GetTypeInfo().FindInterfaces((Type t, object c) => t.Name.Contains(c.ToString()), "IFace");
            Assert.Equal(2, interfaces.Length);
            Assert.True(interfaces.All(m => m.Name.Contains("IFace")));

            interfaces = typeof(ClassWithItfImplInHierarchy).GetTypeInfo().FindInterfaces((Type t, object c) => true, "notused");
            Assert.Equal(1, interfaces.Length);
            Assert.Equal("IFace1", interfaces[0].Name);

            interfaces = typeof(ClassWithItfImplInHierarchy).GetTypeInfo().FindInterfaces((Type t, object c) => t.Name.Contains(c.ToString()), "IFace");
            Assert.Equal(1, interfaces.Length);
            Assert.Equal("IFace1", interfaces[0].Name);

            interfaces = typeof(ClassWithDirectAndInHierarchyItfImpl).GetTypeInfo().FindInterfaces((Type t, object c) => true, "notused");
            Assert.Equal(3, interfaces.Length);
            Assert.True(interfaces.All(m => m.Name.Contains("IFace")));

            interfaces = typeof(ClassWithDirectAndInHierarchyItfImpl).GetTypeInfo().FindInterfaces((Type t, object c) => t.Name.Contains(c.ToString()), "IFace");
            Assert.Equal(3, interfaces.Length);
            Assert.True(interfaces.All(m => m.Name.Contains("IFace")));

            interfaces = typeof(ClassWithDirectAndInHierarchyItfImpl).GetTypeInfo().FindInterfaces((Type t, object c) => t.Name.Contains(c.ToString()), "IFace1");
            Assert.Equal(1, interfaces.Length);
            Assert.Equal("IFace1", interfaces[0].Name);
        }

        public interface IFace1 { }
        public interface IFace2 { }
        public interface IFace3 { }
        public class ClassWithNoItfImpl { }
        public class ClassWithSingleItfImpl : IFace1 { }
        public class ClassWithMultipleItfImpl : IFace2, IFace3 { }
        public class ClassWithItfImplInHierarchy : ClassWithSingleItfImpl { }
        public class ClassWithDirectAndInHierarchyItfImpl : ClassWithMultipleItfImpl, IFace1 { }
    }
}