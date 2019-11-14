// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// IDebuggerDisplay.cs
//
//
// An interface implemented by objects that expose their debugger display
// attribute content through a property, making it possible for code to query
// for the same content.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

namespace System.Threading.Tasks.Dataflow.Internal
{
    /// <summary>Implemented to provide customizable data for debugger displays.</summary>
    internal interface IDebuggerDisplay
    {
        /// <summary>The object to be displayed as the content of a DebuggerDisplayAttribute.</summary>
        /// <remarks>
        /// The property returns an object to allow the debugger to interpret arbitrary .NET objects.
        /// The return value may be, but need not be limited to be, a string.
        /// </remarks>
        object Content { get; }
    }
}
