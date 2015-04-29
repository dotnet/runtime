// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Compatibility.UnitTests;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.PropertyInfoTests
{
    /// <summary>
    /// System.Reflection.PropertyInfo.GetAccessors()
    /// </summary>
    public class PropertyInfoGetAccessor1
    {
        // Positive Test 1: Get the accessor in the current class
        [Fact]
        public void PosTest1()
        {
            Type t1 = typeof(PropertyInfoGetAccessor1);
            PropertyInfo p1 = t1.GetProperty("pro1");
            MethodInfo[] methodinfo = p1.GetAccessors();
            Assert.NotNull(methodinfo);
            Assert.Equal(2, methodinfo.Length);
        }

        // Positive Test 2: Get the accessor in an interface
        [Fact]
        public void PosTest2()
        {
            Type type = typeof(interface1);
            PropertyInfo properinfo = type.GetProperty("prop");
            MethodInfo[] methodinfo = properinfo.GetAccessors();
            Assert.NotNull(methodinfo);
            Assert.Equal(2, methodinfo.Length);
        }

        // Positive Test 3: Get the accessor in a base class
        [Fact]
        public void PosTest3()
        {
            Type type = typeof(baseclass);
            PropertyInfo properinfo = type.GetProperty("prop2");
            MethodInfo[] methodinfo = properinfo.GetAccessors();
            Assert.NotNull(methodinfo);
            Assert.Equal(2, methodinfo.Length);
        }

        // Positive Test 4: Get the accessor in a sub class
        [Fact]
        public void PosTest4()
        {
            Type type = typeof(subclass);
            PropertyInfo properinfo = type.GetProperty("prop3");
            MethodInfo[] methodinfo = properinfo.GetAccessors();
            Assert.NotNull(methodinfo);
            Assert.Equal(1, methodinfo.Length);
        }

        // Positive Test 5: Get the accessor in an abstract class
        [Fact]
        public void PosTest5()
        {
            Type type = typeof(abs);
            PropertyInfo properinfo = type.GetProperty("value");
            MethodInfo[] methodinfo = properinfo.GetAccessors();
            Assert.NotNull(methodinfo);
            Assert.Equal(2, methodinfo.Length);
        }

        // Negative Test 1
        [Fact]
        public void NegTest1()
        {
            Type type = typeof(baseclass);
            PropertyInfo properinfo = type.GetProperty("prop10");
            Assert.Throws<ArgumentNullException>(() =>
           {
               MethodInfo[] methodinfo = properinfo.GetAccessors();
           });
        }

        public int pro1
        {
            set
            {
                _i1 = value;
            }
            get
            {
                return _i1;
            }
        }
        private int _i1;
    }
    internal class baseclass
    {
        public int prop2
        {
            get
            {
                return 1;
            }
            set
            {
            }
        }
        public int prop3
        {
            get
            {
                return 0;
            }
        }
        public int prop4
        {
            set
            {
            }
        }
    }
    internal class subclass : baseclass
    {
        new public int prop4
        {
            set
            {
            }
            get
            {
                return 0;
            }
        }
    }
    internal interface interface1
    {
        int prop
        {
            get;
            set;
        }
    }
    internal class class_vir
    {
        public virtual int prop_vir
        {
            get
            {
                return 0;
            }
        }
    }

    internal abstract class abs
    {
        public int value
        {
            set { }
            get { return 1; }
        }
    }
}