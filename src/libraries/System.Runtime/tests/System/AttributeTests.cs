// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public static partial class AttributeTests
    {
        [Fact]
        public static void DefaultEquality()
        {
            var a1 = new ParentAttribute { Prop = 1 };
            var a2 = new ParentAttribute { Prop = 42 };
            var a3 = new ParentAttribute { Prop = 1 };

            var d1 = new ChildAttribute { Prop = 1 };
            var d2 = new ChildAttribute { Prop = 42 };
            var d3 = new ChildAttribute { Prop = 1 };

            var s1 = new GrandchildAttribute { Prop = 1 };
            var s2 = new GrandchildAttribute { Prop = 42 };
            var s3 = new GrandchildAttribute { Prop = 1 };

            var f1 = new ChildAttributeWithField { Prop = 1 };
            var f2 = new ChildAttributeWithField { Prop = 42 };
            var f3 = new ChildAttributeWithField { Prop = 1 };

            Assert.NotEqual(a1, a2);
            Assert.NotEqual(a2, a3);
            Assert.Equal(a1, a3);

            // The implementation of Attribute.Equals uses reflection to
            // enumerate fields. On .NET core, we add `BindingFlags.DeclaredOnly`
            // to fix a bug where an instance of a subclass of an attribute can
            // be equal to an instance of the parent class.
            // See https://github.com/dotnet/coreclr/pull/6240
            Assert.Equal(false, d1.Equals(d2));
            Assert.Equal(false, d2.Equals(d3));
            Assert.Equal(d1, d3);

            Assert.Equal(false, s1.Equals(s2));
            Assert.Equal(false, s2.Equals(s3));
            Assert.Equal(s1, s3);

            Assert.Equal(false, f1.Equals(f2));
            Assert.Equal(false, f2.Equals(f3));
            Assert.Equal(f1, f3);

            Assert.NotEqual(d1, a1);
            Assert.NotEqual(d2, a2);
            Assert.NotEqual(d3, a3);
            Assert.NotEqual(d1, a3);
            Assert.NotEqual(d3, a1);
            
            Assert.NotEqual(d1, s1);
            Assert.NotEqual(d2, s2);
            Assert.NotEqual(d3, s3);
            Assert.NotEqual(d1, s3);
            Assert.NotEqual(d3, s1);

            Assert.NotEqual(f1, a1);
            Assert.NotEqual(f2, a2);
            Assert.NotEqual(f3, a3);
            Assert.NotEqual(f1, a3);
            Assert.NotEqual(f3, a1);
        }

        [Fact]
        public static void DefaultHashCode()
        {
            var a1 = new ParentAttribute { Prop = 1 };
            var a2 = new ParentAttribute { Prop = 42 };
            var a3 = new ParentAttribute { Prop = 1 };
            
            var d1 = new ChildAttribute { Prop = 1 };
            var d2 = new ChildAttribute { Prop = 42 };
            var d3 = new ChildAttribute { Prop = 1 };

            var s1 = new GrandchildAttribute { Prop = 1 };
            var s2 = new GrandchildAttribute { Prop = 42 };
            var s3 = new GrandchildAttribute { Prop = 1 };

            var f1 = new ChildAttributeWithField { Prop = 1 };
            var f2 = new ChildAttributeWithField { Prop = 42 };
            var f3 = new ChildAttributeWithField { Prop = 1 }; 

            Assert.NotEqual(a1.GetHashCode(), 0);
            Assert.NotEqual(a2.GetHashCode(), 0);
            Assert.NotEqual(a3.GetHashCode(), 0);
            Assert.NotEqual(d1.GetHashCode(), 0);
            Assert.NotEqual(d2.GetHashCode(), 0);
            Assert.NotEqual(d3.GetHashCode(), 0);
            Assert.NotEqual(s1.GetHashCode(), 0);
            Assert.NotEqual(s2.GetHashCode(), 0);
            Assert.NotEqual(s3.GetHashCode(), 0);
            Assert.Equal(f1.GetHashCode(), 0);
            Assert.Equal(f2.GetHashCode(), 0);
            Assert.Equal(f3.GetHashCode(), 0);

            Assert.NotEqual(a1.GetHashCode(), a2.GetHashCode());
            Assert.NotEqual(a2.GetHashCode(), a3.GetHashCode());
            Assert.Equal(a1.GetHashCode(), a3.GetHashCode());

            // The implementation of Attribute.GetHashCode uses reflection to
            // enumerate fields. On .NET core, we add `BindingFlags.DeclaredOnly`
            // to fix a bug where the hash code of a subclass of an attribute can
            // be equal to an instance of the parent class.
            // See https://github.com/dotnet/coreclr/pull/6240
            Assert.Equal(false, s1.GetHashCode().Equals(s2.GetHashCode()));
            Assert.Equal(false, s2.GetHashCode().Equals(s3.GetHashCode()));
            Assert.Equal(s1.GetHashCode(), s3.GetHashCode());

            Assert.Equal(false, d1.GetHashCode().Equals(d2.GetHashCode()));
            Assert.Equal(false, d2.GetHashCode().Equals(d3.GetHashCode()));
            Assert.Equal(d1.GetHashCode(), d3.GetHashCode());

            Assert.Equal(f1.GetHashCode(), f2.GetHashCode());
            Assert.Equal(f2.GetHashCode(), f3.GetHashCode());
            Assert.Equal(f1.GetHashCode(), f3.GetHashCode());

            Assert.Equal(true, d1.GetHashCode().Equals(a1.GetHashCode()));
            Assert.Equal(true, d2.GetHashCode().Equals(a2.GetHashCode()));
            Assert.Equal(true, d3.GetHashCode().Equals(a3.GetHashCode()));
            Assert.Equal(true, d1.GetHashCode().Equals(a3.GetHashCode()));
            Assert.Equal(true, d3.GetHashCode().Equals(a1.GetHashCode()));

            Assert.Equal(true, d1.GetHashCode().Equals(s1.GetHashCode()));
            Assert.Equal(true, d2.GetHashCode().Equals(s2.GetHashCode()));
            Assert.Equal(true, d3.GetHashCode().Equals(s3.GetHashCode()));
            Assert.Equal(true, d1.GetHashCode().Equals(s3.GetHashCode()));
            Assert.Equal(true, d3.GetHashCode().Equals(s1.GetHashCode()));

            Assert.NotEqual(f1.GetHashCode(), a1.GetHashCode());
            Assert.NotEqual(f2.GetHashCode(), a2.GetHashCode());
            Assert.NotEqual(f3.GetHashCode(), a3.GetHashCode());
            Assert.NotEqual(f1.GetHashCode(), a3.GetHashCode());
            Assert.NotEqual(f3.GetHashCode(), a1.GetHashCode());
        }

        class ParentAttribute : Attribute
        {
            public int Prop {get;set;}
        }

        class ChildAttribute : ParentAttribute { }
        class GrandchildAttribute : ChildAttribute { }
        class ChildAttributeWithField : ParentAttribute 
        { 
            public int Field = 0;
        }

        [Fact]
        [StringValue("\uDFFF")]
        public static void StringArgument_InvalidCodeUnits_FallbackUsed()
        {
            MethodInfo thisMethod = typeof(AttributeTests).GetTypeInfo().GetDeclaredMethod("StringArgument_InvalidCodeUnits_FallbackUsed");
            Assert.NotNull(thisMethod);

            CustomAttributeData cad = thisMethod.CustomAttributes.Where(ca => ca.AttributeType == typeof(StringValueAttribute)).FirstOrDefault();
            Assert.NotNull(cad);

            string stringArg = cad.ConstructorArguments[0].Value as string;
            Assert.NotNull(stringArg);
            Assert.Equal("\uFFFD\uFFFD", stringArg);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new StringValueAttribute("hello"), new StringValueAttribute("hello"), true, true };
            yield return new object[] { new StringValueAttribute("hello"), new StringValueAttribute("foo"), false, false };

            yield return new object[] { new StringValueIntValueAttribute("hello", 1), new StringValueIntValueAttribute("hello", 1), true, true };
            yield return new object[] { new StringValueIntValueAttribute("hello", 1), new StringValueIntValueAttribute("hello", 2), false, true }; // GetHashCode() ignores the int value

            yield return new object[] { new EmptyAttribute(), new EmptyAttribute(), true, true };

            yield return new object[] { new StringValueAttribute("hello"), new StringValueIntValueAttribute("hello", 1), false, true }; // GetHashCode() ignores the int value
            yield return new object[] { new StringValueAttribute("hello"), "hello", false, false };
            yield return new object[] { new StringValueAttribute("hello"), null, false, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals(Attribute attr1, object obj, bool expected, bool hashEqualityExpected)
        {
            Assert.Equal(expected, attr1.Equals(obj));

            Attribute attr2 = obj as Attribute;
            if (attr2 != null)
            {
                Assert.Equal(hashEqualityExpected, attr1.GetHashCode() == attr2.GetHashCode());
            }
        }

        [AttributeUsage(AttributeTargets.Method)]
        private sealed class StringValueAttribute : Attribute
        {
            public string StringValue;
            public StringValueAttribute(string stringValue)
            {
                StringValue = stringValue;
            }
        }

        private sealed class StringValueIntValueAttribute : Attribute
        {
            public string StringValue;
            private int IntValue;

            public StringValueIntValueAttribute(string stringValue, int intValue)
            {
                StringValue = stringValue;
                IntValue = intValue;
            }
        }

        [AttributeUsage(AttributeTargets.Method)]
        private sealed class EmptyAttribute : Attribute { }

        [Fact]
        public static void ValidateDefaults()
        {
            StringValueAttribute sav =  new StringValueAttribute("test");
            Assert.Equal(false, sav.IsDefaultAttribute());
            Assert.Equal(sav.GetType(), sav.TypeId);
            Assert.Equal(true, sav.Match(sav));
        }
    }
}
