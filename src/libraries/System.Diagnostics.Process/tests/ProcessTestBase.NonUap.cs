// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    partial class ProcessTestBase
    {
        protected Process CreateProcessLong([CallerMemberName] string callerName = null)
        {
            return CreateSleepProcess(RemotelyInvokable.WaitInMS, callerName);
        }

        protected Process CreateSleepProcess(int durationMs, [CallerMemberName] string callerName = null)
        {
            return CreateProcess(RemotelyInvokable.Sleep, durationMs.ToString(), callerName);
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
