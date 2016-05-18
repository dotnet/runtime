// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class PropertyInfoGetValueTests
    {
        [Theory]
        [InlineData(typeof(ClassWithObjectArrayProperties), "PropertyA", null, new string[] { "hello" })]
        [InlineData(typeof(ClassWithObjectArrayProperties), "PropertyB", null, null)]
        [InlineData(typeof(ClassWithObjectArrayProperties), "PropertyC", new object[] { 1, "2" }, null)]
        [InlineData(typeof(ClassWithPrivateMethod), "Property2", null, 100)]
        [InlineData(typeof(InterfacePropertyClassImplementation), "Name", (object[])null, null)]
        public static void GetValue(Type type, string propertyName, object[] index, object expectedValue)
        {
            PropertyInfo propertyInfo = type.GetTypeInfo().GetProperty(propertyName);
            object instance = Activator.CreateInstance(type);
            if (index == null)
            {
                Assert.Equal(expectedValue, propertyInfo.GetValue(instance));
            }
            Assert.Equal(expectedValue, propertyInfo.GetValue(instance, index));
        }

        [Theory]
        [InlineData(typeof(ClassWithObjectArrayProperties), "PropertyA", new string[] { "hello" }, null)]
        [InlineData(typeof(ClassWithObjectArrayProperties), "PropertyB", new string[] { "hello" }, null)]
        [InlineData(typeof(InterfacePropertyClassImplementation), "Name", "hello", null)]
        public static void SetValue(Type type, string propertyName, object setValue, object[] index)
        {
            PropertyInfo propertyInfo = type.GetTypeInfo().GetProperty(propertyName);
            object instance = Activator.CreateInstance(type);
            if (index == null)
            {
                propertyInfo.SetValue(instance, setValue);
            }
            propertyInfo.SetValue(instance, setValue, index);

            Assert.Equal(setValue, propertyInfo.GetValue(instance));
        }

        [Fact]
        public static void SetValue_Item()
        {
            string propertyName = "Item";
            TypeWithProperties obj = new TypeWithProperties();
            PropertyInfo pi = typeof(TypeWithProperties).GetTypeInfo().GetProperty(propertyName);
            string[] h = { "hello" };

            Assert.Equal(pi.Name, propertyName);

            pi.SetValue(obj, "someotherstring", new object[] { 99, 2, h, "f" });

            Assert.Equal("992f1someotherstring", obj.setValue);

            pi.SetValue(obj, "pw", new object[] { 99, 2, h, "SOME  string" });

            Assert.Equal("992SOME  string1pw", obj.setValue);
        }

        //
        // Negative Tests for PropertyInfo
        //

        [Theory]
        [InlineData("PropertyC", typeof(ClassWithObjectArrayProperties), new object[] { 1, "2", 3 })]
        [InlineData("PropertyC", typeof(ClassWithObjectArrayProperties), null)]
        public static void GetValue_ThrowsTargetParameterCountException(string propertyName, Type type, object[]testObj)
        {
            object obj = Activator.CreateInstance(type);
            PropertyInfo pi = type.GetTypeInfo().GetProperty(propertyName);

            Assert.Equal(propertyName, pi.Name);

            Assert.Throws<TargetParameterCountException>(() => pi.GetValue(obj, testObj));
        }

        [Fact]
        public static void GetValue_ThrowsException()
        {
            string propertyName = "PropertyC";
            object obj = Activator.CreateInstance(typeof(ClassWithObjectArrayProperties));
            PropertyInfo pi = typeof(ClassWithObjectArrayProperties).GetTypeInfo().GetProperty(propertyName);

            Assert.Equal(propertyName, pi.Name);

            Assert.Throws<TargetException>(() => pi.GetValue(null, new object[] { "1", "2" }));
        }

        [Theory]
        [InlineData("Property1", typeof(ClassWithPrivateMethod), null)]
        [InlineData("PropertyC", typeof(ClassWithObjectArrayProperties), new object[] { "1", "2" })]
        public static void GetValue_ThrowsArgumentException(string propertyName, Type type, object[] testObj)
        {
            object obj = Activator.CreateInstance(type);
            PropertyInfo pi = type.GetTypeInfo().GetProperty(propertyName);

            Assert.Equal(propertyName, pi.Name);

            Assert.Throws<ArgumentException>(() => pi.GetValue(obj, testObj));
        }

        [Fact]
        public static void SetValue_IndexerProperty_IncorrectNumberOfParameters_ThrowsTargetParameterCountException()
        {
            string propertyName = "Item";
            TypeWithProperties obj = new TypeWithProperties();
            PropertyInfo pi = typeof(TypeWithProperties).GetTypeInfo().GetProperty(propertyName);

            Assert.Equal(propertyName, pi.Name);

            Assert.Throws<TargetParameterCountException>(() => pi.SetValue(obj, "pw", new object[] { 99, 2, new string[] { "SOME string" } }));
        }

        //Try to set instance Property with null object 
        [Theory]
        [InlineData("HasSetterProp", null, null)]
        [InlineData("Item", "pw", new object[] { 99, 2, "SOME string" })]
        public static void SetValueNull_ThrowsException(string propertyName, object setValue, object[] index)
        {
            PropertyInfo pi = typeof(TypeWithProperties).GetTypeInfo().GetProperty(propertyName);

            Assert.Equal(propertyName, pi.Name);

            Assert.Throws<TargetException>(() => pi.SetValue(null, setValue, index));
        }

        [Theory]
        [InlineData("NoSetterProp", 100, null)]
        [InlineData("Item", "pw", new object[] { 99, 2, "hello", "SOME  string" })]
        [InlineData("Item", 100, new object[] { 99, 2, new string[] { "hello" }, "SOME  string" })]
        [InlineData("HasSetterProp", "foo", null)]
        public static void SetValue_ThrowsArgumentException(string propertyName, object setValue, object[] index)
        {
            TypeWithProperties obj = new TypeWithProperties();
            PropertyInfo pi = typeof(TypeWithProperties).GetTypeInfo().GetProperty(propertyName);

            Assert.Equal(propertyName, pi.Name);

            Assert.Throws<ArgumentException>(() => pi.SetValue(obj, setValue, index));
        }
    }


    //Reflection Metadata  

    public class ClassWithObjectArrayProperties
    {
        public static object[] objArr = new object[1];
        public object[] objArr2;

        public static object[] PropertyA
        {
            get { return objArr; }
            set { objArr = value; }
        }

        public object[] PropertyB
        {
            get { return objArr2; }
            set { objArr2 = value; }
        }

        [System.Runtime.CompilerServices.IndexerNameAttribute("PropertyC")]   // will make the property name be MyPropAA instead of default Item
        public object[] this[int index, string s]
        {
            get { return objArr2; }
            set { objArr2 = value; }
        }
    }

    public class TypeWithProperties
    {
        public int NoSetterProp
        {
            get { return 0; }
        }

        public int HasSetterProp
        {
            set { }
        }

        public string this[int index, int index2, string[] h, string myStr]
        {
            get
            {
                string strHashLength = "null";
                if (h != null)
                {
                    strHashLength = h.Length.ToString();
                }
                return index.ToString() + index2.ToString() + myStr + strHashLength;
            }

            set
            {
                string strHashLength = "null";
                if (h != null)
                {
                    strHashLength = h.Length.ToString();
                }
                setValue = setValue = index.ToString() + index2.ToString() + myStr + strHashLength + value;
            }
        }
        public string setValue = null;
    }

    public interface InterfaceProperty
    {
        string Name
        {
            get;
            set;
        }
    }

    public class InterfacePropertyClassImplementation : InterfaceProperty
    {
        private string _name = null;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
    }


    public class ClassWithPrivateMethod
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
