// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Security;
using System.Text;
using System.Threading;
using System.Collections;
using System.ComponentModel;
using Microsoft.Win32;
using System.IO;

using static Interop.Advapi32;

#if NETCOREAPP2_0 || !NETCOREAPP
using MemoryMarshal = System.Diagnostics.PerformanceCounterLib;
#endif

namespace System.Diagnostics
{
    internal sealed class PerformanceCounterLib
    {
        internal const string PerfShimName = "netfxperf.dll";
        private const string PerfShimFullNameSuffix = @"\netfxperf.dll";
        internal const string OpenEntryPoint = "OpenPerformanceData";
        internal const string CollectEntryPoint = "CollectPerformanceData";
        internal const string CloseEntryPoint = "ClosePerformanceData";
        internal const string SingleInstanceName = "systemdiagnosticsperfcounterlibsingleinstance";

        private const string PerflibPath = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Perflib";
        internal const string ServicePath = "SYSTEM\\CurrentControlSet\\Services";
        private const string CategorySymbolPrefix = "OBJECT_";
        private const string ConterSymbolPrefix = "DEVICE_COUNTER_";
        private const string HelpSufix = "_HELP";
        private const string NameSufix = "_NAME";
        private const string TextDefinition = "[text]";
        private const string InfoDefinition = "[info]";
        private const string LanguageDefinition = "[languages]";
        private const string ObjectDefinition = "[objects]";
        private const string DriverNameKeyword = "drivername";
        private const string SymbolFileKeyword = "symbolfile";
        private const string DefineKeyword = "#define";
        private const string LanguageKeyword = "language";
        private const string DllName = "netfxperf.dll";

        private const int EnglishLCID = 0x009;

        private static volatile string s_computerName;
        private static volatile string s_iniFilePath;
        private static volatile string s_symbolFilePath;

        private PerformanceMonitor _performanceMonitor;
        private readonly string _machineName;
        private readonly string _perfLcid;


        private static volatile Hashtable s_libraryTable;
        private Hashtable _customCategoryTable;
        private Hashtable _categoryTable;
        private Hashtable _nameTable;
        private Hashtable _helpTable;
        private readonly object _categoryTableLock = new object();
        private readonly object _nameTableLock = new object();
        private readonly object _helpTableLock = new object();

        private static object s_internalSyncObject;
        private static object InternalSyncObject
        {
            get
            {
                if (s_internalSyncObject == null)
                {
                    object o = new object();
                    Interlocked.CompareExchange(ref s_internalSyncObject, o, null);
                }
                return s_internalSyncObject;
            }
        }

        internal PerformanceCounterLib(string machineName, string lcid)
        {
            _machineName = machineName;
            _perfLcid = lcid;
        }

        /// <internalonly/>
        internal static string ComputerName
        {
            get
            {
                if (s_computerName == null)
                {
                    lock (InternalSyncObject)
                    {
                        if (s_computerName == null)
                        {
                            s_computerName = Interop.Kernel32.GetComputerName() ?? string.Empty;
                        }
                    }
                }

                return s_computerName;
            }
        }

#if NETCOREAPP2_0 || !NETCOREAPP
        internal static T Read<T>(ReadOnlySpan<byte> span) where T : struct
            => System.Runtime.InteropServices.MemoryMarshal.Read<T>(span);

        internal static ref readonly T AsRef<T>(ReadOnlySpan<byte> span) where T : struct
            => ref System.Runtime.InteropServices.MemoryMarshal.Cast<byte, T>(span)[0];
#endif

        private Hashtable CategoryTable
        {
            get
            {
                if (_categoryTable == null)
                {
                    lock (_categoryTableLock)
                    {
                        if (_categoryTable == null)
                        {
                            ReadOnlySpan<byte> data = GetPerformanceData("Global");

                            ref readonly PERF_DATA_BLOCK dataBlock = ref MemoryMarshal.AsRef<PERF_DATA_BLOCK>(data);
                            int pos = dataBlock.HeaderLength;

                            int numPerfObjects = dataBlock.NumObjectTypes;

                            // on some machines MSMQ claims to have 4 categories, even though it only has 2.
                            // This causes us to walk past the end of our data, potentially crashing or reading
                            // data we shouldn't.  We use dataBlock.TotalByteLength to make sure we don't go past the end
                            // of the perf data.
                            Hashtable tempCategoryTable = new Hashtable(numPerfObjects, StringComparer.OrdinalIgnoreCase);
                            for (int index = 0; index < numPerfObjects && pos < dataBlock.TotalByteLength; index++)
                            {
                                ref readonly PERF_OBJECT_TYPE perfObject = ref MemoryMarshal.AsRef<PERF_OBJECT_TYPE>(data.Slice(pos));

                                CategoryEntry newCategoryEntry = new CategoryEntry(in perfObject);
                                int nextPos = pos + perfObject.TotalByteLength;
                                pos += perfObject.HeaderLength;

                                int index3 = 0;
                                int previousCounterIndex = -1;
                                //Need to filter out counters that are repeated, some providers might
                                //return several adjacent copies of the same counter.
                                for (int index2 = 0; index2 < newCategoryEntry.CounterIndexes.Length; ++index2)
                                {
                                    ref readonly PERF_COUNTER_DEFINITION perfCounter = ref MemoryMarshal.AsRef<PERF_COUNTER_DEFINITION>(data.Slice(pos));
                                    if (perfCounter.CounterNameTitleIndex != previousCounterIndex)
                                    {
                                        newCategoryEntry.CounterIndexes[index3] = perfCounter.CounterNameTitleIndex;
                                        newCategoryEntry.HelpIndexes[index3] = perfCounter.CounterHelpTitleIndex;
                                        previousCounterIndex = perfCounter.CounterNameTitleIndex;
                                        ++index3;
                                    }
                                    pos += perfCounter.ByteLength;
                                }

                                //Lets adjust the entry counter arrays in case there were repeated copies
                                if (index3 < newCategoryEntry.CounterIndexes.Length)
                                {
                                    int[] adjustedCounterIndexes = new int[index3];
                                    int[] adjustedHelpIndexes = new int[index3];
                                    Array.Copy(newCategoryEntry.CounterIndexes, adjustedCounterIndexes, index3);
                                    Array.Copy(newCategoryEntry.HelpIndexes, adjustedHelpIndexes, index3);
                                    newCategoryEntry.CounterIndexes = adjustedCounterIndexes;
                                    newCategoryEntry.HelpIndexes = adjustedHelpIndexes;
                                }

                                string categoryName = (string)NameTable[newCategoryEntry.NameIndex];
                                if (categoryName != null)
                                    tempCategoryTable[categoryName] = newCategoryEntry;

                                pos = nextPos;
                            }

                            _categoryTable = tempCategoryTable;
                        }
                    }
                }

                return _categoryTable;
            }
        }

