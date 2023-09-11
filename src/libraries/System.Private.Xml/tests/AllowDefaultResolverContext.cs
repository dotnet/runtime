// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Xml.Tests
{
    internal sealed class AllowDefaultResolverContext : IDisposable
    {
        private const string SwitchName = "Switch.System.Xml.AllowDefaultResolver";

        public AllowDefaultResolverContext()
        {
            AppContext.SetSwitch(SwitchName, isEnabled: true);
            ClearCachedSwitch();
        }

        public void Dispose()
        {
            AppContext.SetSwitch(SwitchName, isEnabled: false);
            ClearCachedSwitch();
        }

        private static void ClearCachedSwitch()
        {
            // Many tests try to change the value of this switch, which affects process-wide
            // state and which is cached by the implementation. As a hack to deal with this,
            // all tests in this assembly are run serially, and we use reflection to clear
            // out the cached value before and after each relevant test. Longer term, the better
            // approach is to use RemoteExecutor to isolate those tests that need to change
            // the default, or alternatively separate all such tests into a separate test project
            // where that project contains only those tests that require the switch set.

            Type t = Type.GetType("System.Xml.LocalAppContextSwitches, System.Private.Xml");
            Assert.NotNull(t);

            FieldInfo fi = t.GetField("s_allowDefaultResolver", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(fi);

            fi.SetValue(null, 0);
        }
    }
}
