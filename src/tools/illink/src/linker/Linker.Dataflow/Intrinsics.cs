// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct Intrinsics
	{
		readonly LinkContext _context;
		readonly MethodDefinition _callingMethodDefinition;

		public Intrinsics (LinkContext context, MethodDefinition callingMethodDefinition) => (_context, _callingMethodDefinition) = (context, callingMethodDefinition);

		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method)
			=> _context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (method.Method);

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method)
			=> _context.Annotations.FlowAnnotations.GetReturnParameterAnnotation (method.Method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (ResolveToTypeDefinition (method.Method.ReturnType), method.Method, dynamicallyAccessedMemberTypes);

		private partial string GetContainingSymbolDisplayName () => _callingMethodDefinition.GetDisplayName ();

		// Array types that are dynamically accessed should resolve to System.Array instead of its element type - which is what Cecil resolves to.
		// Any data flow annotations placed on a type parameter which receives an array type apply to the array itself. None of the members in its
		// element type should be marked.
		public TypeDefinition? ResolveToTypeDefinition (TypeReference typeReference)
		{
			if (typeReference is ArrayType)
				return BCL.FindPredefinedType ("System", "Array", _context);

			return _context.TryResolve (typeReference);
		}
	}
}
