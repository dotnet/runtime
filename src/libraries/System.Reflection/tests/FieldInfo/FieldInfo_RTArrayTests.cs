// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class FieldInfoRTArrayTests
    {
        //Verify FieldInfo for aArray Field
        [Fact]
        public static void TestSetValue_Field1()
        {
            FieldInfo fi = GetField(type, "aArray");

            fi.SetValue(obj, ATypeWithMixedAB);
            Assert.True((fi.GetValue(obj)).Equals(ATypeWithMixedAB), "Failed!! Could not set ArrayField aArray using FieldInfo");

            fi.SetValue(obj, ATypeWithAllA);
            Assert.True((fi.GetValue(obj)).Equals(ATypeWithAllA), "Failed!! Could not set ArrayField aArray using FieldInfo");

            fi.SetValue(obj, ATypeWithAllB);
            Assert.True((fi.GetValue(obj)).Equals(ATypeWithAllB), "Failed!! Could not set ArrayField aArray using FieldInfo");

            fi.SetValue(obj, BTypeWithAllB);
            Assert.True((fi.GetValue(obj)).Equals(BTypeWithAllB), "Failed!! Could not set ArrayField aArray using FieldInfo");

            fi.SetValue(obj, BTypeWithAllB_Contra);
            Assert.True((fi.GetValue(obj)).Equals(BTypeWithAllB_Contra), "Failed!! Could not set ArrayField aArray using FieldInfo");
        }

        //Verify FieldInfo for bArray Field
        [Fact]
        public static void TestSetValue_Field2()
        {
            FieldInfo fi = GetField(type, "bArray");

            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, ATypeWithMixedAB));
            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, ATypeWithAllA));
            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, ATypeWithAllB));

            fi.SetValue(obj, BTypeWithAllB);
            Assert.True((fi.GetValue(obj)).Equals(BTypeWithAllB), "Failed!! Could not set ArrayField bArray using FieldInfo");

            fi.SetValue(obj, BTypeWithAllB_Contra);
            Assert.True((fi.GetValue(obj)).Equals(BTypeWithAllB_Contra), "Failed!! Could not set ArrayField bArray using FieldInfo");
        }

        //Verify FieldInfo for iArray Field
        [Fact]
        public static void TestSetValue_Field3()
        {
            FieldInfo fi = GetField(type, "iArray");

            fi.SetValue(obj, mixedMN);
            Assert.True((fi.GetValue(obj)).Equals(mixedMN), "Failed!! Could not set ArrayField iArray using FieldInfo");
        }

        //Verify FieldInfo for intArray Field
        [Fact]
        public static void TestSetValue_Field4()
        {
            FieldInfo fi = GetField(type, "intArray");

            fi.SetValue(obj, mixedInt);
            Assert.True((fi.GetValue(obj)).Equals(mixedInt), "Failed!! Could not set ArrayField intArray using FieldInfo");

            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, allByte));
        }

        //Verify FieldInfo for Array Field
        [Fact]
        public static void TestSetValue_Field5()
        {
            FieldInfo fi = GetField(type, "objectArray");

            fi.SetValue(obj, mixedMN);
            Assert.True((fi.GetValue(obj)).Equals(mixedMN), "Failed!! Could not set ArrayField objectArray using FieldInfo");

            fi.SetValue(obj, BTypeWithAllB_Contra);

            Assert.True((fi.GetValue(obj)).Equals(BTypeWithAllB_Contra), "Failed!! Could not set ArrayField objectArray using FieldInfo");
            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, mixedInt));
            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, allByte));
        }

        // Helper method to get field from Type type
        private static FieldInfo GetField(Type type, string field)
        {
            TypeInfo ti = type.GetTypeInfo();
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

        // Reflection fields

        private static Type type = typeof(ArrayAsField);
        private static object obj = Activator.CreateInstance(typeof(ArrayAsField));

        private static A[] ATypeWithMixedAB = new A[] { new A(), new FieldInfoArrayB() };
        private static A[] ATypeWithAllA = new A[] { new A(), new A() };
        private static A[] ATypeWithAllB = new A[] { new FieldInfoArrayB(), new FieldInfoArrayB() };
        private static FieldInfoArrayB[] BTypeWithAllB = new FieldInfoArrayB[] { new FieldInfoArrayB(), new FieldInfoArrayB() };
        private static A[] BTypeWithAllB_Contra = new FieldInfoArrayB[] { new FieldInfoArrayB(), new FieldInfoArrayB() };

        private static I[] mixedMN = new I[] { new M(), new N() };
        private static int[] mixedInt = new int[] { 200, -200, (byte)30, (ushort)2 };
        private static byte[] allByte = new byte[] { 2, 3, 4 };
    }

    public class A { }
    public class FieldInfoArrayB : A { }

    public interface I { }
    public class M : I { }
    public class N : I { }

    public class ArrayAsField
    {
        public A[] aArray;
        public FieldInfoArrayB[] bArray;

        public I[] iArray;

        public int[] intArray;
        public object[] objectArray;
    }
}
