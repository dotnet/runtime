// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class UserDefinedShortCircuitOperators
    {
        private class NumTypeBitwiseOpsOnly
        {
            public int Value;

            public static NumTypeBitwiseOpsOnly operator &(NumTypeBitwiseOpsOnly x, NumTypeBitwiseOpsOnly y)
                => new NumTypeBitwiseOpsOnly{ Value = x.Value & y.Value };

            public static NumTypeBitwiseOpsOnly operator |(NumTypeBitwiseOpsOnly x, NumTypeBitwiseOpsOnly y)
                => new NumTypeBitwiseOpsOnly{ Value = x.Value | y.Value };
        }

        private class NumTypeBitwiseAndBoolCast
        {
            public int Value;

            public static NumTypeBitwiseAndBoolCast operator &(NumTypeBitwiseAndBoolCast x, NumTypeBitwiseAndBoolCast y)
                => new NumTypeBitwiseAndBoolCast { Value = x.Value & y.Value };

            public static NumTypeBitwiseAndBoolCast operator |(NumTypeBitwiseAndBoolCast x, NumTypeBitwiseAndBoolCast y)
                => new NumTypeBitwiseAndBoolCast { Value = x.Value | y.Value };

            public static implicit operator bool(NumTypeBitwiseAndBoolCast val) => val.Value != 0;
        }

        private class NumTypeBitwiseAndTruthiness
        {
            public int Value;

            public static NumTypeBitwiseAndTruthiness operator &(NumTypeBitwiseAndTruthiness x, NumTypeBitwiseAndTruthiness y)
                => new NumTypeBitwiseAndTruthiness { Value = x.Value & y.Value };

            public static NumTypeBitwiseAndTruthiness operator |(NumTypeBitwiseAndTruthiness x, NumTypeBitwiseAndTruthiness y)
                => new NumTypeBitwiseAndTruthiness { Value = x.Value | y.Value };

            public static bool operator true(NumTypeBitwiseAndTruthiness val) => val.Value != 0;

            public static bool operator false(NumTypeBitwiseAndTruthiness val) => val.Value == 0;
        }

        private class NumTypeBitwiseTruthinessAndBoolCast
        {
            public int Value;

            public static NumTypeBitwiseTruthinessAndBoolCast operator &(NumTypeBitwiseTruthinessAndBoolCast x, NumTypeBitwiseTruthinessAndBoolCast y)
                => new NumTypeBitwiseTruthinessAndBoolCast { Value = x.Value & y.Value };

            public static NumTypeBitwiseTruthinessAndBoolCast operator |(NumTypeBitwiseTruthinessAndBoolCast x, NumTypeBitwiseTruthinessAndBoolCast y)
                => new NumTypeBitwiseTruthinessAndBoolCast { Value = x.Value | y.Value };

            public static implicit operator bool(NumTypeBitwiseTruthinessAndBoolCast val) => val.Value != 0;

            public static bool operator true(NumTypeBitwiseTruthinessAndBoolCast val) => val.Value != 0;

            public static bool operator false(NumTypeBitwiseTruthinessAndBoolCast val) => val.Value == 0;
        }

        [Fact]
        public void CannotShortCircuitWithoutBoolOperators()
        {
            dynamic x = new NumTypeBitwiseOpsOnly { Value = -1 };
            dynamic y = new NumTypeBitwiseOpsOnly { Value = 0 };
            Assert.Throws<RuntimeBinderException>(() => x && x);
            Assert.Throws<RuntimeBinderException>(() => x && y);
            Assert.Throws<RuntimeBinderException>(() => y && x);
            Assert.Throws<RuntimeBinderException>(() => y && y);
            Assert.Throws<RuntimeBinderException>(() => x || x);
            Assert.Throws<RuntimeBinderException>(() => x || y);
            Assert.Throws<RuntimeBinderException>(() => y || x);
            Assert.Throws<RuntimeBinderException>(() => y || y);
        }

        [Fact]
        public void CannotShortCircuitWithBoolCast()
        {
            dynamic x = new NumTypeBitwiseAndBoolCast { Value = -1 };
            dynamic y = new NumTypeBitwiseAndBoolCast { Value = 0 };
            Assert.Throws<RuntimeBinderException>(() => x && x);
            Assert.Throws<RuntimeBinderException>(() => x && y);

            // Because the expression is evaluated dynamically, if casting the first operand to bool
            // gives the short-circuiting answer, then that is the result, even though with static
            // compilation this would result in CS0218, because the short-circuiting short-circuits
            // the check that would throw!
            Assert.Equal(0, (y && x).Value);
            Assert.Equal(0, (y && y).Value);
            Assert.Equal(-1, (x || x).Value);
            Assert.Equal(-1, (x || y).Value);

            Assert.Throws<RuntimeBinderException>(() => y || x);
            Assert.Throws<RuntimeBinderException>(() => y || y);
        }

        [Fact]
        public void CannotShortCircuitWithTruthiness()
        {
            dynamic x = new NumTypeBitwiseAndTruthiness { Value = -1 };
            dynamic y = new NumTypeBitwiseAndTruthiness { Value = 0 };
            Assert.Equal(-1, (x && x).Value);
            Assert.Equal(0, (x && y).Value);
            Assert.Equal(0, (y && x).Value);
            Assert.Equal(0, (y && y).Value);
            Assert.Equal(-1, (x || x).Value);
            Assert.Equal(-1, (x || y).Value);
            Assert.Equal(-1, (y || x).Value);
            Assert.Equal(0, (y || y).Value);
        }

        [Fact]
        public void CannotShortCircuitWithTruthinessAndBoolCast()
        {
            dynamic x = new NumTypeBitwiseTruthinessAndBoolCast { Value = -1 };
            dynamic y = new NumTypeBitwiseTruthinessAndBoolCast { Value = 0 };
            Assert.Equal(-1, (x && x).Value);
            Assert.Equal(0, (x && y).Value);
            Assert.Equal(0, (y && x).Value);
            Assert.Equal(0, (y && y).Value);
            Assert.Equal(-1, (x || x).Value);
            Assert.Equal(-1, (x || y).Value);
            Assert.Equal(-1, (y || x).Value);
            Assert.Equal(0, (y || y).Value);
        }
    }
}
