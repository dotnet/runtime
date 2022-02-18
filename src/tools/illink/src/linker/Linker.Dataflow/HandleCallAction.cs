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
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods

		readonly LinkContext _context;
		readonly ReflectionMethodBodyScanner _reflectionMethodBodyScanner;
		readonly ReflectionMethodBodyScanner.AnalysisContext _analysisContext;
		readonly MethodDefinition _callingMethodDefinition;

		public HandleCallAction (
			LinkContext context,
			ReflectionMethodBodyScanner reflectionMethodBodyScanner,
			in ReflectionMethodBodyScanner.AnalysisContext analysisContext,
			MethodDefinition callingMethodDefinition)
		{
			_context = context;
			_reflectionMethodBodyScanner = reflectionMethodBodyScanner;
			_analysisContext = analysisContext;
			_callingMethodDefinition = callingMethodDefinition;
			_diagnosticContext = new DiagnosticContext (analysisContext.Origin, analysisContext.DiagnosticsEnabled, context);
			_requireDynamicallyAccessedMembersAction = new (context, reflectionMethodBodyScanner, analysisContext);
		}

		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method)
			=> _context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (method.Method);

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method)
			=> _context.Annotations.FlowAnnotations.GetReturnParameterAnnotation (method.Method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (ReflectionMethodBodyScanner.ResolveToTypeDefinition (_context, method.Method.ReturnType), method.Method, dynamicallyAccessedMemberTypes);

		private partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter)
			=> new (genericParameter.GenericParameter, _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (genericParameter.GenericParameter));

		private partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (method.Method, dynamicallyAccessedMemberTypes);

		private partial void MarkStaticConstructor (TypeProxy type)
			=> _reflectionMethodBodyScanner.MarkStaticConstructor (_analysisContext, type.Type);

		private partial string GetContainingSymbolDisplayName () => _callingMethodDefinition.GetDisplayName ();
	}
}
