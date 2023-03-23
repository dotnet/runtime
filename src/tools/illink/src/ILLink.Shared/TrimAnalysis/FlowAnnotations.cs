// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	// Shared helpers to go from MethodProxy to dataflow values.
#if ILLINK
	internal
#else
	public
#endif
	partial class FlowAnnotations
	{
		internal partial bool MethodRequiresDataFlowAnalysis (MethodProxy method);

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method);

		internal partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter);

		internal partial MethodParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

		internal partial MethodParameterValue GetMethodThisParameterValue (MethodProxy method);

		internal partial MethodParameterValue GetMethodParameterValue (ParameterProxy param, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

		internal partial MethodParameterValue GetMethodParameterValue (ParameterProxy param);
	}
}
