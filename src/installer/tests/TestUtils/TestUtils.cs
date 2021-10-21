// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.DotNet
{
    public static class TestUtils
    {
        public static bool FailFast(Exception e)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            Environment.FailFast(e.ToString(), e);
            // Should never happen
            throw new InvalidOperationException();
        }
    }
}