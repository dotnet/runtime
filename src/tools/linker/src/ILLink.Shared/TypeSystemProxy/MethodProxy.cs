// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

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
		internal partial string GetParameterDisplayName (int parameterIndex);
		internal partial bool HasGenericParameters ();
		internal partial bool HasGenericParametersCount (int genericParameterCount);
		internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters ();
		internal partial bool IsStatic ();
		internal partial bool ReturnsVoid ();
	}
}
