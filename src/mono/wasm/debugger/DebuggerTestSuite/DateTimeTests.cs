// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace DebuggerTests
{
    public class DateTimeTests : DebuggerTestBase
    {

        [Theory]
        [InlineData("en-US", "dddd, MMMM d, yyyy h:mm:ss tt", "dddd, MMMM d, yyyy", "h:mm:ss tt", "M/d/yyyy", "h:mm tt")]
        [InlineData("ja-JP", "yyyy年M月d日dddd H:mm:ss", "yyyy年M月d日dddd", "H:mm:ss", "yyyy/MM/dd", "H:mm")]
        [InlineData("es-ES", "dddd, d 'de' MMMM 'de' yyyy H:mm:ss", "dddd, d 'de' MMMM 'de' yyyy", "H:mm:ss", "d/M/yyyy", "H:mm")]
        [InlineData("de-DE", "dddd, d. MMMM yyyy HH:mm:ss", "dddd, d. MMMM yyyy", "HH:mm:ss", "dd.MM.yyyy", "HH:mm")]
        public async Task CheckDateTimeLocale(string locale, string fdtp, string ldp, string ltp, string sdp, string stp)
        {
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-datetime-test.cs";

            await SetBreakpointInMethod("debugger-test", "DebuggerTests.DateTimeTest", "LocaleTest", 15);

            var pause_location = await EvaluateAndCheck(
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.DateTimeTest:LocaleTest'," +
                $"'{locale}'); }}, 1);",
                debugger_test_loc, 25, 12, "LocaleTest",
                locals_fn: async (locals) =>
                {
                    DateTimeFormatInfo dtfi = CultureInfo.GetCultureInfo(locale).DateTimeFormat;
                    CultureInfo.CurrentCulture = new CultureInfo(locale, false);

                    await CheckProps(locals, new
                    {
                        fdtp = TString(fdtp),
                        ldp = TString(ldp),
                        ltp = TString(ltp),
                        sdp = TString(sdp),
                        stp = TString(stp),
                        dt = TDateTime(new DateTime(2020, 1, 2, 3, 4, 5))
                    }, "locals", num_fields: 8);
                }
            );
        }

    }
}
