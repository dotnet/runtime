// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    public interface IDESProvider
    {
        DES Create();
    }

    public static partial class DESFactory
    {
        public static DES Create()
        {
            return s_provider.Create();
        }
    }
}
