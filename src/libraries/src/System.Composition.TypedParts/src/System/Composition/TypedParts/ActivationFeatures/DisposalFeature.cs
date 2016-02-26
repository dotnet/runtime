// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Reflection;

namespace System.Composition.TypedParts.ActivationFeatures
{
    /// <summary>
    /// Modifies the activator so that disposable instances are bound to the appropriate scope.
    /// </summary>
    internal class DisposalFeature : ActivationFeature
    {
        public override CompositeActivator RewriteActivator(
            TypeInfo partType,
            CompositeActivator activator,
            IDictionary<string, object> partMetadata,
            IEnumerable<CompositionDependency> dependencies)
        {
            if (!typeof(IDisposable).GetTypeInfo().IsAssignableFrom(partType))
                return activator;

            return (c, o) =>
            {
                var inst = (IDisposable)activator(c, o);
                c.AddBoundInstance(inst);
                return inst;
            };
        }
    }
}
