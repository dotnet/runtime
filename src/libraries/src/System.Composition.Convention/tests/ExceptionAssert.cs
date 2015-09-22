// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Composition.UnitTests.Util;
using System.Runtime.Serialization;
using Xunit;

namespace System.Composition.Convention
{
    public static class ExceptionAssert
    {
        // NOTE: To catch state corrupting exceptions, it is by design that
        // this retries.

        /// <summary>
        ///     Verifies that the specified action throws an exception of type <typeparam name="T"/>,
        ///     and the message contains the given string
        /// </summary>
        public static T ThrownMessageContains<T>(string message, Action action)
            where T : Exception
        {
            return Retry<T>(action, e => Assert.Contains(message, e.Message));
        }

        private static T Retry<T>(Action action, Action<T> validator)
            where T : Exception
        {
            T exception = null;

            for (int i = 0; i < 1; i++)
            {
                exception = Assert.Throws<T>(action);
                validator(exception);
            }

            return exception;
        }
    }
}
