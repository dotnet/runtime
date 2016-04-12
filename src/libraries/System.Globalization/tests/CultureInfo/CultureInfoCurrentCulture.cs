// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Diagnostics;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoCurrentCultureTests : RemoteExecutorTestBase
    {
        [Fact]
        public void CurrentCulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo newCulture = new CultureInfo(CultureInfo.CurrentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
                CultureInfo.CurrentCulture = newCulture;

                Assert.Equal(CultureInfo.CurrentCulture, newCulture);

                newCulture = new CultureInfo("de-DE_phoneb");
                CultureInfo.CurrentCulture = newCulture;

                Assert.Equal(CultureInfo.CurrentCulture, newCulture);
                Assert.Equal("de-DE_phoneb", newCulture.CompareInfo.Name);

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void CurrentCulture_Set_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("value", () => CultureInfo.CurrentCulture = null);
        }

        [Fact]
        public void CurrentUICulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo newUICulture = new CultureInfo(CultureInfo.CurrentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
                CultureInfo.CurrentUICulture = newUICulture;

                Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);

                newUICulture = new CultureInfo("de-DE_phoneb");
                CultureInfo.CurrentUICulture = newUICulture;

                Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);
                Assert.Equal("de-DE_phoneb", newUICulture.CompareInfo.Name);
                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void DefaultThreadCurrentCulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo newCulture = new CultureInfo(CultureInfo.DefaultThreadCurrentCulture == null || CultureInfo.DefaultThreadCurrentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
                CultureInfo.DefaultThreadCurrentCulture = newCulture;

                Task task = Task.Run(() =>
                {
                    Assert.Equal(CultureInfo.CurrentCulture, newCulture);
                });
                ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
                task.Wait();

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void DefaultThreadCurrentUICulture()
        {
            RemoteInvoke(() =>
            {
                CultureInfo newUICulture = new CultureInfo(CultureInfo.DefaultThreadCurrentUICulture == null || CultureInfo.DefaultThreadCurrentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
                CultureInfo.DefaultThreadCurrentUICulture = newUICulture;

                Task task = Task.Run(() =>
                {
                    Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);
                });
                ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
                task.Wait();

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void CurrentUICulture_Set_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("value", () => CultureInfo.CurrentUICulture = null);
        }
    }
}
