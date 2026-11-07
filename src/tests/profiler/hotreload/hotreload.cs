// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using HotReloadEdited;

namespace Profiler.Tests
{
    class HotReloadGetILFunctionBody
    {
        static readonly Guid HotReloadProfilerGuid = new Guid("C4A3B8E2-6F1D-4B7A-9E05-3D2F8A61B7C9");

        // The profiler reads Edited.Value's IL again while this method is compiled after the update.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AfterUpdate()
        {
        }

        public static int RunTest(string[] args)
        {
            if (!MetadataUpdater.IsSupported)
            {
                Console.WriteLine("MetadataUpdater is not supported");
                return 101;
            }

            if (Edited.Value() != 1)
            {
                Console.WriteLine("Edited.Value returned an unexpected value before the update");
                return 102;
            }

            Assembly assembly = typeof(Edited).Assembly;
            string basename = assembly.Location;
            MetadataUpdater.ApplyUpdate(assembly,
                                        File.ReadAllBytes(basename + ".1.dmeta"),
                                        File.ReadAllBytes(basename + ".1.dil"),
                                        File.ReadAllBytes(basename + ".1.dpdb"));

            if (Edited.Value() != 2)
            {
                Console.WriteLine("Edited.Value returned an unexpected value after the update");
                return 103;
            }

            AfterUpdate();
            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            foreach (string testName in new[] { "HotReloadGetILFunctionBody", "HotReloadGetILFunctionBodyAfterSetBody" })
            {
                int result = ProfilerTestRunner.Run(profileePath: Assembly.GetExecutingAssembly().Location,
                                                    testName: testName,
                                                    profilerClsid: HotReloadProfilerGuid,
                                                    envVars: new Dictionary<string, string> { { "DOTNET_MODIFIABLE_ASSEMBLIES", "debug" } });
                if (result != 100)
                {
                    return result;
                }
            }

            return 100;
        }
    }
}
