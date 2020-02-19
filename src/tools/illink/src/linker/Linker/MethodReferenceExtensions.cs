using System;
using Mono.Cecil;

namespace Mono.Linker
{
	public static class MethodReferenceExtensions
	{
		public static TypeReference GetReturnType (this MethodReference method)
		{
			if (method.DeclaringType is GenericInstanceType genericInstance)
				return TypeReferenceExtensions.InflateGenericType (genericInstance, method.ReturnType);

			return method.ReturnType;
		}

		public static TypeReference GetParameterType (this MethodReference method, int parameterIndex)
		{
			if (method.DeclaringType is GenericInstanceType genericInstance)
				return TypeReferenceExtensions.InflateGenericType (genericInstance, method.Parameters [parameterIndex].ParameterType);

			return method.Parameters [parameterIndex].ParameterType;
		}
	}
}
