// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Transactions.Diagnostics
{
    /// <summary>
    /// Server side infrastructure file on the server for use
    /// by Indigo infrastructure classes
    ///
    /// DiagnosticTrace consists of static methods, properties and collections
    /// that can be accessed by Indigo infrastructure code to provide
    /// instrumentation.
    /// </summary>

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.Remoting.Messaging;
    using System.Security;
    using System.Text;
    using System.Threading;
    using System.Web;
    using System.Xml;
    using System.Xml.XPath;
    using System.ComponentModel;

    internal static class DiagnosticTrace
    {
        internal const string DefaultTraceListenerName = "Default";
        static TraceSource traceSource = null;
        static bool tracingEnabled = true;
        static bool haveListeners = false;
        static Dictionary<int, string> traceEventTypeNames;
        static object localSyncObject = new object();
        static int traceFailureCount = 0;
        static int traceFailureThreshold = 0;
        static SourceLevels level;
        static bool calledShutdown = false;
        static bool shouldCorrelate = false;
        static bool shouldTraceVerbose = false;
        static bool shouldTraceInformation = false;
        static bool shouldTraceWarning = false;
        static bool shouldTraceError = false;
        static bool shouldTraceCritical = false;
        internal static Guid EmptyGuid = Guid.Empty;
        static string AppDomainFriendlyName = null;

        const string subType = "";
        const string version = "1";

        const int traceFailureLogThreshold = 10;
        const string EventLogSourceName = ".NET Runtime";
        const string TraceSourceName = "System.Transactions";
        const string TraceRecordVersion = "http://schemas.microsoft.com/2004/10/E2ETraceEvent/TraceRecord";

        // System.Diagnostics.Process has a FullTrust link demand. We satisfy that FullTrust demand and do not leak the
        // Process object out of this call.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        static string ProcessName
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


        // System.Diagnostics.Process has a FullTrust link demand. We satisfy that FullTrust demand and do not leak the
        // Process object out of this call.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        static int ProcessId
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

        static TraceSource TraceSource
        {
            get
            {
                return DiagnosticTrace.traceSource;
            }

            set
            {
                DiagnosticTrace.traceSource = value;
            }
        }

        static Dictionary<int, string> TraceEventTypeNames
        {
            get
            {
                return DiagnosticTrace.traceEventTypeNames;
            }
        }

        static SourceLevels FixLevel(SourceLevels level)
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

            return (level & ~SourceLevels.Warning) != 0 ? level | SourceLevels.ActivityTracing : level;
        }

        static void SetLevel(SourceLevels level)
        {
            SourceLevels fixedLevel = FixLevel(level);
            DiagnosticTrace.level = fixedLevel;
            if (DiagnosticTrace.TraceSource != null)
            {
                DiagnosticTrace.TraceSource.Switch.Level = fixedLevel;
                DiagnosticTrace.shouldCorrelate = DiagnosticTrace.ShouldTrace(TraceEventType.Transfer);
                DiagnosticTrace.shouldTraceVerbose = DiagnosticTrace.ShouldTrace(TraceEventType.Verbose);
                DiagnosticTrace.shouldTraceInformation = DiagnosticTrace.ShouldTrace(TraceEventType.Information);
                DiagnosticTrace.shouldTraceWarning = DiagnosticTrace.ShouldTrace(TraceEventType.Warning);
                DiagnosticTrace.shouldTraceError = DiagnosticTrace.ShouldTrace(TraceEventType.Error);
                DiagnosticTrace.shouldTraceCritical = DiagnosticTrace.ShouldTrace(TraceEventType.Critical);
            }
        }

        static void SetLevelThreadSafe(SourceLevels level)
        {
            if (DiagnosticTrace.TracingEnabled && level != DiagnosticTrace.Level)
            {
                lock (DiagnosticTrace.localSyncObject)
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
                if (DiagnosticTrace.TraceSource != null && (DiagnosticTrace.TraceSource.Switch.Level != DiagnosticTrace.level))
                {
                    DiagnosticTrace.level = DiagnosticTrace.TraceSource.Switch.Level;
                }
                return DiagnosticTrace.level;
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
                return DiagnosticTrace.haveListeners;
            }
        }

        internal static bool TracingEnabled
        {
            get
            {
                return DiagnosticTrace.tracingEnabled && DiagnosticTrace.traceSource != null;
            }
        }

        static DiagnosticTrace()
        {
            // We own the resource and it hasn't been filled in yet.
            //needed for logging events to event log
            DiagnosticTrace.AppDomainFriendlyName = AppDomain.CurrentDomain.FriendlyName;
            DiagnosticTrace.traceEventTypeNames = new Dictionary<int, string>();
            // Initialize the values here to avoid bringing in unnecessary pages.
            // Address MB#20806
            DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Critical] = "Critical";
            DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Error] = "Error";
            DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Warning] = "Warning";
            DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Information] = "Information";
            DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Verbose] = "Verbose";
            DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Resume] = "Resume";
            DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Start] = "Start";
            DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Stop] = "Stop";
            DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Suspend] = "Suspend";
            DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Transfer] = "Transfer";

