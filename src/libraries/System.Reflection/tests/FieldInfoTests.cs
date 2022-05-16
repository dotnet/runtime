// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public partial class FieldInfoTests
    {
        [Theory]
        [InlineData(nameof(FieldInfoTests.ConstIntField), 222)]
        [InlineData(nameof(FieldInfoTests.ConstStringField), "new value")]
        [InlineData(nameof(FieldInfoTests.ConstCharField), 'A')]
        [InlineData(nameof(FieldInfoTests.ConstBoolField), false)]
        [InlineData(nameof(FieldInfoTests.ConstFloatField), 4.56f)]
        [InlineData(nameof(FieldInfoTests.ConstDoubleField), double.MaxValue)]
        [InlineData(nameof(FieldInfoTests.ConstInt64Field), long.MaxValue)]
        [InlineData(nameof(FieldInfoTests.ConstByteField), byte.MaxValue)]
        public void SetValue_ConstantField_ThrowsFieldAccessException(string field, object value)
        {
            FieldInfo fieldInfo = GetField(typeof(FieldInfoTests), field);
            Assert.Throws<FieldAccessException>(() => fieldInfo.SetValue(new FieldInfoTests(), value));
        }

        [Fact]
        public void SetValue_ReadonlyField()
        {
            FieldInfo fieldInfo = typeof(FieldInfoTests).GetTypeInfo().GetDeclaredField("readonlyIntField");
            FieldInfoTests myInstance = new FieldInfoTests();

            object current = fieldInfo.GetValue(myInstance);
            Assert.Equal(1, current);

            fieldInfo.SetValue(myInstance, int.MinValue);
            Assert.Equal(int.MinValue, fieldInfo.GetValue(myInstance));
        }

        [Theory]
        [InlineData(typeof(Int32Attr), "[System.Reflection.Tests.Int32Attr((Int32)77, name = \"Int32AttrSimple\")]")]
        [InlineData(typeof(Int64Attr), "[System.Reflection.Tests.Int64Attr((Int64)77, name = \"Int64AttrSimple\")]")]
        [InlineData(typeof(StringAttr), "[System.Reflection.Tests.StringAttr(\"hello\", name = \"StringAttrSimple\")]")]
        [InlineData(typeof(EnumAttr), "[System.Reflection.Tests.EnumAttr((System.Reflection.Tests.PublicEnum)1, name = \"EnumAttrSimple\")]")]
        [InlineData(typeof(TypeAttr), "[System.Reflection.Tests.TypeAttr(typeof(System.Object), name = \"TypeAttrSimple\")]")]
        [InlineData(typeof(Attr), "[System.Reflection.Tests.Attr((Int32)77, name = \"AttrSimple\")]")]
        public static void CustomAttributes(Type type, string expectedToString)
        {
            FieldInfo fieldInfo = GetField(typeof(FieldInfoTests), "fieldWithAttributes");
            CustomAttributeData attributeData = fieldInfo.CustomAttributes.First(attribute => attribute.AttributeType.Equals(type));
            Assert.Equal(expectedToString, attributeData.ToString());
        }

        public static IEnumerable<object[]> GetValue_TestData()
        {
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.s_intField), new FieldInfoTests(), 100 };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.s_intField), null, 100 };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.intField), new FieldInfoTests(), 101 };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.s_stringField), new FieldInfoTests(), "static" };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), new FieldInfoTests(), "non static" };
        }

        [Theory]
        [MemberData(nameof(GetValue_TestData))]
        public void GetValue(Type type, string name, object obj, object expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.GetValue(obj));
        }

        public static IEnumerable<object[]> GetValue_Invalid_TestData()
        {
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), null, typeof(TargetException) };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), new object(), typeof(ArgumentException) };
        }

        [Theory]
        [MemberData(nameof(GetValue_Invalid_TestData))]
        public void GetValue_Invalid(Type type, string name, object obj, Type exceptionType)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Throws(exceptionType, () => fieldInfo.GetValue(obj));
        }

        public static IEnumerable<object[]> SetValue_TestData()
        {
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.s_intField), new FieldInfoTests(), 1000, 1000 };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.s_intField), null, 1000, 1000 };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.intField), new FieldInfoTests(), 1000, 1000 };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.s_stringField), new FieldInfoTests(), "new", "new" };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), new FieldInfoTests(), "new", "new" };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.shortEnumField), new FieldInfoTests(), (byte)1, (ShortEnum)1 };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.intEnumField), new FieldInfoTests(), (short)2, (IntEnum)2 };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.longEnumField), new FieldInfoTests(), (int)3, (LongEnum)3 };
        }

        [Theory]
        [MemberData(nameof(SetValue_TestData))]
        public void SetValue(Type type, string name, object obj, object value, object expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            object original = fieldInfo.GetValue(obj);
            try
            {
                fieldInfo.SetValue(obj, value);
                Assert.Equal(expected, fieldInfo.GetValue(obj));
            }
            finally
            {
                fieldInfo.SetValue(obj, original);
            }
        }

        public static IEnumerable<object[]> SetValue_Invalid_TestData()
        {
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), null, "new", typeof(TargetException) };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), new object(), "new", typeof(ArgumentException) };
            yield return new object[] { typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), new FieldInfoTests(), 100, typeof(ArgumentException) };
        }

        [Theory]
        [MemberData(nameof(SetValue_Invalid_TestData))]
        public void SetValue_Invalid(Type type, string name, object obj, object value, Type exceptionType)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Throws(exceptionType, () => fieldInfo.SetValue(obj, value));
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), true)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), typeof(FieldInfoTests), nameof(FieldInfoTests.s_intField), false)]
        public void EqualsTest(Type type1, string name1, Type type2, string name2, bool expected)
        {
            FieldInfo fieldInfo1 = GetField(type1, name1);
            FieldInfo fieldInfo2 = GetField(type2, name2);
            Assert.Equal(expected, fieldInfo1.Equals(fieldInfo2));
        }

        [Fact]
        public void GetHashCodeTest()
        {
            FieldInfo fieldInfo = GetField(typeof(FieldInfoTests), nameof(FieldInfoTests.stringField));
            Assert.NotEqual(0, fieldInfo.GetHashCode());
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.intField), typeof(int))]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), typeof(string))]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField1), typeof(object))]
        public void FieldType(Type type, string name, Type expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.FieldType);
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_intField), true)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.intField), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_stringField), true)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), false)]
        [InlineData(typeof(FieldInfoTests), "privateIntField", false)]
        public void IsStatic(Type type, string name, bool expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.IsStatic);
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField1), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField2), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField3), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField4), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField5), true)]
        public void IsAssembly(Type type, string name, bool expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.IsAssembly);
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField1), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField2), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField3), true)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField4), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField5), false)]
        public void IsFamily(Type type, string name, bool expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.IsFamily);
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField1), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField2), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField3), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField4), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField5), false)]
        public void IsFamilyAndAssembly(Type type, string name, bool expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.IsFamilyAndAssembly);
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField1), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField2), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField3), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField4), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_assemblyField5), false)]
        public void IsFamilyOrAssembly(Type type, string name, bool expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.IsFamilyOrAssembly);
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), true)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_intField), true)]
        [InlineData(typeof(FieldInfoTests), "privateIntField", false)]
        [InlineData(typeof(FieldInfoTests), "privateStringField", false)]
        public void IsPublic(Type type, string name, bool expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.IsPublic);
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), "privateIntField", true)]
        [InlineData(typeof(FieldInfoTests), "privateStringField", true)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.stringField), false)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_intField), false)]
        public void IsPrivate(Type type, string name, bool expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.IsPrivate);
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.readonlyIntField), true)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.intField), false)]
        public void IsInitOnly(Type type, string name, bool expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.IsInitOnly);
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.ConstIntField), true)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.intField), false)]
        public void IsLiteral(Type type, string name, bool expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.IsLiteral);
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.intField), FieldAttributes.Public)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.s_intField), FieldAttributes.Public | FieldAttributes.Static)]
        [InlineData(typeof(FieldInfoTests), "privateIntField", FieldAttributes.Private)]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.readonlyIntField), FieldAttributes.Public | FieldAttributes.InitOnly)]
        public void Attributes(Type type, string name, FieldAttributes expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.Attributes);
        }

        [Theory]
        [InlineData(typeof(FieldInfoTests), nameof(FieldInfoTests.intField), false)]
        public void IsSpecialName(Type type, string name, bool expected)
        {
            FieldInfo fieldInfo = GetField(type, name);
            Assert.Equal(expected, fieldInfo.IsSpecialName);
        }

        [Fact]
        public void SetValue_MixedArrayTypes_CommonBaseClass()
        {
            FI_BaseClass[] ATypeWithMixedAB = new FI_BaseClass[] { new FI_BaseClass(), new FI_SubClass() };
            FI_BaseClass[] ATypeWithAllA = new FI_BaseClass[] { new FI_BaseClass(), new FI_BaseClass() };
            FI_BaseClass[] ATypeWithAllB = new FI_BaseClass[] { new FI_SubClass(), new FI_SubClass() };
            FI_SubClass[] BTypeWithAllB = new FI_SubClass[] { new FI_SubClass(), new FI_SubClass() };
            FI_BaseClass[] BTypeWithAllB_Contra = new FI_SubClass[] { new FI_SubClass(), new FI_SubClass() };

            Type type = typeof(FI_FieldArray);
            object obj = Activator.CreateInstance(type);
            FieldInfo fieldInfo = GetField(type, "aArray");

            fieldInfo.SetValue(obj, ATypeWithMixedAB);
            Assert.Equal(ATypeWithMixedAB, fieldInfo.GetValue(obj));

            fieldInfo.SetValue(obj, ATypeWithAllA);
            Assert.Equal(ATypeWithAllA, fieldInfo.GetValue(obj));

            fieldInfo.SetValue(obj, ATypeWithAllB);
            Assert.Equal(ATypeWithAllB, fieldInfo.GetValue(obj));

            fieldInfo.SetValue(obj, BTypeWithAllB);
            Assert.Equal(BTypeWithAllB, fieldInfo.GetValue(obj));

            fieldInfo.SetValue(obj, BTypeWithAllB_Contra);
            Assert.Equal(BTypeWithAllB_Contra, fieldInfo.GetValue(obj));
        }

        [Fact]
        public void SetValue_MixedArrayTypes_SubClass()
        {
            FI_BaseClass[] ATypeWithMixedAB = new FI_BaseClass[] { new FI_BaseClass(), new FI_SubClass() };
            FI_BaseClass[] ATypeWithAllA = new FI_BaseClass[] { new FI_BaseClass(), new FI_BaseClass() };
            FI_BaseClass[] ATypeWithAllB = new FI_BaseClass[] { new FI_SubClass(), new FI_SubClass() };
            FI_SubClass[] BTypeWithAllB = new FI_SubClass[] { new FI_SubClass(), new FI_SubClass() };
            FI_BaseClass[] BTypeWithAllB_Contra = new FI_SubClass[] { new FI_SubClass(), new FI_SubClass() };

            Type type = typeof(FI_FieldArray);
            object obj = Activator.CreateInstance(type);
            FieldInfo fieldInfo = GetField(type, "bArray");

            AssertExtensions.Throws<ArgumentException>(null, () => fieldInfo.SetValue(obj, ATypeWithMixedAB));
            AssertExtensions.Throws<ArgumentException>(null, () => fieldInfo.SetValue(obj, ATypeWithAllA));
            AssertExtensions.Throws<ArgumentException>(null, () => fieldInfo.SetValue(obj, ATypeWithAllB));

            fieldInfo.SetValue(obj, BTypeWithAllB);
            Assert.Equal(BTypeWithAllB, fieldInfo.GetValue(obj));

            fieldInfo.SetValue(obj, BTypeWithAllB_Contra);
            Assert.Equal(BTypeWithAllB_Contra, fieldInfo.GetValue(obj));
        }

        [Fact]
        public void SetValue_MixedArrayTypes_Interface()
        {
            Type type = typeof(FI_FieldArray);
            object obj = Activator.CreateInstance(type);
            FieldInfo fieldInfo = GetField(type, "iArray");

            FI_Interface[] mixedMN = new FI_Interface[] { new FI_ClassWithInterface1(), new FI_ClassWithInterface2() };
            fieldInfo.SetValue(obj, mixedMN);
            Assert.Equal(mixedMN, fieldInfo.GetValue(obj));
        }

        [Fact]
        public void SetValue_MixedArrayTypes_IntByte()
        {
            Type type = typeof(FI_FieldArray);
            object obj = Activator.CreateInstance(type);
            FieldInfo fieldInfo = GetField(type, "intArray");

            int[] intArray = new int[] { 200, -200, 30, 2 };
            fieldInfo.SetValue(obj, intArray);
            Assert.Equal(intArray, fieldInfo.GetValue(obj));

            AssertExtensions.Throws<ArgumentException>(null, () => fieldInfo.SetValue(obj, new byte[] { 2, 3, 4 }));
        }

        [Fact]
        public void SetValue_MixedArrayTypes_ObjectArray()
        {
            Type type = typeof(FI_FieldArray);
            object obj = Activator.CreateInstance(type);
            FieldInfo fieldInfo = GetField(type, "objectArray");

            FI_Interface[] mixedMN = new FI_Interface[] { new FI_ClassWithInterface1(), new FI_ClassWithInterface2() };
            fieldInfo.SetValue(obj, mixedMN);
            Assert.Equal(mixedMN, fieldInfo.GetValue(obj));

            FI_BaseClass[] BTypeWithAllB_Contra = new FI_SubClass[] { new FI_SubClass(), new FI_SubClass() };
            fieldInfo.SetValue(obj, BTypeWithAllB_Contra);
            Assert.Equal(BTypeWithAllB_Contra, fieldInfo.GetValue(obj));

            AssertExtensions.Throws<ArgumentException>(null, () => fieldInfo.SetValue(obj, new int[] { 1, -1, 2, -2 }));
            AssertExtensions.Throws<ArgumentException>(null, () => fieldInfo.SetValue(obj, new byte[] { 2, 3, 4 }));
        }

        public static IEnumerable<object[]> FieldInfoRTGenericTests_TestData()
        {
            foreach (Type genericType in new Type[] { typeof(FI_GenericClassField<>), typeof(FI_StaticGenericField<>) })
            {
                yield return new object[] { genericType, typeof(int), "genparamField", 0, -300 };
                yield return new object[] { genericType, typeof(int), "dependField", null, g_int };
                yield return new object[] { genericType, typeof(int), "gparrayField", null, gpa_int };
                yield return new object[] { genericType, typeof(int), "arrayField", null, ga_int };

                yield return new object[] { genericType, typeof(string), "genparamField", null, "hello   !" };
                yield return new object[] { genericType, typeof(string), "dependField", null, g_string };
                yield return new object[] { genericType, typeof(string), "gparrayField", null, gpa_string };
                yield return new object[] { genericType, typeof(string), "arrayField", null, ga_string };

                yield return new object[] { genericType, typeof(object), "genparamField", null, 300 };
                yield return new object[] { genericType, typeof(object), "dependField", null, g_object };
                yield return new object[] { genericType, typeof(object), "gparrayField", null, gpa_object };
                yield return new object[] { genericType, typeof(object), "arrayField", null, ga_object };

                yield return new object[] { genericType, typeof(FI_GenericClass<object>), "genparamField", null, g_object };
                yield return new object[] { genericType, typeof(FI_GenericClass<object>), "dependField", null, g_g_object };
                yield return new object[] { genericType, typeof(FI_GenericClass<object>), "gparrayField", null, gpa_g_object };
                yield return new object[] { genericType, typeof(FI_GenericClass<object>), "arrayField", null, ga_g_object };
            }

            yield return new object[] { typeof(FI_GenericClassField<>), typeof(int), nameof(FI_GenericClassField<int>.selfField), null, pfg_int };
            yield return new object[] { typeof(FI_GenericClassField<>), typeof(string), nameof(FI_GenericClassField<int>.selfField), null, pfg_string };
            yield return new object[] { typeof(FI_GenericClassField<>), typeof(object), nameof(FI_GenericClassField<int>.selfField), null, pfg_object };
            yield return new object[] { typeof(FI_GenericClassField<>), typeof(FI_GenericClass<object>), nameof(FI_GenericClassField<int>.selfField), null, pfg_g_object };

            yield return new object[] { typeof(FI_StaticGenericField<>), typeof(int), nameof(FI_GenericClassField<int>.selfField), null, sfg_int };
            yield return new object[] { typeof(FI_StaticGenericField<>), typeof(string), nameof(FI_GenericClassField<int>.selfField), null, sfg_string };
            yield return new object[] { typeof(FI_StaticGenericField<>), typeof(object), nameof(FI_GenericClassField<int>.selfField), null, sfg_object };
            yield return new object[] { typeof(FI_StaticGenericField<>), typeof(FI_GenericClass<object>), nameof(FI_GenericClassField<int>.selfField), null, sfg_g_object };
        }

        [Theory]
        [MemberData(nameof(FieldInfoRTGenericTests_TestData))]
        public static void SetValue_Generic(Type openType, Type gaType, string fieldName, object initialValue, object changedValue)
        {
            Type type = openType.MakeGenericType(gaType);
            object obj = Activator.CreateInstance(type);

            FieldInfo fi = GetField(type, fieldName);
            Assert.Equal(initialValue, fi.GetValue(obj));

            fi.SetValue(obj, changedValue);
            Assert.Equal(changedValue, fi.GetValue(obj));

            fi.SetValue(obj, null);
            Assert.Equal(initialValue, fi.GetValue(obj));
        }

        [Fact]
        public void SecurityAttributes()
        {
            FieldInfo info = GetField(typeof(FieldInfoTests), nameof(FieldInfoTests.s_intField));

            Assert.True(info.IsSecurityCritical);
            Assert.False(info.IsSecuritySafeCritical);
            Assert.False(info.IsSecurityTransparent);
        }

        [Fact]
        public static void SetNullOnValueTypeDefaultToZero()
        {
            FieldInfoTests obj = new();
            FieldInfo info = GetField(typeof(FieldInfoTests), nameof(intField));

            info.SetValue(obj, 5);
            Assert.Equal(5, obj.intField);
            info.SetValue(obj, null);
            Assert.Equal(0, obj.intField);
        }

        private static FieldInfo GetField(Type type, string name)
        {
            return type.GetTypeInfo().DeclaredFields.FirstOrDefault(fieldInfo => fieldInfo.Name.Equals(name));
        }

        public readonly int readonlyIntField = 1;
        public const int ConstIntField = 1222;
        public const string ConstStringField = "Hello";
        public const char ConstCharField = 'c';
        public const bool ConstBoolField = true;
        public const float ConstFloatField = (float)22 / 7;
        public const double ConstDoubleField = 22.33;
        public const long ConstInt64Field = 1000;
        public const byte ConstByteField = 0;

        public static int s_intField = 100;
        public static string s_stringField = "static";

        public int intField = 101;
        public string stringField = "non static";

        public enum ShortEnum : short {}
        public enum IntEnum {}
        public enum LongEnum : long {}
        public ShortEnum shortEnumField;
        public IntEnum intEnumField;
        public LongEnum longEnumField;

        private int privateIntField = 1;
        private string privateStringField = "privateStringField";

        [Attr(77, name = "AttrSimple"),
        Int32Attr(77, name = "Int32AttrSimple"),
        Int64Attr(77, name = "Int64AttrSimple"),
        StringAttr("hello", name = "StringAttrSimple"),
        EnumAttr(PublicEnum.Case1, name = "EnumAttrSimple"),
        TypeAttr(typeof(object), name = "TypeAttrSimple")]
        public string fieldWithAttributes = "";

        private static object s_assemblyField1 = null; // Without keyword
        private static object s_assemblyField2 = null; // With private keyword
        protected static object s_assemblyField3 = null; // With protected keyword
        public static object s_assemblyField4 = null; // With public keyword
        internal static object s_assemblyField5 = null; // With internal keyword

        public static FI_GenericClass<int> g_int = new FI_GenericClass<int>();
        public static FI_GenericClassField<int> pfg_int = new FI_GenericClassField<int>();
        public static FI_StaticGenericField<int> sfg_int = new FI_StaticGenericField<int>();
        public static int[] gpa_int = new int[] { 300, 400 };
        public static FI_GenericClass<int>[] ga_int = new FI_GenericClass<int>[] { g_int };

        public static FI_GenericClass<string> g_string = new FI_GenericClass<string>();
        public static FI_GenericClassField<string> pfg_string = new FI_GenericClassField<string>();
        public static FI_StaticGenericField<string> sfg_string = new FI_StaticGenericField<string>();
        public static string[] gpa_string = new string[] { "forget", "about this" };
        public static FI_GenericClass<string>[] ga_string = new FI_GenericClass<string>[] { g_string, g_string };

        public static FI_GenericClass<object> g_object = new FI_GenericClass<object>();
        public static FI_GenericClassField<object> pfg_object = new FI_GenericClassField<object>();
        public static FI_StaticGenericField<object> sfg_object = new FI_StaticGenericField<object>();
        public static object[] gpa_object = new object[] { "world", 300, g_object };
        public static FI_GenericClass<object>[] ga_object = new FI_GenericClass<object>[] { g_object, g_object, g_object };

        public static FI_GenericClass<FI_GenericClass<object>> g_g_object = new FI_GenericClass<FI_GenericClass<object>>();
        public static FI_GenericClassField<FI_GenericClass<object>> pfg_g_object = new FI_GenericClassField<FI_GenericClass<object>>();
        public static FI_StaticGenericField<FI_GenericClass<object>> sfg_g_object = new FI_StaticGenericField<FI_GenericClass<object>>();
        public static FI_GenericClass<object>[] gpa_g_object = new FI_GenericClass<object>[] { g_object, g_object };
        public static FI_GenericClass<FI_GenericClass<object>>[] ga_g_object = new FI_GenericClass<FI_GenericClass<object>>[] { g_g_object, g_g_object, g_g_object, g_g_object };

        public class FI_BaseClass { }
        public class FI_SubClass : FI_BaseClass { }

        public interface FI_Interface { }
        public class FI_ClassWithInterface1 : FI_Interface { }
        public class FI_ClassWithInterface2 : FI_Interface { }

        public class FI_FieldArray
        {
            public FI_BaseClass[] aArray;
            public FI_SubClass[] bArray;

            public FI_Interface[] iArray;

            public int[] intArray;
            public object[] objectArray;
        }

        public class FI_GenericClass<T> { public FI_GenericClass() { } }

        public class FI_GenericClassField<T>
        {
            public T genparamField;
            public T[] gparrayField;
            public FI_GenericClass<T> dependField;
            public FI_GenericClass<T>[] arrayField;
            public FI_GenericClassField<T> selfField;
        }

        public class FI_StaticGenericField<T>
        {
            public static T genparamField;
            public static T[] gparrayField;
            public static FI_GenericClass<T> dependField;
            public static FI_GenericClass<T>[] arrayField;
            public static FI_StaticGenericField<T> selfField;
        }

        struct FieldData
        {
            public Inner inner;
        }

        struct Inner
        {
            public object field;
        }

        [Theory]
        [InlineData(222)]
        [InlineData("new value")]
        [InlineData('A')]
        [InlineData(false)]
        [InlineData(4.56f)]
        [InlineData(double.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData(byte.MaxValue)]
        [InlineData(null)]
        public static void SetValueDirect_GetValueDirectRoundDataTest(object value)
        {
            FieldData testField = new FieldData { inner = new Inner() { field = -1 } };
            FieldInfo innerFieldInfo = typeof(FieldData).GetField(nameof(FieldData.inner));
            FieldInfo[] fields = { innerFieldInfo };
            FieldInfo fieldFieldInfo = typeof(Inner).GetField(nameof(Inner.field));
            TypedReference reference = TypedReference.MakeTypedReference(testField, fields);
            fieldFieldInfo.SetValueDirect(reference, value);
            object result = fieldFieldInfo.GetValueDirect(reference);

            Assert.Equal(value, result);
        }
    }
}
