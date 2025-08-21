// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to the x86 WAITPKG hardware instruction via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class WaitPkg : X86Base
    {
        internal WaitPkg() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get => false; }

        /// <summary>Provides access to the x86 WAITPKG hardware instructions, which are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : X86Base.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get => false; }
        }

        /// <summary>
        ///   <para>void _umonitor(void *address)</para>
        ///   <para>   UMONITOR r64</para>
        /// </summary>
        public static unsafe void SetUpUserLevelMonitor(void* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint8_t _tpause(uint32_t control, uint64_t counter)</para>
        ///   <para>  TPAUSE r32, &lt;EDX&gt;, &lt;EAX&gt;</para>
        /// </summary>
        public static bool TimedPause(uint control, ulong counter) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint8_t _umwait(uint32_t control, uint64_t counter)</para>
        ///   <para>  UMWAIT r32, &lt;EDX&gt;, &lt;EAX&gt;</para>
        /// </summary>
        public static bool WaitForUserLevelMonitor(uint control, ulong counter) { throw new PlatformNotSupportedException(); }
    }
}
