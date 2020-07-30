using System;
using System.Threading.Tasks;
using Xunit;
using System.Globalization;

namespace DebuggerTests
{
	public class DateTimeList : DebuggerTestBase {

		[Theory]
		[InlineData ("en-US")]
		[InlineData ("de-DE")]
		[InlineData ("ka-GE")]
		[InlineData ("hu-HU")]
        
        // Currently not passing tests. Issue #19743
        // [InlineData ("ja-JP")]
        // [InlineData ("es-ES")]
		public async Task CheckDateTimeLocale (string locale) {
			var insp = new Inspector ();
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-datetime-test.cs";

				await SetBreakpointInMethod ("debugger-test", "DebuggerTests.DateTimeTest", "LocaleTest", 15);
				
				var pause_location = await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.DateTimeTest:LocaleTest'," 
					+ $"'{locale}'); }}, 1);",
					debugger_test_loc, 20, 3, "LocaleTest",
					locals_fn: async (locals) => {
						DateTimeFormatInfo dtfi = CultureInfo.GetCultureInfo(locale).DateTimeFormat;
						CultureInfo.CurrentCulture = new CultureInfo (locale, false);
						DateTime dt = new DateTime (2020, 1, 2, 3, 4, 5);
						string dt_str = dt.ToString();

						var fdtp = dtfi.FullDateTimePattern;
						var ldp = dtfi.LongDatePattern;
						var ltp = dtfi.LongTimePattern;
						var sdp = dtfi.ShortDatePattern;
						var stp = dtfi.ShortTimePattern;

						CheckString(locals, "fdtp", fdtp);
						CheckString(locals, "ldp", ldp);
						CheckString(locals, "ltp", ltp);
						CheckString(locals, "sdp", sdp);
						CheckString(locals, "stp", stp);
						await CheckDateTime(locals, "dt", dt);
						CheckString(locals, "dt_str", dt_str);
					}
				);
				
				
			});
		}

	}
}