        internal Hashtable HelpTable
        {
            get
            {
                if (_helpTable == null)
                {
                    lock (_helpTableLock)
                    {
                        if (_helpTable == null)
                            _helpTable = GetStringTable(true);
                    }
                }

                return _helpTable;
            }
        }

        // Returns a temp file name
        private static string IniFilePath
        {
            get
            {
                if (s_iniFilePath == null)
                {
                    lock (InternalSyncObject)
                    {
                        if (s_iniFilePath == null)
                        {
                            try
                            {
                                s_iniFilePath = Path.GetTempFileName();
                            }
                            finally
                            { }
                        }
                    }
                }

                return s_iniFilePath;
            }
        }

        internal Hashtable NameTable
        {
            get
            {
                if (_nameTable == null)
                {
                    lock (_nameTableLock)
                    {
                        if (_nameTable == null)
                            _nameTable = GetStringTable(false);
                    }
                }

                return _nameTable;
            }
        }

        // Returns a temp file name
        private static string SymbolFilePath
        {
            get
            {
                if (s_symbolFilePath == null)
                {
                    lock (InternalSyncObject)
                    {
                        if (s_symbolFilePath == null)
                        {
                            string tempPath;

                            tempPath = Path.GetTempPath();

                            try
                            {
                                s_symbolFilePath = Path.GetTempFileName();
                            }
                            finally
                            { }
                        }
                    }
                }

                return s_symbolFilePath;
            }
        }

        internal static bool CategoryExists(string machine, string category)
        {
            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            if (library.CategoryExists(category))
                return true;

            if (CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID)
            {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture)
                {
                    library = GetPerformanceCounterLib(machine, culture);
                    if (library.CategoryExists(category))
                        return true;
                    culture = culture.Parent;
                }
            }

            return false;
        }

        internal bool CategoryExists(string category)
        {
            return CategoryTable.ContainsKey(category);
        }

        internal static void CloseAllLibraries()
        {
            if (s_libraryTable != null)
            {
                //race with GetPerformanceCounterLib
                lock (InternalSyncObject)
                {
                    if (s_libraryTable != null)
                    {
                        foreach (PerformanceCounterLib library in s_libraryTable.Values)
                            library.Close();

                        s_libraryTable = null;
                    }
                }
            }
        }

        internal static void CloseAllTables()
        {
            if (s_libraryTable != null)
            {
                foreach (PerformanceCounterLib library in s_libraryTable.Values)
                    library.CloseTables();
            }
        }

        internal void CloseTables()
        {
            _nameTable = null;
            _helpTable = null;
            _categoryTable = null;
            _customCategoryTable = null;
        }

        internal void Close()
        {
            if (_performanceMonitor != null)
            {
                _performanceMonitor.Close();
                _performanceMonitor = null;
            }

            CloseTables();
        }

        internal static bool CounterExists(string machine, string category, string counter)
        {
            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            bool categoryExists = false;
            bool counterExists = library.CounterExists(category, counter, ref categoryExists);

            if (!categoryExists && CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID)
            {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture)
                {
                    library = GetPerformanceCounterLib(machine, culture);
                    counterExists = library.CounterExists(category, counter, ref categoryExists);
                    if (counterExists)
                        break;

                    culture = culture.Parent;
                }
            }

            if (!categoryExists)
            {
                // Consider adding diagnostic logic here, may be we can dump the nameTable...
                throw new InvalidOperationException(SR.MissingCategory);
            }

