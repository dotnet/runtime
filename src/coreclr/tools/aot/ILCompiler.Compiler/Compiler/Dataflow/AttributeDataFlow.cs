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
        readonly Logger _logger;
        readonly NodeFactory _factory;
        readonly FlowAnnotations _annotations;
        readonly MessageOrigin _origin;

        public AttributeDataFlow(Logger logger, NodeFactory factory, FlowAnnotations annotations, in MessageOrigin origin)
        {
            _annotations = annotations;
            _factory = factory;
            _logger = logger;
            _origin = origin;
        }

        public DependencyList? ProcessAttributeDataflow(MethodDesc method, CustomAttributeValue arguments)
        {
            DependencyList? result = null;

            // First do the dataflow for the constructor parameters if necessary.
            if (_annotations.RequiresDataflowAnalysis(method))
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
                        ProcessAttributeDataflow(setter, ImmutableArray.Create(namedArgument.Value), ref result);
                    }
                }
            }

            return result;
        }

        void ProcessAttributeDataflow(MethodDesc method, ImmutableArray<object?> arguments, ref DependencyList? result)
        {
            for (int i = 0; i < method.Signature.Length; i++)
            {
                var parameterValue = _annotations.GetMethodParameterValue(method, i);
                if (parameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
                {
                    MultiValue value = GetValueForCustomAttributeArgument(arguments[i]);
                    var diagnosticContext = new DiagnosticContext(_origin, diagnosticsEnabled: true, _logger);
                    RequireDynamicallyAccessedMembers(diagnosticContext, value, parameterValue, parameterValue.ParameterOrigin, ref result);
                }
            }
        }

        public void ProcessAttributeDataflow(FieldDesc field, object? value, ref DependencyList? result)
        {
            var fieldValueCandidate = _annotations.GetFieldValue(field);
            if (fieldValueCandidate is ValueWithDynamicallyAccessedMembers fieldValue
                && fieldValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
            {
                MultiValue valueNode = GetValueForCustomAttributeArgument(value);
                var diagnosticContext = new DiagnosticContext(_origin, diagnosticsEnabled: true, _logger);
                RequireDynamicallyAccessedMembers(diagnosticContext, valueNode, fieldValue, new FieldOrigin(field), ref result);
            }
        }

        MultiValue GetValueForCustomAttributeArgument(object? argument)
            => argument switch
            {
                TypeDesc td => new SystemTypeValue(td),
                string str => new KnownStringValue(str),
                null => NullValue.Instance,
                // We shouldn't have gotten a None annotation from flow annotations since only string/Type can have annotations
                _ => throw new InvalidOperationException()
            };

        void RequireDynamicallyAccessedMembers(
            in DiagnosticContext diagnosticContext,
            in MultiValue value,
            ValueWithDynamicallyAccessedMembers targetValue,
            Origin memberWithRequirements,
            ref DependencyList? result)
        {
            var reflectionMarker = new ReflectionMarker(_logger, _factory, _annotations, typeHierarchyDataFlow: false, enabled: true);
            var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction(reflectionMarker, diagnosticContext, memberWithRequirements);
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
