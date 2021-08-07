// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    ///     Performance Counter component.
    ///     This class provides support for NT Performance counters.
    ///     It handles both the existing counters (accessible by Perf Registry Interface)
    ///     and user defined (extensible) counters.
    ///     This class is a part of a larger framework, that includes the perf dll object and
    ///     perf service.
    /// </summary>
    public sealed class PerformanceCounter : Component, ISupportInitialize
    {
        private string _machineName;
        private string _categoryName;
        private string _counterName;
        private string _instanceName;
        private PerformanceCounterInstanceLifetime _instanceLifetime = PerformanceCounterInstanceLifetime.Global;

        private bool _isReadOnly;
        private bool _initialized;
        private string _helpMsg;
        private int _counterType = -1;

        // Cached old sample
        private CounterSample _oldSample = CounterSample.Empty;

        // Cached IP Shared Performanco counter
        private SharedPerformanceCounter _sharedCounter;

        [ObsoleteAttribute("PerformanceCounter.DefaultFileMappingSize has been deprecated and is not used. Use machine.config or an application configuration file to set the size of the PerformanceCounter file mapping instead.")]
        public static int DefaultFileMappingSize = 524288;

        private object _instanceLockObject;
        private object InstanceLockObject
        {
            get
            {
                if (_instanceLockObject == null)
                {
                    object o = new object();
                    Interlocked.CompareExchange(ref _instanceLockObject, o, null);
                }
                return _instanceLockObject;
            }
        }

        /// <summary>
        ///     The defaut constructor. Creates the perf counter object
        /// </summary>
        public PerformanceCounter()
        {
            _machineName = ".";
            _categoryName = string.Empty;
            _counterName = string.Empty;
            _instanceName = string.Empty;
            _isReadOnly = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Creates the Performance Counter Object
        /// </summary>
        public PerformanceCounter(string categoryName, string counterName, string instanceName, string machineName)
        {
            MachineName = machineName;
            CategoryName = categoryName;
            CounterName = counterName;
            InstanceName = instanceName;
            _isReadOnly = true;
            Initialize();
            GC.SuppressFinalize(this);
        }

        internal PerformanceCounter(string categoryName, string counterName, string instanceName, string machineName, bool skipInit)
        {
            MachineName = machineName;
            CategoryName = categoryName;
            CounterName = counterName;
            InstanceName = instanceName;
            _isReadOnly = true;
            _initialized = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Creates the Performance Counter Object on local machine.
        /// </summary>
        public PerformanceCounter(string categoryName, string counterName, string instanceName) :
        this(categoryName, counterName, instanceName, true)
        {
        }

        /// <summary>
        ///     Creates the Performance Counter Object on local machine.
        /// </summary>
        public PerformanceCounter(string categoryName, string counterName, string instanceName, bool readOnly)
        {
            if (!readOnly)
            {
                VerifyWriteableCounterAllowed();
            }
            MachineName = ".";
            CategoryName = categoryName;
            CounterName = counterName;
            InstanceName = instanceName;
            _isReadOnly = readOnly;
            Initialize();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Creates the Performance Counter Object, assumes that it's a single instance
        /// </summary>
        public PerformanceCounter(string categoryName, string counterName) :
        this(categoryName, counterName, true)
        {
        }

        /// <summary>
        ///     Creates the Performance Counter Object, assumes that it's a single instance
        /// </summary>
        public PerformanceCounter(string categoryName, string counterName, bool readOnly) :
        this(categoryName, counterName, "", readOnly)
        {
        }

        /// <summary>
        ///     Returns the performance category name for this performance counter
        /// </summary>
        public string CategoryName
        {
            get
            {
                return _categoryName;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (_categoryName == null || !string.Equals(_categoryName, value, StringComparison.OrdinalIgnoreCase))
                {
                    _categoryName = value;
                    Close();
                }
            }
        }

        /// <summary>
        ///     Returns the description message for this performance counter
        /// </summary>
        public string CounterHelp
        {
            get
            {
                string currentCategoryName = _categoryName;
                string currentMachineName = _machineName;

                Initialize();

                if (_helpMsg == null)
                    _helpMsg = PerformanceCounterLib.GetCounterHelp(currentMachineName, currentCategoryName, _counterName);

                return _helpMsg;
            }
        }

        /// <summary>
        ///     Sets/returns the performance counter name for this performance counter
        /// </summary>
        public string CounterName
        {
            get
            {
                return _counterName;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (_counterName == null || !string.Equals(_counterName, value, StringComparison.OrdinalIgnoreCase))
                {
                    _counterName = value;
                    Close();
                }
            }
        }

        /// <summary>
        ///     Sets/Returns the counter type for this performance counter
        /// </summary>
        public PerformanceCounterType CounterType
        {
            get
            {
                if (_counterType == -1)
                {
                    string currentCategoryName = _categoryName;
                    string currentMachineName = _machineName;

                    // This is the same thing that NextSample does, except that it doesn't try to get the actual counter
                    // value.  If we wanted the counter value, we would need to have an instance name.
                    Initialize();
                    using (CategorySample categorySample = PerformanceCounterLib.GetCategorySample(currentMachineName, currentCategoryName))
                    {
                        CounterDefinitionSample counterSample = categorySample.GetCounterDefinitionSample(_counterName);
                        _counterType = counterSample._counterType;
                    }
                }

                return (PerformanceCounterType)_counterType;
            }
        }

        public PerformanceCounterInstanceLifetime InstanceLifetime
        {
            get { return _instanceLifetime; }
            set
            {
                if (value > PerformanceCounterInstanceLifetime.Process || value < PerformanceCounterInstanceLifetime.Global)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (_initialized)
                    throw new InvalidOperationException(SR.CantSetLifetimeAfterInitialized);

                _instanceLifetime = value;
            }
        }

        /// <summary>
        ///     Sets/returns an instance name for this performance counter
        /// </summary>
        public string InstanceName
        {
            get
            {
                return _instanceName;
            }
            set
            {
                if (value == null && _instanceName == null)
                    return;

                if ((value == null && _instanceName != null) ||
                      (value != null && _instanceName == null) ||
                      !string.Equals(_instanceName, value, StringComparison.OrdinalIgnoreCase))
                {
                    _instanceName = value;
                    Close();
                }
            }
        }

        /// <summary>
        ///     Returns true if counter is read only (system counter, foreign extensible counter or remote counter)
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                return _isReadOnly;
            }

            set
            {
                if (value != _isReadOnly)
                {
                    if (value == false)
                    {
                        VerifyWriteableCounterAllowed();
                    }
                    _isReadOnly = value;
                    Close();
                }
            }
        }

        /// <summary>
        ///     Set/returns the machine name for this performance counter
        /// </summary>
        public string MachineName
        {
            get
            {
                return _machineName;
            }
            set
            {
                if (!SyntaxCheck.CheckMachineName(value))
                    throw new ArgumentException(SR.Format(SR.InvalidProperty, nameof(MachineName), value), nameof(value));

                if (_machineName != value)
                {
                    _machineName = value;
                    Close();
                }
            }
        }

        /// <summary>
        ///     Directly accesses the raw value of this counter.  If counter type is of a 32-bit size, it will truncate
        ///     the value given to 32 bits.  This can be significantly more performant for scenarios where
        ///     the raw value is sufficient.   Note that this only works for custom counters created using
        ///     this component,  non-custom counters will throw an exception if this property is accessed.
        /// </summary>
        public long RawValue
        {
            get
            {
                if (ReadOnly)
                {
                    //No need to initialize or Demand, since NextSample already does.
                    return NextSample().RawValue;
                }
                else
                {
                    Initialize();

                    return _sharedCounter.Value;
                }
            }
            set
            {
                if (ReadOnly)
                    ThrowReadOnly();

                Initialize();

                _sharedCounter.Value = value;
            }
        }

        /// <summary>
        /// </summary>
        public void BeginInit()
        {
            Close();
        }

        /// <summary>
        ///     Frees all the resources allocated by this counter
        /// </summary>
        public void Close()
        {
            _helpMsg = null;
            _oldSample = CounterSample.Empty;
            _sharedCounter = null;
            _initialized = false;
            _counterType = -1;
        }

        /// <summary>
        ///     Frees all the resources allocated for all performance
        ///     counters, frees File Mapping used by extensible counters,
        ///     unloads dll's used to read counters.
        /// </summary>
        public static void CloseSharedResources()
        {
            PerformanceCounterLib.CloseAllLibraries();
        }

        /// <internalonly/>
        /// <summary>
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            // safe to call while finalizing or disposing
            if (disposing)
            {
                //Dispose managed and unmanaged resources
                Close();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        ///     Decrements counter by one using an efficient atomic operation.
        /// </summary>
        public long Decrement()
        {
            if (ReadOnly)
                ThrowReadOnly();

            Initialize();

            return _sharedCounter.Decrement();
        }

        /// <summary>
        /// </summary>
        public void EndInit()
        {
            Initialize();
        }

        /// <summary>
        ///     Increments the value of this counter.  If counter type is of a 32-bit size, it'll truncate
        ///     the value given to 32 bits. This method uses a mutex to guarantee correctness of
        ///     the operation in case of multiple writers. This method should be used with caution because of the negative
        ///     impact on performance due to creation of the mutex.
        /// </summary>
        public long IncrementBy(long value)
        {
            if (_isReadOnly)
                ThrowReadOnly();

            Initialize();

            return _sharedCounter.IncrementBy(value);
        }

        /// <summary>
        ///     Increments counter by one using an efficient atomic operation.
        /// </summary>
        public long Increment()
        {
            if (_isReadOnly)
                ThrowReadOnly();

            Initialize();

            return _sharedCounter.Increment();
        }

        private void ThrowReadOnly()
        {
            throw new InvalidOperationException(SR.ReadOnlyCounter);
        }

        private static void VerifyWriteableCounterAllowed()
        {
            if (EnvironmentHelpers.IsAppContainerProcess)
            {
                throw new NotSupportedException(SR.PCNotSupportedUnderAppContainer);
            }
        }

        private void Initialize()
        {
            // Keep this method small so the JIT will inline it.
            if (!_initialized && !DesignMode)
            {
                InitializeImpl();
            }
        }

        /// <summary>
        ///     Intializes required resources
        /// </summary>
        private void InitializeImpl()
        {
            bool tookLock = false;
            try
            {
                Monitor.Enter(InstanceLockObject, ref tookLock);

                if (!_initialized)
                {
                    string currentCategoryName = _categoryName;
                    string currentMachineName = _machineName;

                    if (currentCategoryName.Length == 0)
                        throw new InvalidOperationException(SR.CategoryNameMissing);
                    if (_counterName.Length == 0)
                        throw new InvalidOperationException(SR.CounterNameMissing);

                    if (ReadOnly)
                    {
                        if (!PerformanceCounterLib.CounterExists(currentMachineName, currentCategoryName, _counterName))
                            throw new InvalidOperationException(SR.Format(SR.CounterExists, currentCategoryName, _counterName));

                        PerformanceCounterCategoryType categoryType = PerformanceCounterLib.GetCategoryType(currentMachineName, currentCategoryName);
                        if (categoryType == PerformanceCounterCategoryType.MultiInstance)
                        {
                            if (string.IsNullOrEmpty(_instanceName))
                                throw new InvalidOperationException(SR.Format(SR.MultiInstanceOnly, currentCategoryName));
                        }
                        else if (categoryType == PerformanceCounterCategoryType.SingleInstance)
                        {
                            if (!string.IsNullOrEmpty(_instanceName))
                                throw new InvalidOperationException(SR.Format(SR.SingleInstanceOnly, currentCategoryName));
                        }

                        if (_instanceLifetime != PerformanceCounterInstanceLifetime.Global)
                            throw new InvalidOperationException(SR.InstanceLifetimeProcessonReadOnly);

                        _initialized = true;
                    }
                    else
                    {
                        if (currentMachineName != "." && !string.Equals(currentMachineName, PerformanceCounterLib.ComputerName, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException(SR.RemoteWriting);

                        if (!PerformanceCounterLib.IsCustomCategory(currentMachineName, currentCategoryName))
                            throw new InvalidOperationException(SR.NotCustomCounter);

                        // check category type
                        PerformanceCounterCategoryType categoryType = PerformanceCounterLib.GetCategoryType(currentMachineName, currentCategoryName);
                        if (categoryType == PerformanceCounterCategoryType.MultiInstance)
                        {
                            if (string.IsNullOrEmpty(_instanceName))
                                throw new InvalidOperationException(SR.Format(SR.MultiInstanceOnly, currentCategoryName));
                        }
                        else if (categoryType == PerformanceCounterCategoryType.SingleInstance)
                        {
                            if (!string.IsNullOrEmpty(_instanceName))
                                throw new InvalidOperationException(SR.Format(SR.SingleInstanceOnly, currentCategoryName));
                        }

                        if (string.IsNullOrEmpty(_instanceName) && InstanceLifetime == PerformanceCounterInstanceLifetime.Process)
                            throw new InvalidOperationException(SR.InstanceLifetimeProcessforSingleInstance);

                        _sharedCounter = new SharedPerformanceCounter(currentCategoryName.ToLowerInvariant(), _counterName.ToLowerInvariant(), _instanceName.ToLowerInvariant(), _instanceLifetime);
                        _initialized = true;
                    }
                }
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(InstanceLockObject);
            }

        }

        // Will cause an update, raw value
        /// <summary>
        ///     Obtains a counter sample and returns the raw value for it.
        /// </summary>
        public CounterSample NextSample()
        {
            string currentCategoryName = _categoryName;
            string currentMachineName = _machineName;

            Initialize();


            using (CategorySample categorySample = PerformanceCounterLib.GetCategorySample(currentMachineName, currentCategoryName))
            {
                CounterDefinitionSample counterSample = categorySample.GetCounterDefinitionSample(_counterName);
                _counterType = counterSample._counterType;
                if (!categorySample._isMultiInstance)
                {
                    if (_instanceName != null && _instanceName.Length != 0)
                        throw new InvalidOperationException(SR.Format(SR.InstanceNameProhibited, _instanceName));

                    return counterSample.GetSingleValue();
                }
                else
                {
                    if (_instanceName == null || _instanceName.Length == 0)
                        throw new InvalidOperationException(SR.InstanceNameRequired);

                    return counterSample.GetInstanceValue(_instanceName);
                }
            }
        }

        /// <summary>
        ///     Obtains a counter sample and returns the calculated value for it.
        ///     NOTE: For counters whose calculated value depend upon 2 counter reads,
        ///           the very first read will return 0.0.
        /// </summary>
        public float NextValue()
        {
            //No need to initialize or Demand, since NextSample already does.
            CounterSample newSample = NextSample();
            float retVal = 0.0f;

            retVal = CounterSample.Calculate(_oldSample, newSample);
            _oldSample = newSample;

            return retVal;
        }

        /// <summary>
        ///     Removes this counter instance from the shared memory
        /// </summary>
        public void RemoveInstance()
        {
            if (_isReadOnly)
                throw new InvalidOperationException(SR.ReadOnlyRemoveInstance);

            Initialize();
            _sharedCounter.RemoveInstance(_instanceName.ToLowerInvariant(), _instanceLifetime);
        }
    }
}
