// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static class CallerArgumentExpressionAttributeTests
    {
        public static string IntParamMethod(int val, [CallerArgumentExpression("val")] string expr = null)
        {
            return expr;
        }

        [Theory, InlineData("testParamName"), InlineData(""), InlineData(null)]
        public static void ArgumentToCallerArgumentExpressionSetsParameterNameProperty(string paramName)
        {
            var attr = new CallerArgumentExpressionAttribute(paramName);

            Assert.Equal(paramName, attr.ParameterName);
        }

        [Fact]
        public static void SuppliedArgumentOverridesExpression()
        {
            int notVal = 0;

            string suppliedVal = "supplied value";
            Assert.Equal(suppliedVal, IntParamMethod(notVal, suppliedVal));

            Assert.Equal(IntParamMethod(notVal), IntParamMethodPassthrough(notVal));
        }

        private static string IntParamMethodPassthrough(int val, [CallerArgumentExpression("val")] string expr = null)
        {
            return IntParamMethod(val, expr);
        }

        [Fact]
        public static void InvalidParameterName()
        {
            int notVal = 0;

            Assert.Null(InvalidParameterNameMethod(notVal));
        }

        private static string InvalidParameterNameMethod(int val, [CallerArgumentExpression("notVal")] string expr = null)
        {
            return expr;
        }

        [Fact]
        public static void NullParameterName()
        {
            int notVal = 0;

            Assert.Null(NullParameterNameMethod(notVal));
        }

        private static string NullParameterNameMethod(int val, [CallerArgumentExpression(null)] string expr = null)
        {
            return expr;
        }

        [Fact]
        public static void OverloadedMethodPrecedence()
        {
            int notVal = 0;

            Assert.Equal(OverloadedMethodReturn, OverloadedMethod(notVal));
        }

        private const string OverloadedMethodReturn = "not CallerArgumentExpression";
 
        private static string OverloadedMethod(int val)
        {
            return OverloadedMethodReturn;
        }

        private static string OverloadedMethod(int val, [CallerArgumentExpression(null)] string expr = null)
        {
            return expr;
        }

        [Fact(Skip = "Not yet implemented in compiler. Final behavior may differ.")]
        [ActiveIssue("https://github.com/dotnet/roslyn/issues/19605", TestPlatforms.Any)]
        public static void SimpleExpression()
        {
            int notVal = 0;

            Assert.Equal("notVal", IntParamMethod(notVal));
        }

        [Fact(Skip = "Not yet implemented in compiler. Final behavior may differ.")]
        [ActiveIssue("https://github.com/dotnet/roslyn/issues/19605", TestPlatforms.Any)]
        public static void ComplexExpression()
        {
            int x = 5;

            Assert.Equal("Math.Min(x + 20, x * x)",
                IntParamMethod(Math.Min(x + 20, x * x)));
        }

        [Fact(Skip = "Not yet implemented in compiler. Final behavior may differ.")]
        [ActiveIssue("https://github.com/dotnet/roslyn/issues/19605", TestPlatforms.Any)]
        public static void SurroundingWhitespaceHandling()
        {
            int notVal = 0;

            Assert.Equal("notVal", IntParamMethod(notVal));
        }

        [Fact(Skip = "Not yet implemented in compiler. Final behavior may differ.")]
        [ActiveIssue("https://github.com/dotnet/roslyn/issues/19605", TestPlatforms.Any)]
        public static void InternalWhitespaceHandling()
        {
            int notVal = 0;

            Assert.Equal("notVal  + 20", IntParamMethod(notVal + 20));

            Assert.Equal(@"Math.Min(notVal * 2,
                    notVal + 20)",
                IntParamMethod(Math.Min(notVal * 2,
                    notVal + 20)));
        }

        [Fact(Skip = "Not yet implemented in compiler. Final behavior may differ.")]
        [ActiveIssue("https://github.com/dotnet/roslyn/issues/19605", TestPlatforms.Any)]
        public static void InternalCommentHandling()
        {
            int notVal = 0;

            Assert.Equal("notVal + /*comment*/20", IntParamMethod(notVal + /*comment*/20));
            Assert.Equal("notVal + 20 //comment",
                IntParamMethod(notVal + 20 //comment
                ));
        }

        [Fact(Skip = "Not yet implemented in compiler. Final behavior may differ.")]
        [ActiveIssue("https://github.com/dotnet/roslyn/issues/19605", TestPlatforms.Any)]
        public static void OptionalParameterHandling()
        {
            string suppliedVal = "supplied value";
            Assert.Equal("suppliedVal", OptionalParamMethod(suppliedVal));
            Assert.Equal("suppliedVal", OptionalParamMethod(val: suppliedVal));

            Assert.Equal("StringConst + \" string literal\"", OptionalParamMethod());

            Assert.Equal("\"no file\"", CompilerSuppliedParamMethod());
        }

        private const string StringConst = "hello";

        private static string OptionalParamMethod(string val = StringConst + " string literal", [CallerArgumentExpression("val")] string expr = null)
        {
            return expr;
        }

        private static string CompilerSuppliedParamMethod([CallerFilePath] string val = "no file", [CallerArgumentExpression("val")] string expr = null)
        {
            return expr;
        }

        [Fact(Skip = "Not yet implemented in compiler. Final behavior may differ.")]
        [ActiveIssue("https://github.com/dotnet/roslyn/issues/19605", TestPlatforms.Any)]
        public static void ExtensionMethodThisParameterHandling()
        {
            int notVal = 0;

            Assert.Equal("notVal", notVal.ExtensionMethod());
        }

        private static string ExtensionMethod(this int val, [CallerArgumentExpression("val")] string expr = null)
        {
            return expr;
        }

        [Fact(Skip = "Not yet implemented in compiler. Final behavior may differ.")]
        [ActiveIssue("https://github.com/dotnet/roslyn/issues/19605", TestPlatforms.Any)]
        public static void InstanceMethodThisHandling()
        {
            var instance = new InstanceTest();

            Assert.Equal("instance", instance.Method());
            Assert.Equal("new InstanceTest()", new InstanceTest().Method());
            Assert.Equal("(instance ?? new InstanceTest())", (instance ?? new InstanceTest()).Method());

            Assert.Equal("", instance.NoThisMethodCaller());
            Assert.Equal("this", instance.ThisMethodCaller());
        }

        private class InstanceTest
        {
            public string NoThisMethodCaller()
            {
                return Method();
            }

            public string ThisMethodCaller()
            {
                return this.Method();
            }

            public string Method([CallerArgumentExpression("this")] string expr = null)
            {
                return expr;
            }
        }
    }
}
