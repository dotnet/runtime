// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Diagnostics.Tests
{
    internal sealed class DelegateSynchronizeInvoke : ISynchronizeInvoke
    {
        public Func<bool> InvokeRequiredDelegate;
        public Func<Delegate, object[], IAsyncResult> BeginInvokeDelegate;
        public Func<Delegate, object[], object> InvokeDelegate;

        public bool InvokeRequired => InvokeRequiredDelegate();
        public IAsyncResult BeginInvoke(Delegate method, object[] args) => BeginInvokeDelegate(method, args);
        public object EndInvoke(IAsyncResult result) => null;
        public object Invoke(Delegate method, object[] args) => InvokeDelegate(method, args);
    }
}
