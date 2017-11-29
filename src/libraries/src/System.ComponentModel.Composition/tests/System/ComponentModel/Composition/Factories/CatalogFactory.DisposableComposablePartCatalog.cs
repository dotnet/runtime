// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition.Factories
{
    partial class CatalogFactory
    {
        // NOTE: Do not add any more behavior to this class, as ComposablePartCatalogTests.cs 
        // uses this to verify default behavior of the base class.
        private class DisposableComposablePartCatalog : ComposablePartCatalog
        {
            private readonly Action<bool> _disposeCallback;

            public DisposableComposablePartCatalog(Action<bool> disposeCallback)
            {
                Assert.NotNull(disposeCallback);

                _disposeCallback = disposeCallback;
            }

            ~DisposableComposablePartCatalog()
            {
                Dispose(false);
            }

            public override IQueryable<ComposablePartDefinition> Parts
            {
                get { throw new NotImplementedException(); }
            }

            protected override void Dispose(bool disposing)
            {
                _disposeCallback(disposing);

                base.Dispose(disposing);
            }
        }
    }
}
