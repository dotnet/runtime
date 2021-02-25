using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Collections.Generic;

namespace MonoDelta {
    public class DeltaHelper {
        private static Action<Assembly, byte[], byte[], byte[]> _updateMethod;

        private static Action<Assembly, byte[], byte[], byte[]> UpdateMethod => _updateMethod ?? InitUpdateMethod();

        private static Action<Assembly, byte[], byte[], byte[]> InitUpdateMethod ()
        {
            var monoType = typeof(System.Reflection.Metadata.AssemblyExtensions);
            const string methodName = "ApplyUpdate";
            var mi = monoType.GetMethod (methodName, BindingFlags.Public | BindingFlags.Static);
            if (mi == null)
                throw new Exception ($"Couldn't get {methodName} from {monoType.FullName}");
            _updateMethod = MakeUpdateMethod (mi); //Delegate.CreateDelegate (typeof(Action<Assembly, byte[], byte[], byte[]>), mi) as Action<Assembly, byte[], byte[], byte[]>;
            return _updateMethod;
        }

        private static Action<Assembly, byte[], byte[], byte[]> MakeUpdateMethod (MethodInfo applyUpdate)
        {
            // Make
            // void ApplyUpdateArray (Assembly a, byte[] dmeta, byte[] dil, byte[] dpdb)
            // {
            //   ApplyUpdate (a, (ReadOnlySpan<byte>)dmeta, (ReadOnlySpan<byte>)dil, (ReadOnlySpan<byte>)dpdb);
            // }
            var dm = new DynamicMethod ("CallApplyUpdate", typeof(void), new Type[] { typeof(Assembly), typeof(byte[]), typeof(byte[]), typeof(byte[])}, typeof (DeltaHelper).Module);
            var ilg = dm.GetILGenerator ();
            var conv = typeof(ReadOnlySpan<byte>).GetMethod("op_Implicit", new Type[] {typeof(byte[])});

            ilg.Emit (OpCodes.Ldarg_0);
            ilg.Emit (OpCodes.Ldarg_1);
            ilg.Emit (OpCodes.Call, conv);
            ilg.Emit (OpCodes.Ldarg_2);
            ilg.Emit (OpCodes.Call, conv);
            ilg.Emit (OpCodes.Ldarg_3);
            ilg.Emit (OpCodes.Call, conv);
            ilg.Emit (OpCodes.Call, applyUpdate);
            ilg.Emit (OpCodes.Ret);

            return dm.CreateDelegate(typeof(Action<Assembly, byte[], byte[], byte[]>)) as Action<Assembly, byte[], byte[], byte[]>;
        }

        private static void LoadMetadataUpdate (Assembly assm, byte[] dmeta_data, byte[] dil_data, byte[] dpdb_data)
        {
            UpdateMethod (assm, dmeta_data, dil_data, dpdb_data);
        }

        DeltaHelper () { }

        public static DeltaHelper Make ()
        {
            return new DeltaHelper ();
        }

        public static void InjectUpdate (string assemblyName, string dmeta_base64, string dil_base64) {
            var an = new AssemblyName (assemblyName);
            Assembly assm = null;
            /* TODO: non-default ALCs */
            foreach (var candidate in AssemblyLoadContext.Default.Assemblies) {
                if (candidate.GetName().Name == an.Name) {
                    assm = candidate;
                    break;
                }
            }
            if (assm == null)
                throw new ArgumentException ("assemblyName");
            var dmeta_data = Convert.FromBase64String (dmeta_base64);
            var dil_data = Convert.FromBase64String (dil_base64);
            byte[] dpdb_data = null;
            LoadMetadataUpdate (assm, dmeta_data, dil_data, dpdb_data);
        }

        private Dictionary<Assembly, int> assembly_count = new Dictionary<Assembly, int> ();

        public void Update (Assembly assm) {
            int count;
            if (!assembly_count.TryGetValue (assm, out count))
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

            LoadMetadataUpdate (assm, dmeta_data, dil_data, dpdb_data);
        }
    }
}
