// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using ILCompiler.DependencyAnalysis;
using ILCompiler.Logging;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Internal.TypeSystem;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

#nullable enable

namespace ILCompiler.Dataflow
{
    public class ReflectionMarker
    {
        private DependencyList _dependencies = new DependencyList();
        private readonly Logger _logger;
        public NodeFactory Factory { get; }
        public FlowAnnotations Annotations { get; }
        private bool _typeHierarchyDataFlow;
        private bool _enabled;
        public DependencyList Dependencies { get => _dependencies; }

        public ReflectionMarker(Logger logger, NodeFactory factory, FlowAnnotations annotations, bool typeHierarchyDataFlow, bool enabled)
        {
            _logger = logger;
            Factory = factory;
            Annotations = annotations;
            _typeHierarchyDataFlow = typeHierarchyDataFlow;
            _enabled = enabled;
        }

        internal void MarkTypeForDynamicallyAccessedMembers(in MessageOrigin origin, TypeDesc typeDefinition, DynamicallyAccessedMemberTypes requiredMemberTypes, Origin memberWithRequirements, bool declaredOnly = false)
        {
            if (!_enabled)
                return;

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

        internal bool TryResolveTypeNameAndMark(string typeName, in DiagnosticContext diagnosticContext, bool needsAssemblyName, Origin memberWithRequirements, [NotNullWhen(true)] out TypeDesc? type)
        {
            ModuleDesc? callingModule = ((diagnosticContext.Origin.MemberDefinition as MethodDesc)?.OwningType as MetadataType)?.Module;

            // NativeAOT doesn't have a fully capable type name resolver yet
            // Once this is implemented don't forget to wire up marking of type forwards which are used in generic parameters
            if (!ILCompiler.DependencyAnalysis.ReflectionMethodBodyScanner.ResolveType(typeName, callingModule, diagnosticContext.Origin.MemberDefinition!.Context, out TypeDesc foundType, out ModuleDesc referenceModule))
            {
                type = default;
                return false;
            }

            if (_enabled)
            {
                // Also add module metadata in case this reference was through a type forward
                if (Factory.MetadataManager.CanGenerateMetadata(referenceModule.GetGlobalModuleType()))
                    _dependencies.Add(Factory.ModuleMetadata(referenceModule), memberWithRequirements.ToString());

                MarkType(diagnosticContext.Origin, foundType, memberWithRequirements);
            }

            type = foundType;
            return true;
        }

        internal void MarkType(in MessageOrigin origin, TypeDesc type, Origin memberWithRequirements)
        {
            if (!_enabled)
                return;

            RootingHelpers.TryGetDependenciesForReflectedType(ref _dependencies, Factory, type, memberWithRequirements.ToString());
        }

        internal void MarkMethod(in MessageOrigin origin, MethodDesc method, Origin memberWithRequirements)
        {
            if (!_enabled)
                return;

            CheckAndWarnOnReflectionAccess(origin, method, memberWithRequirements);

            RootingHelpers.TryGetDependenciesForReflectedMethod(ref _dependencies, Factory, method, memberWithRequirements.ToString());
        }

        void MarkField(in MessageOrigin origin, FieldDesc field, Origin memberWithRequirements)
        {
            if (!_enabled)
                return;

            CheckAndWarnOnReflectionAccess(origin, field, memberWithRequirements);

            RootingHelpers.TryGetDependenciesForReflectedField(ref _dependencies, Factory, field, memberWithRequirements.ToString());
        }

        internal void MarkProperty(in MessageOrigin origin, PropertyPseudoDesc property, Origin memberWithRequirements)
        {
            if (!_enabled)
                return;

            if (property.GetMethod != null)
                MarkMethod(origin, property.GetMethod, memberWithRequirements);
            if (property.SetMethod != null)
                MarkMethod(origin, property.SetMethod, memberWithRequirements);
        }

        void MarkEvent(in MessageOrigin origin, EventPseudoDesc @event, Origin memberWithRequirements)
        {
            if (!_enabled)
                return;

            if (@event.AddMethod != null)
                MarkMethod(origin, @event.AddMethod, memberWithRequirements);
            if (@event.RemoveMethod != null)
                MarkMethod(origin, @event.RemoveMethod, memberWithRequirements);
        }

        internal void MarkConstructorsOnType(in MessageOrigin origin, TypeDesc type, Func<MethodDesc, bool>? filter, Origin memberWithRequirements, BindingFlags? bindingFlags = null)
        {
            if (!_enabled)
                return;

            foreach (var ctor in type.GetConstructorsOnType(filter, bindingFlags))
                MarkMethod(origin, ctor, memberWithRequirements);
        }

        internal void MarkFieldsOnTypeHierarchy(in MessageOrigin origin, TypeDesc type, Func<FieldDesc, bool> filter, Origin memberWithRequirements, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            if (!_enabled)
                return;

            foreach (var field in type.GetFieldsOnTypeHierarchy(filter, bindingFlags))
                MarkField(origin, field, memberWithRequirements);
        }

        internal void MarkPropertiesOnTypeHierarchy(in MessageOrigin origin, TypeDesc type, Func<PropertyPseudoDesc, bool> filter, Origin memberWithRequirements, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            if (!_enabled)
                return;

            foreach (var property in type.GetPropertiesOnTypeHierarchy(filter, bindingFlags))
                MarkProperty(origin, property, memberWithRequirements);
        }

        internal void MarkEventsOnTypeHierarchy(in MessageOrigin origin, TypeDesc type, Func<EventPseudoDesc, bool> filter, Origin memberWithRequirements, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            if (!_enabled)
                return;

            foreach (var @event in type.GetEventsOnTypeHierarchy(filter, bindingFlags))
                MarkEvent(origin, @event, memberWithRequirements);
        }

        internal void MarkStaticConstructor(in MessageOrigin origin, TypeDesc type)
        {
            if (!_enabled)
                return;

            if (!type.IsGenericDefinition && !type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true) && Factory.PreinitializationManager.HasLazyStaticConstructor(type))
            {
                // Mark the GC static base - it contains a pointer to the class constructor, but also info
                // about whether the class constructor already executed and it's what is looked at at runtime.
                _dependencies.Add(Factory.TypeNonGCStaticsSymbol((MetadataType)type), "RunClassConstructor reference");
            }
        }

