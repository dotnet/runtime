// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILCompiler.Dataflow;
using ILLink.Shared.TypeSystemProxy;
using Internal.TypeSystem;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    internal partial struct RequireDynamicallyAccessedMembersAction
    {
        private readonly ReflectionMarker _reflectionMarker;
        private readonly string _reason;

        public RequireDynamicallyAccessedMembersAction(
            ReflectionMarker reflectionMarker,
            in DiagnosticContext diagnosticContext,
            string reason)
        {
            _reflectionMarker = reflectionMarker;
            _diagnosticContext = diagnosticContext;
            _reason = reason;
        }

        public partial bool TryResolveTypeNameAndMark(string typeName, bool needsAssemblyName, out TypeProxy type)
        {
            if (_reflectionMarker.TryResolveTypeNameAndMark(typeName, _diagnosticContext, needsAssemblyName, _reason, out TypeDesc? foundType))
            {
                if (foundType.HasInstantiation && _reflectionMarker.Annotations.HasGenericParameterAnnotation(foundType))
                {
                    GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(_diagnosticContext, _reflectionMarker, foundType);
                }

                type = new(foundType);
                return true;
            }
            else
            {
                type = default;
                return false;
            }
        }

        private partial void MarkTypeForDynamicallyAccessedMembers(in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
        {
            _reflectionMarker.MarkTypeForDynamicallyAccessedMembers(_diagnosticContext.Origin, type.Type, dynamicallyAccessedMemberTypes, _reason);
        }
    }
}
