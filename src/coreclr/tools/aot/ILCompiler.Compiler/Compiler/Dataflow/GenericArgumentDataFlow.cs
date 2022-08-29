// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using ILCompiler.DependencyAnalysis;
using ILCompiler.Logging;

using ILLink.Shared.TrimAnalysis;

using Internal.TypeSystem;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

#nullable enable

namespace ILCompiler.Dataflow
{
    public readonly struct GenericArgumentDataFlow
    {
        readonly Logger _logger;
        readonly NodeFactory _factory;
        readonly FlowAnnotations _annotations;
        readonly MessageOrigin _origin;

        public GenericArgumentDataFlow(Logger logger, NodeFactory factory, FlowAnnotations annotations, in MessageOrigin origin)
        {
            _logger = logger;
            _factory = factory;
            _annotations = annotations;
            _origin = origin;
        }

        public DependencyList ProcessGenericArgumentDataFlow(GenericParameterDesc genericParameter, TypeDesc genericArgument)
        {
            var genericParameterValue = _annotations.GetGenericParameterValue(genericParameter);
            Debug.Assert(genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None);

            MultiValue genericArgumentValue = _annotations.GetTypeValueFromGenericArgument(genericArgument);

            var diagnosticContext = new DiagnosticContext(
                _origin,
                _logger.ShouldSuppressAnalysisWarningsForRequires(_origin.MemberDefinition, DiagnosticUtilities.RequiresUnreferencedCodeAttribute),
                _logger);
            return RequireDynamicallyAccessedMembers(diagnosticContext, genericArgumentValue, genericParameterValue, new GenericParameterOrigin(genericParameter));
        }

        DependencyList RequireDynamicallyAccessedMembers(
            in DiagnosticContext diagnosticContext,
            in MultiValue value,
            ValueWithDynamicallyAccessedMembers targetValue,
            Origin memberWithRequirements)
        {
            var reflectionMarker = new ReflectionMarker(_logger, _factory, _annotations, typeHierarchyDataFlow: false, enabled: true);
            var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction(reflectionMarker, diagnosticContext, memberWithRequirements);
            requireDynamicallyAccessedMembersAction.Invoke(value, targetValue);
            return reflectionMarker.Dependencies;
        }
    }
}
