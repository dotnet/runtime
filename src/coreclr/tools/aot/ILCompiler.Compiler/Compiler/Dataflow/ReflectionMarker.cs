// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private Origin _memberWithRequirements;

        public DependencyList Dependencies { get => _dependencies; }

        public ReflectionMarker(Logger logger, NodeFactory factory, FlowAnnotations annotations, bool typeHierarchyDataFlow, Origin memberWithRequirements)
        {
            _logger = logger;
            _factory = factory;
            _annotations = annotations;
            _typeHierarchyDataFlow = typeHierarchyDataFlow;
            _memberWithRequirements = memberWithRequirements;
        }

        internal void MarkTypeForDynamicallyAccessedMembers (in MessageOrigin origin, TypeDesc typeDefinition, DynamicallyAccessedMemberTypes requiredMemberTypes, bool declaredOnly = false)
		{
			foreach (var member in typeDefinition.GetDynamicallyAccessedMembers (requiredMemberTypes, declaredOnly)) {
				switch (member) {
				case MethodDesc method:
					MarkMethod (origin, method);
					break;
				case FieldDesc field:
					MarkField (origin, field);
					break;
				case MetadataType nestedType:
					MarkType (origin, nestedType);
					break;
				case PropertyPseudoDesc property:
					MarkProperty (origin, property);
					break;
				case EventPseudoDesc @event:
					MarkEvent (origin, @event);
					break;
				//case InterfaceImplementation interfaceImplementation:
				//	MarkInterfaceImplementation (origin, interfaceImplementation, dependencyKind);
				//	break;
				}
			}
		}

		internal bool TryResolveTypeNameAndMark (string typeName, MessageOrigin origin, bool needsAssemblyName, [NotNullWhen (true)] out TypeDesc? type)
		{
            ModuleDesc? callingModule = ((origin.MemberDefinition as MethodDesc)?.OwningType as MetadataType)?.Module;

            if (!ILCompiler.DependencyAnalysis.ReflectionMethodBodyScanner.ResolveType(typeName, callingModule, origin.MemberDefinition.Context, out TypeDesc foundType, out ModuleDesc referenceModule)) {
                type = default;
				return false;
			}

            // Also add module metadata in case this reference was through a type forward
            if (_factory.MetadataManager.CanGenerateMetadata(referenceModule.GetGlobalModuleType()))
                _dependencies.Add(_factory.ModuleMetadata(referenceModule), _memberWithRequirements.ToString());

            MarkType(origin, foundType);

            type = foundType;
			return true;
		}

		internal void MarkType (in MessageOrigin origin, TypeDesc type)
		{
            RootingHelpers.TryGetDependenciesForReflectedType(ref _dependencies, _factory, type, _memberWithRequirements.ToString());
		}

		internal void MarkMethod (in MessageOrigin origin, MethodDesc method)
		{
            if (method.HasCustomAttribute("System.Diagnostics.CodeAnalysis", "RequiresUnreferencedCodeAttribute"))
            {
                if (_typeHierarchyDataFlow)
                {
                    _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersOnTypeReferencesMemberOnBaseWithRequiresUnreferencedCode,
                        ((TypeOrigin)_memberWithRequirements).GetDisplayName(), method.GetDisplayName());
                }
            }

            if (_annotations.ShouldWarnWhenAccessedForReflection(method))
            {
                WarnOnReflectionAccess(origin, method);
            }

            RootingHelpers.TryGetDependenciesForReflectedMethod(ref _dependencies, _factory, method, _memberWithRequirements.ToString());
		}

		void MarkField (in MessageOrigin origin, FieldDesc field)
		{
            if (_annotations.ShouldWarnWhenAccessedForReflection(field))
            {
                WarnOnReflectionAccess(origin, field);
            }

            RootingHelpers.TryGetDependenciesForReflectedField(ref _dependencies, _factory, field, _memberWithRequirements.ToString());
		}

		internal void MarkProperty (in MessageOrigin origin, PropertyPseudoDesc property)
		{
            if (property.GetMethod != null)
                MarkMethod(origin, property.GetMethod);
            if (property.SetMethod != null)
                MarkMethod(origin, property.SetMethod);
		}

		void MarkEvent (in MessageOrigin origin, EventPseudoDesc @event)
		{
            if (@event.AddMethod != null)
                MarkMethod(origin, @event.AddMethod);
            if (@event.RemoveMethod != null)
                MarkMethod(origin, @event.RemoveMethod);
		}

		//void MarkInterfaceImplementation (in MessageOrigin origin, InterfaceImplementation interfaceImplementation, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		//{
		//	_markStep.MarkInterfaceImplementation (interfaceImplementation, null, new DependencyInfo (dependencyKind, origin.Provider));
		//}

		internal void MarkConstructorsOnType (in MessageOrigin origin, TypeDesc type, Func<MethodDesc, bool>? filter, BindingFlags? bindingFlags = null)
		{
			foreach (var ctor in type.GetConstructorsOnType (filter, bindingFlags))
				MarkMethod (origin, ctor);
		}

		internal void MarkFieldsOnTypeHierarchy (in MessageOrigin origin, TypeDesc type, Func<FieldDesc, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var field in type.GetFieldsOnTypeHierarchy (filter, bindingFlags))
				MarkField (origin, field);
		}

		internal void MarkPropertiesOnTypeHierarchy (in MessageOrigin origin, TypeDesc type, Func<PropertyPseudoDesc, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var property in type.GetPropertiesOnTypeHierarchy (filter, bindingFlags))
				MarkProperty (origin, property);
		}

		internal void MarkEventsOnTypeHierarchy (in MessageOrigin origin, TypeDesc type, Func<EventPseudoDesc, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var @event in type.GetEventsOnTypeHierarchy (filter, bindingFlags))
				MarkEvent (origin, @event);
		}

		//internal void MarkStaticConstructor (in MessageOrigin origin, TypeDefinition type)
		//{
		//	_markStep.MarkStaticConstructorVisibleToReflection (type, new DependencyInfo (DependencyKind.AccessedViaReflection, origin.Provider), origin);
		//}

        void WarnOnReflectionAccess(in MessageOrigin origin, TypeSystemEntity entity)
        {
            if (_typeHierarchyDataFlow)
            {
                // Don't check whether the current scope is a RUC type or RUC method because these warnings
                // are not suppressed in RUC scopes. Here the scope represents the DynamicallyAccessedMembers
                // annotation on a type, not a callsite which uses the annotation. We always want to warn about
                // possible reflection access indicated by these annotations.
                _logger.LogWarning(origin, DiagnosticId.DynamicallyAccessedMembersOnTypeReferencesMemberOnBaseWithDynamicallyAccessedMembers,
                    ((TypeOrigin)_memberWithRequirements).GetDisplayName(), entity.GetDisplayName());
            }
            else
            {
                if (entity is FieldDesc && ReflectionMethodBodyScanner.ShouldEnableReflectionPatternReporting(origin.MemberDefinition))
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
