// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.TypeTests
{
    public class GetFieldsTests
    {
        public static void TryGetFields(string AssemblyQualifiedNameOfTypeToGet, string[] fieldsExpected)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            FieldInfo[] fieldsReturned = typeToCheck.GetFields();
            Assert.Equal(fieldsExpected.Length, fieldsReturned.Length);
            int foundIndex;
            Array.Sort(fieldsExpected);
            for (int i = 0; i < fieldsReturned.Length; i++)
            {
                foundIndex = Array.BinarySearch(fieldsExpected, fieldsReturned[i].ToString());
                Assert.False(foundIndex < 0, "An unexpected field " + fieldsReturned[i].ToString() + " was returned");
            }
        }

        public static string ArrayToCommaList(Type[] ArrayToConvert)
        {
            string returnString = "";
            if (ArrayToConvert.Length > 0)
            {
                returnString = ArrayToConvert[0].ToString();
                for (int i = 1; i < ArrayToConvert.Length; i++)
                {
                    returnString += ", " + ArrayToConvert[i].ToString();
                }
            }
            return returnString;
        }

        public static string ArrayToCommaList(FieldInfo[] ArrayToConvert)
        {
            string returnString = "";
            if (ArrayToConvert.Length > 0)
            {
                returnString = ArrayToConvert[0].ToString();
                for (int i = 1; i < ArrayToConvert.Length; i++)
                {
                    returnString += ", " + ArrayToConvert[i].ToString();
                }
            }
            return returnString;
        }

        public static string ArrayToCommaList(string[] ArrayToConvert)
        {
            string returnString = "";
            if (ArrayToConvert.Length > 0)
            {
                returnString = ArrayToConvert[0].ToString();
                for (int i = 1; i < ArrayToConvert.Length; i++)
                {
                    returnString += ", " + ArrayToConvert[i].ToString();
                }
            }
            return returnString;
        }

        [Fact]
        public void Test1()
        {
            TryGetFields("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassUsingNestedInterfaces`2[System.String,System.Int32]", new string[] { "System.String FieldZero", "System.String FieldOne", "Int32 FieldTwo", "Int32 FieldThree" });
        }

        [Fact]
        public void Test2()
        {
            TryGetFields("System.Reflection.Compatibility.UnitTests.TypeTests.GenericStructWithInterface`1[System.String]", new string[] { "Int32 field2", "System.String field" });
        }

        [Fact]
        public void Test3()
        {
            TryGetFields("System.Reflection.Compatibility.UnitTests.TypeTests.NonGenericClassWithGenericInterface", new string[] { "Int32 field" });
        }

        [Fact]
        public void Test4()
        {
            TryGetFields("System.Reflection.Compatibility.UnitTests.TypeTests.GenericStruct2TP`2[System.Int32,System.String]", new string[] { "System.String field2", "Int32 field" });
        }

        [Fact]
        public void Test5()
        {
            TryGetFields("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithVarArgMethod`1", new string[] { "T field" });
        }
    }
}
