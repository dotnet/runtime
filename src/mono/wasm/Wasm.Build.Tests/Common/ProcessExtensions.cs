// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Wasm.Build.Tests
{
    internal static class ProcessExtensions
    {
        public static Task StartAndWaitForExitAsync(this Process subject)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            subject.EnableRaisingEvents = true;

            subject.Exited += (s, a) =>
            {
                taskCompletionSource.SetResult(null);
            };

            subject.Start();

            return taskCompletionSource.Task;
        }
    }
}
