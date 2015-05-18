// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class PropertyInfoGetValueTests
    {
        //Verify PropertyInfo.GetValue(Object obj , object[] index) returns correct value for static property.
        [Fact]
        public static void TestGetValue1()
        {
            string propertyName = "PropertyA";
            PropertyInfo pi = GetProperty(typeof(MyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            var value = pi.GetValue(typeof(MyCoVariantTest), (Object[])null);

            Assert.NotNull(value);
        }


        //Verify PropertyInfo.GetValue(Object obj , object[] index) returns correct value for static property.
        [Fact]
        public static void TestGetValue2()
        {
            string propertyName = "PropertyA";
            PropertyInfo pi = GetProperty(typeof(MyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            String[] strs = new String[1];
            strs[0] = "hello";

            //set value
            pi.SetValue(null, strs, (Object[])null);

            var value = pi.GetValue(typeof(MyCoVariantTest), (Object[])null);

            String[] strs2 = (String[])value;

            Assert.Equal(strs2[0], strs[0]);
        }


        //Verify PropertyInfo.GetValue(Object obj , object[] index) returns correct value for non-static property.
        [Fact]
        public static void TestGetValue3()
        {
            string propertyName = "PropertyB";
            Object obj = (MyCoVariantTest)new MyCoVariantTest();
            PropertyInfo pi = GetProperty(typeof(MyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            var value = pi.GetValue(obj, (Object[])null);

            Assert.Null(value);
        }


        //Verify PropertyInfo.GetValue(Object obj , object[] index) returns value for non-static property.
        [Fact]
        public static void TestGetValue4()
        {
            string propertyName = "PropertyB";
            Object obj = (MyCoVariantTest)new MyCoVariantTest();
            PropertyInfo pi = GetProperty(typeof(MyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            String[] strs = new String[1];
            strs[0] = "hello";

            //set value
            pi.SetValue(obj, strs, (Object[])null);

            var value = pi.GetValue(obj, (Object[])null);

            String[] strs2 = (String[])value;

            Assert.Equal(strs2[0], strs[0]);
        }


        //Verify PropertyInfo.GetValue(Object obj , object[] index)returns correct value for Interface property
        [Fact]
        public static void TestGetValue5()
        {
            string propertyName = "Name";
            Object obj = (InterfacePropertyImpl)new InterfacePropertyImpl();
            PropertyInfo pi = GetProperty(typeof(InterfacePropertyImpl), propertyName);


            Assert.Equal(pi.Name, propertyName);

            var value = pi.GetValue(obj, (Object[])null);

            Assert.Null(value);
        }


        //Verify PropertyInfo.GetValue(Object obj , object[] index) returns correct value for Interface property
        [Fact]
        public static void TestGetValue6()
        {
            string propertyName = "Name";
            Object obj = (InterfacePropertyImpl)new InterfacePropertyImpl();
            PropertyInfo pi = GetProperty(typeof(InterfacePropertyImpl), propertyName);


            Assert.Equal(pi.Name, propertyName);

            //set value
            String strs1 = "hello";
            pi.SetValue(obj, strs1, (Object[])null);

            var value = pi.GetValue(obj, (Object[])null);

            String strs2 = (String)value;

            Assert.Equal(strs2, strs1);
        }

        //Verify PropertyInfo.GetValue(Object obj , object[] index) returns correct value for  property
        [Fact]
        public static void TestGetValue7()
        {
            string propertyName = "PropertyC";
            Object obj = Activator.CreateInstance(typeof(MyCoVariantTest));
            PropertyInfo pi = GetProperty(typeof(MyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            var value = pi.GetValue(obj, new Object[] { 1, "2" });

            Assert.Null(value);
        }


        //Verify PropertyInfo.GetValue(Object obj , object[] index) returns correct value for  property
        [Fact]
        public static void TestGetValue8()
        {
            string propertyName = "PropertyC";
            Object obj = Activator.CreateInstance(typeof(MyCoVariantTest));
            PropertyInfo pi = GetProperty(typeof(MyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            var value = pi.GetValue(obj, new Object[] { 1, "2" });

            Assert.Null(value);
        }

        //
        // Negative Tests for PropertyInfo
        //

        //Verify PropertyInfo.GetValue throws ParameterCountException
        [Fact]
        public static void TestGetValue9()
        {
            string propertyName = "PropertyC";
            Object obj = Activator.CreateInstance(typeof(MyCoVariantTest));
            PropertyInfo pi = GetProperty(typeof(MyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            Assert.Throws<TargetParameterCountException>(() =>
            {
                var value = pi.GetValue(obj, new Object[] { 1, "2", 3 });
            });
        }


        //Verify PropertyInfo.GetValue throws ParameterCountException
        [Fact]
        public static void TestGetValue10()
        {
            string propertyName = "PropertyC";
            Object obj = Activator.CreateInstance(typeof(MyCoVariantTest));
            PropertyInfo pi = GetProperty(typeof(MyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            Assert.Throws<TargetParameterCountException>(() =>
            {
                var value = pi.GetValue(obj, null);
            });
        }


        //Verify PropertyInfo.GetValue throws ArgumentException
        [Fact]
        public static void TestGetValue11()
        {
            string propertyName = "PropertyC";
            Object obj = Activator.CreateInstance(typeof(MyCoVariantTest));
            PropertyInfo pi = GetProperty(typeof(MyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            Assert.Throws<ArgumentException>(() =>
            {
                var value = pi.GetValue(obj, new Object[] { "1", "2" }); ;
            });
        }


        //Verify PropertyInfo.GetValue throws TargetException
        [Fact]
        public static void TestGetValue12()
        {
            string propertyName = "PropertyC";
            Object obj = Activator.CreateInstance(typeof(MyCoVariantTest));
            PropertyInfo pi = GetProperty(typeof(MyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            // In Win8p instead of TargetException , generic Exception is thrown
            // Refer http://msdn.microsoft.com/en-us/library/b05d59ty.aspx

            try
            {
                var value = pi.GetValue(null, new Object[] { "1", "2" });
                Assert.False(true, "TargetException expected.");
            }
            catch (Exception) { }
        }


        //Verify PropertyInfo.GetValue throws ArgumentException
        [Fact]
        public static void TestGetValue13()
        {
            string propertyName = "Property1";
            Object obj = new LaterClass();
            PropertyInfo pi = GetProperty(typeof(LaterClass), propertyName);


            Assert.Equal(pi.Name, propertyName);

            Assert.Throws<ArgumentException>(() =>
            {
                var value = pi.GetValue(obj, null); ;
            });
        }


        //Verify PropertyInfo.GetValue() returns hardcoded value
        [Fact]
        public static void TestGetValue14()
        {
            string propertyName = "Property2";
            Object obj = new LaterClass();
            PropertyInfo pi = GetProperty(typeof(LaterClass), propertyName);


            Assert.Equal(pi.Name, propertyName);

            int value = (int)pi.GetValue(obj);

            Assert.Equal(value, 100);
        }


        //Verify PropertyInfo.GetValue(Object obj) returns correct value for Interface property
        [Fact]
        public static void TestGetValue15()
        {
            string propertyName = "Name";
            Object obj = (InterfacePropertyImpl)new InterfacePropertyImpl();
            PropertyInfo pi = GetProperty(typeof(InterfacePropertyImpl), propertyName);


            Assert.Equal(pi.Name, propertyName);

            //set value
            String strs1 = "hello";
            pi.SetValue(obj, strs1);

            var value = pi.GetValue(obj);

            String strs2 = (String)value;

            Assert.Equal(strs2, strs1);
        }



        // Gets PropertyInfo object from current class
        public static PropertyInfo getProperty(string property)
        {
            return GetProperty(typeof(PropertyInfoGetValueTests), property);
        }


        //Gets PropertyInfo object from a Type
        public static PropertyInfo GetProperty(Type t, string property)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<PropertyInfo> allproperties = ti.DeclaredProperties.GetEnumerator();
            PropertyInfo pi = null;

            while (allproperties.MoveNext())
            {
                if (allproperties.Current.Name.Equals(property))
                {
                    //found property
                    pi = allproperties.Current;
                    break;
                }
            }
            return pi;
        }
    }


    //Reflection Metadata  

    public class MyCoVariantTest
    {
        public static Object[] objArr = new Object[1];
        public Object[] objArr2;

        public static Object[] PropertyA
        {
            get { return objArr; }
            set { objArr = value; }
        }

        public Object[] PropertyB
        {
            get { return objArr2; }
            set { objArr2 = value; }
        }

        [System.Runtime.CompilerServices.IndexerNameAttribute("PropertyC")]   // will make the property name be MyPropAA instead of default Item
        public Object[] this[int index, String s]
        {
            get { return objArr2; }
            set { objArr2 = value; }
        }
    }

    public interface InterfaceProperty
    {
        String Name
        {
            get;
            set;
        }
    }

    public class InterfacePropertyImpl : InterfaceProperty
    {
        private String _name = null;

        public String Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }


    public class LaterClass
    {
        public int Property1
        {
            set { }
        }

        public int Property2
        {
            private get { return 100; }
            set { }
        }
    }
}
