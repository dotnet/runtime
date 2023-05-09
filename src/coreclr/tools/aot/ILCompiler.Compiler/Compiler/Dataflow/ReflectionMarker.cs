// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
#pragma warning disable IDE0060

namespace ILCompiler.Dataflow
{
    public class ReflectionMarker
    {
        private DependencyList _dependencies = new DependencyList();
        private readonly Logger _logger;
        private readonly MetadataType? _typeHierarchyDataFlowOrigin;
        private readonly bool _enabled;

        public NodeFactory Factory { get; }
        public FlowAnnotations Annotations { get; }
        public DependencyList Dependencies { get => _dependencies; }

        internal enum AccessKind
        {
            Unspecified,
            DynamicallyAccessedMembersMark,
            TokenAccess
        }

        public ReflectionMarker(Logger logger, NodeFactory factory, FlowAnnotations annotations, MetadataType? typeHierarchyDataFlowOrigin, bool enabled)
        {
            _logger = logger;
            Factory = factory;
            Annotations = annotations;
            _typeHierarchyDataFlowOrigin = typeHierarchyDataFlowOrigin;
            _enabled = enabled;
        }

        internal void MarkTypeForDynamicallyAccessedMembers(in MessageOrigin origin, TypeDesc typeDefinition, DynamicallyAccessedMemberTypes requiredMemberTypes, string reason, bool declaredOnly = false)
        {
            if (!_enabled)
                return;

            foreach (var member in typeDefinition.GetDynamicallyAccessedMembers(requiredMemberTypes, declaredOnly))
            {
                MarkTypeSystemEntity(origin, member, reason, AccessKind.DynamicallyAccessedMembersMark);
            }
        }

        internal void MarkTypeSystemEntity(in MessageOrigin origin, TypeSystemEntity entity, string reason, AccessKind accessKind = AccessKind.Unspecified)
        {
            switch (entity)
            {
                case MethodDesc method:
                    MarkMethod(origin, method, reason, accessKind);
                    break;
                case FieldDesc field:
                    MarkField(origin, field, reason, accessKind);
                    break;
                case MetadataType nestedType:
                    MarkType(origin, nestedType, reason, accessKind);
                    break;
                case PropertyPseudoDesc property:
                    MarkProperty(origin, property, reason, accessKind);
                    break;
                case EventPseudoDesc @event:
                    MarkEvent(origin, @event, reason, accessKind);
                    break;
                    // case InterfaceImplementation
                    //  Nothing to do currently as Native AOT will preserve all interfaces on a preserved type
            }
        }

        internal bool TryResolveTypeNameAndMark(string typeName, in DiagnosticContext diagnosticContext, bool needsAssemblyName, string reason, [NotNullWhen(true)] out TypeDesc? type)
        {
            ModuleDesc? callingModule = ((diagnosticContext.Origin.MemberDefinition as MethodDesc)?.OwningType as MetadataType)?.Module;

            List<ModuleDesc> referencedModules = new();
            TypeDesc foundType = System.Reflection.TypeNameParser.ResolveType(typeName, callingModule, diagnosticContext.Origin.MemberDefinition!.Context,
                referencedModules, out bool typeWasNotFoundInAssemblyNorBaseLibrary);
            if (foundType == null)
            {
                if (needsAssemblyName && typeWasNotFoundInAssemblyNorBaseLibrary)
                    diagnosticContext.AddDiagnostic(DiagnosticId.TypeWasNotFoundInAssemblyNorBaseLibrary, typeName);

                type = default;
                return false;
            }

            if (_enabled)
            {
                foreach (ModuleDesc referencedModule in referencedModules)
                {
                    // Also add module metadata in case this reference was through a type forward
                    if (Factory.MetadataManager.CanGenerateMetadata(referencedModule.GetGlobalModuleType()))
                        _dependencies.Add(Factory.ModuleMetadata(referencedModule), reason);
                }

                MarkType(diagnosticContext.Origin, foundType, reason);
            }

            type = foundType;
            return true;
        }

