// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Tests
{
    /// <summary>
    /// Whenever we need to simulate an user exception or an unhandled exception, we use this class
    /// </summary>
    internal class TPLTestException : Exception
    {
        public readonly int FromTaskId;

        public TPLTestException()
            : base("Throwing an exception")
        {
            FromTaskId = Task.CurrentId ?? -1;
        }
    }
}
