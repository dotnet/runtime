// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

            Assert.Equal("new CustomAttributeTypedArgument[1] { 0 }", argument.ToString());
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
}
