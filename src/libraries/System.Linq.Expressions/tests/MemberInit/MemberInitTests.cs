// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class MemberInitTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckMemberInitTest(bool useInterpreter)
        {
            VerifyMemberInit(() => new X { Y = { Z = 42, YS = { 2, 3 } }, XS = { 5, 7 } }, x => x.Y.Z == 42 && x.XS.Sum() == 5 + 7 && x.Y.YS.Sum() == 2 + 3, useInterpreter);
        }

        #endregion

        #region Test verifiers

        private static void VerifyMemberInit<T>(Expression<Func<T>> expr, Func<T, bool> check, bool useInterpreter)
        {
            Func<T> c = expr.Compile(useInterpreter);
            Assert.True(check(c()));
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

