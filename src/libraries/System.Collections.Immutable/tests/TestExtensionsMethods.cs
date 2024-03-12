// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace System.Collections.Immutable.Tests
{
    internal static partial class TestExtensionsMethods
    {
        private static readonly double s_GoldenRatio = (1 + Math.Sqrt(5)) / 2;

        internal static void ValidateDefaultThisBehavior(Action a)
        {
            Assert.Throws<NullReferenceException>(a);
        }

        internal static void ValidateDefaultThisBehavior<TArg>(ReadOnlySpan<TArg> span, AssertExtensions.AssertThrowsActionReadOnly<TArg> action)
        {
            try
            {
                action(span);
            }
            catch (NullReferenceException nullRefEx) when (nullRefEx.GetType() == typeof(NullReferenceException))
            {
                return;
            }
            catch (Exception ex)
            {
                throw ThrowsException.ForIncorrectExceptionType(typeof(NullReferenceException), ex);
            }

            throw ThrowsException.ForNoException(typeof(NullReferenceException));
        }
    }
}