#if DEBUG
            // The following asserts are established to make sure that
            // the strings we have above continue to be correct. Any failures
            // should be discoverable during development time since this
            // code is in the main path.
            Debug.Assert(string.Equals(DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Critical], TraceEventType.Critical.ToString(), StringComparison.Ordinal));
            Debug.Assert(string.Equals(DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Error], TraceEventType.Error.ToString(), StringComparison.Ordinal));
            Debug.Assert(string.Equals(DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Warning], TraceEventType.Warning.ToString(), StringComparison.Ordinal));
            Debug.Assert(string.Equals(DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Information], TraceEventType.Information.ToString(), StringComparison.Ordinal));
            Debug.Assert(string.Equals(DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Verbose], TraceEventType.Verbose.ToString(), StringComparison.Ordinal));
            Debug.Assert(string.Equals(DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Resume], TraceEventType.Resume.ToString(), StringComparison.Ordinal));
            Debug.Assert(string.Equals(DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Start], TraceEventType.Start.ToString(), StringComparison.Ordinal));
            Debug.Assert(string.Equals(DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Stop], TraceEventType.Stop.ToString(), StringComparison.Ordinal));
            Debug.Assert(string.Equals(DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Suspend], TraceEventType.Suspend.ToString(), StringComparison.Ordinal));
            Debug.Assert(string.Equals(DiagnosticTrace.traceEventTypeNames[(int)TraceEventType.Transfer], TraceEventType.Transfer.ToString(), StringComparison.Ordinal));
