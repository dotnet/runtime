// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;
using System.IO;
using System.Reflection;
using Xunit;

namespace System.Diagnostics.TraceSourceConfigTests
{
    // Note that parallelization is disabled due to file access as each test replaces the single config file on disk.
    public class ConfigurationTests
    {
        private static volatile string? _configFile = null;

        private static void CreateAndLoadConfigFile(string filename)
        {
            Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string dir = Path.GetDirectoryName(config.FilePath);
            string from = Path.Combine(dir, filename);

            if (_configFile == null)
            {
                _configFile = Path.GetFileName(config.FilePath);
                File.Copy(from, _configFile, overwrite: true);
                TraceConfiguration.Register();
                // Do not call Trace.Refresh() here since the first access should be tested without it.
            }
            else
            {
                File.Copy(from, _configFile, overwrite: true);
                Trace.Refresh();
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/74244", TestPlatforms.tvOS)]
        public void RuntimeFilterChange()
        {
            CreateAndLoadConfigFile("testhost_ConfigWithRuntime.config");

            TraceSource mySource = new TraceSource("TraceSourceApp");
            StringTraceListener origListener = (StringTraceListener)mySource.Listeners["origListener"];
            StringTraceListener secondListener = (StringTraceListener)mySource.Listeners["secondListener"];

            // Issue an error and a warning message. Only the error message should be logged.
            mySource.TraceEvent(TraceEventType.Error, 1, "Error message.");
            mySource.TraceEvent(TraceEventType.Warning, 2, "Warning message.");

            Assert.Equal($"TraceSourceApp Error: 1 : Error message.{Environment.NewLine}", origListener.Output);
            Assert.Equal($"TraceSourceApp Error: 1 : Error message.{Environment.NewLine}", secondListener.Output);

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
                $"TraceSourceApp Critical: 3 : Critical message.{Environment.NewLine}" +
                $"TraceSourceApp Warning: 4 : Warning message.{Environment.NewLine}", origListener.Output);

            // secondListener is unchanged and doesn't log warnings.
            Assert.Equal($"TraceSourceApp Critical: 3 : Critical message.{Environment.NewLine}", secondListener.Output);

            // Restore the original filter settings.
            origListener.Clear();
            secondListener.Clear();
            origListener.Filter = configFilter;

            // Issue an error and information message. Only the error message should be logged.
            mySource.TraceEvent(TraceEventType.Error, 5, "Error message.");
            mySource.TraceInformation("Informational message.");

            Assert.Equal($"TraceSourceApp Error: 5 : Error message.{Environment.NewLine}", origListener.Output);
            Assert.Equal($"TraceSourceApp Error: 5 : Error message.{Environment.NewLine}", secondListener.Output);

            origListener.Clear();
            secondListener.Clear();
            mySource.Close();
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/74244", TestPlatforms.tvOS)]
        public void Refresh_RemoveSwitch()
        {
            // Use a SourceSwitch that logs Error.
            CreateAndLoadConfigFile("testhost_RemoveSwitch_before.config");

            SourceSwitch sswitch = new SourceSwitch("Refresh_RemoveSwitch_sourceSwitchToBeRemoved", "Warning");
            Assert.Equal("Warning", sswitch.DefaultValue);
            Assert.Equal("Error", sswitch.Value);

            TraceSource mySource = new TraceSource("Refresh_RemoveSwitch");
            StringTraceListener listener = (StringTraceListener)mySource.Listeners["listener"];

            Log();
            Assert.Equal(
                $"Refresh_RemoveSwitch Error: 1 : Error message.{Environment.NewLine}" +
                $"Refresh_RemoveSwitch Critical: 3 : Critical message.{Environment.NewLine}", listener.Output);

            // Change the switch to log All.
            listener.Clear();
            CreateAndLoadConfigFile("testhost_RemoveSwitch_after.config");
            Trace.Refresh();

            Assert.Equal("Warning", sswitch.DefaultValue);
            Assert.Equal("Warning", sswitch.Value); // Changed to Warning since the switch was removed from the config.

            Log();
            Assert.Equal(string.Empty, listener.Output); // The default replacement switch is off.

            listener.Clear();
            void Log()
            {
                mySource.TraceEvent(TraceEventType.Error, 1, "Error message.");
                mySource.TraceEvent(TraceEventType.Warning, 2, "Warning message.");
                mySource.TraceEvent(TraceEventType.Critical, 3, "Critical message.");
                mySource.TraceInformation("Informational message.");
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/74244", TestPlatforms.tvOS)]
        public void Refresh_ChangeSwitch()
        {
            // Use a SourceSwitch that logs Error.
            CreateAndLoadConfigFile("testhost_ChangeSwitch_before.config");

            TraceSource mySource = new TraceSource("Refresh_ChangeSwitch");
            StringTraceListener listener = (StringTraceListener)mySource.Listeners["listener"];

            mySource.TraceInformation("Informational message.");
            Assert.Equal(string.Empty, listener.Output); // Switch is off.

            // Change the switch to log.
            listener.Clear();
            CreateAndLoadConfigFile("testhost_ChangeSwitch_after.config");
            Trace.Refresh();

            mySource.TraceInformation("Informational message.");
            Assert.Equal($"Refresh_ChangeSwitch Information: 0 : Informational message.{Environment.NewLine}", listener.Output);

            listener.Close();
            mySource.Close();
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/74244", TestPlatforms.tvOS)]
        public void Refresh_RemoveSource()
        {
            // Use a SourceSwitch that logs Error.
            CreateAndLoadConfigFile("testhost_RemoveSource_before.config");

            TraceSource mySourceToBeRemoved = new TraceSource("Refresh_RemoveSource", SourceLevels.Warning);
            Assert.Equal(SourceLevels.Warning, mySourceToBeRemoved.DefaultLevel);
            Assert.Equal(SourceLevels.Error, mySourceToBeRemoved.Switch.Level); // Config has Error.
            Assert.Equal("Error", mySourceToBeRemoved.Switch.Value);

            StringTraceListener listenerToBeRemoved = (StringTraceListener)mySourceToBeRemoved.Listeners["listener"];
            listenerToBeRemoved.Clear();
            mySourceToBeRemoved.TraceEvent(TraceEventType.Error, 1, "Error message.");
            Assert.Equal($"Refresh_RemoveSource Error: 1 : Error message.{Environment.NewLine}", listenerToBeRemoved.Output);

            // Change the switch to log All.
            listenerToBeRemoved.Clear();
            CreateAndLoadConfigFile("testhost_RemoveSource_after.config");
            Trace.Refresh();

            Assert.Equal(SourceLevels.Warning, mySourceToBeRemoved.DefaultLevel);
            Assert.Equal(SourceLevels.Warning, mySourceToBeRemoved.Switch.Level); // Changed to Warning since the switch was removed from the config.
            Assert.Equal("Error", mySourceToBeRemoved.Switch.Value);

            mySourceToBeRemoved.TraceEvent(TraceEventType.Error, 1, "Error message.");
            Assert.Equal(string.Empty, listenerToBeRemoved.Output);

            listenerToBeRemoved.Clear();
            mySourceToBeRemoved.Close();
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/74244", TestPlatforms.tvOS)]
        public void ConfigWithEvents_RuntimeListener()
        {
            CreateAndLoadConfigFile("testhost_ConfigWithRuntime.config");

            TraceSource.Initializing += SubscribeToTraceSource_Initializing;
            Switch.Initializing += SubscribeToSwitch_Initializing;

            TraceSource mySource = new("TraceSource_NoListeners");
            Assert.Equal(1, mySource.Listeners.Count); // The default listener was removed via the config
            StringTraceListener dynamicallyAddedListener = (StringTraceListener)mySource.Listeners[0];

            // Only the Critical should be logged.
            // The config setting was to only log Error, but changed to Critical in the event handler.
            Log();
            Assert.Equal($"TraceSource_NoListeners Critical: 3 : Critical message.{Environment.NewLine}", dynamicallyAddedListener.Output);

            // Log all.
            dynamicallyAddedListener.Clear();
            mySource.Switch.Level = SourceLevels.All;
            Log();
            Assert.Equal(
                $"TraceSource_NoListeners Error: 1 : Error message.{Environment.NewLine}" +
                $"TraceSource_NoListeners Warning: 2 : Warning message.{Environment.NewLine}" +
                $"TraceSource_NoListeners Critical: 3 : Critical message.{Environment.NewLine}" +
                $"TraceSource_NoListeners Information: 0 : Informational message.{Environment.NewLine}", dynamicallyAddedListener.Output);

            dynamicallyAddedListener.Clear();
            mySource.Close();
            TraceSource.Initializing -= SubscribeToTraceSource_Initializing;
            Switch.Initializing -= SubscribeToSwitch_Initializing;

            void Log()
            {
                mySource.TraceEvent(TraceEventType.Error, 1, "Error message.");
                mySource.TraceEvent(TraceEventType.Warning, 2, "Warning message.");
                mySource.TraceEvent(TraceEventType.Critical, 3, "Critical message.");
                mySource.TraceInformation("Informational message.");
            }
        }

        private void SubscribeToTraceSource_Initializing(object? sender, InitializingTraceSourceEventArgs e)
        {
            TraceSource traceSource = e.TraceSource;
            if (traceSource.Name == "TraceSource_NoListeners")
            {
                Assert.Equal("generalSourceSwitch_Error", traceSource.Switch.DisplayName);
                traceSource.Listeners.Add(new StringTraceListener());
            }
        }

        private void SubscribeToSwitch_Initializing(object? sender, InitializingSwitchEventArgs e)
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
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/74244", TestPlatforms.tvOS)]
        public void AllTypes()
        {
            CreateAndLoadConfigFile("testhost_AllTypes.config");

            TraceSource mySource;

            mySource = new("ConsoleTraceListener");
            Assert.Equal("L1", mySource.Listeners[1].Name);

            mySource = new("DefaultTraceListener");
            Assert.Equal("L1", mySource.Listeners[1].Name);

            mySource = new("DelimitedListTraceListener");
            Assert.Equal("L1", mySource.Listeners[1].Name);

            // The referenced S.R.ConfigurationManager.dll is NetStandard, which does not support EventLogTraceListener.
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

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/74244", TestPlatforms.tvOS)]
        public void Switch_MissingValue_Throws()
        {
            Exception e = Assert.Throws<ConfigurationErrorsException>(() =>
                CreateAndLoadConfigFile("testhost_Switch_MissingValue_Throws.config"));

            Assert.Contains("'value'", e.ToString());
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/74244", TestPlatforms.tvOS)]
        public void UnsupportedAttribute_Throws()
        {
            CreateAndLoadConfigFile("testhost_UnsupportedAttribute_Throws.config");

            var traceSource = new TraceSource("Foo", SourceLevels.Off);
            // When the config is loaded and TraceUtil.CopyStringDictionary() works, you get
            //   System.ArgumentException : 'foo' is not a valid attribute for type 'System.Diagnostics.TraceSource'.
            Assert.Throws<ArgumentException>(() => traceSource.TraceInformation("Test"));
        }
    }
}
