// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.
    /// Represents the runtime state of a dynamically generated method.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never), DebuggerStepThrough]
    public sealed class Closure
    {
        /// <summary>
        /// Represents the non-trivial constants and locally executable expressions that are referenced by a dynamically generated method.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public readonly object[] Constants;

        /// <summary>
        /// Represents the hoisted local variables from the parent context.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public readonly object[] Locals;

        /// <summary>
        /// Creates an object to hold state of a dynamically generated method.
        /// </summary>
        /// <param name="constants">The constant values used by the method.</param>
        /// <param name="locals">The hoisted local variables from the parent context.</param>
        public Closure(object[] constants, object[] locals)
        {
            Constants = constants;
            Locals = locals;
        }
    }
}
