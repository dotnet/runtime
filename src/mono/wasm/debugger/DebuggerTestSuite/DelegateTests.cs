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

    public class DelegateTests : DebuggerTests
    {

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(0, 53, 8, "DelegatesTest", false)]
        [InlineData(0, 53, 8, "DelegatesTest", true)]
        [InlineData(2, 99, 8, "InnerMethod2", false)]
        [InlineData(2, 99, 8, "InnerMethod2", true)]
        public async Task InspectLocalsWithDelegatesAtBreakpointSite(int frame, int line, int col, string method_name, bool use_cfo) =>
            await CheckInspectLocalsAtBreakpointSite(
                "dotnet://debugger-test.dll/debugger-test.cs", line, col, method_name,
                "window.setTimeout(function() { invoke_delegates_test (); }, 1);",
                use_cfo: use_cfo,
                wait_for_event_fn: async (pause_location) =>
               {
                   var locals = await GetProperties(pause_location["callFrames"][frame]["callFrameId"].Value<string>());

                   await CheckProps(locals, new
                   {
                       fn_func = TDelegate("System.Func<Math, bool>", "bool <DelegatesTest>|(Math)"),
                       fn_func_null = TObject("System.Func<Math, bool>", is_null: true),
                       fn_func_arr = TArray("System.Func<Math, bool>[]", "System.Func<Math, bool>[1]"),
                       fn_del = TDelegate("Math.IsMathNull", "bool IsMathNullDelegateTarget (Math)"),
                       fn_del_null = TObject("Math.IsMathNull", is_null: true),
                       fn_del_arr = TArray("Math.IsMathNull[]", "Math.IsMathNull[1]"),

                       // Unused locals
                       fn_func_unused = TDelegate("System.Func<Math, bool>", "bool <DelegatesTest>|(Math)"),
                       fn_func_null_unused = TObject("System.Func<Math, bool>", is_null: true),
                       fn_func_arr_unused = TArray("System.Func<Math, bool>[]", "System.Func<Math, bool>[1]"),

                       fn_del_unused = TDelegate("Math.IsMathNull", "bool IsMathNullDelegateTarget (Math)"),
                       fn_del_null_unused = TObject("Math.IsMathNull", is_null: true),
                       fn_del_arr_unused = TArray("Math.IsMathNull[]", "Math.IsMathNull[1]"),

                       res = TBool(false),
                       m_obj = TObject("Math")
                   }, "locals");

                   await CompareObjectPropertiesFor(locals, "fn_func_arr", new[]
                   {
                        TDelegate(
                            "System.Func<Math, bool>",
                            "bool <DelegatesTest>|(Math)")
                   }, "locals#fn_func_arr");

                   await CompareObjectPropertiesFor(locals, "fn_del_arr", new[]
                   {
                        TDelegate(
                            "Math.IsMathNull",
                            "bool IsMathNullDelegateTarget (Math)")
                   }, "locals#fn_del_arr");

                   await CompareObjectPropertiesFor(locals, "fn_func_arr_unused", new[]
                   {
                        TDelegate(
                            "System.Func<Math, bool>",
                            "bool <DelegatesTest>|(Math)")
                   }, "locals#fn_func_arr_unused");

                   await CompareObjectPropertiesFor(locals, "fn_del_arr_unused", new[]
                   {
                        TDelegate(
                            "Math.IsMathNull",
                            "bool IsMathNullDelegateTarget (Math)")
                   }, "locals#fn_del_arr_unused");
               }
            );

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(0, 202, 8, "DelegatesSignatureTest", false)]
        [InlineData(0, 202, 8, "DelegatesSignatureTest", true)]
        [InlineData(2, 99, 8, "InnerMethod2", false)]
        [InlineData(2, 99, 8, "InnerMethod2", true)]
        public async Task InspectDelegateSignaturesWithFunc(int frame, int line, int col, string bp_method, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-test.cs",
            line, col,
            bp_method,
            "window.setTimeout (function () { invoke_static_method ('[debugger-test] Math:DelegatesSignatureTest'); }, 1)",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][frame]["callFrameId"].Value<string>());

               await CheckProps(locals, new
               {
                   fn_func = TDelegate("System.Func<Math, Math.GenericStruct<Math.GenericStruct<int[]>>, Math.GenericStruct<bool[]>>",
                           "Math.GenericStruct<bool[]> <DelegatesSignatureTest>|(Math,Math.GenericStruct<Math.GenericStruct<int[]>>)"),

                   fn_func_del = TDelegate("System.Func<Math, Math.GenericStruct<Math.GenericStruct<int[]>>, Math.GenericStruct<bool[]>>",
                           "Math.GenericStruct<bool[]> DelegateTargetForSignatureTest (Math,Math.GenericStruct<Math.GenericStruct<int[]>>)"),

                   fn_func_null = TObject("System.Func<Math, Math.GenericStruct<Math.GenericStruct<int[]>>, Math.GenericStruct<bool[]>>", is_null: true),
                   fn_func_only_ret = TDelegate("System.Func<bool>", "bool <DelegatesSignatureTest>|()"),
                   fn_func_arr = TArray("System.Func<Math, Math.GenericStruct<Math.GenericStruct<int[]>>, Math.GenericStruct<bool[]>>[]", "System.Func<Math, Math.GenericStruct<Math.GenericStruct<int[]>>, Math.GenericStruct<bool[]>>[1]"),

                   fn_del = TDelegate("Math.DelegateForSignatureTest",
                           "Math.GenericStruct<bool[]> DelegateTargetForSignatureTest (Math,Math.GenericStruct<Math.GenericStruct<int[]>>)"),

                   fn_del_l = TDelegate("Math.DelegateForSignatureTest",
                           "Math.GenericStruct<bool[]> <DelegatesSignatureTest>|(Math,Math.GenericStruct<Math.GenericStruct<int[]>>)"),

                   fn_del_null = TObject("Math.DelegateForSignatureTest", is_null: true),
                   fn_del_arr = TArray("Math.DelegateForSignatureTest[]", "Math.DelegateForSignatureTest[2]"),
                   m_obj = TObject("Math"),
                   gs_gs = TValueType("Math.GenericStruct<Math.GenericStruct<int[]>>"),
                   fn_void_del = TDelegate("Math.DelegateWithVoidReturn",
                           "void DelegateTargetWithVoidReturn (Math.GenericStruct<int[]>)"),

                   fn_void_del_arr = TArray("Math.DelegateWithVoidReturn[]", "Math.DelegateWithVoidReturn[1]"),
                   fn_void_del_null = TObject("Math.DelegateWithVoidReturn", is_null: true),
                   gs = TValueType("Math.GenericStruct<int[]>"),
                   rets = TArray("Math.GenericStruct<bool[]>[]", "Math.GenericStruct<bool[]>[6]")
               }, "locals");

               await CompareObjectPropertiesFor(locals, "fn_func_arr", new[]
               {
                    TDelegate(
                        "System.Func<Math, Math.GenericStruct<Math.GenericStruct<int[]>>, Math.GenericStruct<bool[]>>",
                        "Math.GenericStruct<bool[]> <DelegatesSignatureTest>|(Math,Math.GenericStruct<Math.GenericStruct<int[]>>)"),
               }, "locals#fn_func_arr");

               await CompareObjectPropertiesFor(locals, "fn_del_arr", new[]
               {
                    TDelegate(
                            "Math.DelegateForSignatureTest",
                            "Math.GenericStruct<bool[]> DelegateTargetForSignatureTest (Math,Math.GenericStruct<Math.GenericStruct<int[]>>)"),
                        TDelegate(
                            "Math.DelegateForSignatureTest",
                            "Math.GenericStruct<bool[]> <DelegatesSignatureTest>|(Math,Math.GenericStruct<Math.GenericStruct<int[]>>)")
               }, "locals#fn_del_arr");

               await CompareObjectPropertiesFor(locals, "fn_void_del_arr", new[]
               {
                    TDelegate(
                        "Math.DelegateWithVoidReturn",
                        "void DelegateTargetWithVoidReturn (Math.GenericStruct<int[]>)")
               }, "locals#fn_void_del_arr");
           });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(0, 224, 8, "ActionTSignatureTest", false)]
        [InlineData(0, 224, 8, "ActionTSignatureTest", true)]
        [InlineData(2, 99, 8, "InnerMethod2", false)]
        [InlineData(2, 99, 8, "InnerMethod2", true)]
        public async Task ActionTSignatureTest(int frame, int line, int col, string bp_method, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-test.cs", line, col,
            bp_method,
            "window.setTimeout (function () { invoke_static_method ('[debugger-test] Math:ActionTSignatureTest'); }, 1)",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][frame]["callFrameId"].Value<string>());

               await CheckProps(locals, new
               {
                   fn_action = TDelegate("System.Action<Math.GenericStruct<int[]>>",
                           "void <ActionTSignatureTest>|(Math.GenericStruct<int[]>)"),
                   fn_action_del = TDelegate("System.Action<Math.GenericStruct<int[]>>",
                           "void DelegateTargetWithVoidReturn (Math.GenericStruct<int[]>)"),
                   fn_action_bare = TDelegate("System.Action",
                           "void|()"),

                   fn_action_null = TObject("System.Action<Math.GenericStruct<int[]>>", is_null: true),

                   fn_action_arr = TArray("System.Action<Math.GenericStruct<int[]>>[]", "System.Action<Math.GenericStruct<int[]>>[3]"),

                   gs = TValueType("Math.GenericStruct<int[]>"),
               }, "locals");

               await CompareObjectPropertiesFor(locals, "fn_action_arr", new[]
               {
                    TDelegate(
                            "System.Action<Math.GenericStruct<int[]>>",
                            "void <ActionTSignatureTest>|(Math.GenericStruct<int[]>)"),
                        TDelegate(
                            "System.Action<Math.GenericStruct<int[]>>",
                            "void DelegateTargetWithVoidReturn (Math.GenericStruct<int[]>)"),
                        TObject("System.Action<Math.GenericStruct<int[]>>", is_null : true)
               }, "locals#fn_action_arr");
           });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(0, 242, 8, "NestedDelegatesTest", false)]
        [InlineData(0, 242, 8, "NestedDelegatesTest", true)]
        [InlineData(2, 99, 8, "InnerMethod2", false)]
        [InlineData(2, 99, 8, "InnerMethod2", true)]
        public async Task NestedDelegatesTest(int frame, int line, int col, string bp_method, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-test.cs", line, col,
            bp_method,
            "window.setTimeout (function () { invoke_static_method ('[debugger-test] Math:NestedDelegatesTest'); }, 1)",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][frame]["callFrameId"].Value<string>());

               await CheckProps(locals, new
               {
                   fn_func = TDelegate("System.Func<System.Func<int, bool>, bool>",
                           "bool <NestedDelegatesTest>|(Func<int, bool>)"),
                   fn_func_null = TObject("System.Func<System.Func<int, bool>, bool>", is_null: true),
                   fn_func_arr = TArray("System.Func<System.Func<int, bool>, bool>[]", "System.Func<System.Func<int, bool>, bool>[1]"),
                   fn_del_arr = TArray("System.Func<System.Func<int, bool>, bool>[]", "System.Func<System.Func<int, bool>, bool>[1]"),

                   m_obj = TObject("Math"),
                   fn_del_null = TObject("System.Func<System.Func<int, bool>, bool>", is_null: true),
                   fs = TDelegate("System.Func<int, bool>",
                           "bool <NestedDelegatesTest>|(int)")
               }, "locals");

               await CompareObjectPropertiesFor(locals, "fn_func_arr", new[]
               {
                    TDelegate(
                        "System.Func<System.Func<int, bool>, bool>",
                        "bool <NestedDelegatesTest>|(System.Func<int, bool>)")
               }, "locals#fn_func_arr");

               await CompareObjectPropertiesFor(locals, "fn_del_arr", new[]
               {
                    TDelegate(
                        "System.Func<System.Func<int, bool>, bool>",
                        "bool DelegateTargetForNestedFunc (Func<int, bool>)")
               }, "locals#fn_del_arr");
           });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(0, 262, 8, "MethodWithDelegateArgs", false)]
        [InlineData(0, 262, 8, "MethodWithDelegateArgs", true)]
        [InlineData(2, 99, 8, "InnerMethod2", false)]
        [InlineData(2, 99, 8, "InnerMethod2", true)]
        public async Task DelegatesAsMethodArgsTest(int frame, int line, int col, string bp_method, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-test.cs", line, col,
            bp_method,
            "window.setTimeout (function () { invoke_static_method ('[debugger-test] Math:DelegatesAsMethodArgsTest'); }, 1)",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][frame]["callFrameId"].Value<string>());

               await CheckProps(locals, new
               {
                   @this = TObject("Math"),
                   dst_arr = TArray("Math.DelegateForSignatureTest[]", "Math.DelegateForSignatureTest[2]"),
                   fn_func = TDelegate("System.Func<char[], bool>",
                           "bool <DelegatesAsMethodArgsTest>|(char[])"),
                   fn_action = TDelegate("System.Action<Math.GenericStruct<int>[]>",
                           "void <DelegatesAsMethodArgsTest>|(Math.GenericStruct<int>[])")
               }, "locals");

               await CompareObjectPropertiesFor(locals, "dst_arr", new[]
               {
                    TDelegate("Math.DelegateForSignatureTest",
                            "Math.GenericStruct<bool[]> DelegateTargetForSignatureTest (Math,Math.GenericStruct<Math.GenericStruct<int[]>>)"),
                        TDelegate("Math.DelegateForSignatureTest",
                            "Math.GenericStruct<bool[]> <DelegatesAsMethodArgsTest>|(Math,Math.GenericStruct<Math.GenericStruct<int[]>>)"),
               }, "locals#dst_arr");
           });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MethodWithDelegatesAsyncTest(bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-test.cs", 281, 8,
            "MoveNext", //"DelegatesAsMethodArgsTestAsync"
            "window.setTimeout (function () { invoke_static_method_async ('[debugger-test] Math:MethodWithDelegatesAsyncTest'); }, 1)",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               await CheckProps(locals, new
               {
                   @this = TObject("Math"),
                   _dst_arr = TArray("Math.DelegateForSignatureTest[]", "Math.DelegateForSignatureTest[2]"),
                   _fn_func = TDelegate("System.Func<char[], bool>",
                           "bool <MethodWithDelegatesAsync>|(char[])"),
                   _fn_action = TDelegate("System.Action<Math.GenericStruct<int>[]>",
                           "void <MethodWithDelegatesAsync>|(Math.GenericStruct<int>[])")
               }, "locals");

               await CompareObjectPropertiesFor(locals, "_dst_arr", new[]
               {
                    TDelegate(
                            "Math.DelegateForSignatureTest",
                            "Math.GenericStruct<bool[]> DelegateTargetForSignatureTest (Math,Math.GenericStruct<Math.GenericStruct<int[]>>)"),
                        TDelegate(
                            "Math.DelegateForSignatureTest",
                            "Math.GenericStruct<bool[]> <MethodWithDelegatesAsync>|(Math,Math.GenericStruct<Math.GenericStruct<int[]>>)"),
               }, "locals#dst_arr");
           });
    }

}
