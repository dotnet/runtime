// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    public sealed class DefaultDESProvider : DESProvider
    {
        public static readonly DefaultDESProvider Instance = new DefaultDESProvider();

        private DefaultDESProvider() { }

        public override DES Create() => DES.Create();

        public override bool OneShotSupported => true;
    }
}
