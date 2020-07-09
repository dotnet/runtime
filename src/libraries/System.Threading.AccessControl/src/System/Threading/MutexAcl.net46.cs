// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.AccessControl;

namespace System.Threading
{
    public static class MutexAcl
    {
        public static Mutex Create(bool initiallyOwned, string? name, out bool createdNew, MutexSecurity? mutexSecurity)
        {
            return new Mutex(initiallyOwned, name, out createdNew, mutexSecurity);
        }
    }
}
