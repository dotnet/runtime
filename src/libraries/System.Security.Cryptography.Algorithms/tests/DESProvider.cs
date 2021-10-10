// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    internal class DesProvider : IDESProvider
    {
        public DES Create() => DES.Create();
        public bool OneShotSupported => true;
    }

    public partial class DESFactory
    {
        private static readonly IDESProvider s_provider = new DesProvider();
    }
}
