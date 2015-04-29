// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Compatibility.UnitTests.TypeTests
{
    public class GetMethodsTests
    {
        public static void TryGetMethods(string AssemblyQualifiedNameOfTypeToGet, BindingFlags bindingAttr, string[] methodsExpected)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            MethodInfo[] methodsReturned = typeToCheck.GetMethods(bindingAttr);
            Assert.Equal(methodsExpected.Length, methodsReturned.Length);
            int foundIndex;
            Array.Sort(methodsExpected);
            for (int i = 0; i < methodsReturned.Length; i++)
            {
                foundIndex = Array.BinarySearch(methodsExpected, methodsReturned[i].ToString());
                Assert.False(foundIndex < 0, "An unexpected method " + methodsReturned[i].ToString() + " was returned");
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

        public static string ArrayToCommaList(MethodInfo[] ArrayToConvert)
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
            BindingFlags declaredPublicInstanceBindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
            TryGetMethods("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClass`1[System.String]", declaredPublicInstanceBindingFlags, new string[] { "System.String ReturnAndSetField(System.String)" });
        }

        [Fact]
        public void Test2()
        {
            BindingFlags declaredPublicInstanceBindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
            TryGetMethods("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassUsingNestedInterfaces`2[System.String,System.Int32]", declaredPublicInstanceBindingFlags, new string[] { "System.String ReturnAndSetFieldZero(System.String)", "Void SetFieldOne(Int32)", "Void SetFieldTwo(System.String)", "Int32 ReturnAndSetFieldThree(Int32)" });
        }

        [Fact]
        public void Test3()
        {
            BindingFlags declaredPublicInstanceBindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
            TryGetMethods("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithInterface`1", declaredPublicInstanceBindingFlags, new string[] { "W GenericMethod[W](W)", "T ReturnAndSetFieldZero(T)" });
        }

        [Fact]
        public void Test4()
        {
            BindingFlags declaredPublicInstanceBindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
            TryGetMethods("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithInterface`1[System.Int32]", declaredPublicInstanceBindingFlags, new string[] { "W GenericMethod[W](W)", "Int32 ReturnAndSetFieldZero(Int32)" });
        }

        [Fact]
        public void Test5()
        {
            BindingFlags declaredPublicInstanceBindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;
            TryGetMethods("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithVarArgMethod`1", declaredPublicInstanceBindingFlags, new string[] { "T get_publicField()", "Void set_publicField(T)", "T ReturnAndSetField(T, T[])" });
        }

        [Fact]
        public void Test6()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase, new string[] { });
        }

        [Fact]
        public void Test7()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly, new string[] { });
        }

        [Fact]
        public void Test8()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly, new string[] { });
        }

        [Fact]
        public void Test9()
        {
            TryGetMethods("System.Int32", BindingFlags.Instance, new string[] { });
        }

        [Fact]
        public void Test10()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Instance, new string[] { });
        }

        [Fact]
        public void Test11()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Instance, new string[] { });
        }

        [Fact]
        public void Test12()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance, new string[] { });
        }

        [Fact]
        public void Test13()
        {
            TryGetMethods("System.Int32", BindingFlags.Static, new string[] { });
        }

        [Fact]
        public void Test14()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Static, new string[] { });
        }

        [Fact]
        public void Test15()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Static, new string[] { });
        }

        [Fact]
        public void Test16()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static, new string[] { });
        }

        [Fact]
        public void Test17()
        {
            TryGetMethods("System.Int32", BindingFlags.Instance | BindingFlags.Static, new string[] { });
        }

        [Fact]
        public void Test18()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static, new string[] { });
        }

        [Fact]
        public void Test19()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static, new string[] { });
        }

        [Fact]
        public void Test20()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static, new string[] { });
        }

        [Fact]
        public void Test21()
        {
            TryGetMethods("System.Int32", BindingFlags.Public, new string[] { });
        }

        [Fact]
        public void Test22()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Public, new string[] { });
        }

        [Fact]
        public void Test23()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Public, new string[] { });
        }

        [Fact]
        public void Test24()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public, new string[] { });
        }

        [Fact]
        public void Test25()
        {
            TryGetMethods("System.Int32", BindingFlags.Instance | BindingFlags.Public, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "System.TypeCode GetTypeCode()", "System.Type GetType()", });
        }

        [Fact]
        public void Test26()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "System.TypeCode GetTypeCode()", "System.Type GetType()", });
        }

        [Fact]
        public void Test27()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "System.TypeCode GetTypeCode()", });
        }

        [Fact]
        public void Test28()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "System.TypeCode GetTypeCode()", });
        }

        [Fact]
        public void Test29()
        {
            TryGetMethods("System.Int32", BindingFlags.Static | BindingFlags.Public, new string[] { "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", });
        }

        [Fact]
        public void Test30()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public, new string[] { "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", });
        }

        [Fact]
        public void Test31()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public, new string[] { "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", });
        }

        [Fact]
        public void Test32()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public, new string[] { "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", });
        }

        [Fact]
        public void Test33()
        {
            TryGetMethods("System.Int32", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", "System.TypeCode GetTypeCode()", "System.Type GetType()", });
        }

        [Fact]
        public void Test34()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", "System.TypeCode GetTypeCode()", "System.Type GetType()", });
        }

        [Fact]
        public void Test35()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", "System.TypeCode GetTypeCode()", });
        }

        [Fact]
        public void Test36()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", "System.TypeCode GetTypeCode()", });
        }

        [Fact]
        public void Test37()
        {
            TryGetMethods("System.Int32", BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test38()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test39()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test40()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test41()
        {
            TryGetMethods("System.Int32", BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", "Void Finalize()", "System.Object MemberwiseClone()", });
        }

        [Fact]
        public void Test42()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", "Void Finalize()", "System.Object MemberwiseClone()", });
        }

        [Fact]
        public void Test43()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", });
        }

        [Fact]
        public void Test44()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", });
        }

        [Fact]
        public void Test45()
        {
            TryGetMethods("System.Int32", BindingFlags.Static | BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test46()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test47()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test48()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test49()
        {
            TryGetMethods("System.Int32", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic, new string[] { "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", "Void Finalize()", "System.Object MemberwiseClone()", });
        }

        [Fact]
        public void Test50()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic, new string[] { "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", "Void Finalize()", "System.Object MemberwiseClone()", });
        }

        [Fact]
        public void Test51()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic, new string[] { "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", });
        }

        [Fact]
        public void Test52()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic, new string[] { "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", });
        }

        [Fact]
        public void Test53()
        {
            TryGetMethods("System.Int32", BindingFlags.Public | BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test54()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test55()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test56()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic, new string[] { });
        }

        [Fact]
        public void Test57()
        {
            TryGetMethods("System.Int32", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "System.TypeCode GetTypeCode()", "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", "System.Type GetType()", "Void Finalize()", "System.Object MemberwiseClone()", });
        }

        [Fact]
        public void Test58()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "System.TypeCode GetTypeCode()", "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", "System.Type GetType()", "Void Finalize()", "System.Object MemberwiseClone()", });
        }

        [Fact]
        public void Test59()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "System.TypeCode GetTypeCode()", "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", });
        }

        [Fact]
        public void Test60()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "System.TypeCode GetTypeCode()", "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", });
        }

        [Fact]
        public void Test61()
        {
            TryGetMethods("System.Int32", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", });
        }

        [Fact]
        public void Test62()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", });
        }

        [Fact]
        public void Test63()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", });
        }

        [Fact]
        public void Test64()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", });
        }

        [Fact]
        public void Test65()
        {
            TryGetMethods("System.Int32", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", "System.TypeCode GetTypeCode()", "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", "System.Type GetType()", "Void Finalize()", "System.Object MemberwiseClone()", });
        }

        [Fact]
        public void Test66()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", "System.TypeCode GetTypeCode()", "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", "System.Type GetType()", "Void Finalize()", "System.Object MemberwiseClone()", });
        }

        [Fact]
        public void Test67()
        {
            TryGetMethods("System.Int32", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", "System.TypeCode GetTypeCode()", "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", });
        }

        [Fact]
        public void Test68()
        {
            TryGetMethods("System.Int32", BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Int32 CompareTo(System.Object)", "Int32 CompareTo(Int32)", "Boolean Equals(System.Object)", "Boolean Equals(Int32)", "Int32 GetHashCode()", "System.String ToString()", "System.String ToString(System.String)", "System.String ToString(System.IFormatProvider)", "System.String ToString(System.String, System.IFormatProvider)", "Int32 Parse(System.String)", "Int32 Parse(System.String, System.Globalization.NumberStyles)", "Int32 Parse(System.String, System.IFormatProvider)", "Int32 Parse(System.String, System.Globalization.NumberStyles, System.IFormatProvider)", "Boolean TryParse(System.String, Int32 ByRef)", "Boolean TryParse(System.String, System.Globalization.NumberStyles, System.IFormatProvider, Int32 ByRef)", "System.TypeCode GetTypeCode()", "Boolean System.IConvertible.ToBoolean(System.IFormatProvider)", "Char System.IConvertible.ToChar(System.IFormatProvider)", "SByte System.IConvertible.ToSByte(System.IFormatProvider)", "Byte System.IConvertible.ToByte(System.IFormatProvider)", "Int16 System.IConvertible.ToInt16(System.IFormatProvider)", "UInt16 System.IConvertible.ToUInt16(System.IFormatProvider)", "Int32 System.IConvertible.ToInt32(System.IFormatProvider)", "UInt32 System.IConvertible.ToUInt32(System.IFormatProvider)", "Int64 System.IConvertible.ToInt64(System.IFormatProvider)", "UInt64 System.IConvertible.ToUInt64(System.IFormatProvider)", "Single System.IConvertible.ToSingle(System.IFormatProvider)", "Double System.IConvertible.ToDouble(System.IFormatProvider)", "System.Decimal System.IConvertible.ToDecimal(System.IFormatProvider)", "System.DateTime System.IConvertible.ToDateTime(System.IFormatProvider)", "System.Object System.IConvertible.ToType(System.Type, System.IFormatProvider)", });
        }

        [Fact]
        public void Test69()
        {
            TryGetMethods("System.Int32", BindingFlags.FlattenHierarchy, new string[] { });
        }
    }
}

