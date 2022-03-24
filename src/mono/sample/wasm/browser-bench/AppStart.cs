// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sample
{
    public class AppStartTask : BenchTask
    {
        public override string Name => "AppStart";
        public override bool BrowserOnly => true;

        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, "System.Runtime.InteropServices.JavaScript.Runtime", "System.Private.Runtime.InteropServices.JavaScript")]
        static Type jsRuntimeType = System.Type.GetType("System.Runtime.InteropServices.JavaScript.Runtime, System.Private.Runtime.InteropServices.JavaScript", true);
        static Type jsFunctionType = System.Type.GetType("System.Runtime.InteropServices.JavaScript.Function, System.Private.Runtime.InteropServices.JavaScript", true);
        [DynamicDependency("InvokeJS(System.String)", "System.Runtime.InteropServices.JavaScript.Runtime", "System.Private.Runtime.InteropServices.JavaScript")]
        static MethodInfo invokeJSMethod = jsRuntimeType.GetMethod("InvokeJS", new Type[] { typeof(string) });
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, "System.Runtime.InteropServices.JavaScript.Function", "System.Private.Runtime.InteropServices.JavaScript")]
        static ConstructorInfo functionConstructor = jsRuntimeType.GetConstructor(new Type[] { typeof(object[]) });
        [DynamicDependency("Call()", "System.Runtime.InteropServices.JavaScript.Function", "System.Private.Runtime.InteropServices.JavaScript")]
        static MethodInfo functionCall = jsFunctionType.GetMethod("Call", BindingFlags.Instance | BindingFlags.Public, new Type[] { });

        public AppStartTask()
        {
            measurements = new Measurement[] {
                new PageShow(),
                new ReachManaged(),
            };
        }

        Measurement[] measurements;
        public override Measurement[] Measurements => measurements;

        static string InvokeJS(string js)
        {
            return (string)invokeJSMethod.Invoke(null, new object[] { js });
        }

        class PageShow : BenchTask.Measurement
        {
            public override string Name => "Page show";

            public override int InitialSamples => 3;

            public override bool HasRunStepAsync => true;

            public override async Task RunStepAsync()
            {
                var function = Activator.CreateInstance(jsFunctionType, new object[] { new object[] { @"return mainApp.PageShow();" } });
                await (Task<object>)functionCall.Invoke(function, null);
            }
        }

        class ReachManaged : BenchTask.Measurement
        {
            public override string Name => "Reach managed";
            public override int InitialSamples => 3;
            public override bool HasRunStepAsync => true;

            static object jsUIReachedManagedFunction = Activator.CreateInstance(jsFunctionType, new object[] { new object[] { @"return mainApp.ReachedManaged();" } });
            static object jsReached = Activator.CreateInstance(jsFunctionType, new object[] { new object[] { @"return frameApp.reached();" } });

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Reached()
            {
                functionCall.Invoke(jsReached, null);
            }

            public override async Task RunStepAsync()
            {
                await (Task<object>)functionCall.Invoke(jsUIReachedManagedFunction, null);
            }
        }
    }
}