        internal void MarkType(in MessageOrigin origin, TypeDesc type, string reason, AccessKind accessKind = AccessKind.Unspecified)
        {
            if (!_enabled)
                return;

            RootingHelpers.TryGetDependenciesForReflectedType(ref _dependencies, Factory, type, reason);
        }

        internal void MarkMethod(in MessageOrigin origin, MethodDesc method, string reason, AccessKind accessKind = AccessKind.Unspecified)
        {
            if (!_enabled)
                return;

            CheckAndWarnOnReflectionAccess(origin, method, accessKind);

            RootingHelpers.TryGetDependenciesForReflectedMethod(ref _dependencies, Factory, method, reason);
        }

        internal void MarkField(in MessageOrigin origin, FieldDesc field, string reason, AccessKind accessKind = AccessKind.Unspecified)
        {
            if (!_enabled)
                return;

            CheckAndWarnOnReflectionAccess(origin, field, accessKind);

            RootingHelpers.TryGetDependenciesForReflectedField(ref _dependencies, Factory, field, reason);
        }

        internal void MarkProperty(in MessageOrigin origin, PropertyPseudoDesc property, string reason, AccessKind accessKind = AccessKind.Unspecified)
        {
            if (!_enabled)
                return;

            if (property.GetMethod != null)
                MarkMethod(origin, property.GetMethod, reason);
            if (property.SetMethod != null)
                MarkMethod(origin, property.SetMethod, reason);
        }

        private void MarkEvent(in MessageOrigin origin, EventPseudoDesc @event, string reason, AccessKind accessKind = AccessKind.Unspecified)
        {
            if (!_enabled)
                return;

            if (@event.AddMethod != null)
                MarkMethod(origin, @event.AddMethod, reason);
            if (@event.RemoveMethod != null)
                MarkMethod(origin, @event.RemoveMethod, reason);
        }

        internal void MarkConstructorsOnType(in MessageOrigin origin, TypeDesc type, Func<MethodDesc, bool>? filter, string reason, BindingFlags? bindingFlags = null)
        {
            if (!_enabled)
                return;

            foreach (var ctor in type.GetConstructorsOnType(filter, bindingFlags))
                MarkMethod(origin, ctor, reason);
        }

        internal void MarkFieldsOnTypeHierarchy(in MessageOrigin origin, TypeDesc type, Func<FieldDesc, bool> filter, string reason, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            if (!_enabled)
                return;

            foreach (var field in type.GetFieldsOnTypeHierarchy(filter, bindingFlags))
                MarkField(origin, field, reason);
        }

        internal void MarkPropertiesOnTypeHierarchy(in MessageOrigin origin, TypeDesc type, Func<PropertyPseudoDesc, bool> filter, string reason, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            if (!_enabled)
                return;

            foreach (var property in type.GetPropertiesOnTypeHierarchy(filter, bindingFlags))
                MarkProperty(origin, property, reason);
        }

        internal void MarkEventsOnTypeHierarchy(in MessageOrigin origin, TypeDesc type, Func<EventPseudoDesc, bool> filter, string reason, BindingFlags? bindingFlags = BindingFlags.Default)
        {
            if (!_enabled)
                return;

            foreach (var @event in type.GetEventsOnTypeHierarchy(filter, bindingFlags))
                MarkEvent(origin, @event, reason);
        }

        internal void MarkStaticConstructor(in MessageOrigin origin, TypeDesc type, string reason)
        {
            if (!_enabled)
                return;

            if (type.HasStaticConstructor)
                CheckAndWarnOnReflectionAccess(origin, type.GetStaticConstructor());

            if (!type.IsGenericDefinition && !type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true) && Factory.PreinitializationManager.HasLazyStaticConstructor(type))
            {
                // Mark the GC static base - it contains a pointer to the class constructor, but also info
                // about whether the class constructor already executed and it's what is looked at at runtime.
                _dependencies.Add(Factory.TypeNonGCStaticsSymbol((MetadataType)type), "RunClassConstructor reference");
            }
        }

