// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class FieldInfoConstantTests
    {
        //Verify SetValue method does not set value for constant Fields
        [Fact]
        public void TestSetValue_constIntField()
        {
            TestSetValue_constantField("constIntField", (object)222);
        }

        //Verify SetValue method does not set value for constant Fields
        [Fact]
        public void TestSetValue_constStrField()
        {
            TestSetValue_constantField("constStrField", (object)"new value");
        }

        //Verify SetValue method does not set value for constant Fields
        [Fact]
        public void TestSetValue_constCharField()
        {
            TestSetValue_constantField("charField", (object)'A');
        }

        //Verify SetValue method does not set value for constant Fields
        [Fact]
        public void TestSetValue_constboolField()
        {
            TestSetValue_constantField("boolField", (object)false);
        }

        //Verify SetValue method does not set value for constant Fields
        [Fact]
        public void TestSetValue_constfloatField()
        {
            TestSetValue_constantField("floatField", (object)4.56);
        }

        //Verify SetValue method does not set value for constant Fields
        [Fact]
        public void TestSetValue_constdoubleField()
        {
            TestSetValue_constantField("doubleField", (object)Double.MaxValue);
        }

        //Verify SetValue method does not set value for constant Fields
        [Fact]
        public void TestSetValue_constInt64Field()
        {
            TestSetValue_constantField("int64Field", (object)Int64.MaxValue);
        }

        //Verify SetValue method does not set value for constant Fields
        [Fact]
        public void TestSetValue_constbyteField()
        {
            TestSetValue_constantField("byteField", (object)byte.MaxValue);
        }

        //Verify SetValue method does not set value for RO Field
        [Fact]
        public void TestSetValue_roIntField()
        {
            TestSetValue_constantField("rointField", (object)Int32.MinValue);
        }

        //Helper method to Verify constant Field can not be set using Reflection
        public void TestSetValue_constantField(string field, object newvalue)
        {
            FieldInfo fi = GetField(field);
            FieldInfoConstantTests myInstance = new FieldInfoConstantTests();

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(field));

            try
            {
                fi.SetValue(myInstance, newvalue);
                Assert.False(true, "Exception expected.");
            }
            catch (Exception) { }
        }

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

        public readonly int rointField = 1;
        public const int constIntField = 1222;
        public const string constStrField = "Hello";
        public const char charField = 'c';
        public const bool boolField = true;
        public const float floatField = (float)22 / 7;
        public const double doubleField = (double)22.33;
        public const Int64 int64Field = 1000;
        public const byte byteField = 0;
    }
}
