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
        protected static readonly string RunnerName = HostRunner;

        protected Process CreateProcessLong()
        {
            return CreateProcess(RemotelyInvokable.LongWait);
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
