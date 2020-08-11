// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

namespace DebuggerTests
{
    public class PointerTests
    {

        public static unsafe void LocalPointers()
        {
            int ivalue0 = 5;
            int ivalue1 = 10;

            int* ip = &ivalue0;
            int* ip_null = null;
            int** ipp = &ip;
            int** ipp_null = &ip_null;
            int*[] ipa = new int*[] { &ivalue0, &ivalue1, null };
            int**[] ippa = new int**[] { &ip, &ip_null, ipp, ipp_null, null };
            char cvalue0 = 'q';
            char* cp = &cvalue0;

            DateTime dt = new DateTime(5, 6, 7, 8, 9, 10);
            void* vp = &dt;
            void* vp_null = null;
            void** vpp = &vp;
            void** vpp_null = &vp_null;

            DateTime* dtp = &dt;
            DateTime* dtp_null = null;
            DateTime*[] dtpa = new DateTime*[] { dtp, dtp_null };
            DateTime**[] dtppa = new DateTime**[] { &dtp, &dtp_null, null };
            Console.WriteLine($"-- break here: ip_null==null: {ip_null == null}, ipp_null: {ipp_null == null}, *ipp_null==ip_null: {*ipp_null == ip_null}, *ipp_null==null: {*ipp_null == null}");

            var gs = new GenericStructWithUnmanagedT<DateTime> { Value = new DateTime(1, 2, 3, 4, 5, 6), IntField = 4, DTPP = &dtp };
            var gs_null = new GenericStructWithUnmanagedT<DateTime> { Value = new DateTime(1, 2, 3, 4, 5, 6), IntField = 4, DTPP = &dtp_null };
            var gsp = &gs;
            var gsp_null = &gs_null;
            var gspa = new GenericStructWithUnmanagedT<DateTime>*[] { null, gsp, gsp_null };

            var cwp = new GenericClassWithPointers<DateTime> { Ptr = dtp };
            var cwp_null = new GenericClassWithPointers<DateTime>();
            Console.WriteLine($"{(int)*ip}, {(int)**ipp}, {ipp_null == null}, {ip_null == null}, {ippa == null}, {ipa}, {(char)*cp}, {(vp == null ? "null" : "not null")}, {dtp->Second}, {gsp->IntField}, {cwp}, {cwp_null}, {gs_null}");

            PointersAsArgsTest(ip, ipp, ipa, ippa, &dt, &dtp, dtpa, dtppa);
        }

        static unsafe void  PointersAsArgsTest(int* ip, int** ipp, int*[] ipa, int**[] ippa,
                            DateTime* dtp, DateTime** dtpp, DateTime*[] dtpa, DateTime**[] dtppa)
        {
            Console.WriteLine($"break here!");
            if (ip == null)
                Console.WriteLine($"ip is null");
            Console.WriteLine($"done!");
        }

        public static unsafe async Task LocalPointersAsync()
        {
            int ivalue0 = 5;
            int ivalue1 = 10;

            int* ip = &ivalue0;
            int* ip_null = null;
            int** ipp = &ip;
            int** ipp_null = &ip_null;
            int*[] ipa = new int*[] { &ivalue0, &ivalue1, null };
            int**[] ippa = new int**[] { &ip, &ip_null, ipp, ipp_null, null };
            char cvalue0 = 'q';
            char* cp = &cvalue0;

            DateTime dt = new DateTime(5, 6, 7, 8, 9, 10);
            void* vp = &dt;
            void* vp_null = null;
            void** vpp = &vp;
            void** vpp_null = &vp_null;

            DateTime* dtp = &dt;
            DateTime* dtp_null = null;
            DateTime*[] dtpa = new DateTime*[] { dtp, dtp_null };
            DateTime**[] dtppa = new DateTime**[] { &dtp, &dtp_null, null };
            Console.WriteLine($"-- break here: ip_null==null: {ip_null == null}, ipp_null: {ipp_null == null}, *ipp_null==ip_null: {*ipp_null == ip_null}, *ipp_null==null: {*ipp_null == null}");

            var gs = new GenericStructWithUnmanagedT<DateTime> { Value = new DateTime(1, 2, 3, 4, 5, 6), IntField = 4, DTPP = &dtp };
            var gs_null = new GenericStructWithUnmanagedT<DateTime> { Value = new DateTime(1, 2, 3, 4, 5, 6), IntField = 4, DTPP = &dtp_null };
            var gsp = &gs;
            var gsp_null = &gs_null;
            var gspa = new GenericStructWithUnmanagedT<DateTime>*[] { null, gsp, gsp_null };

            var cwp = new GenericClassWithPointers<DateTime> { Ptr = dtp };
            var cwp_null = new GenericClassWithPointers<DateTime>();
            Console.WriteLine($"{(int)*ip}, {(int)**ipp}, {ipp_null == null}, {ip_null == null}, {ippa == null}, {ipa}, {(char)*cp}, {(vp == null ? "null" : "not null")}, {dtp->Second}, {gsp->IntField}, {cwp}, {cwp_null}, {gs_null}");
        }

        // async methods cannot have unsafe params, so no test for that
    }

    public unsafe struct GenericStructWithUnmanagedT<T> where T : unmanaged
    {
        public T Value;
        public int IntField;

        public DateTime** DTPP;
    }

    public unsafe class GenericClassWithPointers<T> where T : unmanaged
    {
        public unsafe T* Ptr;
    }
}