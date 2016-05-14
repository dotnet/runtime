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
        public static IEnumerable<object[]> GetBaseDefinitionData()
        { 
            yield return new object[] { "ItfMethod1" , typeof(BaseClass), typeof(BaseClass) };
            yield return new object[] { "ItfMethod1", typeof(DerivedClass), typeof(BaseClass) };
            yield return new object[] { "BaseClassVirtualMethod", typeof(DerivedClass), typeof(BaseClass) };
            yield return new object[] { "BaseClassMethod", typeof(DerivedClass), typeof(DerivedClass) };
            yield return new object[] { "ToString", typeof(DerivedClass), typeof(object) };
            yield return new object[] { "DerivedClassMethod", typeof(DerivedClass), typeof(DerivedClass) };
        }

        [Theory]
        [MemberData(nameof(GetBaseDefinitionData))]
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
