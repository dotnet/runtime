// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

		public static bool IsDeclaredOnType (this MethodReference method, string namespaceName, string typeName)
		{
			return method.DeclaringType.IsTypeOf (namespaceName, typeName);
		}

		public static bool HasParameterOfType (this MethodReference method, int parameterIndex, string namespaceName, string typeName)
		{
			return method.Parameters.Count > parameterIndex && method.Parameters [parameterIndex].ParameterType.IsTypeOf (namespaceName, typeName);
		}
	}
}
