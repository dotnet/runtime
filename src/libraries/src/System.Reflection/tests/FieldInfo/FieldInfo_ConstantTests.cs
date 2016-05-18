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
        [Theory]
        [InlineData("constIntField", 222)]
        [InlineData("constStrField", "new value")]
        [InlineData("charField", 'A')]
        [InlineData("boolField", false)]
        [InlineData("floatField", 4.56)]
        [InlineData("doubleField", double.MaxValue)]
        [InlineData("int64Field", long.MaxValue)]
        [InlineData("byteField", byte.MaxValue)]
        public void SetValue_ConstantField_Fails(string fieldName, object newValue)
        {
            FieldInfo fi = GetField(fieldName);
            FieldInfoConstantTests myInstance = new FieldInfoConstantTests();

            Assert.NotNull(fi);
            Assert.Equal(fieldName, fi.Name);
            Assert.Throws<FieldAccessException>(() => fi.SetValue(myInstance, newValue));
        }

        [Fact]
        public void SetValue_ReadOnlyIntField_Succeeds()
        {
            FieldInfo fi = typeof(FieldInfoConstantTests).GetTypeInfo().GetDeclaredField("readOnlyIntField");
            FieldInfoConstantTests myInstance = new FieldInfoConstantTests();

            Assert.NotNull(fi);
            Assert.NotNull(myInstance);

            object current = fi.GetValue(myInstance);
            Assert.Equal(1, current);

            fi.SetValue(myInstance, int.MinValue);

            Assert.Equal(int.MinValue, fi.GetValue(myInstance));
        }

        // Helper method to get field from Type type
        private static FieldInfo GetField(string fieldName)
        {
            Type t = typeof(FieldInfoConstantTests);
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<FieldInfo> alldefinedFields = ti.DeclaredFields.GetEnumerator();
            FieldInfo fi = null, found = null;

            while (alldefinedFields.MoveNext())
            {
                fi = alldefinedFields.Current;
                if (fi.Name.Equals(fieldName))
                {
                    //found type
                    found = fi;
                    break;
                }
            }

            return found;
        }

        // Fields for Reflection

        public readonly int readOnlyIntField = 1;
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
