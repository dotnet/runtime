// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Resources.Extensions.Compat.Tests
{
    public class CompatSwitchTests
    {
        [Fact]
        public void TheFlagIsSet()
        {
            FieldInfo fieldInfo = typeof(DeserializingResourceReader).GetField("s_useBinaryFormatter", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.True((bool)fieldInfo.GetValue(null));
        }
    }
}