#endif
            DiagnosticTrace.TraceFailureThreshold = DiagnosticTrace.traceFailureLogThreshold;
            DiagnosticTrace.TraceFailureCount = DiagnosticTrace.TraceFailureThreshold + 1;

            try
            {
                DiagnosticTrace.traceSource = new TraceSource(DiagnosticTrace.TraceSourceName, SourceLevels.Critical);
                AppDomain currentDomain = AppDomain.CurrentDomain;

                if (DiagnosticTrace.TraceSource.Switch.ShouldTrace(TraceEventType.Critical))
                {
                    currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);
                }
                currentDomain.DomainUnload += new EventHandler(ExitOrUnloadEventHandler);
                currentDomain.ProcessExit += new EventHandler(ExitOrUnloadEventHandler);
                DiagnosticTrace.haveListeners = DiagnosticTrace.TraceSource.Listeners.Count > 0;
                DiagnosticTrace.SetLevel(DiagnosticTrace.TraceSource.Switch.Level);
            }
            catch (System.Configuration.ConfigurationErrorsException)
            {
                throw;
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (StackOverflowException)
            {
                throw;
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (DiagnosticTrace.TraceSource == null)
                {
                    LogEvent(TraceEventType.Error, String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.FailedToCreateTraceSource), e), true);
                }
                else
                {
                    DiagnosticTrace.TraceSource = null;
                    LogEvent(TraceEventType.Error, String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.FailedToInitializeTraceSource), e), true);
                }
            }
        }

        internal static bool ShouldTrace(TraceEventType type)
        {
            return 0 != ((int)type & (int)DiagnosticTrace.Level) &&
                (DiagnosticTrace.TraceSource != null) &&
                (DiagnosticTrace.HaveListeners);
        }

        internal static bool ShouldCorrelate
        {
            get { return DiagnosticTrace.shouldCorrelate; }
        }

        internal static bool Critical
        {
            get { return DiagnosticTrace.shouldTraceCritical; }
        }

        internal static bool Error
        {
            get { return DiagnosticTrace.shouldTraceError; }
        }

        internal static bool Warning
        {
            get { return DiagnosticTrace.shouldTraceWarning; }
        }

        internal static bool Information
        {
            get { return DiagnosticTrace.shouldTraceInformation; }
        }

        internal static bool Verbose
        {
            get { return DiagnosticTrace.shouldTraceVerbose; }
        }

        static internal void TraceEvent(TraceEventType type, string code, string description)
        {
            DiagnosticTrace.TraceEvent(type, code, description, null, null, ref DiagnosticTrace.EmptyGuid, false, null);
        }

        static internal void TraceEvent(TraceEventType type, string code, string description, TraceRecord trace)
        {
            DiagnosticTrace.TraceEvent(type, code, description, trace, null, ref DiagnosticTrace.EmptyGuid, false, null);
        }

        static internal void TraceEvent(TraceEventType type, string code, string description, TraceRecord trace, Exception exception)
        {
            DiagnosticTrace.TraceEvent(type, code, description, trace, exception, ref DiagnosticTrace.EmptyGuid, false, null);
        }

        static internal void TraceEvent(TraceEventType type, string code, string description, TraceRecord trace, Exception exception, ref Guid activityId, bool emitTransfer, object source)
        {
#if DEBUG
            Debug.Assert(exception == null || type <= TraceEventType.Information);
            Debug.Assert(!string.IsNullOrEmpty(description), "All TraceCodes should have a description");
#endif
            if (DiagnosticTrace.ShouldTrace(type))
            {
                using (Activity.CreateActivity(activityId, emitTransfer))
                {
                    XPathNavigator navigator = BuildTraceString(type, code, description, trace, exception, source);
                    try
                    {
                        DiagnosticTrace.TraceSource.TraceData(type, 0, navigator);
                        if (DiagnosticTrace.calledShutdown)
                        {
                            DiagnosticTrace.TraceSource.Flush();
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch (StackOverflowException)
                    {
                        throw;
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        string traceString = SR.GetString(SR.TraceFailure,
                            type.ToString(),
                            code,
                            description,
                            source == null ? string.Empty : DiagnosticTrace.CreateSourceString(source));
                        LogTraceFailure(traceString, e);
                    }
                }
            }
        }

        static internal void TraceAndLogEvent(TraceEventType type, string code, string description, TraceRecord trace, Exception exception, ref Guid activityId, object source)
        {
            bool shouldTrace = DiagnosticTrace.ShouldTrace(type);
            string traceString = null;
            try
            {
                LogEvent(type, code, description, trace, exception, source);

                if (shouldTrace)
                {
                    DiagnosticTrace.TraceEvent(type, code, description, trace, exception, ref activityId, false, source);
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (StackOverflowException)
            {
                throw;
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                LogTraceFailure(traceString, e);
            }
        }

        static internal void TraceTransfer(Guid newId)
        {
            Guid oldId = DiagnosticTrace.GetActivityId();
            if (DiagnosticTrace.ShouldCorrelate && newId != oldId)
            {
                if (DiagnosticTrace.HaveListeners)
                {
                    try
                    {
                        if (newId != oldId)
                        {
                            DiagnosticTrace.TraceSource.TraceTransfer(0, null, newId);
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        throw;
                    }
                    catch (StackOverflowException)
                    {
                        throw;
                    }
                    catch (ThreadAbortException)
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        static internal Guid GetActivityId()
        {
            object id = Trace.CorrelationManager.ActivityId;
            return id == null ? Guid.Empty : (Guid)id;
        }

        static internal void GetActivityId(ref Guid guid)
        {
            //If activity id propagation is disabled for performance, we return nothing avoiding CallContext access.
            if (DiagnosticTrace.ShouldCorrelate)
            {
                guid = DiagnosticTrace.GetActivityId();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        static internal void SetActivityId(Guid id)
        {
            Trace.CorrelationManager.ActivityId = id;
        }

        static string CreateSourceString(object source)
        {
            return source.GetType().ToString() + "/" + source.GetHashCode().ToString(CultureInfo.CurrentCulture);
        }

        static void LogEvent(TraceEventType type, string code, string description, TraceRecord trace, Exception exception, object source)
        {
            StringBuilder traceString = new StringBuilder(SR.GetString(SR.EventLogValue,
                DiagnosticTrace.ProcessName,
                DiagnosticTrace.ProcessId.ToString(CultureInfo.CurrentCulture),
                code,
                description));
            if (source != null)
            {
                traceString.AppendLine(SR.GetString(SR.EventLogSourceValue, DiagnosticTrace.CreateSourceString(source)));
            }

            if (exception != null)
            {
                traceString.AppendLine(SR.GetString(SR.EventLogExceptionValue, exception.ToString()));
            }

            if (trace != null)
            {
                traceString.AppendLine(SR.GetString(SR.EventLogEventIdValue, trace.EventId));
                traceString.AppendLine(SR.GetString(SR.EventLogTraceValue, trace.ToString()));
            }

            LogEvent(type, traceString.ToString(), false);
        }

        static internal void LogEvent(TraceEventType type, string message, bool addProcessInfo)
        {
            if (addProcessInfo)
            {
                message = String.Format(CultureInfo.CurrentCulture, "{0}: {1}\n{2}: {3}\n{4}", DiagnosticStrings.ProcessName, DiagnosticTrace.ProcessName, DiagnosticStrings.ProcessId, DiagnosticTrace.ProcessId, message);
            }

            LogEvent(type, message);
        }

        static internal void LogEvent(TraceEventType type, string message)
        {
            try
            {
                const int MaxEventLogLength = 8192;
                if (!string.IsNullOrEmpty(message) && message.Length >= MaxEventLogLength)
                {
                    message = message.Substring(0, MaxEventLogLength - 1);
                }
                EventLog.WriteEntry(DiagnosticTrace.EventLogSourceName, message, EventLogEntryTypeFromEventType(type));
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (StackOverflowException)
            {
                throw;
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch
            {
            }
        }

        static string LookupSeverity(TraceEventType type)
        {
            int level = (int)type & (int)SourceLevels.Verbose;
            if (((int)type & ((int)TraceEventType.Start | (int)TraceEventType.Stop)) != 0)
            {
                level = (int)type;
            }
            else if (level == 0)
            {
                level = (int)TraceEventType.Verbose;
            }
            return DiagnosticTrace.TraceEventTypeNames[level];
        }

        static int TraceFailureCount
        {
            get { return DiagnosticTrace.traceFailureCount; }
            set { DiagnosticTrace.traceFailureCount = value; }
        }

        static int TraceFailureThreshold
        {
            get { return DiagnosticTrace.traceFailureThreshold; }
            set { DiagnosticTrace.traceFailureThreshold = value; }
        }

        //log failure every traceFailureLogThreshold time, increase the threshold progressively
        static void LogTraceFailure(string traceString, Exception e)
        {
            if (e != null)
            {
                traceString = String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.FailedToTraceEvent), e, traceString != null ? traceString : "");
            }
            lock (DiagnosticTrace.localSyncObject)
            {
                if (DiagnosticTrace.TraceFailureCount > DiagnosticTrace.TraceFailureThreshold)
                {
                    DiagnosticTrace.TraceFailureCount = 1;
                    DiagnosticTrace.TraceFailureThreshold *= 2;
                    LogEvent(TraceEventType.Error, traceString, true);
                }
                else
                {
                    DiagnosticTrace.TraceFailureCount++;
                }
            }
        }

        static void ShutdownTracing()
        {
            if (null != DiagnosticTrace.TraceSource)
            {
                try
                {
                    if (DiagnosticTrace.Level != SourceLevels.Off)
                    {
                        if (DiagnosticTrace.Information)
                        {
                            Dictionary<string, string> values = new Dictionary<string, string>(3);
                            values["AppDomain.FriendlyName"] = AppDomain.CurrentDomain.FriendlyName;
                            values["ProcessName"] = DiagnosticTrace.ProcessName;
                            values["ProcessId"] = DiagnosticTrace.ProcessId.ToString(CultureInfo.CurrentCulture);
                            DiagnosticTrace.TraceEvent(TraceEventType.Information, DiagnosticTraceCode.AppDomainUnload, SR.GetString(SR.TraceCodeAppDomainUnloading),
                                new DictionaryTraceRecord(values), null, ref DiagnosticTrace.EmptyGuid, false, null);
                        }
                        DiagnosticTrace.calledShutdown = true;
                        DiagnosticTrace.TraceSource.Flush();
                    }
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
                catch (StackOverflowException)
                {
                    throw;
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    LogTraceFailure(null, exception);
                }
            }
        }

        static void ExitOrUnloadEventHandler(object sender, EventArgs e)
        {
            ShutdownTracing();
        }

        static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            TraceEvent(TraceEventType.Critical, DiagnosticTraceCode.UnhandledException, SR.GetString(SR.UnhandledException), null, e, ref DiagnosticTrace.EmptyGuid, false, null);
            ShutdownTracing();
        }

        static XPathNavigator BuildTraceString(TraceEventType type,
                                      string code,
                                      string description,
                                      TraceRecord trace,
                                      Exception exception,
                                      object source)
        {
            return DiagnosticTrace.BuildTraceString(new PlainXmlWriter(), type, code, description, trace, exception, source);
        }

        static XPathNavigator BuildTraceString(PlainXmlWriter xml,
                                       TraceEventType type,
                                       string code,
                                       string description,
                                       TraceRecord trace,
                                       Exception exception,
                                       object source)
        {
            xml.WriteStartElement(DiagnosticStrings.TraceRecordTag);
            xml.WriteAttributeString(DiagnosticStrings.NamespaceTag, DiagnosticTrace.TraceRecordVersion);
            xml.WriteAttributeString(DiagnosticStrings.SeverityTag, DiagnosticTrace.LookupSeverity(type));

            xml.WriteElementString(DiagnosticStrings.TraceCodeTag, code);
            xml.WriteElementString(DiagnosticStrings.DescriptionTag, description);
            xml.WriteElementString(DiagnosticStrings.AppDomain, DiagnosticTrace.AppDomainFriendlyName);

            if (source != null)
            {
                xml.WriteElementString(DiagnosticStrings.SourceTag, DiagnosticTrace.CreateSourceString(source));
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
                DiagnosticTrace.AddExceptionToTraceString(xml, exception);
                xml.WriteEndElement();
            }

            xml.WriteEndElement();

            return xml.ToNavigator();
        }

        static void AddExceptionToTraceString(XmlWriter xml, Exception exception)
        {
            xml.WriteElementString(DiagnosticStrings.ExceptionTypeTag, DiagnosticTrace.XmlEncode(exception.GetType().AssemblyQualifiedName));
            xml.WriteElementString(DiagnosticStrings.MessageTag, DiagnosticTrace.XmlEncode(exception.Message));
            xml.WriteElementString(DiagnosticStrings.StackTraceTag, DiagnosticTrace.XmlEncode(DiagnosticTrace.StackTraceString(exception)));
            xml.WriteElementString(DiagnosticStrings.ExceptionStringTag, DiagnosticTrace.XmlEncode(exception.ToString()));
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
                        xml.WriteElementString(DiagnosticStrings.KeyTag, DiagnosticTrace.XmlEncode(dataItem.ToString()));
                        xml.WriteElementString(DiagnosticStrings.ValueTag, DiagnosticTrace.XmlEncode(exception.Data[dataItem].ToString()));
                    }
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            if (exception.InnerException != null)
            {
                xml.WriteStartElement(DiagnosticStrings.InnerExceptionTag);
                DiagnosticTrace.AddExceptionToTraceString(xml, exception.InnerException);
                xml.WriteEndElement();
            }
        }

        static string StackTraceString(Exception exception)
        {
            string retval = exception.StackTrace;
            if (string.IsNullOrEmpty(retval))
            {
                // This means that the exception hasn't been thrown yet. We need to manufacture the stack then.
                StackTrace stackTrace = new StackTrace(true);
                System.Diagnostics.StackFrame[] stackFrames = stackTrace.GetFrames();
                int numFramesToSkip = 0;
                foreach (System.Diagnostics.StackFrame stackFrame in stackFrames)
                {
                    Type declaringType = stackFrame.GetMethod().DeclaringType;
                    if (declaringType == typeof(DiagnosticTrace))
                    {
                        ++numFramesToSkip;
                    }
                    else
                    {
                        break;
                    }
                }
                stackTrace = new StackTrace(numFramesToSkip);
                retval = stackTrace.ToString();
            }
            return retval;
        }

        //only used for exceptions, perf is not important
        static internal string XmlEncode(string text)
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

        //<summary>
        // Converts incompatible serverity enumeration TraceEvetType into EventLogEntryType
        //</summary>
        static EventLogEntryType EventLogEntryTypeFromEventType(TraceEventType type)
        {
            EventLogEntryType retval = EventLogEntryType.Information;
            switch (type)
            {
                case TraceEventType.Critical:
                case TraceEventType.Error:
                    retval = EventLogEntryType.Error;
                    break;
                case TraceEventType.Warning:
                    retval = EventLogEntryType.Warning;
                    break;
            }
            return retval;
        }
    }
}
