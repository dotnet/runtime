// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// An <see cref="IChangeToken"/> which represents one or more <see cref="IChangeToken"/> instances.
    /// </summary>
    public class CompositeChangeToken : IChangeToken
    {
        private static readonly Action<object> _onChangeDelegate = OnChange;
        private readonly object _callbackLock = new object();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _registeredCallbackProxy;
        private List<IDisposable> _disposables;

        /// <summary>
        /// Creates a new instance of <see cref="CompositeChangeToken"/>.
        /// </summary>
        /// <param name="changeTokens">The list of <see cref="IChangeToken"/> to compose.</param>
        public CompositeChangeToken(IReadOnlyList<IChangeToken> changeTokens)
        {
            ChangeTokens = changeTokens ?? throw new ArgumentNullException(nameof(changeTokens));
            for (int i = 0; i < ChangeTokens.Count; i++)
            {
                if (ChangeTokens[i].ActiveChangeCallbacks)
                {
                    ActiveChangeCallbacks = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Returns the list of <see cref="IChangeToken"/> which compose the current <see cref="CompositeChangeToken"/>.
        /// </summary>
        public IReadOnlyList<IChangeToken> ChangeTokens { get; }

        /// <inheritdoc />
        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            EnsureCallbacksInitialized();
            return _cancellationTokenSource.Token.Register(callback, state);
        }

        /// <inheritdoc />
        public bool HasChanged
        {
            get
            {
                if (_cancellationTokenSource != null && _cancellationTokenSource.Token.IsCancellationRequested)
                {
                    return true;
                }

                for (int i = 0; i < ChangeTokens.Count; i++)
                {
                    if (ChangeTokens[i].HasChanged)
                    {
                        OnChange(this);
                        return true;
                    }
                }

                return false;
            }
        }

        /// <inheritdoc />
        public bool ActiveChangeCallbacks { get; }

        private void EnsureCallbacksInitialized()
        {
            if (_registeredCallbackProxy)
            {
                return;
            }

            lock (_callbackLock)
            {
                if (_registeredCallbackProxy)
                {
                    return;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _disposables = new List<IDisposable>();
                for (int i = 0; i < ChangeTokens.Count; i++)
                {
                    if (ChangeTokens[i].ActiveChangeCallbacks)
                    {
                        IDisposable disposable = ChangeTokens[i].RegisterChangeCallback(_onChangeDelegate, this);
                        _disposables.Add(disposable);
                    }
                }
                _registeredCallbackProxy = true;
            }
        }

        private static void OnChange(object state)
        {
            var compositeChangeTokenState = (CompositeChangeToken)state;
            if (compositeChangeTokenState._cancellationTokenSource == null)
            {
                return;
            }

            lock (compositeChangeTokenState._callbackLock)
            {
                try
                {
                    compositeChangeTokenState._cancellationTokenSource.Cancel();
                }
                catch
                {
                }
            }

            List<IDisposable> disposables = compositeChangeTokenState._disposables;
            Debug.Assert(disposables != null);
            for (int i = 0; i < disposables.Count; i++)
            {
                disposables[i].Dispose();
            }

        }
    }
}
