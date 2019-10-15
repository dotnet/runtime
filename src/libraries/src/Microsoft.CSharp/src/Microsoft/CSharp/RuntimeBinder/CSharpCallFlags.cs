// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CSharp.RuntimeBinder
{
    /// <summary>
    /// Represents information about a <see cref="CSharpInvokeBinder" /> or <see cref="CSharpInvokeMemberBinder" /> that
    /// is not specific to any particular argument given to those operations.
    /// </summary>
    [Flags]
    internal enum CSharpCallFlags
    {
        /// <summary>
        /// No extra information.
        /// </summary>
        None = 0,

        /// <summary>
        /// The method was called given only a simple name, such as M(), unlike x.M().
        /// </summary>
        SimpleNameCall = 1,

        /// <summary>
        /// The call is permitted to bind against special names
        /// </summary>
        EventHookup = 2,

        ResultDiscarded = 4,
    }
}
