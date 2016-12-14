using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Build.Utilities; // Task
using Microsoft.Build.Framework; // MessageImportance
using Microsoft.NET.Build.Tasks; // LockFileCache
using NuGet.ProjectModel; // LockFileTargetLibrary
using NuGet.Frameworks; // NuGetFramework.Parse(targetframework)

namespace ILLink.Tasks
{
	public class GetRuntimeLibraries : Task
	{
		/// <summary>
		///   Path to the assets file.
		/// </summary>
		[Required]
		public ITaskItem AssetsFilePath { get; set; }

		/// <summary>
		///   Target framework for which to get the platform
		///   libraries.
		/// </summary>
		[Required]
		public string TargetFramework { get; set; }

		/// <summary>
		///   Runtime identifier for which to get the platform
		///   libraries.
		/// </summary>
		[Required]
		public string RuntimeIdentifier { get; set; }

		/// <summary>
		///   Name of the library to consider the "platform"
		///   library.
		/// </summary>
		[Required]
		public string[] PackageNames { get; set; }

		[Output]
		public ITaskItem[] RuntimeLibraries { get; private set; }

		public override bool Execute()
		{
			var lockFile = new LockFileCache(BuildEngine4).GetLockFile(AssetsFilePath.ItemSpec);
			var lockFileTarget = lockFile.GetTarget(NuGetFramework.Parse(TargetFramework), RuntimeIdentifier);

			if (lockFileTarget == null) {
				var targetString = string.IsNullOrEmpty(RuntimeIdentifier) ? TargetFramework : $"{TargetFramework}/{RuntimeIdentifier}";

				throw new Exception($"Missing target section {targetString} from assets file {AssetsFilePath}. Ensure you have restored this project previously.");
			}

			Dictionary<string, LockFileTargetLibrary> packages = new Dictionary<string, LockFileTargetLibrary>(lockFileTarget.Libraries.Count, StringComparer.OrdinalIgnoreCase);

			foreach (var lib in lockFileTarget.Libraries) {
				packages.Add(lib.Name, lib);
			}

			HashSet<string> packageNames = new HashSet<string>(PackageNames);
			var rootPackages = lockFileTarget.Libraries.Where(l => packageNames.Contains(l.Name));

			var packageQueue = new Queue<LockFileTargetLibrary>(rootPackages);

			var libraries = new List<string>();
			while (packageQueue.Count > 0) {
				var package = packageQueue.Dequeue();
				foreach (var lib in package.RuntimeAssemblies) {
					libraries.Add(lib.ToString());
				}

				foreach (var dep in package.Dependencies.Select(d => d.Id)) {
					packageQueue.Enqueue(packages[dep]);
				}
			}

			RuntimeLibraries = libraries.Select(l => new TaskItem(l)).ToArray();

			return true;
		}
	}
}
