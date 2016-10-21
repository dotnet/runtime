// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using System.Globalization;

namespace System.Net
{
    // TODO: Issue #5144: This class should not change or it can introduce incompatibility issues.
    [EventSource(Name = "Microsoft-System-Net",
        Guid = "501a994a-eb63-5415-9af1-1b031260f16c")]
    internal sealed class NetEventSource : EventSource
    {
        private const int FunctionStartId = 1;
        private const int FunctionStopId = 2;
        private const int CriticalExceptionId = 3;
        private const int CriticalErrorId = 4;

        private readonly static NetEventSource s_log = new NetEventSource();
        private NetEventSource() { }
        public static NetEventSource Log
        {
            get
            {
                return s_log;
            }
        }

        [NonEvent]
        internal static void Enter(ComponentType componentType, object obj, string method, object paramObject)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }

            string callerName;
            int callerHash;
            string parametersName = "";
            int parametersHash = 0;
            if (obj is string)
            {
                callerName = obj as string;
                callerHash = 0;
            }
            else
            {
                callerName = LoggingHash.GetObjectName(obj);
                callerHash = LoggingHash.HashInt(obj);
            }

            if (paramObject is string)
            {
                parametersName = paramObject as string;
                parametersHash = 0;
            }
            else if (paramObject != null)
            {
                parametersName = LoggingHash.GetObjectName(paramObject);
                parametersHash = LoggingHash.HashInt(paramObject);
            }

