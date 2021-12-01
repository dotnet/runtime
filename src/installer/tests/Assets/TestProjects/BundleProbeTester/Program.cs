// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace BundleProbeTester
{
    public static class Program
    {
        // The return type on BundleProbeDelegate is byte instead of bool because
        // using non-blitable bool type caused a failure (incorrect value) on linux-musl-x64.
        // The bundle-probe callback is only called from native code in the product
        // Therefore the type on this test is adjusted to circumvent the failure.
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate byte BundleProbeDelegate([MarshalAs(UnmanagedType.LPUTF8Str)] string path, IntPtr offset, IntPtr size, IntPtr compressedSize);

        unsafe static bool Probe(BundleProbeDelegate bundleProbe, string path, bool isExpected)
        {
            Int64 size, offset, compressedSize;
            bool exists = bundleProbe(path, (IntPtr)(&offset), (IntPtr)(&size), (IntPtr)(&compressedSize)) != 0;

            switch (exists, isExpected)
            {
                case (true, true):
                    if (compressedSize < 0 || compressedSize > size)
                    {
                        Console.WriteLine($"Invalid compressedSize obtained for {path} within bundle.");
                        return false;
                    }

                    if (size > 0 && offset > 0)
                    {
                        return true;
                    }

                    Console.WriteLine($"Invalid location obtained for {path} within bundle.");
                    return false;

                case (true, false):
                    Console.WriteLine($"Unexpected file {path} found in bundle.");
                    return false;

                case (false, true):
                    Console.WriteLine($"Expected file {path} not found in bundle.");
                    return false;

                case (false, false):
                    return true;
            }
        }

        public static int Main(string[] args)
        {
            bool isSingleFile = args.Length > 0 && args[0].Equals("SingleFile");
            object probeObject = System.AppDomain.CurrentDomain.GetData("BUNDLE_PROBE");

            if (!isSingleFile)
            {
                if (probeObject != null)
                {
                    Console.WriteLine("BUNDLE_PROBE property passed in for a non-single-file app");
                    return -1;
                }

                Console.WriteLine("No BUNDLE_PROBE");
                return 0;
            }

            if (probeObject == null)
            {
                Console.WriteLine("BUNDLE_PROBE property not passed in for a single-file app");
                return -2;
            }

            string probeString = probeObject as string;
            IntPtr probePtr = (IntPtr)Convert.ToUInt64(probeString, 16);
            BundleProbeDelegate bundleProbeDelegate = Marshal.GetDelegateForFunctionPointer<BundleProbeDelegate>(probePtr);
            bool success =
                Probe(bundleProbeDelegate, "BundleProbeTester.dll", isExpected: true) &&
                Probe(bundleProbeDelegate, "BundleProbeTester.runtimeconfig.json", isExpected: true) &&
                Probe(bundleProbeDelegate, "System.Private.CoreLib.dll", isExpected: true) &&
                Probe(bundleProbeDelegate, "hostpolicy.dll", isExpected: false) &&
                Probe(bundleProbeDelegate, "--", isExpected: false) &&
                Probe(bundleProbeDelegate, "", isExpected: false);

            if (!success)
            {
                return -3;
            }

            Console.WriteLine("BUNDLE_PROBE OK");
            return 0;
        }
    }
}
