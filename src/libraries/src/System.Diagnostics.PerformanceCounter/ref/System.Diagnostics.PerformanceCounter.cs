// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics
{
    public partial class CounterCreationData
    {
        public CounterCreationData() { }
        public CounterCreationData(string counterName, string counterHelp, System.Diagnostics.PerformanceCounterType counterType) { }
        public string CounterHelp { get { throw null; } set { } }
        public string CounterName { get { throw null; } set { } }
        public System.Diagnostics.PerformanceCounterType CounterType { get { throw null; } set { } }
    }
    public partial class CounterCreationDataCollection : System.Collections.CollectionBase
    {
        public CounterCreationDataCollection() { }
        public CounterCreationDataCollection(System.Diagnostics.CounterCreationDataCollection value) { }
        public CounterCreationDataCollection(System.Diagnostics.CounterCreationData[] value) { }
        public System.Diagnostics.CounterCreationData this[int index] { get { throw null; } set { } }
        public int Add(System.Diagnostics.CounterCreationData value) { throw null; }
        public void AddRange(System.Diagnostics.CounterCreationDataCollection value) { }
        public void AddRange(System.Diagnostics.CounterCreationData[] value) { }
        public bool Contains(System.Diagnostics.CounterCreationData value) { throw null; }
        public void CopyTo(System.Diagnostics.CounterCreationData[] array, int index) { }
        public int IndexOf(System.Diagnostics.CounterCreationData value) { throw null; }
        public void Insert(int index, System.Diagnostics.CounterCreationData value) { }
        protected override void OnValidate(object value) { }
        public virtual void Remove(System.Diagnostics.CounterCreationData value) { }
    }
    public readonly partial struct CounterSample
    {
        private readonly int _dummy;
        public static System.Diagnostics.CounterSample Empty;
        public CounterSample(long rawValue, long baseValue, long counterFrequency, long systemFrequency, long timeStamp, long timeStamp100nSec, System.Diagnostics.PerformanceCounterType counterType) { throw null; }
        public CounterSample(long rawValue, long baseValue, long counterFrequency, long systemFrequency, long timeStamp, long timeStamp100nSec, System.Diagnostics.PerformanceCounterType counterType, long counterTimeStamp) { throw null; }
        public long BaseValue { get { throw null; } }
        public long CounterFrequency { get { throw null; } }
        public long CounterTimeStamp { get { throw null; } }
        public System.Diagnostics.PerformanceCounterType CounterType { get { throw null; } }
        public long RawValue { get { throw null; } }
        public long SystemFrequency { get { throw null; } }
        public long TimeStamp { get { throw null; } }
        public long TimeStamp100nSec { get { throw null; } }
        public static float Calculate(System.Diagnostics.CounterSample counterSample) { throw null; }
        public static float Calculate(System.Diagnostics.CounterSample counterSample, System.Diagnostics.CounterSample nextCounterSample) { throw null; }
        public bool Equals(System.Diagnostics.CounterSample sample) { throw null; }
        public override bool Equals(object o) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Diagnostics.CounterSample a, System.Diagnostics.CounterSample b) { throw null; }
        public static bool operator !=(System.Diagnostics.CounterSample a, System.Diagnostics.CounterSample b) { throw null; }
    }
    public static partial class CounterSampleCalculator
    {
        public static float ComputeCounterValue(System.Diagnostics.CounterSample newSample) { throw null; }
        public static float ComputeCounterValue(System.Diagnostics.CounterSample oldSample, System.Diagnostics.CounterSample newSample) { throw null; }
    }
    public partial interface ICollectData
    {
        void CloseData();
        void CollectData(int id, System.IntPtr valueName, System.IntPtr data, int totalBytes, out System.IntPtr res);
    }
    public partial class InstanceData
    {
        public InstanceData(string instanceName, System.Diagnostics.CounterSample sample) { }
        public string InstanceName { get { throw null; } }
        public long RawValue { get { throw null; } }
        public System.Diagnostics.CounterSample Sample { get { throw null; } }
    }
    public partial class InstanceDataCollection : System.Collections.DictionaryBase
    {
        [System.ObsoleteAttribute("This constructor has been deprecated.  Please use System.Diagnostics.InstanceDataCollectionCollection.get_Item to get an instance of this collection instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public InstanceDataCollection(string counterName) { }
        public string CounterName { get { throw null; } }
        public System.Diagnostics.InstanceData this[string instanceName] { get { throw null; } }
        public System.Collections.ICollection Keys { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public bool Contains(string instanceName) { throw null; }
        public void CopyTo(System.Diagnostics.InstanceData[] instances, int index) { }
    }
    public partial class InstanceDataCollectionCollection : System.Collections.DictionaryBase
    {
        [System.ObsoleteAttribute("This constructor has been deprecated.  Please use System.Diagnostics.PerformanceCounterCategory.ReadCategory() to get an instance of this collection instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public InstanceDataCollectionCollection() { }
        public System.Diagnostics.InstanceDataCollection this[string counterName] { get { throw null; } }
        public System.Collections.ICollection Keys { get { throw null; } }
        public System.Collections.ICollection Values { get { throw null; } }
        public bool Contains(string counterName) { throw null; }
        public void CopyTo(System.Diagnostics.InstanceDataCollection[] counters, int index) { }
    }
    public sealed partial class PerformanceCounter : System.ComponentModel.Component, System.ComponentModel.ISupportInitialize
    {
        [System.ObsoleteAttribute("This field has been deprecated and is not used.  Use machine.config or an application configuration file to set the size of the PerformanceCounter file mapping.")]
        public static int DefaultFileMappingSize;
        public PerformanceCounter() { }
        public PerformanceCounter(string categoryName, string counterName) { }
        public PerformanceCounter(string categoryName, string counterName, bool readOnly) { }
        public PerformanceCounter(string categoryName, string counterName, string instanceName) { }
        public PerformanceCounter(string categoryName, string counterName, string instanceName, bool readOnly) { }
        public PerformanceCounter(string categoryName, string counterName, string instanceName, string machineName) { }
        public string CategoryName { get { throw null; } set { } }
        public string CounterHelp { get { throw null; } }
        public string CounterName { get { throw null; } set { } }
        public System.Diagnostics.PerformanceCounterType CounterType { get { throw null; } }
        public System.Diagnostics.PerformanceCounterInstanceLifetime InstanceLifetime { get { throw null; } set { } }
        public string InstanceName { get { throw null; } set { } }
        public string MachineName { get { throw null; } set { } }
        public long RawValue { get { throw null; } set { } }
        public bool ReadOnly { get { throw null; } set { } }
        public void BeginInit() { }
        public void Close() { }
        public static void CloseSharedResources() { }
        public long Decrement() { throw null; }
        protected override void Dispose(bool disposing) { }
        public void EndInit() { }
        public long Increment() { throw null; }
        public long IncrementBy(long value) { throw null; }
        public System.Diagnostics.CounterSample NextSample() { throw null; }
        public float NextValue() { throw null; }
        public void RemoveInstance() { }
    }
    public sealed partial class PerformanceCounterCategory
    {
        public PerformanceCounterCategory() { }
        public PerformanceCounterCategory(string categoryName) { }
        public PerformanceCounterCategory(string categoryName, string machineName) { }
        public string CategoryHelp { get { throw null; } }
        public string CategoryName { get { throw null; } set { } }
        public System.Diagnostics.PerformanceCounterCategoryType CategoryType { get { throw null; } }
        public string MachineName { get { throw null; } set { } }
        public bool CounterExists(string counterName) { throw null; }
        public static bool CounterExists(string counterName, string categoryName) { throw null; }
        public static bool CounterExists(string counterName, string categoryName, string machineName) { throw null; }
        [System.ObsoleteAttribute("This method has been deprecated.  Please use System.Diagnostics.PerformanceCounterCategory.Create(string categoryName, string categoryHelp, PerformanceCounterCategoryType categoryType, CounterCreationDataCollection counterData) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public static System.Diagnostics.PerformanceCounterCategory Create(string categoryName, string categoryHelp, System.Diagnostics.CounterCreationDataCollection counterData) { throw null; }
        public static System.Diagnostics.PerformanceCounterCategory Create(string categoryName, string categoryHelp, System.Diagnostics.PerformanceCounterCategoryType categoryType, System.Diagnostics.CounterCreationDataCollection counterData) { throw null; }
        public static System.Diagnostics.PerformanceCounterCategory Create(string categoryName, string categoryHelp, System.Diagnostics.PerformanceCounterCategoryType categoryType, string counterName, string counterHelp) { throw null; }
        [System.ObsoleteAttribute("This method has been deprecated.  Please use System.Diagnostics.PerformanceCounterCategory.Create(string categoryName, string categoryHelp, PerformanceCounterCategoryType categoryType, string counterName, string counterHelp) instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public static System.Diagnostics.PerformanceCounterCategory Create(string categoryName, string categoryHelp, string counterName, string counterHelp) { throw null; }
        public static void Delete(string categoryName) { }
        public static bool Exists(string categoryName) { throw null; }
        public static bool Exists(string categoryName, string machineName) { throw null; }
        public static System.Diagnostics.PerformanceCounterCategory[] GetCategories() { throw null; }
        public static System.Diagnostics.PerformanceCounterCategory[] GetCategories(string machineName) { throw null; }
        public System.Diagnostics.PerformanceCounter[] GetCounters() { throw null; }
        public System.Diagnostics.PerformanceCounter[] GetCounters(string instanceName) { throw null; }
        public string[] GetInstanceNames() { throw null; }
        public bool InstanceExists(string instanceName) { throw null; }
        public static bool InstanceExists(string instanceName, string categoryName) { throw null; }
        public static bool InstanceExists(string instanceName, string categoryName, string machineName) { throw null; }
        public System.Diagnostics.InstanceDataCollectionCollection ReadCategory() { throw null; }
    }
    public enum PerformanceCounterCategoryType
    {
        MultiInstance = 1,
        SingleInstance = 0,
        Unknown = -1,
    }
    public enum PerformanceCounterInstanceLifetime
    {
        Global = 0,
        Process = 1,
    }
    public sealed partial class PerformanceCounterManager : System.Diagnostics.ICollectData
    {
        [System.ObsoleteAttribute("This class has been deprecated.  Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public PerformanceCounterManager() { }
        [System.ObsoleteAttribute("This class has been deprecated.  Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        void System.Diagnostics.ICollectData.CloseData() { }
        [System.ObsoleteAttribute("This class has been deprecated.  Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        void System.Diagnostics.ICollectData.CollectData(int callIdx, System.IntPtr valueNamePtr, System.IntPtr dataPtr, int totalBytes, out System.IntPtr res) { throw null; }
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
