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
		public ITaskItem[] ManagedAssemblyPaths { get; set; }

		/// <summary>
		///   The set of native dependencies to keep even if they
		///   aren't found to be referenced by a managed assembly..
		/// </summary>
		public ITaskItem[] NativeDepsToKeep { get; set; }

		/// <summary>
		///   The paths to the available native dependencies. We
		///   expect that all references found point to existing
		///   native files.
		/// </summary>
		[Required]
		public ITaskItem[] NativeDepsPaths { get; set; }

		/// <summary>
		///   The set of native dependencies to keep, including those
		///   found in the analysis, and those explicitly marked keep
		///   by NativeDepsToKeep. This includes metadata from the
		///   input NativeDepsToKeep.
		/// </summary>
		[Output]
		public ITaskItem[] KeptNativeDepsPaths { get; set; }

		public override bool Execute()
		{
			var allNative = new Dictionary<string, ITaskItem> ();
			foreach (var n in NativeDepsPaths)
			{
				var fileName = Path.GetFileName(n.ItemSpec);
				if (!allNative.ContainsKey(fileName))
				{
					allNative.Add(fileName, n);
				}
			}
			var keptNative = new List<ITaskItem> ();
			var managedAssemblies = ManagedAssemblyPaths.Select (i => i.ItemSpec).ToArray();
			foreach (string managedAssembly in managedAssemblies)
			{
				using (var peReader = new PEReader(new FileStream(managedAssembly, FileMode.Open, FileAccess.Read, FileShare.Read)))
				{
					if (peReader.HasMetadata)
					{
						var reader = peReader.GetMetadataReader();
						for (int i = 1, count = reader.GetTableRowCount(TableIndex.ModuleRef); i <= count; i++)
						{
							var moduleRef = reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(i));
							var moduleName = reader.GetString(moduleRef.Name);

							var moduleRefCandidates = new[] { moduleName, moduleName + ".dll", moduleName + ".so", moduleName + ".dylib" };

							ITaskItem referencedNativeFile = null;
							foreach (string moduleRefCandidate in moduleRefCandidates)
							{
								if (allNative.TryGetValue (moduleRefCandidate, out referencedNativeFile))
								{
									break;
								}
							}

							if (referencedNativeFile != null)
							{
								keptNative.Add(referencedNativeFile);
							}
							else
							{
								// DLLImport that wasn't satisfied
								Log.LogMessage(MessageImportance.High, "unsatisfied DLLImport: " + managedAssembly + " -> " + moduleName);
							}
						}
					}
				}
			}

			foreach (var n in NativeDepsToKeep)
			{
				ITaskItem nativeFile = null;
				if (allNative.TryGetValue (n.ItemSpec, out nativeFile))
				{
					keptNative.Add(nativeFile);
				}
			}
			KeptNativeDepsPaths = keptNative.ToArray();
			return true;
		}
	}
}
