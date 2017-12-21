using Microsoft.Build.Utilities; // Task
using Microsoft.Build.Framework; // ITaskItem
using Mono.Cecil;

namespace ILLink.Tasks
{
	public class CheckEmbeddedRootDescriptor : Task
	{
		/// <summary>
		///   Path to the assembly.
		/// </summary>
		[Required]
		public ITaskItem AssemblyPath { get; set; }

		/// <summary>
		///   This will be set to true if the assembly has an embedded root descriptor.
		/// </summary>
		[Output]
		public bool HasEmbeddedRootDescriptor { get; set; }

		public override bool Execute()
		{
			ModuleDefinition module = ModuleDefinition.ReadModule (AssemblyPath.ItemSpec);
			string assemblyName = module.Assembly.Name.Name;
			string expectedResourceName = assemblyName + ".xml";
			HasEmbeddedRootDescriptor = false;
			foreach (var resource in module.Resources) {
				if (resource.Name == expectedResourceName) {
					HasEmbeddedRootDescriptor = true;
					break;
				}
			}

			return true;
		}
	}
}
