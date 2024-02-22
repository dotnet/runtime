// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Tests;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoAsync
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void TestCurrentCulturesAsync()
        {
            var newCurrentCulture = new CultureInfo(CultureInfo.CurrentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");
            var newCurrentUICulture = new CultureInfo(CultureInfo.CurrentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");
            using (new ThreadCultureChange(newCurrentCulture, newCurrentUICulture))
            {
                Task t = Task.Run(() =>
                {
                    Assert.Equal(CultureInfo.CurrentCulture, newCurrentCulture);
                    Assert.Equal(CultureInfo.CurrentUICulture, newCurrentUICulture);
                });

                ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                t.Wait();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupportedOrBrowserBackgroundExec))]
        public void TestCurrentCulturesWithAwait()
        {
            var newCurrentCulture = new CultureInfo(CultureInfo.CurrentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");
            var newCurrentUICulture = new CultureInfo(CultureInfo.CurrentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");
            using (new ThreadCultureChange(newCurrentCulture, newCurrentUICulture))
            {
                MainAsync().Wait();

                async Task MainAsync()
                {
                    await Task.Delay(1).ConfigureAwait(false);

                    Assert.Equal(CultureInfo.CurrentCulture, newCurrentCulture);
                    Assert.Equal(CultureInfo.CurrentUICulture, newCurrentUICulture);
                }
            }
        }
    }
}
