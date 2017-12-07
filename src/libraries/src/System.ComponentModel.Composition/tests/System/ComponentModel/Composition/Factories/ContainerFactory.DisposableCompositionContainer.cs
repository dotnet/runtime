// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using Xunit;

namespace System.ComponentModel.Composition.Factories
{
    partial class ContainerFactory
    {
        // NOTE: Do not add any more behavior to this class, as CompositionContainerTests.cs 
        // uses this to verify default behavior of the base class.
        private class DisposableCompositionContainer : CompositionContainer
        {
            private readonly Action<bool> _disposeCallback;

            public DisposableCompositionContainer(Action<bool> disposeCallback)
            {
                Assert.NotNull(disposeCallback);

                _disposeCallback = disposeCallback;
            }

            ~DisposableCompositionContainer()
            {
                Dispose(false);
            }

            protected override void Dispose(bool disposing)
            {
                _disposeCallback(disposing);

                base.Dispose(disposing);
            }
        }
    }
}
