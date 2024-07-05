// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class ActivityTest
    {
        private const string ActivitySourceName = "System.Net.NameResolution";
        private const string ActivityName = ActivitySourceName + ".DnsLookup";

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveValidHostName_ActivityRecorded(bool createParentActivity)
        {
            await RemoteExecutor.Invoke(static async (createParentActivity) =>
            {
                const string ValidHostName = "localhost";
                using var recorder = new ActivityRecorder(ActivitySourceName, ActivityName)
                {
                    ExpectedParent = bool.Parse(createParentActivity) ? new Activity("parent").Start() : null
                };

                string expected4 = IPAddress.Loopback.ToString();
                string expected6 = IPAddress.IPv6Loopback.ToString();
                
                await Dns.GetHostEntryAsync(ValidHostName);
                Verify(1);

                await Dns.GetHostAddressesAsync(ValidHostName);
                Verify(2);

                Dns.GetHostEntry(ValidHostName);
                Verify(3);

                Dns.GetHostAddresses(ValidHostName);
                Verify(4);

                Dns.EndGetHostEntry(Dns.BeginGetHostEntry(ValidHostName, null, null));
                Verify(5);

                Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses(ValidHostName, null, null));
                Verify(6);

                void Verify(int timesLookupRecorded)
                {
                    recorder.VerifyActivityRecorded(timesLookupRecorded);
                    Activity activity = recorder.LastFinishedActivity;
                    VerifyCommonActivityInfo(activity, ValidHostName);
                    ActivityAssert.HasTag(activity, "dns.answer", (string[] answers) => answers.Contains(expected4) || answers.Contains(expected6));
                    ActivityAssert.HasNoTag(activity, "error.type");
                }

            }, createParentActivity.ToString()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task ResolveInvalidHostName_ActivityRecorded(bool createParentActivity)
        {
            const string InvalidHostName = $"invalid...example.com...{nameof(ResolveInvalidHostName_ActivityRecorded)}";

            await RemoteExecutor.Invoke(async (createParentActivity) =>
            {
                using var recorder = new ActivityRecorder(ActivitySourceName, ActivityName)
                {
                    ExpectedParent = bool.Parse(createParentActivity) ? new Activity("parent").Start() : null
                };

                await Assert.ThrowsAnyAsync<SocketException>(async () => await Dns.GetHostEntryAsync(InvalidHostName));
                Verify(1);

                await Assert.ThrowsAnyAsync<SocketException>(async () => await Dns.GetHostAddressesAsync(InvalidHostName));
                Verify(2);

                Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry(InvalidHostName));
                Verify(3);

                Assert.ThrowsAny<SocketException>(() => Dns.GetHostAddresses(InvalidHostName));
                Verify(4);

                Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostEntry(Dns.BeginGetHostEntry(InvalidHostName, null, null)));
                Verify(5);

                Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses(InvalidHostName, null, null)));
                Verify(6);

                void Verify(int timesLookupRecorded)
                {
                    recorder.VerifyActivityRecorded(timesLookupRecorded);

                    Activity activity = recorder.LastFinishedActivity;
                    Assert.Equal(ActivityStatusCode.Error, activity.Status);
                    VerifyCommonActivityInfo(activity, InvalidHostName);
                    ActivityAssert.HasTag(activity, "error.type", "host_not_found");
                }
            }, createParentActivity.ToString()).DisposeAsync();
        }

        static void VerifyCommonActivityInfo(Activity activity, string host)
        {
            Assert.Equal(ActivityKind.Client, activity.Kind);
            Assert.Equal("System.Net.NameResolution.DnsLookup", activity.OperationName);
            Assert.Equal($"DNS {host}", activity.DisplayName);
            ActivityAssert.HasTag(activity, "dns.question.name", host);
        }
    }
}
