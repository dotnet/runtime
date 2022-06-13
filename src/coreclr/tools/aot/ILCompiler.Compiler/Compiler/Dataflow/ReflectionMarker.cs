// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ILCompiler.DependencyAnalysis;
using ILCompiler.Logging;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Internal.TypeSystem;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

#nullable enable

namespace ILCompiler.Dataflow
{
    internal class ReflectionMarker
    {
        private DependencyList _dependencies = new DependencyList();
        private readonly Logger _logger;
        private readonly NodeFactory _factory;
        private readonly FlowAnnotations _annotations;
        private bool _typeHierarchyDataFlow;
        private const string RequiresUnreferencedCodeAttribute = nameof(RequiresUnreferencedCodeAttribute);

        public DependencyList Dependencies { get => _dependencies; }

        public ReflectionMarker(Logger logger, NodeFactory factory, FlowAnnotations annotations, bool typeHierarchyDataFlow)
        {
            _logger = logger;
            _factory = factory;
            _annotations = annotations;
            _typeHierarchyDataFlow = typeHierarchyDataFlow;
        }

        internal void MarkTypeForDynamicallyAccessedMembers(in MessageOrigin origin, TypeDesc typeDefinition, DynamicallyAccessedMemberTypes requiredMemberTypes, Origin memberWithRequirements, bool declaredOnly = false)
        {
            foreach (var member in typeDefinition.GetDynamicallyAccessedMembers(requiredMemberTypes, declaredOnly))
            {
                switch (member)
                {
                    case MethodDesc method:
                        MarkMethod(origin, method, memberWithRequirements);
                        break;
                    case FieldDesc field:
                        MarkField(origin, field, memberWithRequirements);
                        break;
                    case MetadataType nestedType:
                        MarkType(origin, nestedType, memberWithRequirements);
                        break;
                    case PropertyPseudoDesc property:
                        MarkProperty(origin, property, memberWithRequirements);
                        break;
                    case EventPseudoDesc @event:
                        MarkEvent(origin, @event, memberWithRequirements);
                        break;
                        // case InterfaceImplementation
                        //  Nothing to do currently as Native AOT will presere all interfaces on a preserved type
                }
            }
        }

        internal bool TryResolveTypeNameAndMark(string typeName, MessageOrigin origin, bool needsAssemblyName, Origin memberWithRequirements, [NotNullWhen(true)] out TypeDesc? type)
        {
            ModuleDesc? callingModule = ((origin.MemberDefinition as MethodDesc)?.OwningType as MetadataType)?.Module;

            if (!ILCompiler.DependencyAnalysis.ReflectionMethodBodyScanner.ResolveType(typeName, callingModule, origin.MemberDefinition.Context, out TypeDesc foundType, out ModuleDesc referenceModule))
            {
                type = default;
                return false;
            }

            // Also add module metadata in case this reference was through a type forward
            if (_factory.MetadataManager.CanGenerateMetadata(referenceModule.GetGlobalModuleType()))
                _dependencies.Add(_factory.ModuleMetadata(referenceModule), memberWithRequirements.ToString());

            MarkType(origin, foundType, memberWithRequirements);

            type = foundType;
            return true;
        }

        internal void MarkType(in MessageOrigin origin, TypeDesc type, Origin memberWithRequirements)
        {
            RootingHelpers.TryGetDependenciesForReflectedType(ref _dependencies, _factory, type, memberWithRequirements.ToString());
        }

        internal void MarkMethod(in MessageOrigin origin, MethodDesc method, Origin memberWithRequirements)
        {
            if (method.DoesMethodRequire(RequiresUnreferencedCodeAttribute, out _))
            {
                if (_typeHierarchyDataFlow)
                {
                    _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersOnTypeReferencesMemberOnBaseWithRequiresUnreferencedCode,
                        ((TypeOrigin)memberWithRequirements).GetDisplayName(), method.GetDisplayName());
                }
            }

            if (_annotations.ShouldWarnWhenAccessedForReflection(method) && !ReflectionMethodBodyScanner.ShouldSuppressAnalysisWarningsForRequires(method, RequiresUnreferencedCodeAttribute))
            {
                WarnOnReflectionAccess(origin, method, memberWithRequirements);
            }

            RootingHelpers.TryGetDependenciesForReflectedMethod(ref _dependencies, _factory, method, memberWithRequirements.ToString());
        }

