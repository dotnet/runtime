using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	public class FindNativeDeps : Task
	{
		/// <summary>
		///   The managed assemblies to scan for references to native
		///   dependencies.
		/// </summary>
		[Required]
		public ITaskItem [] ManagedAssemblyPaths { get; set; }

		/// <summary>
		///   The set of native dependencies to keep even if they
		///   aren't found to be referenced by a managed assembly.
		/// </summary>
		public ITaskItem [] NativeDepsToKeep { get; set; }

		/// <summary>
		///   The paths to the available native dependencies. We
		///   expect that all references found point to existing
		///   native files.
		/// </summary>
		[Required]
		public ITaskItem [] NativeDepsPaths { get; set; }

		/// <summary>
		///   The set of native dependencies to keep, including those
		///   found in the analysis, and those explicitly marked keep
		///   by NativeDepsToKeep. This includes metadata from the
		///   input NativeDepsToKeep.
		/// </summary>
		[Output]
		public ITaskItem [] KeptNativeDepsPaths { get; set; }

		public override bool Execute ()
		{
			var allNativeNames = new HashSet<string> ();
			foreach (var nativeDep in NativeDepsPaths)
				allNativeNames.Add (Path.GetFileName (nativeDep.ItemSpec));
			var keptNativeNames = new HashSet<string> ();
			foreach (var nativeDep in NativeDepsToKeep)
				keptNativeNames.Add (Path.GetFileName (nativeDep.ItemSpec));

			var managedAssemblies = ManagedAssemblyPaths.Select (i => i.ItemSpec).ToArray ();
			foreach (string managedAssembly in managedAssemblies) {
				using (var peReader = new PEReader(new FileStream (managedAssembly, FileMode.Open, FileAccess.Read, FileShare.Read))) {
					if (peReader.HasMetadata) {
						var reader = peReader.GetMetadataReader ();
						for (int i = 1, count = reader.GetTableRowCount (TableIndex.ModuleRef); i <= count; i++) {
							var moduleRef = reader.GetModuleReference (MetadataTokens.ModuleReferenceHandle (i));
							var moduleName = reader.GetString (moduleRef.Name);

							var moduleRefCandidates = new [] { moduleName, moduleName + ".dll", moduleName + ".so", moduleName + ".dylib" };

							bool foundModuleRef = false;
							foreach (string moduleRefCandidate in moduleRefCandidates) {
								if (allNativeNames.Contains (moduleRefCandidate)) {
									keptNativeNames.Add (moduleRefCandidate);
									foundModuleRef = true;
								}
							}

							if (!foundModuleRef)
								Log.LogMessage("unsatisfied DLLImport: " + managedAssembly + " -> " + moduleName);
						}
					}
				}
			}

			var keptNativeDeps = new List<ITaskItem> ();
			foreach (var nativeDep in NativeDepsPaths) {
				var fileName = Path.GetFileName (nativeDep.ItemSpec);
				if (keptNativeNames.Contains (fileName))
					keptNativeDeps.Add (nativeDep);
			}

			KeptNativeDepsPaths = keptNativeDeps.ToArray ();
			return true;
		}
	}
}
