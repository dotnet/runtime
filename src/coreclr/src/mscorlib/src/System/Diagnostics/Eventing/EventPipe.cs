// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

namespace System.Diagnostics.Tracing
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct EventPipeProviderConfiguration
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        private string m_providerName;
        private UInt64 m_keywords;
        private uint m_loggingLevel;

        internal EventPipeProviderConfiguration(
            string providerName,
            UInt64 keywords,
            uint loggingLevel)
        {
            if(string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentNullException(nameof(providerName));
            }
            if(loggingLevel > 5) // 5 == Verbose, the highest value in EventPipeLoggingLevel.
            {
                throw new ArgumentOutOfRangeException(nameof(loggingLevel));
            }
            m_providerName = providerName;
            m_keywords = keywords;
            m_loggingLevel = loggingLevel;
        }

        internal string ProviderName
        {
            get { return m_providerName; }
        }

        internal UInt64 Keywords
        {
            get { return m_keywords; }
        }

        internal uint LoggingLevel
        {
            get { return m_loggingLevel; }
        }
    }

    internal sealed class EventPipeConfiguration
    {
        private string m_outputFile;
        private uint m_circularBufferSizeInMB;
        private List<EventPipeProviderConfiguration> m_providers;
        private TimeSpan m_minTimeBetweenSamples = TimeSpan.FromMilliseconds(1);

        internal EventPipeConfiguration(
            string outputFile,
            uint circularBufferSizeInMB)
        {
            if(string.IsNullOrEmpty(outputFile))
            {
                throw new ArgumentNullException(nameof(outputFile));
            }
            if(circularBufferSizeInMB == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(circularBufferSizeInMB));
            }
            m_outputFile = outputFile;
            m_circularBufferSizeInMB = circularBufferSizeInMB;
            m_providers = new List<EventPipeProviderConfiguration>();
        }

        internal string OutputFile
        {
            get { return m_outputFile; }
        }

        internal uint CircularBufferSizeInMB
        {
            get { return m_circularBufferSizeInMB; }
        }

        internal EventPipeProviderConfiguration[] Providers
        {
            get { return m_providers.ToArray(); }
        }

        internal long ProfilerSamplingRateInNanoseconds
        {
            // 100 nanoseconds == 1 tick.
            get { return m_minTimeBetweenSamples.Ticks * 100; }
        }

        internal void EnableProvider(string providerName, UInt64 keywords, uint loggingLevel)
        {
            m_providers.Add(new EventPipeProviderConfiguration(
                providerName,
                keywords,
                loggingLevel));
        }

        internal void SetProfilerSamplingRate(TimeSpan minTimeBetweenSamples)
        {
            if(minTimeBetweenSamples.Ticks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minTimeBetweenSamples));
            }

            m_minTimeBetweenSamples = minTimeBetweenSamples;
        }
    }

    internal static class EventPipe
    {
        internal static void Enable(EventPipeConfiguration configuration)
        {
            if(configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            EventPipeProviderConfiguration[] providers = configuration.Providers;

            EventPipeInternal.Enable(
                configuration.OutputFile,
                configuration.CircularBufferSizeInMB,
                configuration.ProfilerSamplingRateInNanoseconds,
                providers,
                providers.Length);
        }

        internal static void Disable()
        {
            EventPipeInternal.Disable();
        }
    }

    internal static class EventPipeInternal
    {
        //
        // These PInvokes are used by the configuration APIs to interact with EventPipe.
        //
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void Enable(string outputFile, uint circularBufferSizeInMB, long profilerSamplingRateInNanoseconds, EventPipeProviderConfiguration[] providers, int numProviders);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void Disable();

        //
        // These PInvokes are used by EventSource to interact with the EventPipe.
        //
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr CreateProvider(Guid providerID, UnsafeNativeMethods.ManifestEtw.EtwEnableCallback callbackFunc);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern unsafe IntPtr DefineEvent(IntPtr provHandle, uint eventID, Int64 keywords, uint eventVersion, uint level, void *pMetadata, uint metadataLength);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void DeleteProvider(IntPtr provHandle);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern unsafe void WriteEvent(IntPtr eventHandle, uint eventID, void* pData, uint length, Guid* activityId, Guid* relatedActivityId);
    }
}
