// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit;

namespace System.Diagnostics.Tests
{
    partial class ProcessTestBase
    {
        protected Process CreateProcessLong()
        {
            return CreateSleepProcess(RemotelyInvokable.WaitInMS);
        }

        protected Process CreateSleepProcess(int durationMs)
        {
            return CreateProcess(RemotelyInvokable.Sleep, durationMs.ToString());
        }

        protected Process CreateProcessPortable(Func<int> func)
        {
            return CreateProcess(func);
        }

        protected Process CreateProcessPortable(Func<string, int> func, string arg)
        {
            return CreateProcess(func, arg);
        }
    }
}
