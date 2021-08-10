// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sample
{
    public class Test
    {
        public static void Main(string[] args)
        {
            Console.WriteLine ("Hello, World!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestMeaning()
        {
            const int success = 42;
            const int failure = 1;

            var ty = typeof(System.Reflection.Metadata.MetadataUpdater);
            var mi = ty.GetMethod("GetCapabilities", BindingFlags.NonPublic | BindingFlags.Static, Array.Empty<Type>());

            if (mi == null)
                return failure;

            var caps = mi.Invoke(null, null) as string;

            if (String.IsNullOrEmpty(caps))
                return failure;

            var assm = typeof (ApplyUpdateReferencedAssembly.MethodBody1).Assembly;

            var r = ApplyUpdateReferencedAssembly.MethodBody1.StaticMethod1();
            if ("OLD STRING" != r)
                return failure;

            ApplyUpdate(assm);

            r = ApplyUpdateReferencedAssembly.MethodBody1.StaticMethod1();
            if ("NEW STRING" != r)
                return failure;

            ApplyUpdate(assm);

            r = ApplyUpdateReferencedAssembly.MethodBody1.StaticMethod1();
            if ("NEWEST STRING" != r)
                return failure;

            return success;
        }

        private static System.Collections.Generic.Dictionary<Assembly, int> assembly_count = new();

        internal static void ApplyUpdate (System.Reflection.Assembly assm)
        {
            int count;
            if (!assembly_count.TryGetValue(assm, out count))
                count = 1;
            else
                count++;
            assembly_count [assm] = count;

            /* FIXME WASM: Location is empty on wasm. Make up a name based on Name */
            string basename = assm.Location;
            if (basename == "")
                basename = assm.GetName().Name + ".dll";
            Console.Error.WriteLine($"Apply Delta Update for {basename}, revision {count}");

            string dmeta_name = $"{basename}.{count}.dmeta";
            string dil_name = $"{basename}.{count}.dil";
            byte[] dmeta_data = System.IO.File.ReadAllBytes(dmeta_name);
            byte[] dil_data = System.IO.File.ReadAllBytes(dil_name);
            byte[] dpdb_data = null; // TODO also use the dpdb data

            System.Reflection.Metadata.MetadataUpdater.ApplyUpdate(assm, dmeta_data, dil_data, dpdb_data);
        }
    }
}
