// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Security;

namespace System.IO.IsolatedStorage
{
    internal static partial class Helper
    {
        public const string IsolatedStorageDirectoryName = "IsolatedStorage";

        internal static string GetRandomDirectory(string rootDirectory, IsolatedStorageScope scope)
        {
            string? randomDirectory = GetExistingRandomDirectory(rootDirectory);
            if (string.IsNullOrEmpty(randomDirectory))
            {
                using (Mutex m = CreateMutexNotOwned(rootDirectory))
                {
                    if (!m.WaitOne())
                    {
                        throw new IsolatedStorageException(SR.IsolatedStorage_Init);
                    }

                    try
                    {
                        randomDirectory = GetExistingRandomDirectory(rootDirectory);
                        if (string.IsNullOrEmpty(randomDirectory))
                        {
                            // Someone else hasn't created the directory before we took the lock
                            randomDirectory = Path.Combine(rootDirectory, Path.GetRandomFileName(), Path.GetRandomFileName());
                            CreateDirectory(randomDirectory, scope);
                        }
                    }
                    finally
                    {
                        m.ReleaseMutex();
                    }
                }
            }

            return randomDirectory;
        }

        private static Mutex CreateMutexNotOwned(string pathName)
        {
            return new Mutex(initiallyOwned: false, name: @"Global\" + IdentityHelper.GetStrongHashSuitableForObjectName(pathName));
        }
    }
}
