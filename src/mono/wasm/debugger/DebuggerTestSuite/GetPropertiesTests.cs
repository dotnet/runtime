// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{
    public class GetPropertiesTests : DebuggerTestBase
    {
        public static TheoryData<string, bool?, bool?, string[], Dictionary<string, (JObject, bool)>, bool> ClassGetPropertiesTestData(bool is_async)
        {
            var data = new TheoryData<string, bool?, bool?, string[], Dictionary<string, (JObject, bool)>, bool>();

            var type_name = "DerivedClass";
            var all_props = new Dictionary<string, (JObject, bool)>()
            {
                {"_stringField",                    (TString("DerivedClass#_stringField"), true)},
                {"_dateTime",                       (TDateTime(new DateTime(2020, 7, 6, 5, 4, 3)), true)},
                {"_DTProp",                         (TGetter("_DTProp"), true)},

                // own public
                {"a",                               (TNumber(4), true)},
                {"DateTime",                        (TGetter("DateTime"), true)},
                {"AutoStringProperty",              (TString("DerivedClass#AutoStringProperty"), true)},
                {"FirstName",                       (TGetter("FirstName"), true)},
                {"DateTimeForOverride",             (TGetter("DateTimeForOverride"), true)},

                {"StringPropertyForOverrideWithAutoProperty",   (TString("DerivedClass#StringPropertyForOverrideWithAutoProperty"), true)},
                {"Base_AutoStringPropertyForOverrideWithField", (TString("DerivedClass#Base_AutoStringPropertyForOverrideWithField"), true)},
                {"Base_GetterForOverrideWithField",             (TString("DerivedClass#Base_GetterForOverrideWithField"), true)},
                {"BaseBase_MemberForOverride",                  (TString("DerivedClass#BaseBase_MemberForOverride"), true)},

                // indexers don't show up in getprops
                // {"Item",                    (TSymbol("int { get; }"), true)},

                // inherited private
                {"_base_name",                      (TString("private_name"), false)},
                {"_base_dateTime",                  (TGetter("_base_dateTime"), false)},

                // inherited public
                {"Base_AutoStringProperty",         (TString("base#Base_AutoStringProperty"), false)},
                {"base_num",                        (TNumber(5), false)},
                {"LastName",                        (TGetter("LastName"), false)}
            };

            // default, all properties
            // n, n
            data.Add(type_name, null, null, all_props.Keys.ToArray(), all_props, is_async);
            // f, f
            data.Add(type_name, false, false, all_props.Keys.ToArray(), all_props, is_async);
            // f, n
            data.Add(type_name, false, null, all_props.Keys.ToArray(), all_props, is_async);
            // n, f
            data.Add(type_name, null, false, all_props.Keys.ToArray(), all_props, is_async);

            // all own
            // t, f
            // t, n
            foreach (bool? accessors in new bool?[] { false, null })
            {
                // Breaking from JS behavior, we return *all* members irrespective of `ownMembers`
                data.Add(type_name, true, accessors, all_props.Keys.ToArray(), all_props, is_async);
                // data.Add(type_name, true, accessors, new[]
                // {
                //     "_stringField",
                //     "_dateTime",
                //     "_DTProp",
                //     "a",
                //     "DateTime",
                //     "AutoStringProperty",
                //     "FirstName",
                //     "DateTimeForOverride",
                //     "StringPropertyForOverrideWithAutoProperty"
                // }, all_props, is_async);
            }

            var all_accessors = new[]
            {
                "_DTProp",
                "DateTime",
                "_base_dateTime",
                "FirstName",
                "LastName",
                "DateTimeForOverride"
            };

            var only_own_accessors = new[]
            {
                "_DTProp",
                "DateTime",
                "FirstName",
                "DateTimeForOverride"
            };

            // all own, only accessors
            // t, t

            // Breaking from JS behavior, we return *all* members irrespective of `ownMembers`
            // data.Add(type_name, true, true, only_own_accessors, all_props, is_async);
            data.Add(type_name, true, true, all_accessors, all_props, is_async);

            // all accessors
            // f, t
            // n, t
            foreach (bool? own in new bool?[] { false, null })
            {
                data.Add(type_name, own, true, all_accessors, all_props, is_async);
            }

            return data;
        }

        public static TheoryData<string, bool?, bool?, string[], Dictionary<string, (JObject, bool)>, bool> StructGetPropertiesTestData(bool is_async)
        {
            var data = new TheoryData<string, bool?, bool?, string[], Dictionary<string, (JObject, bool)>, bool>();

            var type_name = "CloneableStruct";
            var all_props = new Dictionary<string, (JObject, bool)>()
            {
                {"_stringField",            (TString("CloneableStruct#_stringField"), true)},
                {"_dateTime",               (TDateTime(new DateTime(2020, 7, 6, 5, 4, 3 + 3)), true)},
                {"_DTProp",                 (TGetter("_DTProp"), true)},

                // own public
                {"a",                       (TNumber(4), true)},
                {"DateTime",                (TGetter("DateTime"), true)},
                {"AutoStringProperty",      (TString("CloneableStruct#AutoStringProperty"), true)},
                {"FirstName",               (TGetter("FirstName"), true)},
                {"LastName",                (TGetter("LastName"), true)},

                // indexers don't show up in getprops
                // {"Item",                    (TSymbol("int { get; }"), true)}
            };

            // default, all properties
            data.Add(type_name, null, null, all_props.Keys.ToArray(), all_props, is_async);
            data.Add(type_name, false, false, all_props.Keys.ToArray(), all_props, is_async);

            // all own
            data.Add(type_name, true, false, all_props.Keys.ToArray(), all_props, is_async);

            var all_accessor_names = new[]
            {
                "_DTProp",
                "DateTime",
                "FirstName",
                "LastName"
            };

            // all own, only accessors
            data.Add(type_name, true, true, all_accessor_names, all_props, is_async);
            // all accessors
            data.Add(type_name, false, true, all_accessor_names, all_props, is_async);

            return data;
        }

        [Theory]
        [MemberData(nameof(ClassGetPropertiesTestData), parameters: true)]
        [MemberData(nameof(ClassGetPropertiesTestData), parameters: false)]
        [MemberData(nameof(StructGetPropertiesTestData), parameters: true)]
        [MemberData(nameof(StructGetPropertiesTestData), parameters: false)]
        public async Task InspectTypeInheritedMembers(string type_name, bool? own_properties, bool? accessors_only, string[] expected_names, Dictionary<string, (JObject, bool)> all_props, bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.GetPropertiesTests.{type_name}",
            $"InstanceMethod{(is_async ? "Async" : "")}", 1, (is_async ? "MoveNext" : "InstanceMethod"),
            $"window.setTimeout(function() {{ invoke_static_method_async ('[debugger-test] DebuggerTests.GetPropertiesTests.{type_name}:run'); }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var frame_id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var frame_locals = await GetProperties(frame_id);
                var this_obj = GetAndAssertObjectWithName(frame_locals, "this");
                var this_props = await GetProperties(this_obj["value"]?["objectId"]?.Value<string>(), own_properties: own_properties, accessors_only: accessors_only);

                await CheckExpectedProperties(expected_names, name => GetAndAssertObjectWithName(this_props, name), all_props);

                // indexer properties shouldn't show up here
                var item = this_props.FirstOrDefault(jt => jt["name"]?.Value<string>() == "Item");
                Assert.Null(item);

                // Useful for debugging: AssertHasOnlyExpectedProperties(expected_names, this_props.Values<JObject>());
                AssertEqual(expected_names.Length, this_props.Count(), $"expected number of properties");
            });

        public static IEnumerable<object[]> MembersForLocalNestedStructData(bool is_async)
            => StructGetPropertiesTestData(false).Select(datum => datum[1..]);

        [Theory]
        [MemberData(nameof(MembersForLocalNestedStructData), parameters: false)]
        [MemberData(nameof(MembersForLocalNestedStructData), parameters: true)]
        public async Task MembersForLocalNestedStruct(bool? own_properties, bool? accessors_only, string[] expected_names, Dictionary<string, (JObject, bool)> all_props, bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.GetPropertiesTests.NestedStruct",
            is_async ? $"TestNestedStructStaticAsync" : "TestNestedStructStatic",
            2,
            is_async ? "MoveNext" : $"TestNestedStructStatic",
            $"window.setTimeout(function() {{ invoke_static_method_async ('[debugger-test] DebuggerTests.GetPropertiesTests.NestedStruct:run'); }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var ns_props = await GetObjectOnFrame(pause_location["callFrames"][0], "ns");

                var cs_obj = GetAndAssertObjectWithName(ns_props, "cloneableStruct");
                var cs_props = await GetProperties(cs_obj["value"]?["objectId"]?.Value<string>(), own_properties: own_properties, accessors_only: accessors_only);

                await CheckExpectedProperties(expected_names, name => GetAndAssertObjectWithName(cs_props, name), all_props);
                AssertHasOnlyExpectedProperties(expected_names, cs_props.Values<JObject>());

                // indexer properties shouldn't show up here
                var item = cs_props.FirstOrDefault(jt => jt["name"]?.Value<string>() == "Item");
                Assert.Null(item);

                // Useful for debugging: AssertHasOnlyExpectedProperties(expected_names, this_props.Values<JObject>());
                AssertEqual(expected_names.Length, cs_props.Count(), $"expected number of properties");
            });

        public static TheoryData<bool, bool?, bool?, string[]> JSGetPropertiesTestData(bool test_js) => new TheoryData<bool, bool?, bool?, string[]>
        {
            // default, no args set
            {
                test_js,
                null, null, new[]
                {
                    "owner_name",
                    "owner_last_name",
                    "kind",
                    "make",
                    "available"
                }
            },

            // all props
            {
                test_js,
                false, false, new[]
                {
                    "owner_name",
                    "owner_last_name",
                    "kind",
                    "make",
                    "available"
                }
            },

            // all own
            {
                test_js,
                true, false, new[]
                {
                    "owner_name",
                    "owner_last_name"
                }
            },

            // all own accessors
            {
                test_js,
                true, true, new[]
                {
                    "owner_last_name"
                }
            },

            // all accessors
            {
                test_js,
                false, true, new[]
                {
                    "available",
                    "owner_last_name"
                }
            }
        };

        [Theory]
        [MemberData(nameof(JSGetPropertiesTestData), parameters: true)]
        // Note: Disabled because we don't match JS's behavior here!
        //       We return inherited members too for `ownProperties:true`
        // [MemberData(nameof(JSGetPropertiesTestData), parameters: false)]
        public async Task GetPropertiesTestJSAndManaged(bool test_js, bool? own_properties, bool? accessors_only, string[] expected_names)
        {
            string eval_expr;
            if (test_js)
            {
                await SetBreakpoint("/other.js", 93, 1);
                eval_expr = "window.setTimeout(function() { get_properties_test (); }, 1)";
            }
            else
            {
                await SetBreakpointInMethod("debugger-test.dll", "DebuggerTests.GetPropertiesTests.DerivedClassForJSTest", "run", 2);
                eval_expr = "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.GetPropertiesTests.DerivedClassForJSTest:run'); }, 1)";
            }

            var result = await cli.SendCommand("Runtime.evaluate", JObject.FromObject(new { expression = eval_expr }), token);
            var pause_location = await insp.WaitFor(Inspector.PAUSE);

            var id = pause_location["callFrames"][0]["scopeChain"][0]["object"]["objectId"].Value<string>();

            var frame_locals = await GetProperties(id);
            var obj = GetAndAssertObjectWithName(frame_locals, "obj");
            var obj_props = await GetProperties(obj["value"]?["objectId"]?.Value<string>(),
                                    own_properties: own_properties, accessors_only: accessors_only);

            IEnumerable<JToken> filtered_props;
            if (test_js)
            {
                filtered_props = obj_props.Children().Where(jt => jt["enumerable"]?.Value<bool>() == true);
            }
            else
            {
                // we don't set `enumerable` right now
                filtered_props = obj_props.Children().Where(jt => true);
            }

            var expected_props = new Dictionary<string, (JObject exp_obj, bool is_own)>()
                {
                    // own
                    {"owner_name", (TString("foo"), true)},
                    {"owner_last_name", (TGetter("owner_last_name"), true)},

                    // inherited
                    {"kind", (TString("car"), false)},
                    {"make", (TString("mini"), false)},
                    {"available", (TGetter("available"), false)},
                };

            await CheckExpectedProperties(
                    expected_names,
                    name => filtered_props.Where(jt => jt["name"]?.Value<string>() == name).SingleOrDefault(),
                    expected_props);

            AssertEqual(expected_names.Length, filtered_props.Count(), $"expected number of properties");
        }

        [Fact]
        public async Task GetObjectValueWithInheritance()
        {
            var pause_location = await EvaluateAndCheck(
               "window.setTimeout(function() { invoke_static_method('[debugger-test] TestChild:TestWatchWithInheritance'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test2.cs", 127, 8,
               "TestWatchWithInheritance");
            var frame_id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
            var frame_locals = await GetProperties(frame_id);
            var test_props = await GetObjectOnLocals(frame_locals, "test");
            await CheckProps(test_props, new
            {
                j = TNumber(20),
                i = TNumber(50),
                k = TNumber(30),
                GetJ = TGetter("GetJ"),
                GetI = TGetter("GetI"),
                GetK = TGetter("GetK")
            }, "test_props");
            await EvaluateOnCallFrameAndCheck(frame_id,
                ($"test.GetJ", TNumber(20)),
                ($"test.GetI", TNumber(50)),
                ($"test.GetK", TNumber(30))
            );
        }

        private async Task CheckExpectedProperties(string[] expected_names, Func<string, JToken> get_actual_prop, Dictionary<string, (JObject, bool)> all_props)
        {
            foreach (var exp_name in expected_names)
            {
                if (!all_props.TryGetValue(exp_name, out var expected))
                {
                    Assert.True(false, $"Test Bug: Could not find property named {exp_name}");
                }
                var (exp_prop, is_own) = expected;
                var actual_prop = get_actual_prop(exp_name);

                AssertEqual(is_own, actual_prop["isOwn"]?.Value<bool>() == true, $"{exp_name}#isOwn");

                if (exp_prop["__custom_type"]?.Value<string>() == "getter")
                {
                    // HACK! CheckValue normally expects to get a value:{}
                    // from `{name: "..", value: {}, ..}
                    // but for getters we actually have: `{name: "..", get: {..} }`
                    // and no `value`
                    await CheckValue(actual_prop, exp_prop, exp_name);
                }
                else
                {
                    await CheckValue(actual_prop["value"], exp_prop, exp_name);
                }
            }
        }

        private static void AssertHasOnlyExpectedProperties(string[] expected_names, IEnumerable<JObject> actual)
        {
            var exp = new HashSet<string>(expected_names);

            foreach (var obj in actual)
            {
                if (!exp.Contains(obj["name"]?.Value<string>()))
                    Console.WriteLine($"Unexpected: {obj}");
            }
        }

    }
}
