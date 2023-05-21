// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.ReadyToRunConstants;

using ILCompiler;
using ILCompiler.DependencyAnalysis;

using Debug = System.Diagnostics.Debug;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

#pragma warning disable IDE0060

namespace Internal.IL
{
    // Implements an IL scanner that scans method bodies to be compiled by the code generation
    // backend before the actual compilation happens to gain insights into the code.
    internal partial class ILImporter
    {
        private readonly MethodIL _methodIL;
        private readonly MethodIL _canonMethodIL;
        private readonly ILScanner _compilation;
        private readonly ILScanNodeFactory _factory;

        // True if we're scanning a throwing method body because scanning the real body failed.
        private readonly bool _isFallbackBodyCompilation;

        private readonly MethodDesc _canonMethod;

        private DependencyList _dependencies = new DependencyList();

        private readonly byte[] _ilBytes;

        private sealed class BasicBlock
        {
            // Common fields
            public enum ImportState : byte
            {
                Unmarked,
                IsPending
            }

            public BasicBlock Next;

            public int StartOffset;
            public ImportState State = ImportState.Unmarked;

            public bool TryStart;
            public bool FilterStart;
            public bool HandlerStart;
        }

        private bool _isReadOnly;
        private TypeDesc _constrained;

        private int _currentInstructionOffset;
        private int _previousInstructionOffset;

        private sealed class ExceptionRegion
        {
            public ILExceptionRegion ILRegion;
        }
        private ExceptionRegion[] _exceptionRegions;

        public ILImporter(ILScanner compilation, MethodDesc method, MethodIL methodIL = null)
        {
            if (methodIL == null)
            {
                methodIL = compilation.GetMethodIL(method);
            }
            else
            {
                _isFallbackBodyCompilation = true;
            }

            // This is e.g. an "extern" method in C# without a DllImport or InternalCall.
            if (methodIL == null)
            {
                ThrowHelper.ThrowInvalidProgramException(ExceptionStringID.InvalidProgramSpecific, method);
            }

            _compilation = compilation;
            _factory = (ILScanNodeFactory)compilation.NodeFactory;

            _ilBytes = methodIL.GetILBytes();

            _canonMethodIL = methodIL;

            // Get the runtime determined method IL so that this works right in shared code
            // and tokens in shared code resolve to runtime determined types.
            MethodIL uninstantiatiedMethodIL = methodIL.GetMethodILDefinition();
            if (methodIL != uninstantiatiedMethodIL)
            {
                MethodDesc sharedMethod = method.GetSharedRuntimeFormMethodTarget();
                _methodIL = new InstantiatedMethodIL(sharedMethod, uninstantiatiedMethodIL);
            }
            else
            {
                _methodIL = methodIL;
            }

            _canonMethod = method;

            var ilExceptionRegions = methodIL.GetExceptionRegions();
            _exceptionRegions = new ExceptionRegion[ilExceptionRegions.Length];
            for (int i = 0; i < ilExceptionRegions.Length; i++)
            {
                _exceptionRegions[i] = new ExceptionRegion() { ILRegion = ilExceptionRegions[i] };
            }
        }

        public DependencyList Import()
        {
            TypeDesc owningType = _canonMethod.OwningType;
            if (_compilation.HasLazyStaticConstructor(owningType))
            {
                // Don't trigger cctor if this is a fallback compilation (bad cctor could have been the reason for fallback).
                // Otherwise follow the rules from ECMA-335 I.8.9.5.
                if (!_isFallbackBodyCompilation &&
                    (_canonMethod.Signature.IsStatic || _canonMethod.IsConstructor || owningType.IsValueType || owningType.IsInterface))
                {
                    // For beforefieldinit, we can wait for field access.
                    if (!((MetadataType)owningType).IsBeforeFieldInit)
                    {
                        MethodDesc method = _methodIL.OwningMethod;
                        if (method.OwningType.IsRuntimeDeterminedSubtype)
                        {
                            _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.GetNonGCStaticBase, method.OwningType), "Owning type cctor");
                        }
                        else
                        {
                            _dependencies.Add(_factory.ReadyToRunHelper(ReadyToRunHelperId.GetNonGCStaticBase, method.OwningType), "Owning type cctor");
                        }
                    }
                }
            }

