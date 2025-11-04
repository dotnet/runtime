// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System;

namespace Internal.TypeSystem
{
    public sealed partial class MethodSignature
    {
        // Flags that extend MethodSignatureFlags.
        // These are not in metadata, just cached values.
        // They must not conflict with MethodSignatureFlags.
        private static class MethodSignatureFlagsExtensions
        {
            public const MethodSignatureFlags ReturnsTaskOrValueTask = (MethodSignatureFlags)(1 << 30); // Not a real flag, just a cached value
            public const MethodSignatureFlags ReturnsTaskOrValueTaskMask = (MethodSignatureFlags)(1 << 31); // Not a real flag, just a cached value
        }

        public bool ReturnsTaskOrValueTask
        {
            get
            {
                if ((_flags & MethodSignatureFlagsExtensions.ReturnsTaskOrValueTaskMask) == 0)
                {
                    // Compute and cache the value
                    if (ReturnsTaskOrValueTaskCore())
                    {
                        _flags |= MethodSignatureFlagsExtensions.ReturnsTaskOrValueTask;
                    }
                    _flags |= MethodSignatureFlagsExtensions.ReturnsTaskOrValueTaskMask;
                }
                return (_flags & MethodSignatureFlagsExtensions.ReturnsTaskOrValueTask) != 0;
            }
        }

        public MethodSignature CreateAsyncSignature()
        {
            Debug.Assert(!IsAsyncCallConv);
            Debug.Assert(ReturnsTaskOrValueTask);
            TypeDesc md = this.ReturnType;
            MethodSignatureBuilder builder = new MethodSignatureBuilder(this);
            builder.ReturnType = md.HasInstantiation ? md.Instantiation[0] : this.Context.GetWellKnownType(WellKnownType.Void);
            builder.Flags = this.Flags | MethodSignatureFlags.AsyncCallingConvention;
            return builder.ToSignature();
        }

        private bool ReturnsTaskOrValueTaskCore()
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
    }

    public enum AsyncMethodKind
    {
        // Regular methods not returning tasks
        // These are "normal" methods that do not get other variants.
        // Note: Generic T-returning methods are NotAsync, even if T could be a Task.
        NotAsync,

        // Regular methods that return Task/ValueTask
        // Such method has its actual IL body and there also a synthetic variant that is an
        // Async-callable thunk. (AsyncVariantThunk)
        TaskReturning,

        // Task-returning methods marked as MethodImpl::Async in metadata.
        // Such method has a body that is a thunk that forwards to an Async implementation variant
        // which owns the original IL. (AsyncVariantImpl)
        RuntimeAsync,

        //=============================================================
        // On {TaskReturning, AsyncVariantThunk} and {RuntimeAsync, AsyncVariantImpl} pairs:
        //
        // When we see a Task-returning method we create 2 method variants that logically match the same method definition.
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

    public abstract partial class MethodDesc : TypeSystemEntity
    {
        public virtual AsyncMethodKind AsyncMethodKind
        {
            get
            {
                return AsyncMethodKind.NotAsync;
            }
        }

        public bool IsTaskReturning
        {
            get
            {
                return Signature.ReturnsTaskOrValueTask;
            }
        }

        public bool IsAsyncVariant
        {
            get
            {
                return AsyncMethodKind is
                AsyncMethodKind.AsyncVariantImpl or
                AsyncMethodKind.AsyncVariantThunk;
            }
        }

        /// <summary>
        /// Is this synthetic Task/async adapter to an async/Task implementation?
        /// If yes, the method has another variant, which has the actual user-defined method body.
        /// </summary>
        public bool IsAsyncThunk
        {
            get
            {
                return AsyncMethodKind is
                    AsyncMethodKind.AsyncVariantThunk or
                    AsyncMethodKind.RuntimeAsync;
            }
        }

        /// <summary>
        /// Is this method callable as an async method? (i.e. uses Async calling convention)
        /// </summary>
        public bool IsAsyncCallConv
        {
            get
            {
                return AsyncMethodKind is
                AsyncMethodKind.AsyncVariantImpl or
                AsyncMethodKind.AsyncVariantThunk or
                AsyncMethodKind.AsyncExplicitImpl;
            }
        }
    }
}
