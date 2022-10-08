// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using ILCompiler;
using ILCompiler.Dataflow;
using ILLink.Shared.TypeSystemProxy;
using Internal.TypeSystem;

using WellKnownType = ILLink.Shared.TypeSystemProxy.WellKnownType;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    internal partial struct HandleCallAction
    {
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods

        private readonly ReflectionMarker _reflectionMarker;
        private readonly MethodDesc _callingMethod;
        private readonly Origin _memberWithRequirements;

        public HandleCallAction(
            FlowAnnotations annotations,
            ReflectionMarker reflectionMarker,
            in DiagnosticContext diagnosticContext,
            MethodDesc callingMethod,
            Origin memberWithRequirements)
        {
            _reflectionMarker = reflectionMarker;
            _diagnosticContext = diagnosticContext;
            _callingMethod = callingMethod;
            _annotations = annotations;
            _memberWithRequirements = memberWithRequirements;
            _requireDynamicallyAccessedMembersAction = new(reflectionMarker, diagnosticContext, memberWithRequirements);
        }

        private partial bool MethodIsTypeConstructor(MethodProxy method)
        {
            if (!method.Method.IsConstructor)
                return false;
            TypeDesc? type = method.Method.OwningType;
            while (type is not null)
            {
                if (type.IsTypeOf(WellKnownType.System_Type))
                    return true;
                type = type.BaseType;
            }
            return false;
        }

        private partial IEnumerable<SystemReflectionMethodBaseValue> GetMethodsOnTypeHierarchy(TypeProxy type, string name, BindingFlags? bindingFlags)
        {
            foreach (var method in type.Type.GetMethodsOnTypeHierarchy(m => m.Name == name, bindingFlags))
                yield return new SystemReflectionMethodBaseValue(new MethodProxy(method));
        }

        private partial IEnumerable<SystemTypeValue> GetNestedTypesOnType(TypeProxy type, string name, BindingFlags? bindingFlags)
        {
            foreach (var nestedType in type.Type.GetNestedTypesOnType(t => t.Name == name, bindingFlags))
                yield return new SystemTypeValue(new TypeProxy(nestedType));
        }

        private partial bool TryGetBaseType(TypeProxy type, out TypeProxy? baseType)
        {
            if (type.Type.BaseType != null)
            {
                baseType = new TypeProxy(type.Type.BaseType);
                return true;
            }

            baseType = null;
            return false;
        }

        private partial bool TryResolveTypeNameForCreateInstanceAndMark(in MethodProxy calledMethod, string assemblyName, string typeName, out TypeProxy resolvedType)
        {
            // TODO: niche APIs that we probably shouldn't even have added
            // We have to issue a warning, otherwise we could break the app without a warning.
            // This is not the ideal warning, but it's good enough for now.
            _diagnosticContext.AddDiagnostic(DiagnosticId.UnrecognizedParameterInMethodCreateInstance, calledMethod.GetParameterDisplayName(1), calledMethod.GetDisplayName());
            resolvedType = default;
            return false;
        }

        private partial void MarkStaticConstructor(TypeProxy type)
            => _reflectionMarker.MarkStaticConstructor(_diagnosticContext.Origin, type.Type);

        private partial void MarkEventsOnTypeHierarchy(TypeProxy type, string name, BindingFlags? bindingFlags)
            => _reflectionMarker.MarkEventsOnTypeHierarchy(_diagnosticContext.Origin, type.Type, e => e.Name == name, _memberWithRequirements, bindingFlags);

        private partial void MarkFieldsOnTypeHierarchy(TypeProxy type, string name, BindingFlags? bindingFlags)
            => _reflectionMarker.MarkFieldsOnTypeHierarchy(_diagnosticContext.Origin, type.Type, f => f.Name == name, _memberWithRequirements, bindingFlags);

        private partial void MarkPropertiesOnTypeHierarchy(TypeProxy type, string name, BindingFlags? bindingFlags)
            => _reflectionMarker.MarkPropertiesOnTypeHierarchy(_diagnosticContext.Origin, type.Type, p => p.Name == name, _memberWithRequirements, bindingFlags);

        private partial void MarkPublicParameterlessConstructorOnType(TypeProxy type)
            => _reflectionMarker.MarkConstructorsOnType(_diagnosticContext.Origin, type.Type, m => m.IsPublic() && m.Signature.Length == 0, _memberWithRequirements);

        private partial void MarkConstructorsOnType(TypeProxy type, BindingFlags? bindingFlags, int? parameterCount)
            => _reflectionMarker.MarkConstructorsOnType(_diagnosticContext.Origin, type.Type, parameterCount == null ? null : m => m.Signature.Length == parameterCount, _memberWithRequirements, bindingFlags);

        private partial void MarkMethod(MethodProxy method)
            => _reflectionMarker.MarkMethod(_diagnosticContext.Origin, method.Method, _memberWithRequirements);

        private partial void MarkType(TypeProxy type)
            => _reflectionMarker.MarkType(_diagnosticContext.Origin, type.Type, _memberWithRequirements);

        private partial bool MarkAssociatedProperty(MethodProxy method)
        {
            var propertyDefinition = method.Method.GetPropertyForAccessor();
            if (propertyDefinition is null)
            {
                return false;
            }

            _reflectionMarker.MarkProperty(_diagnosticContext.Origin, propertyDefinition, _memberWithRequirements);
            return true;
        }

        private partial string GetContainingSymbolDisplayName() => _callingMethod.GetDisplayName();
    }
}
