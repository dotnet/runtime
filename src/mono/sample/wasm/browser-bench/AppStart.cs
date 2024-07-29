// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sample
{
    // http://localhost:8000/?task=AppStart
    public partial class AppStartTask : BenchTask
    {
        public override string Name => "AppStart";
        public override bool BrowserOnly => true;

        public AppStartTask()
        {
            measurements = new Measurement[] {
                new PageShow(),
                new ReachManaged(),
                new ReachManagedCold(),
                new BlazorPageShow(),
                new BlazorReachManaged(),
                new BlazorFirstUI(),
                new BlazorReachManagedCold(),
                new BrowserPageShow(),
                new BrowserReachManaged(),
                new BrowserReachManagedCold(),
            };
        }

        Measurement[] measurements;
        public override Measurement[] Measurements => measurements;

        class PageShow : BenchTask.Measurement
        {
            public override string Name => "Page show";
            public override int InitialSamples => 3;
            public override bool HasRunStepAsync => true;

            public override async Task RunStepAsync()
            {
                await MainApp.PageShow(null, null);
            }
        }

        class ReachManaged : BenchTask.Measurement
        {
            public override string Name => "Reach managed";
            public override int InitialSamples => 3;
            public override bool HasRunStepAsync => true;

            public override async Task RunStepAsync()
            {
                await MainApp.FrameReachedManaged(null, null);
            }
        }

        class ReachManagedCold : BenchTask.Measurement
        {
            public override string Name => "Reach managed cold";
            public override int InitialSamples => 1;
            public override int RunLength => 20000;
            public override bool HasRunStepAsync => true;

            public override async Task RunStepAsync()
            {
                await MainApp.FrameReachedManaged(Guid.NewGuid().ToString(), null);
            }
        }

        abstract class BlazorAppStartMeasurement : BenchTask.Measurement
        {
            protected readonly string urlBase = "blazor-template/";
            protected virtual string FramePage => "";

            public override async Task<bool> IsEnabled()
            {
                using var client = new HttpClient();
                try
                {
                    var url = $"{MainApp.Origin()}/{urlBase}{FramePage}";
                    await client.GetStringAsync(url);
                }
                catch
                {
                    return false;
                }

                return true;
            }

            public override Task BeforeBatch()
            {
                MainApp.SetFramePage(FramePage);

                return Task.CompletedTask;
            }
        }

        class BlazorPageShow : BlazorAppStartMeasurement
        {
            public override string Name => "Blazor Page show";
            public override int InitialSamples => 3;
            public override bool HasRunStepAsync => true;

            public override async Task RunStepAsync()
            {
                await MainApp.PageShow(null, urlBase);
            }
        }

        class BlazorReachManaged : BlazorAppStartMeasurement
        {
            public override string Name => "Blazor Reach managed";
            public override int InitialSamples => 3;
            public override bool HasRunStepAsync => true;

            public override async Task RunStepAsync()
            {
                await MainApp.FrameReachedManaged(null, urlBase);
            }
        }

        class BlazorFirstUI : BlazorAppStartMeasurement
        {
            public override string Name => "Blazor First UI";
            public override int InitialSamples => 3;
            public override bool HasRunStepAsync => true;

            public override async Task RunStepAsync()
            {
                await MainApp.FrameBlazorFirstUI(null, urlBase);
            }
        }

        class BlazorReachManagedCold : BlazorAppStartMeasurement
        {
            public override string Name => "Blazor Reach managed cold";
            public override int InitialSamples => 1;
            public override int RunLength => 20000;
            public override bool HasRunStepAsync => true;

            public override async Task RunStepAsync()
            {
                await MainApp.FrameReachedManaged(Guid.NewGuid().ToString(), urlBase);
            }
        }

        abstract class BrowserAppStartMeasurement : BenchTask.Measurement
        {
            protected readonly string urlBase = "browser-template/";
            protected virtual string FramePage => "";

            public override async Task<bool> IsEnabled()
            {
                using var client = new HttpClient();
                try
                {
                    var url = $"{MainApp.Origin()}/{urlBase}{FramePage}";
                    await client.GetStringAsync(url);
                }
                catch
                {
                    return false;
                }

                return true;
            }

            public override Task BeforeBatch()
            {
                MainApp.SetFramePage(FramePage);

                return Task.CompletedTask;
            }
        }

        class BrowserPageShow : BrowserAppStartMeasurement
        {
            public override string Name => "Browser Page show";
            public override int InitialSamples => 3;
            public override bool HasRunStepAsync => true;

            public override async Task RunStepAsync()
            {
                await MainApp.PageShow(null, urlBase);
            }
        }

        class BrowserReachManaged : BrowserAppStartMeasurement
        {
            public override string Name => "Browser Reach managed";
            public override int InitialSamples => 3;
            public override bool HasRunStepAsync => true;

            public override async Task RunStepAsync()
            {
                await MainApp.FrameReachedManaged(null, urlBase);
            }
        }

        class BrowserReachManagedCold : BrowserAppStartMeasurement
        {
            public override string Name => "Browser Reach managed cold";
            public override int InitialSamples => 1;
            public override int RunLength => 20000;
            public override bool HasRunStepAsync => true;

            public override async Task RunStepAsync()
            {
                await MainApp.FrameReachedManaged(Guid.NewGuid().ToString(), urlBase);
            }
        }

        public partial class MainApp
        {
            [JSImport("globalThis.mainApp.FrameBlazorFirstUI")]
            public static partial Task FrameBlazorFirstUI(string guid, string urlBase);
            [JSImport("globalThis.mainApp.PageShow")]
            public static partial Task PageShow(string guid, string urlBase);
            [JSImport("globalThis.mainApp.FrameReachedManaged")]
            public static partial Task FrameReachedManaged(string guid, string urlBase);
            [JSImport("globalThis.mainApp.SetFramePage")]
            public static partial Task SetFramePage(string page);
            [JSImport("globalThis.mainApp.Origin")]
            public static partial string Origin();
        }

        public partial class FrameApp
        {
            [JSImport("globalThis.frameApp.ReachedCallback")]
            public static partial Task ReachedCallback();

            [JSExport]
            public static void ReachedManaged()
            {
                ReachedCallback();
            }
        }
    }
}
