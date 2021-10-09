// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class FormattedLogValuesTest
    {
        [Theory]
        [InlineData("", "", null)]
        [InlineData("", "", new object[] { })]
        [InlineData("arg1 arg2", "{0} {1}", new object[] { "arg1", "arg2" })]
        [InlineData("arg1 arg2", "{Start} {End}", new object[] { "arg1", "arg2" })]
        [InlineData("arg1     arg2", "{Start,-6} {End,6}", new object[] { "arg1", "arg2" })]
        [InlineData("0064", "{Hex:X4}", new object[] { 100 })]
        [InlineData("8,765", "{Number:#,#}", new object[] { 8765.4321 })]
        [InlineData(" 8,765", "{Number,6:#,#}", new object[] { 8765.4321 })]
        public void LogValues_With_Basic_Types(string expected, string format, object[] args)
        {
            var logValues = new FormattedLogValues(format, args);
            Assert.Equal(expected, logValues.ToString());

            // Original format is expected to be returned from GetValues.
            Assert.Equal(format, logValues.First(v => v.Key == "{OriginalFormat}").Value);
        }

        [Theory]
        [InlineData("[null]", null, null)]
        [InlineData("[null]", null, new object[] { })]
        [InlineData("[null]", null, new object[] { null })]
        [InlineData("[null]", null, new object[] { 1 })]
        public void Log_NullFormat(string expected, string format, object[] args)
        {
            var logValues = new FormattedLogValues(format, args);
            Assert.Equal(expected, logValues.ToString());
        }

        [Theory]
        [InlineData("(null), (null) : (null)", "{0} : {1}", new object[] { new object[] { null, null }, null })]
        [InlineData("(null)", "{0}", new object[] { null })]
        public void LogValues_WithNulls(string expected, string format, object[] args)
        {
            var logValues = new FormattedLogValues(format, args);
            Assert.Equal(expected, logValues.ToString());
        }

        [Theory]
        [InlineData("1 2015", "{Year,6:d yyyy}")]
        [InlineData("1:01:2015 AM,:        01", "{Year,-10:d:MM:yyyy tt},:{second,10:ss}")]
        [InlineData("{prefix{1 2015}suffix}", "{{prefix{{{Year,6:d yyyy}}}suffix}}")]
        public void LogValues_With_DateTime(string expected, string format)
        {
            var dateTime = new DateTime(2015, 1, 1, 1, 1, 1);
            var logValues = new FormattedLogValues(format, new object[] { dateTime, dateTime });
            Assert.Equal(expected, logValues.ToString());

            // Original format is expected to be returned from GetValues.
            Assert.Equal(format, logValues.First(v => v.Key == "{OriginalFormat}").Value);
        }

        [Theory]
        [InlineData("{{", "{{", null)]
        [InlineData("'{{'", "'{{'", null)]
        [InlineData("'{{}}'", "'{{}}'", null)]
        [InlineData("arg1 arg2 '{}'  '{' '{:}' '{,:}' {,}- test string",
            "{0} {1} '{{}}'  '{{' '{{:}}' '{{,:}}' {{,}}- test string",
            new object[] { "arg1", "arg2" })]
        [InlineData("{prefix{arg1}suffix}", "{{prefix{{{Argument}}}suffix}}", new object[] { "arg1" })]
        public void LogValues_With_Escaped_Braces(string expected, string format, object[] args)
        {
            var logValues = args == null ?
                new FormattedLogValues(format) :
                new FormattedLogValues(format, args);

            Assert.Equal(expected, logValues.ToString());

            // Original format is expected to be returned from GetValues.
            Assert.Equal(format, logValues.First(v => v.Key == "{OriginalFormat}").Value);
        }

        [Theory]
        [InlineData("{foo")]
        [InlineData("bar}")]
        [InlineData("{foo bar}}")]
        public void LogValues_With_UnbalancedBraces(string format)
        {
            Assert.Throws<FormatException>(() =>
            {
                var logValues = new FormattedLogValues(format, new object[] { "arg1" });
                logValues.ToString();
            });
        }

        [Fact]
        public void LogValues_WithNullAndEnumerable_IsNotMutatingParameter()
        {
            string format = "TestMessage {Param1} {Param2} {Param3} {Param4}";
            int param1 = 1;
            string param2 = null;
            int[] param3 = new[] { 1, 2, 3, 4 };
            string param4 = "string";

            var logValues = new FormattedLogValues(format, param1, param2, param3, param4);
            logValues.ToString();

            var state = logValues.ToArray();
            Assert.Equal(new[]
            {
                new KeyValuePair<string, object>("Param1", param1),
                new KeyValuePair<string, object>("Param2", param2),
                new KeyValuePair<string, object>("Param3", param3),
                new KeyValuePair<string, object>("Param4", param4),
                new KeyValuePair<string, object>("{OriginalFormat}", format),
            }, state);
        }

        [Fact]
        public void CachedFormattersAreCapped()
        {
            for (var i = 0; i < FormattedLogValues.MaxCachedFormatters; ++i)
            {
                var ignore = new FormattedLogValues($"{i}{{i}}", i);
            }

            // check cached formatter
            var formatter = new FormattedLogValues("0{i}", 0).Formatter;
            Assert.Same(formatter, new FormattedLogValues("0{i}", 0).Formatter);

            // check non-cached formatter
            formatter = new FormattedLogValues("test {}", 0).Formatter;
            Assert.NotSame(formatter, new FormattedLogValues("test {}", 0).Formatter);
        }

        // message format, format arguments, expected message
        public static TheoryData<string, object[], string> FormatsEnumerableValuesData
        {
            get
            {
                return new TheoryData<string, object[], string>
                {
                    // null value
                    {
                        "The view '{ViewName}' was not found. Searched locations: {SearchedLocations}",
                        new object[] { "Index", null },
                        "The view 'Index' was not found. Searched locations: (null)"
                    },
                    // empty enumerable
                    {
                        "The view '{ViewName}' was not found. Searched locations: {SearchedLocations}",
                        new object[] { "Index", new string[] { } },
                        "The view 'Index' was not found. Searched locations: "
                    },
                    // single item enumerable
                    {
                        "The view '{ViewName}' was not found. Searched locations: {SearchedLocations}",
                        new object[] { "Index", new[] { "Views/Home/Index.cshtml" } },
                        "The view 'Index' was not found. Searched locations: Views/Home/Index.cshtml"
                    },
                    // null value item in enumerable
                    {
                        "The view '{ViewName}' was not found. Searched locations: {SearchedLocations}",
                        new object[] { "Index", new string[] { null } },
                        "The view 'Index' was not found. Searched locations: (null)"
                    },
                    // null value item in enumerable
                    {
                        "The view '{ViewName}' was not found. Searched locations: {SearchedLocations}",
                        new object[] { "Index", new string[] { null, "Views/Home/Index.cshtml" } },
                        "The view 'Index' was not found. Searched locations: (null), Views/Home/Index.cshtml"
                    },
                    // multi item enumerable
                    {
                        "The view '{ViewName}' was not found. Searched locations: {SearchedLocations}",
                        new object[] { "Index", new[] { "Views/Home/Index.cshtml", "Views/Shared/Index.cshtml" } },
                        "The view 'Index' was not found. Searched locations: " +
                        "Views/Home/Index.cshtml, Views/Shared/Index.cshtml"
                    },
                    // non-string enumerable. ToString() should be called on non-string types
                    {
                        "Media type '{MediaType}' did not match any of the supported media types." +
                        "Supported media types: {SupportedMediaTypes}",
                        new object[]
                        {
                            new MediaType("application", "blah"),
                            new[]
                            {
                                new MediaType("text", "foo"),
                                new MediaType("application", "xml")
                            }
                        },
                        "Media type 'application/blah' did not match any of the supported media types." +
                        "Supported media types: text/foo, application/xml"
                    },
                    // List<string> parameter
                    {
                        "The view '{ViewName}' was not found. Searched locations: {SearchedLocations}",
                        new object[] { "Index", new[] { "Home/Index.cshtml", "Shared/Index.cshtml" }.ToList() },
                        "The view 'Index' was not found. Searched locations: " +
                        "Home/Index.cshtml, Shared/Index.cshtml"
                    },
                    // We support only one level of enumerable value
                    {
                        "The view '{ViewName}' was not found. Searched locations: {SearchedLocations}",
                        new object[] { "Index", new[] { new[] { "abc", "def" }, new[] { "ghi", "jkl" } } },
                        "The view 'Index' was not found. Searched locations: " +
                        "System.String[], System.String[]"
                    },
                    // sub-enumerable having null value item
                    {
                        "The view '{ViewName}' was not found. Searched locations: {SearchedLocations}",
                        new object[] { "Index", new Uri[][] { null, new[] { new Uri("http://def") } } },
                        "The view 'Index' was not found. Searched locations: " +
                        "(null), System.Uri[]"
                    },
                    // non-string sub-enumerables
                    {
                        "The view '{ViewName}' was not found. Searched locations: {SearchedLocations}",
                        new object[] { "Index", new[] { new Uri[] { null }, new[] { new Uri("http://def") } } },
                        "The view 'Index' was not found. Searched locations: " +
                        "System.Uri[], System.Uri[]"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(FormatsEnumerableValuesData))]
        public void FormatsEnumerableValues(string messageFormat, object[] arguments, string expected)
        {
            var logValues = new FormattedLogValues(messageFormat, arguments);

            Assert.Equal(expected, logValues.ToString());
        }

        private class MediaType
        {
            public MediaType(string type, string subType)
            {
                Type = type;
                SubType = subType;
            }

            public string Type { get; }

            public string SubType { get; }

            public override string ToString()
            {
                return $"{Type}/{SubType}";
            }
        }
    }
}
