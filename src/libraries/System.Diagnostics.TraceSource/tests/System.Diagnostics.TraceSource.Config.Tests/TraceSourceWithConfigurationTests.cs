// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.TraceSourceConfigTests
{
    public class ConfigurationTests
    {
        private const string ConfigFile = "testhost.dll.config";

        private static void CreateAndLoadConfigFile(string filename)
        {
            Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            Assert.Equal(ConfigFile, Path.GetFileName(config.FilePath));
            string dir = Path.GetDirectoryName(config.FilePath);
            string from = Path.Combine(dir, filename);
            File.Copy(from, ConfigFile, overwrite: true);
            TraceConfiguration.Register();
            Trace.Refresh();
        }

        [Fact]
        public void ConfigWithRuntimeFilterChange()
        {
            CreateAndLoadConfigFile("testhost_ConfigWithRuntime.dll.config");

            TraceSource mySource = new TraceSource("TraceSourceApp");
            StringTraceListener origListener = (StringTraceListener)mySource.Listeners["origListener"];
            StringTraceListener secondListener = (StringTraceListener)mySource.Listeners["secondListener"];

            // Issue an error and a warning message. Only the error message should be logged.
            mySource.TraceEvent(TraceEventType.Error, 1, "Error message.");
            mySource.TraceEvent(TraceEventType.Warning, 2, "Warning message.");

            Assert.Equal("TraceSourceApp Error: 1 : Error message.\r\n", origListener.Output);
            Assert.Equal("TraceSourceApp Error: 1 : Error message.\r\n", secondListener.Output);

            // Save the original settings from the configuration file.
            EventTypeFilter configFilter = (EventTypeFilter)mySource.Listeners["origListener"].Filter;
            Assert.NotNull(configFilter);

            // Create a new filter that logs warnings.
            origListener.Filter = new EventTypeFilter(SourceLevels.Warning);

            // Allow the trace source to send messages to listeners for all event types.
            // This overrides settings in the configuration file.
            // If the switch level is not changed, the event filter changes have no effect.
            mySource.Switch.Level = SourceLevels.All;

            // Issue a critical and warning message.
            origListener.Clear();
            secondListener.Clear();
            mySource.TraceEvent(TraceEventType.Critical, 3, "Critical message.");
            mySource.TraceEvent(TraceEventType.Warning, 4, "Warning message.");

            // Both should be logged for origListener.
            Assert.Equal(
                "TraceSourceApp Critical: 3 : Critical message.\r\n" +
                "TraceSourceApp Warning: 4 : Warning message.\r\n", origListener.Output);

            // secondListener is unchanged and doesn't log warnings.
            Assert.Equal("TraceSourceApp Critical: 3 : Critical message.\r\n", secondListener.Output);

            // Restore the original filter settings.
            origListener.Clear();
            secondListener.Clear();
            origListener.Filter = configFilter;

            // Issue an error and information message. Only the error message should be logged.
            mySource.TraceEvent(TraceEventType.Error, 5, "Error message.");
            mySource.TraceInformation("Informational message.");

            Assert.Equal("TraceSourceApp Error: 5 : Error message.\r\n", origListener.Output);
            Assert.Equal("TraceSourceApp Error: 5 : Error message.\r\n", secondListener.Output);

            origListener.Clear();
            secondListener.Clear();
            mySource.Close();
        }

        [Fact]
        public void RefreshSwitchFromConfigFile()
        {
            // Use a SourceSwitch that logs Error.
            CreateAndLoadConfigFile("testhost_RefreshSwitch_before.dll.config");

            TraceSource mySource = new TraceSource("TraceSourceApp");
            StringTraceListener listener = (StringTraceListener)mySource.Listeners["origListener"];
            Log();
            Assert.Equal(
                "TraceSourceApp Error: 1 : Error message.\r\n" +
                "TraceSourceApp Critical: 3 : Critical message.\r\n", listener.Output);

            // Change the switch to log All.
            listener.Clear();
            CreateAndLoadConfigFile("testhost_RefreshSwitch_after.dll.config");
            Trace.Refresh();
            Log();
            Assert.Equal(
                "TraceSourceApp Error: 1 : Error message.\r\n" +
                "TraceSourceApp Warning: 2 : Warning message.\r\n" +
                "TraceSourceApp Critical: 3 : Critical message.\r\n" +
                "TraceSourceApp Information: 0 : Informational message.\r\n", listener.Output);

            listener.Clear();
            mySource.Close();

            void Log()
            {
                mySource.TraceEvent(TraceEventType.Error, 1, "Error message.");
                mySource.TraceEvent(TraceEventType.Warning, 2, "Warning message.");
                mySource.TraceEvent(TraceEventType.Critical, 3, "Critical message.");
                mySource.TraceInformation("Informational message.");
            }
        }

        [Fact]
        public void ConfigWithEvents_RuntimeListener()
        {
            CreateAndLoadConfigFile("testhost_ConfigWithRuntime.dll.config");

            Trace.ConfigureTraceSource += SubscribeToConfigTracesource_ConfigureTraceSource;
            Trace.ConfigureSwitch += SubscribeToConfigTracesource_ConfigureSwitch;

            TraceSource mySource = new("TraceSource_NoListeners");
            Assert.Equal(1, mySource.Listeners.Count); // The default listener was removed via the config
            StringTraceListener dynamicallyAddedListener = (StringTraceListener)mySource.Listeners[0];

            // Only the Critical should be logged.
            // The config setting was to only log Error, but changed to Critical in the event handler.
            Log();
            Assert.Equal("TraceSource_NoListeners Critical: 3 : Critical message.\r\n", dynamicallyAddedListener.Output);

            // Log all.
            dynamicallyAddedListener.Clear();
            mySource.Switch.Level = SourceLevels.All;
            Log();
            Assert.Equal(
                "TraceSource_NoListeners Error: 1 : Error message.\r\n" +
                "TraceSource_NoListeners Warning: 2 : Warning message.\r\n" +
                "TraceSource_NoListeners Critical: 3 : Critical message.\r\n" +
                "TraceSource_NoListeners Information: 0 : Informational message.\r\n", dynamicallyAddedListener.Output);

            dynamicallyAddedListener.Clear();
            mySource.Close();
            Trace.ConfigureTraceSource -= SubscribeToConfigTracesource_ConfigureTraceSource;
            Trace.ConfigureSwitch -= SubscribeToConfigTracesource_ConfigureSwitch;

            void Log()
            {
                mySource.TraceEvent(TraceEventType.Error, 1, "Error message.");
                mySource.TraceEvent(TraceEventType.Warning, 2, "Warning message.");
                mySource.TraceEvent(TraceEventType.Critical, 3, "Critical message.");
                mySource.TraceInformation("Informational message.");
            }
        }

        private void SubscribeToConfigTracesource_ConfigureTraceSource(object? sender, ConfigureTraceSourceEventArgs e)
        {
            TraceSource traceSource = e.TraceSource;
            if (traceSource.Name == "TraceSource_NoListeners")
            {
                Assert.Equal("generalSourceSwitch_Error", traceSource.Switch.DisplayName);
                traceSource.Listeners.Add(new StringTraceListener());
            }
        }

        private void SubscribeToConfigTracesource_ConfigureSwitch(object? sender, ConfigureSwitchEventArgs e)
        {
            Switch sw = e.Switch;
            if (sw.DisplayName == "generalSourceSwitch_Error")
            {
                Assert.IsType<SourceSwitch>(sw);
                SourceSwitch sourceSwitch = (SourceSwitch)sw;
                Assert.Equal(TraceLevel.Error.ToString(), sourceSwitch.Level.ToString());

                // Change to critical
                sourceSwitch.Level = SourceLevels.Critical;
            }
        }

        [Fact]
        public void AllTypes()
        {
            CreateAndLoadConfigFile("testhost_AllTypes.dll.config");

            TraceSource mySource;

            mySource = new("ConsoleTraceListener");
            Assert.Equal("L1", mySource.Listeners[1].Name);

            mySource = new("DefaultTraceListener");
            Assert.Equal("L1", mySource.Listeners[1].Name);

            mySource = new("DelimitedListTraceListener");
            Assert.Equal("L1", mySource.Listeners[1].Name);

            // Only supported on .NET Framework.
            mySource = new("EventLogTraceListener");
            Exception e = Assert.Throws<ConfigurationErrorsException>(() => mySource.Listeners[1].Name);
            Assert.IsType<PlatformNotSupportedException>(e.InnerException);

            mySource = new("TextWriterTraceListener");
            Assert.Equal("L1", mySource.Listeners[1].Name);

            mySource = new("XmlWriterTraceListener");
            Assert.Equal("L1", mySource.Listeners[1].Name);

            Switch switch_booleanSwitch = new BooleanSwitch("booleanSwitch", null);
            Assert.Equal("True", switch_booleanSwitch.Value);

            Switch switch_sourceSwitch = new SourceSwitch("sourceSwitch");
            Assert.Equal("Critical", switch_sourceSwitch.Value);

            Switch switch_traceSwitch = new TraceSwitch("traceSwitch", null);
            Assert.Equal("Info", switch_traceSwitch.Value);

            TraceSource filter_sourceFilter = new("filter_sourceFilter");
            Assert.IsType<SourceFilter>(filter_sourceFilter.Listeners[1].Filter);

            TraceSource filter_eventTypeFilter = new("filter_eventTypeFilter");
            Assert.IsType<EventTypeFilter>(filter_eventTypeFilter.Listeners[1].Filter);
        }
    }
}
