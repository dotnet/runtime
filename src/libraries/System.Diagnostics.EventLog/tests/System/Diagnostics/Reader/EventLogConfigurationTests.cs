// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Eventing.Reader;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogConfigurationTests
    {
        [ConditionalTheory(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [InlineData("Microsoft-Windows-TaskScheduler/Operational")]
        [InlineData("Application")]
        public void EventLogConfiguration_CheckProperties_RemainsTheSame(string logName)
        {
            bool isEnabled;
            string securityDescriptor;
            EventLogMode logMode;
            int? providerBufferSize;
            long maximumSizeInBytes;
            int? providerMinimumNumberOfBuffers;
            int? providerMaximumNumberOfBuffers;
            int? providerLatency;
            int? providerLevel;
            long? providerKeywords;
            Guid? providerControlGuid;

            using (var session = new EventLogSession())
            {
                EventLogConfiguration configuration = null;
                try
                {
                    configuration = new EventLogConfiguration(logName, session);
                }
                catch (EventLogNotFoundException)
                {
                    configuration?.Dispose();
                    return;
                }
                
                Assert.Equal(logName, configuration.LogName);
                Assert.NotEmpty(configuration.ProviderNames);
                Assert.Equal("Application", configuration.LogIsolation.ToString());

                if (logName.Equals("Application"))
                {
                    Assert.Equal(EventLogType.Administrative, configuration.LogType);
                    Assert.True(configuration.IsClassicLog);
                    Assert.Contains("Application.evtx", configuration.LogFilePath);
                    Assert.Empty(configuration.OwningProviderName);
                }
                else
                {
                    Assert.Equal(EventLogType.Operational, configuration.LogType);
                    Assert.False(configuration.IsClassicLog);
                    Assert.Contains("Microsoft-Windows-TaskScheduler%4Operational.evtx", configuration.LogFilePath);
                    Assert.Equal("Microsoft-Windows-TaskScheduler", configuration.OwningProviderName);
                }

                isEnabled = configuration.IsEnabled;
                securityDescriptor = configuration.SecurityDescriptor;
                logMode = configuration.LogMode;
                providerBufferSize = configuration.ProviderBufferSize;
                maximumSizeInBytes = configuration.MaximumSizeInBytes;
                providerMinimumNumberOfBuffers = configuration.ProviderMinimumNumberOfBuffers;
                providerMaximumNumberOfBuffers = configuration.ProviderMaximumNumberOfBuffers;
                providerLevel = configuration.ProviderLevel;
                providerKeywords = configuration.ProviderKeywords;
                providerControlGuid = configuration.ProviderControlGuid;
                providerLatency = configuration.ProviderLatency;

                configuration.Dispose();
            }
            using (var session = new EventLogSession())
            {
                using (var configuration = new EventLogConfiguration(logName, session))
                {
                    Assert.Equal(isEnabled, configuration.IsEnabled);
                    Assert.Equal(securityDescriptor, configuration.SecurityDescriptor);
                    Assert.Equal(logMode, configuration.LogMode);
                    Assert.Equal(providerBufferSize, configuration.ProviderBufferSize);
                    Assert.Equal(maximumSizeInBytes, configuration.MaximumSizeInBytes);
                    Assert.Equal(providerMinimumNumberOfBuffers, configuration.ProviderMinimumNumberOfBuffers);
                    Assert.Equal(providerMaximumNumberOfBuffers, configuration.ProviderMaximumNumberOfBuffers);
                    Assert.Equal(providerLevel, configuration.ProviderLevel);
                    Assert.Equal(providerKeywords, configuration.ProviderKeywords);
                    Assert.Equal(providerControlGuid, configuration.ProviderControlGuid);
                    Assert.Equal(providerLatency, configuration.ProviderLatency);
                }
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.NotElevatedAndSupportsEventLogs))]
        public void SetProperties_SaveChanges_NotAdmin_Throws()
        {
            const string LogName = "Application";
            using (var session = new EventLogSession())
            {
                EventLogConfiguration configuration = null;
                try
                {
                    configuration = new EventLogConfiguration(LogName, session);
                }
                catch (EventLogNotFoundException)
                {
                    configuration?.Dispose();
                    return;
                }

                configuration.IsEnabled = false;
                configuration.SecurityDescriptor = string.Empty;
                configuration.LogFilePath = null;
                configuration.LogMode = EventLogMode.Retain;
                configuration.ProviderLevel = 1;
                configuration.ProviderKeywords = 1;
                configuration.MaximumSizeInBytes = long.MaxValue;
                Assert.Throws<UnauthorizedAccessException>(() => configuration.SaveChanges());

                configuration.Dispose();
                session.CancelCurrentOperations();
            }
        }
    }
}