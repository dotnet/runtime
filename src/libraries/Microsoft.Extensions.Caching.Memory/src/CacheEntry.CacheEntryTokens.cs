// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    internal partial class CacheEntry
    {
        // this type exists just to reduce average CacheEntry size
        // which typicall is not using expiration tokens or callbacks
        private sealed class CacheEntryTokens
        {
            internal List<IChangeToken> _expirationTokens;
            internal List<IDisposable> _expirationTokenRegistrations;
            internal List<PostEvictionCallbackRegistration> _postEvictionCallbacks; // this is not really related to tokens, but was moved here to shrink typicall CacheEntry size

            internal List<IChangeToken> ExpirationTokens => _expirationTokens ??= new List<IChangeToken>();
            internal List<PostEvictionCallbackRegistration> PostEvictionCallbacks => _postEvictionCallbacks ??= new List<PostEvictionCallbackRegistration>();

            internal void AttachTokens()
            {
                if (_expirationTokens != null)
                {
                    lock (this)
                    {
                        for (int i = 0; i < _expirationTokens.Count; i++)
                        {
                            IChangeToken expirationToken = _expirationTokens[i];
                            if (expirationToken.ActiveChangeCallbacks)
                            {
                                if (_expirationTokenRegistrations == null)
                                {
                                    _expirationTokenRegistrations = new List<IDisposable>(1);
                                }
                                IDisposable registration = expirationToken.RegisterChangeCallback(ExpirationCallback, this);
                                _expirationTokenRegistrations.Add(registration);
                            }
                        }
                    }
                }
            }

            internal bool CheckForExpiredTokens(CacheEntry cacheEntry)
            {
                if (_expirationTokens != null)
                {
                    for (int i = 0; i < _expirationTokens.Count; i++)
                    {
                        IChangeToken expiredToken = _expirationTokens[i];
                        if (expiredToken.HasChanged)
                        {
                            cacheEntry.SetExpired(EvictionReason.TokenExpired);
                            return true;
                        }
                    }
                }
                return false;
            }

            internal void CopyTokens(CacheEntry parentEntry)
            {
                if (_expirationTokens != null)
                {
                    lock (this)
                    {
                        lock (parentEntry.GetOrCreateTokens())
                        {
                            foreach (IChangeToken expirationToken in _expirationTokens)
                            {
                                parentEntry.AddExpirationToken(expirationToken);
                            }
                        }
                    }
                }
            }

            internal void DetachTokens()
            {
                // _expirationTokenRegistrations is not checked for null, because AttachTokens might initialize it under lock
                // instead we are checking for _expirationTokens, because if they are not null, then _expirationTokenRegistrations might also be not null
                if (_expirationTokens != null)
                {
                    lock (this)
                    {
                        IList<IDisposable> registrations = _expirationTokenRegistrations;
                        if (registrations != null)
                        {
                            _expirationTokenRegistrations = null;
                            for (int i = 0; i < registrations.Count; i++)
                            {
                                IDisposable registration = registrations[i];
                                registration.Dispose();
                            }
                        }
                    }
                }
            }
        }
    }
}
