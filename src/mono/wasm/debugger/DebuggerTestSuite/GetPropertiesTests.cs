// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Sdk;
using Xunit.Abstractions;

namespace DebuggerTests
{
    public class GetPropertiesTests : DebuggerTests
    {
        public GetPropertiesTests(ITestOutputHelper testOutput) : base(testOutput)
        {}

        public static TheoryData<string, bool?, bool?, string[], Dictionary<string, (JObject, bool)>, bool> ClassGetPropertiesTestData(bool is_async)
        {
            // FIXME: invoking getter on the hidden(base) properties - is that supported??
            // FIXME: add case - v -> o -> n, v -> n -> o
            var data = new TheoryData<string, bool?, bool?, string[], Dictionary<string, (JObject, bool)>, bool>();

            var type_name = "DerivedClass2";
            var all_props = new Dictionary<string, (JObject, bool)>()
            {
                // own:
                // public:
                {"BaseBase_PropertyForHidingWithField",             (TNumber(210), true)},
                {"Base_PropertyForOverridingWithProperty",          (TGetter("Base_PropertyForOverridingWithProperty", TDateTime(new DateTime(2020, 7, 6, 5, 4, 3))), true)},
                {"BaseBase_PropertyForVHO",                         (TGetter("BaseBase_PropertyForVHO", TString("Derived#BaseBase_PropertyForVHO")), true)},
                {"BaseBase_PropertyForVOH",                         (TGetter("BaseBase_PropertyForVOH", TString("Derived#BaseBase_PropertyForVOH")), true)},
                // {"BaseBase_PropertyForVOO",                         (TGetter("BaseBase_PropertyForVOO", TString("Derived#BaseBase_PropertyForVOO")), true)}, //FixMe: Issue #69788

                // protected / internal:
                {"BaseBase_AutoPropertyForHidingWithProperty",      (TGetter("BaseBase_AutoPropertyForHidingWithProperty", TString("Derived#BaseBase_AutoPropertyForHidingWithProperty")), true)},
                {"Base_PropertyForOverridingWithAutoProperty",      (TDateTime(new DateTime(2022, 7, 6, 5, 4, 3)), true)},
                {"Base_AutoPropertyForOverridingWithAutoProperty",  (TDateTime(new DateTime(2023, 7, 6, 5, 4, 3)), true)},
                {"Base_AutoPropertyForOverridingWithProperty",      (TGetter("Base_AutoPropertyForOverridingWithProperty", TDateTime(new DateTime(2021, 7, 6, 5, 4, 3))), true)},

                // private:
                {"BaseBase_FieldForHidingWithAutoProperty",         (TString("Derived#BaseBase_FieldForHidingWithAutoProperty"), true)},

                // inherited from Base:
                // public:
                {"BaseBase_AutoPropertyForHidingWithField",                 (TNumber(115), false)},
                {"BaseBase_PropertyForHidingWithProperty",                  (TGetter("BaseBase_PropertyForHidingWithProperty", TString("Base#BaseBase_PropertyForHidingWithProperty")), false)},
                {"BaseBase_FieldForHidingWithAutoProperty (BaseClass2)",    (TString("Base#BaseBase_FieldForHidingWithAutoProperty"), false)},
                {"Base_VirtualPropertyNotOverriddenOrHidden",               (TGetter("Base_VirtualPropertyNotOverriddenOrHidden", TDateTime(new DateTime(2124, 5, 7, 1, 9, 2))), false)},
                {"FirstName",                                               (TGetter("FirstName", TString("BaseClass#FirstName")), false)},
                {"LastName",                                                (TGetter("LastName", TString("BaseClass#LastName")), false)},
                {"BaseBase_PropertyForVOH (BaseClass2)",                    (TGetter("BaseBase_PropertyForVOH (BaseClass2)", TString("Base#BaseBase_PropertyForVOH")), false)},

                // protected / internal:
                {"BaseBase_PropertyForHidingWithField (BaseClass2)",        (TNumber(110), false)},
                {"BaseBase_FieldForHidingWithProperty",                     (TGetter("BaseBase_FieldForHidingWithProperty", TString("Base#BaseBase_FieldForHidingWithProperty")), false)},
                {"BaseBase_AutoPropertyForHidingWithAutoProperty",          (TString("Base#BaseBase_AutoPropertyForHidingWithAutoProperty"), false)},

                // private:
                {"BaseBase_FieldForHidingWithField",                         (TNumber(105), false)},
                {"BaseBase_AutoPropertyForHidingWithProperty (BaseClass2)",  (TGetter("BaseBase_AutoPropertyForHidingWithProperty (BaseClass2)", TString("Base#BaseBase_AutoPropertyForHidingWithProperty")), false)},
                {"BaseBase_PropertyForHidingWithAutoProperty",               (TString("Base#BaseBase_PropertyForHidingWithAutoProperty"), false)},

                // inherited from BaseBase:
                // public:
                {"BaseBase_FieldForHidingWithField (BaseBaseClass2)",                (TNumber(5), false)},
                {"BaseBase_PropertyForHidingWithField (BaseBaseClass2)",             (TGetter("BaseBase_PropertyForHidingWithField (BaseBaseClass2)", TNumber(10)), false)},
                {"BaseBase_AutoPropertyForHidingWithField (BaseBaseClass2)",         (TNumber(15), false)},
                {"BaseBase_FieldForHidingWithProperty (BaseBaseClass2)",             (TString("BaseBase#BaseBase_FieldForHidingWithProperty"), false)},
                {"BaseBase_PropertyForHidingWithProperty (BaseBaseClass2)",          (TGetter("BaseBase_PropertyForHidingWithProperty (BaseBaseClass2)", TString("BaseBase#BaseBase_PropertyForHidingWithProperty")), false)},
                {"BaseBase_AutoPropertyForHidingWithProperty (BaseBaseClass2)",      (TString("BaseBase#BaseBase_AutoPropertyForHidingWithProperty"), false)},
                {"BaseBase_FieldForHidingWithAutoProperty (BaseBaseClass2)",         (TString("BaseBase#BaseBase_FieldForHidingWithAutoProperty"), false)},
                {"BaseBase_PropertyForHidingWithAutoProperty (BaseBaseClass2)",      (TGetter("BaseBase_PropertyForHidingWithAutoProperty (BaseBaseClass2)", TString("BaseBase#BaseBase_PropertyForHidingWithAutoProperty")), false)},
                {"BaseBase_AutoPropertyForHidingWithAutoProperty (BaseBaseClass2)",  (TString("BaseBase#BaseBase_AutoPropertyForHidingWithAutoProperty"), false)},
                // {"BaseBase_PropertyForVHO (BaseBaseClass2)",                         (TGetter("BaseBase_PropertyForVHO (BaseBaseClass2)", TString("BaseBase#BaseBase_PropertyForVHO")), false)}, // FixMe: Issue #69788
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
            }

            var all_accessors = new[]
            {
                "Base_PropertyForOverridingWithProperty",
                "BaseBase_AutoPropertyForHidingWithProperty",
                "Base_AutoPropertyForOverridingWithProperty",
                "BaseBase_PropertyForVHO",
                "BaseBase_PropertyForVOH",
                // "BaseBase_PropertyForVOO", // FixMe: Issue #69788

                "BaseBase_PropertyForHidingWithProperty",
                "FirstName",
                "LastName",
                "BaseBase_FieldForHidingWithProperty",
                "BaseBase_AutoPropertyForHidingWithProperty (BaseClass2)",
                "BaseBase_PropertyForVOH (BaseClass2)",

                "BaseBase_PropertyForHidingWithField (BaseBaseClass2)",
                "BaseBase_PropertyForHidingWithProperty (BaseBaseClass2)",
                "BaseBase_PropertyForHidingWithAutoProperty (BaseBaseClass2)",
                "Base_VirtualPropertyNotOverriddenOrHidden",
                // "BaseBase_PropertyForVHO (BaseBaseClass2)" // FixMe: Issue #69788
            };

            var only_own_accessors = new[]
            {
                "Base_PropertyForOverridingWithProperty",
                "BaseBase_AutoPropertyForHidingWithProperty",
                "Base_AutoPropertyForOverridingWithProperty",
                "BaseBase_PropertyForVHO",
                "BaseBase_PropertyForVOH",
                // "BaseBase_PropertyForVOO", // FixMe: Issue #69788
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

                // protected
                {"b",                       (TBool(true), true)},

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

        [ConditionalTheory(nameof(RunningOnChrome))]
        [MemberData(nameof(ClassGetPropertiesTestData), parameters: true)]
        [MemberData(nameof(ClassGetPropertiesTestData), parameters: false)]
        [MemberData(nameof(StructGetPropertiesTestData), parameters: true)]
        [MemberData(nameof(StructGetPropertiesTestData), parameters: false)]
        public async Task InspectTypeInheritedMembers(string type_name, bool? own_properties, bool? accessors_only, string[] expected_names, Dictionary<string, (JObject, bool)> all_props, bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.GetPropertiesTests.{type_name}",
            $"InstanceMethod{(is_async ? "Async" : "")}", 1, $"DebuggerTests.GetPropertiesTests.{type_name}." + (is_async ? "InstanceMethodAsync" : "InstanceMethod"),
            $"window.setTimeout(function() {{ invoke_static_method_async ('[debugger-test] DebuggerTests.GetPropertiesTests.{type_name}:run'); }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var frame_id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var frame_locals = await GetProperties(frame_id);
                var this_obj = GetAndAssertObjectWithName(frame_locals, "this");
                var this_props = await GetProperties(this_obj["value"]?["objectId"]?.Value<string>(), own_properties: own_properties, accessors_only: accessors_only);

                AssertHasOnlyExpectedProperties(expected_names, this_props.Values<JObject>());
                await CheckExpectedProperties(expected_names, name => GetAndAssertObjectWithName(this_props, name), all_props);

                // indexer properties shouldn't show up here
                var item = this_props.FirstOrDefault(jt => jt["name"]?.Value<string>() == "Item");
                Assert.Null(item);
            });

        public static IEnumerable<object[]> MembersForLocalNestedStructData(bool is_async)
            => StructGetPropertiesTestData(false).Select(datum => datum[1..]);

        [ConditionalTheory(nameof(RunningOnChrome))]
        [MemberData(nameof(MembersForLocalNestedStructData), parameters: false)]
        [MemberData(nameof(MembersForLocalNestedStructData), parameters: true)]
        public async Task MembersForLocalNestedStruct(bool? own_properties, bool? accessors_only, string[] expected_names, Dictionary<string, (JObject, bool)> all_props, bool is_async) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.GetPropertiesTests.NestedStruct",
            is_async ? $"TestNestedStructStaticAsync" : "TestNestedStructStatic",
            2,
            "DebuggerTests.GetPropertiesTests.NestedStruct." + (is_async ? "TestNestedStructStaticAsync" : $"TestNestedStructStatic"),
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

        [ConditionalTheory(nameof(RunningOnChrome))]
        [MemberData(nameof(JSGetPropertiesTestData), parameters: true)]
        // Note: Disabled because we don't match JS's behavior here!
        //       We return inherited members too for `ownProperties:true`
        // [MemberData(nameof(JSGetPropertiesTestData), parameters: false)]
        public async Task GetPropertiesTestJSAndManaged(bool test_js, bool? own_properties, bool? accessors_only, string[] expected_names)
        {
            string eval_expr;
            if (test_js)
            {
                await SetBreakpoint("/other.js", 95, 1);
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

            //AssertEqual(expected_names.Length, filtered_props.Count(), $"expected number of properties");
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task GetObjectValueWithInheritance()
        {
            var pause_location = await EvaluateAndCheck(
               "window.setTimeout(function() { invoke_static_method('[debugger-test] TestChild:TestWatchWithInheritance'); }, 1);",
                "dotnet://debugger-test.dll/debugger-test2.cs", 128, 8,
               "TestChild.TestWatchWithInheritance");
            var frame_id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
            var frame_locals = await GetProperties(frame_id);
            var test_props = await GetObjectOnLocals(frame_locals, "test");
            await CheckProps(test_props, new
            {
                j = TNumber(20),
                i = TNumber(50),
                k = TNumber(30),
                GetJ = TGetter("GetJ", TNumber(20)),
                GetI = TGetter("GetI", TNumber(50)),
                GetK = TGetter("GetK", TNumber(30)),
                GetD = TGetter("GetD", TDateTime(new DateTime(2020, 7, 6, 5, 4, 3)))
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

        private void AssertHasOnlyExpectedProperties(string[] expected_names, IEnumerable<JObject> actual)
        {
            bool fail = false;
            var exp = new HashSet<string>(expected_names);

            foreach (var obj in actual)
            {
                if (!exp.Contains(obj["name"]?.Value<string>()))
                {
                    _testOutput.WriteLine($"Unexpected: {obj}");
                    fail = true;
                }
            }

            var act = new HashSet<string>(actual.Select(a => a["name"].Value<string>()));
            foreach (var obj in expected_names.Where(ename => !act.Contains(ename)))
            {
                _testOutput.WriteLine($"Missing: {obj}");
                fail = true;
            }

            if (fail)
                throw new XunitException($"missing or unexpected members found");
        }

        public static TheoryData<Dictionary<string, JObject>, Dictionary<string, JObject>, Dictionary<string, JObject>, string> GetDataForProtectionLevels()
        {
            var data = new TheoryData<Dictionary<string, JObject>, Dictionary<string, JObject>, Dictionary<string, JObject>, string>();

            var public_props = new Dictionary<string, JObject>()
            {
                // own:
                {"BaseBase_PropertyForHidingWithField",             TNumber(210)},
                {"Base_PropertyForOverridingWithProperty",          TGetter("Base_PropertyForOverridingWithProperty", TDateTime(new DateTime(2020, 7, 6, 5, 4, 3)))},
                {"BaseBase_PropertyForVHO",                         TGetter("BaseBase_PropertyForVHO", TString("Derived#BaseBase_PropertyForVHO"))},
                {"BaseBase_PropertyForVOH",                         TGetter("BaseBase_PropertyForVOH", TString("Derived#BaseBase_PropertyForVOH"))},
                // {"BaseBase_PropertyForVOO",                         TGetter("BaseBase_PropertyForVOO", TString("Derived#BaseBase_PropertyForVOO"))}, //FixMe: Issue #69788

                // inherited from Base:
                {"BaseBase_AutoPropertyForHidingWithField",                 TNumber(115)},
                {"BaseBase_PropertyForHidingWithProperty",                  TGetter("BaseBase_PropertyForHidingWithProperty", TString("Base#BaseBase_PropertyForHidingWithProperty"))},
                {"BaseBase_FieldForHidingWithAutoProperty (BaseClass2)",    TString("Base#BaseBase_FieldForHidingWithAutoProperty")},
                {"FirstName",                                               TGetter("FirstName", TString("BaseClass#FirstName"))},
                {"LastName",                                                TGetter("LastName", TString("BaseClass#LastName"))},
                {"Base_VirtualPropertyNotOverriddenOrHidden",               TGetter("Base_VirtualPropertyNotOverriddenOrHidden", TDateTime(new DateTime(2124, 5, 7, 1, 9, 2)))},
                {"BaseBase_PropertyForVOH (BaseClass2)",                    TGetter("BaseBase_PropertyForVOH (BaseClass2)", TString("Base#BaseBase_PropertyForVOH"))},

                // inherited from BaseBase:
                {"BaseBase_FieldForHidingWithField (BaseBaseClass2)",                TNumber(5)},
                {"BaseBase_PropertyForHidingWithField (BaseBaseClass2)",             TGetter("BaseBase_PropertyForHidingWithField (BaseBaseClass2)", TNumber(10))},
                {"BaseBase_AutoPropertyForHidingWithField (BaseBaseClass2)",         TNumber(15)},
                {"BaseBase_FieldForHidingWithProperty (BaseBaseClass2)",             TString("BaseBase#BaseBase_FieldForHidingWithProperty")},
                {"BaseBase_PropertyForHidingWithProperty (BaseBaseClass2)",          TGetter("BaseBase_PropertyForHidingWithProperty (BaseBaseClass2)", TString("BaseBase#BaseBase_PropertyForHidingWithProperty"))},
                {"BaseBase_AutoPropertyForHidingWithProperty (BaseBaseClass2)",      TString("BaseBase#BaseBase_AutoPropertyForHidingWithProperty")},
                {"BaseBase_FieldForHidingWithAutoProperty (BaseBaseClass2)",         TString("BaseBase#BaseBase_FieldForHidingWithAutoProperty")},
                {"BaseBase_PropertyForHidingWithAutoProperty (BaseBaseClass2)",      TGetter("BaseBase_PropertyForHidingWithAutoProperty (BaseBaseClass2)", TString("BaseBase#BaseBase_PropertyForHidingWithAutoProperty"))},
                {"BaseBase_AutoPropertyForHidingWithAutoProperty (BaseBaseClass2)",  TString("BaseBase#BaseBase_AutoPropertyForHidingWithAutoProperty")},
                // {"BaseBase_PropertyForVHO (BaseBaseClass2)",                         TGetter("BaseBase_PropertyForVHO (BaseBaseClass2)", TString("BaseBase#BaseBase_PropertyForVHO"))}, // FixMe: Issue #69788
            };

            var internal_protected_props = new Dictionary<string, JObject>(){

                // own:
                {"BaseBase_AutoPropertyForHidingWithProperty",          TGetter("BaseBase_AutoPropertyForHidingWithProperty", TString("Derived#BaseBase_AutoPropertyForHidingWithProperty"))},
                {"Base_PropertyForOverridingWithAutoProperty",          TDateTime(new DateTime(2022, 7, 6, 5, 4, 3))},
                {"Base_AutoPropertyForOverridingWithAutoProperty",      TDateTime(new DateTime(2023, 7, 6, 5, 4, 3))},
                {"Base_AutoPropertyForOverridingWithProperty",          TGetter("Base_AutoPropertyForOverridingWithProperty", TDateTime(new DateTime(2021, 7, 6, 5, 4, 3)))},
                // inherited from Base:
                {"BaseBase_PropertyForHidingWithField (BaseClass2)",    TNumber(110)},
                {"BaseBase_FieldForHidingWithProperty",                 TGetter("BaseBase_FieldForHidingWithProperty", TString("Base#BaseBase_FieldForHidingWithProperty"))},
                {"BaseBase_AutoPropertyForHidingWithAutoProperty",      TString("Base#BaseBase_AutoPropertyForHidingWithAutoProperty")}
            };

            var private_props = new Dictionary<string, JObject>(){
                 // own
                {"BaseBase_FieldForHidingWithAutoProperty",                 TString("Derived#BaseBase_FieldForHidingWithAutoProperty")},
                // from Base:
                {"BaseBase_FieldForHidingWithField",                        TNumber(105)},
                {"BaseBase_AutoPropertyForHidingWithProperty (BaseClass2)", TGetter("BaseBase_AutoPropertyForHidingWithProperty (BaseClass2)", TString("Base#BaseBase_AutoPropertyForHidingWithProperty"))},
                {"BaseBase_PropertyForHidingWithAutoProperty",              TString("Base#BaseBase_PropertyForHidingWithAutoProperty")},
            };
            data.Add(public_props, internal_protected_props, private_props, "DerivedClass2");

            // structure CloneableStruct:
            public_props = new Dictionary<string, JObject>()
            {
                // own
                {"a",                       TNumber(4)},
                {"DateTime",                TGetter("DateTime")},
                {"AutoStringProperty",      TString("CloneableStruct#AutoStringProperty")},
                {"FirstName",               TGetter("FirstName")},
                {"LastName",                TGetter("LastName")}
            };
            internal_protected_props = new Dictionary<string, JObject>()
            {
                // internal
                {"b",                       TBool(true)}
            };
            private_props = new Dictionary<string, JObject>()
            {
                {"_stringField",            TString("CloneableStruct#_stringField")},
                {"_dateTime",               TDateTime(new DateTime(2020, 7, 6, 5, 4, 3 + 3))},
                {"_DTProp",                 TGetter("_DTProp")}
            };
            data.Add(public_props, internal_protected_props, private_props, "CloneableStruct");
            return data;
        }

        [ConditionalTheory(nameof(RunningOnChrome))]
        [MemberData(nameof(GetDataForProtectionLevels))]
        public async Task PropertiesSortedByProtectionLevel(
            Dictionary<string, JObject> expectedPublic, Dictionary<string, JObject> expectedProtInter, Dictionary<string, JObject> expectedPriv, string entryMethod) =>
            await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.GetPropertiesTests.{entryMethod}", "InstanceMethod", 1, $"DebuggerTests.GetPropertiesTests.{entryMethod}.InstanceMethod",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.GetPropertiesTests.{entryMethod}:run'); }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var (obj, _) = await EvaluateOnCallFrame(id, "this");
                var (pub, internalAndProtected, priv) = await GetPropertiesSortedByProtectionLevels(obj["objectId"]?.Value<string>());

                AssertHasOnlyExpectedProperties(expectedPublic.Keys.ToArray(), pub.Values<JObject>());
                AssertHasOnlyExpectedProperties(expectedProtInter.Keys.ToArray(), internalAndProtected.Values<JObject>());
                AssertHasOnlyExpectedProperties(expectedPriv.Keys.ToArray(), priv.Values<JObject>());

                await CheckProps(pub, expectedPublic, "public");
                await CheckProps(internalAndProtected, expectedProtInter, "internalAndProtected");
                await CheckProps(priv, expectedPriv, "private");
            });
    }
}