        void MarkField(in MessageOrigin origin, FieldDesc field, Origin memberWithRequirements)
        {
            if (_annotations.ShouldWarnWhenAccessedForReflection(field) && !ReflectionMethodBodyScanner.ShouldSuppressAnalysisWarningsForRequires(origin.MemberDefinition, RequiresUnreferencedCodeAttribute))
            {
                WarnOnReflectionAccess(origin, field, memberWithRequirements);
            }

            RootingHelpers.TryGetDependenciesForReflectedField(ref _dependencies, _factory, field, memberWithRequirements.ToString());
        }

        internal void MarkProperty(in MessageOrigin origin, PropertyPseudoDesc property, Origin memberWithRequirements)
        {
            if (property.GetMethod != null)
                MarkMethod(origin, property.GetMethod, memberWithRequirements);
            if (property.SetMethod != null)
                MarkMethod(origin, property.SetMethod, memberWithRequirements);
        }

        void MarkEvent(in MessageOrigin origin, EventPseudoDesc @event, Origin memberWithRequirements)
        {
            if (@event.AddMethod != null)
                MarkMethod(origin, @event.AddMethod, memberWithRequirements);
            if (@event.RemoveMethod != null)
                MarkMethod(origin, @event.RemoveMethod, memberWithRequirements);
        }

        internal void MarkConstructorsOnType(in MessageOrigin origin, TypeDesc type, Func<MethodDesc, bool>? filter, Origin memberWithRequirements, BindingFlags? bindingFlags = null)
        {
            foreach (var ctor in type.GetConstructorsOnType(filter, bindingFlags))
                MarkMethod(origin, ctor, memberWithRequirements);
        }

        internal void MarkFieldsOnTypeHierarchy(in MessageOrigin origin, TypeDesc type, Func<FieldDesc, bool> filter, Origin memberWithRequirements, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            foreach (var field in type.GetFieldsOnTypeHierarchy(filter, bindingFlags))
                MarkField(origin, field, memberWithRequirements);
        }

        internal void MarkPropertiesOnTypeHierarchy(in MessageOrigin origin, TypeDesc type, Func<PropertyPseudoDesc, bool> filter, Origin memberWithRequirements, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            foreach (var property in type.GetPropertiesOnTypeHierarchy(filter, bindingFlags))
                MarkProperty(origin, property, memberWithRequirements);
        }

        internal void MarkEventsOnTypeHierarchy(in MessageOrigin origin, TypeDesc type, Func<EventPseudoDesc, bool> filter, Origin memberWithRequirements, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            foreach (var @event in type.GetEventsOnTypeHierarchy(filter, bindingFlags))
                MarkEvent(origin, @event, memberWithRequirements);
        }

        internal void MarkStaticConstructor(in MessageOrigin origin, TypeDesc type)
        {
            if (!type.IsGenericDefinition && !type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true) && type.HasStaticConstructor)
            {
                // Mark the GC static base - it contains a pointer to the class constructor, but also info
                // about whether the class constructor already executed and it's what is looked at at runtime.
                _dependencies.Add(_factory.TypeNonGCStaticsSymbol((MetadataType)type), "RunClassConstructor reference");
            }
        }

        void WarnOnReflectionAccess(in MessageOrigin origin, TypeSystemEntity entity, Origin memberWithRequirements)
        {
            if (_typeHierarchyDataFlow)
            {
                // Don't check whether the current scope is a RUC type or RUC method because these warnings
                // are not suppressed in RUC scopes. Here the scope represents the DynamicallyAccessedMembers
                // annotation on a type, not a callsite which uses the annotation. We always want to warn about
                // possible reflection access indicated by these annotations.
                _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersOnTypeReferencesMemberOnBaseWithDynamicallyAccessedMembers,
                    ((TypeOrigin)memberWithRequirements).GetDisplayName(), entity.GetDisplayName());
            }
            else
            {
                if (!ReflectionMethodBodyScanner.ShouldSuppressAnalysisWarningsForRequires(origin.MemberDefinition, RequiresUnreferencedCodeAttribute))
                {
                    if (entity is FieldDesc)
                    {
                        _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection, entity.GetDisplayName());
                    }
                    else
                    {
                        Debug.Assert(entity is MethodDesc);

                        _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, entity.GetDisplayName());
                    }
                }
            }
        }
    }
}
