// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum MethodKindEnum
    {
        None = 0,
        Constructor = 1,              // Ctor or static ctor
        Destructor = 2,
        PropAccessor = 3,
        EventAccessor = 4,
        ExplicitConv = 5,      // Explicit user defined conversion
        ImplicitConv = 6,      // Implicit user defined conversion
        Anonymous = 7,
        // delegates
        Invoke = 8,            // Invoke method of a delegate type
        BeginInvoke = 9,       // BeginInvoke method of a delegate type
        EndInvoke = 10,        // EndInvoke method of a delegate type
        // AnonymousTypes
        AnonymousTypeToString = 11,
        AnonymousTypeEquals = 12,
        AnonymousTypeGetHashCode = 13,
        // Iterators
        IteratorDispose = 14,
        IteratorReset = 15,
        IteratorGetEnumerator = 16,
        IteratorGetEnumeratorDelegating = 17,
        IteratorMoveNext = 18,
        // Partial Methods
        Latent = 19,
        Actual = 20,
        IteratorFinally = 21,
    }
}
