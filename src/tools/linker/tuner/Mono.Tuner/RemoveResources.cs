using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;

namespace Mono.Tuner {

	public class RemoveResources : IStep {

		I18nAssemblies assemblies;

		public RemoveResources (I18nAssemblies assemblies)
		{
			this.assemblies = assemblies;
		}

		public void Process (LinkContext context)
		{
			AssemblyDefinition assembly;
			if (!context.TryGetLinkedAssembly ("mscorlib", out assembly))
				return;

			var resources = assembly.MainModule.Resources;

			for (int i = 0; i < resources.Count; i++) {
				var resource = resources [i] as EmbeddedResource;
				if (resource == null)
					continue;

				switch (resource.Name) {
				case "collation.core.bin":
				case "collation.tailoring.bin":
					continue;
				default:
					if (!resource.Name.Contains ("cjk"))
						continue;
					if (IncludeCJK ())
						continue;

					resources.RemoveAt (i--);
					break;
				}
			}
		}

		bool IncludeCJK ()
		{
			return (assemblies & I18nAssemblies.CJK) != 0;
		}
	}
}
