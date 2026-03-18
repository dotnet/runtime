// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime;

namespace Internal.Runtime.CompilerHelpers
{
    internal static unsafe class ReachabilityInstrumentationSupport
    {
        private static byte* s_region;
        private static int s_size;

        private const string ReachabilityFileName = "reach.mprof";

        public static void Register(byte* region, int size)
        {
            s_region = region;
            s_size = size;

            // If profiling information already exists, load it into memory. This is to support scenarios
            // where one needs to re-run the app to exercise all the code paths.
            if (File.Exists(ReachabilityFileName)
                && File.ReadAllBytes(ReachabilityFileName) is byte[] bytes
                && bytes.Length == size)
            {
                // Check that the profile matches what we're currently running. Instead of parsing the file format,
                // just check differing bytes. The bytes either need to be the same, or one of them is 1 and another
                // is 0 (We don't check which one since the File.ReadAllBytes call above might not be reachable
                // in a first run of the app so both 0->1 and 1->0 are okay for us.)
                for (int i = 0; i < size; i++)
                {
                    if (region[i] == bytes[i])
                    {
                        continue;
                    }
                    else if ((region[i] | bytes[i]) != 1)
                    {
                        return;
                    }
                }

                // Combine profile data.
                for (int i = 0; i < size; i++)
                {
                    region[i] |= bytes[i];
                }
            }
        }

        [FeatureSwitchDefinition("Internal.Runtime.CompilerHelpers.ReachabilityInstrumentationSupport")]
        public static bool IsSupported => false;

        public static void Shutdown()
        {
            if (s_size > 0)
            {
                File.WriteAllBytes(ReachabilityFileName, new ReadOnlySpan<byte>(s_region, s_size));
            }
        }
    }
}
