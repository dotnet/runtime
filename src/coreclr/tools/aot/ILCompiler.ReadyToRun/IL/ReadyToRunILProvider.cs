// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Internal.IL.Stubs;
using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ILCompiler.ReadyToRun.TypeSystem;

namespace Internal.IL
{
    /// <summary>
    /// Marker interface that promises that all tokens from this MethodIL are useable in the current compilation
    /// </summary>
    public interface IMethodTokensAreUseableInCompilation { }


    public sealed class ReadyToRunILProvider : ILProvider
    {
        private CompilationModuleGroup _compilationModuleGroup;
        private MutableModule _manifestMutableModule;
        private int _version = 0;

        public ReadyToRunILProvider(CompilationModuleGroup compilationModuleGroup)
        {
            _compilationModuleGroup = compilationModuleGroup;
        }

        public void InitManifestMutableModule(MutableModule module)
        {
            _manifestMutableModule = module;
        }

        void IncrementVersion()
        {
            _version++;
        }

        public int Version => _version;

        private MethodIL TryGetIntrinsicMethodILForActivator(MethodDesc method)
        {
            if (method.Instantiation.Length == 1
                && method.Signature.Length == 0
                && method.Name.SequenceEqual("CreateInstance"u8))
            {
                TypeDesc type = method.Instantiation[0];
                if (type.IsValueType && type.GetParameterlessConstructor() == null)
                {
                    // Replace the body with implementation that just returns "default"
                    MethodDesc createDefaultInstance = method.OwningType.GetKnownMethod("CreateDefaultInstance"u8, method.GetTypicalMethodDefinition().Signature);
                    return GetMethodIL(createDefaultInstance.MakeInstantiatedMethod(type));
                }
            }

            return null;
        }

        /// <summary>
        /// Provides method bodies for intrinsics recognized by the compiler.
        /// It can return null if it's not an intrinsic recognized by the compiler,
        /// but an intrinsic e.g. recognized by codegen.
        /// </summary>
        private MethodIL TryGetIntrinsicMethodIL(MethodDesc method)
        {
            var mdType = method.OwningType as MetadataType;
            if (mdType == null)
                return null;

            if (mdType.Name.SequenceEqual("RuntimeHelpers"u8) && mdType.Namespace.SequenceEqual("System.Runtime.CompilerServices"u8))
            {
                return RuntimeHelpersIntrinsics.EmitIL(method);
            }

            if (mdType.Name.SequenceEqual("Unsafe"u8) && mdType.Namespace.SequenceEqual("System.Runtime.CompilerServices"u8))
            {
                return UnsafeIntrinsics.EmitIL(method);
            }

            if (mdType.Name.SequenceEqual("InstanceCalliHelper"u8) && mdType.Namespace.SequenceEqual("System.Reflection"u8))
            {
                return InstanceCalliHelperIntrinsics.EmitIL(method);
            }

            return null;
        }

        /// <summary>
        /// Provides method bodies for intrinsics recognized by the compiler that
        /// are specialized per instantiation. It can return null if the intrinsic
        /// is not recognized.
        /// </summary>
        private MethodIL TryGetPerInstantiationIntrinsicMethodIL(MethodDesc method)
        {
            var mdType = method.OwningType as MetadataType;
            if (mdType == null)
                return null;

            if (mdType.Name.SequenceEqual("RuntimeHelpers"u8) && mdType.Namespace.SequenceEqual("System.Runtime.CompilerServices"u8))
            {
                return RuntimeHelpersIntrinsics.EmitIL(method);
            }

            if (mdType.Name.SequenceEqual("Activator"u8) && mdType.Namespace.SequenceEqual("System"u8))
            {
                return TryGetIntrinsicMethodILForActivator(method);
            }

            if (mdType.Name.SequenceEqual("Interlocked"u8) && mdType.Namespace.SequenceEqual("System.Threading"u8))
            {
                return InterlockedIntrinsics.EmitIL(_compilationModuleGroup, method);
            }

            return null;
        }

        private Dictionary<MethodDesc, MethodIL> _manifestModuleWrappedMethods = new Dictionary<MethodDesc, MethodIL>();

