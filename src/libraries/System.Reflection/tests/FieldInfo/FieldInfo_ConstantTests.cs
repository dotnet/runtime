// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class FieldInfoConstantTests
    {
        public static IEnumerable<object[]> FieldInfoConstants_TestData()
        {
            yield return new object[] { "constIntField", 222 };
            yield return new object[] { "constStrField", "new value" };
            yield return new object[] { "charField", 'A' };
            yield return new object[] { "boolField", false };
            yield return new object[] { "floatField", 4.56 };
            yield return new object[] { "doubleField", double.MaxValue };
            yield return new object[] { "int64Field", long.MaxValue };
            yield return new object[] { "byteField", byte.MaxValue };
        }

        // Verify SetValue method cannot set value for constant Fields using Reflection
        [Theory]
        [MemberData(nameof(FieldInfoConstants_TestData))]
        public void TestSetValue_ConstantField(string field, object newValue)
        {
            FieldInfo fi = GetField(field);
            FieldInfoConstantTests myInstance = new FieldInfoConstantTests();

            Assert.NotNull(fi);
            Assert.Equal(field, fi.Name);
            Assert.Throws<FieldAccessException>(() => fi.SetValue(myInstance, newValue));
        }

        // Verify SetValue method _does_ set value for RO Field
        [Fact]
        public void TestSetValue_RoIntField()
        {
            FieldInfo fi = typeof(FieldInfoConstantTests).GetTypeInfo().GetDeclaredField("roIntField");
            FieldInfoConstantTests myInstance = new FieldInfoConstantTests();

            Assert.NotNull(fi);
            Assert.NotNull(myInstance);

            object current = fi.GetValue(myInstance);
            Assert.Equal(1, current);

            fi.SetValue(myInstance, int.MinValue);

            Assert.Equal(int.MinValue, fi.GetValue(myInstance));
        }

        // Helper method to get field from Type type
        private static FieldInfo GetField(string field)
        {
            Type t = typeof(FieldInfoConstantTests);
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<FieldInfo> alldefinedFields = ti.DeclaredFields.GetEnumerator();
            FieldInfo fi = null, found = null;

            while (alldefinedFields.MoveNext())
            {
                fi = alldefinedFields.Current;
                if (fi.Name.Equals(field))
                {
                    //found type
                    found = fi;
                    break;
                }
            }

            return found;
        }

        //Fields for Reflection

        public readonly int roIntField = 1;
        public const int constIntField = 1222;
        public const string constStrField = "Hello";
        public const char charField = 'c';
        public const bool boolField = true;
        public const float floatField = (float)22 / 7;
        public const double doubleField = 22.33;
        public const long int64Field = 1000;
        public const byte byteField = 0;
    }
}
