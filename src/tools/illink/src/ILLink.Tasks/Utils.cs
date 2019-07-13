using System;
using Mono.Cecil;
using System.Linq;

public static class Utils
{
	public static bool IsManagedAssembly (string fileName)
	{
		try {
			ModuleDefinition module = ModuleDefinition.ReadModule (fileName);
			return !IsCPPCLIAssembly (module);
		} catch (BadImageFormatException) {
			return false;
		}
	}

	private static bool IsCPPCLIAssembly (ModuleDefinition module)
	{
		return module.Types.Any(t =>
			t.Namespace == "<CppImplementationDetails>" ||
			t.Namespace == "<CrtImplementationDetails>");
	}
}
