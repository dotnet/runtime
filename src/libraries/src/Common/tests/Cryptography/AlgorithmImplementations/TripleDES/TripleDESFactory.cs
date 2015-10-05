// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Xunit;

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    public interface ITripleDESProvider
    {
        TripleDES Create();
    }

    public static partial class TripleDESFactory
    {
        public static TripleDES Create()
        {
            return s_provider.Create();
        }
    }
}
