// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class UnaryIsFalseNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryIsFalseNullableBoolTest(bool useInterpreter)
        {
            bool?[] values = new bool?[] { null, false, true };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIsFalseNullableBool(values[i], useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyIsFalseNullableBool(bool? value, bool useInterpreter)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.IsFalse(Expression.Constant(value, typeof(bool?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?> f = e.Compile(useInterpreter);
            Assert.Equal((bool?)(value == default(bool?) ? default(bool?) : value == false), f());
        }


        #endregion
    }
}
