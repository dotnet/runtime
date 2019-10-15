// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoAsync
    {
        [Fact]
        public void TestCurrentCulturesAsync()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo currentCulture = CultureInfo.CurrentCulture;
                CultureInfo currentUICulture = CultureInfo.CurrentUICulture;

                CultureInfo newCurrentCulture = new CultureInfo(currentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");
                CultureInfo newCurrentUICulture = new CultureInfo(currentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");

                CultureInfo.CurrentCulture = newCurrentCulture;
                CultureInfo.CurrentUICulture = newCurrentUICulture;

                try
                {
                    Task t = Task.Run(() =>
                    {
                        Assert.Equal(CultureInfo.CurrentCulture, newCurrentCulture);
                        Assert.Equal(CultureInfo.CurrentUICulture, newCurrentUICulture);
                    });

                    ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
                    t.Wait();
                }
                finally
                {
                    CultureInfo.CurrentCulture = currentCulture;
                    CultureInfo.CurrentUICulture = currentUICulture;
                }
            }).Dispose();
        }

        [Fact]
        public void TestCurrentCulturesWithAwait()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo currentCulture = CultureInfo.CurrentCulture;
                CultureInfo currentUICulture = CultureInfo.CurrentUICulture;

                CultureInfo newCurrentCulture = new CultureInfo(currentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");
                CultureInfo newCurrentUICulture = new CultureInfo(currentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");

                CultureInfo.CurrentCulture = newCurrentCulture;
                CultureInfo.CurrentUICulture = newCurrentUICulture;

                async Task MainAsync()
                {
                    await Task.Delay(1).ConfigureAwait(false);

                    Assert.Equal(CultureInfo.CurrentCulture, newCurrentCulture);
                    Assert.Equal(CultureInfo.CurrentUICulture, newCurrentUICulture);
                }

                try
                {
                    MainAsync().Wait();
                }
                finally
                {
                    CultureInfo.CurrentCulture = currentCulture;
                    CultureInfo.CurrentUICulture = currentUICulture;
                }
            }).Dispose();
        }
    }
}
