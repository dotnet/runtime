// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Composition.Hosting.Core
{
    /// <summary>
    /// The delegate signature that allows instances of parts and exports to be accessed during
    /// a composition operation.
    /// </summary>
    /// <param name="context">The context in which the part or export is being accessed.</param>
    /// <param name="operation">The operation within which the activation is occuring.</param>
    /// <returns>The activated part or export.</returns>
    public delegate object CompositeActivator(LifetimeContext context, CompositionOperation operation);
}
