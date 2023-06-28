// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

using ILCompiler.DependencyAnalysis;
using ILCompiler.Logging;

using ILLink.Shared.TrimAnalysis;

using Internal.TypeSystem;

using CustomAttributeValue = System.Reflection.Metadata.CustomAttributeValue<Internal.TypeSystem.TypeDesc>;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

#nullable enable

namespace ILCompiler.Dataflow
{
    public readonly struct AttributeDataFlow
    {
        private readonly Logger _logger;
        private readonly NodeFactory _factory;
        private readonly FlowAnnotations _annotations;
        private readonly MessageOrigin _origin;
        private readonly DiagnosticContext _diagnosticContext;

        public AttributeDataFlow(Logger logger, NodeFactory factory, FlowAnnotations annotations, in MessageOrigin origin)
        {
            _annotations = annotations;
            _factory = factory;
            _logger = logger;
            _origin = origin;

            _diagnosticContext = new DiagnosticContext(
                _origin,
                _logger.ShouldSuppressAnalysisWarningsForRequires(_origin.MemberDefinition, DiagnosticUtilities.RequiresUnreferencedCodeAttribute),
                _logger.ShouldSuppressAnalysisWarningsForRequires(_origin.MemberDefinition, DiagnosticUtilities.RequiresDynamicCodeAttribute),
                _logger.ShouldSuppressAnalysisWarningsForRequires(_origin.MemberDefinition, DiagnosticUtilities.RequiresAssemblyFilesAttribute),
                _logger);
        }

        public DependencyList? ProcessAttributeDataflow(MethodDesc method, CustomAttributeValue arguments)
        {
            DependencyList? result = null;

            ReflectionMethodBodyScanner.CheckAndReportAllRequires(_diagnosticContext, method);

            // First do the dataflow for the constructor parameters if necessary.
            if (_annotations.RequiresDataflowAnalysisDueToSignature(method))
            {
                var builder = ImmutableArray.CreateBuilder<object?>(arguments.FixedArguments.Length);
                foreach (var argument in arguments.FixedArguments)
                {
                    builder.Add(argument.Value);
                }

                ProcessAttributeDataflow(method, builder.ToImmutableArray(), ref result);
            }

            // Named arguments next
            TypeDesc attributeType = method.OwningType;
            foreach (var namedArgument in arguments.NamedArguments)
            {
                if (namedArgument.Kind == CustomAttributeNamedArgumentKind.Field)
                {
                    FieldDesc field = attributeType.GetField(namedArgument.Name);
                    if (field != null)
                    {
                        ReflectionMethodBodyScanner.CheckAndReportAllRequires(_diagnosticContext, field);

                        ProcessAttributeDataflow(field, namedArgument.Value, ref result);
                    }
                }
                else
                {
                    Debug.Assert(namedArgument.Kind == CustomAttributeNamedArgumentKind.Property);
                    PropertyPseudoDesc property = ((MetadataType)attributeType).GetProperty(namedArgument.Name, null);
                    MethodDesc setter = property.SetMethod;
                    if (setter != null && setter.Signature.Length > 0 && !setter.Signature.IsStatic)
                    {
                        ReflectionMethodBodyScanner.CheckAndReportAllRequires(_diagnosticContext, setter);

                        ProcessAttributeDataflow(setter, ImmutableArray.Create(namedArgument.Value), ref result);
                    }
                }
            }

            return result;
        }

        private void ProcessAttributeDataflow(MethodDesc method, ImmutableArray<object?> arguments, ref DependencyList? result)
        {
            foreach (var parameter in method.GetMetadataParameters())
            {
                var parameterValue = _annotations.GetMethodParameterValue(parameter);
                if (parameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
                {
                    MultiValue value = GetValueForCustomAttributeArgument(arguments[parameter.MetadataIndex]);
                    RequireDynamicallyAccessedMembers(_diagnosticContext, value, parameterValue, method.GetDisplayName(), ref result);
                }
            }
        }

        private void ProcessAttributeDataflow(FieldDesc field, object? value, ref DependencyList? result)
        {
            var fieldValueCandidate = _annotations.GetFieldValue(field);
            if (fieldValueCandidate is ValueWithDynamicallyAccessedMembers fieldValue
                && fieldValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
            {
                MultiValue valueNode = GetValueForCustomAttributeArgument(value);
                RequireDynamicallyAccessedMembers(_diagnosticContext, valueNode, fieldValue, field.GetDisplayName(), ref result);
            }
        }

        private static MultiValue GetValueForCustomAttributeArgument(object? argument)
            => argument switch
            {
                TypeDesc td => new SystemTypeValue(td),
                string str => new KnownStringValue(str),
                null => NullValue.Instance,
                // We shouldn't have gotten a None annotation from flow annotations since only string/Type can have annotations
                _ => throw new InvalidOperationException()
            };

        private void RequireDynamicallyAccessedMembers(
            in DiagnosticContext diagnosticContext,
            in MultiValue value,
            ValueWithDynamicallyAccessedMembers targetValue,
            string reason,
            ref DependencyList? result)
        {
            var reflectionMarker = new ReflectionMarker(_logger, _factory, _annotations, typeHierarchyDataFlowOrigin: null, enabled: true);
            var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction(reflectionMarker, diagnosticContext, reason);
            requireDynamicallyAccessedMembersAction.Invoke(value, targetValue);

            if (result == null)
            {
                result = reflectionMarker.Dependencies;
            }
            else
            {
                result.AddRange(reflectionMarker.Dependencies);
            }
        }
    }
}
