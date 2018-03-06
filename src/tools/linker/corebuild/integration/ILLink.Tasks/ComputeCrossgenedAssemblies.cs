using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	public class ComputeCrossgenedAssemblies : Task
	{
		/// <summary>
		///   Paths to assemblies.
		/// </summary>
		[Required]
		public ITaskItem[] Assemblies { get; set; }

		/// <summary>
		///   This will contain the output list of crossgen-ed
		///   assemblies. Metadata from the input parameter
		///   Assemblies is preserved.
		/// </summary>
		[Output]
		public ITaskItem[] CrossgenedAssemblies { get; set; }

		public override bool Execute()
		{
			CrossgenedAssemblies = Assemblies
				.Where(f => Utils.IsCrossgenedAssembly(f.ItemSpec))
				.ToArray();
			return true;
		}
	}
}
