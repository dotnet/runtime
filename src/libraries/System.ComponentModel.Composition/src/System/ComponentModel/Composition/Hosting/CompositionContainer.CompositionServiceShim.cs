// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CompositionContainer
    {
        private sealed class CompositionServiceShim : ICompositionService
        {
            private readonly CompositionContainer _innerContainer;

            public CompositionServiceShim(CompositionContainer innerContainer)
            {
                if (innerContainer == null)
                {
                    throw new ArgumentNullException(nameof(innerContainer));
                }
                _innerContainer = innerContainer;
            }

            void ICompositionService.SatisfyImportsOnce(ComposablePart part)
            {
                _innerContainer.SatisfyImportsOnce(part);
            }
        }
    }
}
