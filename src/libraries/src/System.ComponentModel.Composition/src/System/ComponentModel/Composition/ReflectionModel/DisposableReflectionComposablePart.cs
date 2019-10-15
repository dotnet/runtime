// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal sealed class DisposableReflectionComposablePart : ReflectionComposablePart, IDisposable
    {
        private volatile int _isDisposed = 0;

        public DisposableReflectionComposablePart(ReflectionComposablePartDefinition definition)
            : base(definition)
        {
        }

        protected override void ReleaseInstanceIfNecessary(object instance)
        {
            IDisposable disposable = instance as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        protected override void EnsureRunning()
        {
            base.EnsureRunning();
            if (_isDisposed == 1)
            {
                throw ExceptionBuilder.CreateObjectDisposed(this);
            }
        }

        void IDisposable.Dispose()
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            {
                ReleaseInstanceIfNecessary(CachedInstance);
            }
        }
    }
}
