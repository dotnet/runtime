// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Helps model the behavior of calls to various Type.GetType overloads. Type.GetType is required to search
    /// the calling assembly for a matching type if the type name supplied by the user code was not assembly qualified.
    /// This thunk calls a helper method, passing it a string for what should be considered the "calling assembly".
    /// </summary>
    internal partial class TypeGetTypeMethodThunk : ILStubMethod
    {
        private readonly MethodDesc _helperMethod;

        public TypeGetTypeMethodThunk(TypeDesc owningType, MethodSignature signature, MethodDesc helperMethod, string defaultAssemblyName)
        {
            OwningType = owningType;
            Signature = signature;
            DefaultAssemblyName = defaultAssemblyName;

            _helperMethod = helperMethod;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return OwningType.Context;
            }
        }

        public override string Name
        {
            get
            {
                return $"{_helperMethod.Name}_{Signature.Length}_{DefaultAssemblyName}";
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return $"{_helperMethod.DiagnosticName}_{Signature.Length}_{DefaultAssemblyName}";
            }
        }

        public override TypeDesc OwningType
        {
            get;
        }

        public override MethodSignature Signature
        {
            get;
        }

        public string DefaultAssemblyName
        {
            get;
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            Debug.Assert(Signature[0] == _helperMethod.Signature[0]);
            codeStream.EmitLdArg(0);

            Debug.Assert(_helperMethod.Signature[1].IsString);
            codeStream.Emit(ILOpcode.ldstr, emit.NewToken(DefaultAssemblyName));

            for (int i = 2; i < _helperMethod.Signature.Length; i++)
            {
                // The helper method could be expecting more arguments than what we have - check for that
                // The thunk represents one of the 6 possible overloads:
                // (String), (String, bool), (String, bool, bool)
                // (String, Func<...>, Func<...>), (String, Func<...>, Func<...>, bool), (String, Func<...>, Func<...>, bool, bool)
                // We only need 2 helpers to support all 6 overloads. The default value for the bools is false.

                if (i - 1 < Signature.Length)
                {
                    // Pass user's parameter
                    Debug.Assert(_helperMethod.Signature[i] == Signature[i - 1]);
                    codeStream.EmitLdArg(i - 1);
                }
                else
                {
                    // Pass a default value
                    Debug.Assert(_helperMethod.Signature[i].IsWellKnownType(WellKnownType.Boolean));
                    codeStream.EmitLdc(0);
                }
            }

            codeStream.Emit(ILOpcode.call, emit.NewToken(_helperMethod));
            codeStream.Emit(ILOpcode.ret);

            return emit.Link(this);
        }
    }

    internal class TypeGetTypeMethodThunkCache
    {
        private TypeDesc _owningTypeForThunks;
        private Unifier _cache;

        public TypeGetTypeMethodThunkCache(TypeDesc owningTypeForThunks)
        {
            _owningTypeForThunks = owningTypeForThunks;
            _cache = new Unifier(this);
        }

        public MethodDesc GetHelper(MethodDesc getTypeOverload, string defaultAssemblyName)
        {
            return _cache.GetOrCreateValue(new Key(defaultAssemblyName, getTypeOverload));
        }

        private struct Key
        {
            public readonly string DefaultAssemblyName;
            public readonly MethodDesc GetTypeOverload;

            public Key(string defaultAssemblyName, MethodDesc getTypeOverload)
            {
                DefaultAssemblyName = defaultAssemblyName;
                GetTypeOverload = getTypeOverload;
            }
        }

        private class Unifier : LockFreeReaderHashtable<Key, TypeGetTypeMethodThunk>
        {
            private TypeGetTypeMethodThunkCache _parent;

            public Unifier(TypeGetTypeMethodThunkCache parent)
            {
                _parent = parent;
            }

            protected override int GetKeyHashCode(Key key)
            {
                return key.DefaultAssemblyName.GetHashCode() ^ key.GetTypeOverload.Signature.GetHashCode();
            }
            protected override int GetValueHashCode(TypeGetTypeMethodThunk value)
            {
                return value.DefaultAssemblyName.GetHashCode() ^ value.Signature.GetHashCode();
            }
            protected override bool CompareKeyToValue(Key key, TypeGetTypeMethodThunk value)
            {
                return key.DefaultAssemblyName == value.DefaultAssemblyName &&
                    key.GetTypeOverload.Signature.Equals(value.Signature);
            }
            protected override bool CompareValueToValue(TypeGetTypeMethodThunk value1, TypeGetTypeMethodThunk value2)
            {
                return value1.DefaultAssemblyName == value2.DefaultAssemblyName &&
                    value1.Signature.Equals(value2.Signature);
            }
            protected override TypeGetTypeMethodThunk CreateValueFromKey(Key key)
            {
                TypeSystemContext contex = key.GetTypeOverload.Context;

                // This will be one of the 6 possible overloads:
                // (String), (String, bool), (String, bool, bool)
                // (String, Func<...>, Func<...>), (String, Func<...>, Func<...>, bool), (String, Func<...>, Func<...>, bool, bool)

                // We only need 2 helpers to support this. Use the second parameter to pick the right one.

                string helperName;
                MethodSignature signature = key.GetTypeOverload.Signature;
                if (signature.Length > 1 && signature[1].HasInstantiation)
                    helperName = "ExtensibleGetType";
                else
                    helperName = "GetType";

                MethodDesc helper = contex.GetHelperEntryPoint("ReflectionHelpers", helperName);

                return new TypeGetTypeMethodThunk(_parent._owningTypeForThunks, signature, helper, key.DefaultAssemblyName);
            }
        }
    }
}