        internal void CheckAndWarnOnReflectionAccess(in MessageOrigin origin, TypeSystemEntity entity, AccessKind accessKind = AccessKind.Unspecified)
        {
            if (!_enabled)
                return;

            if (_typeHierarchyDataFlowOrigin is not null)
            {
                ReportWarningsForTypeHierarchyReflectionAccess(origin, entity);
            }
            else
            {
                ReportWarningsForReflectionAccess(origin, entity, accessKind);
            }
        }

        private void ReportWarningsForReflectionAccess(in MessageOrigin origin, TypeSystemEntity entity, AccessKind accessKind)
        {
            Debug.Assert(entity is MethodDesc or FieldDesc);

            // Note that we're using `ShouldSuppressAnalysisWarningsForRequires` instead of `DoesMemberRequire`.
            // This is because reflection access is actually problematic on all members which are in a "requires" scope
            // so for example even instance methods. See for example https://github.com/dotnet/linker/issues/3140 - it's possible
            // to call a method on a "null" instance via reflection.
            if (_logger.ShouldSuppressAnalysisWarningsForRequires(entity, DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out CustomAttributeValue<TypeDesc>? requiresAttribute))
            {
                if (!ShouldSkipWarningsForOverride(entity, accessKind))
                    ReportRequires(origin, entity, DiagnosticUtilities.RequiresUnreferencedCodeAttribute, requiresAttribute.Value);
            }

            if (_logger.ShouldSuppressAnalysisWarningsForRequires(entity, DiagnosticUtilities.RequiresAssemblyFilesAttribute, out requiresAttribute))
            {
                if (!ShouldSkipWarningsForOverride(entity, accessKind))
                    ReportRequires(origin, entity, DiagnosticUtilities.RequiresAssemblyFilesAttribute, requiresAttribute.Value);
            }

            if (_logger.ShouldSuppressAnalysisWarningsForRequires(entity, DiagnosticUtilities.RequiresDynamicCodeAttribute, out requiresAttribute))
            {
                if (!ShouldSkipWarningsForOverride(entity, accessKind))
                    ReportRequires(origin, entity, DiagnosticUtilities.RequiresDynamicCodeAttribute, requiresAttribute.Value);
            }

            // Below is about accessing DAM annotated members, so only RUC is applicable as a suppression scope
            if (_logger.ShouldSuppressAnalysisWarningsForRequires(origin.MemberDefinition, DiagnosticUtilities.RequiresUnreferencedCodeAttribute))
                return;

            bool isReflectionAccessCoveredByDAM = Annotations.ShouldWarnWhenAccessedForReflection(entity);
            if (isReflectionAccessCoveredByDAM && !ShouldSkipWarningsForOverride(entity, accessKind))
            {
                if (entity is MethodDesc)
                    _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, entity.GetDisplayName());
                else
                    _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection, entity.GetDisplayName());
            }

            // We decided to not warn on reflection access to compiler-generated methods:
            // https://github.com/dotnet/runtime/issues/85042

            // All override methods should have the same annotations as their base methods
            // (else we will produce warning IL2046 or IL2092 or some other warning).
            // When marking override methods via DynamicallyAccessedMembers, we should only issue a warning for the base method.
            // PERF: Avoid precomputing this as this method is relatively expensive. Only call it once we're about to produce a warning.
            static bool ShouldSkipWarningsForOverride(TypeSystemEntity entity, AccessKind accessKind)
            {
                if (accessKind != AccessKind.DynamicallyAccessedMembersMark || entity is not MethodDesc method || !method.IsVirtual)
                    return false;

                return MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(method) != method;
            }
        }

        private void ReportWarningsForTypeHierarchyReflectionAccess(MessageOrigin origin, TypeSystemEntity entity)
        {
            Debug.Assert(entity is MethodDesc or FieldDesc);

            // Don't check whether the current scope is a RUC type or RUC method because these warnings
            // are not suppressed in RUC scopes. Here the scope represents the DynamicallyAccessedMembers
            // annotation on a type, not a callsite which uses the annotation. We always want to warn about
            // possible reflection access indicated by these annotations.

            Debug.Assert(_typeHierarchyDataFlowOrigin != null);

            static bool IsDeclaredWithinType(TypeSystemEntity member, TypeDesc type)
            {
                TypeDesc owningType = member.GetOwningType();
                while (owningType != null)
                {
                    if (owningType == type)
                        return true;

                    owningType = owningType.GetOwningType();
                }
                return false;
            }

            var reportOnMember = IsDeclaredWithinType(entity, _typeHierarchyDataFlowOrigin);
            if (reportOnMember)
                origin = new MessageOrigin(entity);

            // For now we decided to not report single-file or dynamic-code warnings due to type hierarchy marking.
            // It is considered too complex to figure out for the user and the likelihood of this
            // causing problems is pretty low.

            bool isReflectionAccessCoveredByRUC = _logger.ShouldSuppressAnalysisWarningsForRequires(entity, DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out CustomAttributeValue<TypeDesc>? requiresUnreferencedCodeAttribute);
            if (isReflectionAccessCoveredByRUC && !ShouldSkipWarningsForOverride(entity))
            {
                var id = reportOnMember ? DiagnosticId.DynamicallyAccessedMembersOnTypeReferencesMemberWithRequiresUnreferencedCode : DiagnosticId.DynamicallyAccessedMembersOnTypeReferencesMemberOnBaseWithRequiresUnreferencedCode;
                _logger.LogWarning(origin, id, _typeHierarchyDataFlowOrigin.GetDisplayName(),
                entity.GetDisplayName(),
                    MessageFormat.FormatRequiresAttributeMessageArg(DiagnosticUtilities.GetRequiresAttributeMessage(requiresUnreferencedCodeAttribute!.Value)),
                    MessageFormat.FormatRequiresAttributeMessageArg(DiagnosticUtilities.GetRequiresAttributeUrl(requiresUnreferencedCodeAttribute!.Value)));
            }

            bool isReflectionAccessCoveredByDAM = Annotations.ShouldWarnWhenAccessedForReflection(entity);
            if (isReflectionAccessCoveredByDAM && !ShouldSkipWarningsForOverride(entity))
            {
                var id = reportOnMember ? DiagnosticId.DynamicallyAccessedMembersOnTypeReferencesMemberWithDynamicallyAccessedMembers : DiagnosticId.DynamicallyAccessedMembersOnTypeReferencesMemberOnBaseWithDynamicallyAccessedMembers;
                _logger.LogWarning(origin, id, _typeHierarchyDataFlowOrigin.GetDisplayName(), entity.GetDisplayName());
            }

            // We decided to not warn on reflection access to compiler-generated methods:
            // https://github.com/dotnet/runtime/issues/85042

            // All override methods should have the same annotations as their base methods
            // (else we will produce warning IL2046 or IL2092 or some other warning).
            // When marking override methods via DynamicallyAccessedMembers, we should only issue a warning for the base method.
            static bool ShouldSkipWarningsForOverride(TypeSystemEntity entity)
            {
                if (entity is not MethodDesc method || !method.IsVirtual)
                    return false;

                return MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(method) != method;
            }
        }

        private void ReportRequires(in MessageOrigin origin, TypeSystemEntity entity, string requiresAttributeName, in CustomAttributeValue<TypeDesc> requiresAttribute)
        {
            var diagnosticContext = new DiagnosticContext(
                origin,
                _logger.ShouldSuppressAnalysisWarningsForRequires(origin.MemberDefinition, DiagnosticUtilities.RequiresUnreferencedCodeAttribute),
                _logger.ShouldSuppressAnalysisWarningsForRequires(origin.MemberDefinition, DiagnosticUtilities.RequiresDynamicCodeAttribute),
                _logger.ShouldSuppressAnalysisWarningsForRequires(origin.MemberDefinition, DiagnosticUtilities.RequiresAssemblyFilesAttribute),
                _logger);

            ReflectionMethodBodyScanner.ReportRequires(diagnosticContext, entity, requiresAttributeName, requiresAttribute);
        }
    }
}
