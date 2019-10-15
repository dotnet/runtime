// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Linq.Expressions.Tests
{
    /// <summary>Defines an order in which tests must be taken, enforced by <see cref="TestOrderer"/></summary>
    /// <remarks>Order must be non-negative. Tests ordered as zero take place in the same batch as those
    /// with no such attribute set.</remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal class TestOrderAttribute : Attribute
    {
        /// <summary>
        /// Initializes a <see cref="TestOrderAttribute"/> object.
        /// </summary>
        /// <param name="order">The order of the batch in which the test must run.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="order"/> was less than zero.</exception>
        public TestOrderAttribute(int order)
        {
            if (order < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(order));
            }

            Order = order;
        }

        /// <summary>The order of the batch in which the test must run.</summary>
        public int Order { get; private set; }
    }
}
