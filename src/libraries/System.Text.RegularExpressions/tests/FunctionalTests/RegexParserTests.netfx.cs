// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Sdk;

namespace System.Text.RegularExpressions.Tests
{
    public partial class RegexParserTests
    {
        /// <summary>
        /// Checks that action throws either a RegexParseException or an ArgumentException depending on the
        /// environment and the supplied error.
        /// </summary>
        /// <param name="error">The expected parse error</param>
        /// <param name="action">The action to invoke.</param>
        static partial void Throws(string pattern, RegexOptions options, RegexParseError error, int offset, Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                // On NetFramework, all we care about is whether the exception is thrown.
                return;
            }
            catch (Exception e)
            {
                throw new XunitException($"Expected ArgumentException -> Actual: {e}");
            }

            throw new XunitException($"Expected ArgumentException with error: ({error}) -> Actual: No exception thrown");
        }

        /// <summary>
        /// Checks that action succeeds or throws either a RegexParseException or an ArgumentException depending on the
        // environment and the action.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        static partial void MayThrow(Action action)
        {
            if (Record.Exception(action) is Exception e && e is not ArgumentException)
            {
                throw new XunitException($"Expected ArgumentException or no exception -> Actual: ({e})");
            }
        }
    }
}
