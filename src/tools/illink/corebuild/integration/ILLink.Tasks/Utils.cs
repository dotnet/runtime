using System;
using Mono.Cecil;
using Mono.Linker;

public static class Utils
{
	public static bool IsManagedAssembly (string fileName)
	{
		try {
			ModuleDefinition module = ModuleDefinition.ReadModule (fileName);
			return true;
		} catch (BadImageFormatException) {
			return false;
		}
	}

	public static bool IsCrossgenedAssembly (string fileName)
	{
		try {
			ModuleDefinition module = ModuleDefinition.ReadModule (fileName);
			return module.IsCrossgened ();
		} catch (BadImageFormatException) {
			return false;
		}
	}
}
