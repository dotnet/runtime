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
#pragma warning disable IDE0060 // Unused parameters - the other partial implementation may need the parameter

		readonly ISymbol _owningSymbol;
		readonly IOperation _operation;

		public HandleCallAction (in DiagnosticContext diagnosticContext, ISymbol owningSymbol, IOperation operation)
		{
			_owningSymbol = owningSymbol;
			_operation = operation;
			_diagnosticContext = diagnosticContext;
			_requireDynamicallyAccessedMembersAction = new (diagnosticContext, new ReflectionAccessAnalyzer ());
		}

		// TODO: This is relatively expensive on the analyzer since it doesn't cache the annotation information
		// In linker this is an optimization to avoid the heavy lifting of analysis if there's no point
		// it's unclear if the same optimization makes sense for the analyzer.
		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method) => FlowAnnotations.RequiresDataFlowAnalysis (method.Method);

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method) => FlowAnnotations.GetMethodReturnValueAnnotation (method.Method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (method.Method, dynamicallyAccessedMemberTypes);

		private partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter)
			=> new (genericParameter.TypeParameterSymbol);

		private partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (method.Method, dynamicallyAccessedMemberTypes);

		// TODO: Does the analyzer need to do something here?
		private partial void MarkStaticConstructor (TypeProxy type) { }

		private partial string GetContainingSymbolDisplayName () => _operation.FindContainingSymbol (_owningSymbol).GetDisplayName ();
	}
}
