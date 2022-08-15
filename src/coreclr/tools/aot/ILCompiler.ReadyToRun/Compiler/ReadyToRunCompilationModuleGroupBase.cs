// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem.Interop;
using Debug = System.Diagnostics.Debug;
using Internal.ReadyToRunConstants;

namespace ILCompiler
{
    public class ReadyToRunCompilationModuleGroupConfig
    {
        public CompilerTypeSystemContext Context;
        public bool IsCompositeBuildMode;
        public bool IsInputBubble;
        public bool CrossModuleInlining;
        public bool CrossModuleGenericCompilation;
        public IEnumerable<EcmaModule> CompilationModuleSet;
        public IEnumerable<ModuleDesc> VersionBubbleModuleSet;
        public IEnumerable<ModuleDesc> CrossModuleInlineable;
        public bool CompileGenericDependenciesFromVersionBubbleModuleSet;
        public bool CompileAllPossibleCrossModuleCode;
    }

    public abstract class ReadyToRunCompilationModuleGroupBase : CompilationModuleGroup
    {
        protected readonly HashSet<EcmaModule> _compilationModuleSet;
        private readonly HashSet<ModuleDesc> _versionBubbleModuleSet;
        private readonly HashSet<ModuleDesc> _crossModuleInlineableModuleSet = new HashSet<ModuleDesc>();
        private Dictionary<TypeDesc, ModuleToken> _typeRefsInCompilationModuleSet;
        private readonly bool _compileGenericDependenciesFromVersionBubbleModuleSet;
        private readonly bool _isCompositeBuildMode;
        private readonly bool _isInputBubble;
        private readonly bool _crossModuleInlining;
        private readonly bool _crossModuleGenericCompilation;
        private readonly ConcurrentDictionary<TypeDesc, CompilationUnitSet> _layoutCompilationUnits = new ConcurrentDictionary<TypeDesc, CompilationUnitSet>();
        private readonly ConcurrentDictionary<TypeDesc, bool> _versionsWithTypeCache = new ConcurrentDictionary<TypeDesc, bool>();
        private readonly ConcurrentDictionary<TypeDesc, bool> _versionsWithTypeReferenceCache = new ConcurrentDictionary<TypeDesc, bool>();
        private readonly ConcurrentDictionary<MethodDesc, bool> _versionsWithMethodCache = new ConcurrentDictionary<MethodDesc, bool>();
        private readonly ConcurrentDictionary<MethodDesc, bool> _crossModuleInlineableCache = new ConcurrentDictionary<MethodDesc, bool>();
        private readonly ConcurrentDictionary<TypeDesc, bool> _crossModuleInlineableTypeCache = new ConcurrentDictionary<TypeDesc, bool>();
        private readonly ConcurrentDictionary<MethodDesc, bool> _crossModuleCompilableCache = new ConcurrentDictionary<MethodDesc, bool>();
        private readonly Dictionary<ModuleDesc, CompilationUnitIndex> _moduleCompilationUnits = new Dictionary<ModuleDesc, CompilationUnitIndex>();
        private ProfileDataManager _profileData;
        private CompilationUnitIndex _nextCompilationUnit = CompilationUnitIndex.FirstDynamicallyAssigned;
        private ModuleTokenResolver _tokenResolver = null;
        private ConcurrentDictionary<EcmaMethod, bool> _tokenTranslationFreeNonVersionable = new ConcurrentDictionary<EcmaMethod, bool>();
        private bool CompileAllPossibleCrossModuleCode = false;

