// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct MethodProxy : IMemberProxy
	{
		// Currently this only needs to work on non-nested, non-generic types.
		// The format of the fullTypeName parameter is 'namespace.typename', so for example 'System.Reflection.Assembly'
		internal partial bool IsDeclaredOnType (string fullTypeName);
		internal partial bool HasParameters ();
		internal partial int GetParametersCount ();
		internal bool HasParametersCount (int parameterCount) => GetParametersCount () == parameterCount;
		// Currently this only needs to work on non-nested, non-generic types.
		// The format of the fullTypeName parameter is 'namespace.typename', so for example 'System.Reflection.Assembly'
		internal partial bool HasParameterOfType (int parameterIndex, string fullTypeName);
		internal partial bool HasGenericParameters ();
		internal partial bool HasGenericParametersCount (int genericParameterCount);
		internal partial bool IsStatic ();
		internal partial bool ReturnsVoid ();
	}
}
