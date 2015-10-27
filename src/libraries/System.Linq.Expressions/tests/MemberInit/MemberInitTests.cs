// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.MemberInit
{
    public static class MemberInitTests
    {
        #region Test methods

        [Fact] // [Issue(4018, "https://github.com/dotnet/corefx/issues/4018")]
        public static void CheckMemberInitTest()
        {
            VerifyMemberInit(() => new X { Y = { Z = 42, YS = { 2, 3 } }, XS = { 5, 7 } }, x => x.Y.Z == 42 && x.XS.Sum() == 5 + 7 && x.Y.YS.Sum() == 2 + 3);
        }

        #endregion

        #region Test verifiers

        private static void VerifyMemberInit<T>(Expression<Func<T>> expr, Func<T, bool> check)
        {
            Func<T> c = expr.Compile();
            Assert.True(check(c()));

#if FEATURE_INTERPRET
            Func<T> i = expr.Compile(true);
            Assert.True(check(i()));
#endif
        }

        #endregion

        #region Helpers

        class X
        {
            private readonly Y _y = new Y();
            private readonly List<int> _xs = new List<int>();

            public Y Y
            {
                get { return _y; }
            }

            public List<int> XS { get { return _xs; } }
        }

        class Y
        {
            private readonly List<int> _ys = new List<int>();

            public int Z { get; set; }

            public List<int> YS { get { return _ys; } }
        }

        #endregion
    }
}

