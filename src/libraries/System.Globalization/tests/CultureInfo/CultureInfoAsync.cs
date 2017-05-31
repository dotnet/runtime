// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoAsync
    {        
        [Fact]
        // async current cultures feature is supported on 4.6.1 and up on Windows desktop framework
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ActiveIssue("https://github.com/dotnet/corert/issues/3747 - Port async-aware CultureInfo property from CoreCLR", TargetFrameworkMonikers.UapAot)]
        public void TestCurrentCulturesAsync()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            CultureInfo currentUICulture = CultureInfo.CurrentUICulture;

            CultureInfo newCurrentCulture = new CultureInfo(currentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");
            CultureInfo newCurrentUICulture = new CultureInfo(currentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");
            
            CultureInfo.CurrentCulture = newCurrentCulture;
            CultureInfo.CurrentUICulture = newCurrentUICulture;
            
            try
            {
                Task t = Task.Run(() => {
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
        }
        
        [Fact]
        // async current cultures feature is supported on 4.6.1 and up on Windows desktop framework
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ActiveIssue("https://github.com/dotnet/corert/issues/3747 - Port async-aware CultureInfo property from CoreCLR", TargetFrameworkMonikers.UapAot)]
        public void TestCurrentCulturesWithAwait()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            CultureInfo currentUICulture = CultureInfo.CurrentUICulture;

            CultureInfo newCurrentCulture = new CultureInfo(currentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");
            CultureInfo newCurrentUICulture = new CultureInfo(currentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "en-US" : "ja-JP");
            
            CultureInfo.CurrentCulture = newCurrentCulture;
            CultureInfo.CurrentUICulture = newCurrentUICulture;
            
            Func<Task> mainAsync = async delegate 
            {
                await Task.Delay(1).ConfigureAwait(false);
                
                Assert.Equal(CultureInfo.CurrentCulture, newCurrentCulture);
                Assert.Equal(CultureInfo.CurrentUICulture, newCurrentUICulture);
            };
            
            try 
            {
                mainAsync().Wait();
            }
            finally
            {
                CultureInfo.CurrentCulture = currentCulture;
                CultureInfo.CurrentUICulture = currentUICulture;               
            }                
        }
    }
}
