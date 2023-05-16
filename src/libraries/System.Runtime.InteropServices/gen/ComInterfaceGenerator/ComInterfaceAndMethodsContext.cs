// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Represents an interface and all of the methods that need to be generated for it (methods declared on the interface and methods inherited from base interfaces).
    /// </summary>
    internal sealed record ComInterfaceAndMethodsContext(ComInterfaceContext Interface, SequenceEqualImmutableArray<ComMethodContext> Methods)
    {
        // Change Calc all methods to return an ordered list of all the methods and the data in comInterfaceandMethodsContext
        // Have a step that runs CalculateMethodStub on each of them.
        // Call GroupMethodsByInterfaceForGeneration

        /// <summary>
        /// COM methods that are declared on the attributed interface declaration.
        /// </summary>
        public IEnumerable<ComMethodContext> DeclaredMethods => Methods.Where(m => !m.IsInheritedMethod);

        /// <summary>
        /// COM methods that are declared on an interface the interface inherits from.
        /// </summary>
        public IEnumerable<ComMethodContext> ShadowingMethods => Methods.Where(m => m.IsInheritedMethod);
    }
}
