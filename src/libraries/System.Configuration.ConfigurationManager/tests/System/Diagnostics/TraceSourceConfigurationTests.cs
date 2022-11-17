﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;
using System.Diagnostics;
using System.DiagnosticsTests;
using System.Reflection;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class DiagnosticConfigurationTests
    {
        [Fact]
        public void ReadConfigSectionsFromFile()
        {
            using (var temp = new TempConfig(DiagnosticsTestData.Sample))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);

                ConfigurationSection section = config.GetSection("system.diagnostics");
                Assert.NotNull(section);
                Assert.Equal("SystemDiagnosticsSection", section.GetType().Name);

                ConfigurationElementCollection collection;
                ConfigurationElement[] items;

                // Verify Switches
                collection = (ConfigurationElementCollection)GetPropertyValue("Switches");
                Assert.Equal("SwitchElementsCollection", collection.GetType().Name);
                Assert.Equal(1, collection.Count);
                items = new ConfigurationElement[1];
                collection.CopyTo(items, 0);
                Assert.Equal("sourceSwitch", items[0].ElementInformation.Properties["name"].Value.ToString());
                Assert.Equal("Error", items[0].ElementInformation.Properties["value"].Value.ToString());

                // Verify SharedListeners
                collection = (ConfigurationElementCollection)GetPropertyValue("SharedListeners");
                Assert.Equal("SharedListenerElementsCollection", collection.GetType().Name);
                Assert.Equal(1, collection.Count);
                items = new ConfigurationElement[1];
                collection.CopyTo(items, 0);
                Assert.Equal("myListener", items[0].ElementInformation.Properties["name"].Value.ToString());
                Assert.Equal("System.Diagnostics.TextWriterTraceListener", items[0].ElementInformation.Properties["type"].Value.ToString());
                Assert.Equal("myListener.log", items[0].ElementInformation.Properties["initializeData"].Value.ToString());

                // Verify Sources
                collection = (ConfigurationElementCollection)GetPropertyValue("Sources");
                Assert.Equal("SourceElementsCollection", GetPropertyValue("Sources").GetType().Name);
                Assert.Equal(1, collection.Count);
                items = new ConfigurationElement[1];
                collection.CopyTo(items, 0);
                Assert.Equal("TraceSourceApp", items[0].ElementInformation.Properties["name"].Value.ToString());
                Assert.Equal("sourceSwitch", items[0].ElementInformation.Properties["switchName"].Value.ToString());
                Assert.Equal("System.Diagnostics.SourceSwitch", items[0].ElementInformation.Properties["switchType"].Value);

                ConfigurationElementCollection listeners = (ConfigurationElementCollection)items[0].ElementInformation.Properties["listeners"].Value;
                Assert.Equal("ListenerElementsCollection", listeners.GetType().Name);
                Assert.Equal(2, listeners.Count);
                ConfigurationElement[] listenerItems = new ConfigurationElement[2];
                listeners.CopyTo(listenerItems, 0);
                Assert.Equal("console", listenerItems[0].ElementInformation.Properties["name"].Value.ToString());
                Assert.Equal("System.Diagnostics.ConsoleTraceListener", listenerItems[0].ElementInformation.Properties["type"].Value.ToString());
                Assert.Equal("myListener", listenerItems[1].ElementInformation.Properties["name"].Value.ToString());

                ConfigurationElement filter = (ConfigurationElement)listenerItems[0].ElementInformation.Properties["filter"].Value;
                Assert.Equal("FilterElement", filter.GetType().Name);
                Assert.Equal("System.Diagnostics.EventTypeFilter", filter.ElementInformation.Properties["type"].Value.ToString());
                Assert.Equal("Error", filter.ElementInformation.Properties["initializeData"].Value.ToString());

                object GetPropertyValue(string propertyName) => section.GetType().
                    GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance).
                    GetValue(section);
            }
        }
    }
}

