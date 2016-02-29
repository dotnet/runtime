// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Tests
{
    public class MethodInfo_GetBaseDefinition
    {
        [Fact]
        public void GetBaseDefinition()
        {
            MethodInfo mi = typeof(BaseClass).GetTypeInfo().GetMethod("ItfMethod1").GetBaseDefinition();
            Assert.Equal(typeof(BaseClass).GetTypeInfo().GetMethod("ItfMethod1"), mi);
            Assert.Equal(MemberTypes.Method, mi.MemberType);

            mi = typeof(DerivedClass).GetTypeInfo().GetMethod("ItfMethod1").GetBaseDefinition();
            Assert.Equal(typeof(BaseClass).GetTypeInfo().GetMethod("ItfMethod1"), mi);
            Assert.Equal(MemberTypes.Method, mi.MemberType);

            mi = typeof(DerivedClass).GetTypeInfo().GetMethod("BaseClassVirtualMethod").GetBaseDefinition();
            Assert.Equal(typeof(BaseClass).GetTypeInfo().GetMethod("BaseClassVirtualMethod"), mi);
            Assert.Equal(MemberTypes.Method, mi.MemberType);

            mi = typeof(DerivedClass).GetTypeInfo().GetMethod("BaseClassMethod").GetBaseDefinition();
            Assert.Equal(typeof(DerivedClass).GetTypeInfo().GetMethod("BaseClassMethod"), mi);
            Assert.Equal(MemberTypes.Method, mi.MemberType);

            mi = typeof(DerivedClass).GetTypeInfo().GetMethod("ToString").GetBaseDefinition();
            Assert.Equal(typeof(object).GetTypeInfo().GetMethod("ToString"), mi);
            Assert.Equal(MemberTypes.Method, mi.MemberType);

            mi = typeof(DerivedClass).GetTypeInfo().GetMethod("DerivedClassMethod").GetBaseDefinition();
            Assert.Equal(typeof(DerivedClass).GetTypeInfo().GetMethod("DerivedClassMethod"), mi);
            Assert.Equal(MemberTypes.Method, mi.MemberType);
        }

        public interface IFace
        {
            void ItfMethod1();
            void ItfMethod2();
        }

        public class BaseClass : IFace
        {
            public void ItfMethod1() { }
            void IFace.ItfMethod2() { }

            public virtual void BaseClassVirtualMethod() { }
            public virtual void BaseClassMethod() { }

            public override string ToString()
            {
                return base.ToString();
            }
        }

        public class DerivedClass : BaseClass
        {
            public override void BaseClassVirtualMethod()
            {
                base.BaseClassVirtualMethod();
            }
            public new void BaseClassMethod() { }
            public override string ToString()
            {
                return base.ToString();
            }

            public void DerivedClassMethod() { }
        }
    }
}
