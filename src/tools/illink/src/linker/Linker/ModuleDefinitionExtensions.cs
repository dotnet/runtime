using Mono.Cecil;

namespace Mono.Linker
{
	public static class ModuleDefinitionExtensions
	{

		public static bool IsCrossgened (this ModuleDefinition module)
		{
			return (module.Attributes & ModuleAttributes.ILOnly) == 0 &&
				(module.Attributes & ModuleAttributes.ILLibrary) != 0;
		}
	}
}
