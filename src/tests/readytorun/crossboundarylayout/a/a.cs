// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace CrossBoundaryLayout
{
    public class A
    {
        public byte _aVal;
    }

    public class AGeneric<T>
    {
        public T _aVal;
    }

    public class ABoringGeneric<T>
    {
        public byte _aVal;
    }

    public class ATest
    {
        public static volatile object s_testFailObj;
        public static void ReportTestFailure(string test, object o, ref int failCount)
        {
            Console.WriteLine(test);
            s_testFailObj = o;
            failCount++;
        }

        public static int Test()
        {
            int failure = 0;
            {
                var a = (A)Activator.CreateInstance(typeof(A));
                a._aVal = 1;
                if (1 != (byte)typeof(A).GetField("_aVal").GetValue(a))
                {
                    ATest.ReportTestFailure("A a._aVal", a, ref failure);
                }
            }

            {
                var a2 = (AGeneric<byte>)Activator.CreateInstance(typeof(AGeneric<byte>));
                a2._aVal = 1;
                if (1 != (byte)typeof(AGeneric<byte>).GetField("_aVal").GetValue(a2))
                {
                    failure++;
                    ATest.ReportTestFailure("A a2_aVal", a2, ref failure);
                }
            }

            {
                var a3 = (AGeneric<ByteStruct>)Activator.CreateInstance(typeof(AGeneric<ByteStruct>));
                a3._aVal._dVal = 1;
                if (1 != ((ByteStruct)typeof(AGeneric<ByteStruct>).GetField("_aVal").GetValue(a3))._dVal)
                {
                    ATest.ReportTestFailure("A a3_aVal", a3, ref failure);
                }
            }

            {
                var a4 = (ABoringGeneric<byte>)Activator.CreateInstance(typeof(ABoringGeneric<byte>));
                a4._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<byte>).GetField("_aVal").GetValue(a4))
                {
                    ATest.ReportTestFailure("A a4_aVal", a4, ref failure);
                }
            }

            {
                var a5 = (ABoringGeneric<ByteStruct>)Activator.CreateInstance(typeof(ABoringGeneric<ByteStruct>));
                a5._aVal = 1;
                if (1 != (byte)typeof(ABoringGeneric<ByteStruct>).GetField("_aVal").GetValue(a5))
                {
                    ATest.ReportTestFailure("A a5_aVal", a5, ref failure);
                }
            }

            return failure;
        }
    }
}
