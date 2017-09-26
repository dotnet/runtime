// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using System;
using System.Collections;
using System.Globalization;
using System.Security;
using System.Security.Permissions;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Threading;


namespace System.Diagnostics {
    /// <summary>
    ///     Performance Counter component.
    ///     This class provides support for NT Performance counters.
    ///     It handles both the existing counters (accesible by Perf Registry Interface)
    ///     and user defined (extensible) counters.
    ///     This class is a part of a larger framework, that includes the perf dll object and
    ///     perf service.
    /// </summary>
    public sealed class PerformanceCounter : Component, ISupportInitialize {
        private string machineName;
        private string categoryName;
        private string counterName;
        private string instanceName;
        private PerformanceCounterInstanceLifetime instanceLifetime = PerformanceCounterInstanceLifetime.Global;

        private bool isReadOnly;
        private bool initialized = false;
        private string helpMsg = null;
        private int counterType = -1;

        // Cached old sample
        private CounterSample oldSample = CounterSample.Empty;

        // Cached IP Shared Performanco counter
        private SharedPerformanceCounter sharedCounter;

        [ObsoleteAttribute("This field has been deprecated and is not used.  Use machine.config or an application configuration file to set the size of the PerformanceCounter file mapping.")]
        public static int DefaultFileMappingSize = 524288;

        private Object m_InstanceLockObject;
        private Object InstanceLockObject {
            get {
                if (m_InstanceLockObject == null) {
                    Object o = new Object();
                    Interlocked.CompareExchange(ref m_InstanceLockObject, o, null);
                }
                return m_InstanceLockObject;
            }
        }

