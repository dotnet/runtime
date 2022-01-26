// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct MethodProxy : IMemberProxy
	{
		// Currently this only needs to work on non-nested, non-generic types.
		// The format of the fullTypeName parameter is 'namespace.typename', so for example 'System.Reflection.Assembly'
		internal partial bool IsDeclaredOnType (string fullTypeName);
		internal partial bool HasParameters ();
		internal partial bool HasParametersCount (int parameterCount);
		// Currently this only needs to work on non-nested, non-generic types.
		// The format of the fullTypeName parameter is 'namespace.typename', so for example 'System.Reflection.Assembly'
		internal partial bool HasParameterOfType (int parameterIndex, string fullTypeName);
		internal partial bool HasGenericParameters ();
		internal partial bool HasGenericParametersCount (int genericParameterCount);
		internal partial bool IsStatic ();
		internal partial TypeProxy GetReturnType ();
	}
}
