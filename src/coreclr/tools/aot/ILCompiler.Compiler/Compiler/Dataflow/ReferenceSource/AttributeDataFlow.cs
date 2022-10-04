// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Linker.Steps;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	public readonly struct AttributeDataFlow
	{
		readonly LinkContext _context;
		readonly MarkStep _markStep;
		readonly MessageOrigin _origin;

		public AttributeDataFlow (LinkContext context, MarkStep markStep, in MessageOrigin origin)
		{
			_context = context;
			_markStep = markStep;
			_origin = origin;
		}

		public void ProcessAttributeDataflow (MethodDefinition method, IList<CustomAttributeArgument> arguments)
		{
			for (int i = 0; i < method.Parameters.Count; i++) {
				var parameterValue = _context.Annotations.FlowAnnotations.GetMethodParameterValue (method, i);
				if (parameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None) {
					MultiValue value = GetValueForCustomAttributeArgument (arguments[i]);
					var diagnosticContext = new DiagnosticContext (_origin, diagnosticsEnabled: true, _context);
					RequireDynamicallyAccessedMembers (diagnosticContext, value, parameterValue);
				}
			}
		}

		public void ProcessAttributeDataflow (FieldDefinition field, CustomAttributeArgument value)
		{
			MultiValue valueNode = GetValueForCustomAttributeArgument (value);
			var fieldValueCandidate = _context.Annotations.FlowAnnotations.GetFieldValue (field);
			if (fieldValueCandidate is not ValueWithDynamicallyAccessedMembers fieldValue)
				return;

			var diagnosticContext = new DiagnosticContext (_origin, diagnosticsEnabled: true, _context);
			RequireDynamicallyAccessedMembers (diagnosticContext, valueNode, fieldValue);
		}

		MultiValue GetValueForCustomAttributeArgument (CustomAttributeArgument argument)
		{
			if (argument.Type.Name == "Type") {
				if (argument.Value is null)
					return NullValue.Instance;

				TypeDefinition? referencedType = ((TypeReference) argument.Value).ResolveToTypeDefinition (_context);
				return referencedType == null
					? UnknownValue.Instance
					: new SystemTypeValue (referencedType);
			}

			if (argument.Type.MetadataType == MetadataType.String)
				return argument.Value is null ? NullValue.Instance : new KnownStringValue ((string) argument.Value);

			// We shouldn't have gotten a non-null annotation for this from GetParameterAnnotation
			throw new InvalidOperationException ();
		}

		void RequireDynamicallyAccessedMembers (in DiagnosticContext diagnosticContext, in MultiValue value, ValueWithDynamicallyAccessedMembers targetValue)
		{
			var reflectionMarker = new ReflectionMarker (_context, _markStep, enabled: true);
			var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (reflectionMarker, diagnosticContext);
			requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
		}
	}
}