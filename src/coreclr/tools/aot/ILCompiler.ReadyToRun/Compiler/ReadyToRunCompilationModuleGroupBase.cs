// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private readonly ConcurrentDictionary<TypeDesc, bool> _versionsWithTypeReferenceCache = new ConcurrentDictionary<TypeDesc, bool>();
        private readonly ConcurrentDictionary<MethodDesc, bool> _versionsWithMethodCache = new ConcurrentDictionary<MethodDesc, bool>();
        private readonly Dictionary<ModuleDesc, CompilationUnitIndex> _moduleCompilationUnits = new Dictionary<ModuleDesc, CompilationUnitIndex>();
        private CompilationUnitIndex _nextCompilationUnit = CompilationUnitIndex.FirstDynamicallyAssigned;
        private ModuleTokenResolver _tokenResolver = null;

        public ReadyToRunCompilationModuleGroupBase(
            CompilerTypeSystemContext context,
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

            _tokenResolver = new ModuleTokenResolver(this, context);
        }

        public ModuleTokenResolver Resolver => _tokenResolver;

        public void AssociateTokenResolver(ModuleTokenResolver tokenResolver)
        {
            Debug.Assert(_tokenResolver == null);
            _tokenResolver = tokenResolver;
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

        private enum CompilationUnitIndex
        {
            RESERVEDForHasMultipleInexactCompilationUnits = 0,
            RESERVEDForHasMultipleCompilationUnits = 1,
            First = 2,
            Current = 2,
            OutsideOfVersionBubble = 3,
            FirstDynamicallyAssigned = 4,
        }

        // Compilation Unit Index is the compilation unit of a given module. If the compilation unit
        // is unknown the module will be given an independent index from other modules, but 
        // IsCompilationUnitIndexExact will return false for that index. All compilation unit indices 
        // are >= 2, to allow for 0 and 1 to be sentinel values.
        private CompilationUnitIndex ModuleToCompilationUnitIndex(ModuleDesc nonEcmaModule)
        {
            EcmaModule module = (EcmaModule)nonEcmaModule;
            if (IsModuleInCompilationGroup(module))
                return CompilationUnitIndex.Current;

            if (!VersionsWithModule(module))
                return CompilationUnitIndex.OutsideOfVersionBubble;
            
            // Assemblies within the version bubble, but not compiled as part of this compilation unit are given 
            // unique seperate compilation units. The practical effect of this is that the compiler can assume that
            // types which are entirely defined in one module can be laid out in an optimal fashion, but types
            // which are laid out relying on multiple modules cannot have their type layout precisely known as
            // it is unknown if the modules are bounding into a single composite image or into individual assemblies. 
            lock (_moduleCompilationUnits)
            {
                if (!_moduleCompilationUnits.TryGetValue(module, out CompilationUnitIndex compilationUnit))
                {
                    compilationUnit = _nextCompilationUnit;
                    _nextCompilationUnit = (CompilationUnitIndex)(((int)_nextCompilationUnit) + 1);
                    _moduleCompilationUnits.Add(module, compilationUnit);
                }

                return compilationUnit;
            }
        }

        // Indicate whether or not the compiler can take a hard dependency on the meaning of
        // the compilation unit index.
        private bool IsCompilationUnitIndexExact(CompilationUnitIndex compilationUnitIndex)
        {
            // Currently the implementation is only allowed to assume 2 details.
            // 1. That any assembly which is compiled with inputbubble set shall have its entire set of dependencies compiled as R2R
            // 2. That any assembly which is compiled in the current process may be considered to be part of a single unit.
            //
            // At some point, the compiler could take new parameters to allow the compiler to know that assemblies not in the current compilation
            // unit are to be compiled into composite images or into seperate binaries, and this helper function could return true for these other
            // compilation unit shapes.
            if (compilationUnitIndex != CompilationUnitIndex.Current)
                return false;
            else
                return true;
        }

        public struct CompilationUnitSet
        {
            private BitArray _bits;

            public CompilationUnitSet(ReadyToRunCompilationModuleGroupBase compilationGroup, ModuleDesc module)
            {
                CompilationUnitIndex compilationIndex = compilationGroup.ModuleToCompilationUnitIndex(module);
                _bits = new BitArray(((int)compilationIndex) + 1);
                _bits.Set((int)compilationIndex, true);
            }

            public bool HasMultipleInexactCompilationUnits
            {
                get
                {
                    if (_bits == null)
                        return false;

                    return _bits[(int)CompilationUnitIndex.RESERVEDForHasMultipleInexactCompilationUnits];
                }
            }

            public bool HasMultipleCompilationUnits
            {
                get
                {
                    if (_bits == null)
                        return false;
                        
                    return _bits[(int)CompilationUnitIndex.RESERVEDForHasMultipleCompilationUnits];
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
                    _bits[(int)CompilationUnitIndex.RESERVEDForHasMultipleCompilationUnits] = true;
                    _bits[(int)CompilationUnitIndex.RESERVEDForHasMultipleInexactCompilationUnits] = true;
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
                for (int i = (int)CompilationUnitIndex.First; i < _bits.Length; i++)
                {
                    if (_bits[i])
                    {
                        if (!compilationGroup.IsCompilationUnitIndexExact((CompilationUnitIndex)i))
                            inexactCompilationUnitCount++;

                        compilationUnitCount++;
                    }
                    if (compilationUnitCount == 2)
                    {
                        // Multiple compilation units found
                        _bits[(int)CompilationUnitIndex.RESERVEDForHasMultipleCompilationUnits] = true;
                    }
                    if (inexactCompilationUnitCount == 2)
                    {
                        // Multiple inexact compilation units involved
                        _bits[(int)CompilationUnitIndex.RESERVEDForHasMultipleInexactCompilationUnits] = true;
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

            if ((type.BaseType != null) && !type.BaseType.IsObject && !type.IsValueType)
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

        public override bool NeedsAlignmentBetweenBaseTypeAndDerived(MetadataType baseType, MetadataType derivedType)
        {
            if (baseType.IsObject)
                return false;

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

        public sealed override bool VersionsWithTypeReference(TypeDesc typeDesc)
        {
            return _versionsWithTypeReferenceCache.GetOrAdd(typeDesc, ComputeTypeReferenceVersionsWithCode);
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

        private bool ComputeTypeReferenceVersionsWithCode(TypeDesc type)
        {
            // Type represented by simple element type
            if (type.IsPrimitive || type.IsVoid || type.IsObject || type.IsString)
                return true;

            if (VersionsWithType(type))
                return true;

            if (type.IsParameterizedType)
            {
                return VersionsWithTypeReference(type.GetParameterType());
            }

            if (type.IsFunctionPointer)
            {
                MethodSignature ptrSignature = ((FunctionPointerType)type).Signature;

                if (!VersionsWithTypeReference(ptrSignature.ReturnType))
                    return false;

                for (int i = 0; i < ptrSignature.Length; i++)
                {
                    if (!VersionsWithTypeReference(ptrSignature[i]))
                        return false;
                }
                if (ptrSignature.HasEmbeddedSignatureData)
                {
                    foreach (var embeddedSigData in ptrSignature.GetEmbeddedSignatureData())
                    {
                        if (embeddedSigData.type != null)
                        {
                            if (!VersionsWithTypeReference(embeddedSigData.type))
                                return false;
                        }
                    }
                }
            }

            if (type is EcmaType ecmaType)
            {
                return !_tokenResolver.GetModuleTokenForType(ecmaType, false).IsNull;
            }

            if (type.GetTypeDefinition() == type)
            {
                // Must not be an ECMA type, which are the only form of simple type which cannot reach here
                return false;
            }

            if (type.HasInstantiation)
            {
                if (!VersionsWithTypeReference(type.GetTypeDefinition()))
                    return false;

                foreach (TypeDesc instParam in type.Instantiation)
                {
                    if (!VersionsWithTypeReference(instParam))
                        return false;
                }

                return true;
            }

            Debug.Assert(false, "Unhandled form of type in VersionsWithTypeReference");
            return false;
        }

        private bool ComputeInstantiationVersionsWithCode(Instantiation inst, TypeSystemEntity entityWithInstantiation)
        {
            for (int iInstantiation = 0; iInstantiation < inst.Length; iInstantiation++)
            {
                TypeDesc instType = inst[iInstantiation];

                if (!ComputeInstantiationTypeVersionsWithCode(this, instType))
                {
                    if (instType.IsPrimitive || instType.IsObject || instType.IsString)
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
                        if (instType.IsPrimitive)
                        {
                            if (genericParam.HasReferenceTypeConstraint)
                                return false;
                        }
                        else
                        {
                            Debug.Assert(instType.IsString || instType.IsObject);
                            if (genericParam.HasNotNullableValueTypeConstraint)
                                return false;

                            if (instType.IsString && genericParam.HasDefaultConstructorConstraint)
                                return false;
                        }

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