        /// <summary>
        ///     The defaut constructor. Creates the perf counter object
        /// </summary>
        public PerformanceCounter() {
            machineName = ".";
            categoryName = String.Empty;
            counterName = String.Empty;
            instanceName = String.Empty;
            this.isReadOnly = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Creates the Performance Counter Object
        /// </summary>
        public PerformanceCounter(string categoryName, string counterName, string instanceName, string machineName) {
            this.MachineName = machineName;
            this.CategoryName = categoryName;
            this.CounterName = counterName;
            this.InstanceName = instanceName;
            this.isReadOnly = true;
            Initialize();
            GC.SuppressFinalize(this);
        }

        internal PerformanceCounter(string categoryName, string counterName, string instanceName, string machineName, bool skipInit) {
            this.MachineName = machineName;
            this.CategoryName = categoryName;
            this.CounterName = counterName;
            this.InstanceName = instanceName;
            this.isReadOnly = true;
            this.initialized = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Creates the Performance Counter Object on local machine.
        /// </summary>
        public PerformanceCounter(string categoryName, string counterName, string instanceName) :
        this(categoryName, counterName, instanceName, true) {
        }

        /// <summary>
        ///     Creates the Performance Counter Object on local machine.
        /// </summary>
        public PerformanceCounter(string categoryName, string counterName, string instanceName, bool readOnly) {
            if(!readOnly) {
                VerifyWriteableCounterAllowed();
            }
            this.MachineName = ".";
            this.CategoryName = categoryName;
            this.CounterName = counterName;
            this.InstanceName = instanceName;
            this.isReadOnly = readOnly;
            Initialize();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Creates the Performance Counter Object, assumes that it's a single instance
        /// </summary>
        public PerformanceCounter(string categoryName, string counterName) :
        this(categoryName, counterName, true) {
        }

        /// <summary>
        ///     Creates the Performance Counter Object, assumes that it's a single instance
        /// </summary>
        public PerformanceCounter(string categoryName, string counterName, bool readOnly) :
        this(categoryName, counterName, "", readOnly) {
        }

        /// <summary>
        ///     Returns the performance category name for this performance counter
        /// </summary>
        public string CategoryName {
            get {
                return categoryName;
            }
            set {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (categoryName == null || String.Compare(categoryName, value, StringComparison.OrdinalIgnoreCase) != 0) {
                    categoryName = value;
                    Close();
                }
            }
        }

        /// <summary>
        ///     Returns the description message for this performance counter
        /// </summary>
        public string CounterHelp {
            get {
                string currentCategoryName = categoryName;
                string currentMachineName = machineName;
                
                PerformanceCounterPermission permission = new PerformanceCounterPermission(PerformanceCounterPermissionAccess.Read, currentMachineName, currentCategoryName);
                permission.Demand();
                Initialize();

                if (helpMsg == null)
                    helpMsg = PerformanceCounterLib.GetCounterHelp(currentMachineName, currentCategoryName, this.counterName);

                return helpMsg;
            }
        }

        /// <summary>
        ///     Sets/returns the performance counter name for this performance counter
        /// </summary>
        public string CounterName {
            get {
                return counterName;
            }
            set {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (counterName == null || String.Compare(counterName, value, StringComparison.OrdinalIgnoreCase) != 0) {
                    counterName = value;
                    Close();
                }
            }
        }

        /// <summary>
        ///     Sets/Returns the counter type for this performance counter
        /// </summary>
        public PerformanceCounterType CounterType {
            get {
                if (counterType == -1) {
                    string currentCategoryName = categoryName;
                    string currentMachineName = machineName;
                    
                    // This is the same thing that NextSample does, except that it doesn't try to get the actual counter
                    // value.  If we wanted the counter value, we would need to have an instance name. 
                    PerformanceCounterPermission permission = new PerformanceCounterPermission(PerformanceCounterPermissionAccess.Read, currentMachineName, currentCategoryName);
                    permission.Demand();
                    
                    Initialize();
                    CategorySample categorySample = PerformanceCounterLib.GetCategorySample(currentMachineName, currentCategoryName);
                    CounterDefinitionSample counterSample = categorySample.GetCounterDefinitionSample(this.counterName);
                    this.counterType = counterSample.CounterType;
                }

                return(PerformanceCounterType) counterType;
            }
        }

        public PerformanceCounterInstanceLifetime InstanceLifetime {
            get { return instanceLifetime; }
            set { 
                if (value > PerformanceCounterInstanceLifetime.Process || value < PerformanceCounterInstanceLifetime.Global)
                    throw new ArgumentOutOfRangeException("value");

                if (initialized)
                    throw new InvalidOperationException(SR.Format(SR.CantSetLifetimeAfterInitialized));
                
                instanceLifetime = value;
            }
        }
        
        /// <summary>
        ///     Sets/returns an instance name for this performance counter
        /// </summary>
        public string InstanceName {
            get {
                return instanceName;
            }
            set {
                if (value == null && instanceName == null)
                    return;

                if ((value == null && instanceName != null) ||
                      (value != null && instanceName == null) ||
                      String.Compare(instanceName, value, StringComparison.OrdinalIgnoreCase) != 0) {
                    instanceName = value;
                    Close();
                }
            }
        }

        /// <summary>
        ///     Returns true if counter is read only (system counter, foreign extensible counter or remote counter)
        /// </summary>
        public bool ReadOnly {
            get {
                return isReadOnly;
            }

            set {
                if (value != this.isReadOnly) {
                    if(value == false) {
                        VerifyWriteableCounterAllowed();
                    }
                    this.isReadOnly = value;
                    Close();
                }
            }
        }


        /// <summary>
        ///     Set/returns the machine name for this performance counter
        /// </summary>
        public string MachineName {
            get {
                return machineName;
            }
            set {
                if (!SyntaxCheck.CheckMachineName(value))
                    throw new ArgumentException(SR.Format(SR.InvalidParameter, "machineName", value));

                if (machineName != value) {
                    machineName = value;
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
        public long RawValue {
            get {
                if (ReadOnly) {
                    //No need to initialize or Demand, since NextSample already does.
                    return NextSample().RawValue;
                }
                else {
                    Initialize();

                    return this.sharedCounter.Value;
                }
            }
            set {
                if (ReadOnly)
                    ThrowReadOnly();

                Initialize();

                this.sharedCounter.Value = value;
            }
        }

        /// <summary>
        /// </summary>
        public void BeginInit() {
            this.Close();
        }

        /// <summary>
        ///     Frees all the resources allocated by this counter
        /// </summary>
        public void Close() {
            this.helpMsg = null;
            this.oldSample = CounterSample.Empty;
            this.sharedCounter = null;
            this.initialized = false;
            this.counterType = -1;
        }

        /// <summary>
        ///     Frees all the resources allocated for all performance
        ///     counters, frees File Mapping used by extensible counters,
        ///     unloads dll's used to read counters.
        /// </summary>
        public static void CloseSharedResources() {
            PerformanceCounterPermission permission = new PerformanceCounterPermission(PerformanceCounterPermissionAccess.Read, ".", "*");
            permission.Demand();
            PerformanceCounterLib.CloseAllLibraries();
        }

        /// <internalonly/>
        /// <summary>
        /// </summary>
        protected override void Dispose(bool disposing) {
            // safe to call while finalizing or disposing
            //
            if (disposing) {
                //Dispose managed and unmanaged resources
                Close();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        ///     Decrements counter by one using an efficient atomic operation.
        /// </summary>
        public long Decrement() {
            if (ReadOnly)
                ThrowReadOnly();

            Initialize();

            return this.sharedCounter.Decrement();
        }

        /// <summary>
        /// </summary>
        public void EndInit() {
            Initialize();
        }

        /// <summary>
        ///     Increments the value of this counter.  If counter type is of a 32-bit size, it'll truncate
        ///     the value given to 32 bits. This method uses a mutex to guarantee correctness of
        ///     the operation in case of multiple writers. This method should be used with caution because of the negative
        ///     impact on performance due to creation of the mutex.
        /// </summary>
        public long IncrementBy(long value) {
            if (isReadOnly)
                ThrowReadOnly();

            Initialize();

            return this.sharedCounter.IncrementBy(value);
        }

        /// <summary>
        ///     Increments counter by one using an efficient atomic operation.
        /// </summary>
        public long Increment() {
            if (isReadOnly)
                ThrowReadOnly();

            Initialize();

            return this.sharedCounter.Increment();
        }

        private void ThrowReadOnly() {
            throw new InvalidOperationException(SR.Format(SR.ReadOnlyCounter));
        }
        
        private static void VerifyWriteableCounterAllowed() {
            if(EnvironmentHelpers.IsAppContainerProcess) {
                throw new NotSupportedException(SR.Format(SR.PCNotSupportedUnderAppContainer));
            }
        }

        private void Initialize() {
            // Keep this method small so the JIT will inline it.
            if (!initialized && !DesignMode) {
                InitializeImpl();
            }
        }

        /// <summary>
        ///     Intializes required resources
        /// </summary>
        private void InitializeImpl() {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                Monitor.Enter(InstanceLockObject, ref tookLock);

                if (!initialized) {
                    string currentCategoryName = categoryName;
                    string currentMachineName = machineName;

                    if (currentCategoryName == String.Empty)
                        throw new InvalidOperationException(SR.Format(SR.CategoryNameMissing));
                    if (this.counterName == String.Empty)
                        throw new InvalidOperationException(SR.Format(SR.CounterNameMissing));

                    if (this.ReadOnly) {
                        PerformanceCounterPermission permission = new PerformanceCounterPermission(PerformanceCounterPermissionAccess.Read, currentMachineName, currentCategoryName);

                        permission.Demand();

                        if (!PerformanceCounterLib.CounterExists(currentMachineName, currentCategoryName, counterName))
                            throw new InvalidOperationException(SR.Format(SR.CounterExists, currentCategoryName, counterName));

                        PerformanceCounterCategoryType categoryType = PerformanceCounterLib.GetCategoryType(currentMachineName, currentCategoryName);
                        if (categoryType == PerformanceCounterCategoryType.MultiInstance) {
                            if (String.IsNullOrEmpty(instanceName))
                                throw new InvalidOperationException(SR.Format(SR.MultiInstanceOnly, currentCategoryName));
                        } else if (categoryType == PerformanceCounterCategoryType.SingleInstance) {
                            if (!String.IsNullOrEmpty(instanceName))
                                throw new InvalidOperationException(SR.Format(SR.SingleInstanceOnly, currentCategoryName));
                        }

                        if (instanceLifetime != PerformanceCounterInstanceLifetime.Global)
                            throw new InvalidOperationException(SR.Format(SR.InstanceLifetimeProcessonReadOnly));

                        this.initialized = true;
                    } else {
                        PerformanceCounterPermission permission = new PerformanceCounterPermission(PerformanceCounterPermissionAccess.Write, currentMachineName, currentCategoryName);
                        permission.Demand();

                        if (currentMachineName != "." && String.Compare(currentMachineName, PerformanceCounterLib.ComputerName, StringComparison.OrdinalIgnoreCase) != 0)
                            throw new InvalidOperationException(SR.Format(SR.RemoteWriting));

                        SharedUtils.CheckNtEnvironment();

                        if (!PerformanceCounterLib.IsCustomCategory(currentMachineName, currentCategoryName))
                            throw new InvalidOperationException(SR.Format(SR.NotCustomCounter));

                        // check category type
                        PerformanceCounterCategoryType categoryType = PerformanceCounterLib.GetCategoryType(currentMachineName, currentCategoryName);
                        if (categoryType == PerformanceCounterCategoryType.MultiInstance) {
                            if (String.IsNullOrEmpty(instanceName))
                                throw new InvalidOperationException(SR.Format(SR.MultiInstanceOnly, currentCategoryName));
                        } else if (categoryType == PerformanceCounterCategoryType.SingleInstance) {
                            if (!String.IsNullOrEmpty(instanceName))
                                throw new InvalidOperationException(SR.Format(SR.SingleInstanceOnly, currentCategoryName));
                        }

                        if (String.IsNullOrEmpty(instanceName) && InstanceLifetime == PerformanceCounterInstanceLifetime.Process)
                            throw new InvalidOperationException(SR.Format(SR.InstanceLifetimeProcessforSingleInstance));

                        this.sharedCounter = new SharedPerformanceCounter(currentCategoryName.ToLower(CultureInfo.InvariantCulture), counterName.ToLower(CultureInfo.InvariantCulture), instanceName.ToLower(CultureInfo.InvariantCulture), instanceLifetime);
                        this.initialized = true;
                    }
                }
            } finally {
                if (tookLock)
                    Monitor.Exit(InstanceLockObject);
            }

        }

         // Will cause an update, raw value
        /// <summary>
        ///     Obtains a counter sample and returns the raw value for it.
        /// </summary>
        public CounterSample NextSample() {
            string currentCategoryName = categoryName;
            string currentMachineName = machineName;
             
            PerformanceCounterPermission permission = new PerformanceCounterPermission(PerformanceCounterPermissionAccess.Read, currentMachineName, currentCategoryName);
            permission.Demand();

            Initialize();
            CategorySample categorySample = PerformanceCounterLib.GetCategorySample(currentMachineName, currentCategoryName);
            CounterDefinitionSample counterSample = categorySample.GetCounterDefinitionSample(this.counterName);
            this.counterType = counterSample.CounterType;
            if (!categorySample.IsMultiInstance) {
                if (instanceName != null && instanceName.Length != 0)
                    throw new InvalidOperationException(SR.Format(SR.InstanceNameProhibited, this.instanceName));

                return counterSample.GetSingleValue();
            }
            else {
                if (instanceName == null || instanceName.Length == 0)
                    throw new InvalidOperationException(SR.Format(SR.InstanceNameRequired));

                return counterSample.GetInstanceValue(this.instanceName);
            }
        }

        /// <summary>
        ///     Obtains a counter sample and returns the calculated value for it.
        ///     NOTE: For counters whose calculated value depend upon 2 counter reads,
        ///           the very first read will return 0.0.
        /// </summary>
        public float NextValue() {
            //No need to initialize or Demand, since NextSample already does.
            CounterSample newSample = NextSample();
            float retVal = 0.0f;

            retVal = CounterSample.Calculate(oldSample, newSample);
            oldSample = newSample;

            return retVal;
        }

        /// <summary>
        ///     Removes this counter instance from the shared memory
        /// </summary>
        public void RemoveInstance() {
            if (isReadOnly)
                throw new InvalidOperationException(SR.Format(SR.ReadOnlyRemoveInstance));

            Initialize();
            sharedCounter.RemoveInstance(this.instanceName.ToLower(CultureInfo.InvariantCulture), instanceLifetime);
        }
    }
}
