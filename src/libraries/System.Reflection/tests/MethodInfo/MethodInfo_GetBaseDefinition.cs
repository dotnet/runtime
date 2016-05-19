// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public class MethodInfo_GetBaseDefinition
    {

        [Theory]
        [InlineData("ItfMethod1", typeof(BaseClass), typeof(BaseClass))]
        [InlineData("ItfMethod1", typeof(DerivedClass), typeof(BaseClass))]
        [InlineData("BaseClassVirtualMethod", typeof(DerivedClass), typeof(BaseClass))]
        [InlineData("BaseClassMethod", typeof(DerivedClass), typeof(DerivedClass))]
        [InlineData("ToString", typeof(DerivedClass), typeof(object))]
        [InlineData("DerivedClassMethod", typeof(DerivedClass), typeof(DerivedClass))]

        public void GetBaseDefinition(string str1, Type typ1, Type typ2)
        {
            MethodInfo mi = typ1.GetTypeInfo().GetMethod(str1).GetBaseDefinition();
            Assert.Equal(typ2.GetTypeInfo().GetMethod(str1), mi);
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