            return counterExists;
        }

        private bool CounterExists(string category, string counter, ref bool categoryExists)
        {
            categoryExists = false;
            if (!CategoryTable.ContainsKey(category))
                return false;
            else
                categoryExists = true;

            CategoryEntry entry = (CategoryEntry)CategoryTable[category];
            for (int index = 0; index < entry.CounterIndexes.Length; ++index)
            {
                int counterIndex = entry.CounterIndexes[index];
                string counterName = (string)NameTable[counterIndex];
                if (counterName == null)
                    counterName = string.Empty;

                if (string.Equals(counterName, counter, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void CreateIniFile(string categoryName, string categoryHelp, CounterCreationDataCollection creationData, string[] languageIds)
        {
            try
            {
                StreamWriter iniWriter = new StreamWriter(IniFilePath, false, Encoding.Unicode);
                try
                {
                    //NT4 won't be able to parse Unicode ini files without this
                    //extra white space.
                    iniWriter.WriteLine("");
                    iniWriter.WriteLine(InfoDefinition);
                    iniWriter.Write(DriverNameKeyword);
                    iniWriter.Write("=");
                    iniWriter.WriteLine(categoryName);
                    iniWriter.Write(SymbolFileKeyword);
                    iniWriter.Write("=");
                    iniWriter.WriteLine(Path.GetFileName(SymbolFilePath));
                    iniWriter.WriteLine("");

                    iniWriter.WriteLine(LanguageDefinition);
                    foreach (string languageId in languageIds)
                    {
                        iniWriter.Write(languageId);
                        iniWriter.Write("=");
                        iniWriter.Write(LanguageKeyword);
                        iniWriter.WriteLine(languageId);
                    }
                    iniWriter.WriteLine("");

                    iniWriter.WriteLine(ObjectDefinition);
                    foreach (string languageId in languageIds)
                    {
                        iniWriter.Write(CategorySymbolPrefix);
                        iniWriter.Write("1_");
                        iniWriter.Write(languageId);
                        iniWriter.Write(NameSufix);
                        iniWriter.Write("=");
                        iniWriter.WriteLine(categoryName);
                    }
                    iniWriter.WriteLine("");

                    iniWriter.WriteLine(TextDefinition);
                    foreach (string languageId in languageIds)
                    {
                        iniWriter.Write(CategorySymbolPrefix);
                        iniWriter.Write("1_");
                        iniWriter.Write(languageId);
                        iniWriter.Write(NameSufix);
                        iniWriter.Write("=");
                        iniWriter.WriteLine(categoryName);
                        iniWriter.Write(CategorySymbolPrefix);
                        iniWriter.Write("1_");
                        iniWriter.Write(languageId);
                        iniWriter.Write(HelpSufix);
                        iniWriter.Write("=");
                        if (string.IsNullOrEmpty(categoryHelp))
                            iniWriter.WriteLine(SR.HelpNotAvailable);
                        else
                            iniWriter.WriteLine(categoryHelp);


                        int counterIndex = 0;
                        foreach (CounterCreationData counterData in creationData)
                        {
                            ++counterIndex;
                            string counterIndexString = counterIndex.ToString(CultureInfo.InvariantCulture);

                            iniWriter.WriteLine("");
                            iniWriter.Write(ConterSymbolPrefix);
                            iniWriter.Write(counterIndexString);
                            iniWriter.Write("_");
                            iniWriter.Write(languageId);
                            iniWriter.Write(NameSufix);
                            iniWriter.Write("=");
                            iniWriter.WriteLine(counterData.CounterName);

                            iniWriter.Write(ConterSymbolPrefix);
                            iniWriter.Write(counterIndexString);
                            iniWriter.Write("_");
                            iniWriter.Write(languageId);
                            iniWriter.Write(HelpSufix);
                            iniWriter.Write("=");

                            Debug.Assert(!string.IsNullOrEmpty(counterData.CounterHelp), "CounterHelp should have been fixed up by the caller");
                            iniWriter.WriteLine(counterData.CounterHelp);
                        }
                    }

                    iniWriter.WriteLine("");
                }
                finally
                {
                    iniWriter.Close();
                }
            }
            finally
            { }
        }

        private static void CreateRegistryEntry(string categoryName, PerformanceCounterCategoryType categoryType, CounterCreationDataCollection creationData, ref bool iniRegistered)
        {
            RegistryKey serviceParentKey = null;
            RegistryKey serviceKey = null;
            RegistryKey linkageKey = null;

            try
            {
                serviceParentKey = Registry.LocalMachine.OpenSubKey(ServicePath, true);

                serviceKey = serviceParentKey.OpenSubKey(categoryName + "\\Performance", true);
                if (serviceKey == null)
                    serviceKey = serviceParentKey.CreateSubKey(categoryName + "\\Performance");

                serviceKey.SetValue("Open", "OpenPerformanceData");
                serviceKey.SetValue("Collect", "CollectPerformanceData");
                serviceKey.SetValue("Close", "ClosePerformanceData");
                serviceKey.SetValue("Library", DllName);
                serviceKey.SetValue("IsMultiInstance", (int)categoryType, RegistryValueKind.DWord);
                serviceKey.SetValue("CategoryOptions", 0x3, RegistryValueKind.DWord);

                string[] counters = new string[creationData.Count];
                string[] counterTypes = new string[creationData.Count];
                for (int i = 0; i < creationData.Count; i++)
                {
                    counters[i] = creationData[i].CounterName;
                    counterTypes[i] = ((int)creationData[i].CounterType).ToString(CultureInfo.InvariantCulture);
                }

                linkageKey = serviceParentKey.OpenSubKey(categoryName + "\\Linkage", true);
                if (linkageKey == null)
                    linkageKey = serviceParentKey.CreateSubKey(categoryName + "\\Linkage");

                linkageKey.SetValue("Export", new string[] { categoryName });

                serviceKey.SetValue("Counter Types", (object)counterTypes);
                serviceKey.SetValue("Counter Names", (object)counters);

                object firstID = serviceKey.GetValue("First Counter");
                if (firstID != null)
                    iniRegistered = true;
                else
                    iniRegistered = false;
            }
            finally
            {
                if (serviceKey != null)
                    serviceKey.Close();

                if (linkageKey != null)
                    linkageKey.Close();

                if (serviceParentKey != null)
                    serviceParentKey.Close();
            }
        }

        private static void CreateSymbolFile(CounterCreationDataCollection creationData)
        {
            try
            {
                StreamWriter symbolWriter = new StreamWriter(SymbolFilePath);
                try
                {
                    symbolWriter.Write(DefineKeyword);
                    symbolWriter.Write(" ");
                    symbolWriter.Write(CategorySymbolPrefix);
                    symbolWriter.WriteLine("1 0;");

                    for (int counterIndex = 1; counterIndex <= creationData.Count; ++counterIndex)
                    {
                        symbolWriter.Write(DefineKeyword);
                        symbolWriter.Write(" ");
                        symbolWriter.Write(ConterSymbolPrefix);
                        symbolWriter.Write(counterIndex.ToString(CultureInfo.InvariantCulture));
                        symbolWriter.Write(" ");
                        symbolWriter.Write((counterIndex * 2).ToString(CultureInfo.InvariantCulture));
                        symbolWriter.WriteLine(";");
                    }

                    symbolWriter.WriteLine("");
                }
                finally
                {
                    symbolWriter.Close();
                }
            }
            finally
            { }
        }

        private static void DeleteRegistryEntry(string categoryName)
        {
            RegistryKey serviceKey = null;

            try
            {
                serviceKey = Registry.LocalMachine.OpenSubKey(ServicePath, true);

                bool deleteCategoryKey = false;
                using (RegistryKey categoryKey = serviceKey.OpenSubKey(categoryName, true))
                {
                    if (categoryKey != null)
                    {
                        if (categoryKey.GetValueNames().Length == 0)
                        {
                            deleteCategoryKey = true;
                        }
                        else
                        {
                            categoryKey.DeleteSubKeyTree("Linkage");
                            categoryKey.DeleteSubKeyTree("Performance");
                        }
                    }
                }
                if (deleteCategoryKey)
                    serviceKey.DeleteSubKeyTree(categoryName);

            }
            finally
            {
                if (serviceKey != null)
                    serviceKey.Close();
            }
        }

        private static void DeleteTemporaryFiles()
        {
            try
            {
                File.Delete(IniFilePath);
            }
            catch
            {
            }

            try
            {
                File.Delete(SymbolFilePath);
            }
            catch
            {
            }
        }

        // Ensures that the customCategoryTable is initialized and decides whether the category passed in
        //  1) is a custom category
        //  2) is a multi instance custom category
        // The return value is whether the category is a custom category or not.
        internal bool FindCustomCategory(string category, out PerformanceCounterCategoryType categoryType)
        {
            RegistryKey key = null;
            RegistryKey baseKey = null;
            categoryType = PerformanceCounterCategoryType.Unknown;

            Hashtable table =
                _customCategoryTable ??
                Interlocked.CompareExchange(ref _customCategoryTable, new Hashtable(StringComparer.OrdinalIgnoreCase), null) ??
                _customCategoryTable;

            if (table.ContainsKey(category))
            {
                categoryType = (PerformanceCounterCategoryType)table[category];
                return true;
            }
            else
            {
                try
                {
                    string keyPath = ServicePath + "\\" + category + "\\Performance";
                    if (_machineName == "." || string.Equals(_machineName, ComputerName, StringComparison.OrdinalIgnoreCase))
                    {
                        key = Registry.LocalMachine.OpenSubKey(keyPath);
                    }
                    else
                    {
                        baseKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "\\\\" + _machineName);
                        if (baseKey != null)
                        {
                            try
                            {
                                key = baseKey.OpenSubKey(keyPath);
                            }
                            catch (SecurityException)
                            {
                                // we may not have permission to read the registry key on the remote machine.  The security exception
                                // is thrown when RegOpenKeyEx returns ERROR_ACCESS_DENIED or ERROR_BAD_IMPERSONATION_LEVEL
                                //
                                // In this case we return an 'Unknown' category type and 'false' to indicate the category is *not* custom.
                                //
                                categoryType = PerformanceCounterCategoryType.Unknown;
                                lock (table)
                                {
                                    table[category] = categoryType;
                                }
                                return false;
                            }
                        }
                    }

                    if (key != null)
                    {
                        object systemDllName = key.GetValue("Library", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        if (systemDllName != null && systemDllName is string
                            && (string.Equals((string)systemDllName, PerformanceCounterLib.PerfShimName, StringComparison.OrdinalIgnoreCase)
                              || ((string)systemDllName).EndsWith(PerformanceCounterLib.PerfShimFullNameSuffix, StringComparison.OrdinalIgnoreCase)))
                        {

                            object isMultiInstanceObject = key.GetValue("IsMultiInstance");
                            if (isMultiInstanceObject != null)
                            {
                                categoryType = (PerformanceCounterCategoryType)isMultiInstanceObject;
                                if (categoryType < PerformanceCounterCategoryType.Unknown || categoryType > PerformanceCounterCategoryType.MultiInstance)
                                    categoryType = PerformanceCounterCategoryType.Unknown;
                            }
                            else
                                categoryType = PerformanceCounterCategoryType.Unknown;

                            object objectID = key.GetValue("First Counter");
                            if (objectID != null)
                            {
                                lock (table)
                                {
                                    table[category] = categoryType;
                                }
                                return true;
                            }
                        }
                    }
                }
                finally
                {
                    if (key != null)
                        key.Close();
                    if (baseKey != null)
                        baseKey.Close();
                }
            }

            return false;
        }

        internal static string[] GetCategories(string machineName)
        {
            PerformanceCounterLib library;
            CultureInfo culture = CultureInfo.CurrentCulture;
            while (culture != CultureInfo.InvariantCulture)
            {
                library = GetPerformanceCounterLib(machineName, culture);
                string[] categories = library.GetCategories();
                if (categories.Length != 0)
                    return categories;
                culture = culture.Parent;
            }

            library = GetPerformanceCounterLib(machineName, new CultureInfo(EnglishLCID));
            return library.GetCategories();
        }

        internal string[] GetCategories()
        {
            ICollection keys = CategoryTable.Keys;
            string[] categories = new string[keys.Count];
            keys.CopyTo(categories, 0);
            return categories;
        }

        internal static string GetCategoryHelp(string machine, string category)
        {
            PerformanceCounterLib library;
            string help;

            //First check the current culture for the category. This will allow
            //PerformanceCounterCategory.CategoryHelp to return localized strings.
            if (CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID)
            {
                CultureInfo culture = CultureInfo.CurrentCulture;

                while (culture != CultureInfo.InvariantCulture)
                {
                    library = GetPerformanceCounterLib(machine, culture);
                    help = library.GetCategoryHelp(category);
                    if (help != null)
                        return help;
                    culture = culture.Parent;
                }
            }

            //We did not find the category walking up the culture hierarchy. Try looking
            // for the category in the default culture English.
            library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            help = library.GetCategoryHelp(category);

            if (help == null)
                throw new InvalidOperationException(SR.MissingCategory);

            return help;
        }

        private string GetCategoryHelp(string category)
        {
            CategoryEntry entry = (CategoryEntry)CategoryTable[category];
            if (entry == null)
                return null;

            return (string)HelpTable[entry.HelpIndex];
        }

        internal static CategorySample GetCategorySample(string machine, string category)
        {
            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            CategorySample sample = library.GetCategorySample(category);
            if (sample == null && CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID)
            {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture)
                {
                    library = GetPerformanceCounterLib(machine, culture);
                    sample = library.GetCategorySample(category);
                    if (sample != null)
                        return sample;
                    culture = culture.Parent;
                }
            }
            if (sample == null)
                throw new InvalidOperationException(SR.MissingCategory);

            return sample;
        }

        private CategorySample GetCategorySample(string category)
        {
            CategoryEntry entry = (CategoryEntry)CategoryTable[category];
            if (entry == null)
                return null;

            CategorySample sample = null;
            byte[] dataRef = GetPerformanceData(entry.NameIndex.ToString(CultureInfo.InvariantCulture), usePool: true);
            if (dataRef == null)
                throw new InvalidOperationException(SR.Format(SR.CantReadCategory, category));

            sample = new CategorySample(dataRef, entry, this);
            return sample;
        }

        internal static string[] GetCounters(string machine, string category)
        {
            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            bool categoryExists = false;
            string[] counters = library.GetCounters(category, ref categoryExists);

            if (!categoryExists && CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID)
            {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture)
                {
                    library = GetPerformanceCounterLib(machine, culture);
                    counters = library.GetCounters(category, ref categoryExists);
                    if (categoryExists)
                        return counters;

                    culture = culture.Parent;
                }
            }

            if (!categoryExists)
                throw new InvalidOperationException(SR.MissingCategory);

            return counters;
        }

        private string[] GetCounters(string category, ref bool categoryExists)
        {
            categoryExists = false;
            CategoryEntry entry = (CategoryEntry)CategoryTable[category];
            if (entry == null)
                return null;
            else
                categoryExists = true;

            int index2 = 0;
            string[] counters = new string[entry.CounterIndexes.Length];
            for (int index = 0; index < counters.Length; ++index)
            {
                int counterIndex = entry.CounterIndexes[index];
                string counterName = (string)NameTable[counterIndex];
                if (counterName != null && counterName != string.Empty)
                {
                    counters[index2] = counterName;
                    ++index2;
                }
            }

            //Lets adjust the array in case there were null entries
            if (index2 < counters.Length)
            {
                string[] adjustedCounters = new string[index2];
                Array.Copy(counters, adjustedCounters, index2);
                counters = adjustedCounters;
            }

            return counters;
        }

        internal static string GetCounterHelp(string machine, string category, string counter)
        {
            PerformanceCounterLib library;
            bool categoryExists = false;
            string help;

            //First check the current culture for the counter. This will allow
            //PerformanceCounter.CounterHelp to return localized strings.
            if (CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID)
            {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture)
                {
                    library = GetPerformanceCounterLib(machine, culture);
                    help = library.GetCounterHelp(category, counter, ref categoryExists);
                    if (categoryExists)
                        return help;
                    culture = culture.Parent;
                }
            }

            //We did not find the counter walking up the culture hierarchy. Try looking
            // for the counter in the default culture English.
            library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            help = library.GetCounterHelp(category, counter, ref categoryExists);

            if (!categoryExists)
                throw new InvalidOperationException(SR.Format(SR.MissingCategoryDetail, category));

            return help;
        }

        private string GetCounterHelp(string category, string counter, ref bool categoryExists)
        {
            categoryExists = false;
            CategoryEntry entry = (CategoryEntry)CategoryTable[category];
            if (entry == null)
                return null;
            else
                categoryExists = true;

            int helpIndex = -1;
            for (int index = 0; index < entry.CounterIndexes.Length; ++index)
            {
                int counterIndex = entry.CounterIndexes[index];
                string counterName = (string)NameTable[counterIndex];
                if (counterName == null)
                    counterName = string.Empty;

                if (string.Equals(counterName, counter, StringComparison.OrdinalIgnoreCase))
                {
                    helpIndex = entry.HelpIndexes[index];
                    break;
                }
            }

            if (helpIndex == -1)
                throw new InvalidOperationException(SR.Format(SR.MissingCounter, counter));

            string help = (string)HelpTable[helpIndex];
            if (help == null)
                return string.Empty;
            else
                return help;
        }

        private static string[] GetLanguageIds()
        {
            RegistryKey libraryParentKey = null;
            string[] ids = Array.Empty<string>();
            try
            {
                libraryParentKey = Registry.LocalMachine.OpenSubKey(PerflibPath);

                if (libraryParentKey != null)
                    ids = libraryParentKey.GetSubKeyNames();
            }
            finally
            {
                if (libraryParentKey != null)
                    libraryParentKey.Close();
            }

            return ids;
        }

        internal static PerformanceCounterLib GetPerformanceCounterLib(string machineName, CultureInfo culture)
        {
            string lcidString = culture.LCID.ToString("X3", CultureInfo.InvariantCulture);

            machineName = (machineName == "." ? ComputerName : machineName).ToLowerInvariant();

            //race with CloseAllLibraries
            lock (InternalSyncObject)
            {
                if (PerformanceCounterLib.s_libraryTable == null)
                    PerformanceCounterLib.s_libraryTable = new Hashtable();

                string libraryKey = machineName + ":" + lcidString;
                if (PerformanceCounterLib.s_libraryTable.Contains(libraryKey))
                    return (PerformanceCounterLib)PerformanceCounterLib.s_libraryTable[libraryKey];
                else
                {
                    PerformanceCounterLib library = new PerformanceCounterLib(machineName, lcidString);
                    PerformanceCounterLib.s_libraryTable[libraryKey] = library;
                    return library;
                }
            }
        }

        internal byte[] GetPerformanceData(string item, bool usePool = false)
        {
            if (_performanceMonitor == null)
            {
                lock (InternalSyncObject)
                {
                    if (_performanceMonitor == null)
                        _performanceMonitor = new PerformanceMonitor(_machineName);
                }
            }

            return _performanceMonitor.GetData(item, usePool);
        }

        internal void ReleasePerformanceData(byte[] data)
        {
            _performanceMonitor.ReleaseData(data);
        }

        private Hashtable GetStringTable(bool isHelp)
        {
            Hashtable stringTable;
            RegistryKey libraryKey;

            if (string.Equals(_machineName, ComputerName, StringComparison.OrdinalIgnoreCase))
                libraryKey = Registry.PerformanceData;
            else
            {
                libraryKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.PerformanceData, _machineName);
            }

            try
            {
                string[] names = null;
                int waitRetries = 14;   //((2^13)-1)*10ms == approximately 1.4mins
                int waitSleep = 0;

                // In some stress situations, querying counter values from
                // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\009
                // often returns null/empty data back. We should build fault-tolerance logic to
                // make it more reliable because getting null back once doesn't necessarily mean
                // that the data is corrupted, most of the time we would get the data just fine
                // in subsequent tries.
                while (waitRetries > 0)
                {
                    try
                    {
                        if (!isHelp)
                            names = (string[])libraryKey.GetValue("Counter " + _perfLcid);
                        else
                            names = (string[])libraryKey.GetValue("Explain " + _perfLcid);

                        if ((names == null) || (names.Length == 0))
                        {
                            --waitRetries;
                            if (waitSleep == 0)
                                waitSleep = 10;
                            else
                            {
                                System.Threading.Thread.Sleep(waitSleep);
                                waitSleep *= 2;
                            }
                        }
                        else
                            break;
                    }
                    catch (IOException)
                    {
                        // RegistryKey throws if it can't find the value.  We want to return an empty table
                        // and throw a different exception higher up the stack.
                        names = null;
                        break;
                    }
                    catch (InvalidCastException)
                    {
                        // Unable to cast object of type 'System.Byte[]' to type 'System.String[]'.
                        // this happens when the registry data store is corrupt and the type is not even REG_MULTI_SZ
                        names = null;
                        break;
                    }
                }

                if (names == null)
                    stringTable = new Hashtable();
                else
                {
                    stringTable = new Hashtable(names.Length / 2);

                    for (int index = 0; index < (names.Length / 2); ++index)
                    {
                        string nameString = names[(index * 2) + 1];
                        if (nameString == null)
                            nameString = string.Empty;

                        int key;
                        if (!int.TryParse(names[index * 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out key))
                        {
                            if (isHelp)
                            {
                                // Category Help Table
                                throw new InvalidOperationException(SR.Format(SR.CategoryHelpCorrupt, names[index * 2]));
                            }
                            else
                            {
                                // Counter Name Table
                                throw new InvalidOperationException(SR.Format(SR.CounterNameCorrupt, names[index * 2]));
                            }
                        }

                        stringTable[key] = nameString;
                    }
                }
            }
            finally
            {
                libraryKey.Close();
            }

            return stringTable;
        }

        internal static bool IsCustomCategory(string machine, string category)
        {
            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            if (library.IsCustomCategory(category))
                return true;

            if (CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID)
            {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture)
                {
                    library = GetPerformanceCounterLib(machine, culture);
                    if (library.IsCustomCategory(category))
                        return true;
                    culture = culture.Parent;
                }
            }

            return false;
        }

        internal static bool IsBaseCounter(int type)
        {
            return (type == Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_BASE ||
                    type == Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_BASE ||
                    type == Interop.Kernel32.PerformanceCounterOptions.PERF_RAW_BASE ||
                    type == Interop.Kernel32.PerformanceCounterOptions.PERF_LARGE_RAW_BASE ||
                    type == Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_BASE);
        }

        private bool IsCustomCategory(string category)
        {
            PerformanceCounterCategoryType categoryType;

            return FindCustomCategory(category, out categoryType);
        }

        internal static PerformanceCounterCategoryType GetCategoryType(string machine, string category)
        {
            PerformanceCounterCategoryType categoryType = PerformanceCounterCategoryType.Unknown;

            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            if (!library.FindCustomCategory(category, out categoryType))
            {
                if (CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID)
                {
                    CultureInfo culture = CultureInfo.CurrentCulture;
                    while (culture != CultureInfo.InvariantCulture)
                    {
                        library = GetPerformanceCounterLib(machine, culture);
                        if (library.FindCustomCategory(category, out categoryType))
                            return categoryType;
                        culture = culture.Parent;
                    }
                }
            }
            return categoryType;
        }

        internal static void RegisterCategory(string categoryName, PerformanceCounterCategoryType categoryType, string categoryHelp, CounterCreationDataCollection creationData)
        {
            try
            {
                bool iniRegistered = false;
                CreateRegistryEntry(categoryName, categoryType, creationData, ref iniRegistered);
                if (!iniRegistered)
                {
                    string[] languageIds = GetLanguageIds();
                    CreateIniFile(categoryName, categoryHelp, creationData, languageIds);
                    CreateSymbolFile(creationData);
                    RegisterFiles(IniFilePath, false);
                }
                CloseAllTables();
                CloseAllLibraries();
            }
            finally
            {
                DeleteTemporaryFiles();
            }
        }

        private static void RegisterFiles(string arg0, bool unregister)
        {
            Process p;
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.ErrorDialog = false;
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processStartInfo.WorkingDirectory = Environment.SystemDirectory;

            if (unregister)
                processStartInfo.FileName = Environment.SystemDirectory + "\\unlodctr.exe";
            else
                processStartInfo.FileName = Environment.SystemDirectory + "\\lodctr.exe";

            int res = 0;
            try
            {
                processStartInfo.Arguments = "\"" + arg0 + "\"";
                p = Process.Start(processStartInfo);
                p.WaitForExit();

                res = p.ExitCode;
            }
            finally
            {
            }


            if (res == Interop.Errors.ERROR_ACCESS_DENIED)
            {
                throw new UnauthorizedAccessException(SR.Format(SR.CantChangeCategoryRegistration, arg0));
            }

            // Look at Q269225, unlodctr might return 2 when WMI is not installed.
            if (unregister && res == 2)
                res = 0;

            if (res != 0)
                throw new Win32Exception(res);
        }

        internal static void UnregisterCategory(string categoryName)
        {
            RegisterFiles(categoryName, true);
            DeleteRegistryEntry(categoryName);
            CloseAllTables();
            CloseAllLibraries();
        }
    }

    internal sealed class PerformanceMonitor
    {
        private PerformanceDataRegistryKey perfDataKey;
        private readonly string machineName;

        internal PerformanceMonitor(string machineName)
        {
            this.machineName = machineName;
            Init();
        }

        private void Init()
        {
            try
            {
                if (machineName != "." && !string.Equals(machineName, PerformanceCounterLib.ComputerName, StringComparison.OrdinalIgnoreCase))
                {
                    perfDataKey = PerformanceDataRegistryKey.OpenRemoteBaseKey(machineName);
                }
                else
                    perfDataKey = PerformanceDataRegistryKey.OpenLocal();
            }
            catch (UnauthorizedAccessException)
            {
                // we need to do this for compatibility with v1.1 and v1.0.
                throw new Win32Exception(Interop.Errors.ERROR_ACCESS_DENIED);
            }
            catch (IOException e)
            {
                // we need to do this for compatibility with v1.1 and v1.0.
                throw new Win32Exception(Marshal.GetHRForException(e));
            }
        }

        internal void Close()
        {
            if (perfDataKey != null)
                perfDataKey.Close();

            perfDataKey = null;
        }

        // Win32 RegQueryValueEx for perf data could deadlock (for a Mutex) up to 2mins in some
        // scenarios before they detect it and exit gracefully. In the mean time, ERROR_BUSY,
        // ERROR_NOT_READY etc can be seen by other concurrent calls (which is the reason for the
        // wait loop and switch case below). We want to wait most certainly more than a 2min window.
        // The curent wait time of up to 10mins takes care of the known stress deadlock issues. In most
        // cases we wouldn't wait for more than 2mins anyways but in worst cases how much ever time
        // we wait may not be sufficient if the Win32 code keeps running into this deadlock again
        // and again. A condition very rare but possible in theory. We would get back to the user
        // in this case with InvalidOperationException after the wait time expires.
        internal byte[] GetData(string item, bool usePool)
        {
            int waitRetries = 17;   //2^16*10ms == approximately 10mins
            int waitSleep = 0;
            byte[] data = null;
            int error = 0;

            // no need to revert here since we'll fall off the end of the method
            while (waitRetries > 0)
            {
                try
                {
                    data = perfDataKey.GetValue(item, usePool);
                    return data;
                }
                catch (IOException e)
                {
                    error = Marshal.GetHRForException(e);
                    switch (error)
                    {
                        case Interop.Advapi32.RPCStatus.RPC_S_CALL_FAILED:
                        case Interop.Errors.ERROR_INVALID_HANDLE:
                        case Interop.Advapi32.RPCStatus.RPC_S_SERVER_UNAVAILABLE:
                            Init();
                            goto case Interop.Kernel32.WAIT_TIMEOUT;

                        case Interop.Kernel32.WAIT_TIMEOUT:
                        case Interop.Errors.ERROR_NOT_READY:
                        case Interop.Errors.ERROR_LOCK_FAILED:
                        case Interop.Errors.ERROR_BUSY:
                            --waitRetries;
                            if (waitSleep == 0)
                            {
                                waitSleep = 10;
                            }
                            else
                            {
                                System.Threading.Thread.Sleep(waitSleep);
                                waitSleep *= 2;
                            }
                            break;

                        default:
                            throw new Win32Exception(error);
                    }
                }
                catch (InvalidCastException e)
                {
                    throw new InvalidOperationException(SR.Format(SR.CounterDataCorrupt, perfDataKey.ToString()), e);
                }
            }

            throw new Win32Exception(error);
        }

        internal void ReleaseData(byte[] data)
        {
            perfDataKey.ReleaseData(data);
        }

    }

    internal sealed class CategoryEntry
    {
        internal int NameIndex;
        internal int HelpIndex;
        internal int[] CounterIndexes;
        internal int[] HelpIndexes;

        internal CategoryEntry(in PERF_OBJECT_TYPE perfObject)
        {
            NameIndex = perfObject.ObjectNameTitleIndex;
            HelpIndex = perfObject.ObjectHelpTitleIndex;
            CounterIndexes = new int[perfObject.NumCounters];
            HelpIndexes = new int[perfObject.NumCounters];
        }
    }

    internal sealed class CategorySample : IDisposable
    {
        internal readonly long _systemFrequency;
        internal readonly long _timeStamp;
        internal readonly long _timeStamp100nSec;
        internal readonly long _counterFrequency;
        internal readonly long _counterTimeStamp;
        internal Hashtable _counterTable;
        internal Hashtable _instanceNameTable;
        internal bool _isMultiInstance;
        private readonly CategoryEntry _entry;
        private readonly PerformanceCounterLib _library;
        private bool _disposed;
        private readonly byte[] _data;

        internal CategorySample(byte[] rawData, CategoryEntry entry, PerformanceCounterLib library)
        {
            _data = rawData;
            ReadOnlySpan<byte> data = rawData;
            _entry = entry;
            _library = library;
            int categoryIndex = entry.NameIndex;

            ref readonly PERF_DATA_BLOCK dataBlock = ref MemoryMarshal.AsRef<PERF_DATA_BLOCK>(data);

            _systemFrequency = dataBlock.PerfFreq;
            _timeStamp = dataBlock.PerfTime;
            _timeStamp100nSec = dataBlock.PerfTime100nSec;
            int pos = dataBlock.HeaderLength;
            int numPerfObjects = dataBlock.NumObjectTypes;
            if (numPerfObjects == 0)
            {
                _counterTable = new Hashtable();
                _instanceNameTable = new Hashtable(StringComparer.OrdinalIgnoreCase);
                return;
            }

            //Need to find the right category, GetPerformanceData might return
            //several of them.
            bool foundCategory = false;
            for (int index = 0; index < numPerfObjects; index++)
            {
                ref readonly PERF_OBJECT_TYPE perfObjectType = ref MemoryMarshal.AsRef<PERF_OBJECT_TYPE>(data.Slice(pos));

                if (perfObjectType.ObjectNameTitleIndex == categoryIndex)
                {
                    foundCategory = true;
                    break;
                }

                pos += perfObjectType.TotalByteLength;
            }

            if (!foundCategory)
                throw new InvalidOperationException(SR.Format(SR.CantReadCategoryIndex, categoryIndex.ToString()));

            ref readonly PERF_OBJECT_TYPE perfObject = ref MemoryMarshal.AsRef<PERF_OBJECT_TYPE>(data.Slice(pos));

            _counterFrequency = perfObject.PerfFreq;
            _counterTimeStamp = perfObject.PerfTime;
            int counterNumber = perfObject.NumCounters;
            int instanceNumber = perfObject.NumInstances;

            if (instanceNumber == -1)
                _isMultiInstance = false;
            else
                _isMultiInstance = true;

            // Move pointer forward to end of PERF_OBJECT_TYPE
            pos += perfObject.HeaderLength;

            CounterDefinitionSample[] samples = new CounterDefinitionSample[counterNumber];
            _counterTable = new Hashtable(counterNumber);
            for (int index = 0; index < samples.Length; ++index)
            {
                ref readonly PERF_COUNTER_DEFINITION perfCounter = ref MemoryMarshal.AsRef<PERF_COUNTER_DEFINITION>(data.Slice(pos));
                samples[index] = new CounterDefinitionSample(in perfCounter, this, instanceNumber);
                pos += perfCounter.ByteLength;

                int currentSampleType = samples[index]._counterType;
                if (!PerformanceCounterLib.IsBaseCounter(currentSampleType))
                {
                    // We'll put only non-base counters in the table.
                    if (currentSampleType != Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_NODATA)
                        _counterTable[samples[index]._nameIndex] = samples[index];
                }
                else
                {
                    // it's a base counter, try to hook it up to the main counter.
                    Debug.Assert(index > 0, "Index > 0 because base counters should never be at index 0");
                    if (index > 0)
                        samples[index - 1]._baseCounterDefinitionSample = samples[index];
                }
            }

            // now set up the InstanceNameTable.
            if (!_isMultiInstance)
            {
                _instanceNameTable = new Hashtable(1, StringComparer.OrdinalIgnoreCase);
                _instanceNameTable[PerformanceCounterLib.SingleInstanceName] = 0;

                for (int index = 0; index < samples.Length; ++index)
                {
                    samples[index].SetInstanceValue(0, data.Slice(pos));
                }
            }
            else
            {
                string[] parentInstanceNames = null;
                _instanceNameTable = new Hashtable(instanceNumber, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < instanceNumber; i++)
                {
                    ref readonly PERF_INSTANCE_DEFINITION perfInstance = ref MemoryMarshal.AsRef<PERF_INSTANCE_DEFINITION>(data.Slice(pos));
                    if (perfInstance.ParentObjectTitleIndex > 0 && parentInstanceNames == null)
                        parentInstanceNames = GetInstanceNamesFromIndex(perfInstance.ParentObjectTitleIndex);

                    string instanceName = PERF_INSTANCE_DEFINITION.GetName(in perfInstance, data.Slice(pos)).ToString();
                    if (parentInstanceNames != null && perfInstance.ParentObjectInstance >= 0 && perfInstance.ParentObjectInstance < parentInstanceNames.Length - 1)
                        instanceName = parentInstanceNames[perfInstance.ParentObjectInstance] + "/" + instanceName;

                    //In some cases instance names are not unique (Process), same as perfmon
                    //generate a unique name.
                    string newInstanceName = instanceName;
                    int newInstanceNumber = 1;
                    while (true)
                    {
                        if (!_instanceNameTable.ContainsKey(newInstanceName))
                        {
                            _instanceNameTable[newInstanceName] = i;
                            break;
                        }
                        else
                        {
                            newInstanceName = instanceName + "#" + newInstanceNumber.ToString(CultureInfo.InvariantCulture);
                            ++newInstanceNumber;
                        }
                    }


                    pos += perfInstance.ByteLength;

                    for (int index = 0; index < samples.Length; ++index)
                        samples[index].SetInstanceValue(i, data.Slice(pos));

                    pos += MemoryMarshal.AsRef<PERF_COUNTER_BLOCK>(data.Slice(pos)).ByteLength;
                }
            }
        }

        internal string[] GetInstanceNamesFromIndex(int categoryIndex)
        {
            CheckDisposed();

            ReadOnlySpan<byte> data = _library.GetPerformanceData(categoryIndex.ToString(CultureInfo.InvariantCulture));

            ref readonly PERF_DATA_BLOCK dataBlock = ref MemoryMarshal.AsRef<PERF_DATA_BLOCK>(data);
            int pos = dataBlock.HeaderLength;
            int numPerfObjects = dataBlock.NumObjectTypes;

            bool foundCategory = false;
            for (int index = 0; index < numPerfObjects; index++)
            {
                ref readonly PERF_OBJECT_TYPE type = ref MemoryMarshal.AsRef<PERF_OBJECT_TYPE>(data.Slice(pos));

                if (type.ObjectNameTitleIndex == categoryIndex)
                {
                    foundCategory = true;
                    break;
                }

                pos += type.TotalByteLength;
            }

            if (!foundCategory)
                return Array.Empty<string>();

            ref readonly PERF_OBJECT_TYPE perfObject = ref MemoryMarshal.AsRef<PERF_OBJECT_TYPE>(data.Slice(pos));

            int counterNumber = perfObject.NumCounters;
            int instanceNumber = perfObject.NumInstances;
            pos += perfObject.HeaderLength;

            if (instanceNumber == -1)
                return Array.Empty<string>();

            CounterDefinitionSample[] samples = new CounterDefinitionSample[counterNumber];
            for (int index = 0; index < samples.Length; ++index)
            {
                pos += MemoryMarshal.AsRef<PERF_COUNTER_DEFINITION>(data.Slice(pos)).ByteLength;
            }

            string[] instanceNames = new string[instanceNumber];
            for (int i = 0; i < instanceNumber; i++)
            {
                ref readonly PERF_INSTANCE_DEFINITION perfInstance = ref MemoryMarshal.AsRef<PERF_INSTANCE_DEFINITION>(data.Slice(pos));
                instanceNames[i] = PERF_INSTANCE_DEFINITION.GetName(in perfInstance, data.Slice(pos)).ToString();
                pos += perfInstance.ByteLength;

                pos += MemoryMarshal.AsRef<PERF_COUNTER_BLOCK>(data.Slice(pos)).ByteLength;
            }

            return instanceNames;
        }

        internal CounterDefinitionSample GetCounterDefinitionSample(string counter)
        {
            CheckDisposed();

            for (int index = 0; index < _entry.CounterIndexes.Length; ++index)
            {
                int counterIndex = _entry.CounterIndexes[index];
                string counterName = (string)_library.NameTable[counterIndex];
                if (counterName != null)
                {
                    if (string.Equals(counterName, counter, StringComparison.OrdinalIgnoreCase))
                    {
                        CounterDefinitionSample sample = (CounterDefinitionSample)_counterTable[counterIndex];
                        if (sample == null)
                        {
                            //This is a base counter and has not been added to the table
                            foreach (CounterDefinitionSample multiSample in _counterTable.Values)
                            {
                                if (multiSample._baseCounterDefinitionSample != null &&
                                    multiSample._baseCounterDefinitionSample._nameIndex == counterIndex)
                                    return multiSample._baseCounterDefinitionSample;
                            }

                            throw new InvalidOperationException(SR.CounterLayout);
                        }
                        return sample;
                    }
                }
            }

            throw new InvalidOperationException(SR.Format(SR.CantReadCounter, counter));
        }

        internal InstanceDataCollectionCollection ReadCategory()
        {

#pragma warning disable 618
            InstanceDataCollectionCollection data = new InstanceDataCollectionCollection();
#pragma warning restore 618
            for (int index = 0; index < _entry.CounterIndexes.Length; ++index)
            {
                int counterIndex = _entry.CounterIndexes[index];

                string name = (string)_library.NameTable[counterIndex];
                if (name != null && name != string.Empty)
                {
                    CounterDefinitionSample sample = (CounterDefinitionSample)_counterTable[counterIndex];
                    if (sample != null)
                        //If the current index refers to a counter base,
                        //the sample will be null
                        data.Add(name, sample.ReadInstanceData(name));
                }
            }

            return data;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _library.ReleasePerformanceData(_data);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(SR.ObjectDisposed_CategorySampleClosed, nameof(CategorySample));
            }
        }
    }

    internal sealed class CounterDefinitionSample
    {
        internal readonly int _nameIndex;
        internal readonly int _counterType;
        internal CounterDefinitionSample _baseCounterDefinitionSample;

        private readonly int _size;
        private readonly int _offset;
        private readonly long[] _instanceValues;
        private readonly CategorySample _categorySample;

        internal CounterDefinitionSample(in PERF_COUNTER_DEFINITION perfCounter, CategorySample categorySample, int instanceNumber)
        {
            _nameIndex = perfCounter.CounterNameTitleIndex;
            _counterType = perfCounter.CounterType;
            _offset = perfCounter.CounterOffset;
            _size = perfCounter.CounterSize;
            if (instanceNumber == -1)
            {
                _instanceValues = new long[1];
            }
            else
                _instanceValues = new long[instanceNumber];

            _categorySample = categorySample;
        }

        private long ReadValue(ReadOnlySpan<byte> data)
        {
            if (_size == 4)
            {
                return (long)MemoryMarshal.Read<uint>(data.Slice(_offset));
            }
            else if (_size == 8)
            {
                return MemoryMarshal.Read<long>(data.Slice(_offset));
            }

            return -1;
        }

        internal CounterSample GetInstanceValue(string instanceName)
        {

            if (!_categorySample._instanceNameTable.ContainsKey(instanceName))
            {
                // Our native dll truncates instance names to 128 characters.  If we can't find the instance
                // with the full name, try truncating to 128 characters.
                if (instanceName.Length > SharedPerformanceCounter.InstanceNameMaxLength)
                    instanceName = instanceName.Substring(0, SharedPerformanceCounter.InstanceNameMaxLength);

                if (!_categorySample._instanceNameTable.ContainsKey(instanceName))
                    throw new InvalidOperationException(SR.Format(SR.CantReadInstance, instanceName));
            }

            int index = (int)_categorySample._instanceNameTable[instanceName];
            long rawValue = _instanceValues[index];
            long baseValue = 0;
            if (_baseCounterDefinitionSample != null)
            {
                CategorySample baseCategorySample = _baseCounterDefinitionSample._categorySample;
                int baseIndex = (int)baseCategorySample._instanceNameTable[instanceName];
                baseValue = _baseCounterDefinitionSample._instanceValues[baseIndex];
            }

            return new CounterSample(rawValue,
                                                        baseValue,
                                                        _categorySample._counterFrequency,
                                                        _categorySample._systemFrequency,
                                                        _categorySample._timeStamp,
                                                        _categorySample._timeStamp100nSec,
                                                        (PerformanceCounterType)_counterType,
                                                        _categorySample._counterTimeStamp);

        }

        internal InstanceDataCollection ReadInstanceData(string counterName)
        {
#pragma warning disable 618
            InstanceDataCollection data = new InstanceDataCollection(counterName);
#pragma warning restore 618

            string[] keys = new string[_categorySample._instanceNameTable.Count];
            _categorySample._instanceNameTable.Keys.CopyTo(keys, 0);
            int[] indexes = new int[_categorySample._instanceNameTable.Count];
            _categorySample._instanceNameTable.Values.CopyTo(indexes, 0);
            for (int index = 0; index < keys.Length; ++index)
            {
                long baseValue = 0;
                if (_baseCounterDefinitionSample != null)
                {
                    CategorySample baseCategorySample = _baseCounterDefinitionSample._categorySample;
                    int baseIndex = (int)baseCategorySample._instanceNameTable[keys[index]];
                    baseValue = _baseCounterDefinitionSample._instanceValues[baseIndex];
                }

                CounterSample sample = new CounterSample(_instanceValues[indexes[index]],
                                                        baseValue,
                                                        _categorySample._counterFrequency,
                                                        _categorySample._systemFrequency,
                                                        _categorySample._timeStamp,
                                                        _categorySample._timeStamp100nSec,
                                                        (PerformanceCounterType)_counterType,
                                                        _categorySample._counterTimeStamp);

                data.Add(keys[index], new InstanceData(keys[index], sample));
            }

            return data;
        }

        internal CounterSample GetSingleValue()
        {
            long rawValue = _instanceValues[0];
            long baseValue = 0;
            if (_baseCounterDefinitionSample != null)
                baseValue = _baseCounterDefinitionSample._instanceValues[0];

            return new CounterSample(rawValue,
                                                        baseValue,
                                                        _categorySample._counterFrequency,
                                                        _categorySample._systemFrequency,
                                                        _categorySample._timeStamp,
                                                        _categorySample._timeStamp100nSec,
                                                        (PerformanceCounterType)_counterType,
                                                        _categorySample._counterTimeStamp);
        }

        internal void SetInstanceValue(int index, ReadOnlySpan<byte> data)
        {
            long rawValue = ReadValue(data);
            _instanceValues[index] = rawValue;
        }
    }
}
