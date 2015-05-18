// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class MethodInfoReturnTypeTests
    {
        //Invoke a method  that returns IntPtr
        [Fact]
        public static void TestInvokeMethod1()
        {
            MethodInfo mi = null;
            C clsObj = new C();

            mi = GetMethod(typeof(C), "M_IntPtr");
            IntPtr intptr = (IntPtr)mi.Invoke(clsObj, (Object[])null);

            Assert.True(intptr.ToInt32().Equals(200));
        }


        //Invoke a method  that returns int array
        [Fact]
        public static void TestInvokeMethod2()
        {
            MethodInfo mi = null;
            C clsObj = new C();

            mi = GetMethod(typeof(C), "M_Array1");
            int[] array1 = (int[])mi.Invoke(clsObj, (Object[])null);

            Assert.True(array1[0].Equals(2));

            Assert.True(array1[1].Equals(3));
        }

        //Gets MethodInfo object from a Type
        public static MethodInfo GetMethod(Type t, string method)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<MethodInfo> alldefinedMethods = ti.DeclaredMethods.GetEnumerator();
            MethodInfo mi = null;

            while (alldefinedMethods.MoveNext())
            {
                if (alldefinedMethods.Current.Name.Equals(method))
                {
                    //found method
                    mi = alldefinedMethods.Current;
                    break;
                }
            }
            return mi;
        }
    }


    public class C
    {
        public IntPtr M_IntPtr()
        {
            return new IntPtr(200);
        }

        public int[] M_Array1()
        {
            int[] primes = new int[] { 2, 3, 5, 7, 11 };
            return primes;
        }
    }
}
