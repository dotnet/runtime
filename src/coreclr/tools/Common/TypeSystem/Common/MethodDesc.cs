// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Internal.NativeFormat;

namespace Internal.TypeSystem
{
    [Flags]
    public enum MethodSignatureFlags
    {
        None = 0x0000,

        UnmanagedCallingConventionMask       = 0x000F,
        UnmanagedCallingConventionCdecl      = 0x0001,
        UnmanagedCallingConventionStdCall    = 0x0002,
        UnmanagedCallingConventionThisCall   = 0x0003,
        CallingConventionVarargs             = 0x0005,
        UnmanagedCallingConvention           = 0x0009,

        Static = 0x0010,
        ExplicitThis = 0x0020,
        AsyncCallConv = 0x0040,
    }

    public enum EmbeddedSignatureDataKind
    {
        RequiredCustomModifier = 0,
        OptionalCustomModifier = 1,
        ArrayShape = 2,
        UnmanagedCallConv = 3,
    }

    public struct EmbeddedSignatureData
    {
        public string index;
        public EmbeddedSignatureDataKind kind;
        public TypeDesc type;
    }

    /// <summary>
    /// Represents the parameter types, the return type, and flags of a method.
    /// </summary>
    public sealed partial class MethodSignature : TypeSystemEntity, IEquatable<MethodSignature>
    {
        internal MethodSignatureFlags _flags;
        internal int _genericParameterCount;
        internal TypeDesc _returnType;
        internal TypeDesc[] _parameters;
        internal EmbeddedSignatureData[] _embeddedSignatureData;

        // Value of <see cref="EmbeddedSignatureData.index" /> for any custom modifiers on the return type
        public const string IndexOfCustomModifiersOnReturnType = "0.1.1.1";

        // Value of <see cref="EmbeddedSignatureData.index" /> for any custom modifiers on
        // SomeStruct when SomeStruct *, or SomeStruct & is the type of a parameter or return type
        // Parameter index 0 represents the return type, and indices 1-n represent the parameters to the signature
        public static string GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(int parameterIndex)
        {
            return $"0.1.1.2.{(parameterIndex + 1).ToStringInvariant()}.1";
        }

        // Provide a means to create a MethodSignature which ignores EmbeddedSignature data in the MethodSignatures it is compared to
        public static EmbeddedSignatureData[] EmbeddedSignatureMismatchPermittedFlag = Array.Empty<EmbeddedSignatureData>();

        public MethodSignature(MethodSignatureFlags flags, int genericParameterCount, TypeDesc returnType, TypeDesc[] parameters, EmbeddedSignatureData[] embeddedSignatureData = null)
        {
            _flags = flags;
            _genericParameterCount = genericParameterCount;
            _returnType = returnType;
            _parameters = parameters;
            _embeddedSignatureData = embeddedSignatureData;

            Debug.Assert(parameters != null, "Parameters must not be null");
        }

        public MethodSignature ApplySubstitution(Instantiation substitution)
        {
            if (substitution.IsNull)
                return this;

            bool needsNewMethodSignature = false;
            TypeDesc[] newParameters = _parameters; // Re-use existing array until conflict appears
            TypeDesc returnTypeNew = _returnType.InstantiateSignature(substitution, default(Instantiation));
            if (returnTypeNew != _returnType)
            {
                needsNewMethodSignature = true;
                newParameters = (TypeDesc[])_parameters.Clone();
            }

            for (int i = 0; i < newParameters.Length; i++)
            {
                TypeDesc newParameter = newParameters[i].InstantiateSignature(substitution, default(Instantiation));
                if (newParameter != newParameters[i])
                {
                    if (!needsNewMethodSignature)
                    {
                        needsNewMethodSignature = true;
                        newParameters = (TypeDesc[])_parameters.Clone();
                    }
                    newParameters[i] = newParameter;
                }
            }

            if (needsNewMethodSignature)
            {
                return new MethodSignature(_flags, _genericParameterCount, returnTypeNew, newParameters, _embeddedSignatureData);
            }
            else
            {
                return this;
            }
        }

        public MethodSignatureFlags Flags
        {
            get
            {
                return _flags;
            }
        }

        public bool IsStatic
        {
            get
            {
                return (_flags & MethodSignatureFlags.Static) != 0;
            }
        }

