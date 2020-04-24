// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem.Interop;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public abstract class ReadyToRunCompilationModuleGroupBase : CompilationModuleGroup
    {
        protected readonly HashSet<EcmaModule> _compilationModuleSet;
        private readonly HashSet<ModuleDesc> _versionBubbleModuleSet;
        private Dictionary<TypeDesc, ModuleToken> _typeRefsInCompilationModuleSet;
        private readonly bool _compileGenericDependenciesFromVersionBubbleModuleSet;
        private readonly bool _isCompositeBuildMode;
        private readonly bool _isInputBubble;
        private readonly ConcurrentDictionary<TypeDesc, bool> _layoutDependsOnOtherModules = new ConcurrentDictionary<TypeDesc, bool>();
        private readonly ConcurrentDictionary<TypeDesc, bool> _layoutDependsOnOtherVersionBubbles = new ConcurrentDictionary<TypeDesc, bool>();
        private readonly ConcurrentDictionary<TypeDesc, bool> _versionsWithTypeCache = new ConcurrentDictionary<TypeDesc, bool>();
        private readonly ConcurrentDictionary<MethodDesc, bool> _versionsWithMethodCache = new ConcurrentDictionary<MethodDesc, bool>();

        public ReadyToRunCompilationModuleGroupBase(
            TypeSystemContext context,
            bool isCompositeBuildMode,
            bool isInputBubble,
            IEnumerable<EcmaModule> compilationModuleSet,
            IEnumerable<ModuleDesc> versionBubbleModuleSet,
            bool compileGenericDependenciesFromVersionBubbleModuleSet)
        {
            _compilationModuleSet = new HashSet<EcmaModule>(compilationModuleSet);
            _isCompositeBuildMode = isCompositeBuildMode;
            _isInputBubble = isInputBubble;

            Debug.Assert(_isCompositeBuildMode || _compilationModuleSet.Count == 1);

            _versionBubbleModuleSet = new HashSet<ModuleDesc>(versionBubbleModuleSet);
            _versionBubbleModuleSet.UnionWith(_compilationModuleSet);

            _compileGenericDependenciesFromVersionBubbleModuleSet = compileGenericDependenciesFromVersionBubbleModuleSet;
        }

        public sealed override bool ContainsType(TypeDesc type)
        {
            return type.GetTypeDefinition() is EcmaType ecmaType && IsModuleInCompilationGroup(ecmaType.EcmaModule);
        }

        private bool IsModuleInCompilationGroup(EcmaModule module)
        {
            return _compilationModuleSet.Contains(module);
        }

        protected bool CompileVersionBubbleGenericsIntoCurrentModule(MethodDesc method)
        {
            if (!_compileGenericDependenciesFromVersionBubbleModuleSet)
                return false;

            if (!VersionsWithMethodBody(method))
                return false;

            if (!method.HasInstantiation && !method.OwningType.HasInstantiation)
                return false;

            return true;
        }

        public virtual bool TypeLayoutDependsOnOtherModules(TypeDesc type)
        {
            return _layoutDependsOnOtherModules.GetOrAdd(type, TypeLayoutDependsOnOtherModulesUncached);
        }

        public virtual bool TypeLayoutDependsOnOtherVersionBubbles(TypeDesc type)
        {
            return _layoutDependsOnOtherVersionBubbles.GetOrAdd(type, TypeLayoutDependsOnOtherVersionBubblesUncached);
        }

        private bool TypeLayoutDependsOnOtherModulesUncached(TypeDesc type)
        {
            if (type.IsObject ||
                type.IsPrimitive ||
                type.IsEnum ||
                type.IsPointer ||
                type.IsFunctionPointer ||
                type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
            {
                return false;
            }

            var defType = (MetadataType)type;

            if (type.BaseType != null)
            {
                if (CompareTypeLayoutForModuleCheck((MetadataType)type.BaseType))
                    return true;
            }

            foreach (FieldDesc field in defType.GetFields())
            {
                if (field.IsStatic)
                    continue;

                TypeDesc fieldType = field.FieldType;

                if (fieldType.IsValueType &&
                    !fieldType.IsPrimitive)
                {
                    if (CompareTypeLayoutForModuleCheck((MetadataType)fieldType))
                    {
                        return true;
                    }
                }
            }

            return false;

            bool CompareTypeLayoutForModuleCheck(MetadataType otherType)
            {
                if (otherType.Module != defType.Module ||
                    TypeLayoutDependsOnOtherModules(otherType))
                {
                    return true;
                }
                return false;
            }
        }

        private bool ModuleInMatchingVersionBubble(ModuleDesc module1, ModuleDesc module2)
        {
            return VersionsWithModule(module1) == VersionsWithModule(module2);
        }

        public bool NeedsAlignmentBetweenBaseTypeAndDerived(MetadataType baseType, MetadataType derivedType)
        {

            if (!ModuleInMatchingVersionBubble(derivedType.Module, baseType.Module) ||
                TypeLayoutDependsOnOtherVersionBubbles(baseType) ||
                LayoutDependsOnTypeVariableInstantiationWhichDependsOnOtherModules(baseType))
            {
                return true;
            }

            return false;

            bool LayoutDependsOnTypeVariableInstantiationWhichDependsOnOtherModules(MetadataType type)
            {
                // Types without instantiations  do not depend on their type variables for layout
                if (!type.HasInstantiation)
                    return false;
                
                // Types that are not instantiated do not depend on their type variables for layout.
                if (type.IsTypeDefinition)
                    return false;

                // If no part of the layout of this type depends on other modules, then there is no need
                // to check for a type variable caused case
                if (!TypeLayoutDependsOnOtherModules(type))
                    return false;

                foreach (var field in type.GetFields())
                {
                    var fieldType = field.FieldType;

                    // non valuetypes are uninteresting for this check
                    if (!fieldType.IsValueType)
                        continue;

                    // As primitive types are considered part of all version bubbles, they are also unconsidered
                    if (fieldType.IsPrimitive)
                        continue;

                    var fieldTypeOnOpenType = field.GetTypicalFieldDefinition().FieldType;

                    if (fieldType == fieldTypeOnOpenType)
                    {
                        // If the field type is the same, it isn't dependent on a type variable
                        continue;
                    }

                    if (TypeLayoutDependsOnOtherModules(fieldType) || 
                        ((MetadataType)fieldType).Module != type.Module)
                    {
                        // The layout of this field depends on other modules.
                        return true;
                    }
                }

                return false;
            }
        }

        private bool TypeLayoutDependsOnOtherVersionBubblesUncached(TypeDesc type)
        {
            if (type.IsObject ||
                type.IsPrimitive ||
                type.IsEnum ||
                type.IsPointer ||
                type.IsFunctionPointer ||
                type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
            {
                return false;
            }

            var defType = (MetadataType)type;

            if ((type.BaseType != null) && !type.BaseType.IsObject)
            {
                if (CompareTypeLayoutForVersionBubble((MetadataType)type.BaseType))
                    return true;

                if (NeedsAlignmentBetweenBaseTypeAndDerived(baseType: (MetadataType)type.BaseType, derivedType: defType))
                    return true;
            }

            foreach (FieldDesc field in defType.GetFields())
            {
                if (field.IsStatic)
                    continue;

                TypeDesc fieldType = field.FieldType;

                if (fieldType.IsValueType &&
                    !fieldType.IsPrimitive)
                {
                    if (CompareTypeLayoutForVersionBubble((MetadataType)fieldType))
                    {
                        return true;
                    }
                }
            }

            return false;

            bool CompareTypeLayoutForVersionBubble(MetadataType otherType)
            {
                if (!ModuleInMatchingVersionBubble(otherType.Module, defType.Module) ||
                    TypeLayoutDependsOnOtherVersionBubbles(otherType))
                {
                    return true;
                }
                return false;
            }
        }

        public sealed override bool VersionsWithModule(ModuleDesc module)
        {
            return _versionBubbleModuleSet.Contains(module);
        }

        public sealed override bool VersionsWithType(TypeDesc typeDesc)
        {
            return typeDesc.GetTypeDefinition() is EcmaType ecmaType &&
                _versionsWithTypeCache.GetOrAdd(typeDesc, ComputeTypeVersionsWithCode);
        }


        public sealed override bool VersionsWithMethodBody(MethodDesc method)
        {
            return _versionsWithMethodCache.GetOrAdd(method, VersionsWithMethodUncached);
        }

        private bool VersionsWithMethodUncached(MethodDesc method)
        {
            if (method.OwningType is MetadataType owningType)
            {
                if (!VersionsWithType(owningType))
                    return false;
            }
            else
                return false;

            if (method == method.GetMethodDefinition())
                return true;

            return ComputeInstantiationVersionsWithCode(method.Instantiation, method);
        }

        public sealed override bool CanInline(MethodDesc callerMethod, MethodDesc calleeMethod)
        {
            // Allow inlining if the caller is within the current version bubble
            // (because otherwise we may not be able to encode its tokens)
            // and if the callee is either in the same version bubble or is marked as non-versionable.
            bool canInline = VersionsWithMethodBody(callerMethod) &&
                (VersionsWithMethodBody(calleeMethod) || calleeMethod.IsNonVersionable());

            return canInline;
        }

        public sealed override bool GeneratesPInvoke(MethodDesc method)
        {
            // PInvokes depend on details of the core library, so for now only compile them if:
            //    1) We're compiling the core library module, or
            //    2) We're compiling any module, and no marshalling is needed
            //
            // TODO Future: consider compiling PInvokes with complex marshalling in version bubble
            // mode when the core library is included in the bubble.

            Debug.Assert(method is EcmaMethod);

            // If the PInvoke is declared on an external module, we can only compile it if
            // that module is part of the version bubble.
            if (!_versionBubbleModuleSet.Contains(((EcmaMethod)method).Module))
                return false;

            if (((EcmaMethod)method).Module.Equals(method.Context.SystemModule))
                return true;

            return !Marshaller.IsMarshallingRequired(method);
        }

        public sealed override bool TryGetModuleTokenForExternalType(TypeDesc type, out ModuleToken token)
        {
            Debug.Assert(!VersionsWithType(type));

            if (_typeRefsInCompilationModuleSet == null)
            {
                _typeRefsInCompilationModuleSet = new Dictionary<TypeDesc, ModuleToken>();

                foreach (var module in _compilationModuleSet)
                {
                    EcmaModule ecmaModule = (EcmaModule)module;
                    foreach (var typeRefHandle in ecmaModule.MetadataReader.TypeReferences)
                    {
                        try
                        {
                            TypeDesc typeFromTypeRef = ecmaModule.GetType(typeRefHandle);
                            if (!_typeRefsInCompilationModuleSet.ContainsKey(typeFromTypeRef))
                            {
                                _typeRefsInCompilationModuleSet.Add(typeFromTypeRef, new ModuleToken(ecmaModule, typeRefHandle));
                            }
                        }
                        catch (TypeSystemException) { }
                    }
                }
            }

            return _typeRefsInCompilationModuleSet.TryGetValue(type, out token);
        }

        public sealed override bool IsCompositeBuildMode => _isCompositeBuildMode;

        public sealed override bool IsInputBubble => _isInputBubble;

        public sealed override IEnumerable<EcmaModule> CompilationModuleSet => _compilationModuleSet;

        private bool ComputeTypeVersionsWithCode(TypeDesc type)
        {
            if (type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
                return true;

            if (type is MetadataType mdType)
            {
                if (!_versionBubbleModuleSet.Contains(mdType.Module))
                    return false;
            }

            if (type == type.GetTypeDefinition())
                return true;

            return ComputeInstantiationVersionsWithCode(type.Instantiation, type);
        }

        private bool ComputeInstantiationVersionsWithCode(Instantiation inst, TypeSystemEntity entityWithInstantiation)
        {
            for (int iInstantiation = 0; iInstantiation < inst.Length; iInstantiation++)
            {
                TypeDesc instType = inst[iInstantiation];

                if (!ComputeInstantiationTypeVersionsWithCode(this, instType))
                {
                    if (instType.IsPrimitive)
                    {
                        // Primitive type instantiations are only instantiated in the module of the generic defining type
                        // if the generic does not apply interface constraints to that type parameter, or if System.Private.CoreLib is part of the version bubble

                        Instantiation entityDefinitionInstantiation;
                        if (entityWithInstantiation is TypeDesc type)
                        {
                            entityDefinitionInstantiation = type.GetTypeDefinition().Instantiation;
                        }
                        else
                        {
                            entityDefinitionInstantiation = ((MethodDesc)entityWithInstantiation).GetTypicalMethodDefinition().Instantiation;
                        }

                        GenericParameterDesc genericParam = (GenericParameterDesc)entityDefinitionInstantiation[iInstantiation];
                        if (genericParam.HasReferenceTypeConstraint)
                            return false;

                        // This checks to see if the type constraints list is empty
                        if (genericParam.TypeConstraints.GetEnumerator().MoveNext())
                            return false;
                    }
                    else
                    {
                        // Non-primitive which doesn't version with type implies instantiation doesn't version with type
                        return false;
                    }
                }
            }
            return true;

            static bool ComputeInstantiationTypeVersionsWithCode(ReadyToRunCompilationModuleGroupBase compilationGroup, TypeDesc type)
            {
                if (type == type.Context.CanonType)
                    return true;

                if (compilationGroup.VersionsWithType(type))
                    return true;

                if (type.IsArray)
                    return ComputeInstantiationTypeVersionsWithCode(compilationGroup, type.GetParameterType());

                if (type.IsPointer)
                    return ComputeInstantiationTypeVersionsWithCode(compilationGroup, type.GetParameterType());

                return false;
            }
        }

        public abstract void ApplyProfilerGuidedCompilationRestriction(ProfileDataManager profileGuidedCompileRestriction);
    }
}
