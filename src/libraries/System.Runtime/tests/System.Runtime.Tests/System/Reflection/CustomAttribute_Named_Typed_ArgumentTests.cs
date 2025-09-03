// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Reflection.Tests
{
    public static class CustomAttribute_Named_Typed_ArgumentTests
    {
        [Fact]
        public static void Test_CustomAttributeNamedTypedArgument_Constructor()
        {
            AssertExtensions.Throws<ArgumentNullException>("memberInfo", () => new CustomAttributeNamedArgument(null, null));

            MethodInfo m = typeof(CustomAttribute_Named_Typed_ArgumentTests).GetMethod("MyMethod", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(m))
            {
                foreach (CustomAttributeTypedArgument cata in cad.ConstructorArguments)
                {
                    Assert.True(cata.ArgumentType == typeof(MyKinds));
                    Assert.Equal("0", cata.Value.ToString());
                }

                foreach (CustomAttributeNamedArgument cana in cad.NamedArguments)
                {
                    Assert.Equal("System.String Desc", cana.MemberInfo.ToString());
                    Assert.True(cana.TypedValue.ArgumentType == typeof(string));
                    Assert.Equal("This is a description on a method", cana.TypedValue.Value.ToString());
                }
                return;
            }

            Assert.Fail("Expected to find MyAttr Attribute");
        }

        [Fact]
        public static void Test_CustomAttributeTypedArgument_Constructor()
        {
            Type t = typeof(MyClass);
            foreach (CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(t))
            {
                foreach (CustomAttributeTypedArgument cata in cad.ConstructorArguments)
                {
                    Assert.True(cata.ArgumentType == typeof(MyKinds));
                    Assert.Equal("1", cata.Value.ToString());
                    return;
                }
            }

            Assert.Fail("Expected to find MyAttr Attribute");
        }

        [Fact]
        public static void Test_CustomAttributeTypedArgument_Equals()
        {
            Type t = typeof(MyClass);
            foreach (CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(t))
            {
                foreach (CustomAttributeTypedArgument cata in cad.ConstructorArguments)
                {
                    Assert.True(cata.Equals(cata));
                    Assert.True(cata.Equals((object)cata));

                    var notEqualArgument = new CustomAttributeTypedArgument(new [] { new CustomAttributeTypedArgument(0) });
                    Assert.False(cata.Equals(notEqualArgument));
                    Assert.False(cata.Equals((object)notEqualArgument));

                    return;
                }
            }

            Assert.Fail("Expected to find MyAttr Attribute");
        }

        [Fact]
        public static void Test_CustomAttributeTypedArgument_ToString()
        {
            var argument = new CustomAttributeTypedArgument(new [] { new CustomAttributeTypedArgument(0) });

            Assert.Equal("new CustomAttributeTypedArgument[1] { (Int32)0 }", argument.ToString());
        }

        [Fact]
        public static void Test_CustomAttributeTypedArgument_ArrayValue_ShouldBeReadOnlyCollection()
        {
            // Test case for issue #119292: CustomAttributeTypedArgument.Value should return ReadOnlyCollection<CustomAttributeTypedArgument> for arrays
            var attr = typeof(TestClassWithArray).CustomAttributes.Single(d => d.AttributeType == typeof(TestArrayAttribute));
            var arg = attr.ConstructorArguments.Single();

            // The ArgumentType should be an array type
            Assert.True(arg.ArgumentType.IsArray);
            
            // The Value should be a ReadOnlyCollection<CustomAttributeTypedArgument>
            Assert.IsAssignableFrom<System.Collections.ObjectModel.ReadOnlyCollection<CustomAttributeTypedArgument>>(arg.Value);
            
            // ToString() should work without throwing InvalidCastException
            var result = arg.ToString();
            Assert.NotNull(result);
        }

        [MyAttr(MyKinds.First, Desc = "This is a description on a method")]
        private static void MyMethod() { }
    }

    internal enum MyKinds {
        First,
        Second
    };

    [AttributeUsage(AttributeTargets.All)]
    internal class MyAttr : Attribute
    {
        private MyKinds kindVal;
        private string desc;

        public MyAttr(MyKinds kind)
        {
            kindVal = kind;
        }

        public string Desc
        {
            get { return desc; }
            set { desc = value; }
        }
    }

    [MyAttr(MyKinds.Second)]
    internal class MyClass
    {
#pragma warning disable 0649
        public string str;
#pragma warning restore 0649
    }

    // Test classes for array CustomAttributeTypedArgument bug #119292
    public class TestArrayAttribute : Attribute
    {
        public unsafe TestArrayAttribute(params TestGeneric<delegate*<void>[]>.TestEnum[] a) { }
    }

    public class TestGeneric<T>
    {
        public enum TestEnum { Value1, Value2 }
    }

    [TestArrayAttribute(new TestGeneric<delegate*<void>[]>.TestEnum())]
    public unsafe class TestClassWithArray { }
}
