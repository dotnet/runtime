using System;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class RemoveResources : IStep {

		readonly I18nAssemblies assemblies;

		public RemoveResources (I18nAssemblies assemblies)
		{
			this.assemblies = assemblies;
		}

		public virtual void Process (LinkContext context)
		{
			AssemblyDefinition assembly;
			if (!context.TryGetLinkedAssembly ("mscorlib", out assembly))
				return;

			// skip this if we're not linking mscorlib, e.g. --linkskip=mscorlib
			if (context.Annotations.GetAction (assembly) != AssemblyAction.Link)
				return;

			var resources = assembly.MainModule.Resources;

			for (int i = 0; i < resources.Count; i++) {
				var resource = resources [i] as EmbeddedResource;
				if (resource == null)
					continue;

				if (RemoveResource (resource.Name))
					resources.RemoveAt(i--);
			}
		}

		bool RemoveResource (string name)
		{
			switch (name) {
			case "mscorlib.xml":
				return true;
			case "collation.core.bin":
			case "collation.tailoring.bin":
				return false;
			default:
				if (!name.Contains("cjk"))
					return false;
				if (IncludeCJK())
					return false;
				return true;
			}
		}

		bool IncludeCJK ()
		{
			return (assemblies & I18nAssemblies.CJK) != 0;
		}
	}
}