        public bool IsExplicitThis
        {
            get
            {
                return (_flags & MethodSignatureFlags.ExplicitThis) != 0;
            }
        }

        public bool IsAsyncCallConv
        {
            get
            {
                return (_flags & MethodSignatureFlags.AsyncCallConv) != 0;
            }
        }

        public int GenericParameterCount
        {
            get
            {
                return _genericParameterCount;
            }
        }

        public TypeDesc ReturnType
        {
            get
            {
                return _returnType;
            }
        }

        /// <summary>
        /// Gets the parameter type at the specified index.
        /// </summary>
        [IndexerName("Parameter")]
        public TypeDesc this[int index]
        {
            get
            {
                return _parameters[index];
            }
        }

        /// <summary>
        /// Gets the number of parameters of this method signature.
        /// </summary>
        public int Length
        {
            get
            {
                return _parameters.Length;
            }
        }

        public bool HasEmbeddedSignatureData
        {
            get
            {
                return _embeddedSignatureData != null && _embeddedSignatureData.Length != 0;
            }
        }

        public bool EmbeddedSignatureMismatchPermitted
        {
            get
            {
                return _embeddedSignatureData == EmbeddedSignatureMismatchPermittedFlag;
            }
        }

        public EmbeddedSignatureData[] GetEmbeddedSignatureData()
        {
            return GetEmbeddedSignatureData(default);
        }

        public EmbeddedSignatureData[] GetEmbeddedSignatureData(ReadOnlySpan<EmbeddedSignatureDataKind> kinds)
        {
            if ((_embeddedSignatureData == null) || (_embeddedSignatureData.Length == 0))
                return null;

            if (kinds.IsEmpty)
                return (EmbeddedSignatureData[])_embeddedSignatureData.Clone();

            List<EmbeddedSignatureData> ret = new();
            foreach (var data in _embeddedSignatureData)
            {
                foreach (var k in kinds)
                {
                    if (data.kind == k)
                    {
                        ret.Add(data);
                        break;
                    }
                }
            }
            return ret.ToArray();
        }

        public bool ReturnsTaskOrValueTask()
        {
            TypeDesc ret = this.ReturnType;

            if (ret is MetadataType md
                && md.Module == this.Context.SystemModule
                && md.Namespace.SequenceEqual("System.Threading.Tasks"u8))
            {
                ReadOnlySpan<byte> name = md.Name;
                if (name.SequenceEqual("Task"u8) || name.SequenceEqual("Task`1"u8)
                    || name.SequenceEqual("ValueTask"u8) || name.SequenceEqual("ValueTask`1"u8))
                {
                    return true;
                }
            }
            return false;
        }

        public MethodSignature CreateAsyncSignature()
        {
            Debug.Assert(!IsAsyncCallConv);
            Debug.Assert(ReturnsTaskOrValueTask());
            MetadataType md = (MetadataType)this.ReturnType;
            MethodSignatureBuilder builder = new MethodSignatureBuilder(this);
            builder.ReturnType = md.HasInstantiation ? md.Instantiation[0] : this.Context.GetWellKnownType(WellKnownType.Void);
            builder.Flags = this.Flags | MethodSignatureFlags.AsyncCallConv;
            return builder.ToSignature();
        }

        public bool Equals(MethodSignature otherSignature)
        {
            return Equals(otherSignature, allowCovariantReturn: false, allowEquivalence: false);
        }

        public bool EquivalentWithCovariantReturnType(MethodSignature otherSignature)
        {
            return Equals(otherSignature, allowCovariantReturn: true, allowEquivalence: true);
        }

        public bool EquivalentTo(MethodSignature otherSignature)
        {
            return Equals(otherSignature, allowCovariantReturn: false, allowEquivalence: true, visited: null);
        }

        internal bool EquivalentTo(MethodSignature otherSignature, StackOverflowProtect visited)
        {
            return Equals(otherSignature, allowCovariantReturn: false, allowEquivalence: true, visited: visited);
        }

