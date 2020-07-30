using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using WebAssembly.Net.Debugging;

[assembly: CollectionBehavior (CollectionBehavior.CollectionPerAssembly)]

namespace DebuggerTests
{

	public class SourceList : DebuggerTestBase {

		[Fact]
		public async Task CheckThatAllSourcesAreSent () {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			//all sources are sent before runtime ready is sent, nothing to check
			await insp.Ready ();
			Assert.Contains ("dotnet://debugger-test.dll/debugger-test.cs", scripts.Values);
			Assert.Contains ("dotnet://debugger-test.dll/debugger-test2.cs", scripts.Values);
			Assert.Contains ("dotnet://Simple.Dependency.dll/dependency.cs", scripts.Values);
		}

		[Fact]
		public async Task CreateGoodBreakpoint () {
			var insp = new Inspector ();

			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready ();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);

				var bp1_res = await SetBreakpoint ("dotnet://debugger-test.dll/debugger-test.cs", 5, 2);

				Assert.EndsWith ("debugger-test.cs", bp1_res.Value ["breakpointId"].ToString());
				Assert.Equal (1, bp1_res.Value ["locations"]?.Value<JArray> ()?.Count);

				var loc = bp1_res.Value ["locations"]?.Value<JArray> ()[0];

				Assert.NotNull (loc ["scriptId"]);
				Assert.Equal("dotnet://debugger-test.dll/debugger-test.cs", scripts [loc["scriptId"]?.Value<string> ()]);
				Assert.Equal (5, loc ["lineNumber"]);
				Assert.Equal (2, loc ["columnNumber"]);
			});
		}

		[Fact]
		public async Task CreateJSBreakpoint () {
			// Test that js breakpoints get set correctly
			var insp = new Inspector ();

			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready ();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				// 13 24
				// 13 31
				var bp1_res = await SetBreakpoint ("/debugger-driver.html", 13, 24);

				Assert.EndsWith ("debugger-driver.html", bp1_res.Value ["breakpointId"].ToString());
				Assert.Equal (1, bp1_res.Value ["locations"]?.Value<JArray> ()?.Count);

				var loc = bp1_res.Value ["locations"]?.Value<JArray> ()[0];

				Assert.NotNull (loc ["scriptId"]);
				Assert.Equal (13, loc ["lineNumber"]);
				Assert.Equal (24, loc ["columnNumber"]);

				var bp2_res = await SetBreakpoint ("/debugger-driver.html", 13, 31);

				Assert.EndsWith ("debugger-driver.html", bp2_res.Value ["breakpointId"].ToString());
				Assert.Equal (1, bp2_res.Value ["locations"]?.Value<JArray> ()?.Count);

				var loc2 = bp2_res.Value ["locations"]?.Value<JArray> ()[0];

				Assert.NotNull (loc2 ["scriptId"]);
				Assert.Equal (13, loc2 ["lineNumber"]);
				Assert.Equal (31, loc2 ["columnNumber"]);
			});
		}

		[Fact]
		public async Task CreateJS0Breakpoint () {
			// Test that js column 0 does as expected
			var insp = new Inspector ();

			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready ();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				// 13 24
				// 13 31
				var bp1_res = await SetBreakpoint ("/debugger-driver.html", 13, 0);

				Assert.EndsWith ("debugger-driver.html", bp1_res.Value ["breakpointId"].ToString());
				Assert.Equal (1, bp1_res.Value ["locations"]?.Value<JArray> ()?.Count);

				var loc = bp1_res.Value ["locations"]?.Value<JArray> ()[0];

				Assert.NotNull (loc ["scriptId"]);
				Assert.Equal (13, loc ["lineNumber"]);
				Assert.Equal (24, loc ["columnNumber"]);

				var bp2_res = await SetBreakpoint ("/debugger-driver.html", 13, 31);

				Assert.EndsWith ("debugger-driver.html", bp2_res.Value ["breakpointId"].ToString());
				Assert.Equal (1, bp2_res.Value ["locations"]?.Value<JArray> ()?.Count);

				var loc2 = bp2_res.Value ["locations"]?.Value<JArray> ()[0];

				Assert.NotNull (loc2 ["scriptId"]);
				Assert.Equal (13, loc2 ["lineNumber"]);
				Assert.Equal (31, loc2 ["columnNumber"]);
			});
		}

		[Theory]
		[InlineData (0)]
		[InlineData (44)]
		public async Task CheckMultipleBreakpointsOnSameLine (int col) {
			var insp = new Inspector ();

			var scripts = SubscribeToScripts(insp);

			await Ready ();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);

				var bp1_res = await SetBreakpoint ("dotnet://debugger-test.dll/debugger-array-test.cs", 197, col);
				Assert.EndsWith ("debugger-array-test.cs", bp1_res.Value["breakpointId"].ToString());
				Assert.Equal (1, bp1_res.Value ["locations"]?.Value<JArray> ()?.Count);

				var loc = bp1_res.Value ["locations"]?.Value<JArray> ()[0];

				CheckLocation ("dotnet://debugger-test.dll/debugger-array-test.cs", 197, 44, scripts, loc);

				var bp2_res = await SetBreakpoint ("dotnet://debugger-test.dll/debugger-array-test.cs", 197, 49);
				Assert.EndsWith ("debugger-array-test.cs", bp2_res.Value["breakpointId"].ToString());
				Assert.Equal (1, bp2_res.Value ["locations"]?.Value<JArray> ()?.Count);

				var loc2 = bp2_res.Value ["locations"]?.Value<JArray> ()[0];

				CheckLocation ("dotnet://debugger-test.dll/debugger-array-test.cs", 197, 49, scripts, loc2);
			});
		}

		[Fact]
		public async Task CreateBadBreakpoint () {
			var insp = new Inspector ();

			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready ();
			await insp.Ready (async (cli, token) => {
				var bp1_req = JObject.FromObject(new {
					lineNumber = 5,
					columnNumber = 2,
					url = "dotnet://debugger-test.dll/this-file-doesnt-exist.cs",
				});

				var bp1_res = await cli.SendCommand ("Debugger.setBreakpointByUrl", bp1_req, token);

				Assert.True (bp1_res.IsOk);
				Assert.Empty (bp1_res.Value["locations"].Values<object>());
				//Assert.Equal ((int)MonoErrorCodes.BpNotFound, bp1_res.Error ["code"]?.Value<int> ());
			});
		}

		[Fact]
		public async Task CreateGoodBreakpointAndHit () {
			var insp = new Inspector ();

			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready ();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);

				var bp = await SetBreakpoint ("dotnet://debugger-test.dll/debugger-test.cs", 5, 2);

				var eval_req = JObject.FromObject(new {
					expression = "window.setTimeout(function() { invoke_add(); }, 1);",
				});

				await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_add(); }, 1);",
					"dotnet://debugger-test.dll/debugger-test.cs", 5, 2,
					"IntAdd",
					wait_for_event_fn: (pause_location) => {
						Assert.Equal ("other", pause_location ["reason"]?.Value<string> ());
						Assert.Equal (bp.Value["breakpointId"]?.ToString(), pause_location ["hitBreakpoints"]?[0]?.Value<string> ());

						var top_frame = pause_location ["callFrames"][0];
						Assert.Equal ("IntAdd", top_frame ["functionName"].Value<string>());
						Assert.Contains ("debugger-test.cs", top_frame ["url"].Value<string> ());

						CheckLocation ("dotnet://debugger-test.dll/debugger-test.cs", 3, 41, scripts, top_frame["functionLocation"]);

						//now check the scope
						var scope = top_frame ["scopeChain"][0];
						Assert.Equal ("local", scope ["type"]);
						Assert.Equal ("IntAdd", scope ["name"]);

						Assert.Equal ("object", scope ["object"]["type"]);
						Assert.Equal ("dotnet:scope:0", scope ["object"]["objectId"]);
						CheckLocation ("dotnet://debugger-test.dll/debugger-test.cs", 3, 41, scripts, scope["startLocation"]);
						CheckLocation ("dotnet://debugger-test.dll/debugger-test.cs", 9, 1, scripts, scope["endLocation"]);
						return Task.CompletedTask;
					}
				);

			});
		}

		[Fact]
		public async Task ExceptionThrownInJS () {
			var insp = new Inspector ();

			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready ();
			await insp.Ready (async (cli, token) => {
				var eval_req = JObject.FromObject(new {
					expression = "invoke_bad_js_test();"
				});

				var eval_res = await cli.SendCommand ("Runtime.evaluate", eval_req, token);
				Assert.True (eval_res.IsErr);
				Assert.Equal ("Uncaught", eval_res.Error ["exceptionDetails"]? ["text"]? .Value<string> ());
			});
		}

		[Fact]
		public async Task ExceptionThrownInJSOutOfBand () {
			var insp = new Inspector ();

			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready ();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);

				await SetBreakpoint ("/debugger-driver.html", 27, 2);

				var eval_req = JObject.FromObject(new {
					expression = "window.setTimeout(function() { invoke_bad_js_test(); }, 1);",
				});

				var eval_res = await cli.SendCommand ("Runtime.evaluate", eval_req, token);
				// Response here will be the id for the timer from JS!
				Assert.True (eval_res.IsOk);

				var ex = await Assert.ThrowsAsync<ArgumentException> (async () => await insp.WaitFor("Runtime.exceptionThrown"));
				var ex_json = JObject.Parse (ex.Message);
				Assert.Equal (dicFileToUrl["/debugger-driver.html"], ex_json ["exceptionDetails"]? ["url"]? .Value<string> ());
			});

		}

		[Theory]
		[InlineData (false)]
		[InlineData (true)]
		public async Task InspectLocalsAtBreakpointSite (bool use_cfo) =>
			await CheckInspectLocalsAtBreakpointSite (
				"dotnet://debugger-test.dll/debugger-test.cs", 5, 2, "IntAdd",
				"window.setTimeout(function() { invoke_add(); }, 1);",
				use_cfo: use_cfo,
				test_fn: (locals) => {
					CheckNumber (locals, "a", 10);
					CheckNumber (locals, "b", 20);
					CheckNumber (locals, "c", 30);
					CheckNumber (locals, "d", 0);
					CheckNumber (locals, "e", 0);
				}
			);

		[Fact]
		public async Task InspectPrimitiveTypeLocalsAtBreakpointSite () =>
			await CheckInspectLocalsAtBreakpointSite (
				"dotnet://debugger-test.dll/debugger-test.cs", 145, 2, "PrimitiveTypesTest",
				"window.setTimeout(function() { invoke_static_method ('[debugger-test] Math:PrimitiveTypesTest'); }, 1);",
				test_fn: (locals) => {
					CheckSymbol (locals, "c0", "8364 '€'");
					CheckSymbol (locals, "c1", "65 'A'");
				}
			);

		[Fact]
		public async Task InspectLocalsTypesAtBreakpointSite () =>
			await CheckInspectLocalsAtBreakpointSite (
				"dotnet://debugger-test.dll/debugger-test2.cs", 40, 2, "Types",
				"window.setTimeout(function() { invoke_static_method (\"[debugger-test] Fancy:Types\")(); }, 1);",
				use_cfo: false,
				test_fn: (locals) => {
						CheckNumber (locals, "dPI", Math.PI);
						CheckNumber (locals, "fPI", (float)Math.PI);
						CheckNumber (locals, "iMax", int.MaxValue);
						CheckNumber (locals, "iMin", int.MinValue);
						CheckNumber (locals, "uiMax", uint.MaxValue);
						CheckNumber (locals, "uiMin", uint.MinValue);

						CheckNumber (locals, "l", uint.MaxValue * (long)2);
						//CheckNumber (locals, "lMax", long.MaxValue); // cannot be represented as double
						//CheckNumber (locals, "lMin", long.MinValue); // cannot be represented as double

						CheckNumber (locals, "sbMax", sbyte.MaxValue);
						CheckNumber (locals, "sbMin", sbyte.MinValue);
						CheckNumber (locals, "bMax", byte.MaxValue);
						CheckNumber (locals, "bMin", byte.MinValue);

						CheckNumber (locals, "sMax", short.MaxValue);
						CheckNumber (locals, "sMin", short.MinValue);
						CheckNumber (locals, "usMin", ushort.MinValue);
						CheckNumber (locals, "usMax", ushort.MaxValue);
				}
			);

		[Theory]
		[InlineData (false)]
		[InlineData (true)]
		public async Task InspectLocalsWithGenericTypesAtBreakpointSite (bool use_cfo) =>
			await CheckInspectLocalsAtBreakpointSite (
				"dotnet://debugger-test.dll/debugger-test.cs", 65, 2, "GenericTypesTest",
				"window.setTimeout(function() { invoke_generic_types_test (); }, 1);",
				use_cfo: use_cfo,
				test_fn: (locals) => {
					CheckObject (locals, "list", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>");
					CheckObject (locals, "list_null", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>", is_null: true);

					CheckArray (locals, "list_arr", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]");
					CheckObject (locals, "list_arr_null", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]", is_null: true);

					// Unused locals
					CheckObject (locals, "list_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>");
					CheckObject (locals, "list_null_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>", is_null: true);

					CheckObject (locals, "list_arr_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]");
					CheckObject (locals, "list_arr_null_unused", "System.Collections.Generic.Dictionary<Math[], Math.IsMathNull>[]", is_null: true);
				}
			);

		object TGenericStruct(string typearg, string stringField)
			=> new {
				List = TObject ($"System.Collections.Generic.List<{typearg}>"),
				StringField = TString (stringField)
			};

		[Fact]
		public async Task RuntimeGetPropertiesWithInvalidScopeIdTest () {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready ();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);

				var bp = await SetBreakpoint ("dotnet://debugger-test.dll/debugger-test.cs", 41, 2);

				await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_delegates_test (); }, 1);",
					"dotnet://debugger-test.dll/debugger-test.cs", 41, 2,
					"DelegatesTest",
					wait_for_event_fn: async (pause_location) => {
						//make sure we're on the right bp
						Assert.Equal (bp.Value ["breakpointId"]?.ToString (), pause_location ["hitBreakpoints"]?[0]?.Value<string> ());

						var top_frame = pause_location ["callFrames"][0];

						var scope = top_frame ["scopeChain"][0];
						Assert.Equal ("dotnet:scope:0", scope ["object"]["objectId"]);

						// Try to get an invalid scope!
						var get_prop_req = JObject.FromObject(new {
							objectId = "dotnet:scope:23490871",
						});

						var frame_props = await cli.SendCommand ("Runtime.getProperties", get_prop_req, token);
						Assert.True (frame_props.IsErr);
					}
				);
			});
		}

		[Fact]
		public async Task TrivalStepping () {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready ();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);

				var bp = await SetBreakpoint ("dotnet://debugger-test.dll/debugger-test.cs", 5, 2);

				await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_add(); }, 1);",
					"dotnet://debugger-test.dll/debugger-test.cs", 5, 2,
					"IntAdd",
					wait_for_event_fn: (pause_location) => {
						//make sure we're on the right bp
						Assert.Equal (bp.Value ["breakpointId"]?.ToString (), pause_location ["hitBreakpoints"]?[0]?.Value<string> ());

						var top_frame = pause_location ["callFrames"][0];
						CheckLocation ("dotnet://debugger-test.dll/debugger-test.cs", 3, 41, scripts, top_frame["functionLocation"]);
						return Task.CompletedTask;
					}
				);

				await StepAndCheck (StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 6, 2, "IntAdd",
						wait_for_event_fn: (pause_location) => {
							var top_frame = pause_location ["callFrames"][0];
							CheckLocation ("dotnet://debugger-test.dll/debugger-test.cs", 3, 41, scripts, top_frame["functionLocation"]);
							return Task.CompletedTask;
						}
				);
			});
		}

		[Fact]
		public async Task InspectLocalsDuringStepping () {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);

				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";
				await SetBreakpoint (debugger_test_loc, 4, 2);

				await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_add(); }, 1);",
					debugger_test_loc, 4, 2, "IntAdd",
					locals_fn: (locals) => {
						CheckNumber (locals, "a", 10);
						CheckNumber (locals, "b", 20);
						CheckNumber (locals, "c", 0);
						CheckNumber (locals, "d", 0);
						CheckNumber (locals, "e", 0);
					}
				);

				await StepAndCheck (StepKind.Over, debugger_test_loc, 5, 2, "IntAdd",
					locals_fn: (locals) => {
						CheckNumber (locals, "a", 10);
						CheckNumber (locals, "b", 20);
						CheckNumber (locals, "c", 30);
						CheckNumber (locals, "d", 0);
						CheckNumber (locals, "e", 0);
					}
				);

				//step and get locals
				await StepAndCheck (StepKind.Over, debugger_test_loc, 6, 2, "IntAdd",
					locals_fn: (locals) => {
						CheckNumber (locals, "a", 10);
						CheckNumber (locals, "b", 20);
						CheckNumber (locals, "c", 30);
						CheckNumber (locals, "d", 50);
						CheckNumber (locals, "e", 0);
					}
				);
			});
		}

		[Theory]
		[InlineData (false)]
		[InlineData (true)]
		public async Task InspectLocalsInPreviousFramesDuringSteppingIn2 (bool use_cfo) {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;

				var dep_cs_loc = "dotnet://Simple.Dependency.dll/dependency.cs";
				await SetBreakpoint (dep_cs_loc, 24, 2);

				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";

				// Will stop in Complex.DoEvenMoreStuff
				var pause_location = await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_use_complex (); }, 1);",
					dep_cs_loc, 24, 2, "DoEvenMoreStuff",
					locals_fn: (locals) => {
						Assert.Single (locals);
						CheckObject (locals, "this", "Simple.Complex");
					}
				);

				var props = await GetObjectOnFrame (pause_location["callFrames"][0], "this");
				Assert.Equal (3, props.Count());
				CheckNumber (props, "A", 10);
				CheckString (props, "B", "xx");
				CheckObject (props, "c", "object");

				// Check UseComplex frame
				var locals_m1 = await GetLocalsForFrame (pause_location ["callFrames"][3], debugger_test_loc, 17, 2, "UseComplex");
				Assert.Equal (7, locals_m1.Count());

				CheckNumber (locals_m1, "a", 10);
				CheckNumber (locals_m1, "b", 20);
				CheckObject (locals_m1, "complex", "Simple.Complex");
				CheckNumber (locals_m1, "c", 30);
				CheckNumber (locals_m1, "d", 50);
				CheckNumber (locals_m1, "e", 60);
				CheckNumber (locals_m1, "f", 0);

				props = await GetObjectOnFrame (pause_location["callFrames"][3], "complex");
				Assert.Equal (3, props.Count());
				CheckNumber (props, "A", 10);
				CheckString (props, "B", "xx");
				CheckObject (props, "c", "object");

				pause_location = await StepAndCheck (StepKind.Over, dep_cs_loc, 16, 2, "DoStuff", times: 2);
				// Check UseComplex frame again
				locals_m1 = await GetLocalsForFrame (pause_location ["callFrames"][1], debugger_test_loc, 17, 2, "UseComplex");
				Assert.Equal (7, locals_m1.Count());

				CheckNumber (locals_m1, "a", 10);
				CheckNumber (locals_m1, "b", 20);
				CheckObject (locals_m1, "complex", "Simple.Complex");
				CheckNumber (locals_m1, "c", 30);
				CheckNumber (locals_m1, "d", 50);
				CheckNumber (locals_m1, "e", 60);
				CheckNumber (locals_m1, "f", 0);

				props = await GetObjectOnFrame (pause_location["callFrames"][1], "complex");
				Assert.Equal (3, props.Count());
				CheckNumber (props, "A", 10);
				CheckString (props, "B", "xx");
				CheckObject (props, "c", "object");
			});
		}

		[Theory]
		[InlineData (false)]
		[InlineData (true)]
		public async Task InspectLocalsInPreviousFramesDuringSteppingIn (bool use_cfo) {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;

				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";
				await SetBreakpoint (debugger_test_loc, 102, 3);

				// Will stop in InnerMethod
				var wait_res = await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_outer_method(); }, 1);",
					debugger_test_loc, 102, 3, "InnerMethod",
					locals_fn: (locals) => {
						Assert.Equal (4, locals.Count());
						CheckNumber (locals, "i", 5);
						CheckNumber (locals, "j", 24);
						CheckString (locals, "foo_str", "foo");
						CheckObject (locals, "this", "Math.NestedInMath");
					}
				);

				var this_props = await GetObjectOnFrame (wait_res["callFrames"][0], "this");
				Assert.Equal (2, this_props.Count());
				CheckObject (this_props, "m", "Math");
				CheckValueType (this_props, "SimpleStructProperty", "Math.SimpleStruct");

				var ss_props = await GetObjectOnLocals (this_props, "SimpleStructProperty");
				Assert.Equal (2, ss_props.Count());
				CheckValueType (ss_props, "dt", "System.DateTime");
				CheckValueType (ss_props, "gs", "Math.GenericStruct<System.DateTime>");

				await CheckDateTime (ss_props, "dt", new DateTime (2020, 1, 2, 3, 4, 5));

				// Check OuterMethod frame
				var locals_m1 = await GetLocalsForFrame (wait_res ["callFrames"][1], debugger_test_loc, 78, 2, "OuterMethod");
				Assert.Equal (5, locals_m1.Count());
				// FIXME: Failing test CheckNumber (locals_m1, "i", 5);
				// FIXME: Failing test CheckString (locals_m1, "text", "Hello");
				CheckNumber (locals_m1, "new_i", 0);
				CheckNumber (locals_m1, "k", 0);
				CheckObject (locals_m1, "nim", "Math.NestedInMath");

				// step back into OuterMethod
				await StepAndCheck (StepKind.Over, debugger_test_loc, 82, 2, "OuterMethod", times: 9,
					locals_fn: (locals) => {
						Assert.Equal (5, locals.Count());

						// FIXME: Failing test CheckNumber (locals_m1, "i", 5);
						CheckString (locals, "text", "Hello");
						// FIXME: Failing test CheckNumber (locals, "new_i", 24);
						CheckNumber (locals, "k", 19);
						CheckObject (locals, "nim", "Math.NestedInMath");
					}
				);

				//await StepAndCheck (StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 81, 2, "OuterMethod", times: 2);

				// step into InnerMethod2
				await StepAndCheck (StepKind.Into, "dotnet://debugger-test.dll/debugger-test.cs", 87, 1, "InnerMethod2",
					locals_fn: (locals) => {
						Assert.Equal (3, locals.Count());

						CheckString (locals, "s", "test string");
						//out var: CheckNumber (locals, "k", 0);
						CheckNumber (locals, "i", 24);
					}
				);

				await StepAndCheck (StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 91, 1, "InnerMethod2", times: 4,
					locals_fn: (locals) => {
						Assert.Equal (3, locals.Count());

						CheckString (locals, "s", "test string");
						// FIXME: Failing test CheckNumber (locals, "k", 34);
						CheckNumber (locals, "i", 24);
					}
				);

				await StepAndCheck (StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 83, 2, "OuterMethod", times: 2,
					locals_fn: (locals) => {
						Assert.Equal (5, locals.Count());

						CheckString (locals, "text", "Hello");
						// FIXME: failing test CheckNumber (locals, "i", 5);
						CheckNumber (locals, "new_i", 22);
						CheckNumber (locals, "k", 34);
						CheckObject (locals, "nim", "Math.NestedInMath");
					}
				);
			});
		}

		[Fact]
		public async Task InspectLocalsDuringSteppingIn () {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);

				await SetBreakpoint ("dotnet://debugger-test.dll/debugger-test.cs", 77, 2);

				await EvaluateAndCheck ("window.setTimeout(function() { invoke_outer_method(); }, 1);",
					"dotnet://debugger-test.dll/debugger-test.cs", 77, 2, "OuterMethod",
					locals_fn: (locals) => {
						Assert.Equal (5, locals.Count());

						CheckObject (locals, "nim", "Math.NestedInMath");
						CheckNumber (locals, "i", 5);
						CheckNumber (locals, "k", 0);
						CheckNumber (locals, "new_i", 0);
						CheckString (locals, "text", null);
					}
				);

				await StepAndCheck (StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 78, 2, "OuterMethod",
					locals_fn: (locals) => {
						Assert.Equal (5, locals.Count());

						CheckObject (locals, "nim", "Math.NestedInMath");
						// FIXME: Failing test CheckNumber (locals, "i", 5);
						CheckNumber (locals, "k", 0);
						CheckNumber (locals, "new_i", 0);
						CheckString (locals, "text", "Hello");
					}
				);

				// Step into InnerMethod
				await StepAndCheck (StepKind.Into, "dotnet://debugger-test.dll/debugger-test.cs", 96, 2, "InnerMethod");
				await StepAndCheck (StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 100, 3, "InnerMethod", times: 5,
					locals_fn: (locals) => {
						Assert.Equal (4, locals.Count());

						CheckNumber (locals, "i", 5);
						CheckNumber (locals, "j", 15);
						CheckString (locals, "foo_str", "foo");
						CheckObject (locals, "this", "Math.NestedInMath");
					}
				);

				// Step back to OuterMethod
				await StepAndCheck (StepKind.Over, "dotnet://debugger-test.dll/debugger-test.cs", 79, 2, "OuterMethod", times: 6,
					locals_fn: (locals) => {
						Assert.Equal (5, locals.Count());

						CheckObject (locals, "nim", "Math.NestedInMath");
						// FIXME: Failing test CheckNumber (locals, "i", 5);
						CheckNumber (locals, "k", 0);
						CheckNumber (locals, "new_i", 24);
						CheckString (locals, "text", "Hello");
					}
				);
			});
		}

		[Theory]
		[InlineData (false)]
		[InlineData (true)]
		public async Task InspectLocalsInAsyncMethods (bool use_cfo) {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-test.cs";

				await SetBreakpoint (debugger_test_loc, 111, 3);
				await SetBreakpoint (debugger_test_loc, 126, 3);

				// Will stop in Asyncmethod0
				var wait_res = await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_async_method_with_await(); }, 1);",
					debugger_test_loc, 111, 3, "MoveNext", //FIXME:
					locals_fn: (locals) => {
						Assert.Equal (4, locals.Count());
						CheckString (locals, "s", "string from js");
						CheckNumber (locals, "i", 42);
						CheckString (locals, "local0", "value0");
						CheckObject (locals, "this", "Math.NestedInMath");
					}
				);
				Console.WriteLine (wait_res);
				
