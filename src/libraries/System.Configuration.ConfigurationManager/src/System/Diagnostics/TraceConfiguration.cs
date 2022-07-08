// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections;

namespace System.Diagnostics
{
    public static class TraceConfiguration
    {
        public static void Register()
        {
            // register callbacks
            Trace.ConfigureSwitch += ConfigureSwitch;
            Trace.ConfigureTrace += ConfigureTraceSettings;
            Trace.ConfigureTraceSource += ConfigureTraceSource;
        }

        private static void ConfigureTraceSource(TraceSource traceSource)
        {
            // ported from https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceSource.cs,176
            SourceElementsCollection sources = DiagnosticsConfiguration.Sources;

            if (sources != null)
            {
                SourceElement sourceElement = sources[traceSource.Name];
                if (sourceElement != null)
                {
                    // first check if the type changed
                    if ((string.IsNullOrEmpty(sourceElement.SwitchType) && traceSource.Switch.GetType() != typeof(SourceSwitch)) ||
                         (sourceElement.SwitchType != traceSource.Switch.GetType().AssemblyQualifiedName))
                    {

                        if (!string.IsNullOrEmpty(sourceElement.SwitchName))
                        {
                            CreateSwitch(sourceElement.SwitchType, sourceElement.SwitchName);
                        }
                        else
                        {
                            CreateSwitch(sourceElement.SwitchType, traceSource.Name);

                            if (!string.IsNullOrEmpty(sourceElement.SwitchValue))
                                traceSource.Switch.Level = (SourceLevels)Enum.Parse(typeof(SourceLevels), sourceElement.SwitchValue);
                        }
                    }
                    else if (!string.IsNullOrEmpty(sourceElement.SwitchName))
                    {
                        // create a new switch if the name changed, otherwise just refresh.
                        if (sourceElement.SwitchName != traceSource.Switch.DisplayName)
                            CreateSwitch(sourceElement.SwitchType, sourceElement.SwitchName);
                        else
                        {
                            traceSource.Switch.Refresh();
                        }
                    }
                    else
                    {
                        // the switchValue changed.  Just update our internalSwitch.
                        if (!string.IsNullOrEmpty(sourceElement.SwitchValue))
                            traceSource.Switch.Level = (SourceLevels)Enum.Parse(typeof(SourceLevels), sourceElement.SwitchValue);
                        else
                            traceSource.Switch.Level = SourceLevels.Off;
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

                    traceSource.Attributes = sourceElement.Attributes;

                    traceSource.Listeners.Clear();
                    traceSource.Listeners.AddRange(newListenerCollection);
                }
                else
                {
                    // there was no config, so clear whatever we have.
                    traceSource.Switch.Level = traceSource.DefaultLevel;
                    traceSource.Listeners.Clear();
                    traceSource.Attributes.Clear();
                }
            }

            void CreateSwitch(string typeName, string name)
            {
                if (!string.IsNullOrEmpty(typeName))
                    traceSource.Switch = (SourceSwitch)TraceUtils.GetRuntimeObject(typeName, typeof(SourceSwitch), name);
                else
                    traceSource.Switch = new SourceSwitch(name, traceSource.DefaultLevel.ToString());
            }
        }

        private static void ConfigureTraceSettings()
        {
            // ported from https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceInternal.cs,06360b4de5e221c2, https://referencesource.microsoft.com/#System/compmod/system/diagnostics/TraceInternal.cs,37

            TraceSection traceSection = DiagnosticsConfiguration.SystemDiagnosticsSection?.Trace;

            if (traceSection != null)
            {
                Trace.UseGlobalLock = traceSection.UseGlobalLock;
                Trace.AutoFlush = traceSection.AutoFlush;
                Trace.IndentSize = traceSection.IndentSize;

                ListenerElementsCollection listeners = DiagnosticsConfiguration.SystemDiagnosticsSection?.Trace.Listeners;
                if (listeners != null)
                {
                    // If listeners were configured, replace the defaults with these
                    Trace.Listeners.Clear();
                    foreach (var listener in listeners.GetRuntimeObject())
                    {
                        Trace.Listeners.Add(listener);
                    }
                }
            }
        }

        private static void ConfigureSwitch(Switch @switch)
        {
            // ported from https://referencesource.microsoft.com/#System/compmod/system/diagnostics/Switch.cs,173
            SwitchElementsCollection switchSettings = DiagnosticsConfiguration.SwitchSettings;
            if (switchSettings != null)
            {
                SwitchElement mySettings = switchSettings[@switch.DisplayName];

                if (mySettings != null)
                {
                    if (mySettings.Value != null)
                    {
                        @switch.Value = mySettings.Value;
                    }
                    else
                    {
                        @switch.Value = @switch.DefaultValue;
                    }

                    @switch.Attributes = mySettings.Attributes;
                }
            }

        }
    }
}
