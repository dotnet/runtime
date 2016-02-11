// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Diagnostics
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct CounterSample
    {
        public static System.Diagnostics.CounterSample Empty;
        public CounterSample(long rawValue, long baseValue, long counterFrequency, long systemFrequency, long timeStamp, long timeStamp100nSec, System.Diagnostics.PerformanceCounterType counterType) { throw new System.NotImplementedException(); }
        public CounterSample(long rawValue, long baseValue, long counterFrequency, long systemFrequency, long timeStamp, long timeStamp100nSec, System.Diagnostics.PerformanceCounterType counterType, long counterTimeStamp) { throw new System.NotImplementedException(); }
        public long BaseValue { get { return default(long); } }
        public long CounterFrequency { get { return default(long); } }
        public long CounterTimeStamp { get { return default(long); } }
        public System.Diagnostics.PerformanceCounterType CounterType { get { return default(System.Diagnostics.PerformanceCounterType); } }
        public long RawValue { get { return default(long); } }
        public long SystemFrequency { get { return default(long); } }
        public long TimeStamp { get { return default(long); } }
        public long TimeStamp100nSec { get { return default(long); } }
        public static float Calculate(System.Diagnostics.CounterSample counterSample) { return default(float); }
        public static float Calculate(System.Diagnostics.CounterSample counterSample, System.Diagnostics.CounterSample nextCounterSample) { return default(float); }
        public bool Equals(System.Diagnostics.CounterSample sample) { return default(bool); }
        public override bool Equals(object o) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Diagnostics.CounterSample a, System.Diagnostics.CounterSample b) { return default(bool); }
        public static bool operator !=(System.Diagnostics.CounterSample a, System.Diagnostics.CounterSample b) { return default(bool); }
    }
    public sealed partial class PerformanceCounter
    {
        [System.ObsoleteAttribute("This field has been deprecated and is not used.  Use machine.config or an application configuration file to set the size of the PerformanceCounter file mapping.")]
        public static int DefaultFileMappingSize;
        public PerformanceCounter() { }
        public PerformanceCounter(string categoryName, string counterName) { }
        public PerformanceCounter(string categoryName, string counterName, bool readOnly) { }
        public PerformanceCounter(string categoryName, string counterName, string instanceName) { }
        public PerformanceCounter(string categoryName, string counterName, string instanceName, bool readOnly) { }
        public PerformanceCounter(string categoryName, string counterName, string instanceName, string machineName) { }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string CategoryName { get { return default(string); } set { } }
        public string CounterHelp { get { return default(string); } }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string CounterName { get { return default(string); } set { } }
        public System.Diagnostics.PerformanceCounterType CounterType { get { return default(System.Diagnostics.PerformanceCounterType); } }
        [System.ComponentModel.DefaultValueAttribute((System.Diagnostics.PerformanceCounterInstanceLifetime)(0))]
        public System.Diagnostics.PerformanceCounterInstanceLifetime InstanceLifetime { get { return default(System.Diagnostics.PerformanceCounterInstanceLifetime); } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string InstanceName { get { return default(string); } set { } }
        [System.ComponentModel.DefaultValueAttribute(".")]
        public string MachineName { get { return default(string); } set { } }
        public long RawValue { get { return default(long); } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool ReadOnly { get { return default(bool); } set { } }
        public void BeginInit() { }
        public void Close() { }
        public void EndInit() { }
        public long Increment() { return default(long); }
        public long IncrementBy(long value) { return default(long); }
        public System.Diagnostics.CounterSample NextSample() { return default(System.Diagnostics.CounterSample); }
        public float NextValue() { return default(float); }
        public void RemoveInstance() { }
    }
    public enum PerformanceCounterInstanceLifetime
    {
        Global = 0,
        Process = 1,
    }
    public enum PerformanceCounterType
    {
        AverageBase = 1073939458,
        AverageCount64 = 1073874176,
        AverageTimer32 = 805438464,
        CounterDelta32 = 4195328,
        CounterDelta64 = 4195584,
        CounterMultiBase = 1107494144,
        CounterMultiTimer = 574686464,
        CounterMultiTimer100Ns = 575735040,
        CounterMultiTimer100NsInverse = 592512256,
        CounterMultiTimerInverse = 591463680,
        CounterTimer = 541132032,
        CounterTimerInverse = 557909248,
        CountPerTimeInterval32 = 4523008,
        CountPerTimeInterval64 = 4523264,
        ElapsedTime = 807666944,
        NumberOfItems32 = 65536,
        NumberOfItems64 = 65792,
        NumberOfItemsHEX32 = 0,
        NumberOfItemsHEX64 = 256,
        RateOfCountsPerSecond32 = 272696320,
        RateOfCountsPerSecond64 = 272696576,
        RawBase = 1073939459,
        RawFraction = 537003008,
        SampleBase = 1073939457,
        SampleCounter = 4260864,
        SampleFraction = 549585920,
        Timer100Ns = 542180608,
        Timer100NsInverse = 558957824,
    }
}
