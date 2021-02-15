using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Collections.Generic;

namespace MonoDelta {
	public class DeltaHelper {
		const string name = "System.Runtime.CompilerServices.RuntimeFeature";

		private static MethodBase _updateMethod;

		private static MethodBase UpdateMethod => _updateMethod ?? InitUpdateMethod();

		private static MethodBase InitUpdateMethod ()
		{
			var monoType = Type.GetType (name, throwOnError: true);
			if (monoType == null)
				throw new Exception ($"Couldn't get the type {name}");
			_updateMethod = monoType.GetMethod ("LoadMetadataUpdate", BindingFlags.NonPublic | BindingFlags.Static);
			if (_updateMethod == null)
				throw new Exception ($"Couldn't get LoadMetadataUpdate from {name}");
			return _updateMethod;
		}

		private static void LoadMetadataUpdate (Assembly assm, byte[] dmeta_data, byte[] dil_data)
		{
			UpdateMethod.Invoke (null, new object [] { assm, dmeta_data, dil_data});
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
			LoadMetadataUpdate (assm, dmeta_data, dil_data);
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

			LoadMetadataUpdate (assm, dmeta_data, dil_data);
		}
	}
}
