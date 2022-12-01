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

		/// <summary>
		/// Returns the number of the parameters in the 'parameters' metadata section. This should map directly to the number of parameters in the C# source declaration as well.
		/// </summary>
		internal partial int GetMetadataParametersCount ();

		/// <summary>
		/// Returns true if the method has parameters in the 'parameters' metadata section (i.e. has parameters besides the implicit 'this' parameter)
		/// </summary>
		internal partial bool HasMetadataParameters ();

		/// <summary>
		/// Returns the number of parameters that are passed to the method in IL (including the implicit 'this' parameter).
		/// In pseudocode: <code>method.HasImplicitThis() ? 1 + MetadataParametersCount : MetadataParametersCount;</code>
		/// </summary>
		internal partial int GetParametersCount ();

		/// <summary>
		/// Returns a List of <see cref="ParameterProxy"/> representing the parameters the method takes, including the implicit 'this' parameters.
		/// </summary>
		internal partial ParameterProxyEnumerable GetParameters ();

		/// <summary>
		/// Returns the ParameterProxy corresponding to the parameter at <paramref name="index"/>, and throws if the index is out of bounds for the method.
		/// <paramref name="index"/> is the index of the parameters as they are passed to the method, with 0 being the implicit this parameter if it exists.
		/// See <see cref="ParameterIndex"/> for more info.
		/// </summary>
		internal partial ParameterProxy GetParameter (ParameterIndex index);

		/// <summary>
		/// Returns true if the 'parameters' metadata section has <paramref name="parameterCount"/> number of parameters.
		/// Metadata parameters count maps directly to the number of parameters in C# source code.
		/// Metadata parameters count excludes the implicit 'this' parameter.
		/// </summary>
		internal bool HasMetadataParametersCount (int parameterCount) => GetMetadataParametersCount () == parameterCount;

		// Currently this only needs to work on non-nested, non-generic types.
		// The format of the fullTypeName parameter is 'namespace.typename', so for example 'System.Reflection.Assembly'
		internal bool HasParameterOfType (ParameterIndex parameterIndex, string fullTypeName)
			=> (int) parameterIndex < GetParametersCount () && GetParameter (parameterIndex).IsTypeOf (fullTypeName);
		internal partial bool HasGenericParameters ();
		internal partial bool HasGenericParametersCount (int genericParameterCount);
		internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters ();
		internal partial bool IsStatic ();
		internal partial bool HasImplicitThis ();
		internal partial bool ReturnsVoid ();
	}
}
