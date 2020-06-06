using Mono.Cecil;

namespace Mono.Linker
{

	public static class AssemblyUtilities
	{

		public static bool IsCrossgened (this ModuleDefinition module)
		{
			return (module.Attributes & ModuleAttributes.ILOnly) == 0 &&
				(module.Attributes & ModuleAttributes.ILLibrary) != 0;
		}

		public static TypeDefinition ResolveFullyQualifiedTypeName (LinkContext context, string name)
		{
			if (!TypeNameParser.TryParseTypeAssemblyQualifiedName (name, out string typeName, out string assemblyName))
				return null;

			foreach (var assemblyDefinition in context.GetAssemblies ()) {
				if (assemblyName != null && assemblyDefinition.Name.Name != assemblyName)
					continue;

				var foundType = assemblyDefinition.MainModule.GetType (typeName);
				if (foundType == null)
					continue;

				return foundType;
			}

			return null;
		}

		public static TypeDefinition FindType (this AssemblyDefinition assembly, string fullName)
		{
			fullName = fullName.ToCecilName ();

			var type = assembly.MainModule.GetType (fullName);
			return type?.Resolve ();
		}

		public static EmbeddedResource FindEmbeddedResource (this AssemblyDefinition assembly, string name)
		{
			foreach (var resource in assembly.MainModule.Resources) {
				if (resource is EmbeddedResource embeddedResource && embeddedResource.Name == name)
					return embeddedResource;
			}
			return null;
		}
	}
}
