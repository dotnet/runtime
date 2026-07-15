// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    public sealed class TripleDESCngProvider : TripleDESProvider
    {
        public static readonly TripleDESCngProvider Instance = new TripleDESCngProvider();

        private TripleDESCngProvider() { }

        public override TripleDES Create() => new TripleDESCng();
    }
}
