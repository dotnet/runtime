// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.TypeTests
{
    public class GetMemberTests
    {
        public static void TryGetMember(string AssemblyQualifiedNameOfTypeToGet, string memberToFind, string[] expectedMemberName)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            MemberInfo[] memberReturned = typeToCheck.GetMember(memberToFind);
            Assert.Equal(expectedMemberName.Length, memberReturned.Length);
            if (expectedMemberName.Length != 0)
            {
                int foundIndex;
                Array.Sort(expectedMemberName);
                for (int i = 0; i < memberReturned.Length; i++)
                {
                    foundIndex = Array.BinarySearch(expectedMemberName, memberReturned[i].ToString());
                    Assert.False(foundIndex < 0, "An unexpected member " + memberReturned[i].ToString() + " was returned");
                }
            }
        }



        public static void TryGetMember(string AssemblyQualifiedNameOfTypeToGet, string memberToFind, BindingFlags bindingAttr, string[] expectedMemberName)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            MemberInfo[] memberReturned = typeToCheck.GetMember(memberToFind, bindingAttr);
            Assert.Equal(expectedMemberName.Length, memberReturned.Length);
            if (expectedMemberName.Length != 0)
            {
                int foundIndex;
                Array.Sort(expectedMemberName);
                for (int i = 0; i < memberReturned.Length; i++)
                {
                    foundIndex = Array.BinarySearch(expectedMemberName, memberReturned[i].ToString());
                    Assert.False(foundIndex < 0, "An unexpected member " + memberReturned[i].ToString() + " was returned");
                }
            }
        }

        public static string ArrayToCommaList(string[] ArrayToConvert)
        {
            string returnString = "";
            if (ArrayToConvert.Length > 0)
            {
                returnString = ArrayToConvert[0];
                for (int i = 1; i < ArrayToConvert.Length; i++)
                {
                    returnString += ", " + ArrayToConvert[i];
                }
            }
            return returnString;
        }

        public static string ArrayToCommaList(MemberInfo[] ArrayToConvert)
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
            TryGetMember("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassUsingNestedInterfaces`2[System.String,System.Int32]", "Field*", new string[] { "System.String FieldZero", "System.String FieldOne", "Int32 FieldTwo", "Int32 FieldThree" });
        }

        [Fact]
        public void Test2()
        {
            TryGetMember("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassUsingNestedInterfaces`2[System.String,System.Int32]", "Return*", new string[] { "System.String ReturnAndSetFieldZero(System.String)", "Int32 ReturnAndSetFieldThree(Int32)" });
        }

        [Fact]
        public void Test3()
        {
            TryGetMember("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithInterface`1[System.Int32]", "*", new string[] { "W GenericMethod[W](W)", "Int32 ReturnAndSetFieldZero(Int32)", "Boolean Equals(System.Object)", "Int32 GetHashCode()", "System.Type GetType()", "System.String ToString()", "Void .ctor(Int32)", "Int32 field" });
        }

        [Fact]
        public void Test4()
        {
            TryGetMember("System.Reflection.Compatibility.UnitTests.TypeTests.IGenericInterface`1", "ReturnAndSetFieldZero", new string[] { "T ReturnAndSetFieldZero(T)" });
        }

        [Fact]
        public void Test5()
        {
            TryGetMember("System.Reflection.Compatibility.UnitTests.TypeTests.GenericArrayWrapperClass`1", "*", new string[] { "Int32 get_myProperty()", "Void set_myProperty(Int32)", "T get_Item(Int32)", "Void set_Item(Int32, T)", "Boolean Equals(System.Object)", "Int32 GetHashCode()", "System.Type GetType()", "System.String ToString()", "Void .ctor(T[])", "Int32 myProperty", "T Item [Int32]" });
        }

        [Fact]
        public void Test6()
        {
            TryGetMember("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", "*", new string[] { "Void add_WeightChanged(System.EventHandler)", "Void remove_WeightChanged(System.EventHandler)", "System.Object[] get_StuffConsumed()", "Void Eat(System.Object)", "System.Object[] Puke(Int32)", "Boolean Equals(System.Object)", "Int32 GetHashCode()", "System.Type GetType()", "System.String ToString()", "Void .ctor()", "System.Object[] StuffConsumed", "System.EventHandler WeightChanged" });
        }

        [Fact]
        public void Test7()
        {
            BindingFlags allBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            TryGetMember("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", "*", allBindingFlags, new string[] { "Void add_WeightChanged(System.EventHandler)", "Void remove_WeightChanged(System.EventHandler)", "Void add_WeightStayedTheSame(System.EventHandler)", "Void remove_WeightStayedTheSame(System.EventHandler)", "System.Object[] get_StuffConsumed()", "Void Eat(System.Object)", "System.Object[] Puke(Int32)", "Void .ctor()", "System.Object[] StuffConsumed", "System.EventHandler WeightChanged", "System.EventHandler WeightStayedTheSame", "System.Collections.Generic.List`1[System.Object] _pStuffConsumed", "System.EventHandler WeightChanged", "System.EventHandler WeightStayedTheSame", "System.EventHandler s_catDisappeared" });
        }

        [Fact]
        public void Test8()
        {
            BindingFlags allBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            TryGetMember("System.Reflection.Compatibility.UnitTests.TypeTests.GenericArrayWrapperClass`1", "*", allBindingFlags, new string[] { "Int32 get_myProperty()", "Void set_myProperty(Int32)", "T get_Item(Int32)", "Void set_Item(Int32, T)", "Void .ctor(T[])", "Int32 myProperty", "T Item [Int32]", "T[] _field", "Int32 _field1" });
        }
    }
}