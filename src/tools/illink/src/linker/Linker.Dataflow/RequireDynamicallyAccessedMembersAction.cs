// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Dataflow;

namespace ILLink.Shared.TrimAnalysis
{
    internal partial struct RequireDynamicallyAccessedMembersAction
    {
        readonly ITryResolveMetadata _resolver;
        readonly ReflectionMarker _reflectionMarker;

        public RequireDynamicallyAccessedMembersAction(
            ITryResolveMetadata resolver,
            ReflectionMarker reflectionMarker,
            in DiagnosticContext diagnosticContext)
        {
            _resolver = resolver;
            _reflectionMarker = reflectionMarker;
            _diagnosticContext = diagnosticContext;
        }

        public partial bool TryResolveTypeNameAndMark(string typeName, bool needsAssemblyName, out TypeProxy type)
        {
            if (_reflectionMarker.TryResolveTypeNameAndMark(typeName, _diagnosticContext, needsAssemblyName, out TypeReference? foundType))
            {
                if (GenericArgumentDataFlow.RequiresGenericArgumentDataFlow(_reflectionMarker.Context.Annotations.FlowAnnotations, foundType))
                {
                    GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(_diagnosticContext, _reflectionMarker, _reflectionMarker.Context, foundType);
                }
                type = new(foundType, _resolver);
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
            _reflectionMarker.MarkTypeForDynamicallyAccessedMembers(_diagnosticContext.Origin, type.Type, dynamicallyAccessedMemberTypes, DependencyKind.DynamicallyAccessedMember);
        }
    }
}
