// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 414, 169, 649

namespace System.Reflection.Tests
{
    public class FieldInfoTests
    {
        [Theory]
        [InlineData("s_privateField", FieldAttributes.Private | FieldAttributes.Static, typeof(int))]
        [InlineData("s_publicField", FieldAttributes.Public | FieldAttributes.Static, typeof(string))]
        [InlineData("s_protectedField", FieldAttributes.Family | FieldAttributes.Static, typeof(BindingFlags))]
        [InlineData("s_internalField", FieldAttributes.Assembly | FieldAttributes.Static, typeof(int?))]
        [InlineData("s_protectedInternalField", FieldAttributes.FamORAssem | FieldAttributes.Static, typeof(int[]))]
        [InlineData("_privateField", FieldAttributes.Private, typeof(FI_EmptyStruct))]
        [InlineData("_publicField", FieldAttributes.Public, typeof(FI_EquatableClass))]
        [InlineData("_protectedField", FieldAttributes.Family, typeof(FI_GenericClass<int>))]
        [InlineData("_internalField", FieldAttributes.Assembly, typeof(FI_GenericClass<string>.NestedClass))]
        [InlineData("_protectedInternalField", FieldAttributes.FamORAssem, typeof(FI_EmptyInterface))]
        [InlineData("ConstField", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, typeof(FI_Enum))]
        public void Properties(string name, FieldAttributes attributes, Type fieldType)
        {
            FieldInfo field = TypeExtensions.GetField(typeof(FI_BaseClass), name, Helpers.AllFlags);
            Assert.Equal(name, field.Name);
            Assert.Equal(attributes, field.Attributes);

            Assert.Equal((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly, field.IsAssembly);
            Assert.Equal((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family, field.IsFamily);
            Assert.Equal((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem, field.IsFamilyAndAssembly);
            Assert.Equal((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem, field.IsFamilyOrAssembly);
            Assert.Equal((attributes & FieldAttributes.InitOnly) != 0, field.IsInitOnly);
            Assert.Equal((attributes & FieldAttributes.Literal) != 0, field.IsLiteral);
            Assert.Equal((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public, field.IsPublic);
            Assert.Equal((attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private, field.IsPrivate);
            Assert.Equal((attributes & FieldAttributes.Static) != 0, field.IsStatic);

            Assert.Equal(fieldType, field.FieldType);
            Assert.Equal(typeof(FI_BaseClass), field.DeclaringType);
            Assert.Equal(field.DeclaringType.GetTypeInfo().Module, field.Module);
            Assert.Equal(MemberTypes.Field, field.MemberType);
        }

        public static IEnumerable<object[]> GetValue_TestData()
        {
            // Trick trimming into keeping the Empty field
            if (string.Empty.Length > 0)
                typeof(string).GetField("Empty");

            yield return new object[] { typeof(string), "Empty", "abc", "" };

            yield return new object[] { typeof(FI_BaseClass), "_privateStringField", new FI_BaseClass(), "2" };
            yield return new object[] { typeof(FI_SubClass), "_protectedIntField", new FI_SubClass(), 3 };
            yield return new object[] { typeof(FI_BaseClass), "_privateNullableIntField", new FI_BaseClass(), null };
            yield return new object[] { typeof(FI_BaseClass), "_publicField", new FI_BaseClass(), new FI_EquatableClass() { ID = 42 } };

            yield return new object[] { typeof(FI_BaseClass), "_protectedField", new FI_BaseClass(), new FI_GenericClass<int>() { ID = 24 } };
            yield return new object[] { typeof(FI_BaseClass), "s_privateField", null, 1 };
        }

        [Theory]
        [MemberData(nameof(GetValue_TestData))]
        public void GetValue(Type type, string name, object obj, object value)
        {
            FieldInfo field = TypeExtensions.GetField(type, name, Helpers.AllFlags);
            Assert.Equal(value, field.GetValue(obj));
        }

        [Fact]
        public void GetValue_NullTarget_ThrowsTargetException()
        {
            FieldInfo field = TypeExtensions.GetField(typeof(FI_BaseClass), "_privateStringField", Helpers.AllFlags);
            Assert.Throws<TargetException>(() => field.GetValue(null));
        }
    }

    public class FI_BaseClass
    {
        private static int s_privateField = 1;
        public static string s_publicField;
        protected static BindingFlags s_protectedField;
        internal static int? s_internalField;
        protected internal static int[] s_protectedInternalField;

        private string _privateStringField = "2";
        protected int _protectedIntField = 3;
        private int? _privateNullableIntField;
        private FI_EmptyStruct _privateField;
        public FI_EquatableClass _publicField = new FI_EquatableClass() { ID = 42 };
        protected FI_GenericClass<int> _protectedField = new FI_GenericClass<int>() { ID = 24 };
        internal FI_GenericClass<string>.NestedClass _internalField;
        protected internal FI_EmptyInterface _protectedInternalField;

        public const FI_Enum ConstField = FI_Enum.Case1;
    }

    public class FI_SubClass : FI_BaseClass { }

    public struct FI_EmptyStruct { }
    public class FI_EquatableClass
    {
        public int ID { get; set; }
        public override bool Equals(object other) => ID.Equals(((FI_EquatableClass)other).ID);
        public override int GetHashCode() => ID;
    }

    public class FI_GenericClass<T>
    {
        public T ID { get; set; }
        public override bool Equals(object other) => ID.Equals(((FI_GenericClass<T>)other).ID);
        public override int GetHashCode() => ID.GetHashCode();

        public class NestedClass { }
    }

    public interface FI_EmptyInterface { }
    public enum FI_Enum { Case1, Case2 }
}
