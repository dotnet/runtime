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
        private const string ActivityName = ActivitySourceName + ".DsnLookup";

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

                    KeyValuePair<string, object?>[] tags = recorder.LastFinishedActivity.TagObjects.ToArray();
                    Assert.Equal(ValidHostName, tags.Single(t => t.Key == "dns.question.name").Value);
                    string[] answers = Assert.IsType<string[]>(tags.Single(t => t.Key == "dns.answer").Value);
                    Assert.True(answers.Contains(expected4) || answers.Contains(expected6));
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

                    Assert.Equal(ActivityStatusCode.Error, recorder.LastFinishedActivity.Status);

                    KeyValuePair<string, object?>[] tags = recorder.LastFinishedActivity.TagObjects.ToArray();
                    Assert.Equal(InvalidHostName, tags.Single(t => t.Key == "dns.question.name").Value);
                    Assert.Equal("host_not_found", tags.Single(t => t.Key == "error.type").Value);
                }
            }, createParentActivity.ToString()).DisposeAsync();
        }
    }
}
