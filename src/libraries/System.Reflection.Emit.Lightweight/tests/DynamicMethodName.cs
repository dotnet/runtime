// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodName
    {
        [Fact]
        public void PosTest1()
        {
            string dynamicname = "TestDynamicStringName";
            DynamicMethod dynamicmethod = new DynamicMethod(dynamicname, null, null, typeof(DynamicMethodName).GetTypeInfo().Module);
            Assert.Equal(dynamicmethod.Name, dynamicname);
        }
    }
}
