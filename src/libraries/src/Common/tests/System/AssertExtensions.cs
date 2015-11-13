// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System
{
    public static class AssertExtensions
    {
        public static void Throws<T>(Action action, string message)
            where T : Exception
        {
            Assert.Equal(Assert.Throws<T>(action).Message, message);
        }
    }
}
