// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** A specially designed handle wrapper to ensure we never leak
** an OS handle.  The runtime treats this class specially during
** P/Invoke marshaling and finalization.  Users should write
** subclasses of SafeHandle for each distinct handle type.
**
** 
===========================================================*/

namespace System.Runtime.InteropServices
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using System.Runtime;
    using System.Runtime.CompilerServices;
    using System.IO;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;

    /*
      Problems addressed by the SafeHandle class:
      1) Critical finalization - ensure we never leak OS resources in SQL.  Done
         without running truly arbitrary & unbounded amounts of managed code.
      2) Reduced graph promotion - during finalization, keep object graph small
      3) GC.KeepAlive behavior - P/Invoke vs. finalizer thread race conditions (HandleRef)
      4) Elimination of security race conditions w/ explicit calls to Close (HandleProtector)
      5) Enforcement of the above via the type system - Don't use IntPtr anymore.
      6) Allows the handle lifetime to be controlled externally via a boolean.

      Subclasses of SafeHandle will implement the ReleaseHandle abstract method
      used to execute any code required to free the handle. This method will be
      prepared as a constrained execution region at instance construction time
      (along with all the methods in its statically determinable call graph). This
      implies that we won't get any inconvenient jit allocation errors or rude
      thread abort interrupts while releasing the handle but the user must still
      write careful code to avoid injecting fault paths of their own (see the CER
      spec for more details). In particular, any sub-methods you call should be
      decorated with a reliability contract of the appropriate level. In most cases
      this should be:
        ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)

      The GC will run ReleaseHandle methods after any normal finalizers have been
      run for objects that were collected at the same time. This ensures classes
      like FileStream can run a normal finalizer to flush out existing buffered
      data. This is key - it means adding this class to a class like FileStream does
      not alter our current semantics w.r.t. finalization today.

      Subclasses must also implement the IsInvalid property so that the
      infrastructure can tell when critical finalization is actually required.
      Again, this method is prepared ahead of time. It's envisioned that direct
      subclasses of SafeHandle will provide an IsInvalid implementation that suits
      the general type of handle they support (null is invalid, -1 is invalid etc.)
      and then these classes will be further derived for specific safe handle types.

      Most classes using SafeHandle should not provide a finalizer.  If they do
      need to do so (ie, for flushing out file buffers, needing to write some data
      back into memory, etc), then they can provide a finalizer that will be 
      guaranteed to run before the SafeHandle's critical finalizer.  

      Note that SafeHandle's ReleaseHandle is called from a constrained execution 
      region, and is eagerly prepared before we create your class.  This means you
      should only call methods with an appropriate reliability contract from your
      ReleaseHandle method.

      Subclasses are expected to be written as follows (note that
      SuppressUnmanagedCodeSecurity should always be used on any P/Invoke methods
      invoked as part of ReleaseHandle, in order to switch the security check from
      runtime to jit time and thus remove a possible failure path from the
      invocation of the method):

      internal sealed MySafeHandleSubclass : SafeHandle {
          // Called by P/Invoke when returning SafeHandles
          private MySafeHandleSubclass() : base(IntPtr.Zero, true)
          {
          }

          // If & only if you need to support user-supplied handles
          internal MySafeHandleSubclass(IntPtr preexistingHandle, bool ownsHandle) : base(IntPtr.Zero, ownsHandle)
          {
              SetHandle(preexistingHandle);
          }

          // Do not provide a finalizer - SafeHandle's critical finalizer will
          // call ReleaseHandle for you.

          public override bool IsInvalid {
              get { return handle == IntPtr.Zero; }
          }

          override protected bool ReleaseHandle()
          {
              return MyNativeMethods.CloseHandle(handle);
          }
      }

      Then elsewhere to create one of these SafeHandles, define a method
      with the following type of signature (CreateFile follows this model).
      Note that when returning a SafeHandle like this, P/Invoke will call your
      class's default constructor.  Also, you probably want to define CloseHandle
      somewhere, and remember to apply a reliability contract to it.

      internal static class MyNativeMethods {
          [DllImport("kernel32")]
          private static extern MySafeHandleSubclass CreateHandle(int someState);

          [DllImport("kernel32", SetLastError=true), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
          private static extern bool CloseHandle(IntPtr handle);
      }

      Drawbacks with this implementation:
      1) Requires some magic to run the critical finalizer.
      2) Requires more memory than just an IntPtr.
      3) If you use DangerousAddRef and forget to call DangerousRelease, you can leak a SafeHandle.  Use CER's & don't do that.
     */


    // This class should not be serializable - it's a handle.  We require unmanaged
    // code permission to subclass SafeHandle to prevent people from writing a 
    // subclass and suddenly being able to run arbitrary native code with the
    // same signature as CloseHandle.  This is technically a little redundant, but
    // we'll do this to ensure we've cut off all attack vectors.  Similarly, all
    // methods have a link demand to ensure untrusted code cannot directly edit
    // or alter a handle.
    public abstract class SafeHandle : CriticalFinalizerObject, IDisposable
    {
        // ! Do not add or rearrange fields as the EE depends on this layout.
        //------------------------------------------------------------------
        protected IntPtr handle;   // this must be protected so derived classes can use out params. 
        private int _state;   // Combined ref count and closed/disposed flags (so we can atomically modify them).
        private bool _ownsHandle;  // Whether we can release this handle.
#pragma warning disable 414
        private bool _fullyInitialized;  // Whether constructor completed.
#pragma warning restore 414

        // Creates a SafeHandle class.  Users must then set the Handle property.
        // To prevent the SafeHandle from being freed, write a subclass that
        // doesn't define a finalizer.
        protected SafeHandle(IntPtr invalidHandleValue, bool ownsHandle)
        {
            handle = invalidHandleValue;
            _state = 4; // Ref count 1 and not closed or disposed.
            _ownsHandle = ownsHandle;

            if (!ownsHandle)
                GC.SuppressFinalize(this);

            // Set this last to prevent SafeHandle's finalizer from freeing an
            // invalid handle.  This means we don't have to worry about 
            // ThreadAbortExceptions interrupting this constructor or the managed
            // constructors on subclasses that call this constructor.
            _fullyInitialized = true;
        }

        // Migrating InheritanceDemands requires this default ctor, so we can mark it critical
        protected SafeHandle()
        {
            Debug.Fail("SafeHandle's protected default ctor should never be used!");
            throw new NotImplementedException();
        }

        ~SafeHandle()
        {
            Dispose(false);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void InternalFinalize();

        protected void SetHandle(IntPtr handle)
        {
            this.handle = handle;
        }

        // This method is necessary for getting an IntPtr out of a SafeHandle.
        // Used to tell whether a call to create the handle succeeded by comparing
        // the handle against a known invalid value, and for backwards 
        // compatibility to support the handle properties returning IntPtrs on
        // many of our Framework classes.
        // Note that this method is dangerous for two reasons:
        //  1) If the handle has been marked invalid with SetHandleAsInvalid,
        //     DangerousGetHandle will still return the original handle value.
        //  2) The handle returned may be recycled at any point. At best this means
        //     the handle might stop working suddenly. At worst, if the handle or
        //     the resource the handle represents is exposed to untrusted code in
        //     any way, this can lead to a handle recycling security attack (i.e. an
        //     untrusted caller can query data on the handle you've just returned
        //     and get back information for an entirely unrelated resource).
        public IntPtr DangerousGetHandle()
        {
            return handle;
        }

        public bool IsClosed
        {
            get { return (_state & 1) == 1; }
        }

        public abstract bool IsInvalid
        {
            get;
        }

        public void Close()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                InternalDispose();
            else
                InternalFinalize();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void InternalDispose();

        // This should only be called for cases when you know for a fact that
        // your handle is invalid and you want to record that information.
        // An example is calling a syscall and getting back ERROR_INVALID_HANDLE.
        // This method will normally leak handles!
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern void SetHandleAsInvalid();

        // Implement this abstract method in your derived class to specify how to
        // free the handle. Be careful not write any code that's subject to faults
        // in this method (the runtime will prepare the infrastructure for you so
        // that no jit allocations etc. will occur, but don't allocate memory unless
        // you can deal with the failure and still free the handle).
        // The boolean returned should be true for success and false if the runtime
        // should fire a SafeHandleCriticalFailure MDA (CustomerDebugProbe) if that
        // MDA is enabled.
        protected abstract bool ReleaseHandle();

        // Add a reason why this handle should not be relinquished (i.e. have
        // ReleaseHandle called on it). This method has dangerous in the name since
        // it must always be used carefully (e.g. called within a CER) to avoid
        // leakage of the handle. It returns a boolean indicating whether the
        // increment was actually performed to make it easy for program logic to
        // back out in failure cases (i.e. is a call to DangerousRelease needed).
        // It is passed back via a ref parameter rather than as a direct return so
        // that callers need not worry about the atomicity of calling the routine
        // and assigning the return value to a variable (the variable should be
        // explicitly set to false prior to the call). The only failure cases are
        // when the method is interrupted prior to processing by a thread abort or
        // when the handle has already been (or is in the process of being)
        // released.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern void DangerousAddRef(ref bool success);

        // Partner to DangerousAddRef. This should always be successful when used in
        // a correct manner (i.e. matching a successful DangerousAddRef and called
        // from a region such as a CER where a thread abort cannot interrupt
        // processing). In the same way that unbalanced DangerousAddRef calls can
        // cause resource leakage, unbalanced DangerousRelease calls may cause
        // invalid handle states to become visible to other threads. This
        // constitutes a potential security hole (via handle recycling) as well as a
        // correctness problem -- so don't ever expose Dangerous* calls out to
        // untrusted code.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern void DangerousRelease();
    }
}