        public ReadyToRunCompilationModuleGroupBase(ReadyToRunCompilationModuleGroupConfig config)
        {
            _compilationModuleSet = new HashSet<EcmaModule>(config.CompilationModuleSet);
            _isCompositeBuildMode = config.IsCompositeBuildMode;
            _isInputBubble = config.IsInputBubble;
            _crossModuleInlining = config.CrossModuleInlining;
            _crossModuleGenericCompilation = config.CrossModuleGenericCompilation;
            CompileAllPossibleCrossModuleCode = config.CompileAllPossibleCrossModuleCode;

            Debug.Assert(_isCompositeBuildMode || _compilationModuleSet.Count == 1);

            _versionBubbleModuleSet = new HashSet<ModuleDesc>(config.VersionBubbleModuleSet);
            _versionBubbleModuleSet.UnionWith(_compilationModuleSet);

            _crossModuleInlineableModuleSet.UnionWith(config.CrossModuleInlineable);
            _crossModuleInlineableModuleSet.UnionWith(_versionBubbleModuleSet);

            _compileGenericDependenciesFromVersionBubbleModuleSet = config.CompileGenericDependenciesFromVersionBubbleModuleSet;

            _tokenResolver = new ModuleTokenResolver(this, config.Context);
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

        public bool IsModuleInCompilationGroup(EcmaModule module)
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

        public override ReadyToRunFlags GetReadyToRunFlags()
        {
            ReadyToRunFlags flags = default(ReadyToRunFlags);
            if (_versionBubbleModuleSet.Count > 0)
            {
                flags |= ReadyToRunFlags.READYTORUN_FLAG_MultiModuleVersionBubble;
            }
            if (CompileAllPossibleCrossModuleCode || _compileGenericDependenciesFromVersionBubbleModuleSet)
            {
                flags |= ReadyToRunFlags.READYTORUN_FLAG_UnrelatedR2RCode;
            }
            return flags;
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
            // unique separate compilation units. The practical effect of this is that the compiler can assume that
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
            // unit are to be compiled into composite images or into separate binaries, and this helper function could return true for these other
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

        public bool CrossModuleInlineableModule(ModuleDesc module)
        {
            return _crossModuleInlineableModuleSet.Contains(module);
        }

        public bool CrossModuleInlineableType(TypeDesc typeDesc)
        {
            return typeDesc.GetTypeDefinition() is EcmaType ecmaType &&
                _crossModuleInlineableTypeCache.GetOrAdd(typeDesc, ComputeCrossModuleInlineableType);
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

            return ComputeInstantiationVersionsWithCode(method.Instantiation, method, VersionsWithType);
        }

        public sealed override bool CanInline(MethodDesc callerMethod, MethodDesc calleeMethod)
        {
            // Allow inlining if the caller is within the current version bubble
            // (because otherwise we may not be able to encode its tokens)
            // and if the callee is either in the same version bubble or is marked as non-versionable.
            bool canInline = (VersionsWithMethodBody(callerMethod) || CrossModuleInlineable(callerMethod)) &&
                (VersionsWithMethodBody(calleeMethod) || CrossModuleInlineable(calleeMethod) || IsNonVersionableWithILTokensThatDoNotNeedTranslation(calleeMethod));

            return canInline;
        }

        public bool IsNonVersionableWithILTokensThatDoNotNeedTranslation(MethodDesc method)
        {
            if (!method.IsNonVersionable())
                return false;

            return _tokenTranslationFreeNonVersionable.GetOrAdd((EcmaMethod)method.GetTypicalMethodDefinition(), IsNonVersionableWithILTokensThatDoNotNeedTranslationUncached);
        }

        public override bool CrossModuleCompileable(MethodDesc method)
        {
            if (!_crossModuleGenericCompilation)
                return false;

            return _crossModuleCompilableCache.GetOrAdd(method, CrossModuleCompileableUncached);
        }

        private bool CrossModuleCompileableUncached(MethodDesc method)
        {
            if (!CrossModuleInlineableInternal(method))
                return false;

            // Only allow generics, non-generics should be compiled into the defining module
            if (!(method.HasInstantiation || method.OwningType.HasInstantiation))
                return false;

            if (CompileAllPossibleCrossModuleCode)
            {
                return true;
            }

            EcmaModule methodDefiningModule = ((MetadataType)method.OwningType.GetTypeDefinition()).Module as EcmaModule;

            if (_compilationModuleSet.Contains(methodDefiningModule))
                return true;

            // Alternate algorithm for generic method placement
            // This is designed to provide a second location to look for a generic instantiation that isn't the
            // defining module of the method. This algorithm is designed to be:
            // 1. Cheap to compute
            // 2. Able to find many generic instantiations assuming a fairly low level of generic complexity
            // 3. Particularly useful for cases where a second module instantiates a generic from a defining module
            //    over types entirely defined in a second module.
            // 4. Simple to implement in a compatible fashion in crossgen2 and runtime, so that the runtime and compile can agree
            //    on code that should be useable. See ReadyToRunInfo::ComputeAlternateGenericLocationForR2RCode
            //    for the native implementation.

            EcmaModule alternateGenericLocationModule = ComputeAlternateGenericLocationForR2RCodeFromInstantiation(methodDefiningModule, method.Instantiation);
            if (alternateGenericLocationModule == null)
            {
                alternateGenericLocationModule = ComputeAlternateGenericLocationForR2RCodeFromInstantiation(methodDefiningModule, method.OwningType.Instantiation);
            }

            if (alternateGenericLocationModule != null)
            {
                return _compilationModuleSet.Contains(alternateGenericLocationModule);
            }
            return false;

            EcmaModule ComputeAlternateGenericLocationForR2RCodeFromInstantiation(ModuleDesc definitionModule, Instantiation inst)
            {
                foreach (var instArgFixed in inst)
                {
                    var instArg = instArgFixed;
                    while (instArg.IsParameterizedType)
                    {
                        instArg = instArg.GetParameterType();
                    }
                    // System.__Canon does not contribute to logical loader module
                    if (instArg.IsCanonicalDefinitionType(CanonicalFormKind.Any))
                        continue;
                    if (instArg.IsPrimitive)
                        continue;
                    if (instArg.GetTypeDefinition() is MetadataType metadataType)
                    {
                        if (metadataType.Module != definitionModule)
                            return metadataType.Module as EcmaModule;
                    }
                }

                return null;
            }
        }

        public override bool CrossModuleInlineable(MethodDesc method)
        {
            if (!_crossModuleInlining)
                return false;
            
            return CrossModuleInlineableInternal(method);
        }

        // Internal predicate so that the switches controlling cross module inlining and compilation are independent
        private bool CrossModuleInlineableInternal(MethodDesc method)
        {
            return _crossModuleInlineableCache.GetOrAdd(method, CrossModuleInlineableUncached);
        }

        private bool CrossModuleInlineableUncached(MethodDesc method)
        {
            // Defined in corelib
            MetadataType owningMetadataType = method.OwningType.GetTypeDefinition() as MetadataType;
            if (owningMetadataType == null)
            {
                // If the type is an array or something
                return false;
            }
            ModuleDesc methodDefiningModule = owningMetadataType.Module;
            if (!_crossModuleInlineableModuleSet.Contains(methodDefiningModule))
                return false;

            // Where all the generic instantiation details are either:
            // 1. uninstantiated
            // 2. Instantiated over some structure type defined within the version bubble, and all other generic arguments are either
            //  - Canon
            //  - A non-generic structure type defined in the same module as the defining module of the method (Current implementation is just for non-generics, but could easily be extended to generics)
            if ((method.GetMethodDefinition() == method) && method.OwningType.IsTypeDefinition)
            {
                // This should be ok
            }
            else
            {
                foreach (TypeDesc t in method.Instantiation)
                {
                    bool usesCanon = t.IsCanonicalDefinitionType(CanonicalFormKind.Any);
                    bool versionsWithType = CrossModuleInlineableType(t);
                    if (!versionsWithType && !usesCanon && (t.HasInstantiation && !(t is EcmaType etype && etype.Module != methodDefiningModule)))
                        return false;
                }
                foreach (TypeDesc t in method.OwningType.Instantiation)
                {
                    bool usesCanon = t.IsCanonicalDefinitionType(CanonicalFormKind.Any);
                    bool versionsWithType = CrossModuleInlineableType(t);
                    if (!versionsWithType && !usesCanon && (t.HasInstantiation && !(t is EcmaType etype && etype.Module != methodDefiningModule)))
                        return false;
                }
            }

            return true;
        }

        private bool IsNonVersionableWithILTokensThatDoNotNeedTranslationUncached(EcmaMethod method)
        {
            bool result = false;
            try
            {

                // Validate that there are no tokens in the IL other than tokens associated with the following
                // instructions with the 
                // 1. ldfld, ldflda, and stfld to instance fields of NonVersionable structs and NonVersionable classes
                // 2. cpobj, initobj, ldobj, stobj, ldelem, ldelema or sizeof, to NonVersionable structures, signature variables, pointers, function pointers, byrefs, classes, or arrays
                // 3. stelem, to NonVersionable structures
                // In addition, the method must not have any EH.
                // The method may only have locals which are NonVersionable structures, or classes

                MethodIL methodIL = new ReadyToRunILProvider(this).GetMethodIL(method);
                if (methodIL.GetExceptionRegions().Length > 0)
                    return false;

                foreach (var local in methodIL.GetLocals())
                {
                    if (local.Type.IsPrimitive)
                        continue;

                    if (local.Type.IsArray)
                        continue;

                    if (local.Type.IsSignatureVariable)
                        continue;
                    MetadataType metadataType = local.Type as MetadataType;

                    if (metadataType == null)
                        return false;

                    if (metadataType.IsValueType)
                    {
                        if (metadataType.IsNonVersionable())
                            continue;
                        else
                            return false;
                    }
                }

                ILReader ilReader = new ILReader(methodIL.GetILBytes());
                while (ilReader.HasNext)
                {
                    ILOpcode opcode = ilReader.ReadILOpcode();
                    switch (opcode)
                    {
                        case ILOpcode.ldfld:
                        case ILOpcode.ldflda:
                        case ILOpcode.stfld:
                            {
                                int token = ilReader.ReadILToken();
                                FieldDesc field = methodIL.GetObject(token) as FieldDesc;
                                if (field == null)
                                    return false;
                                if (field.IsStatic)
                                    return false;
                                MetadataType owningMetadataType = (MetadataType)field.OwningType;
                                if (!owningMetadataType.IsNonVersionable())
                                    return false;
                                break;
                            }

                        case ILOpcode.ldelem:
                        case ILOpcode.ldelema:
                        case ILOpcode.stobj:
                        case ILOpcode.ldobj:
                        case ILOpcode.initobj:
                        case ILOpcode.cpobj:
                        case ILOpcode.sizeof_:
                            {
                                int token = ilReader.ReadILToken();
                                TypeDesc type = methodIL.GetObject(token) as TypeDesc;
                                if (type == null)
                                    return false;

                                MetadataType metadataType = type as MetadataType;
                                if (metadataType == null)
                                    continue; // Types which are not metadata types are all well defined in size

                                if (!metadataType.IsValueType)
                                    continue; // Reference types are all well defined in size for the sizeof instruction

                                if (metadataType.IsNonVersionable())
                                    continue;
                                return false;
                            }

                        case ILOpcode.stelem:
                            {
                                int token = ilReader.ReadILToken();
                                MetadataType type = methodIL.GetObject(token) as MetadataType;
                                if (type == null)
                                    return false;

                                if (!type.IsValueType)
                                    return false;
                                if (!type.IsNonVersionable())
                                    return false;
                                break;
                            }

                        // IL instructions which refer to tokens which are not safe for NonVersionable methods
                        case ILOpcode.box:
                        case ILOpcode.call:
                        case ILOpcode.calli:
                        case ILOpcode.callvirt:
                        case ILOpcode.castclass:
                        case ILOpcode.jmp:
                        case ILOpcode.isinst:
                        case ILOpcode.ldstr:
                        case ILOpcode.ldsfld:
                        case ILOpcode.ldsflda:
                        case ILOpcode.ldtoken:
                        case ILOpcode.ldvirtftn:
                        case ILOpcode.ldftn:
                        case ILOpcode.mkrefany:
                        case ILOpcode.newarr:
                        case ILOpcode.newobj:
                        case ILOpcode.refanyval:
                        case ILOpcode.stsfld:
                        case ILOpcode.unbox:
                        case ILOpcode.unbox_any:
                        case ILOpcode.constrained:
                            return false;

                        default:
                            // Unless its a opcode known to be permitted with a 
                            ilReader.Skip(opcode);
                            break;
                    }
                }

                result = true;
            }
            catch (TypeSystemException)
            {
                return false;
            }

            return result;
        }

        public sealed override bool GeneratesPInvoke(MethodDesc method)
        {
            return !Marshaller.IsMarshallingRequired(method);
        }

        public sealed override bool TryGetModuleTokenForExternalType(TypeDesc type, out ModuleToken token)
        {
            Debug.Assert(!VersionsWithType(type));

            if (_typeRefsInCompilationModuleSet == null)
            {
                lock(_compilationModuleSet)
                {
                    if (_typeRefsInCompilationModuleSet == null)
                    {
                        var typeRefsInCompilationModuleSet = new Dictionary<TypeDesc, ModuleToken>();

                        foreach (var module in _compilationModuleSet)
                        {
                            EcmaModule ecmaModule = (EcmaModule)module;
                            foreach (var typeRefHandle in ecmaModule.MetadataReader.TypeReferences)
                            {
                                try
                                {
                                    TypeDesc typeFromTypeRef = ecmaModule.GetType(typeRefHandle);
                                    if (!typeRefsInCompilationModuleSet.ContainsKey(typeFromTypeRef))
                                    {
                                        typeRefsInCompilationModuleSet.Add(typeFromTypeRef, new ModuleToken(ecmaModule, typeRefHandle));
                                    }
                                }
                                catch (TypeSystemException) { }
                            }
                        }
                        Volatile.Write(ref _typeRefsInCompilationModuleSet, typeRefsInCompilationModuleSet);
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

            return ComputeInstantiationVersionsWithCode(type.Instantiation, type, VersionsWithType);
        }

        private bool ComputeCrossModuleInlineableType(TypeDesc type)
        {
            if (type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
                return true;

            if (type is MetadataType mdType)
            {
                if (!_crossModuleInlineableModuleSet.Contains(mdType.Module))
                    return false;
            }

            if (type == type.GetTypeDefinition())
                return true;

            return ComputeInstantiationVersionsWithCode(type.Instantiation, type, CrossModuleInlineableType);
        }

        private bool ComputeTypeReferenceVersionsWithCode(TypeDesc type)
        {
            // Type represented by simple element type
            if (type.IsPrimitive || type.IsVoid || type.IsObject || type.IsString || type.IsTypedReference)
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
                return !_tokenResolver.GetModuleTokenForType(ecmaType, allowDynamicallyCreatedReference: false, throwIfNotFound: false).IsNull;
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

        private static bool ComputeInstantiationVersionsWithCode(Instantiation inst, TypeSystemEntity entityWithInstantiation, Func<TypeDesc, bool> versionsWithTypePredicate)
        {
            for (int iInstantiation = 0; iInstantiation < inst.Length; iInstantiation++)
            {
                TypeDesc instType = inst[iInstantiation];

                if (!ComputeInstantiationTypeVersionsWithCode(versionsWithTypePredicate, instType))
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

            static bool ComputeInstantiationTypeVersionsWithCode(Func<TypeDesc, bool> versionsWithTypePredicate, TypeDesc type)
            {
                if (type == type.Context.CanonType)
                    return true;

                if (versionsWithTypePredicate(type))
                    return true;

                if (type.IsArray)
                    return ComputeInstantiationTypeVersionsWithCode(versionsWithTypePredicate, type.GetParameterType());

                if (type.IsPointer)
                    return ComputeInstantiationTypeVersionsWithCode(versionsWithTypePredicate, type.GetParameterType());

                return false;
            }
        }

        public virtual void ApplyProfileGuidedOptimizationData(ProfileDataManager profileGuidedCompileRestriction, bool makePartial)
        {
            _profileData = profileGuidedCompileRestriction;
        }
    }
}
