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
    }
}
