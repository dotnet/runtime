// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Linker.Steps;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	public readonly struct GenericArgumentDataFlow
	{
		readonly LinkContext _context;
		readonly MarkStep _markStep;
		readonly MessageOrigin _origin;

		public GenericArgumentDataFlow (LinkContext context, MarkStep markStep, in MessageOrigin origin)
		{
			_context = context;
			_markStep = markStep;
			_origin = origin;
		}

		public void ProcessGenericArgumentDataFlow (GenericParameter genericParameter, TypeReference genericArgument)
		{
			var genericParameterValue = _context.Annotations.FlowAnnotations.GetGenericParameterValue (genericParameter);
			Debug.Assert (genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None);

			MultiValue genericArgumentValue = _context.Annotations.FlowAnnotations.GetTypeValueFromGenericArgument (genericArgument);

			var diagnosticContext = new DiagnosticContext (_origin, !_context.Annotations.ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode (_origin.Provider), _context);
			RequireDynamicallyAccessedMembers (diagnosticContext, genericArgumentValue, genericParameterValue);
		}

		void RequireDynamicallyAccessedMembers (in DiagnosticContext diagnosticContext, in MultiValue value, ValueWithDynamicallyAccessedMembers targetValue)
		{
			var reflectionMarker = new ReflectionMarker (_context, _markStep, enabled: true);
			var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (reflectionMarker, diagnosticContext);
			requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
		}
	}
}