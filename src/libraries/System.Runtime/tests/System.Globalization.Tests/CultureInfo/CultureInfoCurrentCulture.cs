// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Tests;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Globalization.Tests
{
    public class CurrentCultureTests
    {
        [Fact]
        public void CurrentCulture()
        {
            var newCulture = new CultureInfo(CultureInfo.CurrentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
            using (new ThreadCultureChange(newCulture))
            {
                Assert.Equal(CultureInfo.CurrentCulture, newCulture);
            }

            if (PlatformDetection.IsNotBrowser)
            {
                newCulture = new CultureInfo("de-DE_phoneb");
                using (new ThreadCultureChange(newCulture))
                {
                    Assert.Equal(CultureInfo.CurrentCulture, newCulture);
                    Assert.Equal("de-DE_phoneb", newCulture.CompareInfo.Name);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]
        public void CurrentCulture_Default_Not_Invariant()
        {
            // On OSX-like platforms, it should default to what the default system culture is 
            // set to.  Since we shouldn't assume en-US, we just test if it's not the invariant
            // culture.
            Assert.NotEqual(CultureInfo.CurrentCulture, CultureInfo.InvariantCulture);
            Assert.NotEqual(CultureInfo.CurrentUICulture, CultureInfo.InvariantCulture);
        }

        [Fact]
        public void CurrentCulture_Set_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => CultureInfo.CurrentCulture = null);
        }

        [Fact]
        public void CurrentUICulture()
        {
            var newUICulture = new CultureInfo(CultureInfo.CurrentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
            using (new ThreadCultureChange(null, newUICulture))
            {
                Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);
            }

            if (PlatformDetection.IsNotBrowser)
            {
                newUICulture = new CultureInfo("de-DE_phoneb");
                using (new ThreadCultureChange(null, newUICulture))
                {
                    Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);
                    Assert.Equal("de-DE_phoneb", newUICulture.CompareInfo.Name);
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DefaultThreadCurrentCulture()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo newCulture = new CultureInfo(CultureInfo.DefaultThreadCurrentCulture == null || CultureInfo.DefaultThreadCurrentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
                CultureInfo.DefaultThreadCurrentCulture = newCulture;

                Task task = Task.Run(() =>
                {
                    Assert.Equal(CultureInfo.CurrentCulture, newCulture);
                });
                ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
                task.Wait();
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DefaultThreadCurrentUICulture()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo newUICulture = new CultureInfo(CultureInfo.DefaultThreadCurrentUICulture == null || CultureInfo.DefaultThreadCurrentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
                CultureInfo.DefaultThreadCurrentUICulture = newUICulture;

                Task task = Task.Run(() =>
                {
                    Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);
                });
                ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
                task.Wait();
            }).Dispose();
        }

        [Fact]
        public void CurrentUICulture_Set_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => CultureInfo.CurrentUICulture = null);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Windows locale support doesn't rely on LANG variable
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "Bionic is not normal Linux, has no normal locales")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("en-US.UTF-8", "en-US")]
        [InlineData("en-US", "en-US")]
        [InlineData("en_GB", "en-GB")]
        [InlineData("fr-FR", "fr-FR")]
        [InlineData("ru", "ru")]
        public void CurrentCulture_BasedOnLangEnvVar(string langEnvVar, string expectedCultureName)
        {
            var psi = new ProcessStartInfo();
            psi.Environment.Clear();

            CopyEssentialTestEnvironment(psi.Environment);

            psi.Environment["LANG"] = langEnvVar;

            RemoteExecutor.Invoke(expected =>
            {
                Assert.NotNull(CultureInfo.CurrentCulture);
                Assert.NotNull(CultureInfo.CurrentUICulture);

                Assert.Equal(expected, CultureInfo.CurrentCulture.Name);
                Assert.Equal(expected, CultureInfo.CurrentUICulture.Name);
            }, expectedCultureName, new RemoteInvokeOptions { StartInfo = psi }).Dispose();
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "Remote executor has problems with exit codes")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("")]
        [InlineData(null)]
        public void CurrentCulture_DefaultWithNoLang(string langEnvVar)
        {
            var psi = new ProcessStartInfo();
            psi.Environment.Clear();

            CopyEssentialTestEnvironment(psi.Environment);

            if (langEnvVar != null)
            {
               psi.Environment["LANG"] = langEnvVar;
            }

            // When LANG is empty or unset, on Unix it should default to the invariant culture.
            // On OSX-like platforms, it should default to what the default system culture is 
            // set to.  Since we shouldn't assume en-US, we just test if it's not the invariant
            // culture.
            RemoteExecutor.Invoke(() =>
            {
                Assert.NotNull(CultureInfo.CurrentCulture);
                Assert.NotNull(CultureInfo.CurrentUICulture);

                if (PlatformDetection.IsApplePlatform)
                {
                    Assert.NotEqual("", CultureInfo.CurrentCulture.Name);
                    Assert.NotEqual("", CultureInfo.CurrentUICulture.Name);

                    Assert.NotEqual(CultureInfo.CurrentCulture, CultureInfo.InvariantCulture);
                    Assert.NotEqual(CultureInfo.CurrentUICulture, CultureInfo.InvariantCulture);
                }
                else
                {
                    Assert.Equal("", CultureInfo.CurrentCulture.Name);
                    Assert.Equal("", CultureInfo.CurrentUICulture.Name);
                }
            }, new RemoteInvokeOptions { StartInfo = psi }).Dispose();
        }

        private static void CopyEssentialTestEnvironment(IDictionary<string, string> environment)
        {
            string[] essentialVariables = { "HOME", "LD_LIBRARY_PATH", "ICU_DATA" };
            string[] prefixedVariables = { "DOTNET_", "COMPlus_", "SuperPMIShim" };

            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                if (Array.FindIndex(essentialVariables, x => x.Equals(de.Key)) >= 0 ||
                    Array.FindIndex(prefixedVariables, x => ((string)de.Key).StartsWith(x, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    environment[(string)de.Key] = (string)de.Value;
                }
            }
        }
    }
}
