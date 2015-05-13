// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel.EventBasedAsync.Tests
{
    public class TestException : Exception
    {
        public TestException(string message)
            : base(message)
        {
        }
    }
}