#if false // Disabled for now, as we don't have proper async traces
				var locals = await GetProperties (wait_res ["callFrames"][2]["callFrameId"].Value<string> ());
				Assert.Equal (4, locals.Count());
				CheckString (locals, "ls", "string from jstest");
				CheckNumber (locals, "li", 52);
#endif

				// TODO: previous frames have async machinery details, so no point checking that right now

				var pause_loc = await SendCommandAndCheck (null, "Debugger.resume", debugger_test_loc, 126, 3, /*FIXME: "AsyncMethodNoReturn"*/ "MoveNext",
					locals_fn: (locals) => {
						Assert.Equal (4, locals.Count());
						CheckString (locals, "str", "AsyncMethodNoReturn's local");
						CheckObject (locals, "this", "Math.NestedInMath");
						//FIXME: check fields
						CheckValueType (locals, "ss", "Math.SimpleStruct");
						CheckArray (locals, "ss_arr", "Math.SimpleStruct[]");
						// TODO: struct fields
					}
				);

				var this_props = await GetObjectOnFrame (pause_loc ["callFrames"][0], "this");
				Assert.Equal (2, this_props.Count ());
				CheckObject (this_props, "m", "Math");
				CheckValueType (this_props, "SimpleStructProperty", "Math.SimpleStruct");

				// TODO: Check `this` properties
			});
		}

		[Theory]
		[InlineData (false)]
		[InlineData (true)]
		public async Task InspectLocalsWithStructs (bool use_cfo) {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

				await SetBreakpoint (debugger_test_loc, 16, 2);

				var pause_location = await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_method_with_structs(); }, 1);",
					debugger_test_loc, 16, 2, "MethodWithLocalStructs",
					locals_fn: (locals) => {
						Assert.Equal (3, locals.Count ());

						CheckValueType (locals, "ss_local", "DebuggerTests.ValueTypesTest.SimpleStruct");
						CheckValueType (locals, "gs_local", "DebuggerTests.ValueTypesTest.GenericStruct<DebuggerTests.ValueTypesTest>");
						CheckObject (locals, "vt_local", "DebuggerTests.ValueTypesTest");
					}
				);

				var dt = new DateTime (2021, 2, 3, 4, 6, 7);
				// Check ss_local's properties
				var ss_local_props = await GetObjectOnFrame (pause_location ["callFrames"][0], "ss_local");
				await CheckProps (ss_local_props, new {
							str_member = TString ("set in MethodWithLocalStructs#SimpleStruct#str_member"),
							dt = TValueType ("System.DateTime", dt.ToString ()),
							gs = TValueType ("DebuggerTests.ValueTypesTest.GenericStruct<System.DateTime>"),
							Kind = TEnum ("System.DateTimeKind", "Utc")
						}, "ss_local");

				{
					// Check ss_local.dt
					await CheckDateTime (ss_local_props, "dt", dt);

					// Check ss_local.gs
					var gs_props = await GetObjectOnLocals (ss_local_props, "gs");
					CheckString (gs_props, "StringField", "set in MethodWithLocalStructs#SimpleStruct#gs#StringField");
					CheckObject (gs_props, "List", "System.Collections.Generic.List<System.DateTime>");
				}

				// Check gs_local's properties
				var gs_local_props = await GetObjectOnFrame (pause_location ["callFrames"][0], "gs_local");
				await CheckProps (gs_local_props, new {
						StringField = TString ("gs_local#GenericStruct<ValueTypesTest>#StringField"),
						List        = TObject ("System.Collections.Generic.List<DebuggerTests.ValueTypesTest>", is_null: true),
						Options     = TEnum   ("DebuggerTests.Options", "None")
					}, "gs_local");

				// Check vt_local's properties
				var vt_local_props = await GetObjectOnFrame (pause_location ["callFrames"][0], "vt_local");
				Assert.Equal (5, vt_local_props.Count());

				CheckString (vt_local_props, "StringField", "string#0");
				CheckValueType (vt_local_props, "SimpleStructField", "DebuggerTests.ValueTypesTest.SimpleStruct");
				CheckValueType (vt_local_props, "SimpleStructProperty", "DebuggerTests.ValueTypesTest.SimpleStruct");
				await CheckDateTime (vt_local_props, "DT", new DateTime (2020, 1, 2, 3, 4, 5));
				CheckEnum (vt_local_props, "RGB", "DebuggerTests.RGB", "Blue");

				{
					// SimpleStructProperty
					dt = new DateTime (2022, 3, 4, 5, 7, 8);
					var ssp_props = await CompareObjectPropertiesFor (vt_local_props, "SimpleStructProperty",
						new {
							str_member = TString ("SimpleStructProperty#string#0#SimpleStruct#str_member"),
							dt = TValueType ("System.DateTime", dt.ToString ()),
							gs = TValueType ("DebuggerTests.ValueTypesTest.GenericStruct<System.DateTime>"),
							Kind = TEnum ("System.DateTimeKind", "Utc")
						},
						label: "vt_local_props.SimpleStructProperty");

					await CheckDateTime (ssp_props, "dt", dt);

					// SimpleStructField
					dt = new DateTime (2025, 6, 7, 8, 10, 11);
					var ssf_props = await CompareObjectPropertiesFor (vt_local_props, "SimpleStructField",
						new {
							str_member = TString ("SimpleStructField#string#0#SimpleStruct#str_member"),
							dt = TValueType ("System.DateTime", dt.ToString ()),
							gs = TValueType ("DebuggerTests.ValueTypesTest.GenericStruct<System.DateTime>"),
							Kind = TEnum ("System.DateTimeKind", "Local")
						},
						label: "vt_local_props.SimpleStructField");

					await CheckDateTime (ssf_props, "dt", dt);
				}

				// FIXME: check ss_local.gs.List's members
			});
		}

		[Theory]
		[InlineData (false)]
		[InlineData (true)]
		public async Task InspectValueTypeMethodArgs (bool use_cfo) {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

				await SetBreakpoint (debugger_test_loc, 27, 3);


				var pause_location = await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:TestStructsAsMethodArgs'); }, 1);",
					debugger_test_loc, 27, 3, "MethodWithStructArgs",
					locals_fn: (locals) => {
						Assert.Equal (3, locals.Count ());

						CheckString (locals, "label", "TestStructsAsMethodArgs#label");
						CheckValueType (locals, "ss_arg", "DebuggerTests.ValueTypesTest.SimpleStruct");
						CheckNumber (locals, "x", 3);
					}
				);

				var dt = new DateTime (2025, 6, 7, 8, 10, 11);
				var ss_local_as_ss_arg = new {
					str_member = TString    ("ss_local#SimpleStruct#string#0#SimpleStruct#str_member"),
					dt         = TValueType ("System.DateTime", dt.ToString ()),
					gs         = TValueType ("DebuggerTests.ValueTypesTest.GenericStruct<System.DateTime>"),
					Kind       = TEnum      ("System.DateTimeKind", "Local")
				};
				var ss_local_gs = new {
					StringField = TString ("ss_local#SimpleStruct#string#0#SimpleStruct#gs#StringField"),
					List        = TObject ("System.Collections.Generic.List<System.DateTime>"),
					Options     = TEnum   ("DebuggerTests.Options", "Option1")
				};

				// Check ss_arg's properties
				var ss_arg_props = await GetObjectOnFrame (pause_location ["callFrames"][0], "ss_arg");
				await CheckProps (ss_arg_props, ss_local_as_ss_arg, "ss_arg");

				{
					// Check ss_local.dt
					await CheckDateTime (ss_arg_props, "dt", dt);

					// Check ss_local.gs
					await CompareObjectPropertiesFor (ss_arg_props, "gs", ss_local_gs);
				}

				pause_location = await StepAndCheck (StepKind.Over, debugger_test_loc, 31, 3, "MethodWithStructArgs", times: 4,
					locals_fn: (locals) => {
						Assert.Equal (3, locals.Count());

						CheckString (locals, "label", "TestStructsAsMethodArgs#label");
						CheckValueType (locals, "ss_arg", "DebuggerTests.ValueTypesTest.SimpleStruct");
						CheckNumber (locals, "x", 3);

					}
				);

				var ss_arg_updated = new {
					str_member = TString    ("ValueTypesTest#MethodWithStructArgs#updated#ss_arg#str_member"),
					dt         = TValueType ("System.DateTime", dt.ToString ()),
					gs         = TValueType ("DebuggerTests.ValueTypesTest.GenericStruct<System.DateTime>"),
					Kind       = TEnum      ("System.DateTimeKind", "Utc")
				};

				ss_arg_props = await GetObjectOnFrame (pause_location ["callFrames"][0], "ss_arg");
				await CheckProps (ss_arg_props, ss_arg_updated, "ss_ar");

				{
					// Check ss_local.gs
					await CompareObjectPropertiesFor (ss_arg_props, "gs", new {
									StringField = TString ("ValueTypesTest#MethodWithStructArgs#updated#gs#StringField#3"),
									List        = TObject ("System.Collections.Generic.List<System.DateTime>"),
									Options     = TEnum   ("DebuggerTests.Options", "Option1")
							});

					await CheckDateTime (ss_arg_props, "dt", dt);
				}

				// Check locals on previous frame, same as earlier in this test
				ss_arg_props = await GetObjectOnFrame (pause_location ["callFrames"][1], "ss_local");
				await CheckProps (ss_arg_props, ss_local_as_ss_arg, "ss_local");

				{
					// Check ss_local.dt
					await CheckDateTime (ss_arg_props, "dt", dt);

					// Check ss_local.gs
					var gs_props = await GetObjectOnLocals (ss_arg_props, "gs");
					CheckString (gs_props, "StringField", "ss_local#SimpleStruct#string#0#SimpleStruct#gs#StringField");
					CheckObject (gs_props, "List", "System.Collections.Generic.List<System.DateTime>");
				}

				// ----------- Step back to the caller ---------

				pause_location = await StepAndCheck (StepKind.Over, debugger_test_loc, 22, 3, "TestStructsAsMethodArgs",
							times: 2, locals_fn: (l) => { /* non-null to make sure that locals get fetched */} );
				var locals = await GetProperties (pause_location ["callFrames"][0]["callFrameId"].Value<string> ());
				await CheckProps (locals, new {
					ss_local =  TValueType ("DebuggerTests.ValueTypesTest.SimpleStruct"),
					ss_ret   =  TValueType ("DebuggerTests.ValueTypesTest.SimpleStruct")
					},
					"locals#0");

				ss_arg_props = await GetObjectOnFrame (pause_location ["callFrames"] [0], "ss_local");
				await CheckProps (ss_arg_props, ss_local_as_ss_arg, "ss_local");

				{
					// Check ss_local.gs
					await CompareObjectPropertiesFor (ss_arg_props, "gs", ss_local_gs, label: "ss_local_gs");
				}

				// FIXME: check ss_local.gs.List's members
			});
		}

		[Fact]
		public async Task CheckUpdatedValueTypeFieldsOnResume ()
		{
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

				var lines = new [] {186, 189};
				await SetBreakpoint (debugger_test_loc, lines [0], 3);
				await SetBreakpoint (debugger_test_loc, lines [1], 3);

				var pause_location = await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:MethodUpdatingValueTypeMembers'); }, 1);",
					debugger_test_loc, lines [0], 3, "MethodUpdatingValueTypeMembers");

				var dt = new DateTime (1, 2, 3, 4, 5, 6);
				await CheckLocals (pause_location, dt);

				// Resume
				dt = new DateTime (9, 8, 7, 6, 5, 4);
				pause_location = await SendCommandAndCheck (JObject.FromObject (new{}), "Debugger.resume", debugger_test_loc, lines[1], 3, "MethodUpdatingValueTypeMembers");
				await CheckLocals (pause_location, dt);
			});

			async Task CheckLocals (JToken pause_location, DateTime dt)
			{
				var locals = await GetProperties (pause_location ["callFrames"][0]["callFrameId"].Value<string> ());
				await CheckProps (locals, new {
					obj = TObject ("DebuggerTests.ClassForToStringTests"),
					vt = TObject ("DebuggerTests.StructForToStringTests")
				}, "locals");

				var obj_props = await GetObjectOnLocals (locals, "obj");
				{
					await CheckProps (obj_props, new {
						DT = TValueType ("System.DateTime", dt.ToString ())
					}, "locals#obj.DT", num_fields: 5);

					await CheckDateTime (obj_props, "DT", dt);
				}

				var vt_props = await GetObjectOnLocals (locals, "obj");
				{
					await CheckProps (vt_props, new {
						DT = TValueType ("System.DateTime", dt.ToString ())
					}, "locals#obj.DT", num_fields: 5);

					await CheckDateTime (vt_props, "DT", dt);
				}
			}
		}

		[Fact]
		public async Task CheckUpdatedValueTypeLocalsOnResumeAsync ()
		{
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

				var lines = new [] { 195, 197 };
				await SetBreakpoint (debugger_test_loc, lines [0], 3);
				await SetBreakpoint (debugger_test_loc, lines [1], 3);

				var pause_location = await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:MethodUpdatingValueTypeLocalsAsync'); }, 1);",
					debugger_test_loc, lines [0], 3, "MoveNext");

				var dt = new DateTime (1, 2, 3, 4, 5, 6);
				var locals = await GetProperties (pause_location ["callFrames"][0]["callFrameId"].Value<string> ());
				await CheckDateTime (locals, "dt", dt);

				// Resume
				dt = new DateTime (9, 8, 7, 6, 5, 4);
				pause_location = await SendCommandAndCheck (JObject.FromObject (new{}), "Debugger.resume", debugger_test_loc, lines[1], 3, "MoveNext");
				locals = await GetProperties (pause_location ["callFrames"][0]["callFrameId"].Value<string> ());
				await CheckDateTime (locals, "dt", dt);
			});
		}

		[Fact]
		public async Task CheckUpdatedVTArrayMembersOnResume ()
		{
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

				var lines = new [] { 205, 207 };
				await SetBreakpoint (debugger_test_loc, lines [0], 3);
				await SetBreakpoint (debugger_test_loc, lines [1], 3);

				var dt = new DateTime (1, 2, 3, 4, 5, 6);
				var pause_location = await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ValueTypesTest:MethodUpdatingVTArrayMembers'); }, 1);",
					debugger_test_loc, lines [0], 3, "MethodUpdatingVTArrayMembers");
				await CheckArrayElements (pause_location, dt);

				// Resume
				dt = new DateTime (9, 8, 7, 6, 5, 4);
				pause_location = await SendCommandAndCheck (JObject.FromObject (new{}), "Debugger.resume", debugger_test_loc, lines[1], 3, "MethodUpdatingVTArrayMembers");
				await CheckArrayElements (pause_location, dt);
			});

			async Task CheckArrayElements (JToken pause_location, DateTime dt)
			{
				var locals = await GetProperties (pause_location ["callFrames"][0]["callFrameId"].Value<string> ());
				await CheckProps (locals, new {
					ssta = TArray ("DebuggerTests.StructForToStringTests[]", 1)
				}, "locals");

				var ssta = await GetObjectOnLocals (locals, "ssta");
				var sst0 = await GetObjectOnLocals (ssta, "0");
				await CheckProps (sst0, new {
					DT = TValueType ("System.DateTime", dt.ToString ())
				}, "dta [0]", num_fields: 5);

				await CheckDateTime (sst0, "DT", dt);
			}
		}
		[Theory]
		[InlineData (false)]
		[InlineData (true)]
		public async Task InspectLocalsWithStructsStaticAsync (bool use_cfo) {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				ctx.UseCallFunctionOnBeforeGetProperties = use_cfo;
				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

				await SetBreakpoint (debugger_test_loc, 47, 3);

				var pause_location = await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_static_method_async ("
						+ "'[debugger-test] DebuggerTests.ValueTypesTest:MethodWithLocalStructsStaticAsync'"
					+ "); }, 1);",
					debugger_test_loc, 47, 3, "MoveNext"); //BUG: method name

				var locals = await GetProperties (pause_location ["callFrames"][0]["callFrameId"].Value<string> ());
				await CheckProps (locals, new {
					ss_local = TObject ("DebuggerTests.ValueTypesTest.SimpleStruct"),
					gs_local = TValueType ("DebuggerTests.ValueTypesTest.GenericStruct<int>"),
					result   = TBool (true)
					},
					"locals#0");

				var dt = new DateTime (2021, 2, 3, 4, 6, 7);
				// Check ss_local's properties
				var ss_local_props = await GetObjectOnFrame (pause_location ["callFrames"][0], "ss_local");
				await CheckProps (ss_local_props, new {
						str_member = TString ("set in MethodWithLocalStructsStaticAsync#SimpleStruct#str_member"),
						dt         = TValueType ("System.DateTime", dt.ToString ()),
						gs         = TValueType ("DebuggerTests.ValueTypesTest.GenericStruct<System.DateTime>"),
						Kind       = TEnum ("System.DateTimeKind", "Utc")
					}, "ss_local");

				{
					// Check ss_local.dt
					await CheckDateTime (ss_local_props, "dt", dt);

					// Check ss_local.gs
					await CompareObjectPropertiesFor (ss_local_props, "gs",
						new {
							StringField = TString ("set in MethodWithLocalStructsStaticAsync#SimpleStruct#gs#StringField"),
							List        = TObject ("System.Collections.Generic.List<System.DateTime>"),
							Options     = TEnum   ("DebuggerTests.Options", "Option1")
						}
					);
				}

				// Check gs_local's properties
				var gs_local_props = await GetObjectOnFrame (pause_location ["callFrames"][0], "gs_local");
				await CheckProps (gs_local_props, new {
						StringField = TString ("gs_local#GenericStruct<ValueTypesTest>#StringField"),
						List        = TObject ("System.Collections.Generic.List<int>"),
						Options     = TEnum   ("DebuggerTests.Options", "Option2")
					}, "gs_local");

				// FIXME: check ss_local.gs.List's members
			});
		}

		[Theory]
		[InlineData (123, 3, "MethodWithLocalsForToStringTest", false, false)]
		[InlineData (133, 3, "MethodWithArgumentsForToStringTest", true, false)]
		[InlineData (175, 3, "MethodWithArgumentsForToStringTestAsync", true, true)]
		[InlineData (165, 3, "MethodWithArgumentsForToStringTestAsync", false, true)]
		public async Task InspectLocalsForToStringDescriptions (int line, int col, string method_name, bool call_other, bool invoke_async)
		{
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);
			string entry_method_name = $"[debugger-test] DebuggerTests.ValueTypesTest:MethodWithLocalsForToStringTest{(invoke_async ? "Async" : String.Empty)}";
			int frame_idx = 0;

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);
				var debugger_test_loc = "dotnet://debugger-test.dll/debugger-valuetypes-test.cs";

				await SetBreakpoint (debugger_test_loc, line, col);

				var eval_expr = "window.setTimeout(function() {"
									+ (invoke_async ? "invoke_static_method_async (" : "invoke_static_method (")
										+ $"'{entry_method_name}',"
										+ (call_other ? "true" : "false")
									+ "); }, 1);";
				Console.WriteLine ($"{eval_expr}");

				var pause_location = await EvaluateAndCheck (eval_expr, debugger_test_loc, line, col, invoke_async ? "MoveNext" : method_name);

				var dt0 = new DateTime (2020, 1, 2, 3, 4, 5);
				var dt1 = new DateTime (2010, 5, 4, 3, 2, 1);
				var ts = dt0 - dt1;
				var dto = new DateTimeOffset (dt0, new TimeSpan(4, 5, 0));

				var frame_locals = await GetProperties (pause_location ["callFrames"][frame_idx]["callFrameId"].Value<string> ());
				await CheckProps (frame_locals, new {
					call_other     = TBool      (call_other),
					dt0            = TValueType ("System.DateTime", dt0.ToString ()),
					dt1            = TValueType ("System.DateTime", dt1.ToString ()),
					dto            = TValueType ("System.DateTimeOffset", dto.ToString ()),
					ts             = TValueType ("System.TimeSpan", ts.ToString ()),
					dec            = TValueType ("System.Decimal", "123987123"),
					guid           = TValueType ("System.Guid", "3D36E07E-AC90-48C6-B7EC-A481E289D014"),
					dts            = TArray     ("System.DateTime[]", 2),
					obj            = TObject    ("DebuggerTests.ClassForToStringTests"),
					sst            = TObject    ("DebuggerTests.StructForToStringTests")
				}, "locals#0");

				var dts_0 = new DateTime (1983, 6, 7, 5, 6, 10);
				var dts_1 = new DateTime (1999, 10, 15, 1, 2, 3);
				var dts_elements = await GetObjectOnLocals (frame_locals, "dts");
				await CheckDateTime (dts_elements, "0", dts_0);
				await CheckDateTime (dts_elements, "1", dts_1);

				// TimeSpan
				await CompareObjectPropertiesFor (frame_locals, "ts",
						new {
							Days       = TNumber (3530),
							Minutes    = TNumber (2),
							Seconds    = TNumber (4),
						}, "ts_props", num_fields: 12);

				// DateTimeOffset
				await CompareObjectPropertiesFor (frame_locals, "dto",
						new {
							Day        = TNumber (2),
							Year       = TNumber (2020),
							DayOfWeek  = TEnum   ("System.DayOfWeek", "Thursday")
						}, "dto_props", num_fields: 22);

				var DT = new DateTime (2004, 10, 15, 1, 2, 3);
				var DTO = new DateTimeOffset (dt0, new TimeSpan(2, 14, 0));

				var obj_props = await CompareObjectPropertiesFor (frame_locals, "obj",
						new {
							DT         = TValueType ("System.DateTime", DT.ToString ()),
							DTO        = TValueType ("System.DateTimeOffset", DTO.ToString ()),
							TS         = TValueType ("System.TimeSpan", ts.ToString ()),
							Dec        = TValueType ("System.Decimal", "1239871"),
							Guid       = TValueType ("System.Guid", "3D36E07E-AC90-48C6-B7EC-A481E289D014")
						}, "obj_props");

				DTO = new DateTimeOffset (dt0, new TimeSpan (3, 15, 0));
				var sst_props = await CompareObjectPropertiesFor (frame_locals, "sst",
						new {
							DT         = TValueType ("System.DateTime", DT.ToString ()),
							DTO        = TValueType ("System.DateTimeOffset", DTO.ToString ()),
							TS         = TValueType ("System.TimeSpan", ts.ToString ()),
							Dec        = TValueType ("System.Decimal", "1239871"),
							Guid       = TValueType ("System.Guid", "3D36E07E-AC90-48C6-B7EC-A481E289D014")
						}, "sst_props");
			});
		}

		[Fact]
		public async Task InspectLocals () {
			var insp = new Inspector ();
			var scripts = SubscribeToScripts (insp);

			await Ready();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);

				var wait_res = await RunUntil ("locals_inner");
				var locals = await GetProperties (wait_res ["callFrames"][1]["callFrameId"].Value<string> ());
				});
		}

		[Theory]
		[InlineData (false)]
		[InlineData (true)]
		public async Task InspectLocalsForStructInstanceMethod (bool use_cfo)
			=> await CheckInspectLocalsAtBreakpointSite (
				"dotnet://debugger-test.dll/debugger-array-test.cs", 236, 3,
				"GenericInstanceMethod<DebuggerTests.SimpleClass>",
				"window.setTimeout(function() { invoke_static_method_async ('[debugger-test] DebuggerTests.EntryClass:run'); })",
				use_cfo: use_cfo,
				wait_for_event_fn: async (pause_location) => {
					var frame_locals = await GetProperties (pause_location ["callFrames"][0]["callFrameId"].Value<string>());

					await CheckProps (frame_locals, new {
						sc_arg = TObject ("DebuggerTests.SimpleClass"),
						@this = TValueType ("DebuggerTests.Point"),
						local_gs = TValueType ("DebuggerTests.SimpleGenericStruct<int>")
					},
					"locals#0");

					await CompareObjectPropertiesFor (frame_locals, "local_gs",
						new {
							Id = TString ("local_gs#Id"),
							Color = TEnum ("DebuggerTests.RGB", "Green"),
							Value = TNumber (4)
						},
						label: "local_gs#0");

					await CompareObjectPropertiesFor (frame_locals, "sc_arg",
							TSimpleClass (10, 45, "sc_arg#Id", "Blue"),
							label: "sc_arg#0");

					await CompareObjectPropertiesFor (frame_locals, "this",
							TPoint (90, -4, "point#Id", "Green"),
							label: "this#0");

				});
				
		[Fact]
		public async Task SteppingIntoMscorlib () {
			var insp = new Inspector ();
			//Collect events
			var scripts = SubscribeToScripts(insp);

			await Ready ();
			await insp.Ready (async (cli, token) => {
				ctx = new DebugTestContext (cli, insp, token, scripts);

				var bp = await SetBreakpoint ("dotnet://debugger-test.dll/debugger-test.cs", 74, 2);
				var pause_location = await EvaluateAndCheck (
					"window.setTimeout(function() { invoke_static_method ('[debugger-test] Math:OuterMethod'); }, 1);",
					"dotnet://debugger-test.dll/debugger-test.cs", 74, 2,
					"OuterMethod");

				//make sure we're on the right bp
				Assert.Equal (bp.Value ["breakpointId"]?.ToString (), pause_location ["hitBreakpoints"]?[0]?.Value<string> ());

				pause_location = await SendCommandAndCheck (null, $"Debugger.stepInto", null, -1, -1, null);
				var top_frame = pause_location ["callFrames"][0];

				AssertEqual ("WriteLine", top_frame ["functionName"]?.Value<string> (), "Expected to be in WriteLine method");
				var script_id = top_frame ["functionLocation"]["scriptId"].Value<string> ();
				AssertEqual ("dotnet://mscorlib.dll/Console.cs", scripts [script_id], "Expected to stopped in System.Console.WriteLine");
			});
		}

		//TODO add tests covering basic stepping behavior as step in/out/over
	}
}
