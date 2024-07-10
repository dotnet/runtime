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
        private const string ActivitySourceName = "Experimental.System.Net.NameResolution";
        private const string ActivityName = ActivitySourceName + ".DnsLookup";

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ForwardLookup_ValidHostName_ActivityRecorded(bool createParentActivity)
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
                    VerifyForwardActivityInfo(activity, ValidHostName);
                    ActivityAssert.HasTag(activity, "dns.answers", (string[] answers) => answers.Contains(expected4) || answers.Contains(expected6));
                    ActivityAssert.HasNoTag(activity, "error.type");
                }

            }, createParentActivity.ToString()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReverseLookup_ValidIP_ActivityRecorded(bool createParentActivity)
        {
            await RemoteExecutor.Invoke(static async (createParentActivity) =>
            {
                string loopbackIPString = IPAddress.Loopback.ToString();
                using var recorder = new ActivityRecorder(ActivitySourceName, ActivityName)
                {
                    ExpectedParent = bool.Parse(createParentActivity) ? new Activity("parent").Start() : null
                };

                IPHostEntry entry = await Dns.GetHostEntryAsync(IPAddress.Loopback); // Also does a forward lookup
                Verify(2);

                await Dns.GetHostEntryAsync(loopbackIPString);
                Verify(4);

                Dns.GetHostEntry(IPAddress.Loopback);
                Verify(6);

                Dns.GetHostEntry(loopbackIPString);
                Verify(8);

                Dns.EndGetHostEntry(Dns.BeginGetHostEntry(IPAddress.Loopback, null, null));
                Verify(10);

                Dns.EndGetHostEntry(Dns.BeginGetHostEntry(loopbackIPString, null, null));
                Verify(12);

                void Verify(int timesLookupRecorded)
                {
                    recorder.VerifyActivityRecorded(timesLookupRecorded);
                    Activity reverseActivity = recorder.FinishedActivities.ToArray()[^2];
                    VerifyReverseActivityInfo(reverseActivity, IPAddress.Loopback);
                    ActivityAssert.HasTag(reverseActivity, "dns.answers", (string[] answers) => answers.Contains(entry.HostName));
                    ActivityAssert.HasNoTag(reverseActivity, "error.type");
                    VerifyForwardActivityInfo(recorder.LastStartedActivity, entry.HostName);
                }

            }, createParentActivity.ToString()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task ForwardLookup_InvalidHostName_ActivityRecorded(bool createParentActivity)
        {
            const string InvalidHostName = $"invalid...example.com...{nameof(ForwardLookup_InvalidHostName_ActivityRecorded)}";

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
                    VerifyForwardActivityInfo(activity, InvalidHostName);
                    ActivityAssert.HasTag(activity, "error.type", "host_not_found");
                }
            }, createParentActivity.ToString()).DisposeAsync();
        }

        static void VerifyForwardActivityInfo(Activity activity, string question)
        {
            Assert.Equal(ActivityKind.Client, activity.Kind);
            Assert.Equal(ActivityName, activity.OperationName);
            Assert.Equal($"DNS lookup {question}", activity.DisplayName);
            ActivityAssert.HasTag(activity, "dns.question.name", question);
        }

        static void VerifyReverseActivityInfo(Activity activity, IPAddress question)
        {
            Assert.Equal(ActivityKind.Client, activity.Kind);
            Assert.Equal(ActivityName, activity.OperationName);
            Assert.Equal($"DNS reverse lookup {question}", activity.DisplayName);
            ActivityAssert.HasTag(activity, "dns.question.name", question.ToString());
        }
    }
}
