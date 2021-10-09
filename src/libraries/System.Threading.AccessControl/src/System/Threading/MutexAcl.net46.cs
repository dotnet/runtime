// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.AccessControl;

namespace System.Threading
{
    public static class MutexAcl
    {
        public static Mutex Create(bool initiallyOwned, string? name, out bool createdNew, MutexSecurity? mutexSecurity)
        {
            return new Mutex(initiallyOwned, name, out createdNew, mutexSecurity);
        }

        public static Mutex OpenExisting(string name, MutexRights rights)
        {
            return Mutex.OpenExisting(name, rights);
        }

        public static bool TryOpenExisting(string name, MutexRights rights, [NotNullWhen(true)] out Mutex result)
        {
            return Mutex.TryOpenExisting(name, rights, out result);
        }
    }
}
