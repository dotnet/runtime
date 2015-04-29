// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Compatibility.UnitTests.TypeTests
{
    public class GetPropertiesTests
    {
        public static void TryGetProperties(string AssemblyQualifiedNameOfTypeToGet, string[] propertiesExpected)
        {
            TryGetProperties(AssemblyQualifiedNameOfTypeToGet, propertiesExpected, BindingFlags.Instance, false);
        }

        public static void TryGetProperties(string AssemblyQualifiedNameOfTypeToGet, string[] propertiesExpected, BindingFlags flags)
        {
            TryGetProperties(AssemblyQualifiedNameOfTypeToGet, propertiesExpected, flags, true);
        }

        public static void TryGetProperties(string AssemblyQualifiedNameOfTypeToGet, string[] propertiesExpected, BindingFlags flags, bool useFlags)
        {
            Type typeToCheck;
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            PropertyInfo[] propertiesReturned;
            if (useFlags)
            {
                propertiesReturned = typeToCheck.GetProperties(flags);
            }
            else
            {
                propertiesReturned = typeToCheck.GetProperties();
            }
            Assert.Equal(propertiesExpected.Length, propertiesReturned.Length);
            int foundIndex;
            Array.Sort(propertiesExpected);
            for (int i = 0; i < propertiesReturned.Length; i++)
            {
                foundIndex = Array.BinarySearch(propertiesExpected, propertiesReturned[i].ToString());
                Assert.False(foundIndex < 0, "An unexpected property " + propertiesReturned[i].ToString() + " was returned");                
            }
        }

        [Fact]
        public void Test1()
        {
            TryGetProperties("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithVarArgMethod`1[System.String]", new string[] { "System.String publicField" });
        }

        [Fact]
        public void Test2()
        {
            TryGetProperties("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.Object[] StuffConsumed" });
        }

        [Fact]
        public void Test3()
        {
            TryGetProperties("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithVarArgMethod`1[System.Int32]", new string[] { "Int32 publicField" });
        }

        [Fact]
        public void Test4()
        {
            TryGetProperties("System.Reflection.Compatibility.UnitTests.TypeTests.GenericClassWithVarArgMethod`1", new string[] { "T publicField" });
        }

        [Fact]
        public void Test5()
        {
            TryGetProperties("System.Reflection.Compatibility.UnitTests.TypeTests.ClassWithVarArgMethod", new string[] { "Int32 publicField" });
        }

        [Fact]
        public void Test6()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase);
        }

        [Fact]
        public void Test7()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.DeclaredOnly);
        }

        [Fact]
        public void Test8()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly);
        }

        [Fact]
        public void Test9()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.Instance);
        }

        [Fact]
        public void Test10()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Instance);
        }

        [Fact]
        public void Test11()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Instance);
        }

        [Fact]
        public void Test12()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance);
        }

        [Fact]
        public void Test13()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.Static);
        }

        [Fact]
        public void Test14()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Static);
        }


        [Fact]
        public void Test15()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Static);
        }


        [Fact]
        public void Test16()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static);
        }


        [Fact]
        public void Test17()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.Instance | BindingFlags.Static);
        }


        [Fact]
        public void Test18()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static);
        }

        [Fact]
        public void Test19()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
        }

        [Fact]
        public void Test20()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
        }

        [Fact]
        public void Test21()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.Public);
        }

        [Fact]
        public void Test22()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Public);
        }

        [Fact]
        public void Test23()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Public);
        }

        [Fact]
        public void Test24()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public);
        }

        [Fact]
        public void Test25()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Int32 Length", }, BindingFlags.Instance | BindingFlags.Public);
        }

        [Fact]
        public void Test26()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Int32 Length", }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        }

        [Fact]
        public void Test27()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Int32 Length", }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        }

        [Fact]
        public void Test28()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Int32 Length", }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        }

        [Fact]
        public void Test29()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test30()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test31()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test32()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test33()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Int32 Length", }, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test34()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Int32 Length", }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test35()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Int32 Length", }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test36()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Int32 Length", }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test37()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.NonPublic);
        }

        [Fact]
        public void Test38()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test39()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test40()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test41()
        {
            TryGetProperties("System.String", new string[] { "Char FirstChar", }, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test42()
        {
            TryGetProperties("System.String", new string[] { "Char FirstChar", }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test43()
        {
            TryGetProperties("System.String", new string[] { "Char FirstChar", }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test44()
        {
            TryGetProperties("System.String", new string[] { "Char FirstChar", }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
        }
        
        [Fact]
        public void Test45()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test46()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test47()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test48()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test49()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Char FirstChar", "Int32 Length", }, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test50()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Char FirstChar", "Int32 Length", }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test51()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Char FirstChar", "Int32 Length", }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test52()
        {
            TryGetProperties("System.String", new string[] { "Char Chars [Int32]", "Char FirstChar", "Int32 Length", }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        
        [Fact]
        public void Test53()
        {
            TryGetProperties("System.String", new string[] { }, BindingFlags.FlattenHierarchy);
        }
    }
}