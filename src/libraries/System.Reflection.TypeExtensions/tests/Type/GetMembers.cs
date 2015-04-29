// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Compatibility.UnitTests.TypeTests
{
    public class GetMembersTests
    {
        public static void TryGetMembers(string AssemblyQualifiedNameOfTypeToGet, string[] membersExpected)
        {
            Type typeToCheck; typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            MemberInfo[] membersReturned = typeToCheck.GetMembers();
            Assert.Equal(membersExpected.Length, membersReturned.Length);
            int foundIndex;
            Array.Sort(membersExpected);
            for (int i = 0; i < membersReturned.Length; i++)
            {
                foundIndex = Array.BinarySearch(membersExpected, membersReturned[i].ToString());
                Assert.False(foundIndex < 0, "An unexpected member " + membersReturned[i].ToString() + " was returned");
            }
        }

        public static void TryGetMembers(string AssemblyQualifiedNameOfTypeToGet, BindingFlags bindingAttr, string[] membersExpected)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            MemberInfo[] membersReturned = typeToCheck.GetMembers(bindingAttr);
            Assert.Equal(membersExpected.Length, membersReturned.Length);
            int foundIndex;
            Array.Sort(membersExpected);
            for (int i = 0; i < membersReturned.Length; i++)
            {
                foundIndex = Array.BinarySearch(membersExpected, membersReturned[i].ToString());
                Assert.False(foundIndex < 0, "An unexpected member " + membersReturned[i].ToString() + " was returned");
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
            BindingFlags allBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            TryGetMembers("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassUsingNestedInterfaces`2[System.String,System.Int32]", allBindingFlags, new string[] { "System.String ReturnAndSetFieldZero(System.String)", "Void SetFieldOne(Int32)", "Void SetFieldTwo(System.String)", "Int32 ReturnAndSetFieldThree(Int32)", "Void .ctor(System.String, System.String, Int32, Int32)", "System.String FieldZero", "System.String FieldOne", "Int32 FieldTwo", "Int32 FieldThree" });
        }

        [Fact]
        public void Test2()
        {
            BindingFlags allBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            TryGetMembers("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithInterface`1[System.Int32]", allBindingFlags, new string[] { "W GenericMethod[W](W)", "Int32 ReturnAndSetFieldZero(Int32)", "Void .ctor(Int32)", "Int32 field" });
        }

        [Fact]
        public void Test3()
        {
            BindingFlags allBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            TryGetMembers("System.Reflection.Compatibility.UnitTests.TypeTests.IGenericInterface`1", allBindingFlags, new string[] { "T ReturnAndSetFieldZero(T)" });
        }

        [Fact]
        public void Test4()
        {
            BindingFlags allBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            TryGetMembers("System.Reflection.Compatibility.UnitTests.TypeTests.GenericArrayWrapperClass`1", allBindingFlags, new string[] { "Int32 get_myProperty()", "Void set_myProperty(Int32)", "T get_Item(Int32)", "Void set_Item(Int32, T)", "Void .ctor(T[])", "Int32 myProperty", "T Item [Int32]", "T[] _field", "Int32 _field1" });
        }

        [Fact]
        public void Test5()
        {
            BindingFlags allBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            TryGetMembers("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", allBindingFlags, new string[] { "Void add_WeightChanged(System.EventHandler)", "Void remove_WeightChanged(System.EventHandler)", "System.Object[] get_StuffConsumed()", "Void Eat(System.Object)", "System.Object[] Puke(Int32)", "Void .ctor()", "System.Object[] StuffConsumed", "System.EventHandler WeightChanged", "System.Collections.ArrayList _pStuffConsumed", "Void add_WeightStayedTheSame(System.EventHandler)", "Void remove_WeightStayedTheSame(System.EventHandler)", "System.EventHandler WeightStayedTheSame", "System.EventHandler WeightStayedTheSame", "System.EventHandler s_catDisappeared", "System.Collections.Generic.List`1[System.Object] _pStuffConsumed" });
        }

        [Fact]
        public void Test6()
        {
            BindingFlags allBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            TryGetMembers("System.Reflection.Compatibility.UnitTests.TypeTests.GenericArrayWrapperClass`1[System.String]", allBindingFlags, new string[] { "Int32 get_myProperty()", "Void set_myProperty(Int32)", "System.String get_Item(Int32)", "Void set_Item(Int32, System.String)", "Void .ctor(System.String[])", "Int32 myProperty", "System.String Item [Int32]", "System.String[] _field", "Int32 _field1" });
        }
    }
}
