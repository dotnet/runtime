// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
