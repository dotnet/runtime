using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	public class ComputeReadyToRunAssemblies : Task
	{
		/// <summary>
		///   Paths to assemblies.
		/// </summary>
		[Required]
		public ITaskItem[] Assemblies { get; set; }

		/// <summary>
		///   This will contain the output list of
		///   ready-to-run assemblies. Metadata from the input
		///   parameter Assemblies is preserved.
		/// </summary>
		[Output]
		public ITaskItem[] ReadyToRunAssemblies { get; set; }

		public override bool Execute()
		{
			ReadyToRunAssemblies = Assemblies
				.Where(f => Utils.IsCrossgenedAssembly(f.ItemSpec))
				.ToArray();
			return true;
		}
	}
}
