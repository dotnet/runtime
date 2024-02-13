// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace Sample
{
    // http://localhost:8000/?task=JSInterop
    class JSInteropTask : BenchTask
    {
        public override string Name => "JSInterop";
        public override Measurement[] Measurements => measurements;
        public override bool BrowserOnly => false;

        Measurement[] measurements;
        public JSInteropTask()
        {
            measurements = new Measurement[] {
                new JSExportIntMeasurement(),
                new JSExportStringMeasurement(),
                new JSImportIntMeasurement(),
                new JSImportStringMeasurement(),
                new JSImportManyArgsMeasurement(),
                new JSImportTaskMeasurement(),
                new JSImportTaskFailMeasurement(),
                new JSImportFailMeasurement(),
            };
        }

        public class JSExportIntMeasurement : BenchTask.Measurement
        {
            public override int InitialSamples => 10;
            public override string Name => "JSExportInt";
            public override void RunStep()
            {
                ImportsExportsHelper.RunJSExportInt(10000);
            }
        }

        public class JSExportStringMeasurement : BenchTask.Measurement
        {
            public override int InitialSamples => 3;
            public override string Name => "JSExportString";
            public override void RunStep()
            {
                ImportsExportsHelper.RunJSExportString(10000);
            }
        }

        public class JSImportIntMeasurement : BenchTask.Measurement
        {
            public override int InitialSamples => 10;
            public override string Name => "JSImportInt";
            public override void RunStep()
            {
                for (var i = 0; i < 10000; i++)
                {
                    ImportsExportsHelper.ImportTargetInt(currentStep);
                }
            }
        }

        public class JSImportStringMeasurement : BenchTask.Measurement
        {
            public override int InitialSamples => 3;
            public override string Name => "JSImportString";
            public override void RunStep()
            {
                for (var i = 0; i < 10000; i++)
                {
                    ImportsExportsHelper.ImportTargetString(i + "A" + currentStep);
                }
            }
        }

        public class JSImportManyArgsMeasurement : BenchTask.Measurement
        {
            private DateTime start = DateTime.Now;
            public override int InitialSamples => 3;
            public override string Name => "JSImportManyArgs";
            public override void RunStep()
            {
                for (var i = 0; i < 10000; i++)
                {
                    ImportsExportsHelper.ImportTargetManyArgs(currentStep, currentStep + 1, "interned", i + "A" + currentStep, IntPtr.Zero, true, 1L, null, 3.14, start);
                }
            }
        }

        public class JSImportTaskMeasurement : BenchTask.Measurement
        {
            public override bool HasRunStepAsync => true;
            public override int InitialSamples => 1;
            public override string Name => "JSImportTask";
            public override async Task RunStepAsync()
            {
                for (var i = 0; i < 10000; i++)
                {
                    TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                    var promise = ImportsExportsHelper.ImportTargetTask(tcs.Task);
                    tcs.SetResult(currentStep);
                    await promise;
                }
            }
        }

        public class JSImportTaskFailMeasurement : BenchTask.Measurement
        {
            public override bool HasRunStepAsync => true;
            public override int InitialSamples => 1;
            public override string Name => "JSImportTaskFail";
            public override async Task RunStepAsync()
            {
                for (var i = 0; i < 10000; i++)
                {
                    TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                    var promise = ImportsExportsHelper.ImportTargetTask(tcs.Task);
                    tcs.SetException(new Exception("test"));
                    try
                    {
                        await promise;
                    }
                    catch (Exception)
                    {
                        // no action
                    }
                }
            }
        }

        public class JSImportFailMeasurement : BenchTask.Measurement
        {
            public override int InitialSamples => 1;
            public override string Name => "JSImportFail";
            public override void RunStep()
            {
                for (var i = 0; i < 10000; i++)
                {
                    try
                    {
                        ImportsExportsHelper.ImportTargetThrows(currentStep);
                    }
                    catch (Exception)
                    {
                        // no action
                    }
                }
            }
        }
    }

    partial class ImportsExportsHelper
    {
        [JSImport("Sample.Test.runJSExportInt", "main.js")]
        public static partial void RunJSExportInt(int count);

        [JSImport("Sample.Test.runJSExportString", "main.js")]
        public static partial void RunJSExportString(int count);

        [JSImport("Sample.Test.importTargetInt", "main.js")]
        public static partial int ImportTargetInt(int value);

        [JSImport("Sample.Test.importTargetString", "main.js")]
        public static partial string ImportTargetString(string value);

        [JSImport("Sample.Test.importTargetTask", "main.js")]
        public static partial Task<int> ImportTargetTask(Task<int> value);

        [JSImport("Sample.Test.importTargetThrows", "main.js")]
        public static partial void ImportTargetThrows(int value);

        [JSImport("Sample.Test.importTargetManyArgs", "main.js")]
        public static partial double ImportTargetManyArgs(int arg1, int arg2, string arg3, string arg4, IntPtr arg5, bool arg6,
            [JSMarshalAs<JSType.Number>] long arg7, int? arg8, double arg9,
            [JSMarshalAs<JSType.Date>] DateTime arg10);


        [JSExport]
        public static int JSExportTargetInt(int value)
        {
            return value + 1;
        }

        [JSExport]
        public static string JSExportTargetString(string value)
        {
            return value + "A";
        }
    }
}
