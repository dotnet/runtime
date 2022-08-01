// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System;
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Diagnostics
{
    /// <summary>
    ///     A Performance counter category object.
    /// </summary>
    public sealed class PerformanceCounterCategory
    {
        private string _categoryName;
        private string _categoryHelp;
        private string _machineName;
        internal const int MaxCategoryNameLength = 80;
        internal const int MaxCounterNameLength = 32767;
        internal const int MaxHelpLength = 32767;
        private const string PerfMutexName = "netfxperf.1.0";

        public PerformanceCounterCategory()
        {
            _machineName = ".";
        }

        /// <summary>
        ///     Creates a PerformanceCounterCategory object for given category.
        ///     Uses the local machine.
        /// </summary>
        public PerformanceCounterCategory(string categoryName)
            : this(categoryName, ".")
        {
        }

        /// <summary>
        ///     Creates a PerformanceCounterCategory object for given category.
        ///     Uses the given machine name.
        /// </summary>
        public PerformanceCounterCategory(string categoryName, string machineName)
        {
            ArgumentNullException.ThrowIfNull(categoryName);

            if (categoryName.Length == 0)
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(categoryName), categoryName), nameof(categoryName));

            if (!SyntaxCheck.CheckMachineName(machineName))
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(machineName), machineName), nameof(machineName));

            _categoryName = categoryName;
            _machineName = machineName;
        }

        /// <summary>
        ///     Gets/sets the Category name
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

                if (value.Length == 0)
                    throw new ArgumentException(SR.Format(SR.InvalidProperty, nameof(CategoryName), value), nameof(value));

                // the lock prevents a race between setting CategoryName and MachineName, since this permission
                // checks depend on both pieces of info.
                lock (this)
                {
                    _categoryName = value;
                }
            }
        }

        /// <summary>
        ///     Gets/sets the Category help
        /// </summary>
        public string CategoryHelp
        {
            get
            {
                if (_categoryName == null)
                    throw new InvalidOperationException(SR.CategoryNameNotSet);

                _categoryHelp ??= PerformanceCounterLib.GetCategoryHelp(_machineName, _categoryName);

                return _categoryHelp;
            }
        }

        public PerformanceCounterCategoryType CategoryType
        {
            get
            {
                using (CategorySample categorySample = PerformanceCounterLib.GetCategorySample(_machineName, _categoryName))
                {

                    // If we get MultiInstance, we can be confident it is correct.  If it is single instance, though
                    // we need to check if is a custom category and if the IsMultiInstance value is set in the registry.
                    // If not we return Unknown
                    if (categorySample._isMultiInstance)
                        return PerformanceCounterCategoryType.MultiInstance;
                    else
                    {
                        if (PerformanceCounterLib.IsCustomCategory(".", _categoryName))
                            return PerformanceCounterLib.GetCategoryType(".", _categoryName);
                        else
                            return PerformanceCounterCategoryType.SingleInstance;
                    }
                }
            }
        }


        /// <summary>
        ///     Gets/sets the Machine name
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

                // the lock prevents a race between setting CategoryName and MachineName, since this permission
                // checks depend on both pieces of info.
                lock (this)
                {
                    _machineName = value;
                }
            }
        }

        /// <summary>
        ///     Returns true if the counter is registered for this category
        /// </summary>
        public bool CounterExists(string counterName)
        {
            ArgumentNullException.ThrowIfNull(counterName);

            if (_categoryName == null)
                throw new InvalidOperationException(SR.CategoryNameNotSet);

            return PerformanceCounterLib.CounterExists(_machineName, _categoryName, counterName);
        }

        /// <summary>
        ///     Returns true if the counter is registered for this category on the current machine.
        /// </summary>
        public static bool CounterExists(string counterName, string categoryName)
        {
            return CounterExists(counterName, categoryName, ".");
        }

        /// <summary>
        ///     Returns true if the counter is registered for this category on a particular machine.
        /// </summary>
        public static bool CounterExists(string counterName, string categoryName, string machineName)
        {
            ArgumentNullException.ThrowIfNull(counterName);
            ArgumentNullException.ThrowIfNull(categoryName);

            if (categoryName.Length == 0)
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(categoryName), categoryName), nameof(categoryName));

            if (!SyntaxCheck.CheckMachineName(machineName))
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(machineName), machineName), nameof(machineName));

            return PerformanceCounterLib.CounterExists(machineName, categoryName, counterName);
        }

        /// <summary>
        /// Registers one extensible performance category with counter type NumberOfItems32 with the system
        /// </summary>
        [Obsolete("This overload of PerformanceCounterCategory.Create has been deprecated. Use System.Diagnostics.PerformanceCounterCategory.Create(string categoryName, string categoryHelp, PerformanceCounterCategoryType categoryType, string counterName, string counterHelp) instead.")]
        public static PerformanceCounterCategory Create(string categoryName, string categoryHelp, string counterName, string counterHelp)
        {
            CounterCreationData customData = new CounterCreationData(counterName, counterHelp, PerformanceCounterType.NumberOfItems32);
            return Create(categoryName, categoryHelp, PerformanceCounterCategoryType.Unknown, new CounterCreationDataCollection(new CounterCreationData[] { customData }));
        }

        /// <summary>
        /// Registers one extensible performance category of the specified category type with counter type NumberOfItems32 with the system
        /// </summary>
        public static PerformanceCounterCategory Create(string categoryName, string categoryHelp, PerformanceCounterCategoryType categoryType, string counterName, string counterHelp)
        {
            CounterCreationData customData = new CounterCreationData(counterName, counterHelp, PerformanceCounterType.NumberOfItems32);
            return Create(categoryName, categoryHelp, categoryType, new CounterCreationDataCollection(new CounterCreationData[] { customData }));
        }

        /// <summary>
        /// Registers the extensible performance category with the system on the local machine
        /// </summary>
        [Obsolete("This overload of PerformanceCounterCategory.Create has been deprecated. Use System.Diagnostics.PerformanceCounterCategory.Create(string categoryName, string categoryHelp, PerformanceCounterCategoryType categoryType, CounterCreationDataCollection counterData) instead.")]
        public static PerformanceCounterCategory Create(string categoryName, string categoryHelp, CounterCreationDataCollection counterData)
        {
            return Create(categoryName, categoryHelp, PerformanceCounterCategoryType.Unknown, counterData);
        }

        public static PerformanceCounterCategory Create(string categoryName, string categoryHelp, PerformanceCounterCategoryType categoryType, CounterCreationDataCollection counterData)
        {
            if (categoryType < PerformanceCounterCategoryType.Unknown || categoryType > PerformanceCounterCategoryType.MultiInstance)
                throw new ArgumentOutOfRangeException(nameof(categoryType));
            if (counterData == null)
                throw new ArgumentNullException(nameof(counterData));

            CheckValidCategory(categoryName);
            if (categoryHelp != null)
            {
                // null categoryHelp is a valid option - it gets set to "Help Not Available" later on.
                CheckValidHelp(categoryHelp);
            }
            string machineName = ".";

            Mutex mutex = null;
            try
            {
                NetFrameworkUtils.EnterMutex(PerfMutexName, ref mutex);
                if (PerformanceCounterLib.IsCustomCategory(machineName, categoryName) || PerformanceCounterLib.CategoryExists(machineName, categoryName))
                    throw new InvalidOperationException(SR.Format(SR.PerformanceCategoryExists, categoryName));

                CheckValidCounterLayout(counterData);
                PerformanceCounterLib.RegisterCategory(categoryName, categoryType, categoryHelp, counterData);
                return new PerformanceCounterCategory(categoryName, machineName);
            }
            finally
            {
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Close();
                }
            }
        }

        // there is an idential copy of CheckValidCategory in PerformnaceCounterInstaller
        internal static void CheckValidCategory(string categoryName)
        {
            ArgumentNullException.ThrowIfNull(categoryName);

            if (!CheckValidId(categoryName, MaxCategoryNameLength))
                throw new ArgumentException(SR.Format(SR.PerfInvalidCategoryName, 1, MaxCategoryNameLength));

            // 1026 chars is the size of the buffer used in perfcounter.dll to get this name.
            // If the categoryname plus prefix is too long, we won't be able to read the category properly.
            if (categoryName.Length > (1024 - SharedPerformanceCounter.DefaultFileMappingName.Length))
                throw new ArgumentException(SR.CategoryNameTooLong);
        }

        internal static void CheckValidCounter(string counterName)
        {
            ArgumentNullException.ThrowIfNull(counterName);

            if (!CheckValidId(counterName, MaxCounterNameLength))
                throw new ArgumentException(SR.Format(SR.PerfInvalidCounterName, 1, MaxCounterNameLength));
        }

        // there is an idential copy of CheckValidId in PerformnaceCounterInstaller
        internal static bool CheckValidId(string id, int maxLength)
        {
            if (id.Length == 0 || id.Length > maxLength)
                return false;

            for (int index = 0; index < id.Length; ++index)
            {
                char current = id[index];

                if ((index == 0 || index == (id.Length - 1)) && current == ' ')
                    return false;

                if (current == '\"')
                    return false;

                if (char.IsControl(current))
                    return false;
            }

            return true;
        }

        internal static void CheckValidHelp(string help)
        {
            ArgumentNullException.ThrowIfNull(help);

            if (help.Length > MaxHelpLength)
                throw new ArgumentException(SR.Format(SR.PerfInvalidHelp, 0, MaxHelpLength));
        }

        internal static void CheckValidCounterLayout(CounterCreationDataCollection counterData)
        {
            // Ensure that there are no duplicate counter names being created
            Hashtable h = new Hashtable();
            for (int i = 0; i < counterData.Count; i++)
            {
                if (counterData[i].CounterName == null || counterData[i].CounterName.Length == 0)
                {
                    throw new ArgumentException(SR.InvalidCounterName);
                }

                int currentSampleType = (int)counterData[i].CounterType;
                if ((currentSampleType == Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_BULK) ||
                        (currentSampleType == Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_MULTI_TIMER) ||
                        (currentSampleType == Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_MULTI_TIMER_INV) ||
                        (currentSampleType == Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER) ||
                        (currentSampleType == Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER_INV) ||
                        (currentSampleType == Interop.Kernel32.PerformanceCounterOptions.PERF_RAW_FRACTION) ||
                        (currentSampleType == Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_FRACTION) ||
                        (currentSampleType == Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_TIMER))
                {
                    if (counterData.Count <= (i + 1))
                        throw new InvalidOperationException(SR.CounterLayout);
                    else
                    {
                        currentSampleType = (int)counterData[i + 1].CounterType;


                        if (!PerformanceCounterLib.IsBaseCounter(currentSampleType))
                            throw new InvalidOperationException(SR.CounterLayout);
                    }
                }
                else if (PerformanceCounterLib.IsBaseCounter(currentSampleType))
                {
                    if (i == 0)
                        throw new InvalidOperationException(SR.CounterLayout);
                    else
                    {
                        currentSampleType = (int)counterData[i - 1].CounterType;

                        if (
                        (currentSampleType != Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_BULK) &&
                        (currentSampleType != Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_MULTI_TIMER) &&
                        (currentSampleType != Interop.Kernel32.PerformanceCounterOptions.PERF_100NSEC_MULTI_TIMER_INV) &&
                        (currentSampleType != Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER) &&
                        (currentSampleType != Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_TIMER_INV) &&
                        (currentSampleType != Interop.Kernel32.PerformanceCounterOptions.PERF_RAW_FRACTION) &&
                        (currentSampleType != Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_FRACTION) &&
                        (currentSampleType != Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_TIMER))
                            throw new InvalidOperationException(SR.CounterLayout);
                    }
                }

                if (h.ContainsKey(counterData[i].CounterName))
                {
                    throw new ArgumentException(SR.Format(SR.DuplicateCounterName, counterData[i].CounterName));
                }
                else
                {
                    h.Add(counterData[i].CounterName, string.Empty);

                    // Ensure that all counter help strings aren't null or empty
                    if (counterData[i].CounterHelp == null || counterData[i].CounterHelp.Length == 0)
                    {
                        counterData[i].CounterHelp = counterData[i].CounterName;
                    }
                }
            }
        }

        /// <summary>
        ///     Removes the counter (category) from the system
        /// </summary>
        public static void Delete(string categoryName)
        {
            CheckValidCategory(categoryName);
            string machineName = ".";

            categoryName = categoryName.ToLowerInvariant();

            Mutex mutex = null;
            try
            {
                NetFrameworkUtils.EnterMutex(PerfMutexName, ref mutex);
                if (!PerformanceCounterLib.IsCustomCategory(machineName, categoryName))
                    throw new InvalidOperationException(SR.CantDeleteCategory);

                SharedPerformanceCounter.RemoveAllInstances(categoryName);

                PerformanceCounterLib.UnregisterCategory(categoryName);
                PerformanceCounterLib.CloseAllLibraries();
            }
            finally
            {
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Close();
                }
            }
        }

        /// <summary>
        ///     Returns true if the category is registered on the current machine.
        /// </summary>
        public static bool Exists(string categoryName)
        {
            return Exists(categoryName, ".");
        }

        /// <summary>
        ///     Returns true if the category is registered in the machine.
        /// </summary>
        public static bool Exists(string categoryName, string machineName)
        {
            ArgumentNullException.ThrowIfNull(categoryName);

            if (categoryName.Length == 0)
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(categoryName), categoryName), nameof(categoryName));

            if (!SyntaxCheck.CheckMachineName(machineName))
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(machineName), machineName), nameof(machineName));

            if (PerformanceCounterLib.IsCustomCategory(machineName, categoryName))
                return true;

            return PerformanceCounterLib.CategoryExists(machineName, categoryName);
        }

        /// <summary>
        ///     Returns the instance names for a given category
        /// </summary>
        /// <internalonly/>
        internal static string[] GetCounterInstances(string categoryName, string machineName)
        {
            using (CategorySample categorySample = PerformanceCounterLib.GetCategorySample(machineName, categoryName))
            {
                if (categorySample._instanceNameTable.Count == 0)
                    return Array.Empty<string>();

                string[] instanceNames = new string[categorySample._instanceNameTable.Count];
                categorySample._instanceNameTable.Keys.CopyTo(instanceNames, 0);
                if (instanceNames.Length == 1 && instanceNames[0] == PerformanceCounterLib.SingleInstanceName)
                    return Array.Empty<string>();

                return instanceNames;
            }
        }

        /// <summary>
        ///     Returns an array of counters in this category.  The counter must have only one instance.
        /// </summary>
        public PerformanceCounter[] GetCounters()
        {
            if (GetInstanceNames().Length != 0)
                throw new ArgumentException(SR.InstanceNameRequired);
            return GetCounters("");
        }

        /// <summary>
        ///     Returns an array of counters in this category for the given instance.
        /// </summary>
        public PerformanceCounter[] GetCounters(string instanceName)
        {
            ArgumentNullException.ThrowIfNull(instanceName);

            if (_categoryName == null)
                throw new InvalidOperationException(SR.CategoryNameNotSet);

            if (instanceName.Length != 0 && !InstanceExists(instanceName))
                throw new InvalidOperationException(SR.Format(SR.MissingInstance, instanceName, _categoryName));

            string[] counterNames = PerformanceCounterLib.GetCounters(_machineName, _categoryName);
            PerformanceCounter[] counters = new PerformanceCounter[counterNames.Length];
            for (int index = 0; index < counters.Length; index++)
                counters[index] = new PerformanceCounter(_categoryName, counterNames[index], instanceName, _machineName, true);

            return counters;
        }


        /// <summary>
        ///     Returns an array of performance counter categories for the current machine.
        /// </summary>
        public static PerformanceCounterCategory[] GetCategories()
        {
            return GetCategories(".");
        }

        /// <summary>
        ///     Returns an array of performance counter categories for a particular machine.
        /// </summary>
        public static PerformanceCounterCategory[] GetCategories(string machineName)
        {
            if (!SyntaxCheck.CheckMachineName(machineName))
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(machineName), machineName), nameof(machineName));

            string[] categoryNames = PerformanceCounterLib.GetCategories(machineName);
            PerformanceCounterCategory[] categories = new PerformanceCounterCategory[categoryNames.Length];
            for (int index = 0; index < categories.Length; index++)
                categories[index] = new PerformanceCounterCategory(categoryNames[index], machineName);

            return categories;
        }

        /// <summary>
        ///     Returns an array of instances for this category
        /// </summary>
        public string[] GetInstanceNames()
        {
            if (_categoryName == null)
                throw new InvalidOperationException(SR.CategoryNameNotSet);

            return GetCounterInstances(_categoryName, _machineName);
        }

        /// <summary>
        ///     Returns true if the instance already exists for this category.
        /// </summary>
        public bool InstanceExists(string instanceName)
        {
            ArgumentNullException.ThrowIfNull(instanceName);

            if (_categoryName == null)
                throw new InvalidOperationException(SR.CategoryNameNotSet);

            using (CategorySample categorySample = PerformanceCounterLib.GetCategorySample(_machineName, _categoryName))
            {
                return categorySample._instanceNameTable.ContainsKey(instanceName);
            }
        }

        /// <summary>
        ///     Returns true if the instance already exists for the category specified.
        /// </summary>
        public static bool InstanceExists(string instanceName, string categoryName)
        {
            return InstanceExists(instanceName, categoryName, ".");
        }

        /// <summary>
        ///     Returns true if the instance already exists for this category and machine specified.
        /// </summary>
        public static bool InstanceExists(string instanceName, string categoryName, string machineName)
        {
            ArgumentNullException.ThrowIfNull(instanceName);
            ArgumentNullException.ThrowIfNull(categoryName);

            if (categoryName.Length == 0)
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(categoryName), categoryName), nameof(categoryName));

            if (!SyntaxCheck.CheckMachineName(machineName))
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(machineName), machineName), nameof(machineName));

            PerformanceCounterCategory category = new PerformanceCounterCategory(categoryName, machineName);
            return category.InstanceExists(instanceName);
        }

        /// <summary>
        ///     Reads all the counter and instance data of this performance category.  Note that reading the entire category
        ///     at once can be as efficient as reading a single counter because of the way the system provides the data.
        /// </summary>
        public InstanceDataCollectionCollection ReadCategory()
        {
            if (_categoryName == null)
                throw new InvalidOperationException(SR.CategoryNameNotSet);

            using (CategorySample categorySample = PerformanceCounterLib.GetCategorySample(_machineName, _categoryName))
            {
                return categorySample.ReadCategory();
            }
        }
    }

    [Flags]
    internal enum PerformanceCounterCategoryOptions
    {
        EnableReuse = 0x1,
        UseUniqueSharedMemory = 0x2,
    }
}
