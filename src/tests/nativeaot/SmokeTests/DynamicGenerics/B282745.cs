// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using CoreFXTestLibrary;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

#if INTERNAL_CONTRACTS
using Internal.Runtime.Augments;
#endif
using TypeOfRepo;

public static class B282745
{
    class TempObject
    {
        public int field;
    }
    static object s_o = null;

    public static IntPtr GetIntPtrOnHeap()
    {
        TempObject to = new TempObject();
        s_o = to;
        unsafe
        {
            fixed(int* pFld = &to.field)
            {
                return new IntPtr(pFld);
            }
        }
    }

    private static long GetIntPtrOnHeapAsLong()
    {
        return (long)GetIntPtrOnHeap();
    }

    private static int GetIntPtrOnHeapAsInt()
    {
        return unchecked((int)GetIntPtrOnHeapAsLong());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [TestMethod]
    public static void testIntMDArrayWithPointerLikeValues()
    {
        int[] lengths = { 1, 2 };
        int[,] array = (int[,])Array.CreateInstance(typeof(int), lengths);
        array[0,1] = GetIntPtrOnHeapAsInt();

        GC.Collect();
            
        GC.KeepAlive(array);

        RuntimeTypeHandle arrayTypeHandle = array.GetType().TypeHandle;
#if INTERNAL_CONTRACTS
        Assert.IsTrue(RuntimeAugments.IsDynamicType(arrayTypeHandle));
#endif
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [TestMethod]
    public static void testLongMDArrayWithPointerLikeValues()
    {
        int[] lengths = { 1, 2 };
        long[,] array = (long[,])Array.CreateInstance(typeof(long), lengths);
        array[0,1] = GetIntPtrOnHeapAsLong();
        GC.Collect();
            
        GC.KeepAlive(array);

        RuntimeTypeHandle arrayTypeHandle = array.GetType().TypeHandle;
#if INTERNAL_CONTRACTS
        Assert.IsTrue(RuntimeAugments.IsDynamicType(arrayTypeHandle));
#endif
    }

    struct SomeGenStruct<T>
    {
        public T o;
        public int i;
        public long l;
    }
    
    public class GenericType<T>
    {
        public static void test()
        {
            int[] lengths = {1,2,3};
            SomeGenStruct<T>[,,] array = (SomeGenStruct<T>[,,])Array.CreateInstance(typeof(SomeGenStruct<T>), lengths);

            array[0,0,0].o = default(T);
            array[0,0,0].i = GetIntPtrOnHeapAsInt();
            array[0,0,0].l = GetIntPtrOnHeapAsInt();

            array[0,1,2].o = default(T);
            array[0,1,2].i = GetIntPtrOnHeapAsInt();
            array[0,1,2].l = GetIntPtrOnHeapAsLong();

            array[0,1,1].o = default(T);
            array[0,1,1].i = GetIntPtrOnHeapAsInt();
            array[0,1,1].l = GetIntPtrOnHeapAsLong();

            GC.Collect();
            
            GC.KeepAlive(array);

        RuntimeTypeHandle arrayTypeHandle = array.GetType().TypeHandle;
#if INTERNAL_CONTRACTS
            Assert.IsTrue(RuntimeAugments.IsDynamicType(arrayTypeHandle));
#endif
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [TestMethod]
    public static void testMDArrayWithPointerLikeValuesOfKnownStructType()
    {
        GenericType<object>.test();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [TestMethod]
    public static void testMDArrayWithPointerLikeValuesOfUnknownStructReferenceType()
    {
        Type genType = typeof(GenericType<>).MakeGenericType(TypeOf.String);
        MethodInfo m = genType.GetTypeInfo().GetDeclaredMethod("test");
        m.Invoke(null, new object[] {});
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [TestMethod]
    public static void testMDArrayWithPointerLikeValuesOfUnknownStructPrimitiveType()
    {
#if UNIVERSAL_GENERICS
        Type genType = typeof(GenericType<>).MakeGenericType(TypeOf.Short);
        MethodInfo m = genType.GetTypeInfo().GetDeclaredMethod("test");
        m.Invoke(null, new object[] {});
#endif
    }
    
    public class MDArrayTestType
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [TestMethod]
    public static void testMDArrayWith3Dimensions()
    {
        // Test to ensure that we are correctly reporting all of the GC'able pointers in a dynamically
        // generated object mdarray of rank3
        // The idea is to fill a regular object array and an mdarray with X different objects
        // Then, GC, to move the objects around.
        // Then compare to ensure that the object arrays point to the same exact objects

        const int index1Max = 100;
        const int index2Max = 100;
        const int index3Max = 200;

        object[] objArray = new object[index1Max*index2Max*index3Max];
        MDArrayTestType[,,] mdObjArray = (MDArrayTestType[,,])Array.CreateInstance(TypeOf.B282475_MDArrayTestType, new int[]{index1Max,index2Max,index3Max});
        for (int i = 0; i < index1Max; i++)
        {
            for (int j = 0; j < index2Max; j++)
            {
                for (int k = 0; k < index3Max; k++)
                {
                    int index = i*(index3Max*index2Max) + j *index3Max + k;
                    MDArrayTestType o = new MDArrayTestType();
                    objArray[index] = o;
                    mdObjArray[i,j,k] = o;
                }
            }
        }

        foreach (object objInArray in objArray)
            Assert.IsNotNull(objInArray);

        GC.Collect(2);
        GC.Collect(2);
        GC.Collect(2);

        for (int i = 0; i < index1Max; i++)
        {
            for (int j = 0; j < index2Max; j++)
            {
                for (int k = 0; k < index3Max; k++)
                {
                    int index = i*(index3Max*index2Max) + j *index3Max + k;
                    Assert.IsTrue(Object.ReferenceEquals(objArray[index], mdObjArray[i,j,k]));
                }
            }
        }
    }
}

