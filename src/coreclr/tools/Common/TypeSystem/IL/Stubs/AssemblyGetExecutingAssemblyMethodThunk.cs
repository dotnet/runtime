// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Thunk that replaces calls to Assembly.GetExecutingAssembly in user code. The purpose of the thunk
    /// is to load something that will let us identify the current assembly and call a class library
    /// helper that will let us get the Assembly.
    /// </summary>
    internal partial class AssemblyGetExecutingAssemblyMethodThunk : ILStubMethod
    {
        public AssemblyGetExecutingAssemblyMethodThunk(TypeDesc owningType, IAssemblyDesc executingAssembly)
        {
            OwningType = owningType;
            ExecutingAssembly = executingAssembly;

            TypeSystemContext context = owningType.Context;

            Signature = new MethodSignature(MethodSignatureFlags.Static, 0,
                context.SystemModule.GetKnownType("System.Reflection", "Assembly"), TypeDesc.EmptyTypes);
        }

        public override TypeSystemContext Context
        {
            get
            {
                return OwningType.Context;
            }
        }

        public IAssemblyDesc ExecutingAssembly
        {
            get;
        }

        public override string Name
        {
            get
            {
                return $"GetExecutingAssembly_{ExecutingAssembly.GetName().Name}";
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return $"GetExecutingAssembly_{ExecutingAssembly.GetName().Name}";
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

        public override MethodIL EmitIL()
        {
            ILEmitter emit = new ILEmitter();
            ILCodeStream codeStream = emit.NewCodeStream();

            MethodDesc classlibHelper = Context.GetHelperEntryPoint("ReflectionHelpers", "GetExecutingAssembly");

            // Use the global module type as "a type from the assembly that has metadata"
            // Our reflection policy always makes sure this has metadata.
            MetadataType moduleType = ((ModuleDesc)ExecutingAssembly).GetGlobalModuleType();

            codeStream.Emit(ILOpcode.ldtoken, emit.NewToken(moduleType));
            codeStream.Emit(ILOpcode.call, emit.NewToken(classlibHelper));
            codeStream.Emit(ILOpcode.ret);

            return emit.Link(this);
        }
    }

    internal class AssemblyGetExecutingAssemblyMethodThunkCache
    {
        private TypeDesc _owningTypeForThunks;
        private Unifier _cache;

        public AssemblyGetExecutingAssemblyMethodThunkCache(TypeDesc owningTypeForThunks)
        {
            _owningTypeForThunks = owningTypeForThunks;
            _cache = new Unifier(this);
        }

        public MethodDesc GetHelper(IAssemblyDesc executingAssembly)
        {
            return _cache.GetOrCreateValue(executingAssembly);
        }

        private class Unifier : LockFreeReaderHashtable<IAssemblyDesc, AssemblyGetExecutingAssemblyMethodThunk>
        {
            private AssemblyGetExecutingAssemblyMethodThunkCache _parent;

            public Unifier(AssemblyGetExecutingAssemblyMethodThunkCache parent)
            {
                _parent = parent;
            }

            protected override int GetKeyHashCode(IAssemblyDesc key)
            {
                return key.GetHashCode();
            }
            protected override int GetValueHashCode(AssemblyGetExecutingAssemblyMethodThunk value)
            {
                return value.ExecutingAssembly.GetHashCode();
            }
            protected override bool CompareKeyToValue(IAssemblyDesc key, AssemblyGetExecutingAssemblyMethodThunk value)
            {
                return key == value.ExecutingAssembly;
            }
            protected override bool CompareValueToValue(AssemblyGetExecutingAssemblyMethodThunk value1, AssemblyGetExecutingAssemblyMethodThunk value2)
            {
                return value1.ExecutingAssembly == value2.ExecutingAssembly;
            }
            protected override AssemblyGetExecutingAssemblyMethodThunk CreateValueFromKey(IAssemblyDesc key)
            {
                return new AssemblyGetExecutingAssemblyMethodThunk(_parent._owningTypeForThunks, key);
            }
        }
    }
}
