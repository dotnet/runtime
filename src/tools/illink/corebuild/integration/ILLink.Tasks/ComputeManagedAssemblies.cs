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
	public class ComputeManagedAssemblies : Task
	{
		/// <summary>
		///   Paths to assemblies.
		/// </summary>
		[Required]
		public ITaskItem[] Assemblies { get; set; }

		/// <summary>
		///   This will contain the output list of managed
		///   assemblies. Metadata from the input parameter
		///   Assemblies is preserved.
		/// </summary>
		[Output]
		public ITaskItem[] ManagedAssemblies { get; set; }

		public override bool Execute()
		{
			var managedAssemblies = new List<ITaskItem>();
			foreach (var f in Assemblies) {
				try {
					AssemblyName.GetAssemblyName(f.ItemSpec);
					managedAssemblies.Add(f);
				} catch (BadImageFormatException) {
				}
			}
			ManagedAssemblies = managedAssemblies.ToArray();

			return true;
		}
	}
}
