// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Dataflow;

namespace ILLink.Shared.TrimAnalysis
{
    partial struct RequireDynamicallyAccessedMembersAction
    {
        readonly ReflectionMarker _reflectionMarker;

        public RequireDynamicallyAccessedMembersAction(
            ReflectionMarker reflectionMarker,
            in DiagnosticContext diagnosticContext)
        {
            _reflectionMarker = reflectionMarker;
            _diagnosticContext = diagnosticContext;
        }

        public partial bool TryResolveTypeNameAndMark(string typeName, bool needsAssemblyName, out TypeProxy type)
        {
            if (_reflectionMarker.TryResolveTypeNameAndMark(typeName, _diagnosticContext, needsAssemblyName, out TypeDefinition? foundType))
            {
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
            _reflectionMarker.MarkTypeForDynamicallyAccessedMembers(_diagnosticContext.Origin, type.Type, dynamicallyAccessedMemberTypes, DependencyKind.DynamicallyAccessedMember);
        }
    }
}
