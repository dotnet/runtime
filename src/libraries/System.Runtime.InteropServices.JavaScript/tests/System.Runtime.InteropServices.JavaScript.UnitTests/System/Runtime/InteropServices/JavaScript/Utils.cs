// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public static unsafe partial class Utils
    {
        // replaces legacy Runtime.InvokeJS
        [JSImport("globalThis.App.invoke_js")]
        public static partial string InvokeJS(string code);

        [JSImport("INTERNAL.set_property")]
        public static partial void SetProperty(JSObject self, string propertyName,
            [JSMarshalAs<JSType.Function<JSType.Object>>] Action<JSObject> value);

        [JSImport("INTERNAL.get_property")]
        [return: JSMarshalAs<JSType.Function<JSType.Object>>]
        public static partial Action<JSObject> GetActionOfJSObjectProperty(JSObject self, string propertyName);

        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function>]
        public static partial Action CreateAction([JSMarshalAs<JSType.String>] string code);

        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Boolean>>]
        public static partial Func<bool> CreateFunctionBool([JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number>>]
        public static partial Func<int> CreateFunctionInt([JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number>>]
        public static partial Func<long> CreateFunctionLong([JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number>>]
        public static partial Func<double> CreateFunctionDouble([JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.String>>]
        public static partial Func<string> CreateFunctionString([JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Object>>]
        public static partial Func<JSObject> CreateFunctionJSObject([JSMarshalAs<JSType.String>] string code);

        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Boolean>>]
        public static partial Action<bool> CreateActionBool([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number>>]
        public static partial Action<int> CreateActionInt([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number>>]
        public static partial Action<long> CreateActionLong([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number>>]
        public static partial Action<double> CreateActionDouble([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.String>>]
        public static partial Action<string> CreateActionString([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Object>>]
        public static partial Action<JSObject> CreateActionJSObject([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);

        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Boolean, JSType.Boolean>>]
        public static partial Func<bool, bool> CreateFunctionBoolBool([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        public static partial Func<int, int> CreateFunctionIntInt([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        public static partial Func<long, long> CreateFunctionLongLong([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        public static partial Func<double, double> CreateFunctionDoubleDouble([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.String, JSType.String>>]
        public static partial Func<string, string> CreateFunctionStringString([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Object, JSType.Object>>]
        public static partial Func<JSObject, JSObject> CreateFunctionJSObjectJSObject([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);

        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Boolean, JSType.Object>>]
        public static partial Func<bool, JSObject> CreateFunctionBoolJSObject([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Object>>]
        public static partial Func<int, JSObject> CreateFunctionIntJSObject([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Object>>]
        public static partial Func<long, JSObject> CreateFunctionLongJSObject([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Object>>]
        public static partial Func<double, JSObject> CreateFunctionDoubleJSObject([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.String, JSType.Object>>]
        public static partial Func<string, JSObject> CreateFunctionStringJSObject([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);

        /* TODO
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs(JSType.Function, JSType.Boolean, JSType.Promise)]
        public static partial Func<bool, Task<object>> CreateFunctionBoolTask([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs(JSType.Function, JSType.Number, JSType.Promise)]
        public static partial Func<int, Task<object>> CreateFunctionIntTask([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs(JSType.Function, JSType.Number, JSType.Promise)]
        public static partial Func<long, Task<object>> CreateFunctionLongTask([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs(JSType.Function, JSType.Number, JSType.Promise)]
        public static partial Func<double, Task<object>> CreateFunctionDoubleJSTask([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs(JSType.Function, JSType.String, JSType.Promise)]
        public static partial Func<string, Task<object>> CreateFunctionStringTask([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string code);
        [return: JSMarshalAs(JSType.Function, JSType.Promise)]
        public static partial Func<Task<object>> CreateFunctionTask([JSMarshalAs<JSType.String>] string code);
        */

        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        public static partial Action<int, int> CreateActionIntInt([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string arg2Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        public static partial Action<long, long> CreateActionLongLong([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string arg2Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        public static partial Action<double, double> CreateActionDoubleDouble([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string arg2Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.String, JSType.String>>]
        public static partial Action<string, string> CreateActionStringString([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string arg2Name, [JSMarshalAs<JSType.String>] string code);

        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]
        public static partial Func<int, int, int> CreateFunctionIntIntInt([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string arg2Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]
        public static partial Func<long, long, long> CreateFunctionLongLongLong([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string arg2Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]
        public static partial Func<double, double, double> CreateFunctionDoubleDoubleDouble([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string arg2Name, [JSMarshalAs<JSType.String>] string code);
        [JSImport("globalThis.App.create_function")]
        [return: JSMarshalAs<JSType.Function<JSType.String, JSType.String, JSType.String>>]
        public static partial Func<string, string, string> CreateFunctionStringStringString([JSMarshalAs<JSType.String>] string arg1Name, [JSMarshalAs<JSType.String>] string arg2Name, [JSMarshalAs<JSType.String>] string code);
    }
}