            s_log.FunctionStart(
                            callerName,
                            callerHash,
                            LoggingHash.GetObjectName(method),
                            parametersName,
                            parametersHash,
                            componentType);
        }

        [Event(FunctionStartId, Keywords = Keywords.FunctionEntryExit,
            Level = EventLevel.Verbose, Message = "[{5}] {0}#{1}::{2}({3}#{4})")]
        internal unsafe void FunctionStart(
                                        string callerName,
                                        int callerHash,
                                        string method,
                                        string parametersName,
                                        int parametersHash,
                                        ComponentType componentType)
        {
            const int SizeData = 6;
            fixed (char* arg1Ptr = callerName, arg2Ptr = method, arg3Ptr = parametersName)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];

                dataDesc[0].DataPointer = (IntPtr)arg1Ptr;
                dataDesc[0].Size = (callerName.Length + 1) * sizeof(char); // Size in bytes, including a null terminator. 
                dataDesc[1].DataPointer = (IntPtr)(&callerHash);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[2].Size = (method.Length + 1) * sizeof(char);
                dataDesc[3].DataPointer = (IntPtr)(arg3Ptr);
                dataDesc[3].Size = (parametersName.Length + 1) * sizeof(char);
                dataDesc[4].DataPointer = (IntPtr)(&parametersHash);
                dataDesc[4].Size = sizeof(int);
                dataDesc[5].DataPointer = (IntPtr)(&componentType);
                dataDesc[5].Size = sizeof(int);

                WriteEventCore(FunctionStartId, SizeData, dataDesc);
            }
        }

        [NonEvent]
        internal static void Exit(ComponentType componentType, object obj, string method, object retObject)
        {
            if (!s_log.IsEnabled())
            {
                return;
            }
            string callerName;
            int callerHash;
            string parametersName = "";
            int parametersHash = 0;
            if (obj is string)
            {
                callerName = obj as string;
                callerHash = 0;
            }
            else
            {
                callerName = LoggingHash.GetObjectName(obj);
                callerHash = LoggingHash.HashInt(obj);
            }

            if (retObject is string)
            {
                parametersName = retObject as string;
                parametersHash = 0;
            }

            else if (retObject != null)
            {
                parametersName = LoggingHash.GetObjectName(retObject);
                parametersHash = LoggingHash.HashInt(retObject);
            }
            s_log.FunctionStop(callerName,
                    callerHash,
                    LoggingHash.GetObjectName(method),
                    parametersName,
                    parametersHash,
                    componentType);
        }

        [Event(FunctionStopId, Keywords = Keywords.FunctionEntryExit,
            Level = EventLevel.Verbose, Message = "[{5}] {0}#{1}::{2}({3}#{4})")]
        internal unsafe void FunctionStop(
                                        string callerName,
                                        int callerHash,
                                        string method,
                                        string parametersName,
                                        int parametersHash,
                                        ComponentType componentType)
        {
            const int SizeData = 6;
            fixed (char* arg1Ptr = callerName, arg2Ptr = method, arg3Ptr = parametersName)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];

                dataDesc[0].DataPointer = (IntPtr)arg1Ptr;
                dataDesc[0].Size = (callerName.Length + 1) * sizeof(char); // Size in bytes, including a null terminator. 
                dataDesc[1].DataPointer = (IntPtr)(&callerHash);
                dataDesc[1].Size = sizeof(int);
                dataDesc[2].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[2].Size = (method.Length + 1) * sizeof(char);
                dataDesc[3].DataPointer = (IntPtr)(arg3Ptr);
                dataDesc[3].Size = (parametersName.Length + 1) * sizeof(char);
                dataDesc[4].DataPointer = (IntPtr)(&parametersHash);
                dataDesc[4].Size = sizeof(int);
                dataDesc[5].DataPointer = (IntPtr)(&componentType);
                dataDesc[5].Size = sizeof(int);

                WriteEventCore(FunctionStopId, SizeData, dataDesc);
            }
        }

        [NonEvent]
        internal static void Exception(ComponentType componentType, object obj, string method, Exception e)
        {
            s_log.CriticalException(
                                LoggingHash.GetObjectName(obj),
                                LoggingHash.GetObjectName(method),
                                LoggingHash.GetObjectName(e.Message),
                                LoggingHash.HashInt(obj),
                                LoggingHash.GetObjectName(e.StackTrace),
                                componentType);
        }

        [Event(CriticalExceptionId, Keywords = Keywords.Default,
            Level = EventLevel.Critical)]
        internal unsafe void CriticalException(string objName, string method, string message, int objHash, string stackTrace, ComponentType componentType)
        {
            const int SizeData = 6;
            fixed (char* arg1Ptr = objName, arg2Ptr = method, arg3Ptr = message, arg4Ptr = stackTrace)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];

                dataDesc[0].DataPointer = (IntPtr)arg1Ptr;
                dataDesc[0].Size = (objName.Length + 1) * sizeof(char); // Size in bytes, including a null terminator. 
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (method.Length + 1) * sizeof(char);
                dataDesc[2].DataPointer = (IntPtr)(arg3Ptr);
                dataDesc[2].Size = (message.Length + 1) * sizeof(char);
                dataDesc[3].DataPointer = (IntPtr)(&objHash);
                dataDesc[3].Size = sizeof(int);
                dataDesc[4].DataPointer = (IntPtr)(arg4Ptr);
                dataDesc[4].Size = (stackTrace.Length + 1) * sizeof(char);
                dataDesc[5].DataPointer = (IntPtr)(&componentType);
                dataDesc[5].Size = sizeof(int);
                WriteEventCore(CriticalExceptionId, SizeData, dataDesc);
            }
        }

        [NonEvent]
        internal static void PrintError(ComponentType componentType, string msg)
        {
            if (msg == null)
            {
                return;
            }

            s_log.CriticalError(LoggingHash.GetObjectName(msg), "", "", 0, componentType);
        }

        [NonEvent]
        internal static void PrintError(ComponentType componentType, object obj, string method, string msg)
        {
            s_log.CriticalError(
                            LoggingHash.GetObjectName(msg),
                            LoggingHash.GetObjectName(method),
                            LoggingHash.GetObjectName(obj),
                            LoggingHash.HashInt(obj),
                            componentType);
        }

        [Event(CriticalErrorId, Keywords = Keywords.Default,
            Level = EventLevel.Critical)]
        internal unsafe void CriticalError(string message, string method, string objName, int objHash, ComponentType componentType)
        {
            const int SizeData = 5;
            fixed (char* arg1Ptr = message, arg2Ptr = method, arg3Ptr = objName)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SizeData];

                dataDesc[0].DataPointer = (IntPtr)arg1Ptr;
                dataDesc[0].Size = (message.Length + 1) * sizeof(char); // Size in bytes, including a null terminator. 
                dataDesc[1].DataPointer = (IntPtr)(arg2Ptr);
                dataDesc[1].Size = (method.Length + 1) * sizeof(char);
                dataDesc[2].DataPointer = (IntPtr)(arg3Ptr);
                dataDesc[2].Size = (objName.Length + 1) * sizeof(char);
                dataDesc[3].DataPointer = (IntPtr)(&objHash);
                dataDesc[3].Size = sizeof(int);
                dataDesc[4].DataPointer = (IntPtr)(&componentType);
                dataDesc[4].Size = sizeof(int);
                WriteEventCore(CriticalErrorId, SizeData, dataDesc);
            }
        }

        // TODO: Issue #12685
        [NonEvent]
        internal static void PrintInfo(ComponentType componentType, object obj, string msg)
        {
        }

        // TODO: Issue #12685
        [NonEvent]
        internal static void PrintInfo(ComponentType componentType, object obj, string method, string msg)
        {
        }

        // TODO: Issue #12685
        [NonEvent]
        internal static void PrintWarning(ComponentType componentType, string msg)
        {
        }

        // TODO: Issue #12685
        [NonEvent]
        internal static void Associate(ComponentType componentType, object obj1, object obj2)
        {
        }

        public class Keywords
        {
            public const EventKeywords Default = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
            public const EventKeywords FunctionEntryExit = (EventKeywords)0x0004;
        }

        public enum ComponentType
        {
            Socket,
            Http,
            WebSocket,
            Security,
            NetworkInformation,
            Requests,
            Web
        }
    }
}
