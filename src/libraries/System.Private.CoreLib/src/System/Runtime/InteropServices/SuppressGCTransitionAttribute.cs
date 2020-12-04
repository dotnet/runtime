// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// An attribute used to indicate a GC transition should be skipped when making an unmanaged function call.
    /// </summary>
    /// <example>
    /// Example of a valid use case. The Win32 `GetTickCount()` function is a small performance related function
    /// that reads some global memory and returns the value. In this case, the GC transition overhead is significantly
    /// more than the memory read.
    /// <code>
    /// using System;
    /// using System.Runtime.InteropServices;
    /// class Program
    /// {
    ///     [DllImport("Kernel32")]
    ///     [SuppressGCTransition]
    ///     static extern int GetTickCount();
    ///     static void Main()
    ///     {
    ///         Console.WriteLine($"{GetTickCount()}");
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// This attribute is ignored if applied to a method without the <see cref="System.Runtime.InteropServices.DllImportAttribute"/>.
    ///
    /// Forgoing this transition can yield benefits when the cost of the transition is more than the execution time
    /// of the unmanaged function. However, avoiding this transition removes some of the guarantees the runtime
    /// provides through a normal P/Invoke. When exiting the managed runtime to enter an unmanaged function the
    /// GC must transition from Cooperative mode into Preemptive mode. Full details on these modes can be found at
    /// https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/clr-code-guide.md#2.1.8.
    /// Suppressing the GC transition is an advanced scenario and should not be done without fully understanding
    /// potential consequences.
    ///
    /// One of these consequences is an impact to Mixed-mode debugging (https://docs.microsoft.com/visualstudio/debugger/how-to-debug-in-mixed-mode).
    /// During Mixed-mode debugging, it is not possible to step into or set breakpoints in a P/Invoke that
    /// has been marked with this attribute. A workaround is to switch to native debugging and set a breakpoint in the native function.
    /// In general, usage of this attribute is not recommended if debugging the P/Invoke is important, for example
    /// stepping through the native code or diagnosing an exception thrown from the native code.
    ///
    /// The runtime may load the native library for method marked with this attribute in advance before the method is called for the first time.
    /// Usage of this attribute is not recommended for platform neutral libraries with conditional platform specific code.
    ///
    /// The P/Invoke method that this attribute is applied to must have all of the following properties:
    ///   * Native function always executes for a trivial amount of time (less than 1 microsecond).
    ///   * Native function does not perform a blocking syscall (e.g. any type of I/O).
    ///   * Native function does not call back into the runtime (e.g. Reverse P/Invoke).
    ///   * Native function does not throw exceptions.
    ///   * Native function does not manipulate locks or other concurrency primitives.
    ///
    /// Consequences of invalid uses of this attribute:
    ///   * GC starvation.
    ///   * Immediate runtime termination.
    ///   * Data corruption.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    sealed class SuppressGCTransitionAttribute : Attribute
    {
        public SuppressGCTransitionAttribute()
        {
        }
    }
}
