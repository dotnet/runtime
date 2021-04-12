// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Metadata
{

    [CollectionDefinition("NoParallelTests", DisableParallelization = true)]
    public partial class NoParallelTests { }

    public class ApplyUpdateUtil {
        // FIXME: Use runtime API https://github.com/dotnet/runtime/issues/50111 when it is approved/implemented
        public static bool IsSupported => IsModifiableAssembliesSet &&
	    (!IsMonoRuntime || IsSupportedMonoConfiguration()) &&
            IsSupportedTestConfiguration();

	public static bool IsModifiableAssembliesSet =>
	    String.Equals("debug", Environment.GetEnvironmentVariable("DOTNET_MODIFIABLE_ASSEMBLIES"), StringComparison.InvariantCultureIgnoreCase);

        // copied from https://github.com/dotnet/arcade/blob/6cc4c1e9e23d5e65e88a8a57216b3d91e9b3d8db/src/Microsoft.DotNet.XUnitExtensions/src/DiscovererHelpers.cs#L16-L17
        private static readonly Lazy<bool> s_isMonoRuntime = new Lazy<bool>(() => Type.GetType("Mono.RuntimeStructs") != null);
        public static bool IsMonoRuntime => s_isMonoRuntime.Value;
        
        // Not every build of Mono supports ApplyUpdate
        internal static bool IsSupportedMonoConfiguration()
        {
#if FEATURE_MONO_APPLY_UPDATE
            // crude check for interp mode
            return System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported && !System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled;
#else
            return false;
#endif
        }


        // Only Debug assemblies are editable
        internal static bool IsSupportedTestConfiguration()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        private static System.Collections.Generic.Dictionary<Assembly, int> assembly_count = new ();

        public static void ApplyUpdate (System.Reflection.Assembly assm)
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
            Console.WriteLine ($"Apply Delta Update for {basename}, revision {count}");

            string dmeta_name = $"{basename}.{count}.dmeta";
            string dil_name = $"{basename}.{count}.dil";
            byte[] dmeta_data = System.IO.File.ReadAllBytes (dmeta_name);
            byte[] dil_data = System.IO.File.ReadAllBytes (dil_name);
            byte[] dpdb_data = null; // TODO also use the dpdb data

            AssemblyExtensions.ApplyUpdate(assm, dmeta_data, dil_data, dpdb_data);
        }
    }

}    
