// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Thunk that replaces calls to MethodBase.GetCurrentMethod in user code. The purpose of the thunk
    /// is to LDTOKEN the method considered "current method" and call into the class library to
    /// retrieve the associated MethodBase object instance.
    /// </summary>
    internal sealed partial class MethodBaseGetCurrentMethodThunk : ILStubMethod
    {
        public MethodBaseGetCurrentMethodThunk(MethodDesc method)
        {
            Debug.Assert(method.IsTypicalMethodDefinition);

            Method = method;
            Signature = new MethodSignature(MethodSignatureFlags.Static, 0,
                Context.SystemModule.GetKnownType("System.Reflection", "MethodBase"), TypeDesc.EmptyTypes);
        }

        public override TypeSystemContext Context
        {
            get
            {
                return Method.Context;
            }
        }

        public MethodDesc Method
        {
            get;
        }

        public override string Name
        {
            get
            {
                return Method.Name;
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return Method.DiagnosticName;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return Method.OwningType;
            }
        }

        public override MethodSignature Signature
        {
            get;
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            codeStream.Emit(ILOpcode.ldtoken, emit.NewToken(Method));

            string helperName;
            if (Method.OwningType.HasInstantiation)
            {
                codeStream.Emit(ILOpcode.ldtoken, emit.NewToken(Method.OwningType));
                helperName = "GetCurrentMethodGeneric";
            }
            else
                helperName = "GetCurrentMethodNonGeneric";

            MethodDesc classlibHelper = Context.GetHelperEntryPoint("ReflectionHelpers", helperName);

            codeStream.Emit(ILOpcode.call, emit.NewToken(classlibHelper));
            codeStream.Emit(ILOpcode.ret);

            return emit.Link(this);
        }
    }

    internal sealed class MethodBaseGetCurrentMethodThunkCache
    {
        private Unifier _cache;

        public MethodBaseGetCurrentMethodThunkCache()
        {
            _cache = new Unifier();
        }

        public MethodDesc GetHelper(MethodDesc currentMethod)
        {
            return _cache.GetOrCreateValue(currentMethod.GetTypicalMethodDefinition());
        }

        private sealed class Unifier : LockFreeReaderHashtable<MethodDesc, MethodBaseGetCurrentMethodThunk>
        {
            protected override int GetKeyHashCode(MethodDesc key)
            {
                return key.GetHashCode();
            }
            protected override int GetValueHashCode(MethodBaseGetCurrentMethodThunk value)
            {
                return value.Method.GetHashCode();
            }
            protected override bool CompareKeyToValue(MethodDesc key, MethodBaseGetCurrentMethodThunk value)
            {
                return key == value.Method;
            }
            protected override bool CompareValueToValue(MethodBaseGetCurrentMethodThunk value1, MethodBaseGetCurrentMethodThunk value2)
            {
                return value1.Method == value2.Method;
            }
            protected override MethodBaseGetCurrentMethodThunk CreateValueFromKey(MethodDesc key)
            {
                return new MethodBaseGetCurrentMethodThunk(key);
            }
        }
    }
}
