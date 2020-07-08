// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;
using SdtEventSources;
using Microsoft.DotNet.RemoteExecutor;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif

namespace BasicEventSourceTests
{
    public class TestNotSupported
    {
        /// <summary>
        /// Tests using EventSource when the System.Diagnostics.Tracing.EventSource.IsSupported
        /// feature switch is set to disable all EventSource operations.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestBasicOperations_IsSupported_False()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions.Add("System.Diagnostics.Tracing.EventSource.IsSupported", false);

            RemoteExecutor.Invoke(() =>
            {
                using (var log = new EventSourceTest())
                {
                    using (var listener = new LoudListener(log))
                    {
                        IEnumerable<EventSource> sources = EventSource.GetSources();
                        Assert.Empty(sources);

                        Assert.Null(EventSource.GenerateManifest(typeof(SimpleEventSource), string.Empty, EventManifestOptions.OnlyIfNeededForRegistration));
                        Assert.Null(EventSource.GenerateManifest(typeof(EventSourceTest), string.Empty, EventManifestOptions.AllCultures));

                        log.Event0();
                        Assert.Null(LoudListener.LastEvent);

                        EventSource.SendCommand(log, EventCommand.Enable, commandArguments: null);
                        Assert.False(log.IsEnabled());
                    }
                }
            }, options).Dispose();
        }
    }
}
