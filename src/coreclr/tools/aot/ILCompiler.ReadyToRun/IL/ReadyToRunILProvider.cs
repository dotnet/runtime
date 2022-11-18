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
                && method.Name == "CreateInstance")
            {
                TypeDesc type = method.Instantiation[0];
                if (type.IsValueType && type.GetParameterlessConstructor() == null)
                {
                    // Replace the body with implementation that just returns "default"
                    MethodDesc createDefaultInstance = method.OwningType.GetKnownMethod("CreateDefaultInstance", method.GetTypicalMethodDefinition().Signature);
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

            if (mdType.Name == "RuntimeHelpers" && mdType.Namespace == "System.Runtime.CompilerServices")
            {
                return RuntimeHelpersIntrinsics.EmitIL(method);
            }

            if (mdType.Name == "Unsafe" && mdType.Namespace == "System.Runtime.CompilerServices")
            {
                return UnsafeIntrinsics.EmitIL(method);
            }

            if (mdType.Name == "MemoryMarshal" && mdType.Namespace == "System.Runtime.InteropServices")
            {
                return MemoryMarshalIntrinsics.EmitIL(method);
            }

            if (mdType.Name == "Volatile" && mdType.Namespace == "System.Threading")
            {
                return VolatileIntrinsics.EmitIL(method);
            }

            if (mdType.Name == "Interlocked" && mdType.Namespace == "System.Threading")
            {
                return InterlockedIntrinsics.EmitIL(_compilationModuleGroup, method);
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

            if (mdType.Name == "RuntimeHelpers" && mdType.Namespace == "System.Runtime.CompilerServices")
            {
                return RuntimeHelpersIntrinsics.EmitIL(method);
            }

            if (mdType.Name == "Activator" && mdType.Namespace == "System")
            {
                return TryGetIntrinsicMethodILForActivator(method);
            }

            return null;
        }

        private Dictionary<EcmaMethod, MethodIL> _manifestModuleWrappedMethods = new Dictionary<EcmaMethod, MethodIL>();

        // Create the cross module inlineable tokens for a method
        // This method is order dependent, and must be called during the single threaded portion of compilation
        public void CreateCrossModuleInlineableTokensForILBody(EcmaMethod method)
        {
            Debug.Assert(_manifestMutableModule != null);
            Debug.Assert(!_compilationModuleGroup.VersionsWithMethodBody(method) &&
                    _compilationModuleGroup.CrossModuleInlineable(method));
            var wrappedMethodIL = new ManifestModuleWrappedMethodIL();
            if (!wrappedMethodIL.Initialize(_manifestMutableModule, EcmaMethodIL.Create(method)))
            {
                // If we could not initialize the wrapped method IL, we should store a null.
                // That will result in the IL code for the method being unavailable for use in
                // the compilation, which is version safe.
                wrappedMethodIL = null;
            }
            _manifestModuleWrappedMethods.Add(method, wrappedMethodIL);
            IncrementVersion();
        }

        public bool NeedsCrossModuleInlineableTokens(EcmaMethod method)
        {
            if (!_compilationModuleGroup.VersionsWithMethodBody(method) &&
                    _compilationModuleGroup.CrossModuleInlineable(method) &&
                    !_manifestModuleWrappedMethods.ContainsKey(method))
            {
                return true;
            }
            return false;
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
                if (!_manifestModuleWrappedMethods.TryGetValue(ecmaMethod, out var methodIL))
                {
                    methodIL = EcmaMethodIL.Create(ecmaMethod);
                }

                if (methodIL != null)
                    return methodIL;

                return null;
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
            MethodDesc _owningMethod;
            ILExceptionRegion[] _exceptionRegions;
            byte[] _ilBytes;
            LocalVariableDefinition[] _locals;

            MutableModule _mutableModule;

            public ManifestModuleWrappedMethodIL() {}
            
            public bool Initialize(MutableModule mutableModule, EcmaMethodIL wrappedMethod)
            {
                bool failedToReplaceToken = false;
                try
                {
                    Debug.Assert(mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences == null);
                    mutableModule.ModuleThatIsCurrentlyTheSourceOfNewReferences = ((EcmaMethod)wrappedMethod.OwningMethod).Module;
                    var owningMethodHandle = mutableModule.TryGetEntityHandle(wrappedMethod.OwningMethod);
                    if (!owningMethodHandle.HasValue)
                        return false;
                    _mutableModule = mutableModule;
                    _maxStack = wrappedMethod.MaxStack;
                    _isInitLocals = wrappedMethod.IsInitLocals;
                    _owningMethod = wrappedMethod.OwningMethod;
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
                    Debug.Assert(ReadyToRunStandaloneMethodMetadata.Compute((EcmaMethod)_owningMethod) != null);
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
                        newToken = mutableModule.TryGetHandle((TypeSystemEntity)result);
                    }
                    if (!newToken.HasValue)
                    {
                        // Toekn replacement has failed. Do not attempt to use this IL.
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

                return _mutableModule.GetObject(System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle(token), notFoundBehavior);
            }
        }
    }
}
