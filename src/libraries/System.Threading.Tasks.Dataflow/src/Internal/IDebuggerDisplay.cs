// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
