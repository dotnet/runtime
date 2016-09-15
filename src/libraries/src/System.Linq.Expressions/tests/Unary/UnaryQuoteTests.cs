// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

using static System.Linq.Expressions.Expression;

namespace System.Linq.Expressions.Tests
{
    public class UnaryQuoteTests
    {
        [Theory, ClassData(typeof(CompilationTypes))]
        public void QuotePreservesTypingOfBlock(bool useInterpreter)
        {
            var x = Parameter(typeof(int));

            var f1 =
                Lambda<Func<int, Type>>(
                    Call(
                        typeof(UnaryQuoteTests).GetMethod(nameof(Quote1)),
                        Lambda(
                            Block(typeof(void), x)
                        )
                    ),
                    x
                );

            Assert.Equal(typeof(void), f1.Compile(useInterpreter)(42));

            var s = Parameter(typeof(string));

            var f2 =
                Lambda<Func<string, Type>>(
                    Call(
                        typeof(UnaryQuoteTests).GetMethod(nameof(Quote2)),
                        Lambda(
                            Block(typeof(object), s)
                        )
                    ),
                    s
                );

            Assert.Equal(typeof(object), f2.Compile(useInterpreter)("bar"));
        }

        public static Type Quote1(Expression<Action> e) => e.Body.Type;
        public static Type Quote2(Expression<Func<object>> e) => e.Body.Type;
    }
}
