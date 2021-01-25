// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        private static ulong s_sessionID;

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
                providers);
        }

        internal static void Disable()
        {
            EventPipeInternal.Disable(s_sessionID);
        }
    }

    internal static partial class EventPipeInternal
    {
        private unsafe struct EventPipeProviderConfigurationNative
        {
            private char* m_pProviderName;
            private ulong m_keywords;
            private uint m_loggingLevel;
            private char* m_pFilterData;

            internal static void MarshalToNative(EventPipeProviderConfiguration managed, ref EventPipeProviderConfigurationNative native)
            {
                native.m_pProviderName = (char*)Marshal.StringToCoTaskMemUni(managed.ProviderName);
                native.m_keywords = managed.Keywords;
                native.m_loggingLevel = managed.LoggingLevel;
                native.m_pFilterData = (char*)Marshal.StringToCoTaskMemUni(managed.FilterData);
            }

            internal void Release()
            {
                if (m_pProviderName != null)
                {
                    Marshal.FreeCoTaskMem((IntPtr)m_pProviderName);
                }
                if (m_pFilterData != null)
                {
                    Marshal.FreeCoTaskMem((IntPtr)m_pFilterData);
                }
            }
        }

        internal static unsafe ulong Enable(
            string? outputFile,
            EventPipeSerializationFormat format,
            uint circularBufferSizeInMB,
            EventPipeProviderConfiguration[] providers)
        {
            Span<EventPipeProviderConfigurationNative> providersNative = new Span<EventPipeProviderConfigurationNative>((void*)Marshal.AllocCoTaskMem(sizeof(EventPipeProviderConfigurationNative) * providers.Length), providers.Length);
            providersNative.Clear();

            try
            {
                for (int i = 0; i < providers.Length; i++)
                {
                    EventPipeProviderConfigurationNative.MarshalToNative(providers[i], ref providersNative[i]);
                }

                fixed (char* outputFilePath = outputFile)
                fixed (EventPipeProviderConfigurationNative* providersNativePointer = providersNative)
                {
                    return Enable(outputFilePath, format, circularBufferSizeInMB, providersNativePointer, (uint)providersNative.Length);
                }
            }
            finally
            {
                for (int i = 0; i < providers.Length; i++)
                {
                    providersNative[i].Release();
                }

                fixed (EventPipeProviderConfigurationNative* providersNativePointer = providersNative)
                {
                    Marshal.FreeCoTaskMem((IntPtr)providersNativePointer);
                }
            }
        }
    }
}

#endif // FEATURE_PERFTRACING
