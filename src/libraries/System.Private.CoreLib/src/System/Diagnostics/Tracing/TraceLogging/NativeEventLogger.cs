// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Diagnostics.Tracing
{
    internal class NativeEventLogger
    {
        [DllImport(RuntimeHelpers.QCall)]
        private static extern void LogThreadPoolWorkerThreadStart(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

        public static void ThreadPoolWorkerThreadStart(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID)
        {
            LogThreadPoolWorkerThreadStart(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern void LogThreadPoolWorkerThreadStop(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

        public static void ThreadPoolWorkerThreadStop(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID)
        {
            LogThreadPoolWorkerThreadStop(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern void LogThreadPoolWorkerThreadWait(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

        public static void ThreadPoolWorkerThreadWait(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID)
        {
            LogThreadPoolWorkerThreadWait(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern void LogThreadPoolWorkerThreadAdjustmentSample(double Throughput, ushort ClrInstanceID);

        public static void ThreadPoolWorkerThreadAdjustmentSample(double Throughput, ushort ClrInstanceID)
        {
            LogThreadPoolWorkerThreadAdjustmentSample(Throughput, ClrInstanceID);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern void LogThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint NewWorkerThreadCount, NativeRuntimeEventSource.ThreadAdjustmentReasonMap Reason, ushort ClrInstanceID);

        public static void ThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint NewWorkerThreadCount, NativeRuntimeEventSource.ThreadAdjustmentReasonMap Reason, ushort ClrInstanceID)
        {
            LogThreadPoolWorkerThreadAdjustmentAdjustment(AverageThroughput, NewWorkerThreadCount, Reason, ClrInstanceID);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern void LogThreadPoolWorkerThreadAdjustmentStats(
            double Duration,
            double Throughput,
            double ThreadPoolWorkerThreadWait,
            double ThroughputWave,
            double ThroughputErrorEstimate,
            double AverageThroughputErrorEstimate,
            double ThroughputRatio,
            double Confidence,
            double NewControlSetting,
            ushort NewThreadWaveMagnitude,
            ushort ClrInstanceID);

        public static void ThreadPoolWorkerThreadAdjustmentStats(
            double Duration,
            double Throughput,
            double ThreadPoolWorkerThreadWait,
            double ThroughputWave,
            double ThroughputErrorEstimate,
            double AverageThroughputErrorEstimate,
            double ThroughputRatio,
            double Confidence,
            double NewControlSetting,
            ushort NewThreadWaveMagnitude,
            ushort ClrInstanceID)
        {
            LogThreadPoolWorkerThreadAdjustmentStats(Duration, Throughput, ThreadPoolWorkerThreadWait, ThroughputWave, ThroughputErrorEstimate, AverageThroughputErrorEstimate, ThroughputRatio, Confidence, NewControlSetting, NewThreadWaveMagnitude, ClrInstanceID);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern void LogThreadPoolIOEnqueue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            bool MultiDequeues,
            ushort ClrInstanceID);

        public static void ThreadPoolIOEnqueue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            bool MultiDequeues,
            ushort ClrInstanceID)
        {
            LogThreadPoolIOEnqueue(NativeOverlapped, Overlapped, MultiDequeues, ClrInstanceID);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern void LogThreadPoolIODequeue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID);

        public static void ThreadPoolIODequeue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID)
        {
            LogThreadPoolIODequeue(NativeOverlapped, Overlapped, ClrInstanceID);
        }

        [DllImport(RuntimeHelpers.QCall)]
        private static extern void LogThreadPoolWorkingThreadCount(
            uint Count,
            ushort ClrInstanceID
        );

        public static void ThreadPoolWorkingThreadCount(uint Count, ushort ClrInstanceID)
        {
            LogThreadPoolWorkingThreadCount(Count, ClrInstanceID);
        }
    }
}
