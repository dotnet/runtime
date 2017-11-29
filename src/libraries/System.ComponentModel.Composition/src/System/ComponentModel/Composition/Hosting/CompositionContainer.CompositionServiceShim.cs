// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CompositionContainer
    {
        private class CompositionServiceShim : ICompositionService
        {
            CompositionContainer _innerContainer = null;

            public CompositionServiceShim(CompositionContainer innerContainer)
            {
                Assumes.NotNull(innerContainer);
                _innerContainer = innerContainer;
            }

            void ICompositionService.SatisfyImportsOnce(ComposablePart part)
            {
                _innerContainer.SatisfyImportsOnce(part);
            }
        }
    }
}
