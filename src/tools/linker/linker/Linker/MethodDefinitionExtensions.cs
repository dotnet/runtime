using Mono.Cecil;

namespace Mono.Linker
{
	static class MethodDefinitionExtensions 
	{
		public static bool IsDefaultConstructor (this MethodDefinition method)
		{
			return IsInstanceConstructor (method) && !method.HasParameters;
		}

		public static bool IsInstanceConstructor (this MethodDefinition method)
		{
			return method.IsConstructor && !method.IsStatic;
		}

		public static bool IsStaticConstructor (this MethodDefinition method)
		{
			return method.IsConstructor && method.IsStatic;
		}

		public static bool IsFinalizer (this MethodDefinition method)
		{
			if (method.Name != "Finalize" || method.ReturnType.MetadataType != MetadataType.Void)
				return false;

			if (method.HasParameters || method.HasGenericParameters || method.IsStatic)
				return false;

			return true;
		}
	}
}
