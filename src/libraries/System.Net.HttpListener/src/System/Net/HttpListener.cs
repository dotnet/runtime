// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication.ExtendedProtection;

namespace System.Net
{
    public sealed unsafe partial class HttpListener : IDisposable
    {
        public delegate ExtendedProtectionPolicy ExtendedProtectionSelector(HttpListenerRequest request);

        public void Close()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(this, nameof(Close));
            try
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info("HttpListenerRequest::Close()");
                ((IDisposable)this).Dispose();
            }
            catch (Exception exception)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(this, $"Close {exception}");
                throw;
            }
            finally
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(this);
            }
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }
    }
}
