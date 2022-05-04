// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{

    public class PointerTests : DebuggerTests
    {

        public static TheoryData<string, string, string, int, string, bool> PointersTestData =>
            new TheoryData<string, string, string, int, string, bool>
            { { $"invoke_static_method ('[debugger-test] DebuggerTests.PointerTests:LocalPointers');", "DebuggerTests.PointerTests", "LocalPointers", 32, "LocalPointers", false },
                { $"invoke_static_method ('[debugger-test] DebuggerTests.PointerTests:LocalPointers');", "DebuggerTests.PointerTests", "LocalPointers", 32, "LocalPointers", true },
                { $"invoke_static_method_async ('[debugger-test] DebuggerTests.PointerTests:LocalPointersAsync');", "DebuggerTests.PointerTests", "LocalPointersAsync", 32, "LocalPointersAsync", false },
                { $"invoke_static_method_async ('[debugger-test] DebuggerTests.PointerTests:LocalPointersAsync');", "DebuggerTests.PointerTests", "LocalPointersAsync", 32, "LocalPointersAsync", true }
            };

        [ConditionalTheory(nameof(RunningOnChrome))]
        [MemberDataAttribute(nameof(PointersTestData))]
        public async Task InspectLocalPointersToPrimitiveTypes(string eval_fn, string type, string method, int line_offset, string bp_function_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            type, method, line_offset, bp_function_name,
            "window.setTimeout(function() { " + eval_fn + " })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               var dt = new DateTime(5, 6, 7, 8, 9, 10);
               await CheckProps(locals, new
               {
                   ip = TPointer("int*"),
                   ip_null = TPointer("int*", is_null: true),
                   ipp = TPointer("int**"),
                   ipp_null = TPointer("int**"),

                   cvalue0 = TChar('q'),
                   cp = TPointer("char*"),

                   vp = TPointer("void*"),
                   vp_null = TPointer("void*", is_null: true),
               }, "locals", num_fields: 26);

               var props = await GetObjectOnLocals(locals, "ip");
               await CheckPointerValue(props, "*ip", TNumber(5), "locals");

               {
                   var ipp_props = await GetObjectOnLocals(locals, "ipp");
                   await CheckPointerValue(ipp_props, "*ipp", TPointer("int*"));

                   ipp_props = await GetObjectOnLocals(ipp_props, "*ipp");
                   await CheckPointerValue(ipp_props, "**ipp", TNumber(5));
               }

               {
                   var ipp_props = await GetObjectOnLocals(locals, "ipp_null");
                   await CheckPointerValue(ipp_props, "*ipp_null", TPointer("int*", is_null: true));
               }

               // *cp
               props = await GetObjectOnLocals(locals, "cp");
               await CheckPointerValue(props, "*cp", TChar('q'));
           });

        [Theory]
        [MemberDataAttribute(nameof(PointersTestData))]
        public async Task InspectLocalPointerArrays(string eval_fn, string type, string method, int line_offset, string bp_function_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            type, method, line_offset, bp_function_name,
            "window.setTimeout(function() { " + eval_fn + " })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               var dt = new DateTime(5, 6, 7, 8, 9, 10);
               await CheckProps(locals, new
               {
                   ipa = TArray("int*[]", "int*[3]")
               }, "locals", num_fields: 26);

               var ipa_elems = await CompareObjectPropertiesFor(locals, "ipa", new[]
               {
                    TPointer("int*"),
                        TPointer("int*"),
                        TPointer("int*", is_null : true)
               });

               await CheckArrayElements(ipa_elems, new[]
               {
                    TNumber(5),
                        TNumber(10),
                        null
               });
           });

        [Theory]
        [MemberDataAttribute(nameof(PointersTestData))]
        public async Task InspectLocalDoublePointerToPrimitiveTypeArrays(string eval_fn, string type, string method, int line_offset, string bp_function_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            type, method, line_offset, bp_function_name,
            "window.setTimeout(function() { " + eval_fn + " })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               var dt = new DateTime(5, 6, 7, 8, 9, 10);
               await CheckProps(locals, new
               {
                   ippa = TArray("int**[]", "int**[5]")
               }, "locals", num_fields: 26);

               var ippa_elems = await CompareObjectPropertiesFor(locals, "ippa", new[]
               {
                    TPointer("int**"),
                        TPointer("int**"),
                        TPointer("int**"),
                        TPointer("int**"),
                        TPointer("int**", is_null : true)
               });

               {
                   var actual_elems = await CheckArrayElements(ippa_elems, new[]
                   {
                        TPointer("int*"),
                            TPointer("int*", is_null : true),
                            TPointer("int*"),
                            TPointer("int*", is_null : true),
                            null
                   });

                   var val = await GetObjectOnLocals(actual_elems[0], "*[0]");
                   await CheckPointerValue(val, "**[0]", TNumber(5));

                   val = await GetObjectOnLocals(actual_elems[2], "*[2]");
                   await CheckPointerValue(val, "**[2]", TNumber(5));
               }
           });

        [Theory]
        [MemberDataAttribute(nameof(PointersTestData))]
        public async Task InspectLocalPointersToValueTypes(string eval_fn, string type, string method, int line_offset, string bp_function_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            type, method, line_offset, bp_function_name,
            "window.setTimeout(function() { " + eval_fn + " })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               var dt = new DateTime(5, 6, 7, 8, 9, 10);
               await CheckProps(locals, new
               {
                   dt = TDateTime(dt),
                   dtp = TPointer("System.DateTime*"),
                   dtp_null = TPointer("System.DateTime*", is_null: true),

                   gsp = TPointer("DebuggerTests.GenericStructWithUnmanagedT<System.DateTime>*"),
                   gsp_null = TPointer("DebuggerTests.GenericStructWithUnmanagedT<System.DateTime>*")
               }, "locals", num_fields: 26);

               // *dtp
               var props = await GetObjectOnLocals(locals, "dtp");
               await CheckDateTime(props, "*dtp", dt);

               var gsp_props = await GetObjectOnLocals(locals, "gsp");
               await CheckPointerValue(gsp_props, "*gsp", TValueType("DebuggerTests.GenericStructWithUnmanagedT<System.DateTime>"), "locals#gsp");

               {
                   var gs_dt = new DateTime(1, 2, 3, 4, 5, 6);

                   var gsp_deref_props = await GetObjectOnLocals(gsp_props, "*gsp");
                   await CheckProps(gsp_deref_props, new
                   {
                       Value = TDateTime(gs_dt),
                       IntField = TNumber(4),
                       DTPP = TPointer("System.DateTime**")
                   }, "locals#gsp#deref");
                   {
                       var dtpp_props = await GetObjectOnLocals(gsp_deref_props, "DTPP");
                       await CheckPointerValue(dtpp_props, "*DTPP", TPointer("System.DateTime*"), "locals#*gsp");

                       var dtpp_deref_props = await GetObjectOnLocals(dtpp_props, "*DTPP");
                       await CheckDateTime(dtpp_deref_props, "**DTPP", dt);
                   }
               }

               // gsp_null
               var gsp_w_n_props = await GetObjectOnLocals(locals, "gsp_null");
               await CheckPointerValue(gsp_w_n_props, "*gsp_null", TValueType("DebuggerTests.GenericStructWithUnmanagedT<System.DateTime>"), "locals#gsp");

               {
                   var gs_dt = new DateTime(1, 2, 3, 4, 5, 6);

                   var gsp_deref_props = await GetObjectOnLocals(gsp_w_n_props, "*gsp_null");
                   await CheckProps(gsp_deref_props, new
                   {
                       Value = TDateTime(gs_dt),
                       IntField = TNumber(4),
                       DTPP = TPointer("System.DateTime**")
                   }, "locals#gsp#deref");
                   {
                       var dtpp_props = await GetObjectOnLocals(gsp_deref_props, "DTPP");
                       await CheckPointerValue(dtpp_props, "*DTPP", TPointer("System.DateTime*", is_null: true), "locals#*gsp");
                   }
               }
           });

        [Theory]
        [MemberDataAttribute(nameof(PointersTestData))]
        public async Task InspectLocalPointersToValueTypeArrays(string eval_fn, string type, string method, int line_offset, string bp_function_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            type, method, line_offset, bp_function_name,
            "window.setTimeout(function() { " + eval_fn + " })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               var dt = new DateTime(5, 6, 7, 8, 9, 10);
               await CheckProps(locals, new
               {
                   dtpa = TArray("System.DateTime*[]", "System.DateTime*[2]")
               }, "locals", num_fields: 26);

               // dtpa
               var dtpa_elems = (await CompareObjectPropertiesFor(locals, "dtpa", new[]
              {
                    TPointer("System.DateTime*"),
                        TPointer("System.DateTime*", is_null : true)
               }));
               {
                   var actual_elems = await CheckArrayElements(dtpa_elems, new[]
                   {
                        TValueType("System.DateTime", dt.ToString()),
                            null
                   });

                   await CheckDateTime(actual_elems[0], "*[0]", dt);
               }
           });

        [Theory]
        [MemberDataAttribute(nameof(PointersTestData))]
        public async Task InspectLocalPointersToGenericValueTypeArrays(string eval_fn, string type, string method, int line_offset, string bp_function_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            type, method, line_offset, bp_function_name,
            "window.setTimeout(function() { " + eval_fn + " })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               var dt = new DateTime(5, 6, 7, 8, 9, 10);
               await CheckProps(locals, new
               {
                   gspa = TArray("DebuggerTests.GenericStructWithUnmanagedT<System.DateTime>*[]", "DebuggerTests.GenericStructWithUnmanagedT<System.DateTime>*[3]"),
               }, "locals", num_fields: 26);

               // dtpa
               var gspa_elems = await CompareObjectPropertiesFor(locals, "gspa", new[]
              {
                    TPointer("DebuggerTests.GenericStructWithUnmanagedT<System.DateTime>*", is_null : true),
                        TPointer("DebuggerTests.GenericStructWithUnmanagedT<System.DateTime>*"),
                        TPointer("DebuggerTests.GenericStructWithUnmanagedT<System.DateTime>*"),
               });
               {
                   var gs_dt = new DateTime(1, 2, 3, 4, 5, 6);
                   var actual_elems = await CheckArrayElements(gspa_elems, new[]
                   {
                        null,
                        TValueType("DebuggerTests.GenericStructWithUnmanagedT<System.DateTime>"),
                        TValueType("DebuggerTests.GenericStructWithUnmanagedT<System.DateTime>")
                   });

                   // *[1]
                   {
                       var gsp_deref_props = await GetObjectOnLocals(actual_elems[1], "*[1]");
                       await CheckProps(gsp_deref_props, new
                       {
                           Value = TDateTime(gs_dt),
                           IntField = TNumber(4),
                           DTPP = TPointer("System.DateTime**")
                       }, "locals#gsp#deref");
                       {
                           var dtpp_props = await GetObjectOnLocals(gsp_deref_props, "DTPP");
                           await CheckPointerValue(dtpp_props, "*DTPP", TPointer("System.DateTime*"), "locals#*gsp");

                           dtpp_props = await GetObjectOnLocals(dtpp_props, "*DTPP");
                           await CheckDateTime(dtpp_props, "**DTPP", dt);
                       }
                   }

                   // *[2]
                   {
                       var gsp_deref_props = await GetObjectOnLocals(actual_elems[2], "*[2]");
                       await CheckProps(gsp_deref_props, new
                       {
                           Value = TDateTime(gs_dt),
                           IntField = TNumber(4),
                           DTPP = TPointer("System.DateTime**")
                       }, "locals#gsp#deref");
                       {
                           var dtpp_props = await GetObjectOnLocals(gsp_deref_props, "DTPP");
                           await CheckPointerValue(dtpp_props, "*DTPP", TPointer("System.DateTime*", is_null: true), "locals#*gsp");
                       }
                   }
               }
           });

        [Theory]
        [MemberDataAttribute(nameof(PointersTestData))]
        public async Task InspectLocalDoublePointersToValueTypeArrays(string eval_fn, string type, string method, int line_offset, string bp_function_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            type, method, line_offset, bp_function_name,
            "window.setTimeout(function() { " + eval_fn + " })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               var dt = new DateTime(5, 6, 7, 8, 9, 10);
               await CheckProps(locals, new
               {
                   dtppa = TArray("System.DateTime**[]", "System.DateTime**[3]"),
               }, "locals", num_fields: 26);

               // DateTime**[] dtppa = new DateTime**[] { &dtp, &dtp_null, null };
               var dtppa_elems = (await CompareObjectPropertiesFor(locals, "dtppa", new[]
              {
                    TPointer("System.DateTime**"),
                        TPointer("System.DateTime**"),
                        TPointer("System.DateTime**", is_null : true)
               }));

               var exp_elems = new[]
               {
                    TPointer("System.DateTime*"),
                    TPointer("System.DateTime*", is_null : true),
                    null
               };

               var actual_elems = new JToken[exp_elems.Length];
               for (int i = 0; i < exp_elems.Length; i++)
               {
                   if (exp_elems[i] != null)
                   {
                       actual_elems[i] = await GetObjectOnLocals(dtppa_elems, i.ToString());
                       await CheckPointerValue(actual_elems[i], $"*[{i}]", exp_elems[i], $"dtppa->");
                   }
               }
           });

        [Theory]
        [MemberDataAttribute(nameof(PointersTestData))]
        public async Task InspectLocalPointersInClasses(string eval_fn, string type, string method, int line_offset, string bp_function_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            type, method, line_offset, bp_function_name,
            "window.setTimeout(function() { " + eval_fn + " })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               var dt = new DateTime(5, 6, 7, 8, 9, 10);
               await CheckProps(locals, new
               {
                   cwp = TObject("DebuggerTests.GenericClassWithPointers<System.DateTime>"),
                   cwp_null = TObject("DebuggerTests.GenericClassWithPointers<System.DateTime>")
               }, "locals", num_fields: 26);

               var cwp_props = await GetObjectOnLocals(locals, "cwp");
               var ptr_props = await GetObjectOnLocals(cwp_props, "Ptr");
               await CheckDateTime(ptr_props, "*Ptr", dt);
           });

        public static TheoryData<string, string, string, int, string, bool> PointersAsMethodArgsTestData =>
            new TheoryData<string, string, string, int, string, bool>
            { { $"invoke_static_method ('[debugger-test] DebuggerTests.PointerTests:LocalPointers');", "DebuggerTests.PointerTests", "PointersAsArgsTest", 2, "PointersAsArgsTest", false },
                { $"invoke_static_method ('[debugger-test] DebuggerTests.PointerTests:LocalPointers');", "DebuggerTests.PointerTests", "PointersAsArgsTest", 2, "PointersAsArgsTest", true },
            };

        [Theory]
        [MemberDataAttribute(nameof(PointersAsMethodArgsTestData))]
        public async Task InspectPrimitiveTypePointersAsMethodArgs(string eval_fn, string type, string method, int line_offset, string bp_function_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            type, method, line_offset, bp_function_name,
            "window.setTimeout(function() { " + eval_fn + " })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               var dt = new DateTime(5, 6, 7, 8, 9, 10);
               await CheckProps(locals, new
               {
                   ip = TPointer("int*"),
                   ipp = TPointer("int**"),
                   ipa = TArray("int*[]", "int*[3]"),
                   ippa = TArray("int**[]", "int**[5]")
               }, "locals", num_fields: 8);

               // ip
               var props = await GetObjectOnLocals(locals, "ip");
               await CheckPointerValue(props, "*ip", TNumber(5), "locals");

               // ipp
               var ipp_props = await GetObjectOnLocals(locals, "ipp");
               await CheckPointerValue(ipp_props, "*ipp", TPointer("int*"));

               ipp_props = await GetObjectOnLocals(ipp_props, "*ipp");
               await CheckPointerValue(ipp_props, "**ipp", TNumber(5));

               // ipa
               var ipa_elems = await CompareObjectPropertiesFor(locals, "ipa", new[]
              {
                    TPointer("int*"),
                        TPointer("int*"),
                        TPointer("int*", is_null : true)
               });

               await CheckArrayElements(ipa_elems, new[]
               {
                    TNumber(5),
                        TNumber(10),
                        null
               });

               // ippa
               var ippa_elems = await CompareObjectPropertiesFor(locals, "ippa", new[]
              {
                    TPointer("int**"),
                        TPointer("int**"),
                        TPointer("int**"),
                        TPointer("int**"),
                        TPointer("int**", is_null : true)
               });

               {
                   var actual_elems = await CheckArrayElements(ippa_elems, new[]
                   {
                        TPointer("int*"),
                            TPointer("int*", is_null : true),
                            TPointer("int*"),
                            TPointer("int*", is_null : true),
                            null
                   });

                   var val = await GetObjectOnLocals(actual_elems[0], "*[0]");
                   await CheckPointerValue(val, "**[0]", TNumber(5));

                   val = await GetObjectOnLocals(actual_elems[2], "*[2]");
                   await CheckPointerValue(val, "**[2]", TNumber(5));
               }
           });

        [Theory]
        [MemberDataAttribute(nameof(PointersAsMethodArgsTestData))]
        public async Task InspectValueTypePointersAsMethodArgs(string eval_fn, string type, string method, int line_offset, string bp_function_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            type, method, line_offset, bp_function_name,
            "window.setTimeout(function() { " + eval_fn + " })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               var dt = new DateTime(5, 6, 7, 8, 9, 10);
               await CheckProps(locals, new
               {
                   dtp = TPointer("System.DateTime*"),
                   dtpp = TPointer("System.DateTime**"),
                   dtpa = TArray("System.DateTime*[]", "System.DateTime*[2]"),
                   dtppa = TArray("System.DateTime**[]", "System.DateTime**[3]")
               }, "locals", num_fields: 8);

               // *dtp
               var dtp_props = await GetObjectOnLocals(locals, "dtp");
               await CheckDateTime(dtp_props, "*dtp", dt);

               // *dtpp
               var dtpp_props = await GetObjectOnLocals(locals, "dtpp");
               await CheckPointerValue(dtpp_props, "*dtpp", TPointer("System.DateTime*"), "locals");

               dtpp_props = await GetObjectOnLocals(dtpp_props, "*dtpp");
               await CheckDateTime(dtpp_props, "**dtpp", dt);

               // dtpa
               var dtpa_elems = (await CompareObjectPropertiesFor(locals, "dtpa", new[]
              {
                    TPointer("System.DateTime*"),
                        TPointer("System.DateTime*", is_null : true)
               }));
               {
                   var actual_elems = await CheckArrayElements(dtpa_elems, new[]
                   {
                        TDateTime(dt),
                        null
                   });

                   await CheckDateTime(actual_elems[0], "*[0]", dt);
               }

               // dtppa = new DateTime**[] { &dtp, &dtp_null, null };
               var dtppa_elems = (await CompareObjectPropertiesFor(locals, "dtppa", new[]
              {
                    TPointer("System.DateTime**"),
                        TPointer("System.DateTime**"),
                        TPointer("System.DateTime**", is_null : true)
               }));

               var exp_elems = new[]
               {
                    TPointer("System.DateTime*"),
                    TPointer("System.DateTime*", is_null : true),
                    null
               };

               await CheckArrayElements(dtppa_elems, exp_elems);
           });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("invoke_static_method ('[debugger-test] Math:UseComplex', 0, 0);", "Math", "UseComplex", 3, "UseComplex", false)]
        [InlineData("invoke_static_method ('[debugger-test] Math:UseComplex', 0, 0);", "Math", "UseComplex", 3, "UseComplex", true)]
        public async Task DerefNonPointerObject(string eval_fn, string type, string method, int line_offset, string bp_function_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            type, method, line_offset, bp_function_name,
            "window.setTimeout(function() { " + eval_fn + " })",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {

               // this will generate the object ids
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
               var complex = GetAndAssertObjectWithName(locals, "complex");

               // try to deref the non-pointer object, as a pointer
               await GetProperties(complex["value"]["objectId"].Value<string>().Replace(":object:", ":pointer:"), expect_ok: false);

               // try to deref an invalid pointer id
               await GetProperties("dotnet:pointer:123897", expect_ok: false);
           });

        async Task<JToken[]> CheckArrayElements(JToken array, JToken[] exp_elems)
        {
            var actual_elems = new JToken[exp_elems.Length];
            for (int i = 0; i < exp_elems.Length; i++)
            {
                if (exp_elems[i] != null)
                {
                    actual_elems[i] = await GetObjectOnLocals(array, i.ToString());
                    await CheckPointerValue(actual_elems[i], $"*[{i}]", exp_elems[i], $"dtppa->");
                }
            }

            return actual_elems;
        }
    }
}