        // Create the cross module inlineable tokens for a method
        // This method is order dependent, and must be called during the single threaded portion of compilation
        public void CreateCrossModuleInlineableTokensForILBody(MethodDesc method)
        {
            Debug.Assert(_manifestMutableModule != null);
            var wrappedMethodIL = new ManifestModuleWrappedMethodIL();

            if (method.IsAsync)
            {
                if (!wrappedMethodIL.Initialize(_manifestMutableModule, GetMethodILForAsyncMethod(method), (EcmaMethod)method, false))
                {
                    // If we could not initialize the wrapped method IL, we should store a null.
                    // That will result in the IL code for the method being unavailable for use in
                    // the compilation, which is version safe.
                    wrappedMethodIL = null;
                }
            }
            else if (method.IsAsyncVariant())
            {
                if (!wrappedMethodIL.Initialize(_manifestMutableModule,
                    AsyncThunkILEmitter.EmitAsyncMethodThunk(method, method.GetTargetOfAsyncVariant()),
                    (EcmaMethod)method.GetTargetOfAsyncVariant(),
                    false))
                {
                    // If we could not initialize the wrapped method IL, we should store a null.
                    // That will result in the IL code for the method being unavailable for use in
                    // the compilation, which is version safe.
                    wrappedMethodIL = null;
                }
            }
            else if (method is AsyncResumptionStub ars)
            {
                if (!wrappedMethodIL.Initialize(
                    _manifestMutableModule,
                    ars.EmitIL(),
                    (EcmaMethod)ars.TargetMethod.GetPrimaryMethodDesc().GetTypicalMethodDefinition(),
                    false))
                {
                    // If we could not initialize the wrapped method IL, we should store a null.
                    // That will result in the IL code for the method being unavailable for use in
                    // the compilation, which is version safe.
                    wrappedMethodIL = null;
                }
            }
            else
            {
                Debug.Assert(!_compilationModuleGroup.VersionsWithMethodBody(method) &&
                        _compilationModuleGroup.CrossModuleInlineable(method));

                if (!wrappedMethodIL.Initialize(_manifestMutableModule, EcmaMethodIL.Create((EcmaMethod)method)))
                {
                    // If we could not initialize the wrapped method IL, we should store a null.
                    // That will result in the IL code for the method being unavailable for use in
                    // the compilation, which is version safe.
                    wrappedMethodIL = null;
                }
            }

            _manifestModuleWrappedMethods.Add(method, wrappedMethodIL);
            IncrementVersion();
        }

        public bool NeedsCrossModuleInlineableTokens(MethodDesc method)
        {
            if (((!_compilationModuleGroup.VersionsWithMethodBody(method)
                    && _compilationModuleGroup.CrossModuleInlineable(method))
                || (NeedsTaskReturningThunk(method) || NeedsAsyncThunk(method) || method is AsyncResumptionStub))
                && !_manifestModuleWrappedMethods.ContainsKey(method))
            {
                return true;
            }
            return false;
        }

        bool NeedsTaskReturningThunk(MethodDesc method)
        {
            if (method is not EcmaMethod ecmaMethod)
                return false;

            if (!method.IsAsync)
                return false;

            if (method.Signature.ReturnsTaskOrValueTask())
                return true;

            if (ecmaMethod.OwningType.Module != ecmaMethod.Context.SystemModule)
                return true;

            return false;
        }

        bool NeedsAsyncThunk(MethodDesc method)
        {
            if (method is not AsyncMethodVariant)
                return false;
            return !method.IsAsync;
        }

        MethodIL GetMethodILForAsyncMethod(MethodDesc method)
        {
            Debug.Assert(method.IsAsync && method is EcmaMethod);
            if (method.Signature.ReturnsTaskOrValueTask())
            {
                return AsyncThunkILEmitter.EmitTaskReturningThunk(method, method.GetAsyncVariant());
            }
            // We only allow non-Task returning runtime async methods in CoreLib
            // Skip this method
            return null;
        }

