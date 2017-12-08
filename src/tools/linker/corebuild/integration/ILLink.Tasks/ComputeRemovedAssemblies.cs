using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	public class ComputeRemovedAssemblies : Task
	{
		/// <summary>
		///   The paths to the inputs to the linker.
		/// </summary>
		[Required]
		public ITaskItem[] InputAssemblies { get; set; }

		/// <summary>
		///   The paths to the linked assemblies.
		/// </summary>
		[Required]
		public ITaskItem[] KeptAssemblies { get; set; }

		/// <summary>
		///   The set of assemblies in the inputs that weren't kept by
		///   the linker. These items include the full metadata from
		///   the input assemblies, and only the filenames of the
		///   inputs are used to determine which assemblies were
		///   removed.
		/// </summary>
		[Output]
		public ITaskItem[] RemovedAssemblies { get; set; }

		public override bool Execute()
		{
			var keptAssemblyNames = new HashSet<string> (
				KeptAssemblies.Select(i => Path.GetFileName(i.ItemSpec))
			);
			RemovedAssemblies = InputAssemblies.Where(i =>
				!keptAssemblyNames.Contains(Path.GetFileName(i.ItemSpec))
			).ToArray();
			return true;
		}
	}
}
