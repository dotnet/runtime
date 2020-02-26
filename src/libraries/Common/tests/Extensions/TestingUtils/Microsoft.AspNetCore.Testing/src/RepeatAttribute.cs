// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Runs a test multiple times to stress flaky tests that are believed to be fixed.
    /// This can be used on an assembly, class, or method name. Requires using the AspNetCore test framework.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public class RepeatAttribute : Attribute
    {
        public RepeatAttribute(int runCount = 10)
        {
            RunCount = runCount;
        }

        /// <summary>
        /// The number of times to run a test.
        /// </summary>
        public int RunCount { get; }
    }
}
