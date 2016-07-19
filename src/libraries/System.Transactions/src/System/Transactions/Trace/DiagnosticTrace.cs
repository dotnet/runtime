// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.ComponentModel;
using System.Threading;

namespace System.Transactions.Diagnostics
{
    /// <summary>
    /// DiagnosticTrace consists of static methods, properties and collections
    /// that can be accessed by Indigo infrastructure code to provide 
    /// instrumentation.
    /// </summary>
    internal static class DiagnosticTrace
    {
        internal const string DefaultTraceListenerName = "Default";
        private static TraceSource s_traceSource = null;
        private static bool s_tracingEnabled = true;
        private static bool s_haveListeners = false;
        private static Dictionary<int, string> s_traceEventTypeNames;
        private static object s_localSyncObject = new object();
        private static int s_traceFailureCount = 0;
        private static int s_traceFailureThreshold = 0;
        private static SourceLevels s_level;
        private static bool s_calledShutdown = false;
        private static bool s_shouldCorrelate = false;
        private static bool s_shouldTraceVerbose = false;
        private static bool s_shouldTraceInformation = false;
        private static bool s_shouldTraceWarning = false;
        private static bool s_shouldTraceError = false;
        private static bool s_shouldTraceCritical = false;
        internal static Guid EmptyGuid = Guid.Empty;
        private static string s_appDomainFriendlyName = null;

        private const string subType = "";
        private const string version = "1";

        private const int traceFailureLogThreshold = 10;
        private const string EventLogSourceName = ".NET Runtime";
        private const string TraceSourceName = "System.Transactions";
        private const string TraceRecordVersion = "http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord";

        private static string ProcessName
        {
            get
            {
                string retval = null;
                using (Process process = Process.GetCurrentProcess())
                {
                    retval = process.ProcessName;
                }
                return retval;
            }
        }

        private static int ProcessId
        {
            get
            {
                int retval = -1;
                using (Process process = Process.GetCurrentProcess())
                {
                    retval = process.Id;
                }

                return retval;
            }
        }

        private static TraceSource TraceSource
        {
            get
            {
                return s_traceSource;
            }

            set
            {
                s_traceSource = value;
            }
        }

        private static Dictionary<int, string> TraceEventTypeNames
        {
            get
            {
                return s_traceEventTypeNames;
            }
        }

        private static SourceLevels FixLevel(SourceLevels level)
        {
            //the bit fixing below is meant to keep the trace level legal even if somebody uses numbers in config
            if (((level & ~SourceLevels.Information) & SourceLevels.Verbose) != 0)
            {
                level |= SourceLevels.Verbose;
            }
            else if (((level & ~SourceLevels.Warning) & SourceLevels.Information) != 0)
            {
                level |= SourceLevels.Information;
            }
            else if (((level & ~SourceLevels.Error) & SourceLevels.Warning) != 0)
            {
                level |= SourceLevels.Warning;
            }
            if (((level & ~SourceLevels.Critical) & SourceLevels.Error) != 0)
            {
                level |= SourceLevels.Error;
            }
            if ((level & SourceLevels.Critical) != 0)
            {
                level |= SourceLevels.Critical;
            }

            return level;
        }

        private static void SetLevel(SourceLevels level)
        {
            SourceLevels fixedLevel = FixLevel(level);
            s_level = fixedLevel;
            if (TraceSource != null)
            {
                TraceSource.Switch.Level = fixedLevel;
                s_shouldCorrelate = false; // ShouldTrace(TraceEventType.Transfer);
                s_shouldTraceVerbose = ShouldTrace(TraceEventType.Verbose);
                s_shouldTraceInformation = ShouldTrace(TraceEventType.Information);
                s_shouldTraceWarning = ShouldTrace(TraceEventType.Warning);
                s_shouldTraceError = ShouldTrace(TraceEventType.Error);
                s_shouldTraceCritical = ShouldTrace(TraceEventType.Critical);
            }
        }

