// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Specifies that the async2 function in its promise-returning form should be returning ValueTask/ValueTask`1
    ///
    /// The attribute is source-only and is not supposed to be emitted into metadata, where it has no meaning.
    /// TODO: (vsadov) I have a lot of doubts that this is the best possible design to support ValueTask in async2.
    ///       While good enough to unblock working with ValueTask async2, we should think more about the syntax.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ValueTaskAsyncAttribute : Attribute
    {
        public ValueTaskAsyncAttribute() { }
    }
}
