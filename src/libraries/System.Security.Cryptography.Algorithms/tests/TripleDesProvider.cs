// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    internal class TripleDesProvider : ITripleDESProvider
    {
        public TripleDES Create()
        {
            return TripleDES.Create();
        }
    }

    public partial class TripleDESFactory
    {
        private static readonly ITripleDESProvider s_provider = new TripleDesProvider();
    }
}
