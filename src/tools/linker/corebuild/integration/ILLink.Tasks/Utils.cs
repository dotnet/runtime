using System;
using Mono.Cecil;

public class Utils
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

	public static bool IsReadyToRunAssembly (string fileName)
	{
		try {
			ModuleDefinition module = ModuleDefinition.ReadModule (fileName);
			return (module.Attributes & ModuleAttributes.ILOnly) == 0 &&
				(module.Attributes & (ModuleAttributes) 0x04) != 0;
		} catch (BadImageFormatException) {
			return false;
		}
	}
}
