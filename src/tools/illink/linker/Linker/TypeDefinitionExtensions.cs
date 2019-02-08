using Mono.Cecil;

namespace Mono.Linker {
	public static class TypeDefinitionExtensions {
		public static bool HasInterface (this TypeDefinition type, TypeDefinition interfaceType, out InterfaceImplementation implementation)
		{
			implementation = null;
			if (!type.HasInterfaces)
				return false;

			foreach (var iface in type.Interfaces) {
				if (iface.InterfaceType.Resolve () == interfaceType) {
					implementation = iface;
					return true;
				}
			}

			return false;
		}
	}
}