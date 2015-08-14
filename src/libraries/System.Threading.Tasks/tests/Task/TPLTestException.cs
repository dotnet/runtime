// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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