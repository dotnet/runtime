// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    public static class TraceConfiguration
    {
        private static volatile bool s_registered;

        /// <summary>
        /// Register the configuration system to apply settings from configuration files
        /// to <seealso cref="System.Diagnostics.TraceSource"/> and related classes.
        /// </summary>
        public static void Register()
        {
            if (!s_registered)
            {
                // Registering callbacks more than once is fine, but avoid the overhead common cases without taking a lock.
                Trace.Refreshing += RefreshingConfiguration;
                Switch.Initializing += InitializingSwitch;
                TraceSource.Initializing += InitializingTraceSource;

                ConfigureTraceSettings();

                s_registered = true;
            }
        }

        private static void RefreshingConfiguration(object sender, EventArgs e) => DiagnosticsConfiguration.Refresh();

        private static void InitializingTraceSource(object sender, InitializingTraceSourceEventArgs e)
        {
            TraceSource traceSource = e.TraceSource;

            // Ported from https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceSource.cs,176
            SourceElementsCollection sources = DiagnosticsConfiguration.Sources;

            if (sources != null)
            {
                SourceElement sourceElement = sources[traceSource.Name];
                if (sourceElement != null)
                {
                    e.WasInitialized = true;

                    // First check if the type changed.
                    if (HasSourceSwitchTypeChanged())
                    {
                        if (!string.IsNullOrEmpty(sourceElement.SwitchName))
                        {
                            CreateSwitch(sourceElement.SwitchType, sourceElement.SwitchName);
                        }
                        else
                        {
                            CreateSwitch(sourceElement.SwitchType, traceSource.Name);

                            if (!string.IsNullOrEmpty(sourceElement.SwitchValue))
                            {
                                traceSource.Switch.Level = Enum.Parse<SourceLevels>(sourceElement.SwitchValue);
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(sourceElement.SwitchName))
                    {
                        // Create a new switch if the name changed, otherwise just refresh.
                        if (sourceElement.SwitchName != traceSource.Switch.DisplayName)
                            CreateSwitch(sourceElement.SwitchType, sourceElement.SwitchName);
                        else
                        {
                            traceSource.Switch.Refresh();
                        }
                    }
                    else
                    {
                        // The SwitchValue changed; just update our internalSwitch.
                        if (!string.IsNullOrEmpty(sourceElement.SwitchValue))
                        {
                            traceSource.Switch.Level = Enum.Parse<SourceLevels>(sourceElement.SwitchValue);
                        }
                        else
                        {
                            traceSource.Switch.Level = SourceLevels.Off;
                        }
                    }

                    TraceListener[] newListenerCollection = new TraceListener[sourceElement.Listeners.Count];
                    int listnerOffset = 0;
                    foreach (ListenerElement listenerElement in sourceElement.Listeners)
                    {
                        TraceListener listener = traceSource.Listeners[listenerElement.Name];
                        if (listener != null)
                        {
                            newListenerCollection[listnerOffset++] = listenerElement.RefreshRuntimeObject(listener);
                        }
                        else
                        {
                            newListenerCollection[listnerOffset++] = listenerElement.GetRuntimeObject();
                        }
                    }

                    TraceUtils.CopyStringDictionary(sourceElement.Attributes, traceSource.Attributes);

                    traceSource.Listeners.Clear();
                    traceSource.Listeners.AddRange(newListenerCollection);
                }
                else
                {
                    // There was no config, so clear whatever we have.
                    traceSource.Switch.Level = traceSource.DefaultLevel;
                    traceSource.Listeners.Clear();
                    traceSource.Attributes.Clear();
                }

                bool HasSourceSwitchTypeChanged()
                {
                    string sourceTypeName = sourceElement.SwitchType;
                    Type currentType = traceSource.Switch.GetType();

                    if (string.IsNullOrEmpty(sourceTypeName))
                    {
                        // SourceSwitch is the default switch type.
                        return currentType != typeof(SourceSwitch);
                    }

                    if (sourceTypeName == currentType.FullName)
                    {
                        return false;
                    }

                    // Since there can be more than one valid AssemblyQualifiedName for a given Type this
                    // check can return true for some cases which can cause a minor side effect of a new
                    // Switch being created instead of just being refreshed.
                    return sourceElement.SwitchType != currentType.AssemblyQualifiedName;
                }
            }

            void CreateSwitch(string typeName, string name)
            {
                if (!string.IsNullOrEmpty(typeName))
                {
                    traceSource.Switch = (SourceSwitch)TraceUtils.GetRuntimeObject(typeName, typeof(SourceSwitch), name);
                }
                else
                {
                    traceSource.Switch = new SourceSwitch(name, traceSource.DefaultLevel.ToString());
                }
            }
        }

        private static void ConfigureTraceSettings()
        {
            // Ported from https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceInternal.cs,06360b4de5e221c2, https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceInternal.cs,37

            TraceSection traceSection = DiagnosticsConfiguration.SystemDiagnosticsSection?.Trace;

            if (traceSection != null)
            {
                Trace.UseGlobalLock = traceSection.UseGlobalLock;
                Trace.AutoFlush = traceSection.AutoFlush;
                Trace.IndentSize = traceSection.IndentSize;

                ListenerElementsCollection listeners = DiagnosticsConfiguration.SystemDiagnosticsSection?.Trace.Listeners;
                if (listeners != null)
                {
                    // If listeners were configured, replace the defaults with these.
                    Trace.Listeners.Clear();
                    foreach (var listener in listeners.GetRuntimeObject())
                    {
                        Trace.Listeners.Add(listener);
                    }
                }
            }
        }

        private static void InitializingSwitch(object sender, InitializingSwitchEventArgs e)
        {
            Switch sw = e.Switch;

            // Ported from https://referencesource.microsoft.com/#System/compmod/system/diagnostics/Switch.cs,173
            SwitchElementsCollection switchSettings = DiagnosticsConfiguration.SwitchSettings;
            if (switchSettings != null)
            {
                SwitchElement mySettings = switchSettings[sw.DisplayName];

                if (mySettings != null)
                {
                    if (mySettings.Value != null)
                    {
                        sw.Value = mySettings.Value;
                    }
                    else
                    {
                        sw.Value = sw.DefaultValue;
                    }

                    TraceUtils.CopyStringDictionary(sw.Attributes, mySettings.Attributes);
                }
            }
        }
    }
}