            if (_canonMethod.IsSynchronized)
            {
                const string reason = "Synchronized method";
                if (_canonMethod.Signature.IsStatic)
                {
                    _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.MonitorEnterStatic), reason);
                    _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.MonitorExitStatic), reason);

                    MethodDesc method = _methodIL.OwningMethod;
                    if (method.OwningType.IsRuntimeDeterminedSubtype)
                    {
                        _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.NecessaryTypeHandle, method.OwningType), reason);
                    }
                    else
                    {
                        _dependencies.Add(_factory.NecessaryTypeSymbol(method.OwningType), reason);
                    }
                }
                else
                {
                    _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.MonitorEnter), reason);
                    _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.MonitorExit), reason);
                }

            }

            FindBasicBlocks();
            ImportBasicBlocks();

            CodeBasedDependencyAlgorithm.AddDependenciesDueToMethodCodePresence(ref _dependencies, _factory, _canonMethod, _canonMethodIL);

            return _dependencies;
        }

        private ISymbolNode GetGenericLookupHelper(ReadyToRunHelperId helperId, object helperArgument)
        {
            GenericDictionaryLookup lookup = _compilation.ComputeGenericLookup(_canonMethod, helperId, helperArgument);
            Debug.Assert(lookup.UseHelper);

            if (_canonMethod.RequiresInstMethodDescArg())
            {
                return _compilation.NodeFactory.ReadyToRunHelperFromDictionaryLookup(lookup.HelperId, lookup.HelperObject, _canonMethod);
            }
            else
            {
                Debug.Assert(_canonMethod.RequiresInstArg() || _canonMethod.AcquiresInstMethodTableFromThis());
                return _compilation.NodeFactory.ReadyToRunHelperFromTypeLookup(lookup.HelperId, lookup.HelperObject, _canonMethod.OwningType);
            }
        }

        private ISymbolNode GetHelperEntrypoint(ReadyToRunHelper helper)
        {
            return _compilation.GetHelperEntrypoint(helper);
        }

        private static void MarkInstructionBoundary() { }
        private static void EndImportingBasicBlock(BasicBlock basicBlock) { }

        private void StartImportingBasicBlock(BasicBlock basicBlock)
        {
            // Import all associated EH regions
            foreach (ExceptionRegion ehRegion in _exceptionRegions)
            {
                ILExceptionRegion region = ehRegion.ILRegion;
                if (region.TryOffset == basicBlock.StartOffset)
                {
                    MarkBasicBlock(_basicBlocks[region.HandlerOffset]);
                    if (region.Kind == ILExceptionRegionKind.Filter)
                        MarkBasicBlock(_basicBlocks[region.FilterOffset]);

                    if (region.Kind == ILExceptionRegionKind.Catch)
                    {
                        TypeDesc catchType = (TypeDesc)_methodIL.GetObject(region.ClassToken);
                        if (catchType.IsRuntimeDeterminedSubtype)
                        {
                            // For runtime determined Exception types we're going to emit a fake EH filter with isinst for this
                            // type with a runtime lookup
                            _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandleForCasting, catchType), "EH filter");
                        }
                    }
                }
            }

            _currentInstructionOffset = -1;
            _previousInstructionOffset = -1;
        }

        private void StartImportingInstruction()
        {
            _previousInstructionOffset = _currentInstructionOffset;
            _currentInstructionOffset = _currentOffset;
        }

        private void EndImportingInstruction()
        {
            // The instruction should have consumed any prefixes.
            _constrained = null;
            _isReadOnly = false;
        }

        private void ImportCasting(ILOpcode opcode, int token)
        {
            TypeDesc type = (TypeDesc)_methodIL.GetObject(token);

            if (type.IsRuntimeDeterminedSubtype)
            {
                _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandleForCasting, type), "IsInst/CastClass");
            }
            else
            {
                _dependencies.Add(_compilation.ComputeConstantLookup(ReadyToRunHelperId.TypeHandleForCasting, type), "IsInst/CastClass");
            }
        }

        private IMethodNode GetMethodEntrypoint(MethodDesc method)
        {
            if (method.HasInstantiation || method.OwningType.HasInstantiation)
            {
                _compilation.DetectGenericCycles(_canonMethod, method);
            }

            return _factory.MethodEntrypointOrTentativeMethod(method);
        }

        private void ImportCall(ILOpcode opcode, int token)
        {
            // We get both the canonical and runtime determined form - JitInterface mostly operates
            // on the canonical form.
            var runtimeDeterminedMethod = (MethodDesc)_methodIL.GetObject(token);
            var method = (MethodDesc)_canonMethodIL.GetObject(token);

            _compilation.TypeSystemContext.EnsureLoadableMethod(method);
            if ((method.Signature.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask) == MethodSignatureFlags.CallingConventionVarargs)
                ThrowHelper.ThrowBadImageFormatException();

            _compilation.NodeFactory.MetadataManager.GetDependenciesDueToAccess(ref _dependencies, _compilation.NodeFactory, _canonMethodIL, method);

            if (method.IsRawPInvoke())
            {
                // Raw P/invokes don't have any dependencies.
                return;
            }

            string reason = null;
            switch (opcode)
            {
                case ILOpcode.newobj:
                    reason = "newobj"; break;
                case ILOpcode.call:
                    reason = "call"; break;
                case ILOpcode.callvirt:
                    reason = "callvirt"; break;
                case ILOpcode.ldftn:
                    reason = "ldftn"; break;
                case ILOpcode.ldvirtftn:
                    reason = "ldvirtftn"; break;
                default:
                    Debug.Assert(false); break;
            }

            if (opcode == ILOpcode.newobj)
            {
                TypeDesc owningType = runtimeDeterminedMethod.OwningType;
                if (owningType.IsString)
                {
                    // String .ctor handled specially below
                }
                else if (owningType.IsGCPointer)
                {
                    if (owningType.IsRuntimeDeterminedSubtype)
                    {
                        _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, owningType), reason);
                    }
                    else
                    {
                        _dependencies.Add(_factory.ConstructedTypeSymbol(owningType), reason);
                    }

                    if (owningType.IsArray)
                    {
                        // RyuJIT is going to call the "MdArray" creation helper even if this is an SzArray,
                        // hence the IsArray check above. Note that the MdArray helper can handle SzArrays.
                        if (((ArrayType)owningType).Rank == 1)
                            _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.NewMultiDimArrRare), reason);
                        else
                            _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.NewMultiDimArr), reason);
                        return;
                    }
                    else
                    {
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.NewObject), reason);
                    }
                }
            }

            if (method.OwningType.IsDelegate && method.Name == "Invoke" &&
                opcode != ILOpcode.ldftn && opcode != ILOpcode.ldvirtftn)
            {
                // This call is expanded as an intrinsic; it's not an actual function call.
                // Before codegen realizes this is an intrinsic, it might still ask questions about
                // the vtable of this virtual method, so let's make sure it's marked in the scanner's
                // dependency graph.
                _dependencies.Add(_factory.VTable(method.OwningType), reason);
                return;
            }

            if (method.IsIntrinsic)
            {
                if (IsActivatorDefaultConstructorOf(method))
                {
                    if (runtimeDeterminedMethod.IsRuntimeDeterminedExactMethod)
                    {
                        _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.DefaultConstructor, runtimeDeterminedMethod.Instantiation[0]), reason);
                    }
                    else
                    {
                        MethodDesc ctor = Compilation.GetConstructorForCreateInstanceIntrinsic(method.Instantiation[0]);
                        _dependencies.Add(_factory.CanonicalEntrypoint(ctor), reason);
                    }

                    return;
                }

                if (IsActivatorAllocatorOf(method))
                {
                    if (runtimeDeterminedMethod.IsRuntimeDeterminedExactMethod)
                    {
                        _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.ObjectAllocator, runtimeDeterminedMethod.Instantiation[0]), reason);
                    }
                    else
                    {
                        _dependencies.Add(_compilation.ComputeConstantLookup(ReadyToRunHelperId.ObjectAllocator, method.Instantiation[0]), reason);
                    }

                    return;
                }

                if (IsEETypePtrOf(method))
                {
                    if (runtimeDeterminedMethod.IsRuntimeDeterminedExactMethod)
                    {
                        _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, runtimeDeterminedMethod.Instantiation[0]), reason);
                    }
                    else
                    {
                        _dependencies.Add(_factory.ConstructedTypeSymbol(method.Instantiation[0]), reason);
                    }
                    return;
                }
            }

            TypeDesc exactType = method.OwningType;

            bool resolvedConstraint = false;
            bool forceUseRuntimeLookup = false;
            DefaultInterfaceMethodResolution staticResolution = default;

            MethodDesc methodAfterConstraintResolution = method;
            if (_constrained != null)
            {
                // We have a "constrained." call.  Try a partial resolve of the constraint call.  Note that this
                // will not necessarily resolve the call exactly, since we might be compiling
                // shared generic code - it may just resolve it to a candidate suitable for
                // JIT compilation, and require a runtime lookup for the actual code pointer
                // to call.

                TypeDesc constrained = _constrained;
                if (constrained.IsRuntimeDeterminedSubtype)
                    constrained = constrained.ConvertToCanonForm(CanonicalFormKind.Specific);

                MethodDesc directMethod = constrained.GetClosestDefType().TryResolveConstraintMethodApprox(method.OwningType, method, out forceUseRuntimeLookup, ref staticResolution);
                if (directMethod == null && constrained.IsEnum)
                {
                    // Constrained calls to methods on enum methods resolve to System.Enum's methods. System.Enum is a reference
                    // type though, so we would fail to resolve and box. We have a special path for those to avoid boxing.
                    directMethod = _compilation.TypeSystemContext.TryResolveConstrainedEnumMethod(constrained, method);
                }

                if (directMethod != null)
                {
                    // Either
                    //    1. no constraint resolution at compile time (!directMethod)
                    // OR 2. no code sharing lookup in call
                    // OR 3. we have resolved to an instantiating stub

                    methodAfterConstraintResolution = directMethod;

                    Debug.Assert(!methodAfterConstraintResolution.OwningType.IsInterface
                        || methodAfterConstraintResolution.Signature.IsStatic);
                    resolvedConstraint = true;

                    exactType = directMethod.OwningType;

                    _factory.MetadataManager.NoteOverridingMethod(method, directMethod);
                }
                else if (method.Signature.IsStatic)
                {
                    Debug.Assert(method.OwningType.IsInterface);
                    exactType = constrained;
                }
                else if (constrained.IsValueType)
                {
                    // We'll need to box `this`. Note we use _constrained here, because the other one is canonical.
                    AddBoxingDependencies(_constrained, reason);
                }
            }

            MethodDesc targetMethod = methodAfterConstraintResolution;

            bool exactContextNeedsRuntimeLookup;
            if (targetMethod.HasInstantiation)
            {
                exactContextNeedsRuntimeLookup = targetMethod.IsSharedByGenericInstantiations;
            }
            else
            {
                exactContextNeedsRuntimeLookup = exactType.IsCanonicalSubtype(CanonicalFormKind.Any);
            }

            //
            // Determine whether to perform direct call
            //

            bool directCall = false;

            if (targetMethod.Signature.IsStatic)
            {
                if (_constrained != null && (!resolvedConstraint || forceUseRuntimeLookup))
                {
                    // Constrained call to static virtual interface method we didn't resolve statically
                    Debug.Assert(targetMethod.IsVirtual && targetMethod.OwningType.IsInterface);
                }
                else
                {
                    // Static methods are always direct calls
                    directCall = true;
                }
            }
            else if ((opcode != ILOpcode.callvirt && opcode != ILOpcode.ldvirtftn) || resolvedConstraint)
            {
                directCall = true;
            }
            else
            {
                if (!targetMethod.IsVirtual ||
                    // Final/sealed has no meaning for interfaces, but lets us devirtualize otherwise
                    (!targetMethod.OwningType.IsInterface && (targetMethod.IsFinal || targetMethod.OwningType.IsSealed())))
                {
                    directCall = true;
                }
            }

            if (directCall && targetMethod.IsAbstract)
            {
                ThrowHelper.ThrowBadImageFormatException();
            }

            MethodDesc targetForDelegate = !resolvedConstraint || forceUseRuntimeLookup ? runtimeDeterminedMethod : targetMethod;
            TypeDesc constraintForDelegate = !resolvedConstraint || forceUseRuntimeLookup ? _constrained : null;
            int numDependenciesBeforeTargetDetermination = _dependencies.Count;

            bool allowInstParam = opcode != ILOpcode.ldvirtftn && opcode != ILOpcode.ldftn;

            if (directCall && resolvedConstraint && exactContextNeedsRuntimeLookup)
            {
                // We want to do a direct call to a shared method on a valuetype. We need to provide
                // a generic context, but the JitInterface doesn't provide a way for us to do it from here.
                // So we do the next best thing and ask RyuJIT to look up a fat pointer.
                //
                // We have the canonical version of the method - find the runtime determined version.
                // This is simplified because we know the method is on a valuetype.
                Debug.Assert(targetMethod.OwningType.IsValueType);

                if (forceUseRuntimeLookup)
                {
                    // The below logic would incorrectly resolve the lookup into the first match we found,
                    // but there was a compile-time ambiguity due to shared code. The correct fix should
                    // use the ConstrainedMethodUseLookupResult dictionary entry so that the exact
                    // dispatch can be computed with the help of the generic dictionary.
                    // We fail the compilation here to avoid bad codegen. This is not actually an invalid program.
                    // https://github.com/dotnet/runtimelab/issues/1431
                    ThrowHelper.ThrowInvalidProgramException();
                }

                MethodDesc targetOfLookup;
                if (_constrained.IsRuntimeDeterminedType)
                    targetOfLookup = _compilation.TypeSystemContext.GetMethodForRuntimeDeterminedType(targetMethod.GetTypicalMethodDefinition(), (RuntimeDeterminedType)_constrained);
                else if (_constrained.HasInstantiation)
                    targetOfLookup = _compilation.TypeSystemContext.GetMethodForInstantiatedType(targetMethod.GetTypicalMethodDefinition(), (InstantiatedType)_constrained);
                else
                    targetOfLookup = targetMethod.GetMethodDefinition();
                if (targetOfLookup.HasInstantiation)
                {
                    targetOfLookup = targetOfLookup.MakeInstantiatedMethod(runtimeDeterminedMethod.Instantiation);
                }
                Debug.Assert(targetOfLookup.GetCanonMethodTarget(CanonicalFormKind.Specific) == targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific));
                _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.MethodEntry, targetOfLookup), reason);

                targetForDelegate = targetOfLookup;
            }
            else if (directCall && !allowInstParam && targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific).RequiresInstArg())
            {
                // Needs a single address to call this method but the method needs a hidden argument.
                // We need a fat function pointer for this that captures both things.

                if (exactContextNeedsRuntimeLookup)
                {
                    _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.MethodEntry, runtimeDeterminedMethod), reason);
                }
                else
                {
                    _dependencies.Add(_factory.FatFunctionPointer(targetMethod), reason);
                }
            }
            else if (directCall)
            {
                bool referencingArrayAddressMethod = false;

                if (targetMethod.IsIntrinsic)
                {
                    // If this is an intrinsic method with a callsite-specific expansion, this will replace
                    // the method with a method the intrinsic expands into. If it's not the special intrinsic,
                    // method stays unchanged.
                    targetMethod = _compilation.ExpandIntrinsicForCallsite(targetMethod, _canonMethod);

                    // Array address method requires special dependency tracking.
                    referencingArrayAddressMethod = targetMethod.IsArrayAddressMethod();
                }

                MethodDesc concreteMethod = targetMethod;
                targetMethod = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

                if (targetMethod.IsConstructor && targetMethod.OwningType.IsString)
                {
                    _dependencies.Add(_factory.StringAllocator(targetMethod), reason);
                }
                else if (exactContextNeedsRuntimeLookup)
                {
                    if (targetMethod.IsSharedByGenericInstantiations && !resolvedConstraint && !referencingArrayAddressMethod)
                    {
                        ISymbolNode instParam = null;

                        if (targetMethod.RequiresInstMethodDescArg())
                        {
                            instParam = GetGenericLookupHelper(ReadyToRunHelperId.MethodDictionary, runtimeDeterminedMethod);
                        }
                        else if (targetMethod.RequiresInstMethodTableArg())
                        {
                            bool hasHiddenParameter = true;

                            if (targetMethod.IsIntrinsic)
                            {
                                if (_factory.TypeSystemContext.IsSpecialUnboxingThunkTargetMethod(targetMethod))
                                    hasHiddenParameter = false;
                            }

                            if (hasHiddenParameter)
                                instParam = GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, runtimeDeterminedMethod.OwningType);
                        }

                        if (instParam != null)
                        {
                            _dependencies.Add(instParam, reason);
                        }

                        if (instParam == null
                            && !targetMethod.OwningType.IsValueType
                            && !_factory.TypeSystemContext.IsSpecialUnboxingThunk(_canonMethod))
                        {
                            // We have a call to a shared instance method and we're already in a shared context.
                            // e.g. this is a call to Foo<T>.Method() and we're about to add Foo<__Canon>.Method()
                            // to the dependency graph).
                            //
                            // We will pretend the runtime determined owning type (Foo<T>) got allocated as well.
                            // This is because RyuJIT might end up inlining the shared method body, making it concrete again,
                            // without actually having to go through a dictionary.
                            // (This would require inlining across two generic contexts, but RyuJIT does that.)
                            //
                            // If we didn't have a constructed type for this at the scanning time, we wouldn't
                            // know the dictionary dependencies at the inlined site, leading to a compile failure.
                            // (Remember that dictionary dependencies of instance methods on generic reference types
                            // are tied to the owning type.)
                            //
                            // This is not ideal, because if e.g. Foo<string> never got allocated otherwise, this code is
                            // unreachable and we're making the scanner scan more of it.
                            //
                            // Technically, we could get away with injecting a RuntimeDeterminedMethodNode here
                            // but that introduces more complexities and doesn't seem worth it at this time.
                            Debug.Assert(targetMethod.AcquiresInstMethodTableFromThis());
                            _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, runtimeDeterminedMethod.OwningType), reason + " - inlining protection");
                        }

                        _dependencies.Add(_factory.CanonicalEntrypoint(targetMethod), reason);
                    }
                    else
                    {
                        Debug.Assert(!forceUseRuntimeLookup);
                        _dependencies.Add(GetMethodEntrypoint(targetMethod), reason);

                        if (targetMethod.RequiresInstMethodTableArg() && resolvedConstraint)
                        {
                            if (_constrained.IsRuntimeDeterminedSubtype)
                                _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, _constrained), reason);
                            else
                                _dependencies.Add(_factory.ConstructedTypeSymbol(_constrained), reason);
                        }

                        if (referencingArrayAddressMethod && !_isReadOnly)
                        {
                            // Address method is special - it expects an instantiation argument, unless a readonly prefix was applied.
                            _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, runtimeDeterminedMethod.OwningType), reason);
                        }
                    }
                }
                else
                {
                    ISymbolNode instParam = null;

                    if (targetMethod.RequiresInstMethodDescArg())
                    {
                        instParam = _compilation.NodeFactory.MethodGenericDictionary(concreteMethod);
                    }
                    else if (targetMethod.RequiresInstMethodTableArg() || (referencingArrayAddressMethod && !_isReadOnly))
                    {
                        // Ask for a constructed type symbol because we need the vtable to get to the dictionary
                        instParam = _compilation.NodeFactory.ConstructedTypeSymbol(concreteMethod.OwningType);
                    }

                    if (instParam != null)
                    {
                        _dependencies.Add(instParam, reason);
                    }

                    if (instParam == null
                        && concreteMethod != targetMethod
                        && targetMethod.OwningType.NormalizeInstantiation() == targetMethod.OwningType
                        && !targetMethod.OwningType.IsValueType)
                    {
                        // We have a call to a shared instance method and we still know the concrete
                        // type of the generic instance (e.g. this is a call to Foo<string>.Method()
                        // and we're about to add Foo<__Canon>.Method() to the dependency graph).
                        //
                        // We will pretend the concrete type got allocated as well. This is because RyuJIT might
                        // end up inlining the shared method body, making it concrete again.
                        //
                        // If we didn't have a constructed type for this at the scanning time, we wouldn't
                        // know the dictionary dependencies at the inlined site, leading to a compile failure.
                        // (Remember that dictionary dependencies of instance methods on generic reference types
                        // are tied to the owning type.)
                        //
                        // This is not ideal, because if Foo<string> never got allocated otherwise, this code is
                        // unreachable and we're making the scanner scan more of it.
                        //
                        // Technically, we could get away with injecting a ShadowConcreteMethod for the concrete
                        // method, but that's more complex and doesn't seem worth it at this time.
                        Debug.Assert(targetMethod.AcquiresInstMethodTableFromThis());
                        _dependencies.Add(_compilation.NodeFactory.MaximallyConstructableType(concreteMethod.OwningType), reason + " - inlining protection");
                    }

                    _dependencies.Add(GetMethodEntrypoint(targetMethod), reason);
                }
            }
            else if (staticResolution is DefaultInterfaceMethodResolution.Diamond or DefaultInterfaceMethodResolution.Reabstraction)
            {
                Debug.Assert(targetMethod.OwningType.IsInterface && targetMethod.IsVirtual && _constrained != null);

                ThrowHelper.ThrowBadImageFormatException();
            }
            else if (method.Signature.IsStatic)
            {
                // This should be an unresolved static virtual interface method call. Other static methods should
                // have been handled as a directCall above.
                Debug.Assert(targetMethod.OwningType.IsInterface && targetMethod.IsVirtual && _constrained != null);

                var constrainedCallInfo = new ConstrainedCallInfo(_constrained, runtimeDeterminedMethod);
                var constrainedHelperId = ReadyToRunHelperId.ConstrainedDirectCall;

                // Constant lookup doesn't make sense and we don't implement it. If we need constant lookup,
                // the method wasn't implemented. Don't crash on it.
                if (!_compilation.NeedsRuntimeLookup(constrainedHelperId, constrainedCallInfo))
                    ThrowHelper.ThrowTypeLoadException(_constrained);

                _dependencies.Add(GetGenericLookupHelper(constrainedHelperId, constrainedCallInfo), reason);
            }
            else if (method.HasInstantiation)
            {
                // Generic virtual method call

                MethodDesc methodToLookup = _compilation.GetTargetOfGenericVirtualMethodCall(runtimeDeterminedMethod);

                _compilation.DetectGenericCycles(
                        _canonMethod,
                        methodToLookup.GetCanonMethodTarget(CanonicalFormKind.Specific));

                if (exactContextNeedsRuntimeLookup)
                {
                    _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.MethodHandle, methodToLookup), reason);
                }
                else
                {
                    _dependencies.Add(_factory.RuntimeMethodHandle(methodToLookup), reason);
                }

                _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.GVMLookupForSlot), reason);
            }
            else if (method.OwningType.IsInterface)
            {
                if (exactContextNeedsRuntimeLookup)
                {
                    _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.VirtualDispatchCell, runtimeDeterminedMethod), reason);
                }
                else
                {
                    _dependencies.Add(_factory.InterfaceDispatchCell(method), reason);
                }
            }
            else if (_compilation.HasFixedSlotVTable(method.OwningType))
            {
                // No dependencies: virtual call through the vtable
            }
            else
            {
                MethodDesc slotDefiningMethod = targetMethod.IsNewSlot ?
                        targetMethod : MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(targetMethod);
                _dependencies.Add(_factory.VirtualMethodUse(slotDefiningMethod), reason);
            }

            // Is this a verifiable delegate creation sequence (load function pointer followed by newobj)?
            if ((opcode == ILOpcode.ldftn || opcode == ILOpcode.ldvirtftn)
                && _currentOffset + 5 < _ilBytes.Length
                && _basicBlocks[_currentOffset] == null
                && _ilBytes[_currentOffset] == (byte)ILOpcode.newobj)
            {
                // TODO: for ldvirtftn we need to also check for the `dup` instruction
                int ctorToken = ReadILTokenAt(_currentOffset + 1);
                var ctorMethod = (MethodDesc)_methodIL.GetObject(ctorToken);
                if (ctorMethod.OwningType.IsDelegate)
                {
                    // Yep, verifiable delegate creation

                    // Drop any dependencies we inserted so far - the delegate construction helper is the only dependency
                    while (_dependencies.Count > numDependenciesBeforeTargetDetermination)
                        _dependencies.RemoveAt(_dependencies.Count - 1);

                    TypeDesc canonDelegateType = ctorMethod.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific);
                    DelegateCreationInfo info = _compilation.GetDelegateCtor(canonDelegateType, targetForDelegate, constraintForDelegate, opcode == ILOpcode.ldvirtftn);

                    if (info.NeedsRuntimeLookup)
                        _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.DelegateCtor, info), reason);
                    else
                        _dependencies.Add(_factory.ReadyToRunHelper(ReadyToRunHelperId.DelegateCtor, info), reason);
                }
            }
        }

        private void ImportLdFtn(int token, ILOpcode opCode)
        {
            ImportCall(opCode, token);
        }

        private void ImportJmp(int token)
        {
            // JMP is kind of like a tail call (with no arguments pushed on the stack).
            ImportCall(ILOpcode.call, token);
        }

        private void ImportCalli(int token)
        {
            MethodSignature signature = (MethodSignature)_methodIL.GetObject(token);

            // Managed calli
            if ((signature.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask) == 0)
                return;

            // Calli in marshaling stubs
            if (_methodIL is Internal.IL.Stubs.PInvokeILStubMethodIL)
                return;

            MethodDesc stub = _compilation.PInvokeILProvider.GetCalliStub(
                signature,
                ((MetadataType)_methodIL.OwningMethod.OwningType).Module);

            _dependencies.Add(_factory.CanonicalEntrypoint(stub), "calli");
        }

        private void ImportBranch(ILOpcode opcode, BasicBlock target, BasicBlock fallthrough)
        {
            ImportFallthrough(target);

            if (fallthrough != null)
                ImportFallthrough(fallthrough);
        }

        private void ImportSwitchJump(int jmpBase, int[] jmpDelta, BasicBlock fallthrough)
        {
            for (int i = 0; i < jmpDelta.Length; i++)
            {
                BasicBlock target = _basicBlocks[jmpBase + jmpDelta[i]];
                ImportFallthrough(target);
            }

            if (fallthrough != null)
                ImportFallthrough(fallthrough);
        }

        private void ImportUnbox(int token, ILOpcode opCode)
        {
            TypeDesc type = (TypeDesc)_methodIL.GetObject(token);

            if (!type.IsValueType)
            {
                if (opCode == ILOpcode.unbox_any)
                {
                    // When applied to a reference type, unbox_any has the same effect as castclass.
                    ImportCasting(ILOpcode.castclass, token);
                }
                return;
            }

            if (type.IsRuntimeDeterminedSubtype)
            {
                _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, type), "Unbox");
            }
            else
            {
                _dependencies.Add(_factory.NecessaryTypeSymbol(type), "Unbox");
            }

            ReadyToRunHelper helper;
            if (opCode == ILOpcode.unbox)
            {
                helper = ReadyToRunHelper.Unbox;
            }
            else
            {
                Debug.Assert(opCode == ILOpcode.unbox_any);
                helper = ReadyToRunHelper.Unbox_Nullable;
            }

            _dependencies.Add(GetHelperEntrypoint(helper), "Unbox");
        }

        private void ImportRefAnyVal(int token)
        {
            _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.GetRefAny), "refanyval");
            ImportTypedRefOperationDependencies(token, "refanyval");
        }

        private void ImportMkRefAny(int token)
        {
            _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.TypeHandleToRuntimeType), "mkrefany");
            _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.TypeHandleToRuntimeTypeHandle), "mkrefany");
            ImportTypedRefOperationDependencies(token, "mkrefany");
        }

        private void ImportTypedRefOperationDependencies(int token, string reason)
        {
            var type = (TypeDesc)_methodIL.GetObject(token);
            if (type.IsRuntimeDeterminedSubtype)
            {
                _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, type), reason);
            }
            else
            {
                _dependencies.Add(_factory.MaximallyConstructableType(type), reason);
            }
        }

        private void ImportLdToken(int token)
        {
            object obj = _methodIL.GetObject(token);

            if (obj is TypeDesc type)
            {
                // If this is a ldtoken Type / Type.GetTypeFromHandle sequence, we need one more helper.
                // We might also be able to optimize this a little if this is a ldtoken/GetTypeFromHandle/Equals sequence.
                bool isTypeEquals = false;
                BasicBlock nextBasicBlock = _basicBlocks[_currentOffset];
                if (nextBasicBlock == null)
                {
                    if ((ILOpcode)_ilBytes[_currentOffset] == ILOpcode.call)
                    {
                        int methodToken = ReadILTokenAt(_currentOffset + 1);
                        var method = (MethodDesc)_methodIL.GetObject(methodToken);
                        if (IsTypeGetTypeFromHandle(method))
                        {
                            // Codegen will swap this one for GetRuntimeTypeHandle when optimizing
                            _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.GetRuntimeType), "ldtoken");

                            // Is the next instruction a call to Type::Equals?
                            nextBasicBlock = _basicBlocks[_currentOffset + 5];
                            if (nextBasicBlock == null)
                            {
                                if ((ILOpcode)_ilBytes[_currentOffset + 5] == ILOpcode.call)
                                {
                                    methodToken = ReadILTokenAt(_currentOffset + 6);
                                    method = (MethodDesc)_methodIL.GetObject(methodToken);
                                    isTypeEquals = IsTypeEquals(method);
                                }
                            }
                        }
                    }
                }

                _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.GetRuntimeTypeHandle), "ldtoken");

                ISymbolNode reference;
                if (type.IsRuntimeDeterminedSubtype)
                {
                    reference = GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, type);
                }
                else
                {
                    reference = _compilation.ComputeConstantLookup(
                        isTypeEquals ? ReadyToRunHelperId.NecessaryTypeHandle : _compilation.GetLdTokenHelperForType(type), type);
                }
                _dependencies.Add(reference, "ldtoken");
            }
            else if (obj is MethodDesc method)
            {
                _factory.MetadataManager.GetDependenciesDueToAccess(ref _dependencies, _factory, _methodIL, (MethodDesc)_canonMethodIL.GetObject(token));

                if (method.IsRuntimeDeterminedExactMethod)
                {
                    _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.MethodHandle, method), "ldtoken");
                }
                else
                {
                    _dependencies.Add(_factory.RuntimeMethodHandle(method), "ldtoken");
                }

                _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.GetRuntimeMethodHandle), "ldtoken");
            }
            else
            {
                var field = (FieldDesc)obj;

                _factory.MetadataManager.GetDependenciesDueToAccess(ref _dependencies, _factory, _methodIL, (FieldDesc)_canonMethodIL.GetObject(token));

                if (field.OwningType.IsRuntimeDeterminedSubtype)
                {
                    _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.FieldHandle, field), "ldtoken");
                }
                else
                {
                    _dependencies.Add(_factory.RuntimeFieldHandle(field), "ldtoken");
                }

                _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.GetRuntimeFieldHandle), "ldtoken");
            }
        }

        private static void ImportRefAnyType()
        {
            // TODO
        }

        private static void ImportArgList()
        {
        }

        private void ImportConstrainedPrefix(int token)
        {
            _constrained = (TypeDesc)_methodIL.GetObject(token);
        }

        private void ImportReadOnlyPrefix()
        {
            _isReadOnly = true;
        }

        private void ImportFieldAccess(int token, bool isStatic, string reason)
        {
            var field = (FieldDesc)_methodIL.GetObject(token);
            var canonField = (FieldDesc)_canonMethodIL.GetObject(token);

            _compilation.NodeFactory.MetadataManager.GetDependenciesDueToAccess(ref _dependencies, _compilation.NodeFactory, _canonMethodIL, canonField);

            // Covers both ldsfld/ldsflda and ldfld/ldflda with a static field
            if (isStatic || field.IsStatic)
            {
                // ldsfld/ldsflda with an instance field is invalid IL
                if (isStatic && !field.IsStatic)
                    ThrowHelper.ThrowInvalidProgramException();

                // References to literal fields from IL body should never resolve.
                // The CLR would throw a MissingFieldException while jitting and so should we.
                if (field.IsLiteral)
                    ThrowHelper.ThrowMissingFieldException(field.OwningType, field.Name);

                if (field.HasRva)
                {
                    // We don't care about field RVA data for the usual cases, but if this is one of the
                    // magic fields the compiler synthetized, the data blob might bring more dependencies
                    // and we need to scan those.
                    _dependencies.Add(_compilation.GetFieldRvaData(field), reason);
                    // RVA static fields in generic types not implemented
                    Debug.Assert(!field.OwningType.HasInstantiation);
                    if (_compilation.HasLazyStaticConstructor(field.OwningType))
                        _dependencies.Add(_factory.TypeNonGCStaticsSymbol((MetadataType)field.OwningType), "Cctor context");
                    return;
                }

                ReadyToRunHelperId helperId;
                if (field.IsThreadStatic)
                {
                    helperId = ReadyToRunHelperId.GetThreadStaticBase;
                }
                else if (field.HasGCStaticBase)
                {
                    helperId = ReadyToRunHelperId.GetGCStaticBase;
                }
                else
                {
                    helperId = ReadyToRunHelperId.GetNonGCStaticBase;
                }

                TypeDesc owningType = field.OwningType;
                if (owningType.IsRuntimeDeterminedSubtype)
                {
                    _dependencies.Add(GetGenericLookupHelper(helperId, owningType), reason);
                }
                else
                {
                    _dependencies.Add(_factory.ReadyToRunHelper(helperId, owningType), reason);
                }
            }
        }

        private void ImportLoadField(int token, bool isStatic)
        {
            ImportFieldAccess(token, isStatic, isStatic ? "ldsfld" : "ldfld");
        }

        private void ImportAddressOfField(int token, bool isStatic)
        {
            ImportFieldAccess(token, isStatic, isStatic ? "ldsflda" : "ldflda");
        }

        private void ImportStoreField(int token, bool isStatic)
        {
            ImportFieldAccess(token, isStatic, isStatic ? "stsfld" : "stfld");
        }

        private void ImportLoadString(int token)
        {
            // If we care, this can include allocating the frozen string node.
            _dependencies.Add(_factory.SerializedStringObject(""), "ldstr");
        }

        private void ImportBox(int token)
        {
            var type = (TypeDesc)_methodIL.GetObject(token);

            // There are some sequences of box with ByRefLike types that are allowed
            // per the extension to the ECMA-335 specification.
            // Everything else is invalid.
            if (!type.IsRuntimeDeterminedType && type.IsByRefLike)
            {
                ILReader reader = new ILReader(_ilBytes, _currentOffset);
                ILOpcode nextOpcode = reader.ReadILOpcode();

                // box ; br_true/false
                if (nextOpcode is ILOpcode.brtrue or ILOpcode.brtrue_s or ILOpcode.brfalse or ILOpcode.brfalse_s)
                    return;

                if (nextOpcode is ILOpcode.unbox_any or ILOpcode.isinst)
                {
                    var type2 = (TypeDesc)_methodIL.GetObject(reader.ReadILToken());
                    if (type == type2)
                    {
                        // box ; unbox_any with same token
                        if (nextOpcode == ILOpcode.unbox_any)
                            return;

                        nextOpcode = reader.ReadILOpcode();

                        // box ; isinst ; br_true/false
                        if (nextOpcode is ILOpcode.brtrue or ILOpcode.brtrue_s or ILOpcode.brfalse or ILOpcode.brfalse_s)
                            return;

                        // box ; isinst ; unbox_any
                        if (nextOpcode == ILOpcode.unbox_any)
                        {
                            type2 = (TypeDesc)_methodIL.GetObject(reader.ReadILToken());
                            if (type2 == type)
                                return;
                        }
                    }
                }

                ThrowHelper.ThrowInvalidProgramException();
            }

            AddBoxingDependencies(type, "Box");
        }

        private void AddBoxingDependencies(TypeDesc type, string reason)
        {
            Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Any));

            // Generic code will have BOX instructions when referring to T - the instruction is a no-op
            // if the substitution wasn't a value type.
            if (!type.IsValueType)
                return;

            if (type.IsRuntimeDeterminedSubtype)
            {
                _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, type), reason);
            }
            else
            {
                _dependencies.Add(_factory.ConstructedTypeSymbol(type), reason);
            }

            if (type.IsNullable)
            {
                _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.Box_Nullable), reason);
            }
            else
            {
                _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.Box), reason);
            }
        }

        private void ImportLeave(BasicBlock target)
        {
            ImportFallthrough(target);
        }

        private void ImportNewArray(int token)
        {
            var elementType = (TypeDesc)_methodIL.GetObject(token);
            if (elementType.IsRuntimeDeterminedSubtype)
            {
                _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, elementType.MakeArrayType()), "newarr");
                _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.NewArray), "newarr");
            }
            else
            {
                if (elementType.IsVoid)
                    ThrowHelper.ThrowInvalidProgramException();
                _dependencies.Add(_factory.ConstructedTypeSymbol(elementType.MakeArrayType()), "newarr");
            }
        }

        private void ImportLoadElement(int token)
        {
            _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.RngChkFail), "ldelem");
        }

        private void ImportLoadElement(TypeDesc elementType)
        {
            _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.RngChkFail), "ldelem");
        }

        private void ImportStoreElement(int token)
        {
            _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.RngChkFail), "stelem");
        }

        private void ImportStoreElement(TypeDesc elementType)
        {
            _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.RngChkFail), "stelem");
        }

        private void ImportAddressOfElement(int token)
        {
            TypeDesc elementType = (TypeDesc)_methodIL.GetObject(token);
            if (elementType.IsGCPointer && !_isReadOnly)
            {
                if (elementType.IsRuntimeDeterminedSubtype)
                    _dependencies.Add(GetGenericLookupHelper(ReadyToRunHelperId.TypeHandle, elementType), "ldelema");
                else
                    _dependencies.Add(_factory.NecessaryTypeSymbol(elementType), "ldelema");
            }

            _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.RngChkFail), "ldelema");
        }

        private void ImportBinaryOperation(ILOpcode opcode)
        {
            switch (opcode)
            {
                case ILOpcode.add_ovf:
                case ILOpcode.add_ovf_un:
                case ILOpcode.sub_ovf:
                case ILOpcode.sub_ovf_un:
                    _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.Overflow), "_ovf");
                    break;
                case ILOpcode.mul_ovf:
                case ILOpcode.mul_ovf_un:
                    if (_compilation.TypeSystemContext.Target.Architecture == TargetArchitecture.ARM)
                    {
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.LMulOfv), "_lmulovf");
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.ULMulOvf), "_ulmulovf");
                    }

                    _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.Overflow), "_ovf");
                    break;
                case ILOpcode.div:
                case ILOpcode.div_un:
                    if (_compilation.TypeSystemContext.Target.Architecture == TargetArchitecture.ARM)
                    {
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.ULDiv), "_uldiv");
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.LDiv), "_ldiv");
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.UDiv), "_udiv");
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.Div), "_div");
                    }
                    else if (_compilation.TypeSystemContext.Target.Architecture == TargetArchitecture.ARM64)
                    {
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.ThrowDivZero), "_divbyzero");
                    }
                    break;
                case ILOpcode.rem:
                case ILOpcode.rem_un:
                    if (_compilation.TypeSystemContext.Target.Architecture == TargetArchitecture.ARM)
                    {
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.ULMod), "_ulmod");
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.LMod), "_lmod");
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.UMod), "_umod");
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.Mod), "_mod");
                    }
                    else if (_compilation.TypeSystemContext.Target.Architecture == TargetArchitecture.ARM64)
                    {
                        _dependencies.Add(GetHelperEntrypoint(ReadyToRunHelper.ThrowDivZero), "_divbyzero");
                    }
                    break;
            }
        }

        private void ImportFallthrough(BasicBlock next)
        {
            MarkBasicBlock(next);
        }

        private int ReadILTokenAt(int ilOffset)
        {
            return (int)(_ilBytes[ilOffset]
                + (_ilBytes[ilOffset + 1] << 8)
                + (_ilBytes[ilOffset + 2] << 16)
                + (_ilBytes[ilOffset + 3] << 24));
        }

        private static void ReportInvalidBranchTarget(int targetOffset)
        {
            ThrowHelper.ThrowInvalidProgramException();
        }

        private static void ReportFallthroughAtEndOfMethod()
        {
            ThrowHelper.ThrowInvalidProgramException();
        }

        private static void ReportMethodEndInsideInstruction()
        {
            ThrowHelper.ThrowInvalidProgramException();
        }

        private static void ReportInvalidInstruction(ILOpcode opcode)
        {
            ThrowHelper.ThrowInvalidProgramException();
        }

        private static bool IsTypeGetTypeFromHandle(MethodDesc method)
        {
            if (method.IsIntrinsic && method.Name == "GetTypeFromHandle")
            {
                MetadataType owningType = method.OwningType as MetadataType;
                if (owningType != null)
                {
                    return owningType.Name == "Type" && owningType.Namespace == "System";
                }
            }

            return false;
        }

        private static bool IsTypeEquals(MethodDesc method)
        {
            if (method.IsIntrinsic && method.Name == "op_Equality")
            {
                MetadataType owningType = method.OwningType as MetadataType;
                if (owningType != null)
                {
                    return owningType.Name == "Type" && owningType.Namespace == "System";
                }
            }

            return false;
        }

        private static bool IsActivatorDefaultConstructorOf(MethodDesc method)
        {
            if (method.IsIntrinsic && method.Name == "DefaultConstructorOf" && method.Instantiation.Length == 1)
            {
                MetadataType owningType = method.OwningType as MetadataType;
                if (owningType != null)
                {
                    return owningType.Name == "Activator" && owningType.Namespace == "System";
                }
            }

            return false;
        }

        private static bool IsActivatorAllocatorOf(MethodDesc method)
        {
            if (method.IsIntrinsic && method.Name == "AllocatorOf" && method.Instantiation.Length == 1)
            {
                MetadataType owningType = method.OwningType as MetadataType;
                if (owningType != null)
                {
                    return owningType.Name == "Activator" && owningType.Namespace == "System";
                }
            }

            return false;
        }

        private static bool IsEETypePtrOf(MethodDesc method)
        {
            if (method.IsIntrinsic && (method.Name == "EETypePtrOf" || method.Name == "Of") && method.Instantiation.Length == 1)
            {
                MetadataType owningType = method.OwningType as MetadataType;
                if (owningType != null)
                {
                    return (owningType.Name == "EETypePtr" && owningType.Namespace == "System")
                        || (owningType.Name == "MethodTable" && owningType.Namespace == "Internal.Runtime");
                }
            }

            return false;
        }

        private DefType GetWellKnownType(WellKnownType wellKnownType)
        {
            return _compilation.TypeSystemContext.GetWellKnownType(wellKnownType);
        }

        private static void ImportNop() { }
        private static void ImportBreak() { }
        private static void ImportLoadVar(int index, bool argument) { }
        private static void ImportStoreVar(int index, bool argument) { }
        private static void ImportAddressOfVar(int index, bool argument) { }
        private static void ImportDup() { }
        private static void ImportPop() { }
        private static void ImportLoadNull() { }
        private static void ImportReturn() { }
        private static void ImportLoadInt(long value, StackValueKind kind) { }
        private static void ImportLoadFloat(double value) { }
        private static void ImportLoadIndirect(int token) { }
        private static void ImportLoadIndirect(TypeDesc type) { }
        private static void ImportStoreIndirect(int token) { }
        private static void ImportStoreIndirect(TypeDesc type) { }
        private static void ImportShiftOperation(ILOpcode opcode) { }
        private static void ImportCompareOperation(ILOpcode opcode) { }
        private static void ImportConvert(WellKnownType wellKnownType, bool checkOverflow, bool unsigned) { }
        private static void ImportUnaryOperation(ILOpcode opCode) { }
        private static void ImportCpOpj(int token) { }
        private static void ImportCkFinite() { }
        private static void ImportLocalAlloc() { }
        private static void ImportEndFilter() { }
        private static void ImportCpBlk() { }
        private static void ImportInitBlk() { }
        private static void ImportRethrow() { }
        private static void ImportSizeOf(int token) { }
        private static void ImportUnalignedPrefix(byte alignment) { }
        private static void ImportVolatilePrefix() { }
        private static void ImportTailPrefix() { }
        private static void ImportNoPrefix(byte mask) { }
        private static void ImportThrow() { }
        private static void ImportInitObj(int token) { }
        private static void ImportLoadLength() { }
        private static void ImportEndFinally() { }
    }
}
