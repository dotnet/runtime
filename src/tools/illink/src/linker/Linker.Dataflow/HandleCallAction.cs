// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Dataflow;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct HandleCallAction
	{
		readonly LinkContext _context;
		readonly MethodDefinition _callingMethodDefinition;

		public HandleCallAction (
			LinkContext context,
			MethodDefinition callingMethodDefinition)
		{
			_context = context;
			_callingMethodDefinition = callingMethodDefinition;
		}

		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method)
			=> _context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (method.Method);

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method)
			=> _context.Annotations.FlowAnnotations.GetReturnParameterAnnotation (method.Method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (ReflectionMethodBodyScanner.ResolveToTypeDefinition (_context, method.Method.ReturnType), method.Method, dynamicallyAccessedMemberTypes);

		private partial string GetContainingSymbolDisplayName () => _callingMethodDefinition.GetDisplayName ();
	}
}
