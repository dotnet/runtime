// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Tests
{
    public class PropertyInfoTests
    {
        [Theory]
        [InlineData(typeof(BaseClass), nameof(BaseClass.IntProperty))]
        [InlineData(typeof(BaseClass), nameof(BaseClass.StringProperty))]
        [InlineData(typeof(BaseClass), nameof(BaseClass.DoubleProperty))]
        [InlineData(typeof(BaseClass), nameof(BaseClass.FloatProperty))]
        [InlineData(typeof(BaseClass), nameof(BaseClass.EnumProperty))]
        public void GetConstantValue_NotConstant_ThrowsInvalidOperationException(Type type, string name)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            Assert.Throws<InvalidOperationException>(() => propertyInfo.GetConstantValue());
            Assert.Throws<InvalidOperationException>(() => propertyInfo.GetRawConstantValue());
        }

        [Theory]
        [InlineData(typeof(GetSetClass), nameof(GetSetClass.ReadWriteProperty), true, true)]
        [InlineData(typeof(GetSetClass), nameof(GetSetClass.ReadOnlyProperty), true, false)]
        [InlineData(typeof(GetSetClass), nameof(GetSetClass.WriteOnlyProperty), false, true)]
        [InlineData(typeof(GetSetClass), "Item", true, true)]
        [InlineData(typeof(GetSetStruct), nameof(GetSetStruct.ReadWriteProperty), true, true)]
        [InlineData(typeof(GetSetStruct), nameof(GetSetStruct.ReadOnlyProperty), true, false)]
        [InlineData(typeof(GetSetStruct), nameof(GetSetStruct.WriteOnlyProperty), false, true)]
        [InlineData(typeof(GetSetStruct), "Item", true, false)]
        [InlineData(typeof(GetSetInterface), nameof(GetSetInterface.ReadWriteProperty), true, true)]
        [InlineData(typeof(GetSetInterface), nameof(GetSetInterface.ReadOnlyProperty), true, false)]
        [InlineData(typeof(GetSetInterface), nameof(GetSetInterface.WriteOnlyProperty), false, true)]
        [InlineData(typeof(GetSetInterface), "Item", false, true)]
        public void GetMethod_SetMethod(Type type, string name, bool hasGetter, bool hasSetter)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            Assert.True(hasGetter == (propertyInfo.GetMethod != null), string.Format("{0}.GetMethod expected existence: '{1}', but got '{2}'", propertyInfo, hasGetter, !hasGetter));
            Assert.True(hasSetter == (propertyInfo.SetMethod != null), string.Format("{0}.SetMethod expected existence: '{1}', but got '{2}'", propertyInfo, hasSetter, !hasSetter));
        }

        public static IEnumerable<object[]> GetValue_TestData()
        {
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.ReadWriteProperty2), new BaseClass(), null, -1.0 };
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.ReadWriteProperty3), typeof(BaseClass), null, -2 };
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.Name), new BaseClass(), null, "hello" };
            yield return new object[] { typeof(CustomIndexerNameClass), "BasicIndexer", new CustomIndexerNameClass(), new object[] { 1, "2" }, null };
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.ReadOnlyProperty), new BaseClass(), null, 100 };
        }

        [Theory]
        [MemberData(nameof(GetValue_TestData))]
        public void GetValue(Type type, string name, object obj, object[] index, object expected)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            if (index == null)
            {
                Assert.Equal(expected, propertyInfo.GetValue(obj));
            }
            Assert.Equal(expected, propertyInfo.GetValue(obj, index));
        }

        public static IEnumerable<object[]> GetValue_Invalid_TestData()
        {
            // Incorrect indexer parameters
            yield return new object[] { typeof(CustomIndexerNameClass), "BasicIndexer", new CustomIndexerNameClass(), new object[] { 1, "2", 3 }, typeof(TargetParameterCountException) };
            yield return new object[] { typeof(CustomIndexerNameClass), "BasicIndexer", new CustomIndexerNameClass(), null, typeof(TargetParameterCountException) };

            // Incorrect type
            yield return new object[] { typeof(CustomIndexerNameClass), "BasicIndexer", new CustomIndexerNameClass(), new object[] { "1", "2" }, typeof(ArgumentException) };

            // Readonly
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.WriteOnlyProperty), new BaseClass(), null, typeof(ArgumentException) };

            // Null target
            yield return new object[] { typeof(CustomIndexerNameClass), "BasicIndexer", null, new object[] { "1", "2" }, typeof(TargetException) };
        }

        [Theory]
        [MemberData(nameof(GetValue_Invalid_TestData))]
        public void GetValue_Invalid(Type type, string name, object obj, object[] index, Type exceptionType)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            Assert.Throws(exceptionType, () => propertyInfo.GetValue(obj, index));
        }

        public static IEnumerable<object[]> SetValue_TestData()
        {
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.StaticObjectArrayProperty), typeof(BaseClass), new string[] { "hello" }, null, new string[] { "hello" } };
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.ObjectArrayProperty), new BaseClass(), new string[] { "hello" }, null, new string[] { "hello" } };
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.Name), new BaseClass(), "hello", null, "hello" };
            yield return new object[] { typeof(AdvancedIndexerClass), "Item", new AdvancedIndexerClass(), "hello", new object[] { 99, 2, new string[] { "hello" }, "f" }, "992f1" };
            yield return new object[] { typeof(AdvancedIndexerClass), "Item", new AdvancedIndexerClass(), "pw", new object[] { 99, 2, new string[] { "hello" }, "SOME string" }, "992SOME string1" };
        }

        [Theory]
        [MemberData(nameof(SetValue_TestData))]
        public void SetValue(Type type, string name, object obj, object value, object[] index, object expected)
        {
            PropertyInfo PropertyInfo = GetProperty(type, name);
            object originalValue;
            if (index == null)
            {
                // Use SetValue(object, object)
                originalValue = PropertyInfo.GetValue(obj);
                try
                {
                    PropertyInfo.SetValue(obj, value);
                    Assert.Equal(expected, PropertyInfo.GetValue(obj));
                }
                finally
                {
                    PropertyInfo.SetValue(obj, originalValue);
                }
            }
            // Use SetValue(object, object, object[])
            originalValue = PropertyInfo.GetValue(obj, index);
            try
            {
                PropertyInfo.SetValue(obj, value, index);
                Assert.Equal(expected, PropertyInfo.GetValue(obj, index));
            }
            finally
            {
                PropertyInfo.SetValue(obj, originalValue, index);
            }
        }

        public static IEnumerable<object[]> SetValue_Invalid_TestData()
        {
            // Incorrect number of parameters
            yield return new object[] { typeof(AdvancedIndexerClass), "Item", new AdvancedIndexerClass(), "value", new object[] { 99, 2, new string[] { "a" } }, typeof(TargetParameterCountException) };

            // Obj is null
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.WriteOnlyProperty), null, null, null, typeof(TargetException) };
            yield return new object[] { typeof(AdvancedIndexerClass), "Item", null, "value", new object[] { 99, 2, new string[] { "a" }, "b" }, typeof(TargetException) };

            // Readonly
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.ReadOnlyProperty), new BaseClass(), 100, null, typeof(ArgumentException) };

            // Wrong value type
            yield return new object[] { typeof(AdvancedIndexerClass), "Item", new AdvancedIndexerClass(), "value", new object[] { 99, 2, "invalid", "string" }, typeof(ArgumentException) };
            yield return new object[] { typeof(AdvancedIndexerClass), "Item", new AdvancedIndexerClass(), 100, new object[] { 99, 2, new string[] { "a" }, "b" }, typeof(ArgumentException) };
            yield return new object[] { typeof(BaseClass), nameof(BaseClass.WriteOnlyProperty), new BaseClass(), "string", null, typeof(ArgumentException) };
        }

        [Theory]
        [MemberData(nameof(SetValue_Invalid_TestData))]
        public void SetValue_Invalid(Type type, string name, object obj, object value, object[] index, Type exceptionType)
        {
            PropertyInfo PropertyInfo = GetProperty(type, name);
            Assert.Throws(exceptionType, () => PropertyInfo.SetValue(obj, value, index));
        }

        [Theory]
        [InlineData(nameof(PropertyInfoMembers.PublicGetIntProperty))]
        [InlineData(nameof(PropertyInfoMembers.PublicGetPublicSetStringProperty))]
        [InlineData(nameof(PropertyInfoMembers.PublicGetDoubleProperty))]
        [InlineData(nameof(PropertyInfoMembers.PublicGetFloatProperty))]
        [InlineData(nameof(PropertyInfoMembers.PublicGetEnumProperty))]
        [InlineData("PrivateGetPrivateSetIntProperty")]
        [InlineData(nameof(PropertyInfoMembers.PublicGetPrivateSetProperty))]
        public static void GetRequiredCustomModifiers_GetOptionalCustomModifiers(string name)
        {
            PropertyInfo property = typeof(PropertyInfoMembers).GetTypeInfo().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.Empty(property.GetRequiredCustomModifiers());
            Assert.Empty(property.GetOptionalCustomModifiers());
        }

        [Theory]
        [InlineData(nameof(PropertyInfoMembers.PublicGetIntProperty), 1, 1)]
        [InlineData(nameof(PropertyInfoMembers.PublicGetPublicSetStringProperty), 2, 2)]
        [InlineData(nameof(PropertyInfoMembers.PublicGetDoubleProperty), 1, 1)]
        [InlineData(nameof(PropertyInfoMembers.PublicGetFloatProperty), 1, 1)]
        [InlineData(nameof(PropertyInfoMembers.PublicGetEnumProperty), 2, 2)]
        [InlineData("PrivateGetPrivateSetIntProperty", 0, 2)]
        [InlineData(nameof(PropertyInfoMembers.PublicGetPrivateSetProperty), 1, 2)]
        public static void GetAccessors(string name, int accessorPublicCount, int accessorPublicAndNonPublicCount)
        {
            PropertyInfo pi = typeof(PropertyInfoMembers).GetTypeInfo().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(accessorPublicCount, pi.GetAccessors().Length);
            Assert.Equal(accessorPublicCount, pi.GetAccessors(false).Length);
            Assert.Equal(accessorPublicAndNonPublicCount, pi.GetAccessors(true).Length);
        }

        [Theory]
        [InlineData(nameof(PropertyInfoMembers.PublicGetIntProperty), true, true, false, false)]
        [InlineData(nameof(PropertyInfoMembers.PublicGetPublicSetStringProperty), true, true, true, true)]
        [InlineData(nameof(PropertyInfoMembers.PublicGetDoubleProperty), true, true, false, false)]
        [InlineData(nameof(PropertyInfoMembers.PublicGetFloatProperty), true, true, false, false)]
        [InlineData(nameof(PropertyInfoMembers.PublicGetEnumProperty), true, true, true, true)]
        [InlineData("PrivateGetPrivateSetIntProperty", false, true, false, true)]
        [InlineData(nameof(PropertyInfoMembers.PublicGetPrivateSetProperty), true, true, false, true)]
        public static void GetGetMethod_GetSetMethod(string name, bool publicGet, bool nonPublicGet, bool publicSet, bool nonPublicSet)
        {
            PropertyInfo pi = typeof(PropertyInfoMembers).GetTypeInfo().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.True(publicGet ? pi.GetGetMethod().Name.Equals("get_" + name) : pi.GetGetMethod() == null);
            Assert.True(publicGet ? pi.GetGetMethod(false).Name.Equals("get_" + name) : pi.GetGetMethod() == null);
            Assert.True(publicGet ? pi.GetGetMethod(true).Name.Equals("get_" + name) : pi.GetGetMethod() == null);
            Assert.True(nonPublicGet ? pi.GetGetMethod(true).Name.Equals("get_" + name) : pi.GetGetMethod() == null);
            Assert.True(nonPublicGet ? pi.GetGetMethod(true).Name.Equals("get_" + name) : pi.GetGetMethod(false) == null);

            Assert.True(publicSet ? pi.GetSetMethod().Name.Equals("set_" + name) : pi.GetSetMethod() == null);
            Assert.True(publicSet ? pi.GetSetMethod(false).Name.Equals("set_" + name) : pi.GetSetMethod() == null);
            Assert.True(publicSet ? pi.GetSetMethod(true).Name.Equals("set_" + name) : pi.GetSetMethod() == null);
            Assert.True(nonPublicSet ? pi.GetSetMethod(true).Name.Equals("set_" + name) : pi.GetSetMethod() == null);
            Assert.True(nonPublicSet ? pi.GetSetMethod(true).Name.Equals("set_" + name) : pi.GetSetMethod(false) == null);
        }

        [Theory]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadWriteProperty1), typeof(BaseClass), nameof(BaseClass.ReadWriteProperty1), true)]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadWriteProperty1), typeof(BaseClass), nameof(BaseClass.ReadWriteProperty2), false)]
        public void Equals(Type type1, string name1, Type type2, string name2, bool expected)
        {
            PropertyInfo propertyInfo1 = GetProperty(type1, name1);
            PropertyInfo propertyInfo2 = GetProperty(type2, name2);
            Assert.Equal(expected, propertyInfo1.Equals(propertyInfo2));
        }

        [Fact]
        public void GetHashCodeTest()
        {
            PropertyInfo propertyInfo = GetProperty(typeof(BaseClass), "ReadWriteProperty1");
            Assert.NotEqual(0, propertyInfo.GetHashCode());
        }

        [Theory]
        [InlineData(typeof(BaseClass), "Item", new string[] { "Index" })]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadWriteProperty1), new string[0])]
        public void GetIndexParameters(Type type, string name, string[] expectedNames)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            ParameterInfo[] indexParameters = propertyInfo.GetIndexParameters();

            Assert.Equal(expectedNames.Length, indexParameters.Length);
            for (int i = 0; i < indexParameters.Length; i++)
            {
                Assert.Equal(expectedNames[i], indexParameters[i].Name);
            }
        }

        [Theory]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadWriteProperty1), true)]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadOnlyProperty), true)]
        [InlineData(typeof(BaseClass), nameof(BaseClass.WriteOnlyProperty), false)]
        public void CanRead(Type type, string name, bool expected)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            Assert.Equal(expected, propertyInfo.CanRead);
        }

        [Theory]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadWriteProperty1), true)]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadOnlyProperty), false)]
        [InlineData(typeof(BaseClass), nameof(BaseClass.WriteOnlyProperty), true)]
        public void CanWrite(Type type, string name, bool expected)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            Assert.Equal(expected, propertyInfo.CanWrite);
        }

        [Theory]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadWriteProperty1))]
        [InlineData(typeof(SubClass), nameof(SubClass.NewProperty))]
        public void DeclaringType(Type type, string name)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            Assert.Equal(type, propertyInfo.DeclaringType);
        }

        [Theory]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadWriteProperty1), typeof(short))]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadWriteProperty2), typeof(double))]
        [InlineData(typeof(SubClass), nameof(SubClass.NewProperty), typeof(int))]
        public void PropertyType(Type type, string name, Type expected)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            Assert.Equal(expected, propertyInfo.PropertyType);
        }

        [Theory]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadWriteProperty1))]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadOnlyProperty))]
        [InlineData(typeof(BaseClass), nameof(BaseClass.WriteOnlyProperty))]
        public void Name(Type type, string name)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            Assert.Equal(name, propertyInfo.Name);
        }

        [Theory]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadWriteProperty1), false)]
        [InlineData(typeof(BaseClass), nameof(BaseClass.ReadOnlyProperty), false)]
        [InlineData(typeof(BaseClass), nameof(BaseClass.WriteOnlyProperty), false)]
        public void IsSpecialName(Type type, string name, bool expected)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            Assert.Equal(expected, propertyInfo.IsSpecialName);
        }

        [Theory]
        [InlineData(typeof(SubClass), nameof(SubClass.Description), PropertyAttributes.None)]
        [InlineData(typeof(SubClass), nameof(SubClass.NewProperty), PropertyAttributes.None)]
        public void Attributes(Type type, string name, PropertyAttributes expected)
        {
            PropertyInfo propertyInfo = GetProperty(type, name);
            Assert.Equal(expected, propertyInfo.Attributes);
        }

        public static PropertyInfo GetProperty(Type type, string name)
        {
            return type.GetTypeInfo().DeclaredProperties.First(propertyInfo => propertyInfo.Name.Equals(name));
        }

        public interface InterfaceWithPropertyDeclaration
        {
            string Name { get; set; }
        }

        public class BaseClass : InterfaceWithPropertyDeclaration
        {
            public static object[] _staticObjectArrayProperty = new object[1];
            public static object[] StaticObjectArrayProperty
            {
                get { return _staticObjectArrayProperty; }
                set { _staticObjectArrayProperty = value; }
            }

            public object[] _objectArray;
            public object[] ObjectArrayProperty
            {
                get { return _objectArray; }
                set { _objectArray = value; }
            }

            public short _readWriteProperty1 = -2;
            public short ReadWriteProperty1
            {
                get { return _readWriteProperty1; }
                set { _readWriteProperty1 = value; }
            }

            public double _readWriteProperty2 = -1;
            public double ReadWriteProperty2
            {
                get { return _readWriteProperty2; }
                set { _readWriteProperty2 = value; }
            }

            public static int _readWriteProperty3 = -2;
            public static int ReadWriteProperty3
            {
                get { return _readWriteProperty3; }
                set { _readWriteProperty3 = value; }
            }

            public int _readOnlyProperty = 100;
            public int ReadOnlyProperty
            {
                get { return _readOnlyProperty; }
            }

            public long _writeOnlyProperty = 1;
            public int WriteOnlyProperty
            {
                set { _writeOnlyProperty = value; }
            }

            // Indexer properties
            public string[] _stringArray = { "abc", "def", "ghi", "jkl" };
            public string this[int Index]
            {
                get { return _stringArray[Index]; }
                set { _stringArray[Index] = value; }
            }

            // Interface property
            private string _name = "hello";
            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            // Const fields
            private const int IntField = 100;
            private const string StringField = "hello";
            private const double DoubleField = 22.314;
            private const float FloatField = 99.99F;
            private const PublicEnum EnumField = PublicEnum.Case1;

            public int IntProperty { get { return IntField; } }
            public string StringProperty { get { return StringField; } }
            public double DoubleProperty { get { return DoubleField; } }
            public float FloatProperty { get { return FloatField; } }
            public PublicEnum EnumProperty { get { return EnumField; } }
        }

        public class SubClass : BaseClass
        {
            private int _newProperty = 100;
            private string _description;

            public int NewProperty
            {
                get { return _newProperty; }
                set { _newProperty = value; }
            }

            public string Description
            {
                get { return _description; }
                set { _description = value; }
            }
        }

        public class CustomIndexerNameClass
        {
            private object[] _objectArray;

            [IndexerName("BasicIndexer")] // Property name will be BasicIndexer instead of Item
            public object[] this[int index, string s]
            {
                get { return _objectArray; }
                set { _objectArray = value; }
            }
        }

        public class AdvancedIndexerClass
        {
            public string _setValue = null;
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
                    _setValue = _setValue = index.ToString() + index2.ToString() + myStr + strHashLength + value;
                }
            }
        }

        public class GetSetClass
        {
            public int ReadWriteProperty { get { return 1; } set { } }
            public string ReadOnlyProperty { get { return "Test"; } }
            public char WriteOnlyProperty { set { } }
            public int this[int index] { get { return 2; } set { } }
        }

        public struct GetSetStruct
        {
            public int ReadWriteProperty { get { return 1; } set { } }
            public string ReadOnlyProperty { get { return "Test"; } }
            public char WriteOnlyProperty { set { } }
            public string this[int index] { get { return "name"; } }
        }

        public interface GetSetInterface
        {
            int ReadWriteProperty { get; set; }
            string ReadOnlyProperty { get; }
            char WriteOnlyProperty { set; }
            char this[int index] { set; }
        }

        public class PropertyInfoMembers
        {
            public int PublicGetIntProperty { get; }
            public string PublicGetPublicSetStringProperty { get; set; }
            public double PublicGetDoubleProperty { get; }
            public float PublicGetFloatProperty { get; }

            public PublicEnum PublicGetEnumProperty { get; set; }
            private int PrivateGetPrivateSetIntProperty { get; set; }
            public int PublicGetPrivateSetProperty { get; private set; }
        }
    }
}
