// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProviderMetadataTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void SourceDoesNotExist_Throws()
        {
            Assert.Throws<EventLogNotFoundException>(() => new ProviderMetadata("Source_Does_Not_Exist"));
        }

        [ConditionalTheory(typeof(Helpers), nameof(Helpers.IsElevatedAndSupportsEventLogs))]
        [InlineData(true)]
        [InlineData(false)]
        public void ProviderNameTests(bool noProviderName)
        {
            if (PlatformDetection.IsWindows10Version22000OrGreater) // ActiveIssue("https://github.com/dotnet/runtime/issues/58829")
                return;

            string log = "Application";
            string source = "Source_" + nameof(ProviderNameTests);
            using (var session = new EventLogSession())
            {
                try
                {
                    EventLog.CreateEventSource(source, log);

                    string providerName = noProviderName ? "" : source;
                    using (var providerMetadata = new ProviderMetadata(providerName))
                    {
                        Assert.Null(providerMetadata.DisplayName);
                        Assert.Equal(providerName, providerMetadata.Name);
                        Assert.Equal(new Guid(), providerMetadata.Id);
                        Assert.Empty(providerMetadata.Events);
                        Assert.Empty(providerMetadata.Keywords);
                        Assert.Empty(providerMetadata.Levels);
                        Assert.Empty(providerMetadata.Opcodes);
                        Assert.Empty(providerMetadata.Tasks);
                        Assert.NotEmpty(providerMetadata.LogLinks);
                        if (!string.IsNullOrEmpty(providerName))
                        {
                            foreach (var logLink in providerMetadata.LogLinks)
                            {
                                Assert.True(logLink.IsImported);
                                Assert.Equal(log, logLink.LogName);
                                Assert.NotEmpty(logLink.DisplayName);
                                if (CultureInfo.CurrentCulture.Name.Split('-')[0] == "en" )
                                {
                                    Assert.Equal("Application", logLink.DisplayName);
                                }
                                else if (CultureInfo.CurrentCulture.Name.Split('-')[0] == "es" )
                                {
                                    Assert.Equal("Aplicaci\u00F3n", logLink.DisplayName);
                                }
                            }

                            string[] expectedMessageFileNames = new[] { "EventLogMessages.dll", "System.Diagnostics.EventLog.Messages.dll" };
                            string messageFileName = Path.GetFileName(providerMetadata.MessageFilePath);
                            Assert.Contains(expectedMessageFileNames, expected => expected.Equals(messageFileName, StringComparison.OrdinalIgnoreCase));
                            if (providerMetadata.HelpLink != null)
                            {
                                string helpLink = providerMetadata.HelpLink.ToString();
                                Assert.Contains(expectedMessageFileNames, expected => -1 != helpLink.IndexOf(expected, StringComparison.OrdinalIgnoreCase));
                            }
                        }
                        else
                        {
                            Assert.Null(providerMetadata.MessageFilePath);
                            Assert.Null(providerMetadata.HelpLink);
                        }
                        Assert.Null(providerMetadata.ResourceFilePath);
                        Assert.Null(providerMetadata.ParameterFilePath);
                    }
                }
                finally
                {
                    EventLog.DeleteEventSource(source);
                }
                session.CancelCurrentOperations();
            }
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/64153")]
        public void GetProviderNames_AssertProperties()
        {
            const string Prefix = "win:";
            var standardOpcodeNames = new List<string>(Enum.GetNames(typeof(StandardEventOpcode))).Select(x => Prefix + x).ToList();
            using (var session = new EventLogSession())
            {
                Assert.NotEmpty(session.GetProviderNames());
                foreach (string providerName in session.GetProviderNames())
                {
                    try
                    {
                        using (var providerMetadata = new ProviderMetadata(providerName))
                        {
                            foreach (var keyword in providerMetadata.Keywords)
                            {
                                Assert.NotEmpty(keyword.Name);
                            }
                            foreach (var logLink in providerMetadata.LogLinks)
                            {
                                Assert.NotEmpty(logLink.LogName);
                            }
                            foreach (var opcode in providerMetadata.Opcodes)
                            {
                                if (opcode != null && standardOpcodeNames.Contains(opcode.Name))
                                {
                                    Assert.Contains((((StandardEventOpcode)(opcode.Value)).ToString()), opcode.Name);
                                }
                            }
                            foreach (var eventMetadata in providerMetadata.Events)
                            {
                                EventLogLink logLink = eventMetadata.LogLink;
                                if (logLink != null)
                                {
                                    if (logLink.DisplayName != null && logLink.DisplayName.Equals("System"))
                                    {
                                        Assert.Equal("System", logLink.LogName);
                                        Assert.True(logLink.IsImported);
                                    }
                                }
                                EventLevel eventLevel = eventMetadata.Level;
                                if (eventLevel != null)
                                {
                                    if (eventLevel.Name != null)
                                    {
                                        // https://github.com/Microsoft/perfview/blob/d4b044abdfb4c8e40a344ca05383e04b5b6dc13a/src/related/EventRegister/winmeta.xml#L39
                                        if (eventLevel.Name.StartsWith(Prefix) && !eventLevel.Name.Contains("ReservedLevel"))
                                        {
                                            Assert.True(System.Enum.IsDefined(typeof(StandardEventLevel), eventLevel.Value));
                                            Assert.Contains(eventLevel.Name.Substring(4), Enum.GetNames(typeof(StandardEventLevel)));
                                        }
                                    }
                                }
                                EventOpcode opcode = eventMetadata.Opcode;
                                if (opcode != null)
                                {
                                    if (opcode.Name != null && opcode.DisplayName != null && opcode.DisplayName.ToLower().Equals("apprun"))
                                    {
                                        Assert.Contains(opcode.DisplayName.ToLower(), opcode.Name.ToLower());
                                    }
                                }
                                EventTask task = eventMetadata.Task;
                                if (task != null)
                                {
                                    Assert.NotEqual(task, eventMetadata.Task);
                                    Assert.Equal(task.DisplayName, eventMetadata.Task.DisplayName);
                                    Assert.Equal(task.Name, eventMetadata.Task.Name);
                                    Assert.Equal(task.Value, eventMetadata.Task.Value);
                                }
                                IEnumerable<EventKeyword> keywords = eventMetadata.Keywords;
                                if (eventMetadata.Keywords != null)
                                {
                                    foreach (var keyword in eventMetadata.Keywords)
                                    {
                                        if (keyword.Name != null && keyword.Name.StartsWith(Prefix))
                                        {
                                            Assert.True(System.Enum.IsDefined(typeof(StandardEventKeywords), keyword.Value));
                                        }
                                    }
                                }
                                Assert.NotNull(eventMetadata.Template);
                            }
                        }
                    }
                    catch (EventLogException)
                    {
                        continue;
                    }
                }
            }
        }
    }
}