        private static void SetLevelThreadSafe(SourceLevels level)
        {
            if (TracingEnabled && level != Level)
            {
                lock (s_localSyncObject)
                {
                    SetLevel(level);
                }
            }
        }

        internal static SourceLevels Level
        {
            //Do not call this property from Initialize!
            get
            {
                if (TraceSource != null && (TraceSource.Switch.Level != s_level))
                {
                    s_level = TraceSource.Switch.Level;
                }
                return s_level;
            }

            set
            {
                SetLevelThreadSafe(value);
            }
        }

        internal static bool HaveListeners
        {
            get
            {
                return s_haveListeners;
            }
        }

        internal static bool TracingEnabled
        {
            get
            {
                return s_tracingEnabled && s_traceSource != null;
            }
        }

        static DiagnosticTrace()
        {
            // We own the resource and it hasn't been filled in yet.
            //needed for logging events to event log
            s_appDomainFriendlyName = string.Empty; // AppDomain.CurrentDomain.FriendlyName; // .NET Core does not have AppDomains
            s_traceEventTypeNames = new Dictionary<int, string>();

            // Initialize the values here to avoid bringing in unnecessary pages.
            // Address MB#20806
            s_traceEventTypeNames[(int)TraceEventType.Critical] = "Critical";
            s_traceEventTypeNames[(int)TraceEventType.Error] = "Error";
            s_traceEventTypeNames[(int)TraceEventType.Warning] = "Warning";
            s_traceEventTypeNames[(int)TraceEventType.Information] = "Information";
            s_traceEventTypeNames[(int)TraceEventType.Verbose] = "Verbose";

            // TODO: Put back if/when TraceEventType in .NET Core gets these enum values
            //traceEventTypeNames[(int)TraceEventType.Resume] = "Resume";
            //traceEventTypeNames[(int)TraceEventType.Start] = "Start";
            //traceEventTypeNames[(int)TraceEventType.Stop] = "Stop";
            //traceEventTypeNames[(int)TraceEventType.Suspend] = "Suspend";
            //traceEventTypeNames[(int)TraceEventType.Transfer] = "Transfer";

            TraceFailureThreshold = traceFailureLogThreshold;
            TraceFailureCount = TraceFailureThreshold + 1;

            try
            {
                s_traceSource = new TraceSource(TraceSourceName, SourceLevels.Critical);

                // TODO #9327: If/when .NET Core gets back UnhandledException/DomainUnload/ProcessExit handlers,
                // add this support back.
                //AppDomain currentDomain = AppDomain.CurrentDomain;
                //if (TraceSource.Switch.ShouldTrace(TraceEventType.Critical))
                //{
                //    currentDomain.UnhandledException += (s, args) =>
                //    {
                //        Exception e = (Exception)args.ExceptionObject;
                //        TraceEvent(TraceEventType.Critical, DiagnosticTraceCode.UnhandledException, SR.UnhandledException, null, e, ref EmptyGuid, false, null);
                //        ShutdownTracing();
                //    };
                //}
                //currentDomain.DomainUnload += new EventHandler(ExitOrUnloadEventHandler);
                //currentDomain.ProcessExit += new EventHandler(ExitOrUnloadEventHandler);

                s_haveListeners = TraceSource.Listeners.Count > 0;
                SetLevel(TraceSource.Switch.Level);
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (TraceSource == null)
                {
                    LogEvent(TraceEventType.Error, string.Format(CultureInfo.CurrentCulture, SR.FailedToCreateTraceSource, e), true);
                }
                else
                {
                    TraceSource = null;
                    LogEvent(TraceEventType.Error, string.Format(CultureInfo.CurrentCulture, SR.FailedToInitializeTraceSource, e), true);
                }
            }
        }

        internal static bool ShouldTrace(TraceEventType type)
        {
            return 0 != ((int)type & (int)Level) &&
                (TraceSource != null) &&
                (HaveListeners);
        }

        internal static bool ShouldCorrelate
        {
            get { return s_shouldCorrelate; }
        }

        internal static bool Critical
        {
            get { return s_shouldTraceCritical; }
        }

