// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Compatibility.UnitTests;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.PropertyInfoTests
{
    /// <summary>
    /// System.Reflection.PropertyInfo.GetAccessors(System.Boolean)
    /// </summary>
    public class MyProperty
    {
        private string _property1 = "property one";
        private string _property2 = "property two";
        private static string s_property3 = "property three";
        private static string s_property4 = "property four";

        public string Property1
        {
            get
            {
                return _property1;
            }

            set
            {
                if (_property1 != value)
                {
                    _property1 = value;
                }
            }
        }

        protected string Property2
        {
            get
            {
                return _property2;
            }

            set
            {
                if (_property2 != value)
                {
                    _property2 = value;
                }
            }
        }

        public static string Property3
        {
            get
            {
                return s_property3;
            }

            set
            {
                if (s_property3 != value)
                {
                    s_property3 = value;
                }
            }
        }

        protected static string Property4
        {
            get
            {
                return s_property4;
            }

            set
            {
                if (s_property4 != value)
                {
                    s_property4 = value;
                }
            }
        }
    }

    public class PropertyGetAccessors2
    {
        // Verify get public property when NonPublic is set to false.
        [Fact]
        public void PosTest1()
        {
            Type myType = Type.GetType("System.Reflection.Compatibility.UnitTests.PropertyInfoTests.MyProperty");
            PropertyInfo myPropertyInfo = myType.GetProperty("Property1", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo[] myMethodInfos = myPropertyInfo.GetAccessors(false);
            Assert.Equal(2, myMethodInfos.Length);
            Assert.Equal("System.String get_Property1()", myMethodInfos[0].ToString());
            Assert.Equal("Void set_Property1(System.String)", myMethodInfos[1].ToString());
        }

        // Verify get public property when NonPublic is set to true.
        [Fact]
        public void PosTest2()
        {
            Type myType = Type.GetType("System.Reflection.Compatibility.UnitTests.PropertyInfoTests.MyProperty");
            PropertyInfo myPropertyInfo = myType.GetProperty("Property1", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo[] myMethodInfos = myPropertyInfo.GetAccessors(true);
            Assert.Equal(2, myMethodInfos.Length);
            Assert.Equal("System.String get_Property1()", myMethodInfos[0].ToString());
            Assert.Equal("Void set_Property1(System.String)", myMethodInfos[1].ToString());
        }

        // Verify get NonPublic property when NonPublic is set to true.
        [Fact]
        public void PosTest3()
        {
            Type myType = Type.GetType("System.Reflection.Compatibility.UnitTests.PropertyInfoTests.MyProperty");
            PropertyInfo myPropertyInfo = myType.GetProperty("Property2", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo[] myMethodInfos = myPropertyInfo.GetAccessors(true);
            Assert.Equal(2, myMethodInfos.Length);
            Assert.Equal("System.String get_Property2()", myMethodInfos[0].ToString());
            Assert.Equal("Void set_Property2(System.String)", myMethodInfos[1].ToString());
        }

        // Verify get NonPublic property when NonPublic is set to false.
        [Fact]
        public void PosTest4()
        {
            Type myType = Type.GetType("System.Reflection.Compatibility.UnitTests.PropertyInfoTests.MyProperty");
            PropertyInfo myPropertyInfo = myType.GetProperty("Property2", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo[] myMethodInfos = myPropertyInfo.GetAccessors(false);
            Assert.Equal(0, myMethodInfos.Length);
        }

        // Verify get static public property when NonPublic is set to false.
        [Fact]
        public void PosTest5()
        {
            Type myType = Type.GetType("System.Reflection.Compatibility.UnitTests.PropertyInfoTests.MyProperty");
            PropertyInfo myPropertyInfo = myType.GetProperty("Property3", BindingFlags.Public | BindingFlags.Static);
            MethodInfo[] myMethodInfos = myPropertyInfo.GetAccessors(false);
            Assert.Equal(2, myMethodInfos.Length);
            Assert.Equal("System.String get_Property3()", myMethodInfos[0].ToString());
            Assert.Equal("Void set_Property3(System.String)", myMethodInfos[1].ToString());
        }

        // Verify get static public property when NonPublic is set to true.
        [Fact]
        public void PosTest6()
        {
            Type myType = Type.GetType("System.Reflection.Compatibility.UnitTests.PropertyInfoTests.MyProperty");
            PropertyInfo myPropertyInfo = myType.GetProperty("Property3", BindingFlags.Public | BindingFlags.Static);
            MethodInfo[] myMethodInfos = myPropertyInfo.GetAccessors(true);
            Assert.Equal(2, myMethodInfos.Length);
            Assert.Equal("System.String get_Property3()", myMethodInfos[0].ToString());
            Assert.Equal("Void set_Property3(System.String)", myMethodInfos[1].ToString());
        }

        // Verify get static protected property when NonPublic is set to false.
        [Fact]
        public void PosTest7()
        {
            Type myType = Type.GetType("System.Reflection.Compatibility.UnitTests.PropertyInfoTests.MyProperty");
            PropertyInfo myPropertyInfo = myType.GetProperty("Property4", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo[] myMethodInfos = myPropertyInfo.GetAccessors(false);
            Assert.Equal(0, myMethodInfos.Length);
        }

        // Verify get static protected property when NonPublic is set to true.
        [Fact]
        public void PosTest8()
        {
            Type myType = Type.GetType("System.Reflection.Compatibility.UnitTests.PropertyInfoTests.MyProperty");
            PropertyInfo myPropertyInfo = myType.GetProperty("Property4", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo[] myMethodInfos = myPropertyInfo.GetAccessors(true);
            Assert.Equal(2, myMethodInfos.Length);
            Assert.Equal("System.String get_Property4()", myMethodInfos[0].ToString());
            Assert.Equal("Void set_Property4(System.String)", myMethodInfos[1].ToString());
        }

        // Type.GetType(string, bool) throws and does not throw for error scenarios
        [Fact]
        public void NegTest1()
        {
            Type notFoundButNotThrown = Type.GetType("MyTypeThatDoesntExist", false);
            Assert.Throws<TypeLoadException>(() =>
           {
               Type.GetType("MyTypeThatDoesntExist", true);
           });
        }
    }
}