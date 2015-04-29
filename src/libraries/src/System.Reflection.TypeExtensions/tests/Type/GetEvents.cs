// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;


namespace System.Reflection.Compatibility.UnitTests.TypeTests
{
    public class GetEventsTests
    {
        public static void TryGetEvents(string AssemblyQualifiedNameOfTypeToGet, string[] eventsExpected)
        {
            TryGetEvents(AssemblyQualifiedNameOfTypeToGet, eventsExpected, BindingFlags.IgnoreCase, false);
        }

        public static void TryGetEvents(string AssemblyQualifiedNameOfTypeToGet, string[] eventsExpected, BindingFlags flags)
        {
            TryGetEvents(AssemblyQualifiedNameOfTypeToGet, eventsExpected, flags, true);
        }

        public static void TryGetEvents(string AssemblyQualifiedNameOfTypeToGet, string[] eventsExpected, BindingFlags flags, bool useFlags)
        {
            Type typeToCheck;
            //Run tests
            typeToCheck = Type.GetType(AssemblyQualifiedNameOfTypeToGet);
            Assert.NotNull(typeToCheck);
            EventInfo[] eventsReturned;
            if (useFlags)
            {
                eventsReturned = typeToCheck.GetEvents(flags);
            }
            else
            {
                eventsReturned = typeToCheck.GetEvents();
            }
            Assert.Equal(eventsExpected.Length, eventsReturned.Length);
            int foundIndex;
            Array.Sort(eventsExpected);
            for (int i = 0; i < eventsReturned.Length; i++)
            {
                foundIndex = Array.BinarySearch(eventsExpected, eventsReturned[i].ToString());
                Assert.False(foundIndex < 0, "An unexpected event " + eventsReturned[i].ToString() + " was returned");
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

        public static string ArrayToCommaList(EventInfo[] ArrayToConvert)
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
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged" });
        }

        [Fact]
        public void Test2()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase);
        }

        [Fact]
        public void Test3()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.DeclaredOnly);
        }

        [Fact]
        public void Test4()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly);
        }

        [Fact]
        public void Test5()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.Instance);
        }

        [Fact]
        public void Test6()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Instance);
        }

        [Fact]
        public void Test7()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Instance);
        }

        [Fact]
        public void Test8()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance);
        }

        [Fact]
        public void Test9()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.Static);
        }

        [Fact]
        public void Test10()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Static);
        }

        [Fact]
        public void Test11()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Static);
        }

        [Fact]
        public void Test12()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static);
        }

        [Fact]
        public void Test13()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.Instance | BindingFlags.Static);
        }

        [Fact]
        public void Test14()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static);
        }

        [Fact]
        public void Test15()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
        }

        [Fact]
        public void Test16()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static);
        }

        [Fact]
        public void Test17()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.Public);
        }

        [Fact]
        public void Test18()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Public);
        }

        [Fact]
        public void Test19()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Public);
        }

        [Fact]
        public void Test20()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public);
        }

        [Fact]
        public void Test21()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", }, BindingFlags.Instance | BindingFlags.Public);
        }

        [Fact]
        public void Test22()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        }

        [Fact]
        public void Test23()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        }

        [Fact]
        public void Test24()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        }

        [Fact]
        public void Test25()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test26()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test27()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test28()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test29()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", }, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test30()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test31()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test32()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
        }

        [Fact]
        public void Test33()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.NonPublic);
        }

        [Fact]
        public void Test34()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test35()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test36()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test37()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightStayedTheSame", }, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test38()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightStayedTheSame", }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test39()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightStayedTheSame", }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test40()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightStayedTheSame", }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test41()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test42()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test43()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test44()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test45()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightStayedTheSame", }, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test46()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightStayedTheSame", }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test47()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightStayedTheSame", }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test48()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightStayedTheSame", }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test49()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test50()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test51()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test52()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test53()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", "System.EventHandler WeightStayedTheSame", }, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test54()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", "System.EventHandler WeightStayedTheSame", }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test55()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", "System.EventHandler WeightStayedTheSame", }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test56()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", "System.EventHandler WeightStayedTheSame", }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test57()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test58()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test59()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test60()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test61()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", "System.EventHandler WeightStayedTheSame", }, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test62()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", "System.EventHandler WeightStayedTheSame", }, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test63()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", "System.EventHandler WeightStayedTheSame", }, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test64()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { "System.EventHandler WeightChanged", "System.EventHandler WeightStayedTheSame", }, BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [Fact]
        public void Test65()
        {
            TryGetEvents("System.Reflection.Compatibility.UnitTests.TypeTests.Cat`1[System.Int32]", new string[] { }, BindingFlags.FlattenHierarchy);
        }
    }
}