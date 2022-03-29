// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{
    public class ArrayTests : DebuggerTestBase
    {

        [Theory]
        [InlineData(19, 8, "PrimitiveTypeLocals", false, 0, false)]
        [InlineData(19, 8, "PrimitiveTypeLocals", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectPrimitiveTypeArrayLocals(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:PrimitiveTypeLocals",
            method_name: method_name,
            etype_name: "int",
            local_var_name_prefix: "int",
            array: new[] { TNumber(4), TNumber(70), TNumber(1) },
            array_elem_props: null,
            test_prev_frame: test_prev_frame,
            frame_idx: frame_idx,
            use_cfo: use_cfo);

        [Theory]
        [InlineData(36, 8, "ValueTypeLocals", false, 0, false)]
        [InlineData(36, 8, "ValueTypeLocals", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectValueTypeArrayLocals(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:ValueTypeLocals",
            method_name: method_name,
            etype_name: "DebuggerTests.Point",
            local_var_name_prefix: "point",
            array: new[]
            {
                TValueType("DebuggerTests.Point"),
                    TValueType("DebuggerTests.Point"),
            },
            array_elem_props: new[]
            {
                TPoint(5, -2, "point_arr#Id#0", "Green"),
                    TPoint(123, 0, "point_arr#Id#1", "Blue")
            },
            test_prev_frame: test_prev_frame,
            frame_idx: frame_idx,
            use_cfo: use_cfo);

        [Theory]
        [InlineData(54, 8, "ObjectTypeLocals", false, 0, false)]
        [InlineData(54, 8, "ObjectTypeLocals", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectObjectArrayLocals(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:ObjectTypeLocals",
            method_name: method_name,
            etype_name: "DebuggerTests.SimpleClass",
            local_var_name_prefix: "class",
            array: new[]
            {
                TObject("DebuggerTests.SimpleClass"),
                    TObject("DebuggerTests.SimpleClass", is_null : true),
                    TObject("DebuggerTests.SimpleClass")
            },
            array_elem_props: new[]
            {
                TSimpleClass(5, -2, "class_arr#Id#0", "Green"),
                    null, // Element is null
                    TSimpleClass(123, 0, "class_arr#Id#2", "Blue")
            },
            test_prev_frame: test_prev_frame,
            frame_idx: frame_idx,
            use_cfo: use_cfo);

        [Theory]
        [InlineData(72, 8, "GenericTypeLocals", false, 0, false)]
        [InlineData(72, 8, "GenericTypeLocals", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectGenericTypeArrayLocals(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:GenericTypeLocals",
            method_name: method_name,
            etype_name: "DebuggerTests.GenericClass<int>",
            local_var_name_prefix: "gclass",
            array: new[]
            {
                TObject("DebuggerTests.GenericClass<int>", is_null : true),
                    TObject("DebuggerTests.GenericClass<int>"),
                    TObject("DebuggerTests.GenericClass<int>")
            },
            array_elem_props: new[]
            {
                null, // Element is null
                new
                {
                    Id = TString("gclass_arr#1#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Red"),
                        Value = TNumber(5)
                },
                new
                {
                    Id = TString("gclass_arr#2#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Blue"),
                        Value = TNumber(-12)
                }
            },
            test_prev_frame: test_prev_frame,
            frame_idx: frame_idx,
            use_cfo: use_cfo);

        [Theory]
        [InlineData(89, 8, "GenericValueTypeLocals", false, 0, false)]
        [InlineData(89, 8, "GenericValueTypeLocals", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectGenericValueTypeArrayLocals(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:GenericValueTypeLocals",
            method_name: method_name,
            etype_name: "DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>",
            local_var_name_prefix: "gvclass",
            array: new[]
            {
                TValueType("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>"),
                    TValueType("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>")
            },
            array_elem_props: new[]
            {
                new
                {
                    Id = TString("gvclass_arr#1#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Red"),
                        Value = TPoint(100, 200, "gvclass_arr#1#Value#Id", "Red")
                },
                new
                {
                    Id = TString("gvclass_arr#2#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Blue"),
                        Value = TPoint(10, 20, "gvclass_arr#2#Value#Id", "Green")
                }
            },
            test_prev_frame: test_prev_frame,
            frame_idx: frame_idx,
            use_cfo: use_cfo);

        [Theory]
        [InlineData(213, 8, "GenericValueTypeLocals2", false, 0, false)]
        [InlineData(213, 8, "GenericValueTypeLocals2", false, 0, true)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, false)]
        [InlineData(100, 8, "YetAnotherMethod", true, 2, true)]
        public async Task InspectGenericValueTypeArrayLocals2(int line, int col, string method_name, bool test_prev_frame, int frame_idx, bool use_cfo) => await TestSimpleArrayLocals(
            line, col,
            entry_method_name: "[debugger-test] DebuggerTests.ArrayTestsClass:GenericValueTypeLocals2",
            method_name: method_name,
            etype_name: "DebuggerTests.SimpleGenericStruct<DebuggerTests.Point[]>",
            local_var_name_prefix: "gvclass",
            array: new[]
            {
                TValueType("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point[]>"),
                    TValueType("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point[]>")
            },
            array_elem_props: new[]
            {
                new
                {
                    Id = TString("gvclass_arr#0#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Red"),
                        Value = new []
                        {
                            TPoint(100, 200, "gvclass_arr#0#0#Value#Id", "Red"),
                                TPoint(100, 200, "gvclass_arr#0#1#Value#Id", "Green")
                        }
                },
                new
                {
                    Id = TString("gvclass_arr#1#Id"),
                        Color = TEnum("DebuggerTests.RGB", "Blue"),
                        Value = new []
                        {
                            TPoint(100, 200, "gvclass_arr#1#0#Value#Id", "Green"),
                                TPoint(100, 200, "gvclass_arr#1#1#Value#Id", "Blue")
                        }
                }
            },
            test_prev_frame: test_prev_frame,
            frame_idx: frame_idx,
            use_cfo: use_cfo);

        async Task<JToken> GetObjectWithCFO(string objectId, JObject fn_args = null)
        {
            var fn_decl = "function () { return this; }";
            var cfo_args = JObject.FromObject(new
            {
                functionDeclaration = fn_decl,
                objectId = objectId
            });

            if (fn_args != null)
                cfo_args["arguments"] = fn_args;

            // callFunctionOn
            var result = await cli.SendCommand("Runtime.callFunctionOn", cfo_args, token);

            return await GetProperties(result.Value["result"]["objectId"]?.Value<string>(), fn_args);
        }

        async Task TestSimpleArrayLocals(int line, int col, string entry_method_name, string method_name, string etype_name,
            string local_var_name_prefix, object[] array, object[] array_elem_props,
            bool test_prev_frame = false, int frame_idx = 0, bool use_cfo = false)
        {
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";
            UseCallFunctionOnBeforeGetProperties = use_cfo;

            await SetBreakpoint(debugger_test_loc, line, col);

            var eval_expr = "window.setTimeout(function() { invoke_static_method (" +
                $"'{entry_method_name}', { (test_prev_frame ? "true" : "false") }" +
                "); }, 1);";

            var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, method_name);

            var locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
            Assert.Equal(4, locals.Count());
            await CheckArray(locals, $"{local_var_name_prefix}_arr", $"{etype_name}[]", $"{etype_name}[{array?.Length ?? 0}]");
            await CheckArray(locals, $"{local_var_name_prefix}_arr_empty", $"{etype_name}[]", $"{etype_name}[0]");
            await CheckObject(locals, $"{local_var_name_prefix}_arr_null", $"{etype_name}[]", is_null: true);
            await CheckBool(locals, "call_other", test_prev_frame);

            var local_arr_name = $"{local_var_name_prefix}_arr";

            JToken prefix_arr;
            if (use_cfo)
            { // Use `Runtime.callFunctionOn` to get the properties
                var frame = pause_location["callFrames"][frame_idx];
                var name = local_arr_name;
                var fl = await GetProperties(frame["callFrameId"].Value<string>());
                var l_obj = GetAndAssertObjectWithName(locals, name);
                var l_objectId = l_obj["value"]["objectId"]?.Value<string>();

                Assert.True(!String.IsNullOrEmpty(l_objectId), $"No objectId found for {name}");

                prefix_arr = await GetObjectWithCFO(l_objectId);
            }
            else
            {
                prefix_arr = await GetObjectOnFrame(pause_location["callFrames"][frame_idx], local_arr_name);
            }

            await CheckProps(prefix_arr, array, local_arr_name);

            if (array_elem_props?.Length > 0)
            {
                for (int i = 0; i < array_elem_props.Length; i++)
                {
                    var i_str = i.ToString();
                    var label = $"{local_var_name_prefix}_arr[{i}]";
                    if (array_elem_props[i] == null)
                    {
                        var act_i = prefix_arr.FirstOrDefault(jt => jt["name"]?.Value<string>() == i_str);
                        Assert.True(act_i != null, $"[{label}] Couldn't find array element [{i_str}]");

                        await CheckValue(act_i["value"], TObject(etype_name, is_null: true), label);
                    }
                    else
                    {
                        await CompareObjectPropertiesFor(prefix_arr, i_str, array_elem_props[i], label: label);
                    }
                }
            }

            var props = await GetObjectOnFrame(pause_location["callFrames"][frame_idx], $"{local_var_name_prefix}_arr_empty");
            await CheckProps(props, new object[0], "${local_var_name_prefix}_arr_empty");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectObjectArrayMembers(bool use_cfo)
        {
            int line = 227;
            int col = 12;
            string entry_method_name = "[debugger-test] DebuggerTests.ArrayTestsClass:ObjectArrayMembers";
            string method_name = "PlaceholderMethod";
            int frame_idx = 1;

            UseCallFunctionOnBeforeGetProperties = use_cfo;
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";

            await SetBreakpoint(debugger_test_loc, line, col);

            var eval_expr = "window.setTimeout(function() { invoke_static_method (" +
                $"'{entry_method_name}'" +
                "); }, 1);";

            var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, method_name);
            var locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
            Assert.Single(locals);
            await CheckObject(locals, "c", "DebuggerTests.Container");

            var c_props = await GetObjectOnFrame(pause_location["callFrames"][frame_idx], "c");
            await CheckProps(c_props, new
            {
                id = TString("c#id"),
                ClassArrayProperty = TArray("DebuggerTests.SimpleClass[]", "DebuggerTests.SimpleClass[3]"),
                ClassArrayField = TArray("DebuggerTests.SimpleClass[]", "DebuggerTests.SimpleClass[3]"),
                PointsProperty = TArray("DebuggerTests.Point[]", "DebuggerTests.Point[2]"),
                PointsField = TArray("DebuggerTests.Point[]", "DebuggerTests.Point[2]")
            },
                "c"
            );

            await CompareObjectPropertiesFor(c_props, "ClassArrayProperty",
                new[]
                {
                        TSimpleClass(5, -2, "ClassArrayProperty#Id#0", "Green"),
                            TSimpleClass(30, 1293, "ClassArrayProperty#Id#1", "Green"),
                            TObject("DebuggerTests.SimpleClass", is_null : true)
                },
                label: "InspectLocalsWithStructsStaticAsync");

            await CompareObjectPropertiesFor(c_props, "ClassArrayField",
                new[]
                {
                        TObject("DebuggerTests.SimpleClass", is_null : true),
                            TSimpleClass(5, -2, "ClassArrayField#Id#1", "Blue"),
                            TSimpleClass(30, 1293, "ClassArrayField#Id#2", "Green")
                },
                label: "c#ClassArrayField");

            await CompareObjectPropertiesFor(c_props, "PointsProperty",
                new[]
                {
                        TPoint(5, -2, "PointsProperty#Id#0", "Green"),
                            TPoint(123, 0, "PointsProperty#Id#1", "Blue"),
                },
                label: "c#PointsProperty");

            await CompareObjectPropertiesFor(c_props, "PointsField",
                new[]
                {
                        TPoint(5, -2, "PointsField#Id#0", "Green"),
                            TPoint(123, 0, "PointsField#Id#1", "Blue"),
                },
                label: "c#PointsField");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectValueTypeArrayLocalsStaticAsync(bool use_cfo)
        {
            int line = 157;
            int col = 12;
            string entry_method_name = "[debugger-test] DebuggerTests.ArrayTestsClass:ValueTypeLocalsAsync";
            string method_name = "MoveNext"; // BUG: this should be ValueTypeLocalsAsync
            int frame_idx = 0;

            UseCallFunctionOnBeforeGetProperties = use_cfo;
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";

            await SetBreakpoint(debugger_test_loc, line, col);

            var eval_expr = "window.setTimeout(function() { invoke_static_method_async (" +
                $"'{entry_method_name}', false" // *false* here keeps us only in the static method
                +
                "); }, 1);";

            var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, method_name);
            var frame_locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
            await CheckProps(frame_locals, new
            {
                call_other = TBool(false),
                gvclass_arr = TArray("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>[]", "DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>[2]"),
                gvclass_arr_empty = TArray("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>[]", "DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>[0]"),
                gvclass_arr_null = TObject("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>[]", is_null: true),
                gvclass = TValueType("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>"),
                // BUG: this shouldn't be null!
                points = TObject("DebuggerTests.Point[]", is_null: true)
            }, "ValueTypeLocalsAsync#locals");

            var local_var_name_prefix = "gvclass";
            await CompareObjectPropertiesFor(frame_locals, local_var_name_prefix, new
            {
                Id = TString(null),
                Color = TEnum("DebuggerTests.RGB", "Red"),
                Value = TPoint(0, 0, null, "Red")
            });

            await CompareObjectPropertiesFor(frame_locals, $"{local_var_name_prefix}_arr",
                new[]
                {
                        new
                        {
                            Id = TString("gvclass_arr#1#Id"),
                                Color = TEnum("DebuggerTests.RGB", "Red"),
                                Value = TPoint(100, 200, "gvclass_arr#1#Value#Id", "Red")
                        },
                        new
                        {
                            Id = TString("gvclass_arr#2#Id"),
                                Color = TEnum("DebuggerTests.RGB", "Blue"),
                                Value = TPoint(10, 20, "gvclass_arr#2#Value#Id", "Green")
                        }
                }
            );
            await CompareObjectPropertiesFor(frame_locals, $"{local_var_name_prefix}_arr_empty",
                new object[0]);
        }

        // TODO: Check previous frame too
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectValueTypeArrayLocalsInstanceAsync(bool use_cfo)
        {
            //Collect events
            int line = 170;
            int col = 12;
            string entry_method_name = "[debugger-test] DebuggerTests.ArrayTestsClass:ValueTypeLocalsAsync";
            int frame_idx = 0;

            UseCallFunctionOnBeforeGetProperties = use_cfo;
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";

            await SetBreakpoint(debugger_test_loc, line, col);

            var eval_expr = "window.setTimeout(function() { invoke_static_method_async (" +
                $"'{entry_method_name}', true" +
                "); }, 1);";

            // BUG: Should be InspectValueTypeArrayLocalsInstanceAsync
            var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, "MoveNext");

            var frame_locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
            await CheckProps(frame_locals, new
            {
                t1 = TObject("DebuggerTests.SimpleGenericStruct<DebuggerTests.Point>"),
                @this = TObject("DebuggerTests.ArrayTestsClass"),
                point_arr = TArray("DebuggerTests.Point[]", "DebuggerTests.Point[2]"),
                point = TValueType("DebuggerTests.Point")
            }, "InspectValueTypeArrayLocalsInstanceAsync#locals");

            await CompareObjectPropertiesFor(frame_locals, "t1",
                new
                {
                    Id = TString("gvclass_arr#1#Id"),
                    Color = TEnum("DebuggerTests.RGB", "Red"),
                    Value = TPoint(100, 200, "gvclass_arr#1#Value#Id", "Red")
                });

            await CompareObjectPropertiesFor(frame_locals, "point_arr",
                new[]
                {
                        TPoint(5, -2, "point_arr#Id#0", "Red"),
                            TPoint(123, 0, "point_arr#Id#1", "Blue"),
                }
            );

            await CompareObjectPropertiesFor(frame_locals, "point",
                TPoint(45, 51, "point#Id", "Green"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectValueTypeArrayLocalsInAsyncStaticStructMethod(bool use_cfo)
        {
            int line = 244;
            int col = 12;
            string entry_method_name = "[debugger-test] DebuggerTests.ArrayTestsClass:EntryPointForStructMethod";
            int frame_idx = 0;

            UseCallFunctionOnBeforeGetProperties = use_cfo;
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";

            await SetBreakpoint(debugger_test_loc, line, col);
            //await SetBreakpoint (debugger_test_loc, 143, 3);

            var eval_expr = "window.setTimeout(function() { invoke_static_method_async (" +
                $"'{entry_method_name}', false" +
                "); }, 1);";

            // BUG: Should be InspectValueTypeArrayLocalsInstanceAsync
            var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, "MoveNext");

            var frame_locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
            await CheckProps(frame_locals, new
            {
                call_other = TBool(false),
                local_i = TNumber(5),
                sc = TSimpleClass(10, 45, "sc#Id", "Blue")
            }, "InspectValueTypeArrayLocalsInAsyncStaticStructMethod#locals");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectValueTypeArrayLocalsInAsyncInstanceStructMethod(bool use_cfo)
        {
            int line = 251;
            int col = 12;
            string entry_method_name = "[debugger-test] DebuggerTests.ArrayTestsClass:EntryPointForStructMethod";
            int frame_idx = 0;

            UseCallFunctionOnBeforeGetProperties = use_cfo;
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";

            await SetBreakpoint(debugger_test_loc, line, col);

            var eval_expr = "window.setTimeout(function() { invoke_static_method_async (" +
                $"'{entry_method_name}', true" +
                "); }, 1);";

            // BUG: Should be InspectValueTypeArrayLocalsInstanceAsync
            var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, line, col, "MoveNext");

            var frame_locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
            await CheckProps(frame_locals, new
            {
                sc_arg = TObject("DebuggerTests.SimpleClass"),
                @this = TValueType("DebuggerTests.Point"),
                local_gs = TValueType("DebuggerTests.SimpleGenericStruct<int>")
            },
                "locals#0");

            await CompareObjectPropertiesFor(frame_locals, "local_gs",
                new
                {
                    Id = TString("local_gs#Id"),
                    Color = TEnum("DebuggerTests.RGB", "Green"),
                    Value = TNumber(4)
                },
                label: "local_gs#0");

            await CompareObjectPropertiesFor(frame_locals, "sc_arg",
                TSimpleClass(10, 45, "sc_arg#Id", "Blue"),
                label: "sc_arg#0");

            await CompareObjectPropertiesFor(frame_locals, "this",
                TPoint(90, -4, "point#Id", "Green"),
                label: "this#0");
        }

#if false // https://github.com/dotnet/runtime/issues/63560
        [Fact]
        public async Task InvalidArrayId() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.Container", "PlaceholderMethod", 1, "PlaceholderMethod",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ArrayTestsClass:ObjectArrayMembers'); }, 1);",
            wait_for_event_fn: async (pause_location) =>
           {

               int frame_idx = 1;
               var frame_locals = await GetProperties(pause_location["callFrames"][frame_idx]["callFrameId"].Value<string>());
               var c_obj = GetAndAssertObjectWithName(frame_locals, "c");
               var c_obj_id = c_obj["value"]?["objectId"]?.Value<string>();
               Assert.NotNull(c_obj_id);

               // Invalid format
               await GetProperties("dotnet:array:4123", expect_ok: false);

               // Invalid object id
               await GetProperties("dotnet:array:{ \"arrayId\": 234980 }", expect_ok: false);

               // Trying to access object as an array
               if (!DotnetObjectId.TryParse(c_obj_id, out var id) || id.Scheme != "object")
                   Assert.True(false, "Unexpected object id format. Maybe this test is out of sync with the object id format in dotnet.cjs.lib.js?");

               if (!int.TryParse(id.Value, out var idNum))
                   Assert.True(false, "Expected a numeric value part of the object id: {c_obj_id}");
               await GetProperties($"dotnet:array:{{\"arrayId\":{idNum}}}", expect_ok: false);
           });

        [Fact]
        public async Task InvalidValueTypeArrayIndex() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.Container", "PlaceholderMethod", 1, "PlaceholderMethod",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ArrayTestsClass:ObjectArrayMembers'); }, 1);",
            locals_fn: async (locals) =>
           {
               var this_obj = GetAndAssertObjectWithName(locals, "this");
               var c_obj = GetAndAssertObjectWithName(await GetProperties(this_obj["value"]["objectId"].Value<string>()), "c");
               var c_obj_id = c_obj["value"]?["objectId"]?.Value<string>();
               Assert.NotNull(c_obj_id);

               var c_props = await GetProperties(c_obj_id);

               var pf_arr = GetAndAssertObjectWithName(c_props, "PointsField");
               var pf_arr_elems = await GetProperties(pf_arr["value"]["objectId"].Value<string>());

               if (!DotnetObjectId.TryParse(pf_arr_elems[0]["value"]?["objectId"]?.Value<string>(), out var id))
                   Assert.True(false, "Couldn't parse objectId for PointsFields' elements");

               AssertEqual("valuetype", id.Scheme, "Expected a valuetype id");
               var id_args = id.ValueAsJson;
               Assert.True(id_args["arrayId"] != null, "ObjectId format for array seems to have changed. Expected to find 'arrayId' in the value. Update this test");
               Assert.True(id_args != null, "Expected to get a json as the value part of {id}");

               // Try one valid query, to confirm that the id format hasn't changed!
               id_args["arrayIdx"] = 0;
               await GetProperties($"dotnet:valuetype:{id_args.ToString(Newtonsoft.Json.Formatting.None)}", expect_ok: true);

               id_args["arrayIdx"] = 12399;
               await GetProperties($"dotnet:valuetype:{id_args.ToString(Newtonsoft.Json.Formatting.None)}", expect_ok: false);

               id_args["arrayIdx"] = -1;
               await GetProperties($"dotnet:valuetype:{id_args.ToString(Newtonsoft.Json.Formatting.None)}", expect_ok: false);

               id_args["arrayIdx"] = "qwe";
               await GetProperties($"dotnet:valuetype:{id_args.ToString(Newtonsoft.Json.Formatting.None)}", expect_ok: false);
           });
#endif

        [Fact]
        public async Task InvalidAccessors() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.Container", "PlaceholderMethod", 1, "PlaceholderMethod",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.ArrayTestsClass:ObjectArrayMembers'); }, 1);",
            locals_fn: async (locals) =>
            {
                var this_obj = GetAndAssertObjectWithName(locals, "this");
                var this_obj_id = this_obj["value"]?["objectId"]?.Value<string>();
                Assert.NotNull(this_obj_id);

                var this_props = await GetProperties(this_obj_id);

                var pf_arr = GetAndAssertObjectWithName(this_props, "PointsField");

                // Validate the way we test the accessors, with a valid one
                var res = await InvokeGetter(pf_arr, "0", expect_ok: true);
                await CheckValue(res.Value["result"], TValueType("DebuggerTests.Point"), "pf_arr[0]");
                var pf_arr0_props = await GetProperties(res.Value["result"]["objectId"]?.Value<string>());
                await CheckProps(pf_arr0_props, new
                 {
                     X = TNumber(5)
                 }, "pf_arr0_props", num_fields: 4);

                var invalid_accessors = new object[] { "NonExistant", "10000", "-2", 10000, -2, null, String.Empty };
                foreach (var invalid_accessor in invalid_accessors)
                {
                    // var res = await InvokeGetter (JObject.FromObject (new { value = new { objectId = obj_id } }), invalid_accessor, expect_ok: true);
                    res = await InvokeGetter(pf_arr, invalid_accessor, expect_ok: true);
                    AssertEqual("undefined", res.Value["result"]?["type"]?.ToString(), "Expected to get undefined result for non-existant accessor");
                }
           });

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task InspectPrimitiveTypeMultiArrayLocals(bool use_cfo)
        {
            var debugger_test_loc = "dotnet://debugger-test.dll/debugger-array-test.cs";

            var eval_expr = "window.setTimeout(function() { invoke_static_method (" +
                $"'[debugger-test] DebuggerTests.MultiDimensionalArray:run'" +
                "); }, 1);";

            var pause_location = await EvaluateAndCheck(eval_expr, debugger_test_loc, 343, 12, "run");

            var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
            Assert.Equal(3, locals.Count());
            var int_arr_1 = !use_cfo ?
                            await GetProperties(locals[0]["value"]["objectId"].Value<string>()) : 
                            await GetObjectWithCFO((locals[0]["value"]["objectId"].Value<string>()));

            CheckNumber(int_arr_1, "0", 0);
            CheckNumber(int_arr_1, "1", 1);
            var int_arr_2 = !use_cfo ?
                await GetProperties(locals[1]["value"]["objectId"].Value<string>()) : 
                await GetObjectWithCFO((locals[1]["value"]["objectId"].Value<string>()));
            CheckNumber(int_arr_2, "0, 0", 0);
            CheckNumber(int_arr_2, "0, 1", 1);
            CheckNumber(int_arr_2, "0, 2", 2);
            CheckNumber(int_arr_2, "1, 0", 10);
            CheckNumber(int_arr_2, "1, 1", 11);
            CheckNumber(int_arr_2, "1, 2", 12);

            var int_arr_3 = !use_cfo ?
                await GetProperties(locals[2]["value"]["objectId"].Value<string>()) : 
                await GetObjectWithCFO((locals[2]["value"]["objectId"].Value<string>()));
            CheckNumber(int_arr_3, "0, 0, 0", 0);
            CheckNumber(int_arr_3, "0, 0, 1", 1);
            CheckNumber(int_arr_3, "0, 0, 2", 2);
            CheckNumber(int_arr_3, "0, 1, 0", 10);
            CheckNumber(int_arr_3, "0, 1, 1", 11);
            CheckNumber(int_arr_3, "0, 1, 2", 12);
            CheckNumber(int_arr_3, "0, 2, 0", 20);
            CheckNumber(int_arr_3, "0, 2, 1", 21);
            CheckNumber(int_arr_3, "0, 2, 2", 22);
            CheckNumber(int_arr_3, "1, 0, 0", 100);
            CheckNumber(int_arr_3, "1, 0, 1", 101);
            CheckNumber(int_arr_3, "1, 0, 2", 102);
            CheckNumber(int_arr_3, "1, 1, 0", 110);
            CheckNumber(int_arr_3, "1, 1, 1", 111);
            CheckNumber(int_arr_3, "1, 1, 2", 112);
            CheckNumber(int_arr_3, "1, 2, 0", 120);
            CheckNumber(int_arr_3, "1, 2, 1", 121);
            CheckNumber(int_arr_3, "1, 2, 2", 122);
        }
    }
}
