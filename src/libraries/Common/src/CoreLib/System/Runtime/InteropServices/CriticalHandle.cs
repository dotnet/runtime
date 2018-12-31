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
** subclasses of CriticalHandle for each distinct handle type.
** This class is similar to SafeHandle, but lacks the ref counting
** behavior on marshaling that prevents handle recycling errors
** or security holes. This lowers the overhead of using the handle
** considerably, but leaves the onus on the caller to protect
** themselves from any recycling effects.
**
** **** NOTE ****
**
** Since there are no ref counts tracking handle usage there is
** no thread safety either. Your application must ensure that
** usages of the handle do not cross with attempts to close the
** handle (or tolerate such crossings). Normal GC mechanics will
** prevent finalization until the handle class isn't used any more,
** but explicit Close or Dispose operations may be initiated at any
** time.
**
** Similarly, multiple calls to Close or Dispose on different
** threads at the same time may cause the ReleaseHandle method to be
** called more than once.
**
** In general (and as might be inferred from the lack of handle
** recycle protection) you should be very cautious about exposing
** CriticalHandle instances directly or indirectly to untrusted users.
** At a minimum you should restrict their ability to queue multiple
** operations against a single handle at the same time or block their
** access to Close and Dispose unless you are very comfortable with the
** semantics of passing an invalid (or possibly invalidated and
** reallocated) to the unamanged routines you marshal your handle to
** (and the effects of closing such a handle while those calls are in
** progress). The runtime cannot protect you from undefined program
** behvior that might result from such scenarios. You have been warned.
**
** 
===========================================================*/

using System.Runtime.ConstrainedExecution;

/*
  Problems addressed by the CriticalHandle class:
  1) Critical finalization - ensure we never leak OS resources in SQL.  Done
     without running truly arbitrary & unbounded amounts of managed code.
  2) Reduced graph promotion - during finalization, keep object graph small
  3) GC.KeepAlive behavior - P/Invoke vs. finalizer thread race condition (HandleRef)
  4) Enforcement of the above via the type system - Don't use IntPtr anymore.

  Subclasses of CriticalHandle will implement the ReleaseHandle
  abstract method used to execute any code required to free the
  handle. This method will be prepared as a constrained execution
  region at instance construction time (along with all the methods in
  its statically determinable call graph). This implies that we won't
  get any inconvenient jit allocation errors or rude thread abort
  interrupts while releasing the handle but the user must still write
  careful code to avoid injecting fault paths of their own (see the
  CER spec for more details). In particular, any sub-methods you call
  should be decorated with a reliability contract of the appropriate
  level. In most cases this should be:
    ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)

  Subclasses must also implement the IsInvalid property so that the
  infrastructure can tell when critical finalization is actually required.
  Again, this method is prepared ahead of time. It's envisioned that direct
  subclasses of CriticalHandle will provide an IsInvalid implementation that suits
  the general type of handle they support (null is invalid, -1 is invalid etc.)
  and then these classes will be further derived for specific handle types.

  Most classes using CriticalHandle should not provide a finalizer.  If they do
  need to do so (ie, for flushing out file buffers, needing to write some data
  back into memory, etc), then they can provide a finalizer that will be 
  guaranteed to run before the CriticalHandle's critical finalizer.

  Subclasses are expected to be written as follows (note that
  SuppressUnmanagedCodeSecurity should always be used on any P/Invoke methods
  invoked as part of ReleaseHandle, in order to switch the security check from
  runtime to jit time and thus remove a possible failure path from the
  invocation of the method):

  internal sealed MyCriticalHandleSubclass : CriticalHandle {
      // Called by P/Invoke when returning CriticalHandles
      private MyCriticalHandleSubclass() : base(IntPtr.Zero)
      {
      }

      // Do not provide a finalizer - CriticalHandle's critical finalizer will
      // call ReleaseHandle for you.

      public override bool IsInvalid {
          get { return handle == IntPtr.Zero; }
      }

      [DllImport(Interop.Libraries.Kernel32), SuppressUnmanagedCodeSecurity, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
      private static extern bool CloseHandle(IntPtr handle);

      override protected bool ReleaseHandle()
      {
          return CloseHandle(handle);
      }
  }

  Then elsewhere to create one of these CriticalHandles, define a method
  with the following type of signature (CreateFile follows this model).
  Note that when returning a CriticalHandle like this, P/Invoke will call your
  classes default constructor.

      [DllImport(Interop.Libraries.Kernel32)]
      private static extern MyCriticalHandleSubclass CreateHandle(int someState);

 */

namespace System.Runtime.InteropServices
{
    // This class should not be serializable - it's a handle
    public abstract partial class CriticalHandle : CriticalFinalizerObject, IDisposable
    {
        // ! Do not add or rearrange fields as the EE depends on this layout.
        //------------------------------------------------------------------
        protected IntPtr handle;    // This must be protected so derived classes can use out params. 
        private bool _isClosed;     // Set by SetHandleAsInvalid or Close/Dispose/finalization.

        // Creates a CriticalHandle class.  Users must then set the Handle property or allow P/Invoke marshaling to set it implicitly.
        protected CriticalHandle(IntPtr invalidHandleValue)
        {
            handle = invalidHandleValue;
        }

        ~CriticalHandle()
        {
            Dispose(false);
        }

        private void Cleanup()
        {
            if (IsClosed)
                return;
            _isClosed = true;

            if (IsInvalid)
                return;

            // Save last error from P/Invoke in case the implementation of
            // ReleaseHandle trashes it (important because this ReleaseHandle could
            // occur implicitly as part of unmarshaling another P/Invoke).
            int lastError = Marshal.GetLastWin32Error();

            if (!ReleaseHandle())
                ReleaseHandleFailed();

            Marshal.SetLastWin32Error(lastError);
            GC.SuppressFinalize(this);
        }

        protected void SetHandle(IntPtr handle)
        {
            this.handle = handle;
        }

        // Returns whether the handle has been explicitly marked as closed
        // (Close/Dispose) or invalid (SetHandleAsInvalid).
        public bool IsClosed
        {
            get { return _isClosed; }
        }

        // Returns whether the handle looks like an invalid value (i.e. matches one
        // of the handle's designated illegal values). CriticalHandle itself doesn't
        // know what an invalid handle looks like, so this method is abstract and
        // must be provided by a derived type.
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
            Cleanup();
        }

        // This should only be called for cases when you know for a fact that
        // your handle is invalid and you want to record that information.
        // An example is calling a syscall and getting back ERROR_INVALID_HANDLE.
        // This method will normally leak handles!
        public void SetHandleAsInvalid()
        {
            _isClosed = true;
            GC.SuppressFinalize(this);
        }

        // Implement this abstract method in your derived class to specify how to
        // free the handle. Be careful not write any code that's subject to faults
        // in this method (the runtime will prepare the infrastructure for you so
        // that no jit allocations etc. will occur, but don't allocate memory unless
        // you can deal with the failure and still free the handle).
        // The boolean returned should be true for success and false if a
        // catastrophic error occurred and you wish to trigger a diagnostic for
        // debugging purposes.
        protected abstract bool ReleaseHandle();
    }
}
