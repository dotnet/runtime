// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition
{
    internal class CompositionServiceProxy : ICompositionService
    {
        private readonly CompositionContainer _container;

        public CompositionServiceProxy(CompositionContainer container)
        {
            this._container = container;
        }

        public void SatisfyImportsOnce(ComposablePart part)
        {
            this._container.SatisfyImportsOnce(part);
        }
    }
}
