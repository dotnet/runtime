// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

#if FEATURE_PERFTRACING

namespace System.Diagnostics.Tracing
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct EventPipeEventInstanceData
    {
        internal IntPtr ProviderID;
        internal uint EventID;
        internal uint ThreadID;
        internal long TimeStamp;
        internal Guid ActivityId;
        internal Guid ChildActivityId;
        internal IntPtr Payload;
        internal uint PayloadLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EventPipeSessionInfo
    {
        internal long StartTimeAsUTCFileTime;
        internal long StartTimeStamp;
        internal long TimeStampFrequency;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EventPipeProviderConfiguration
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string m_providerName;
        private readonly ulong m_keywords;
        private readonly uint m_loggingLevel;

        [MarshalAs(UnmanagedType.LPWStr)]
        private readonly string? m_filterData;

        internal EventPipeProviderConfiguration(
            string providerName,
            ulong keywords,
            uint loggingLevel,
            string? filterData)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentNullException(nameof(providerName));
            }
            if (loggingLevel > 5) // 5 == Verbose, the highest value in EventPipeLoggingLevel.
            {
                throw new ArgumentOutOfRangeException(nameof(loggingLevel));
            }
            m_providerName = providerName;
            m_keywords = keywords;
            m_loggingLevel = loggingLevel;
            m_filterData = filterData;
        }

        internal string ProviderName
        {
            get { return m_providerName; }
        }

        internal ulong Keywords
        {
            get { return m_keywords; }
        }

        internal uint LoggingLevel
        {
            get { return m_loggingLevel; }
        }

        internal string? FilterData => m_filterData;
    }

    internal enum EventPipeSerializationFormat
    {
        NetPerf,
        NetTrace
    }

    internal sealed class EventPipeWaitHandle : WaitHandle
    {

    }

    internal sealed class EventPipeConfiguration
    {
        private readonly string m_outputFile;
        private readonly EventPipeSerializationFormat m_format;
        private readonly uint m_circularBufferSizeInMB;
        private readonly List<EventPipeProviderConfiguration> m_providers;
        private TimeSpan m_minTimeBetweenSamples = TimeSpan.FromMilliseconds(1);

        internal EventPipeConfiguration(
            string outputFile,
            EventPipeSerializationFormat format,
            uint circularBufferSizeInMB)
        {
            if (string.IsNullOrEmpty(outputFile))
            {
                throw new ArgumentNullException(nameof(outputFile));
            }
            if (circularBufferSizeInMB == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(circularBufferSizeInMB));
            }
            m_outputFile = outputFile;
            m_format = format;
            m_circularBufferSizeInMB = circularBufferSizeInMB;
            m_providers = new List<EventPipeProviderConfiguration>();
        }

        internal string OutputFile
        {
            get { return m_outputFile; }
        }

        internal EventPipeSerializationFormat Format
        {
            get { return m_format; }
        }

        internal uint CircularBufferSizeInMB
        {
            get { return m_circularBufferSizeInMB; }
        }

        internal EventPipeProviderConfiguration[] Providers
        {
            get { return m_providers.ToArray(); }
        }

        internal void EnableProvider(string providerName, ulong keywords, uint loggingLevel)
        {
            EnableProviderWithFilter(providerName, keywords, loggingLevel, null);
        }

        internal void EnableProviderWithFilter(string providerName, ulong keywords, uint loggingLevel, string? filterData)
        {
            m_providers.Add(new EventPipeProviderConfiguration(
                providerName,
                keywords,
                loggingLevel,
                filterData));
        }

        private void EnableProviderConfiguration(EventPipeProviderConfiguration providerConfig)
        {
            m_providers.Add(providerConfig);
        }

        internal void EnableProviderRange(EventPipeProviderConfiguration[] providerConfigs)
        {
            foreach (EventPipeProviderConfiguration config in providerConfigs)
            {
                EnableProviderConfiguration(config);
            }
        }

        internal void SetProfilerSamplingRate(TimeSpan minTimeBetweenSamples)
        {
            if (minTimeBetweenSamples.Ticks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minTimeBetweenSamples));
            }

            m_minTimeBetweenSamples = minTimeBetweenSamples;
        }
    }

    internal static class EventPipe
    {
        private static ulong s_sessionID = 0;

        internal static void Enable(EventPipeConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configuration.Providers == null)
            {
                throw new ArgumentNullException(nameof(configuration.Providers));
            }

            EventPipeProviderConfiguration[] providers = configuration.Providers;

            s_sessionID = EventPipeInternal.Enable(
                configuration.OutputFile,
                configuration.Format,
                configuration.CircularBufferSizeInMB,
                providers,
                (uint)providers.Length);
        }

        internal static void Disable()
        {
            EventPipeInternal.Disable(s_sessionID);
        }
    }

    internal static class EventPipeInternal
    {
        //
        // These PInvokes are used by the configuration APIs to interact with EventPipe.
        //
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern ulong Enable(
            string? outputFile,
            EventPipeSerializationFormat format,
            uint circularBufferSizeInMB,
            EventPipeProviderConfiguration[] providers,
            uint numProviders);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void Disable(ulong sessionID);

        //
        // These PInvokes are used by EventSource to interact with the EventPipe.
        //
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateProvider(string providerName, Interop.Advapi32.EtwEnableCallback callbackFunc);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe IntPtr DefineEvent(IntPtr provHandle, uint eventID, long keywords, uint eventVersion, uint level, void *pMetadata, uint metadataLength);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetProvider(string providerName);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void DeleteProvider(IntPtr provHandle);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern int EventActivityIdControl(uint controlCode, ref Guid activityId);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe void WriteEvent(IntPtr eventHandle, uint eventID, void* pData, uint length, Guid* activityId, Guid* relatedActivityId);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe void WriteEventData(IntPtr eventHandle, uint eventID, EventProvider.EventData* pEventData, uint dataCount, Guid* activityId, Guid* relatedActivityId);


        //
        // These PInvokes are used as part of the EventPipeEventDispatcher.
        //
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe bool GetSessionInfo(ulong sessionID, EventPipeSessionInfo* pSessionInfo);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe bool GetNextEvent(ulong sessionID, EventPipeEventInstanceData* pInstance);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern unsafe IntPtr GetWaitHandle(ulong sessionID);
    }
}

#endif // FEATURE_PERFTRACING
