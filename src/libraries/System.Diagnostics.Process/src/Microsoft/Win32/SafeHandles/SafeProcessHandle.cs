// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Class:  SafeProcessHandle
**
** A wrapper for a process handle
**
**
===========================================================*/

using System;
using System.Diagnostics;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>Provides a managed wrapper for a process handle.</summary>
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static readonly SafeProcessHandle InvalidHandle = new SafeProcessHandle();

        public SafeProcessHandle()
            : this(IntPtr.Zero)
        {
        }

        internal SafeProcessHandle(IntPtr handle)
            : this(handle, true)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="Microsoft.Win32.SafeHandles.SafeProcessHandle" /> class from the specified handle, indicating whether to release the handle during the finalization phase.</summary>
        /// <param name="existingHandle">The handle to be wrapped.</param>
        /// <param name="ownsHandle"><see langword="true" /> to reliably let <see cref="Microsoft.Win32.SafeHandles.SafeProcessHandle" /> release the handle during the finalization phase; otherwise, <see langword="false" />.</param>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// [!IMPORTANT]
        /// > This type implements the <xref:System.IDisposable> interface. When you have finished using the type, you should dispose of it either directly or indirectly. To dispose of the type directly, call its <xref:System.IDisposable.Dispose> method in a `try`/`catch` block. To dispose of it indirectly, use a language construct such as `using` (in C#) or `Using` (in Visual Basic). For more information, see the "Using an Object that Implements IDisposable" section in the <xref:System.IDisposable> interface topic.
        /// ]]></format></remarks>
        public SafeProcessHandle(IntPtr existingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }
    }
}
