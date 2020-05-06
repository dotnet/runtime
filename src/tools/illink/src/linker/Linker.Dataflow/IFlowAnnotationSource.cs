// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	interface IFlowAnnotationSource
	{
		DynamicallyAccessedMemberTypes GetPropertyAnnotation (PropertyDefinition property);

		// Index refers to the index in the formal parameter list (i.e. there's no index for `this` on instance methods)
		DynamicallyAccessedMemberTypes GetParameterAnnotation (MethodDefinition method, int index);

		DynamicallyAccessedMemberTypes GetReturnParameterAnnotation (MethodDefinition method);

		// Should return annotation which applies to the "this" parameter of the method
		// Note that this does not apply to the "this" parameter on extension methods, it's the this on instance methods.
		DynamicallyAccessedMemberTypes GetThisParameterAnnotation (MethodDefinition method);

		DynamicallyAccessedMemberTypes GetFieldAnnotation (FieldDefinition field);
	}
}
