// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    /// <summary>
    /// Class used to create wrappers for interoperability with the Objective-C runtime.
    /// </summary>
    [SupportedOSPlatform("macos")]
    public partial abstract class Wrappers
    {
        /// <summary>
        /// Register the associated instances with one another.
        /// </summary>
        /// <param name="instance">The Objective-C instance</param>
        /// <param name="typeAssociation">A strong type mapping to the <paramref name="instance"/>.</param>
        /// <param name="obj">The managed object</param>
        /// <param name="flags">Flags to help with registration</param>
        /// <remarks>
        /// Called when:
        ///   - When an Objective-C projected type is created in managed code (e.g. new NSObject()).
        ///   - When a .NET defined Objective-C type is created in managed code (e.g. new DotNetObject()).
        ///   - When a .NET defined Objective-C type is created in Objective-C code (e.g. [[DotNetObject alloc] init]).
        ///
        /// The supplied <paramref name="typeAssociation"/> is required to inherit from <see cref="ObjectiveCBase"/>.
        /// </remarks>
        public void RegisterInstanceWithObject(
            IntPtr instance,
            Type typeAssociation,
            ObjectiveCBase obj,
            RegisterInstanceFlags flags)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get or create a managed wrapper for the supplied Objective-C object.
        /// </summary>
        /// <param name="instance">An Objective-C object</param>
        /// <param name="typeAssociation">A strong type mapping to the <paramref name="instance"/>.</param>
        /// <param name="flags">Flags for creation</param>
        /// <returns>A managed wrapper</returns>
        /// <remarks>
        /// Called when:
        ///   - An Objective-C instance enters the managed environment.
        ///
        /// The supplied <paramref name="typeAssociation"/> is required to inherit from <see cref="ObjectiveCBase"/>.
        /// </remarks>
        /// <see cref="CreateObject(IntPtr, Type , CreateObjectFlags)"/>
        public object GetOrCreateObjectForInstance(IntPtr instance, Type typeAssociation, CreateObjectFlags flags)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called by <see cref="GetOrCreateObjectForInstance(IntPtr, Type, CreateObjectFlags)"/>.
        /// </summary>
        /// <param name="instance">An Objective-C instance</param>
        /// <param name="typeAssociation">A strong type mapping to the <paramref name="instance"/>.</param>
        /// <param name="flags">Flags for creation</param>
        /// <returns>A managed wrapper</returns>
        /// <remarks>
        /// Called when:
        ///   - The instance has no currently associated managed object.
        /// </remarks>
        protected abstract ObjectiveCBase CreateObject(IntPtr instance, Type typeAssociation, CreateObjectFlags flags);

        /// <summary>
        /// Get a callback to call when checking for unmanaged references.
        /// </summary>
        /// <param name="isManagedRegistration">Boolean indicating the callback is for a managed registered instance.</param>
        /// <returns>An unmanaged callback</returns>
        /// <remarks>
        /// Overriding this method provides a mechanism to override the default reference check callback.
        ///
        /// The returned callback in C could be defined as below. The argument
        /// is the Objective-C instance.
        /// <code>
        /// int ref_callback(void* id)
        /// {
        ///    return 0; // Return zero for no reference or 1 for reference
        /// }
        /// </code>
        /// </remarks>
        /// <see cref="RegisterInstanceFlags.ManagedDefinition"/>
        protected unsafe virtual delegate* unmanaged[Cdecl]<IntPtr, int> GetReferenceCallback(bool isManagedRegistration) => null;

        /// <summary>
        /// Get the associated Objective-C instance.
        /// </summary>
        /// <param name="obj">Managed wrapper base</param>
        /// <returns>The Objective-C instance</returns>
        /// <remarks>
        /// Called when:
        ///   - Passing an object to the Objective-C runtime when created by an unknown <see cref="Wrappers"/> implementation.
        /// </remarks>
        public IntPtr GetInstanceFromObject(ObjectiveCBase obj)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Separate the object wrapper from the underlying Objective-C instance.
        /// </summary>
        /// <param name="wrapper">Managed wrapper</param>
        /// <remarks>
        /// Called when:
        ///   - The managed object should be separated from its Objective-C instance.
        /// </remarks>
        public void SeparateObjectFromInstance(ObjectiveCBase wrapper)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Register the current wrappers instance as the global one for the system.
        /// </summary>
        /// <remarks>
        /// Registering as the global instance will call this implementation's
        /// <see cref="GetMessageSendCallbacks(out IntPtr, out IntPtr, out IntPtr, out IntPtr, out IntPtr)"/> and
        /// pass the pointers to the runtime to override the resolved pointers.
        /// </remarks>
        public void RegisterAsGlobal()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get function pointers for Objective-C runtime message passing.
        /// </summary>
        /// <remarks>
        /// Providing these overrides can enable support for Objective-C
        /// exception propagation and variadic argument support.
        ///
        /// Allows the implementer of the global Objective-C wrapper class
        /// to provide overrides to the 'objc_msgSend*' APIs for the
        /// Objective-C runtime.
        /// </remarks>
        public abstract void GetMessageSendCallbacks(
            out IntPtr objc_msgSend,
            out IntPtr objc_msgSend_fpret,
            out IntPtr objc_msgSend_stret,
            out IntPtr objc_msgSendSuper,
            out IntPtr objc_msgSendSuper_stret);

        /// <summary>
        /// Get the lifetime and memory management functions for all managed
        /// type definitions that are projected into the Objective-C environment.
        /// </summary>
        /// <param name="allocImpl">Alloc implementation</param>
        /// <param name="deallocImpl">Dealloc implementation</param>
        /// <remarks>
        /// See <see href="https://developer.apple.com/documentation/objectivec/nsobject/1571958-alloc">alloc</see>.
        /// See <see href="https://developer.apple.com/documentation/objectivec/nsobject/1571947-dealloc">dealloc</see>.
        /// </remarks>
        public static void GetLifetimeMethods(
            out IntPtr allocImpl,
            out IntPtr deallocImpl)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create an Objective-C Block for the supplied Delegate.
        /// </summary>
        /// <param name="instance">A Delegate to wrap</param>
        /// <param name="flags">Flags for creation</param>
        /// <returns>An Objective-C Block</returns>
        /// <see cref="GetBlockInvokeAndSignature(Delegate, CreateBlockFlags, out string)"/>
        public BlockLiteral CreateBlockForDelegate(Delegate instance, CreateBlockFlags flags)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called if there is currently no existing Objective-C wrapper for a Delegate.
        /// </summary>
        /// <param name="del">Delegate for block</param>
        /// <param name="flags">Flags for creation</param>
        /// <param name="signature">Type Encoding for returned block</param>
        /// <returns>A callable function pointer for Block dispatch by the Objective-C runtime</returns>
        /// <remarks>
        /// Defer to the implementer for determining the <see cref="https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/ObjCRuntimeGuide/Articles/ocrtTypeEncodings.html#//apple_ref/doc/uid/TP40008048-CH100">Block signature</see>
        /// that should be used to project the managed Delegate.
        /// </remarks>
        protected abstract IntPtr GetBlockInvokeAndSignature(Delegate del, CreateBlockFlags flags, out string signature);

        /// <summary>
        /// Delegate describing a factory function for creation of a .NET Delegate wrapper for an Objective-C Block.
        /// </summary>
        /// <param name="block">The Objective-C block instance</param>
        /// <param name="invoker">The raw pointer to cast to the appropriate function pointer type and invoke</param>
        /// <returns>A Delegate</returns>
        /// <remarks>
        /// The C# function pointer syntax is dependent on the signature of the
        /// Block, but does takes the block argument as the first argument.
        /// For example:
        /// <code>
        /// ((delegate* unmanaged[Cdecl]&lt;IntPtr [, arg]*, ret&gt)invoker)(block, ...);
        /// </code>
        /// </remarks>
        public delegate Delegate CreateDelegate(IntPtr block, IntPtr invoker)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get or create a Delegate to represent the supplied Objective-C Block.
        /// </summary>
        /// <param name="block">Objective-C Block instance.</param>
        /// <param name="flags">Flags for creation</param>
        /// <param name="createDelegate">Delegate to call if one doesn't exist.</param>
        /// <returns>A Delegate</returns>
        public Delegate GetOrCreateDelegateForBlock(IntPtr block, CreateDelegateFlags flags, CreateDelegate createDelegate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Release the supplied block.
        /// </summary>
        /// <param name="block">The block to release</param>
        public void ReleaseBlockLiteral(ref BlockLiteral block)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Provides a way to indicate to the runtime that the .NET ThreadPool should
        /// add a NSAutoreleasePool to a thread and handle draining.
        /// </summary>
        /// <remarks>
        /// Work items executed on threadpool worker threads are wrapped with an NSAutoreleasePool
        /// that drains when the work item completes.
        /// See https://developer.apple.com/documentation/foundation/nsautoreleasepool
        /// </remarks>
        /// Addresses https://github.com/dotnet/runtime/issues/44213
        public static void EnableAutoReleasePoolsForThreadPool()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a <see cref="Wrappers"/> instance.
        /// </summary>
        protected Wrappers()
        {
            throw new NotImplementedException();
        }
    }
}