        private bool Equals(MethodSignature otherSignature, bool allowCovariantReturn, bool allowEquivalence, StackOverflowProtect visited = null)
        {
            if (this._flags != otherSignature._flags)
                return false;

            if (this._genericParameterCount != otherSignature._genericParameterCount)
                return false;

            if (!IsTypeEqualHelper(this._returnType, otherSignature._returnType, allowEquivalence, visited))
            {
                if (!allowCovariantReturn)
                    return false;

                if (!otherSignature._returnType.IsCompatibleWith(this._returnType, visited))
                    return false;
            }

            if (this._parameters.Length != otherSignature._parameters.Length)
                return false;

            for (int i = 0; i < this._parameters.Length; i++)
            {
                if (!IsTypeEqualHelper(this._parameters[i], otherSignature._parameters[i], allowEquivalence, visited))
                    return false;
            }

            if (this._embeddedSignatureData == null && otherSignature._embeddedSignatureData == null)
            {
                return true;
            }

            // Array methods do not need to have matching details for the array parameters they support
            if (this.EmbeddedSignatureMismatchPermitted ||
                otherSignature.EmbeddedSignatureMismatchPermitted)
            {
                return true;
            }

            if (this._embeddedSignatureData != null && otherSignature._embeddedSignatureData != null)
            {
                if (this._embeddedSignatureData.Length != otherSignature._embeddedSignatureData.Length)
                {
                    return false;
                }

                for (int i = 0; i < this._embeddedSignatureData.Length; i++)
                {
                    ref EmbeddedSignatureData thisData = ref this._embeddedSignatureData[i];
                    ref EmbeddedSignatureData otherData = ref otherSignature._embeddedSignatureData[i];

                    if (thisData.index != otherData.index ||
                        thisData.kind != otherData.kind ||
                        !IsTypeEqualHelper(thisData.type, otherData.type, allowEquivalence, visited))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;

            static bool IsTypeEqualHelper(TypeDesc type1, TypeDesc type2, bool allowEquivalence, StackOverflowProtect visited)
            {
                if (type1 == type2)
                    return true;

                if (allowEquivalence)
                {
                    if (type1.IsEquivalentTo(type2, visited))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is MethodSignature && Equals((MethodSignature)obj);
        }

        public override int GetHashCode()
        {
            return TypeHashingAlgorithms.ComputeMethodSignatureHashCode(_returnType.GetHashCode(), _parameters);
        }

        public SignatureEnumerator GetEnumerator()
        {
            return new SignatureEnumerator(this);
        }

        public override TypeSystemContext Context => _returnType.Context;

        public struct SignatureEnumerator
        {
            private int _index;
            private MethodSignature _signature;

            public SignatureEnumerator(MethodSignature signature)
            {
                _signature = signature;
                _index = -1;
            }

            public TypeDesc Current => _signature[_index];

            public bool MoveNext()
            {
                _index++;
                return _index < _signature.Length;
            }
        }
    }

    /// <summary>
    /// Information about the runtime async implementation of the method. For runtime async methods, the signature may differ from what is in the
    /// </summary>
    public struct AsyncMethodData
    {
        public AsyncMethodKind Kind;
        public MethodSignature Signature;

        /// <summary>
        /// Is this an Async variant method?
        /// If yes, the method has another Task-returning variant.
        /// </summary>
        public bool IsAsyncVariant => Kind is
                AsyncMethodKind.AsyncVariantImpl or
                AsyncMethodKind.AsyncVariantThunk;
        /// <summary>
        /// Is this a small(ish) synthetic Task/async adapter to an async/Task implementation?
        /// If yes, the method has another variant, which has the actual user-defined method body.
        /// </summary>
        public bool IsThunk
        {
            get
            {
                return Kind is
                    AsyncMethodKind.AsyncVariantThunk or
                    AsyncMethodKind.RuntimeAsync;
            }
        }
        /// <summary>
        /// Is this method callable as an async method? (i.e. uses Async calling convention)
        /// </summary>
        public bool IsAsyncCallConv => Kind is
                AsyncMethodKind.AsyncVariantImpl or
                AsyncMethodKind.AsyncVariantThunk or
                AsyncMethodKind.AsyncExplicitImpl;
    }

    public enum AsyncMethodKind
    {
        // Regular methods not returning tasks
        // These are "normal" methods that do not get other variants.
        // Note: Generic T-returning methods are NotAsync, even if T could be a Task.
        NotAsync,

        // Regular methods that return Task/ValueTask
        // Such method has its actual IL body and there also a synthetic variant that is an
        // Async-callable think. (AsyncVariantThunk)
        TaskReturning,

        // Task-returning methods marked as MethodImpl::Async in metadata.
        // Such method has a body that is a thunk that forwards to an Async implementation variant
        // which owns the original IL. (AsyncVariantImpl)
        RuntimeAsync,

        //=============================================================
        // On {TaskReturning, AsyncVariantThunk} and {RuntimeAsync, AsyncVariantImpl} pairs:
        //
        // When we see a Task-returning method we create 2 method varaints that logically match the same method definition.
        // One variant has the same signature/callconv as the defining method and another is a matching Async variant.
        // Depending on whether the definition was a runtime async method or an ordinary Task-returning method,
        // the IL body belongs to one of the variants and another variant is a synthetic thunk.
        //
        // The signature of the Async variant is derived from the original signature by replacing Task return type with
        // modreq'd element type:
        //   Example: "Task<int> Foo();"  ===> "modreq(Task`) int Foo();"
        //   Example: "ValueTask Bar();"  ===> "modreq(ValueTask) void Bar();"
        //
        // The reason for this encoding is that:
        //   - it uses parts of original signature, as-is, thus does not need to look for or construct anything
        //   - it "unwraps" the element type.
        //   - it is reversible. In particular nonconflicting signatures will map to nonconflicting ones.
        //
        // Async methods are called with CORINFO_CALLCONV_ASYNCCALL call convention.
        //
        // It is possible to get from one variant to another via GetAsyncOtherVariant.
        //
        // NOTE: not all Async methods are "variants" from a pair, see AsyncExplicitImpl below.
        //=============================================================

        // The following methods use special calling convention (CORINFO_CALLCONV_ASYNCCALL)
        // These methods are emitted by the JIT as resumable state machines and also take an extra
        // parameter and extra return - the continuation object.

        // Async methods with actual IL implementation of a MethodImpl::Async method.
        AsyncVariantImpl,

        // Async methods with synthetic bodies that forward to a TaskReturning method.
        AsyncVariantThunk,

        // Methods that are explicitly declared as Async in metadata while not Task returning.
        // This is a special case used in a few infrastructure methods like `Await`.
        // Such methods do not get non-Async variants/thunks and can only be called from another Async method.
        // NOTE: These methods have the original signature and it is not possible to tell if the method is Async
        //       from the signature alone, thus all these methods are also JIT intrinsics.
        AsyncExplicitImpl,
    }

    /// <summary>
    /// Helper structure for building method signatures by cloning an existing method signature.
    /// </summary>
    /// <remarks>
    /// This can potentially avoid array allocation costs for allocating the parameter type list.
    /// </remarks>
    public struct MethodSignatureBuilder
    {
        private MethodSignature _template;
        private MethodSignatureFlags _flags;
        private int _genericParameterCount;
        private TypeDesc _returnType;
        private TypeDesc[] _parameters;
        private EmbeddedSignatureData[] _embeddedSignatureData;

        public MethodSignatureBuilder(MethodSignature template)
        {
            _template = template;

            _flags = template._flags;
            _genericParameterCount = template._genericParameterCount;
            _returnType = template._returnType;
            _parameters = template._parameters;
            _embeddedSignatureData = template._embeddedSignatureData;
        }

        public MethodSignatureFlags Flags
        {
            set
            {
                _flags = value;
            }
        }

        public TypeDesc ReturnType
        {
            set
            {
                _returnType = value;
            }
        }

        [System.Runtime.CompilerServices.IndexerName("Parameter")]
        public TypeDesc this[int index]
        {
            set
            {
                if (_parameters[index] == value)
                    return;

                if (_template != null && _parameters == _template._parameters)
                {
                    TypeDesc[] parameters = new TypeDesc[_parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                        parameters[i] = _parameters[i];
                    _parameters = parameters;
                }
                _parameters[index] = value;
            }
        }

        public int Length
        {
            set
            {
                _parameters = new TypeDesc[value];
                _template = null;
            }
        }

        public void SetEmbeddedSignatureData(EmbeddedSignatureData[] embeddedSignatureData)
        {
            _embeddedSignatureData = embeddedSignatureData;
        }

        public MethodSignature ToSignature()
        {
            if (_template == null ||
                _flags != _template._flags ||
                _genericParameterCount != _template._genericParameterCount ||
                _returnType != _template._returnType ||
                _parameters != _template._parameters ||
                _embeddedSignatureData != _template._embeddedSignatureData)
            {
                _template = new MethodSignature(_flags, _genericParameterCount, _returnType, _parameters, _embeddedSignatureData);
            }

            return _template;
        }
    }

    /// <summary>
    /// Represents the fundamental base type for all methods within the type system.
    /// </summary>
    public abstract partial class MethodDesc : TypeSystemEntity
    {
#pragma warning disable CA1825 // avoid Array.Empty<T>() instantiation for TypeLoader
        public static readonly MethodDesc[] EmptyMethods = new MethodDesc[0];
#pragma warning restore CA1825

        private int _hashcode;

        /// <summary>
        /// Allows a performance optimization that skips the potentially expensive
        /// construction of a hash code if a hash code has already been computed elsewhere.
        /// Use to allow objects to have their hashcode computed
        /// independently of the allocation of a MethodDesc object
        /// For instance, compute the hashcode when looking up the object,
        /// then when creating the object, pass in the hashcode directly.
        /// The hashcode specified MUST exactly match the algorithm implemented
        /// on this type normally.
        /// </summary>
        protected void SetHashCode(int hashcode)
        {
            _hashcode = hashcode;
            Debug.Assert(hashcode == ComputeHashCode());
        }

        public sealed override int GetHashCode()
        {
            if (_hashcode != 0)
                return _hashcode;

            return AcquireHashCode();
        }

        private int AcquireHashCode()
        {
            _hashcode = ComputeHashCode();
            return _hashcode;
        }

        /// <summary>
        /// Compute HashCode. This hashcode is persisted into the image.
        /// The algorithm to compute it must be in sync with the one used at runtime.
        /// </summary>
        protected virtual int ComputeHashCode()
        {
            return OwningType.GetHashCode() ^ VersionResilientHashCode.NameHashCode(Name);
        }

        public override bool Equals(object o)
        {
            // Its only valid to compare two MethodDescs in the same context
            Debug.Assert(o is not MethodDesc || ReferenceEquals(((MethodDesc)o).Context, this.Context));
            return ReferenceEquals(this, o);
        }

        /// <summary>
        /// Gets the type that owns this method. This will be a <see cref="DefType"/> or
        /// an <see cref="ArrayType"/>.
        /// </summary>
        public abstract TypeDesc OwningType
        {
            get;
        }

        /// <summary>
        /// Gets the signature of the method.
        /// </summary>
        public abstract MethodSignature Signature
        {
            get;
        }

        /// <summary>
        /// Gets the generic instantiation information of this method.
        /// For generic definitions, retrieves the generic parameters of the method.
        /// For generic instantiation, retrieves the generic arguments of the method.
        /// </summary>
        public virtual Instantiation Instantiation
        {
            get
            {
                return Instantiation.Empty;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this method has a generic instantiation.
        /// This will be true for generic method instantiations and generic definitions.
        /// </summary>
        public bool HasInstantiation
        {
            get
            {
                return this.Instantiation.Length != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this method is an instance constructor.
        /// </summary>
        public bool IsConstructor
        {
            get
            {
                // TODO: Precise check
                // TODO: Cache?
                return this.Name.SequenceEqual(".ctor"u8);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a public parameterless instance constructor
        /// on a non-abstract type.
        /// </summary>
        public virtual bool IsDefaultConstructor
        {
            get
            {
                return OwningType.GetDefaultConstructor() == this;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this method is a static constructor.
        /// </summary>
        public virtual bool IsStaticConstructor
        {
            get
            {
                return this == this.OwningType.GetStaticConstructor();
            }
        }

        /// <summary>
        /// Gets the name of the method as specified in the metadata.
        /// </summary>
        public virtual ReadOnlySpan<byte> Name
        {
            get
            {
                return [];
            }
        }

        public string GetName()
        {
            return System.Text.Encoding.UTF8.GetString(Name
#if NETSTANDARD
                .ToArray()
#endif
                );
        }

        /// <summary>
        /// Gets a value indicating whether the method is virtual.
        /// </summary>
        public virtual bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this virtual method should not override any
        /// virtual methods defined in any of the base classes.
        /// </summary>
        public virtual bool IsNewSlot
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this virtual method needs to be overridden
        /// by all non-abstract classes deriving from the method's owning type.
        /// </summary>
        public virtual bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating that this method cannot be overridden.
        /// </summary>
        public virtual bool IsFinal
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsPublic
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsAsync
        {
            get
            {
                return false;
            }
        }

        public virtual AsyncMethodData AsyncMethodData
        {
            get
            {
                return new AsyncMethodData { Kind = AsyncMethodKind.NotAsync, Signature = Signature };
            }
        }

        /// <summary>
        /// Whether the return type is Task/ValueTask or a generic Task{T}/ValueTask{T}.
        /// Note this is different than the Runtime MethodDesc::IsTaskReturning which returns true
        /// if the method returns a Task/ValueTask object and is not asyncCallConv.
        /// </summary>
        public bool IsTaskReturning
        {
            get
            {
                // Not NotAsync or AsyncExplicitImpl
                return AsyncMethodData.Kind is
                    AsyncMethodKind.TaskReturning or
                    AsyncMethodKind.RuntimeAsync or
                    AsyncMethodKind.AsyncVariantImpl or
                    AsyncMethodKind.AsyncVariantThunk;
            }
        }

        /// <summary>
        /// If the method is an async variant (Task-returning or async-callable),
        /// Only valid to call if IsTaskReturning is true.
        /// </summary>
        public virtual MethodDesc GetAsyncOtherVariant()
        {
            // This base implementation really should never be called.
            // Derived types should override it, and callers should check IsTaskReturning first.
            throw new InvalidOperationException();
        }

        public abstract bool HasCustomAttribute(string attributeNamespace, string attributeName);

        /// <summary>
        /// Retrieves the uninstantiated form of the method on the method's <see cref="OwningType"/>.
        /// For generic methods, this strips method instantiation. For non-generic methods, returns 'this'.
        /// To also strip instantiation of the owning type, use <see cref="GetTypicalMethodDefinition"/>.
        /// </summary>
        public virtual MethodDesc GetMethodDefinition()
        {
            return this;
        }

        /// <summary>
        /// Gets a value indicating whether this is a method definition. This property
        /// is true for non-generic methods and for uninstantiated generic methods.
        /// </summary>
        public bool IsMethodDefinition
        {
            get
            {
                return GetMethodDefinition() == this;
            }
        }

        /// <summary>
        /// Retrieves the generic definition of the method on the generic definition of the owning type.
        /// To only uninstantiate the method without uninstantiating the owning type, use <see cref="GetMethodDefinition"/>.
        /// </summary>
        public virtual MethodDesc GetTypicalMethodDefinition()
        {
            return this;
        }

        /// <summary>
        /// Gets a value indicating whether this is a typical definition. This property is true
        /// if neither the owning type, nor the method are instantiated.
        /// </summary>
        public bool IsTypicalMethodDefinition
        {
            get
            {
                return GetTypicalMethodDefinition() == this;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is an uninstantiated generic method.
        /// </summary>
        public bool IsGenericMethodDefinition
        {
            get
            {
                return HasInstantiation && IsMethodDefinition;
            }
        }

        public bool IsFinalizer
        {
            get
            {
                TypeDesc owningType = OwningType;
                return (owningType.IsObject && Name.SequenceEqual("Finalize"u8)) || (owningType.HasFinalizer && owningType.GetFinalizer() == this);
            }
        }

        public virtual MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            Instantiation instantiation = Instantiation;
            TypeDesc[] clone = null;

            for (int i = 0; i < instantiation.Length; i++)
            {
                TypeDesc uninst = instantiation[i];
                TypeDesc inst = uninst.InstantiateSignature(typeInstantiation, methodInstantiation);
                if (inst != uninst)
                {
                    if (clone == null)
                    {
                        clone = new TypeDesc[instantiation.Length];
                        for (int j = 0; j < clone.Length; j++)
                        {
                            clone[j] = instantiation[j];
                        }
                    }
                    clone[i] = inst;
                }
            }

            MethodDesc method = this;

            TypeDesc owningType = method.OwningType;
            TypeDesc instantiatedOwningType = owningType.InstantiateSignature(typeInstantiation, methodInstantiation);
            if (owningType != instantiatedOwningType)
            {
                method = Context.GetMethodForInstantiatedType(method.GetTypicalMethodDefinition(), (InstantiatedType)instantiatedOwningType);
                if (clone == null && instantiation.Length != 0)
                    return Context.GetInstantiatedMethod(method, instantiation);
            }

            return (clone == null) ? method : Context.GetInstantiatedMethod(method.GetMethodDefinition(), new Instantiation(clone));
        }
    }
}
