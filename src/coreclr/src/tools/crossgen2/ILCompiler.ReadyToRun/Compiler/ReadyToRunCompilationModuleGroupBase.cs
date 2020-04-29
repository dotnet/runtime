// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
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
        private readonly ConcurrentDictionary<TypeDesc, CompilationUnitSet> _layoutCompilationUnits = new ConcurrentDictionary<TypeDesc, CompilationUnitSet>();
        private readonly ConcurrentDictionary<TypeDesc, bool> _versionsWithTypeCache = new ConcurrentDictionary<TypeDesc, bool>();
        private readonly ConcurrentDictionary<MethodDesc, bool> _versionsWithMethodCache = new ConcurrentDictionary<MethodDesc, bool>();
        private readonly Dictionary<ModuleDesc, int> _moduleCompilationUnits = new Dictionary<ModuleDesc, int>();
        private int _maxCompilationUnitsKnown = 3;

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

        public CompilationUnitSet TypeLayoutCompilationUnits(TypeDesc type)
        {
            return _layoutCompilationUnits.GetOrAdd(type, TypeLayoutCompilationUnitsUncached);
        }

        // Compilation Unit Index is the compilation unit of a given module. If the compilation unit
        // is unknown the module will be given an independent index from other modules, but 
        // IsCompilationUnitIndexExact will return false for that index. All compilation unit indices 
        // are >= 2, to allow for 0 to be a sentinel value.
        private int ModuleToCompilationUnitIndex(ModuleDesc nonEcmaModule)
        {
            EcmaModule module = (EcmaModule)nonEcmaModule;
            if (IsModuleInCompilationGroup(module))
                return 2;

            if (!VersionsWithModule(module))
                return 3;
            
            lock (_moduleCompilationUnits)
            {
                if (!_moduleCompilationUnits.TryGetValue(module, out int compilationUnit))
                {
                    compilationUnit = ++_maxCompilationUnitsKnown;
                    _moduleCompilationUnits.Add(module, compilationUnit);
                }

                return compilationUnit;
            }
        }

        // Indicate 
        private bool IsCompilationUnitIndexExact(int compilationUnitIndex)
        {
            if (compilationUnitIndex != 2)
                return false;
            else
                return true;
        }

        public struct CompilationUnitSet
        {
            private BitArray _bits;

            public CompilationUnitSet(ReadyToRunCompilationModuleGroupBase compilationGroup, ModuleDesc module)
            {
                int compilationIndex = compilationGroup.ModuleToCompilationUnitIndex(module);
                _bits = new BitArray(compilationIndex + 1);
                _bits.Set(compilationIndex, true);
            }

            public bool HasMultipleInexactCompilationUnits
            {
                get
                {
                    if (_bits == null)
                        return false;

                    return _bits[0];
                }
            }

            public bool HasMultipleCompilationUnits
            {
                get
                {
                    if (_bits == null)
                        return false;
                        
                    return _bits[1];
                }
            }

            public void UnionWith(ReadyToRunCompilationModuleGroupBase compilationGroup, CompilationUnitSet other)
            {
                if (other._bits == null)
                    return;

                if (HasMultipleInexactCompilationUnits)
                    return;

                if (other.HasMultipleInexactCompilationUnits)
                {
                    _bits[1] = true;
                    return;
                }

                if (other._bits.Length > _bits.Length)
                    _bits.Length = other._bits.Length;
                
                if (other._bits.Length < _bits.Length)
                {
                    for (int i = 0; i < other._bits.Length; i++)
                    {
                        if (other._bits[i])
                            _bits[i] = true;
                    }
                }
                else
                {
                    _bits.Or(other._bits);
                }

                int inexactCompilationUnitCount = 0;
                int compilationUnitCount = 0;
                for (int i = 2; i < _bits.Length; i++)
                {
                    if (_bits[i])
                    {
                        if (!compilationGroup.IsCompilationUnitIndexExact(i))
                            inexactCompilationUnitCount++;

                        compilationUnitCount++;
                    }
                    if (inexactCompilationUnitCount == 2)
                    {
                        // Multiple compilation units found
                        _bits[1] = true;
                    }
                    if (inexactCompilationUnitCount > 1)
                    {
                        // Multiple inexact compilation units involved
                        _bits[0] = true;
                        break;
                    }
                }
            }
        }

        private CompilationUnitSet TypeLayoutCompilationUnitsUncached(TypeDesc type)
        {
            if (type.IsObject ||
                type.IsPrimitive ||
                type.IsEnum ||
                type.IsPointer ||
                type.IsFunctionPointer ||
                type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
            {
                return default(CompilationUnitSet);
            }

            var defType = (MetadataType)type;

            CompilationUnitSet moduleDependencySet = new CompilationUnitSet(this, defType.Module);

            if ((type.BaseType != null) && !type.BaseType.IsObject)
            {
                moduleDependencySet.UnionWith(this, TypeLayoutCompilationUnits(type.BaseType));
            }

            foreach (FieldDesc field in defType.GetFields())
            {
                if (field.IsStatic)
                    continue;

                TypeDesc fieldType = field.FieldType;

                if (fieldType.IsValueType &&
                    !fieldType.IsPrimitive)
                {
                    moduleDependencySet.UnionWith(this, TypeLayoutCompilationUnits((MetadataType)fieldType));
                }
            }

            return moduleDependencySet;
        }

        private bool ModuleMatchesCompilationUnitIndex(ModuleDesc module1, ModuleDesc module2)
        {
            return ModuleToCompilationUnitIndex(module1) == ModuleToCompilationUnitIndex(module2);
        }

        public bool NeedsAlignmentBetweenBaseTypeAndDerived(MetadataType baseType, MetadataType derivedType)
        {
            if (!ModuleMatchesCompilationUnitIndex(derivedType.Module, baseType.Module) ||
                TypeLayoutCompilationUnits(baseType).HasMultipleCompilationUnits)
            {
                return true;
            }

            return false;
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