        public override MethodIL GetMethodIL(MethodDesc method)
        {
            if (method is EcmaMethod ecmaMethod)
            {
                if (method.IsIntrinsic)
                {
                    MethodIL result = TryGetIntrinsicMethodIL(method);
                    if (result != null)
                        return result;
                }

                // Check to see if there is an override for the EcmaMethodIL. If there is not
                // then simply return the EcmaMethodIL. In theory this could call
                // CreateCrossModuleInlineableTokensForILBody, but we explicitly do not want
                // to do that. The reason is that this method is called during the multithreaded
                // portion of compilation, and CreateCrossModuleInlineableTokensForILBody
                // will produce tokens which are order dependent thus violating the determinism
                // principles of the compiler.
                if (_manifestModuleWrappedMethods.TryGetValue(ecmaMethod, out var methodIL))
                    return methodIL;

                return NeedsTaskReturningThunk(ecmaMethod) ?
                    GetMethodILForAsyncMethod(ecmaMethod)
                    : EcmaMethodIL.Create(ecmaMethod);
            }
            else if (method is AsyncMethodVariant amv)
            {
                if (_manifestModuleWrappedMethods.TryGetValue(amv, out var methodIL))
                    return methodIL;

                return NeedsAsyncThunk(amv) ?
                    AsyncThunkILEmitter.EmitAsyncMethodThunk(amv, method.GetTargetOfAsyncVariant())
                    : new AsyncEcmaMethodIL(amv, EcmaMethodIL.Create((EcmaMethod)method.GetTargetOfAsyncVariant()));
            }
            else if (method is MethodForInstantiatedType || method is InstantiatedMethod)
            {
                // Intrinsics specialized per instantiation
                if (method.IsIntrinsic)
                {
                    MethodIL methodIL = TryGetPerInstantiationIntrinsicMethodIL(method);
                    if (methodIL != null)
                        return methodIL;
                }

                var methodDefinitionIL = GetMethodIL(method.GetTypicalMethodDefinition());
                if (methodDefinitionIL == null)
                    return null;
                return new InstantiatedMethodIL(method, methodDefinitionIL);
            }
            else if (method is AsyncResumptionStub ars)
            {
                if (_manifestModuleWrappedMethods.TryGetValue(ars, out var methodil))
                    return methodil;
                CreateCrossModuleInlineableTokensForILBody(ars);
                return _manifestModuleWrappedMethods[ars];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// A MethodIL Provider which provides tokens relative to a MutableModule. Used to implement cross
        /// module inlining of code in ReadyToRun files.
        /// </summary>
        class ManifestModuleWrappedMethodIL : MethodIL, IEcmaMethodIL, IMethodTokensAreUseableInCompilation
        {
            int _maxStack;
            bool _isInitLocals;
            EcmaMethod _owningMethod;
            ILExceptionRegion[] _exceptionRegions;
            byte[] _ilBytes;
            LocalVariableDefinition[] _locals;
            HashSet<object> _methodsWithAsyncVariants;

            MutableModule _mutableModule;

            public ManifestModuleWrappedMethodIL() {}

            public bool Initialize(MutableModule mutableModule, EcmaMethodIL wrappedMethod)
            {
                return Initialize(mutableModule, wrappedMethod, wrappedMethod.OwningMethod, true);
            }

            public bool Initialize(MutableModule mutableModule, MethodIL wrappedMethod, EcmaMethod owningMethod, bool validateStandaloneMetadata)
            {
                HashSet<MethodDesc> methodsWhichCannotHaveAsyncVariants = null;
                _methodsWithAsyncVariants = null;

                if (wrappedMethod == null)
                    return false;

                bool failedToReplaceToken = false;
                try
                {
                    Debug.Assert(mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences == null);
                    mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = owningMethod.Module;
                    var owningMethodHandle = mutableModule.TryGetEntityHandle(owningMethod);
                    if (!owningMethodHandle.HasValue)
                        return false;
                    _mutableModule = mutableModule;
                    _maxStack = wrappedMethod.MaxStack;
                    _isInitLocals = wrappedMethod.IsInitLocals;
                    _owningMethod = owningMethod;
                    _exceptionRegions = (ILExceptionRegion[])wrappedMethod.GetExceptionRegions().Clone();
                    _ilBytes = (byte[])wrappedMethod.GetILBytes().Clone();
                    _locals = (LocalVariableDefinition[])wrappedMethod.GetLocals();

                    for (int i = 0; i < _exceptionRegions.Length; i++)
                    {
                        var region = _exceptionRegions[i];
                        if (region.Kind == ILExceptionRegionKind.Catch)
                        {
                            var newHandle = _mutableModule.TryGetHandle((TypeSystemEntity)wrappedMethod.GetObject(region.ClassToken));
                            if (!newHandle.HasValue)
                            {
                                return false;
                            }
                            _exceptionRegions[i] = new ILExceptionRegion(region.Kind, region.TryOffset, region.TryLength, region.HandlerOffset, region.HandlerLength, newHandle.Value, newHandle.Value);
                        }
                    }

                    ILTokenReplacer.Replace(_ilBytes, GetMutableModuleToken);
#if DEBUG
                    if (validateStandaloneMetadata)
                        Debug.Assert(ReadyToRunStandaloneMethodMetadata.Compute(_owningMethod) != null);
#endif // DEBUG
                }
                finally
                {
                    mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = null;
                }


                return !failedToReplaceToken;

                int GetMutableModuleToken(int token)
                {
                    object result = wrappedMethod.GetObject(token);
                    int? newToken;
                    if (result is string str)
                    {
                        newToken = mutableModule.TryGetStringHandle(str);
                    }
                    else
                    {
                        // Since async thunks directly refer to async methods(which is otherwise not permitted in IL), we need to track this detail
                        // when we replace the tokens, and use tokens for the non-async variant method, but return
                        // the async variant as appropriate.
                        if (result is MethodDesc methodDesc)
                        {
                            if (methodDesc.IsAsyncVariant())
                            {
                                // We actually need to store the non-variant method, and force GetObject
                                // to return the async variant
                                methodDesc = methodDesc.GetTargetOfAsyncVariant();
                                if (_methodsWithAsyncVariants == null)
                                    _methodsWithAsyncVariants = new HashSet<object>();
                                _methodsWithAsyncVariants.Add(methodDesc);
                                result = methodDesc;

                                if (methodsWhichCannotHaveAsyncVariants != null &&
                                    methodsWhichCannotHaveAsyncVariants.Contains(methodDesc))
                                {
                                    // This method cannot refer to both an async thunk and async variant, fail the compile
                                    throw new Exception("Method refers in IL directly to an async variant method and a non-async variant");
                                }
                            }
                            else if (methodDesc.IsAsync)
                            {
                                if (methodsWhichCannotHaveAsyncVariants == null)
                                    methodsWhichCannotHaveAsyncVariants = new HashSet<MethodDesc>();
                                methodsWhichCannotHaveAsyncVariants.Add(methodDesc);
                                if (_methodsWithAsyncVariants != null &&
                                    _methodsWithAsyncVariants.Contains(methodDesc))
                                {
                                    // This method cannot refer to both an async thunk and async variant, fail the compile
                                    throw new Exception("Method refers in IL directly to an async variant method and a non-async variant");
                                }
                            }
                        }
                        newToken = mutableModule.TryGetHandle((TypeSystemEntity)result);
                    }
                    if (!newToken.HasValue)
                    {
                        // Token replacement has failed. Do not attempt to use this IL.
                        failedToReplaceToken = true;
                        return 1;
                    }
                    return newToken.Value;
                }
            }

            public override int MaxStack => _maxStack;

            public override bool IsInitLocals => _isInitLocals;

            public override MethodDesc OwningMethod => _owningMethod;

            public IEcmaModule Module => _mutableModule;

            public override ILExceptionRegion[] GetExceptionRegions() => _exceptionRegions;
            public override byte[] GetILBytes() => _ilBytes;
            public override LocalVariableDefinition[] GetLocals() => _locals;
            public override object GetObject(int token, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw)
            {
                // UserStrings cannot be wrapped in EntityHandle
                if ((token & 0xFF000000) == 0x70000000)
                    return _mutableModule.GetUserString(System.Reflection.Metadata.Ecma335.MetadataTokens.UserStringHandle(token));

                object result = _mutableModule.GetObject(System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle(token), notFoundBehavior);
                if (_methodsWithAsyncVariants != null &&
                    _methodsWithAsyncVariants.Contains(result))
                {
                    // Return the async variant method
                    result = ((MethodDesc)result).GetAsyncVariant();
                }
                return result;
            }
        }

        public sealed class AsyncEcmaMethodIL : MethodIL, IEcmaMethodIL
        {
            private readonly AsyncMethodVariant _variant;
            private readonly EcmaMethodIL _ecmaIL;

            public AsyncEcmaMethodIL(AsyncMethodVariant variant, EcmaMethodIL ecmaIL)
                => (_variant, _ecmaIL) = (variant, ecmaIL);

            // This is the reason we need this class - the method that owns the IL is the variant.
            public override MethodDesc OwningMethod => _variant;

            // Everything else dispatches to EcmaMethodIL
            public override MethodDebugInformation GetDebugInfo() => _ecmaIL.GetDebugInfo();
            public override ILExceptionRegion[] GetExceptionRegions() => _ecmaIL.GetExceptionRegions();
            public override byte[] GetILBytes() => _ecmaIL.GetILBytes();
            public override LocalVariableDefinition[] GetLocals() => _ecmaIL.GetLocals();
            public override object GetObject(int token, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw) => _ecmaIL.GetObject(token, notFoundBehavior);
            public override bool IsInitLocals => _ecmaIL.IsInitLocals;
            public override int MaxStack => _ecmaIL.MaxStack;

            public IEcmaModule Module => _ecmaIL.Module;
        }
    }
}
