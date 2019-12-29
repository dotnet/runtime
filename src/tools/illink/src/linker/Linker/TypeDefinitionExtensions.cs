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

		public static bool IsMulticastDelegate (this TypeDefinition td)
		{
			return td.BaseType?.Name == "MulticastDelegate" && td.BaseType.Namespace == "System";
		}

		public static bool IsSerializable (this TypeDefinition td)
		{
			return (td.Attributes & TypeAttributes.Serializable) != 0;
		}
	}
}