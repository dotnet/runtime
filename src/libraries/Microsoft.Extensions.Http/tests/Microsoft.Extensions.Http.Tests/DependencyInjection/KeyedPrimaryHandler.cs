// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class KeyedPrimaryHandler : TestMessageHandler
    {
        private bool _disposed;

        public string Name { get; }

        public KeyedPrimaryHandler([ServiceKey] string name) : base()
        {
            Name = name;
            _responseFactory = _ => CreateResponse();
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }

        private HttpResponseMessage CreateResponse()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            return new HttpResponseMessage() { Content = new StringContent(Name) };
        }
    }
}
