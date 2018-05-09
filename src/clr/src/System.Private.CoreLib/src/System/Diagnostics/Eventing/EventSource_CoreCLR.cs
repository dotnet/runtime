// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Reflection;
using Microsoft.Win32;

namespace System.Diagnostics.Tracing
{
    public partial class EventSource
    {
        // ActivityID support (see also WriteEventWithRelatedActivityIdCore)
        /// <summary>
        /// When a thread starts work that is on behalf of 'something else' (typically another 
        /// thread or network request) it should mark the thread as working on that other work.
        /// This API marks the current thread as working on activity 'activityID'. This API
        /// should be used when the caller knows the thread's current activity (the one being
        /// overwritten) has completed. Otherwise, callers should prefer the overload that
        /// return the oldActivityThatWillContinue (below).
        /// 
        /// All events created with the EventSource on this thread are also tagged with the 
        /// activity ID of the thread. 
        /// 
        /// It is common, and good practice after setting the thread to an activity to log an event
        /// with a 'start' opcode to indicate that precise time/thread where the new activity 
        /// started.
        /// </summary>
        /// <param name="activityId">A Guid that represents the new activity with which to mark 
        /// the current thread</param>
        public static void SetCurrentThreadActivityId(Guid activityId)
        {
            if (TplEtwProvider.Log != null)
                TplEtwProvider.Log.SetActivityId(activityId);

            // We ignore errors to keep with the convention that EventSources do not throw errors.
            // Note we can't access m_throwOnWrites because this is a static method.  
#if FEATURE_MANAGED_ETW
#if FEATURE_PERFTRACING
            // Set the activity id via EventPipe.
            EventPipeInternal.EventActivityIdControl(
                (uint)UnsafeNativeMethods.ManifestEtw.ActivityControl.EVENT_ACTIVITY_CTRL_SET_ID,
                ref activityId);
#endif // FEATURE_PERFTRACING
#if PLATFORM_WINDOWS
            // Set the activity id via ETW.
            UnsafeNativeMethods.ManifestEtw.EventActivityIdControl(
                UnsafeNativeMethods.ManifestEtw.ActivityControl.EVENT_ACTIVITY_CTRL_SET_ID,
                ref activityId);
#endif // PLATFORM_WINDOWS
#endif // FEATURE_MANAGED_ETW
        }

        /// <summary>
        /// When a thread starts work that is on behalf of 'something else' (typically another 
        /// thread or network request) it should mark the thread as working on that other work.
        /// This API marks the current thread as working on activity 'activityID'. It returns 
        /// whatever activity the thread was previously marked with. There is a convention that
        /// callers can assume that callees restore this activity mark before the callee returns. 
        /// To encourage this, this API returns the old activity, so that it can be restored later.
        /// 
        /// All events created with the EventSource on this thread are also tagged with the 
        /// activity ID of the thread. 
        /// 
        /// It is common, and good practice after setting the thread to an activity to log an event
        /// with a 'start' opcode to indicate that precise time/thread where the new activity 
        /// started.
        /// </summary>
        /// <param name="activityId">A Guid that represents the new activity with which to mark 
        /// the current thread</param>
        /// <param name="oldActivityThatWillContinue">The Guid that represents the current activity  
        /// which will continue at some point in the future, on the current thread</param>
        public static void SetCurrentThreadActivityId(Guid activityId, out Guid oldActivityThatWillContinue)
        {
            oldActivityThatWillContinue = activityId;
#if FEATURE_MANAGED_ETW
            // We ignore errors to keep with the convention that EventSources do not throw errors.
            // Note we can't access m_throwOnWrites because this is a static method.  

#if FEATURE_PERFTRACING && PLATFORM_WINDOWS
            EventPipeInternal.EventActivityIdControl(
                (uint)UnsafeNativeMethods.ManifestEtw.ActivityControl.EVENT_ACTIVITY_CTRL_SET_ID,
                    ref oldActivityThatWillContinue);
#elif FEATURE_PERFTRACING
            EventPipeInternal.EventActivityIdControl(
                (uint)UnsafeNativeMethods.ManifestEtw.ActivityControl.EVENT_ACTIVITY_CTRL_GET_SET_ID,
                    ref oldActivityThatWillContinue);
#endif // FEATURE_PERFTRACING && PLATFORM_WINDOWS

#if PLATFORM_WINDOWS
            UnsafeNativeMethods.ManifestEtw.EventActivityIdControl(
                UnsafeNativeMethods.ManifestEtw.ActivityControl.EVENT_ACTIVITY_CTRL_GET_SET_ID,
                    ref oldActivityThatWillContinue);
#endif // PLATFORM_WINDOWS
#endif // FEATURE_MANAGED_ETW

            // We don't call the activityDying callback here because the caller has declared that
            // it is not dying.  
            if (TplEtwProvider.Log != null)
                TplEtwProvider.Log.SetActivityId(activityId);
        }

