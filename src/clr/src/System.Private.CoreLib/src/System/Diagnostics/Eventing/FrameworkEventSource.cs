// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// ResourcesEtwProvider.cs
//
//
// Managed event source for things that can version with MSCORLIB.  
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing
{
    // To use the framework provider
    // 
    //     \\clrmain\tools\Perfmonitor /nokernel /noclr /provider:8E9F5090-2D75-4d03-8A81-E5AFBF85DAF1 start
    //     Run run your app
    //     \\clrmain\tools\Perfmonitor stop
    //     \\clrmain\tools\Perfmonitor print
    //
    // This will produce an XML file, where each event is pretty-printed with all its arguments nicely parsed.
    //
    // [FriendAccessAllowed]
    [EventSource(Guid = "8E9F5090-2D75-4d03-8A81-E5AFBF85DAF1", Name = "System.Diagnostics.Eventing.FrameworkEventSource")]
    sealed internal class FrameworkEventSource : EventSource
    {
        // Defines the singleton instance for the Resources ETW provider
        public static readonly FrameworkEventSource Log = new FrameworkEventSource();

        // Keyword definitions.  These represent logical groups of events that can be turned on and off independently
        // Often each task has a keyword, but where tasks are determined by subsystem, keywords are determined by
        // usefulness to end users to filter.  Generally users don't mind extra events if they are not high volume
        // so grouping low volume events together in a single keywords is OK (users can post-filter by task if desired)
        public static class Keywords
        {
            public const EventKeywords Loader = (EventKeywords)0x0001; // This is bit 0
            public const EventKeywords ThreadPool = (EventKeywords)0x0002;
            public const EventKeywords NetClient = (EventKeywords)0x0004;
            //
            // This is a private event we do not want to expose to customers.  It is to be used for profiling
            // uses of dynamic type loading by ProjectN applications running on the desktop CLR
            //
            public const EventKeywords DynamicTypeUsage = (EventKeywords)0x0008;
            public const EventKeywords ThreadTransfer = (EventKeywords)0x0010;
        }

        /// <summary>ETW tasks that have start/stop events.</summary>
        public static class Tasks // this name is important for EventSource
        {
            /// <summary>Begin / End - GetResponse.</summary>
            public const EventTask GetResponse = (EventTask)1;
            /// <summary>Begin / End - GetRequestStream</summary>
            public const EventTask GetRequestStream = (EventTask)2;
            /// <summary>Send / Receive - begin transfer/end transfer</summary>
            public const EventTask ThreadTransfer = (EventTask)3;
        }

        public static class Opcodes
        {
            public const EventOpcode ReceiveHandled = (EventOpcode)11;
        }

        // This predicate is used by consumers of this class to deteremine if the class has actually been initialized,
        // and therefore if the public statics are available for use. This is typically not a problem... if the static
        // class constructor fails, then attempts to access the statics (or even this property) will result in a 
        // TypeInitializationException. However, that is not the case while the class loader is actually trying to construct
        // the TypeInitializationException instance to represent that failure, and some consumers of this class are on
        // that code path, specifically the resource manager. 
        public static bool IsInitialized
        {
            get
            {
                return Log != null;
            }
        }

        // The FrameworkEventSource GUID is {8E9F5090-2D75-4d03-8A81-E5AFBF85DAF1}
        private FrameworkEventSource() : base(new Guid(0x8e9f5090, 0x2d75, 0x4d03, 0x8a, 0x81, 0xe5, 0xaf, 0xbf, 0x85, 0xda, 0xf1), "System.Diagnostics.Eventing.FrameworkEventSource") { }

        // WriteEvent overloads (to avoid the "params" EventSource.WriteEvent

        // optimized for common signatures (used by the ThreadTransferSend/Receive events)
        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        private unsafe void WriteEvent(int eventId, long arg1, int arg2, string arg3, bool arg4, int arg5, int arg6)
        {
            if (IsEnabled())
            {
                if (arg3 == null) arg3 = "";
                fixed (char* string3Bytes = arg3)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[6];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 8;
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[1].Reserved = 0;
                    descrs[2].DataPointer = (IntPtr)string3Bytes;
                    descrs[2].Size = ((arg3.Length + 1) * 2);
                    descrs[2].Reserved = 0;
                    descrs[3].DataPointer = (IntPtr)(&arg4);
                    descrs[3].Size = 4;
                    descrs[3].Reserved = 0;
                    descrs[4].DataPointer = (IntPtr)(&arg5);
                    descrs[4].Size = 4;
                    descrs[4].Reserved = 0;
                    descrs[5].DataPointer = (IntPtr)(&arg6);
                    descrs[5].Size = 4;
                    descrs[5].Reserved = 0;
                    WriteEventCore(eventId, 6, descrs);
                }
            }
        }

        // optimized for common signatures (used by the ThreadTransferSend/Receive events)
        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        private unsafe void WriteEvent(int eventId, long arg1, int arg2, string arg3)
        {
            if (IsEnabled())
            {
                if (arg3 == null) arg3 = "";
                fixed (char* string3Bytes = arg3)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[3];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 8;
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[1].Reserved = 0;
                    descrs[2].DataPointer = (IntPtr)string3Bytes;
                    descrs[2].Size = ((arg3.Length + 1) * 2);
                    descrs[2].Reserved = 0;
                    WriteEventCore(eventId, 3, descrs);
                }
            }
        }

        // optimized for common signatures (used by the BeginGetResponse/BeginGetRequestStream events)
        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        private unsafe void WriteEvent(int eventId, long arg1, string arg2, bool arg3, bool arg4)
        {
            if (IsEnabled())
            {
                if (arg2 == null) arg2 = "";
                fixed (char* string2Bytes = arg2)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[4];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 8;
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)string2Bytes;
                    descrs[1].Size = ((arg2.Length + 1) * 2);
                    descrs[1].Reserved = 0;
                    descrs[2].DataPointer = (IntPtr)(&arg3);
                    descrs[2].Size = 4;
                    descrs[2].Reserved = 0;
                    descrs[3].DataPointer = (IntPtr)(&arg4);
                    descrs[3].Size = 4;
                    descrs[3].Reserved = 0;
                    WriteEventCore(eventId, 4, descrs);
                }
            }
        }

        // optimized for common signatures (used by the EndGetRequestStream event)
        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        private unsafe void WriteEvent(int eventId, long arg1, bool arg2, bool arg3)
        {
            if (IsEnabled())
            {
                EventSource.EventData* descrs = stackalloc EventSource.EventData[3];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[0].Reserved = 0;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 4;
                descrs[1].Reserved = 0;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 4;
                descrs[2].Reserved = 0;
                WriteEventCore(eventId, 3, descrs);
            }
        }

        // optimized for common signatures (used by the EndGetResponse event)
        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "This does not need to be correct when racing with other threads")]
        private unsafe void WriteEvent(int eventId, long arg1, bool arg2, bool arg3, int arg4)
        {
            if (IsEnabled())
            {
                EventSource.EventData* descrs = stackalloc EventSource.EventData[4];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[0].Reserved = 0;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 4;
                descrs[1].Reserved = 0;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 4;
                descrs[2].Reserved = 0;
                descrs[3].DataPointer = (IntPtr)(&arg4);
                descrs[3].Size = 4;
                descrs[3].Reserved = 0;
                WriteEventCore(eventId, 4, descrs);
            }
        }

        // ResourceManager Event Definitions 

        [Event(1, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerLookupStarted(string baseName, string mainAssemblyName, string cultureName)
        {
            WriteEvent(1, baseName, mainAssemblyName, cultureName);
        }

        [Event(2, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerLookingForResourceSet(string baseName, string mainAssemblyName, string cultureName)
        {
            if (IsEnabled())
                WriteEvent(2, baseName, mainAssemblyName, cultureName);
        }

        [Event(3, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerFoundResourceSetInCache(string baseName, string mainAssemblyName, string cultureName)
        {
            if (IsEnabled())
                WriteEvent(3, baseName, mainAssemblyName, cultureName);
        }

        // After loading a satellite assembly, we already have the ResourceSet for this culture in
        // the cache. This can happen if you have an assembly load callback that called into this
        // instance of the ResourceManager.
        [Event(4, Level = EventLevel.Warning, Keywords = Keywords.Loader)]
        public void ResourceManagerFoundResourceSetInCacheUnexpected(string baseName, string mainAssemblyName, string cultureName)
        {
            if (IsEnabled())
                WriteEvent(4, baseName, mainAssemblyName, cultureName);
        }

        // manifest resource stream lookup succeeded
        [Event(5, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerStreamFound(string baseName, string mainAssemblyName, string cultureName, string loadedAssemblyName, string resourceFileName)
        {
            if (IsEnabled())
                WriteEvent(5, baseName, mainAssemblyName, cultureName, loadedAssemblyName, resourceFileName);
        }

        // manifest resource stream lookup failed
        [Event(6, Level = EventLevel.Warning, Keywords = Keywords.Loader)]
        public void ResourceManagerStreamNotFound(string baseName, string mainAssemblyName, string cultureName, string loadedAssemblyName, string resourceFileName)
        {
            if (IsEnabled())
                WriteEvent(6, baseName, mainAssemblyName, cultureName, loadedAssemblyName, resourceFileName);
        }

        [Event(7, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerGetSatelliteAssemblySucceeded(string baseName, string mainAssemblyName, string cultureName, string assemblyName)
        {
            if (IsEnabled())
                WriteEvent(7, baseName, mainAssemblyName, cultureName, assemblyName);
        }

        [Event(8, Level = EventLevel.Warning, Keywords = Keywords.Loader)]
        public void ResourceManagerGetSatelliteAssemblyFailed(string baseName, string mainAssemblyName, string cultureName, string assemblyName)
        {
            if (IsEnabled())
                WriteEvent(8, baseName, mainAssemblyName, cultureName, assemblyName);
        }

        [Event(9, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerCaseInsensitiveResourceStreamLookupSucceeded(string baseName, string mainAssemblyName, string assemblyName, string resourceFileName)
        {
            if (IsEnabled())
                WriteEvent(9, baseName, mainAssemblyName, assemblyName, resourceFileName);
        }

        [Event(10, Level = EventLevel.Warning, Keywords = Keywords.Loader)]
        public void ResourceManagerCaseInsensitiveResourceStreamLookupFailed(string baseName, string mainAssemblyName, string assemblyName, string resourceFileName)
        {
            if (IsEnabled())
                WriteEvent(10, baseName, mainAssemblyName, assemblyName, resourceFileName);
        }

        // Could not access the manifest resource the assembly
        [Event(11, Level = EventLevel.Error, Keywords = Keywords.Loader)]
        public void ResourceManagerManifestResourceAccessDenied(string baseName, string mainAssemblyName, string assemblyName, string canonicalName)
        {
            if (IsEnabled())
                WriteEvent(11, baseName, mainAssemblyName, assemblyName, canonicalName);
        }

        // Neutral resources are sufficient for this culture. Skipping satellites
        [Event(12, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerNeutralResourcesSufficient(string baseName, string mainAssemblyName, string cultureName)
        {
            if (IsEnabled())
                WriteEvent(12, baseName, mainAssemblyName, cultureName);
        }

        [Event(13, Level = EventLevel.Warning, Keywords = Keywords.Loader)]
        public void ResourceManagerNeutralResourceAttributeMissing(string mainAssemblyName)
        {
            if (IsEnabled())
                WriteEvent(13, mainAssemblyName);
        }

        [Event(14, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerCreatingResourceSet(string baseName, string mainAssemblyName, string cultureName, string fileName)
        {
            if (IsEnabled())
                WriteEvent(14, baseName, mainAssemblyName, cultureName, fileName);
        }

        [Event(15, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerNotCreatingResourceSet(string baseName, string mainAssemblyName, string cultureName)
        {
            if (IsEnabled())
                WriteEvent(15, baseName, mainAssemblyName, cultureName);
        }

        [Event(16, Level = EventLevel.Warning, Keywords = Keywords.Loader)]
        public void ResourceManagerLookupFailed(string baseName, string mainAssemblyName, string cultureName)
        {
            if (IsEnabled())
                WriteEvent(16, baseName, mainAssemblyName, cultureName);
        }

        [Event(17, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerReleasingResources(string baseName, string mainAssemblyName)
        {
            if (IsEnabled())
                WriteEvent(17, baseName, mainAssemblyName);
        }

        [Event(18, Level = EventLevel.Warning, Keywords = Keywords.Loader)]
        public void ResourceManagerNeutralResourcesNotFound(string baseName, string mainAssemblyName, string resName)
        {
            if (IsEnabled())
                WriteEvent(18, baseName, mainAssemblyName, resName);
        }

        [Event(19, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerNeutralResourcesFound(string baseName, string mainAssemblyName, string resName)
        {
            if (IsEnabled())
                WriteEvent(19, baseName, mainAssemblyName, resName);
        }

        [Event(20, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerAddingCultureFromConfigFile(string baseName, string mainAssemblyName, string cultureName)
        {
            if (IsEnabled())
                WriteEvent(20, baseName, mainAssemblyName, cultureName);
        }

        [Event(21, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerCultureNotFoundInConfigFile(string baseName, string mainAssemblyName, string cultureName)
        {
            if (IsEnabled())
                WriteEvent(21, baseName, mainAssemblyName, cultureName);
        }

        [Event(22, Level = EventLevel.Informational, Keywords = Keywords.Loader)]
        public void ResourceManagerCultureFoundInConfigFile(string baseName, string mainAssemblyName, string cultureName)
        {
            if (IsEnabled())
                WriteEvent(22, baseName, mainAssemblyName, cultureName);
        }


        // ResourceManager Event Wrappers

        [NonEvent]
        public void ResourceManagerLookupStarted(string baseName, Assembly mainAssembly, string cultureName)
        {
            if (IsEnabled())
                ResourceManagerLookupStarted(baseName, GetName(mainAssembly), cultureName);
        }

        [NonEvent]
        public void ResourceManagerLookingForResourceSet(string baseName, Assembly mainAssembly, string cultureName)
        {
            if (IsEnabled())
                ResourceManagerLookingForResourceSet(baseName, GetName(mainAssembly), cultureName);
        }

        [NonEvent]
        public void ResourceManagerFoundResourceSetInCache(string baseName, Assembly mainAssembly, string cultureName)
        {
            if (IsEnabled())
                ResourceManagerFoundResourceSetInCache(baseName, GetName(mainAssembly), cultureName);
        }

        [NonEvent]
        public void ResourceManagerFoundResourceSetInCacheUnexpected(string baseName, Assembly mainAssembly, string cultureName)
        {
            if (IsEnabled())
                ResourceManagerFoundResourceSetInCacheUnexpected(baseName, GetName(mainAssembly), cultureName);
        }

        [NonEvent]
        public void ResourceManagerStreamFound(string baseName, Assembly mainAssembly, string cultureName, Assembly loadedAssembly, string resourceFileName)
        {
            if (IsEnabled())
                ResourceManagerStreamFound(baseName, GetName(mainAssembly), cultureName, GetName(loadedAssembly), resourceFileName);
        }

        [NonEvent]
        public void ResourceManagerStreamNotFound(string baseName, Assembly mainAssembly, string cultureName, Assembly loadedAssembly, string resourceFileName)
        {
            if (IsEnabled())
                ResourceManagerStreamNotFound(baseName, GetName(mainAssembly), cultureName, GetName(loadedAssembly), resourceFileName);
        }

        [NonEvent]
        public void ResourceManagerGetSatelliteAssemblySucceeded(string baseName, Assembly mainAssembly, string cultureName, string assemblyName)
        {
            if (IsEnabled())
                ResourceManagerGetSatelliteAssemblySucceeded(baseName, GetName(mainAssembly), cultureName, assemblyName);
        }

        [NonEvent]
        public void ResourceManagerGetSatelliteAssemblyFailed(string baseName, Assembly mainAssembly, string cultureName, string assemblyName)
        {
            if (IsEnabled())
                ResourceManagerGetSatelliteAssemblyFailed(baseName, GetName(mainAssembly), cultureName, assemblyName);
        }

        [NonEvent]
        public void ResourceManagerCaseInsensitiveResourceStreamLookupSucceeded(string baseName, Assembly mainAssembly, string assemblyName, string resourceFileName)
        {
            if (IsEnabled())
                ResourceManagerCaseInsensitiveResourceStreamLookupSucceeded(baseName, GetName(mainAssembly), assemblyName, resourceFileName);
        }

        [NonEvent]
        public void ResourceManagerCaseInsensitiveResourceStreamLookupFailed(string baseName, Assembly mainAssembly, string assemblyName, string resourceFileName)
        {
            if (IsEnabled())
                ResourceManagerCaseInsensitiveResourceStreamLookupFailed(baseName, GetName(mainAssembly), assemblyName, resourceFileName);
        }

        [NonEvent]
        public void ResourceManagerManifestResourceAccessDenied(string baseName, Assembly mainAssembly, string assemblyName, string canonicalName)
        {
            if (IsEnabled())
                ResourceManagerManifestResourceAccessDenied(baseName, GetName(mainAssembly), assemblyName, canonicalName);
        }

        [NonEvent]
        public void ResourceManagerNeutralResourcesSufficient(string baseName, Assembly mainAssembly, string cultureName)
        {
            if (IsEnabled())
                ResourceManagerNeutralResourcesSufficient(baseName, GetName(mainAssembly), cultureName);
        }

        [NonEvent]
        public void ResourceManagerNeutralResourceAttributeMissing(Assembly mainAssembly)
        {
            if (IsEnabled())
                ResourceManagerNeutralResourceAttributeMissing(GetName(mainAssembly));
        }

        [NonEvent]
        public void ResourceManagerCreatingResourceSet(string baseName, Assembly mainAssembly, string cultureName, string fileName)
        {
            if (IsEnabled())
                ResourceManagerCreatingResourceSet(baseName, GetName(mainAssembly), cultureName, fileName);
        }

        [NonEvent]
        public void ResourceManagerNotCreatingResourceSet(string baseName, Assembly mainAssembly, string cultureName)
        {
            if (IsEnabled())
                ResourceManagerNotCreatingResourceSet(baseName, GetName(mainAssembly), cultureName);
        }

        [NonEvent]
        public void ResourceManagerLookupFailed(string baseName, Assembly mainAssembly, string cultureName)
        {
            if (IsEnabled())
                ResourceManagerLookupFailed(baseName, GetName(mainAssembly), cultureName);
        }

        [NonEvent]
        public void ResourceManagerReleasingResources(string baseName, Assembly mainAssembly)
        {
            if (IsEnabled())
                ResourceManagerReleasingResources(baseName, GetName(mainAssembly));
        }

        [NonEvent]
        public void ResourceManagerNeutralResourcesNotFound(string baseName, Assembly mainAssembly, string resName)
        {
            if (IsEnabled())
                ResourceManagerNeutralResourcesNotFound(baseName, GetName(mainAssembly), resName);
        }

        [NonEvent]
        public void ResourceManagerNeutralResourcesFound(string baseName, Assembly mainAssembly, string resName)
        {
            if (IsEnabled())
                ResourceManagerNeutralResourcesFound(baseName, GetName(mainAssembly), resName);
        }

        [NonEvent]
        public void ResourceManagerAddingCultureFromConfigFile(string baseName, Assembly mainAssembly, string cultureName)
        {
            if (IsEnabled())
                ResourceManagerAddingCultureFromConfigFile(baseName, GetName(mainAssembly), cultureName);
        }

        [NonEvent]
        public void ResourceManagerCultureNotFoundInConfigFile(string baseName, Assembly mainAssembly, string cultureName)
        {
            if (IsEnabled())
                ResourceManagerCultureNotFoundInConfigFile(baseName, GetName(mainAssembly), cultureName);
        }

        [NonEvent]
        public void ResourceManagerCultureFoundInConfigFile(string baseName, Assembly mainAssembly, string cultureName)
        {
            if (IsEnabled())
                ResourceManagerCultureFoundInConfigFile(baseName, GetName(mainAssembly), cultureName);
        }

        private static string GetName(Assembly assembly)
        {
            if (assembly == null)
                return "<<NULL>>";
            else
                return assembly.FullName;
        }

        [Event(30, Level = EventLevel.Verbose, Keywords = Keywords.ThreadPool | Keywords.ThreadTransfer)]
        public void ThreadPoolEnqueueWork(long workID)
        {
            WriteEvent(30, workID);
        }
        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        public unsafe void ThreadPoolEnqueueWorkObject(object workID)
        {
            // convert the Object Id to a long
            ThreadPoolEnqueueWork((long)*((void**)Unsafe.AsPointer(ref workID)));
        }

        [Event(31, Level = EventLevel.Verbose, Keywords = Keywords.ThreadPool | Keywords.ThreadTransfer)]
        public void ThreadPoolDequeueWork(long workID)
        {
            WriteEvent(31, workID);
        }

        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        public unsafe void ThreadPoolDequeueWorkObject(object workID)
        {
            // convert the Object Id to a long
            ThreadPoolDequeueWork((long)*((void**)Unsafe.AsPointer(ref workID)));
        }

        // In the desktop runtime they don't use Tasks for the point at which the response happens, which means that the
        // Activity ID created by start using implicit activity IDs does not match.   Thus disable implicit activities (until we fix that)
        [Event(140, Level = EventLevel.Informational, Keywords = Keywords.NetClient, ActivityOptions = EventActivityOptions.Disable,
         Task = Tasks.GetResponse, Opcode = EventOpcode.Start, Version = 1)]
        private void GetResponseStart(long id, string uri, bool success, bool synchronous)
        {
            WriteEvent(140, id, uri, success, synchronous);
        }

        [Event(141, Level = EventLevel.Informational, Keywords = Keywords.NetClient, ActivityOptions = EventActivityOptions.Disable,
         Task = Tasks.GetResponse, Opcode = EventOpcode.Stop, Version = 1)]
        private void GetResponseStop(long id, bool success, bool synchronous, int statusCode)
        {
            WriteEvent(141, id, success, synchronous, statusCode);
        }

        // In the desktop runtime they don't use Tasks for the point at which the response happens, which means that the
        // Activity ID created by start using implicit activity IDs does not match.   Thus disable implicit activities (until we fix that)
        [Event(142, Level = EventLevel.Informational, Keywords = Keywords.NetClient, ActivityOptions = EventActivityOptions.Disable,
         Task = Tasks.GetRequestStream, Opcode = EventOpcode.Start, Version = 1)]
        private void GetRequestStreamStart(long id, string uri, bool success, bool synchronous)
        {
            WriteEvent(142, id, uri, success, synchronous);
        }

        [Event(143, Level = EventLevel.Informational, Keywords = Keywords.NetClient, ActivityOptions = EventActivityOptions.Disable,
         Task = Tasks.GetRequestStream, Opcode = EventOpcode.Stop, Version = 1)]
        private void GetRequestStreamStop(long id, bool success, bool synchronous)
        {
            WriteEvent(143, id, success, synchronous);
        }

        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        public unsafe void BeginGetResponse(object id, string uri, bool success, bool synchronous)
        {
            if (IsEnabled())
                GetResponseStart(IdForObject(id), uri, success, synchronous);
        }

        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        public unsafe void EndGetResponse(object id, bool success, bool synchronous, int statusCode)
        {
            if (IsEnabled())
                GetResponseStop(IdForObject(id), success, synchronous, statusCode);
        }

        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        public unsafe void BeginGetRequestStream(object id, string uri, bool success, bool synchronous)
        {
            if (IsEnabled())
                GetRequestStreamStart(IdForObject(id), uri, success, synchronous);
        }

        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        public unsafe void EndGetRequestStream(object id, bool success, bool synchronous)
        {
            if (IsEnabled())
                GetRequestStreamStop(IdForObject(id), success, synchronous);
        }

        // id -   represents a correlation ID that allows correlation of two activities, one stamped by 
        //        ThreadTransferSend, the other by ThreadTransferReceive
        // kind - identifies the transfer: values below 64 are reserved for the runtime. Currently used values:
        //        1 - managed Timers ("roaming" ID)
        //        2 - managed async IO operations (FileStream, PipeStream, a.o.)
        //        3 - WinRT dispatch operations
        // info - any additional information user code might consider interesting
        // intInfo1/2 - any additional integer information user code might consider interesting
        [Event(150, Level = EventLevel.Informational, Keywords = Keywords.ThreadTransfer, Task = Tasks.ThreadTransfer, Opcode = EventOpcode.Send)]
        public void ThreadTransferSend(long id, int kind, string info, bool multiDequeues, int intInfo1, int intInfo2)
        {
            if (IsEnabled())
                WriteEvent(150, id, kind, info, multiDequeues, intInfo1, intInfo2);
        }
        // id - is a managed object. it gets translated to the object's address. ETW listeners must
        //      keep track of GC movements in order to correlate the value passed to XyzSend with the
        //      (possibly changed) value passed to XyzReceive
        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        public unsafe void ThreadTransferSendObj(object id, int kind, string info, bool multiDequeues, int intInfo1, int intInfo2)
        {
            ThreadTransferSend((long)*((void**)Unsafe.AsPointer(ref id)), kind, info, multiDequeues, intInfo1, intInfo2);
        }

        // id -   represents a correlation ID that allows correlation of two activities, one stamped by 
        //        ThreadTransferSend, the other by ThreadTransferReceive
        // kind - identifies the transfer: values below 64 are reserved for the runtime. Currently used values:
        //        1 - managed Timers ("roaming" ID)
        //        2 - managed async IO operations (FileStream, PipeStream, a.o.)
        //        3 - WinRT dispatch operations
        // info - any additional information user code might consider interesting
        [Event(151, Level = EventLevel.Informational, Keywords = Keywords.ThreadTransfer, Task = Tasks.ThreadTransfer, Opcode = EventOpcode.Receive)]
        public void ThreadTransferReceive(long id, int kind, string info)
        {
            if (IsEnabled())
                WriteEvent(151, id, kind, info);
        }
        // id - is a managed object. it gets translated to the object's address. ETW listeners must
        //      keep track of GC movements in order to correlate the value passed to XyzSend with the
        //      (possibly changed) value passed to XyzReceive
        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        public unsafe void ThreadTransferReceiveObj(object id, int kind, string info)
        {
            ThreadTransferReceive((long)*((void**)Unsafe.AsPointer(ref id)), kind, info);
        }

        // id -   represents a correlation ID that allows correlation of two activities, one stamped by 
        //        ThreadTransferSend, the other by ThreadTransferReceive
        // kind - identifies the transfer: values below 64 are reserved for the runtime. Currently used values:
        //        1 - managed Timers ("roaming" ID)
        //        2 - managed async IO operations (FileStream, PipeStream, a.o.)
        //        3 - WinRT dispatch operations
        // info - any additional information user code might consider interesting
        [Event(152, Level = EventLevel.Informational, Keywords = Keywords.ThreadTransfer, Task = Tasks.ThreadTransfer, Opcode = Opcodes.ReceiveHandled)]
        public void ThreadTransferReceiveHandled(long id, int kind, string info)
        {
            if (IsEnabled())
                WriteEvent(152, id, kind, info);
        }
        // id - is a managed object. it gets translated to the object's address. ETW listeners must
        //      keep track of GC movements in order to correlate the value passed to XyzSend with the
        //      (possibly changed) value passed to XyzReceive
        [NonEvent]
#if !CORECLR
        [System.Security.SecuritySafeCritical]
#endif // !CORECLR
        public unsafe void ThreadTransferReceiveHandledObj(object id, int kind, string info)
        {
            ThreadTransferReceive((long)*((void**)Unsafe.AsPointer(ref id)), kind, info);
        }

        // return a stable ID for a an object.  We use the hash code which is not truely unique but is 
        // close enough for now at least. we add to it 0x7FFFFFFF00000000 to make it distinguishable
        // from the style of ID that simply casts the object reference to a long (since old versions of the 
        // runtime will emit IDs of that form).  
        private static long IdForObject(object obj)
        {
            return obj.GetHashCode() + 0x7FFFFFFF00000000;
        }
    }
}

