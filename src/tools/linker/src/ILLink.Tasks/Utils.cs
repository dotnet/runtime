using System;
using Mono.Cecil;

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
}
