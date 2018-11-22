// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    public partial class DefaultTraceListenerClassTests : RemoteExecutorTestBase
    {
        [Fact]
        public void EntryAssemblyName_Default_IncludedInTrace()
        {
            RemoteInvoke(() =>
            {
                var listener = new TestDefaultTraceListener();
                Trace.Listeners.Add(listener);
                Trace.TraceError("hello world");
                Assert.Equal(Assembly.GetEntryAssembly()?.GetName().Name + " Error: 0 : hello world", listener.Output.Trim());
            }).Dispose();
        }

        [Fact]
        public void EntryAssemblyName_Null_NotIncludedInTrace()
        {
            RemoteInvoke(() =>
            {
                MakeAssemblyGetEntryAssemblyReturnNull();

                var listener = new TestDefaultTraceListener();
                Trace.Listeners.Add(listener);
                Trace.TraceError("hello world");
                Assert.Equal("Error: 0 : hello world", listener.Output.Trim());
            }).Dispose();
        }

        /// <summary>
        /// Makes Assembly.GetEntryAssembly() return null using private reflection.
        /// </summary>
        private static void MakeAssemblyGetEntryAssemblyReturnNull()
        {
            typeof(Assembly)
                .GetField("s_forceNullEntryPoint", BindingFlags.NonPublic | BindingFlags.Static)
                .SetValue(null, true);

            Assert.Null(Assembly.GetEntryAssembly());
        }
    }
}
