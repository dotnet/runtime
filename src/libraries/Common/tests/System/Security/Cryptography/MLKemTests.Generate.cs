// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static partial class MLKemTests
    {
        [Fact]
        public static void Generate_MlKem512()
        {
            using MLKem kem = MLKem.GenerateMLKem512Key();
        }
    }
}
