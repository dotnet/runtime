using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;

namespace ILLink.Tasks
{
	public class SetAssemblyActions : Task
	{
		/// <summary>
		///   Paths to the assembly files that should be considered as
		///   input to the linker.
		/// </summary>
		[Required]
		public ITaskItem [] AssemblyPaths { get; set; }

		/// <summary>
		///   Application assembly names.
		/// </summary>
		[Required]
		public ITaskItem [] ApplicationAssemblyNames { get; set; }

		/// <summary>
		///   Platform assembly names.
		/// </summary>
		[Required]
		public ITaskItem [] PlatformAssemblyNames { get; set; }

		/// <summary>
		///   Action to perform on used application assemblies.
		/// </summary>
		[Required]
		public ITaskItem UsedApplicationAssemblyAction { get; set; }

		/// <summary>
		///   Action to perform on unused application assemblies.
		/// </summary>
		[Required]
		public ITaskItem UnusedApplicationAssemblyAction { get; set; }

		/// <summary>
		///   Action to perform on used platform assemblies.
		/// </summary>
		[Required]
		public ITaskItem UsedPlatformAssemblyAction { get; set; }

		/// <summary>
		///   Action to perform on unused platform assemblies.
		/// </summary>
		[Required]
		public ITaskItem UnusedPlatformAssemblyAction { get; set; }

		[Output]
		public ITaskItem [] AssemblyPathsWithActions { get; set; }

		public override bool Execute ()
		{
			string applicationAssemblyAction;
			string usedApplicationAssemblyAction = UsedApplicationAssemblyAction.ItemSpec;
			string unusedApplicationAssemblyAction = UnusedApplicationAssemblyAction.ItemSpec;
			if (!GetAssemblyAction (usedApplicationAssemblyAction.ToLower (), unusedApplicationAssemblyAction.ToLower (), out applicationAssemblyAction)) {
				Log.LogError ("Unsupported combination of application assembly actions: {0}, {1}.",
							  usedApplicationAssemblyAction, unusedApplicationAssemblyAction);
				return false;
			}

			string platformAssemblyAction;
			string usedPlatformAssemblyAction = UsedPlatformAssemblyAction.ItemSpec;
			string unusedPlatformAssemblyAction = UnusedPlatformAssemblyAction.ItemSpec;
			if (!GetAssemblyAction (usedPlatformAssemblyAction.ToLower (), unusedPlatformAssemblyAction.ToLower (), out platformAssemblyAction)) {
				Log.LogError ("Unsupported combination of platform assembly actions: {0}, {1}.",
							  usedPlatformAssemblyAction, unusedPlatformAssemblyAction);
				return false;
			}

			List<ITaskItem> resultAssemblies = new List<ITaskItem> ();

			AddAssemblyActionMetadata (ApplicationAssemblyNames, applicationAssemblyAction, resultAssemblies);
			AddAssemblyActionMetadata (PlatformAssemblyNames, platformAssemblyAction, resultAssemblies);

			AssemblyPathsWithActions = resultAssemblies.ToArray ();

			return true;
		}

		bool GetAssemblyAction (string usedAssemblyAction, string unusedAssemblyAction, out string assemblyAction)
		{
			assemblyAction = "illegal";
			if ((unusedAssemblyAction != usedAssemblyAction) && (unusedAssemblyAction != "delete")) {
				return false;
			}

			switch (usedAssemblyAction) {
			case "link":
				assemblyAction = "link";
				break;

			case "copy":
				assemblyAction = (unusedAssemblyAction == "delete") ? "copyused" : "copy";
				break;

			case "addbypassngen":
				assemblyAction = (unusedAssemblyAction == "delete") ? "addbypassngenused" : "addbypassngen";
				break;

			case "skip":
				if (unusedAssemblyAction != usedAssemblyAction) {
					return false;
				}
				assemblyAction = "skip";
				break;

			default:
				return false;
			}

			return true;
		}

		void AddAssemblyActionMetadata (ITaskItem [] assemblies, string action, List<ITaskItem> resultList)
		{
			HashSet<string> assemblyHashSet = new HashSet<string> ();
			foreach (var assembly in assemblies) {
				assemblyHashSet.Add (assembly.ItemSpec);
			}

			foreach (var assembly in AssemblyPaths) {
				if (assemblyHashSet.Contains (Path.GetFileNameWithoutExtension (assembly.ItemSpec))) {
					assembly.SetMetadata ("action", action);
					resultList.Add (assembly);
				}
			}
		}

	}
}
