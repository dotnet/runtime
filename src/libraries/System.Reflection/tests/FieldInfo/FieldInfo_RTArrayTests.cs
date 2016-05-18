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
        [Fact]
        public static void SetValue_aArray()
        {
            FieldInfo fi = GetField(type, "aArray");

            fi.SetValue(obj, ATypeWithMixedAB);
            Assert.Equal(ATypeWithMixedAB, fi.GetValue(obj));

            fi.SetValue(obj, ATypeWithAllA);
            Assert.Equal(ATypeWithAllA, fi.GetValue(obj));

            fi.SetValue(obj, ATypeWithAllB);
            Assert.Equal(ATypeWithAllB, fi.GetValue(obj));

            fi.SetValue(obj, BTypeWithAllB);
            Assert.Equal(BTypeWithAllB, fi.GetValue(obj));

            fi.SetValue(obj, BTypeWithAllB_Contra);
            Assert.Equal(BTypeWithAllB_Contra, fi.GetValue(obj));
        }

        [Fact]
        public static void SetValue_bArray()
        {
            FieldInfo fi = GetField(type, "bArray");

            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, ATypeWithMixedAB));
            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, ATypeWithAllA));
            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, ATypeWithAllB));

            fi.SetValue(obj, BTypeWithAllB);
            Assert.Equal(BTypeWithAllB, fi.GetValue(obj));

            fi.SetValue(obj, BTypeWithAllB_Contra);
            Assert.Equal(BTypeWithAllB_Contra, fi.GetValue(obj));
        }

        [Fact]
        public static void SetValue_iArray()
        {
            FieldInfo fi = GetField(type, "iArray");

            fi.SetValue(obj, mixedMN);
            Assert.Equal(mixedMN, fi.GetValue(obj));
        }

        [Fact]
        public static void SetValue_intArray()
        {
            FieldInfo fi = GetField(type, "intArray");

            fi.SetValue(obj, mixedInt);
            Assert.Equal(mixedInt, fi.GetValue(obj));

            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, allByte));
        }

        [Fact]
        public static void SetValue_objectArray()
        {
            FieldInfo fi = GetField(type, "objectArray");

            fi.SetValue(obj, mixedMN);
            Assert.Equal(mixedMN, fi.GetValue(obj));

            fi.SetValue(obj, BTypeWithAllB_Contra);

            Assert.Equal(BTypeWithAllB_Contra, fi.GetValue(obj));
            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, mixedInt));
            Assert.Throws<ArgumentException>(() => fi.SetValue(obj, allByte));
        }

        // Helper method to get field from Type type
        private static FieldInfo GetField(Type type, string fieldName)
        {
            TypeInfo ti = type.GetTypeInfo();
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
