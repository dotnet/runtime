// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    public sealed class DefaultTripleDESProvider : TripleDESProvider
    {
        public static readonly DefaultTripleDESProvider Instance = new DefaultTripleDESProvider();

        private DefaultTripleDESProvider() { }

        public override TripleDES Create() => TripleDES.Create();
    }
}
