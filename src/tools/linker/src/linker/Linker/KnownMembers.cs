using Mono.Cecil;

namespace Mono.Linker
{
	public class KnownMembers
	{
		public MethodReference NotSupportedExceptionCtorString { get; set; }

		public static bool IsNotSupportedExceptionCtorString (MethodDefinition method)
		{
			if (!method.IsConstructor || method.IsStatic || !method.HasParameters)
				return false;

			if (method.Parameters.Count != 1 || method.Parameters [0].ParameterType.MetadataType != MetadataType.String)
				return false;

			return true;
		}
	}
}
