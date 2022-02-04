// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct HandleCallAction
	{
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods

		readonly ISymbol _owningSymbol;
		readonly IOperation _operation;

		public HandleCallAction (ISymbol owningSymbol, IOperation operation) => (_owningSymbol, _operation) = (owningSymbol, operation);

		// TODO: This is relatively expensive on the analyzer since it doesn't cache the annotation information
		// In linker this is an optimization to avoid the heavy lifting of analysis if there's no point
		// it's unclear if the same optimization makes sense for the analyzer.
		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method) => FlowAnnotations.RequiresDataFlowAnalysis (method.Method);

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method) => FlowAnnotations.GetMethodReturnValueAnnotation (method.Method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (method.Method, dynamicallyAccessedMemberTypes);

		private partial string GetContainingSymbolDisplayName () => _operation.FindContainingSymbol (_owningSymbol).GetDisplayName ();
	}
}
