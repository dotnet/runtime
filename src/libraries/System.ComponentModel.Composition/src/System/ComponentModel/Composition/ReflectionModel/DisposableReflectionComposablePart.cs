// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal sealed class DisposableReflectionComposablePart : ReflectionComposablePart, IDisposable
    {
        private volatile int _isDisposed;

        public DisposableReflectionComposablePart(ReflectionComposablePartDefinition definition)
            : base(definition)
        {
        }

        protected override void ReleaseInstanceIfNecessary(object? instance)
        {
            if (instance is IDisposable disposable)
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
