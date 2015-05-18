// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.Reflection.Tests.TypeWithProperties))]

namespace System.Reflection.Tests
{
    public class PropertyInfoSetValueTests
    {
        //Verify PropertyInfo.SetValue(Object obj ,  object value,,object[] index) is able to set value for static property.
        [Fact]
        public static void TestSetValue1()
        {
            string propertyName = "PropertyA";
            PropertyInfo pi = getProperty(typeof(SetValueMyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            String[] strs = new String[1];
            strs[0] = "hello";

            //set value
            pi.SetValue(null, strs, (Object[])null);

            var value = pi.GetValue(typeof(SetValueMyCoVariantTest), (Object[])null);

            String[] strs2 = (String[])value;

            Assert.Equal(strs2[0], strs[0]);
        }




        //Verify PropertyInfo.SetValue(Object obj ,  object value,object[] index) is able to set value for non-static property.
        [Fact]
        public static void TestGetValue2()
        {
            string propertyName = "PropertyB";
            Object obj = (SetValueMyCoVariantTest)new SetValueMyCoVariantTest();
            PropertyInfo pi = getProperty(typeof(SetValueMyCoVariantTest), propertyName);


            Assert.Equal(pi.Name, propertyName);

            String[] strs = new String[1];
            strs[0] = "hello";

            //set value
            pi.SetValue(obj, strs, (Object[])null);

            var value = pi.GetValue(obj, (Object[])null);

            String[] strs2 = (String[])value;

            Assert.Equal(strs2[0], strs[0]);
        }



        //Verify PropertyInfo.SetValue(Object obj , object value, object[] index) is able to set correct value for Interface property
        [Fact]
        public static void TestSetValue3()
        {
            string propertyName = "Name";
            Object obj = (SetValueInterfacePropertyImpl)new SetValueInterfacePropertyImpl();
            PropertyInfo pi = getProperty(typeof(SetValueInterfacePropertyImpl), propertyName);


            Assert.Equal(pi.Name, propertyName);

            //set value
            String strs1 = "hello";
            pi.SetValue(obj, strs1, (Object[])null);

            var value = pi.GetValue(obj, (Object[])null);

            String strs2 = (String)value;

            Assert.Equal(strs2, strs1);
        }


        //Verify PropertyInfo.SetValue(Object obj,Object value) is able to set correct value for Interface property
        [Fact]
        public static void TestSetValue4()
        {
            string propertyName = "Name";
            Object obj = (SetValueInterfacePropertyImpl)new SetValueInterfacePropertyImpl();
            PropertyInfo pi = getProperty(typeof(SetValueInterfacePropertyImpl), propertyName);


            Assert.Equal(pi.Name, propertyName);

            //set value
            String strs1 = "hello";
            pi.SetValue(obj, strs1);

            var value = pi.GetValue(obj);

            String strs2 = (String)value;

            Assert.Equal(strs2, strs1);
        }



        //
        // Negative Tests for PropertyInfo SetValue( ) methods
        //

        //Try to set Property with  no setter
        [Fact]
        public static void TestSetValue5()
        {
            string propertyName = "nosetterprop";
            Object obj = Activator.CreateInstance(typeof(TypeWithProperties));
            PropertyInfo pi = getProperty(typeof(TypeWithProperties), propertyName);


            Assert.Equal(pi.Name, propertyName);

            Assert.Throws<ArgumentException>(() =>
            {
                pi.SetValue(obj, 100, null);
            });
        }


        //Try to set instance Property with null object 
        [Fact]
        public static void TestSetValue6()
        {
            string propertyName = "hassetterprop";
            PropertyInfo pi = getProperty(typeof(TypeWithProperties), propertyName);


            Assert.Equal(pi.Name, propertyName);

            // Generic Exception is thrown in WP8 instead of TargetException
            try
            {
                pi.SetValue(null, null, null);
                Assert.False(true);
            }
            catch (Exception) { }
        }


        //Try to set instance Property with wrong type
        [Fact]
        public static void TestSetValue7()
        {
            string propertyName = "hassetterprop";
            TypeWithProperties twpObj = new TypeWithProperties();
            PropertyInfo pi = getProperty(typeof(TypeWithProperties), propertyName);


            Assert.Equal(pi.Name, propertyName);

            // Generic Exception is thrown in WP8 instead of TargetException
            try
            {
                pi.SetValue(twpObj, "foo", null);
                Assert.False(true);
            }
            catch (Exception) { }
        }

        //Try to set indexer Property
        [Fact]
        public static void TestSetValue8()
        {
            string propertyName = "Item";
            TypeWithProperties twpObj = new TypeWithProperties();
            PropertyInfo pi = getProperty(typeof(TypeWithProperties), propertyName);
            string[] h = { "hello" };



            Assert.Equal(pi.Name, propertyName);

            pi.SetValue(twpObj, "someotherstring", new Object[] { 99, 2, h, "f" });

            Assert.Equal("992f1someotherstring", twpObj.setValue);
        }


        //Try to set indexer Property
        [Fact]
        public static void TestSetValue9()
        {
            string propertyName = "Item";
            TypeWithProperties twpObj = new TypeWithProperties();
            PropertyInfo pi = getProperty(typeof(TypeWithProperties), propertyName);
            string[] h = { "hello" };



            Assert.Equal(pi.Name, propertyName);

            pi.SetValue(twpObj, "pw", new Object[] { 99, 2, h, "SOME  string" });

            Assert.Equal("992SOME  string1pw", twpObj.setValue);
        }

        //Try to set indexer Property , Incorrect Type
        [Fact]
        public static void TestSetValue10()
        {
            string propertyName = "Item";
            TypeWithProperties twpObj = new TypeWithProperties();
            PropertyInfo pi = getProperty(typeof(TypeWithProperties), propertyName);
            string h = "hello";


            Assert.Equal(pi.Name, propertyName);

            Assert.Throws<ArgumentException>(() =>
            {
                pi.SetValue(twpObj, "pw", new Object[] { 99, 2, h, "SOME  string" });  // Incorrect Type for h
            });
        }


        //Try to set indexer Property , Incorrect number of parameters
        [Fact]
        public static void TestSetValue11()
        {
            string propertyName = "Item";
            TypeWithProperties twpObj = new TypeWithProperties();
            PropertyInfo pi = getProperty(typeof(TypeWithProperties), propertyName);


            Assert.Equal(pi.Name, propertyName);

            Assert.Throws<TargetParameterCountException>(() =>
            {
                pi.SetValue(twpObj, "pw", new Object[] { 99, 2, new String[] { "SOME string" } });  // Incorrect number of parameters
            });
        }


        //Try to set indexer Property when object is null
        [Fact]
        public static void TestSetValue12()
        {
            string propertyName = "Item";
            TypeWithProperties twpObj = new TypeWithProperties();
            PropertyInfo pi = getProperty(typeof(TypeWithProperties), propertyName);


            Assert.Equal(pi.Name, propertyName);

            try
            {
                pi.SetValue(null, "pw", new Object[] { 99, 2, "SOME string" });
                Assert.False(true);
            }
            catch (Exception) { }
        }


        //Try to set indexer Property, whenincorrect type for value is specified
        [Fact]
        public static void TestSetValue13()
        {
            string propertyName = "Item";
            TypeWithProperties twpObj = new TypeWithProperties();
            PropertyInfo pi = getProperty(typeof(TypeWithProperties), propertyName);
            string[] h = { "hello" };


            Assert.Equal(pi.Name, propertyName);

            Assert.Throws<ArgumentException>(() =>
            {
                pi.SetValue(twpObj, 100, new Object[] { 99, 2, h, "SOME  string" });  // iuncorrect type for value
            });
        }

        // Gets PropertyInfo object from current class
        public static PropertyInfo getProperty(string property)
        {
            return getProperty(typeof(PropertyInfoSetValueTests), property);
        }


        //Gets PropertyInfo object from a Type
        public static PropertyInfo getProperty(Type t, string property)
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
    public class TypeWithProperties
    {
        public int nosetterprop
        {
            get { return 0; }
        }

        public int hassetterprop
        {
            set { }
        }


        public String this[int index, int index2, string[] h, String myStr]
        {
            get
            {
                String strHashLength = "null";
                if (h != null)
                {
                    strHashLength = h.Length.ToString();
                }
                return index.ToString() + index2.ToString() + myStr + strHashLength;
            }

            set
            {
                String strHashLength = "null";
                if (h != null)
                {
                    strHashLength = h.Length.ToString();
                }
                setValue = setValue = index.ToString() + index2.ToString() + myStr + strHashLength + value;
            }
        }
        public String setValue = null;
    }

    public class SetValueMyCoVariantTest
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

    public interface SetValueInterfaceProperty
    {
        String Name
        {
            get;
            set;
        }
    }

    public class SetValueInterfacePropertyImpl : SetValueInterfaceProperty
    {
        private String _name = null;

        public String Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }
}
