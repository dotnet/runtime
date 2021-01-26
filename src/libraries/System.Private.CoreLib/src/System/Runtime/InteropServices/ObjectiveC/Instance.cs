// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    /// <summary>
    /// An Objective-C object instance.
    /// </summary>
    [SupportedOSPlatform("macos")]
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct Instance
    {
        public IntPtr Isa;

        /// <summary>
        /// Given an instance, return the underlying type.
        /// </summary>
        /// <typeparam name="T">Managed type of the instance.</typeparam>
        /// <param name="instancePtr">Instance pointer</param>
        /// <returns>The managed instance</returns>
        public static unsafe T GetInstance<T>(Instance* instancePtr) where T : class
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// An Objective-C block instance.
    /// </summary>
    /// <remarks>
    /// See http://clang.llvm.org/docs/Block-ABI-Apple.html#high-level for a
    /// description of the ABI represented by this data structure.
    /// </remarks>
    [SupportedOSPlatform("macos")]
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct BlockLiteral
    {
        /// <summary>
        /// Get <typeparamref name="T"/> type from the supplied Block.
        /// </summary>
        /// <typeparam name="T">The delegate type the block is associated with.</typeparam>
        /// <param name="block">The block instance</param>
        /// <returns>A delegate</returns>
        public static unsafe T GetDelegate<T>(BlockLiteral* block) where T : Delegate
        {
            throw new NotImplementedException();
        }
    }
}
