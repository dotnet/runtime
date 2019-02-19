using System;
using System.Collections.Generic;

using Mono.Cecil;

using Mono.Linker;

namespace Mono.Tuner {

	public static partial class Extensions {

		public static bool TryGetLinkedAssembly (this LinkContext context, string name, out AssemblyDefinition assembly)
		{
			assembly = GetAssembly (context, name);
			if (assembly == null)
				return false;

			return context.Annotations.GetAction (assembly) == AssemblyAction.Link;
		}

		public static AssemblyDefinition GetAssembly (this LinkContext context, string assembly_name)
		{
			foreach (var assembly in context.GetAssemblies ())
				if (assembly.Name.Name == assembly_name)
					return assembly;

			return null;
		}

		// note: direct check, no inheritance
		public static bool Is (this TypeReference type, string @namespace, string name)
		{
			return ((type != null) && (type.Name == name) && (type.Namespace == @namespace));
		}
	}
}
