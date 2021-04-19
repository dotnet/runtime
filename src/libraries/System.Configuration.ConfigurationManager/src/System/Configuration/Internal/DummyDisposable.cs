// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration.Internal
{
    /// <summary>
    /// Used to satisfy legacy interfaces.
    /// </summary>
    internal sealed class DummyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