        internal static bool Error
        {
            get { return s_shouldTraceError; }
        }

        internal static bool Warning
        {
            get { return s_shouldTraceWarning; }
        }

        internal static bool Information
        {
            get { return s_shouldTraceInformation; }
        }

        internal static bool Verbose
        {
            get { return s_shouldTraceVerbose; }
        }

        internal static void TraceEvent(TraceEventType type, string code, string description)
        {
            TraceEvent(type, code, description, null, null, ref EmptyGuid, false, null);
        }

        internal static void TraceEvent(TraceEventType type, string code, string description, TraceRecord trace)
        {
            TraceEvent(type, code, description, trace, null, ref EmptyGuid, false, null);
        }

        internal static void TraceEvent(TraceEventType type, string code, string description, TraceRecord trace, Exception exception)
        {
            TraceEvent(type, code, description, trace, exception, ref EmptyGuid, false, null);
        }

        internal static void TraceEvent(TraceEventType type, string code, string description, TraceRecord trace, Exception exception, ref Guid activityId, bool emitTransfer, object source)
        {
            if (ShouldTrace(type))
            {
                using (Activity.CreateActivity(activityId, emitTransfer))
                {
                    XPathNavigator navigator = BuildTraceString(type, code, description, trace, exception, source);
                    try
                    {
                        TraceSource.TraceData(type, 0, navigator);
                        if (s_calledShutdown)
                        {
                            TraceSource.Flush();
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        string traceString = SR.Format(SR.TraceFailure,
                            type.ToString(),
                            code,
                            description,
                            source == null ? string.Empty : CreateSourceString(source));
                        LogTraceFailure(traceString, e);
                    }
                }
            }
        }

        internal static void TraceAndLogEvent(TraceEventType type, string code, string description, TraceRecord trace, Exception exception, ref Guid activityId, object source)
        {
            bool shouldTrace = ShouldTrace(type);
            string traceString = null;
            try
            {
                LogEvent(type, code, description, trace, exception, source);

                if (shouldTrace)
                {
                    TraceEvent(type, code, description, trace, exception, ref activityId, false, source);
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception e)
            {
                LogTraceFailure(traceString, e);
            }
        }

        internal static void TraceTransfer(Guid newId)
        {
            Guid oldId = GetActivityId();
            if (ShouldCorrelate && newId != oldId)
            {
                if (HaveListeners)
                {
                    try
                    {
                        if (newId != oldId)
                        {
                            //TraceSource.TraceTransfer(0, null, newId);
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        LogTraceFailure(null, e);
                    }
                }
            }
        }

        private static AsyncLocal<Guid> s_activityId = new AsyncLocal<Guid>();

        // TODO: Add back full ActivityID support (e.g. Trace.CorrelationManager.ActivityId) if/when it's supported in .NET Core.
        internal static Guid GetActivityId()
        {
            return s_activityId.Value;
        }

        internal static void GetActivityId(ref Guid guid)
        {
            //If activity id propagation is disabled for performance, we return nothing avoiding access.
            if (ShouldCorrelate)
            {
                guid = GetActivityId();
            }
        }

        internal static void SetActivityId(Guid id)
        {
            s_activityId.Value = id;
        }

        private static string CreateSourceString(object source)
        {
            return source.GetType().ToString() + "/" + source.GetHashCode().ToString(CultureInfo.CurrentCulture);
        }

        private static void LogEvent(TraceEventType type, string code, string description, TraceRecord trace, Exception exception, object source)
        {
            StringBuilder traceString = new StringBuilder(SR.Format(SR.EventLogValue,
                ProcessName,
                ProcessId.ToString(CultureInfo.CurrentCulture),
                code,
                description));
            if (source != null)
            {
                traceString.AppendLine(SR.Format(SR.EventLogSourceValue, CreateSourceString(source)));
            }

            if (exception != null)
            {
                traceString.AppendLine(SR.Format(SR.EventLogExceptionValue, exception.ToString()));
            }

            if (trace != null)
            {
                traceString.AppendLine(SR.Format(SR.EventLogEventIdValue, trace.EventId));
                traceString.AppendLine(SR.Format(SR.EventLogTraceValue, trace.ToString()));
            }

            LogEvent(type, traceString.ToString(), false);
        }

        internal static void LogEvent(TraceEventType type, string message, bool addProcessInfo)
        {
            if (addProcessInfo)
            {
                message = string.Format(CultureInfo.CurrentCulture, "{0}: {1}\n{2}: {3}\n{4}", DiagnosticStrings.ProcessName, ProcessName, DiagnosticStrings.ProcessId, ProcessId, message);
            }

            LogEvent(type, message);
        }

        internal static void LogEvent(TraceEventType type, string message)
        {
            try
            {
                const int MaxEventLogLength = 8192;
                if (!string.IsNullOrEmpty(message) && message.Length >= MaxEventLogLength)
                {
                    message = message.Substring(0, MaxEventLogLength - 1);
                }
                //EventLog.WriteEntry(EventLogSourceName, message, EventLogEntryTypeFromEventType(type));
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch
            {
            }
        }

        private static string LookupSeverity(TraceEventType type)
        {
            int level = (int)type & (int)SourceLevels.Verbose;

            // TODO: Put back if/when TraceEventType has Start/Stop in .NET Core
            //if (((int)type & ((int)TraceEventType.Start | (int)TraceEventType.Stop)) != 0)
            //{
            //    level = (int)type;
            //}
            //else 

            if (level == 0)
            {
                level = (int)TraceEventType.Verbose;
            }
            return TraceEventTypeNames[level];
        }

        private static int TraceFailureCount
        {
            get { return s_traceFailureCount; }
            set { s_traceFailureCount = value; }
        }

        private static int TraceFailureThreshold
        {
            get { return s_traceFailureThreshold; }
            set { s_traceFailureThreshold = value; }
        }

        //log failure every traceFailureLogThreshold time, increase the threshold progressively
        private static void LogTraceFailure(string traceString, Exception e)
        {
            if (e != null)
            {
                traceString = string.Format(CultureInfo.CurrentCulture, SR.FailedToTraceEvent, e, traceString != null ? traceString : "");
            }
            lock (s_localSyncObject)
            {
                if (TraceFailureCount > TraceFailureThreshold)
                {
                    TraceFailureCount = 1;
                    TraceFailureThreshold *= 2;
                    LogEvent(TraceEventType.Error, traceString, true);
                }
                else
                {
                    TraceFailureCount++;
                }
            }
        }

        private static void ShutdownTracing()
        {
            if (null != TraceSource)
            {
                try
                {
                    if (Level != SourceLevels.Off)
                    {
                        if (Information)
                        {
                            Dictionary<string, string> values = new Dictionary<string, string>(3);
                            values["AppDomain.FriendlyName"] = string.Empty; // AppDomain.CurrentDomain.FriendlyName;
                            values["ProcessName"] = ProcessName;
                            values["ProcessId"] = ProcessId.ToString(CultureInfo.CurrentCulture);
                            TraceEvent(TraceEventType.Information, DiagnosticTraceCode.AppDomainUnload, SR.TraceCodeAppDomainUnloading,
                                new DictionaryTraceRecord(values), null, ref EmptyGuid, false, null);
                        }
                        s_calledShutdown = true;
                        TraceSource.Flush();
                    }
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    LogTraceFailure(null, exception);
                }
            }
        }

        private static void ExitOrUnloadEventHandler(object sender, EventArgs e)
        {
            ShutdownTracing();
        }

        private static XPathNavigator BuildTraceString(TraceEventType type,
                                      string code,
                                      string description,
                                      TraceRecord trace,
                                      Exception exception,
                                      object source)
        {
            return BuildTraceString(new PlainXmlWriter(), type, code, description, trace, exception, source);
        }

        private static XPathNavigator BuildTraceString(PlainXmlWriter xml,
                                       TraceEventType type,
                                       string code,
                                       string description,
                                       TraceRecord trace,
                                       Exception exception,
                                       object source)
        {
            xml.WriteStartElement(DiagnosticStrings.TraceRecordTag);
            xml.WriteAttributeString(DiagnosticStrings.NamespaceTag, TraceRecordVersion);
            xml.WriteAttributeString(DiagnosticStrings.SeverityTag, LookupSeverity(type));

            xml.WriteElementString(DiagnosticStrings.TraceCodeTag, code);
            xml.WriteElementString(DiagnosticStrings.DescriptionTag, description);
            xml.WriteElementString(DiagnosticStrings.AppDomain, s_appDomainFriendlyName);

            if (source != null)
            {
                xml.WriteElementString(DiagnosticStrings.SourceTag, CreateSourceString(source));
            }

            if (trace != null)
            {
                xml.WriteStartElement(DiagnosticStrings.ExtendedDataTag);
                xml.WriteAttributeString(DiagnosticStrings.NamespaceTag, trace.EventId);

                trace.WriteTo(xml);

                xml.WriteEndElement();
            }

            if (exception != null)
            {
                xml.WriteStartElement(DiagnosticStrings.ExceptionTag);
                AddExceptionToTraceString(xml, exception);
                xml.WriteEndElement();
            }

            xml.WriteEndElement();

            return xml.ToNavigator();
        }

        private static void AddExceptionToTraceString(XmlWriter xml, Exception exception)
        {
            xml.WriteElementString(DiagnosticStrings.ExceptionTypeTag, XmlEncode(exception.GetType().AssemblyQualifiedName));
            xml.WriteElementString(DiagnosticStrings.MessageTag, XmlEncode(exception.Message));
            xml.WriteElementString(DiagnosticStrings.StackTraceTag, XmlEncode(StackTraceString(exception)));
            xml.WriteElementString(DiagnosticStrings.ExceptionStringTag, XmlEncode(exception.ToString()));
            Win32Exception win32Exception = exception as Win32Exception;
            if (win32Exception != null)
            {
                xml.WriteElementString(DiagnosticStrings.NativeErrorCodeTag, win32Exception.NativeErrorCode.ToString("X", CultureInfo.InvariantCulture));
            }

            if (exception.Data != null && exception.Data.Count > 0)
            {
                xml.WriteStartElement(DiagnosticStrings.DataItemsTag);
                foreach (object dataItem in exception.Data.Keys)
                {
                    xml.WriteStartElement(DiagnosticStrings.DataTag);
                    //Fix for Watson bug CSDMain 136718 - Add the null check incase the value is null. Only if both the key and value are non null, 
                    //write out the xml elements corresponding to them
                    if (dataItem != null && exception.Data[dataItem] != null)
                    {
                        xml.WriteElementString(DiagnosticStrings.KeyTag, XmlEncode(dataItem.ToString()));
                        xml.WriteElementString(DiagnosticStrings.ValueTag, XmlEncode(exception.Data[dataItem].ToString()));
                    }
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            if (exception.InnerException != null)
            {
                xml.WriteStartElement(DiagnosticStrings.InnerExceptionTag);
                AddExceptionToTraceString(xml, exception.InnerException);
                xml.WriteEndElement();
            }
        }

        private static string StackTraceString(Exception exception)
        {
            string retval = exception.StackTrace;
            return string.IsNullOrEmpty(retval) ? Environment.StackTrace : retval;
        }

        //only used for exceptions, perf is not important
        internal static string XmlEncode(string text)
        {
            if (text == null)
            {
                return null;
            }

            int len = text.Length;
            StringBuilder encodedText = new StringBuilder(len + 8); //perf optimization, expecting no more than 2 > characters

            for (int i = 0; i < len; ++i)
            {
                char ch = text[i];
                switch (ch)
                {
                    case '<':
                        encodedText.Append("&lt;");
                        break;
                    case '>':
                        encodedText.Append("&gt;");
                        break;
                    case '&':
                        encodedText.Append("&amp;");
                        break;
                    default:
                        encodedText.Append(ch);
                        break;
                }
            }
            return encodedText.ToString();
        }
    }
}
