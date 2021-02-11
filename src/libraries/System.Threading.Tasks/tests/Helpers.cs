// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Tasks.Tests
{
    internal class InvokeActionOnFinalization
    {
        public Action Action;
        ~InvokeActionOnFinalization() => Action?.Invoke();
    }
}
