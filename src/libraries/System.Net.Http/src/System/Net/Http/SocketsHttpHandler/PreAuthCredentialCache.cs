// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Net.Http
{
    internal sealed class PreAuthCredentialCache
    {
        private Dictionary<CredentialCacheKey, NetworkCredential>? _cache;

        public void Add(Uri uriPrefix, string authType, NetworkCredential cred)
        {
            Debug.Assert(uriPrefix != null);
            Debug.Assert(authType != null);

            var key = new CredentialCacheKey(uriPrefix, authType);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Adding key:[{key}], cred:[{cred.Domain}],[{cred.UserName}]");

            _cache ??= new Dictionary<CredentialCacheKey, NetworkCredential>();
            _cache.Add(key, cred);
        }

        public void Remove(Uri uriPrefix, string authType)
        {
            Debug.Assert(uriPrefix != null);
            Debug.Assert(authType != null);

            if (_cache == null)
            {
                return;
            }

            var key = new CredentialCacheKey(uriPrefix, authType);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Removing key:[{key}]");
            _cache.Remove(key);
        }

        public (Uri uriPrefix, NetworkCredential credential)? GetCredential(Uri uriPrefix, string authType)
        {
            Debug.Assert(uriPrefix != null);
            Debug.Assert(authType != null);

            if (_cache == null)
            {
                return null;
            }

            CredentialCacheHelper.TryGetCredential(_cache, uriPrefix, authType, out Uri? mostSpecificMatchUri, out NetworkCredential? mostSpecificMatch);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Returning {(mostSpecificMatch == null ? "null" : "(" + mostSpecificMatch.UserName + ":" + mostSpecificMatch.Domain + ")")}");

            return mostSpecificMatch == null ? null : (mostSpecificMatchUri!, mostSpecificMatch!);
        }
    }
}