        /// <summary>
        /// Retrieves the ETW activity ID associated with the current thread.
        /// </summary>
        public static Guid CurrentThreadActivityId
        {
            get
            {
                // We ignore errors to keep with the convention that EventSources do not throw 
                // errors. Note we can't access m_throwOnWrites because this is a static method.
                Guid retVal = new Guid();
#if FEATURE_MANAGED_ETW
#if PLATFORM_WINDOWS
                UnsafeNativeMethods.ManifestEtw.EventActivityIdControl(
                    UnsafeNativeMethods.ManifestEtw.ActivityControl.EVENT_ACTIVITY_CTRL_GET_ID,
                    ref retVal);
#elif FEATURE_PERFTRACING
                EventPipeInternal.EventActivityIdControl(
                    (uint)UnsafeNativeMethods.ManifestEtw.ActivityControl.EVENT_ACTIVITY_CTRL_GET_ID,
                    ref retVal);
#endif // PLATFORM_WINDOWS
#endif // FEATURE_MANAGED_ETW
                return retVal;
            }
        }

        private int GetParameterCount(EventMetadata eventData)
        {
            return eventData.Parameters.Length;
        }

        private Type GetDataType(EventMetadata eventData, int parameterId)
        {
            return eventData.Parameters[parameterId].ParameterType;
        }

        private static string GetResourceString(string key, params object[] args)
        {
            return SR.Format(SR.GetResourceString(key), args);
        }

        private static readonly bool m_EventSourcePreventRecursion = false;
    }

    internal partial class ManifestBuilder
    {
        private string GetTypeNameHelper(Type type)
        {
            switch (type.GetTypeCode())
            {
                case TypeCode.Boolean:
                    return "win:Boolean";
                case TypeCode.Byte:
                    return "win:UInt8";
                case TypeCode.Char:
                case TypeCode.UInt16:
                    return "win:UInt16";
                case TypeCode.UInt32:
                    return "win:UInt32";
                case TypeCode.UInt64:
                    return "win:UInt64";
                case TypeCode.SByte:
                    return "win:Int8";
                case TypeCode.Int16:
                    return "win:Int16";
                case TypeCode.Int32:
                    return "win:Int32";
                case TypeCode.Int64:
                    return "win:Int64";
                case TypeCode.String:
                    return "win:UnicodeString";
                case TypeCode.Single:
                    return "win:Float";
                case TypeCode.Double:
                    return "win:Double";
                case TypeCode.DateTime:
                    return "win:FILETIME";
                default:
                    if (type == typeof(Guid))
                        return "win:GUID";
                    else if (type == typeof(IntPtr))
                        return "win:Pointer";
                    else if ((type.IsArray || type.IsPointer) && type.GetElementType() == typeof(byte))
                        return "win:Binary";

                    ManifestError(Resources.GetResourceString("EventSource_UnsupportedEventTypeInManifest", type.Name), true);
                    return string.Empty;
            }
        }
    }

    internal partial class EventProvider
    {
        internal unsafe int SetInformation(
            UnsafeNativeMethods.ManifestEtw.EVENT_INFO_CLASS eventInfoClass,
            IntPtr data,
            uint dataSize)
        {
            int status = UnsafeNativeMethods.ManifestEtw.ERROR_NOT_SUPPORTED;

            if (!m_setInformationMissing)
            {
                try
                {
                    status = UnsafeNativeMethods.ManifestEtw.EventSetInformation(
                        m_regHandle,
                        eventInfoClass,
                        (void*)data,
                        (int)dataSize);
                }
                catch (TypeLoadException)
                {
                    m_setInformationMissing = true;
                }
            }

            return status;
        }
    }

    internal static class Resources
    {
        internal static string GetResourceString(string key, params object[] args)
        {
            return SR.Format(SR.GetResourceString(key), args);
        }
    }
}
