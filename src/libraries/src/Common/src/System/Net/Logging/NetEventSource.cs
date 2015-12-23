// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Tracing;
using System.Globalization;

namespace System.Net
{
    [EventSource(Name = "Microsoft-System-Net",
        Guid = "501a994a-eb63-5415-9af1-1b031260f16c")]
    internal sealed class NetEventSource : EventSource
    {
        private const int FUNCTIONSTART_ID = 1;
        private const int FUNCTIONSTOP_ID = 2;
        private const int CRITICALEXCEPTION_ID = 3;
        private const int CRITICALERROR_ID = 4;

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
            s_log.FunctionStart(callerName,
                                callerHash,
                                LoggingHash.GetObjectName(method),
                                parametersName,
                                parametersHash,
                                componentType);
        }

        [Event(FUNCTIONSTART_ID, Keywords = Keywords.FunctionEntryExit,
    Level = EventLevel.Verbose, Message = "[{5}] {0}#{1}::{2}({3}#{4})")]
        internal unsafe void FunctionStart(string callerName,
                                            int callerHash,
                                            string method,
                                            string parametersName,
                                            int parametersHash,
                                            ComponentType componentType)
        {
            const int SIZEDATA = 6;
            fixed (char* arg1Ptr = callerName, arg2Ptr = method, arg3Ptr = parametersName)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];

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

                WriteEventCore(FUNCTIONSTART_ID, SIZEDATA, dataDesc);
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

        [Event(FUNCTIONSTOP_ID, Keywords = Keywords.FunctionEntryExit,
    Level = EventLevel.Verbose, Message = "[{5}] {0}#{1}::{2}({3}#{4})")]
        internal unsafe void FunctionStop(string callerName,
                                            int callerHash,
                                            string method,
                                            string parametersName,
                                            int parametersHash,
                                            ComponentType componentType)
        {
            const int SIZEDATA = 6;
            fixed (char* arg1Ptr = callerName, arg2Ptr = method, arg3Ptr = parametersName)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];

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

                WriteEventCore(FUNCTIONSTOP_ID, SIZEDATA, dataDesc);
            }
        }

        [NonEvent]
        internal static void Exception(ComponentType componentType, object obj, string method, Exception e)
        {
            s_log.CriticalException(LoggingHash.GetObjectName(obj),
                                                    LoggingHash.GetObjectName(method),
                                                    LoggingHash.GetObjectName(e.Message),
                                                    LoggingHash.HashInt(obj),
                                                    LoggingHash.GetObjectName(e.StackTrace),
                                                    componentType);
        }

        [Event(CRITICALEXCEPTION_ID, Keywords = Keywords.Default,
    Level = EventLevel.Critical)]
        internal unsafe void CriticalException(string objName, string method, string message, int objHash, string stackTrace, ComponentType componentType)
        {
            const int SIZEDATA = 6;
            fixed (char* arg1Ptr = objName, arg2Ptr = method, arg3Ptr = message, arg4Ptr = stackTrace)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];

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
                WriteEventCore(CRITICALEXCEPTION_ID, SIZEDATA, dataDesc);
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
            s_log.CriticalError(LoggingHash.GetObjectName(msg),
                                LoggingHash.GetObjectName(method),
                                LoggingHash.GetObjectName(obj),
                                LoggingHash.HashInt(obj),
                                componentType);
        }

        [Event(CRITICALERROR_ID, Keywords = Keywords.Default,
Level = EventLevel.Critical)]
        internal unsafe void CriticalError(string message, string method, string objName, int objHash, ComponentType componentType)
        {
            const int SIZEDATA = 5;
            fixed (char* arg1Ptr = message, arg2Ptr = method, arg3Ptr = objName)
            {
                EventData* dataDesc = stackalloc EventSource.EventData[SIZEDATA];

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
                WriteEventCore(CRITICALERROR_ID, SIZEDATA, dataDesc);
            }
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
            Security
        }
    }
}
