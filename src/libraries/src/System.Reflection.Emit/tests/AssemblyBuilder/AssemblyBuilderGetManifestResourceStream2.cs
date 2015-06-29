// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class AssemblyBuilderGetManifestResourceStream2
    {
        [Fact]
        public void NegTest1()
        {
            AssemblyName name = new AssemblyName("NegTest1Assembly");
            AssemblyBuilder builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
            Assert.Throws<NotSupportedException>(() => { Stream myStream = builder.GetManifestResourceStream(""); });
        }
    }
}