        void CheckAndWarnOnReflectionAccess(in MessageOrigin origin, TypeSystemEntity entity, Origin memberWithRequirements)
        {
            if (entity.DoesMemberRequire(DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out CustomAttributeValue<TypeDesc>? requiresAttribute))
            {
                if (_typeHierarchyDataFlow)
                {
                    _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersOnTypeReferencesMemberOnBaseWithRequiresUnreferencedCode,
                        ((TypeOrigin)memberWithRequirements).GetDisplayName(),
                        entity.GetDisplayName(),
                        MessageFormat.FormatRequiresAttributeMessageArg(DiagnosticUtilities.GetRequiresAttributeMessage(requiresAttribute.Value)),
                        MessageFormat.FormatRequiresAttributeUrlArg(DiagnosticUtilities.GetRequiresAttributeUrl(requiresAttribute.Value)));
                }
                else
                {
                    var diagnosticContext = new DiagnosticContext(
                        origin,
                        _logger.ShouldSuppressAnalysisWarningsForRequires(origin.MemberDefinition, DiagnosticUtilities.RequiresUnreferencedCodeAttribute),
                        _logger.ShouldSuppressAnalysisWarningsForRequires(origin.MemberDefinition, DiagnosticUtilities.RequiresDynamicCodeAttribute),
                        _logger.ShouldSuppressAnalysisWarningsForRequires(origin.MemberDefinition, DiagnosticUtilities.RequiresAssemblyFilesAttribute),
                        _logger);

                    string arg1 = MessageFormat.FormatRequiresAttributeMessageArg(DiagnosticUtilities.GetRequiresAttributeMessage(requiresAttribute.Value));
                    string arg2 = MessageFormat.FormatRequiresAttributeUrlArg(DiagnosticUtilities.GetRequiresAttributeUrl(requiresAttribute.Value));

                    diagnosticContext.AddDiagnostic(DiagnosticId.RequiresUnreferencedCode, entity.GetDisplayName(), arg1, arg2);
                }
            }

            if (!Annotations.ShouldWarnWhenAccessedForReflection(entity))
                return;

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
                if (!_logger.ShouldSuppressAnalysisWarningsForRequires(origin.MemberDefinition, DiagnosticUtilities.RequiresUnreferencedCodeAttribute))
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
