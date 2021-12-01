// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Additional members of MethodDesc related to code generation.
    public abstract partial class MethodDesc
    {
        /// <summary>
        /// Gets a value specifying whether this method is an intrinsic.
        /// This can either be an intrinsic recognized by the compiler,
        /// by the codegen backend, or some other component.
        /// </summary>
        public virtual bool IsIntrinsic
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value specifying whether this method should not be included
        /// into the code of any caller methods by the compiler (and should be kept
        /// as a separate routine).
        /// </summary>
        public virtual bool IsNoInlining
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value specifying whether this method should be included into
        /// the code of the caller methods aggressively.
        /// </summary>
        public virtual bool IsAggressiveInlining
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value specifying whether this method contains hot code and should
        /// be aggressively optimized if possible.
        /// </summary>
        public virtual bool IsAggressiveOptimization
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this method was marked with the
        /// System.Security.DynamicSecurityMethod attribute. For such methods
        /// runtime needs to be able to find their caller, their caller's caller
        /// or the method itself on the call stack using stack walking.
        /// </summary>
        public virtual bool RequireSecObject
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value specifying whether this method should not be optimized.
        /// </summary>
        public virtual bool IsNoOptimization
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value specifying whether the implementation of this method
        /// is provided by the runtime (i.e., through generated IL).
        /// </summary>
        public virtual bool IsRuntimeImplemented
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value specifying whether the implementation of this method is
        /// provided externally by calling out into the runtime.
        /// </summary>
        public virtual bool IsInternalCall
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value specifying whether the implementation of this method is
        /// implicitly synchronized
        /// </summary>
        public virtual bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value specifying whether this method is directly callable
        /// by external unmanaged code.
        /// </summary>
        public virtual bool IsUnmanagedCallersOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value specifying whether this method is an exported managed
        /// entrypoint.
        /// </summary>
        public virtual bool IsRuntimeExport
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value specifying whether this method has special semantics.
        /// The name indicates the semantics.
        /// </summary>
        public virtual bool IsSpecialName
        {
            get
            {
                return false;
            }
        }
    }

    // Additional members of InstantiatedMethod related to code generation.
    public partial class InstantiatedMethod
    {
        public override bool IsIntrinsic
        {
            get
            {
                return _methodDef.IsIntrinsic;
            }
        }

        public override bool IsNoInlining
        {
            get
            {
                return _methodDef.IsNoInlining;
            }
        }

        public override bool IsAggressiveOptimization
        {
            get
            {
                return _methodDef.IsAggressiveOptimization;
            }
        }

        public override bool RequireSecObject
        {
            get
            {
                return _methodDef.RequireSecObject;
            }
        }

        public override bool IsNoOptimization
        {
            get
            {
                return _methodDef.IsNoOptimization;
            }
        }

        public override bool IsAggressiveInlining
        {
            get
            {
                return _methodDef.IsAggressiveInlining;
            }
        }

        public override bool IsRuntimeImplemented
        {
            get
            {
                return _methodDef.IsRuntimeImplemented;
            }
        }

        public override bool IsInternalCall
        {
            get
            {
                return _methodDef.IsInternalCall;
            }
        }

        public override bool IsSynchronized
        {
            get
            {
                return _methodDef.IsSynchronized;
            }
        }

        public override bool IsUnmanagedCallersOnly
        {
            get
            {
                return _methodDef.IsUnmanagedCallersOnly;
            }
        }

        public override bool IsSpecialName
        {
            get
            {
                return _methodDef.IsSpecialName;
            }
        }
    }

    // Additional members of MethodForInstantiatedType related to code generation.
    public partial class MethodForInstantiatedType
    {
        public override bool IsIntrinsic
        {
            get
            {
                return _typicalMethodDef.IsIntrinsic;
            }
        }

        public override bool IsNoInlining
        {
            get
            {
                return _typicalMethodDef.IsNoInlining;
            }
        }

        public override bool IsAggressiveOptimization
        {
            get
            {
                return _typicalMethodDef.IsAggressiveOptimization;
            }
        }

        public override bool RequireSecObject
        {
            get
            {
                return _typicalMethodDef.RequireSecObject;
            }
        }

        public override bool IsNoOptimization
        {
            get
            {
                return _typicalMethodDef.IsNoOptimization;
            }
        }

        public override bool IsAggressiveInlining
        {
            get
            {
                return _typicalMethodDef.IsAggressiveInlining;
            }
        }

        public override bool IsRuntimeImplemented
        {
            get
            {
                return _typicalMethodDef.IsRuntimeImplemented;
            }
        }

        public override bool IsInternalCall
        {
            get
            {
                return _typicalMethodDef.IsInternalCall;
            }
        }

        public override bool IsSynchronized
        {
            get
            {
                return _typicalMethodDef.IsSynchronized;
            }
        }

        public override bool IsUnmanagedCallersOnly
        {
            get
            {
                return _typicalMethodDef.IsUnmanagedCallersOnly;
            }
        }

        public override bool IsSpecialName
        {
            get
            {
                return _typicalMethodDef.IsSpecialName;
            }
        }
    }

    // Additional members of ArrayMethod related to code generation.
    public partial class ArrayMethod
    {
        public override bool IsIntrinsic
        {
            get
            {
                return true;
            }
        }

        public override bool IsNoInlining
        {
            get
            {
                // Do not allow inlining the Address method. The method that actually gets called is
                // the one that has a hidden argument with the expected array type.
                return Kind == ArrayMethodKind.Address;
            }
        }

        public override bool IsInternalCall
        {
            get
            {
                // We consider Address method an internal call since this will end up calling a different
                // method at runtime.
                return Kind == ArrayMethodKind.Address;
            }
        }
    }
}
