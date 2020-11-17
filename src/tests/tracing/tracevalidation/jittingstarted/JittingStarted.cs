// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Tracing.Tests
{
    public static class TraceValidationJittingStarted
    {
        // Delegate declaration matching the signature of the dynamic method.
        private delegate void DynamicallyCompiledMethodInvoker();

        public static int Main(string[] args)
        {
            using (var netPerfFile = NetPerfFile.Create(args))
            {
                Console.WriteLine("\tStart: Enable tracing.");
                TraceControl.EnableDefault(netPerfFile.Path);
                Console.WriteLine("\tEnd: Enable tracing.\n");

                Console.WriteLine("\tStart: Generate some events.");
                DynamicallyCompiledMethodInvoker invoker = BuildDynamicMethod();
                invoker.Invoke();
                Console.WriteLine("\tEnd: Generate some events.\n");

                Console.WriteLine("\tStart: Disable tracing.");
                TraceControl.Disable();
                Console.WriteLine("\tEnd: Disable tracing.\n");

                Console.WriteLine("\tStart: Process the trace file.");

                int matchingEventCount = 0;
                int nonMatchingEventCount = 0;

                using (var trace = TraceEventDispatcher.GetDispatcherFromFileName(netPerfFile.Path))
                {
                    string methodNamespace = "dynamicClass";
                    string methodName = "DynamicallyCompiledMethod";
                    string methodSignature = "void  ()";
                    string providerName = "Microsoft-Windows-DotNETRuntime";
                    string gcTriggeredEventName = "Method/JittingStarted";

                    trace.Clr.MethodJittingStarted += delegate(MethodJittingStartedTraceData data)
                    {
                        if(methodNamespace.Equals(data.MethodNamespace) &&
                           methodName.Equals(data.MethodName) &&
                           methodSignature.Equals(data.MethodSignature) &&
                           providerName.Equals(data.ProviderName) &&
                           gcTriggeredEventName.Equals(data.EventName))
                        {
                            matchingEventCount++;
                        }
                    };

                    trace.Process();
                }
                Console.WriteLine("\tEnd: Processing events from file.\n");

                // CompiledMethod
                Assert.Equal(nameof(matchingEventCount), matchingEventCount, 1);
            }

            return 100;
        }

        private static DynamicallyCompiledMethodInvoker BuildDynamicMethod()
        {
            Type[] methodArgs = { };

            DynamicMethod dynamicMethod = new DynamicMethod(
                "DynamicallyCompiledMethod",
                typeof(void),
                methodArgs);

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ret);

            return (DynamicallyCompiledMethodInvoker)dynamicMethod.CreateDelegate(typeof(DynamicallyCompiledMethodInvoker));
        }
    }
}
