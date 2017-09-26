// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics {
    using System.Runtime.InteropServices;
    using System.Globalization;
    using System.Security.Permissions;
    using System.Security;
    using System.Text;
    using System.Threading;
    using System.Reflection;
    using System.Collections;
    using System.ComponentModel;
    using System.Collections.Specialized;
    using Microsoft.Win32;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;

    internal class PerformanceCounterLib {
        internal const string PerfShimName = "netfxperf.dll";
        private  const string PerfShimFullNameSuffix = @"\netfxperf.dll";
        private  const string PerfShimPathExp = @"%systemroot%\system32\netfxperf.dll";
        internal const string OpenEntryPoint = "OpenPerformanceData";
        internal const string CollectEntryPoint = "CollectPerformanceData";
        internal const string CloseEntryPoint = "ClosePerformanceData";
        internal const string SingleInstanceName = "systemdiagnosticsperfcounterlibsingleinstance";

        private const string PerflibPath = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Perflib";
        internal const string ServicePath = "SYSTEM\\CurrentControlSet\\Services";
        private const string categorySymbolPrefix = "OBJECT_";
        private const string conterSymbolPrefix = "DEVICE_COUNTER_";
        private const string helpSufix = "_HELP";
        private const string nameSufix = "_NAME";
        private const string textDefinition = "[text]";
        private const string infoDefinition = "[info]";
        private const string languageDefinition = "[languages]";
        private const string objectDefinition = "[objects]";
        private const string driverNameKeyword = "drivername";
        private const string symbolFileKeyword = "symbolfile";
        private const string defineKeyword = "#define";
        private const string languageKeyword = "language";
        private const string DllName = "netfxperf.dll";

        private const int EnglishLCID = 0x009;

        private static volatile string computerName;
        private static volatile string iniFilePath;
        private static volatile string symbolFilePath;

        private PerformanceMonitor performanceMonitor;
        private string machineName;
        private string perfLcid;

        private Hashtable customCategoryTable;
        private static volatile Hashtable libraryTable;
        private Hashtable categoryTable;
        private Hashtable nameTable;
        private Hashtable helpTable;
        private readonly object CategoryTableLock = new Object();
        private readonly object NameTableLock = new Object();
        private readonly object HelpTableLock = new Object();

        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject {
            get {
                if (s_InternalSyncObject == null) {
                    Object o = new Object();
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }

        internal PerformanceCounterLib(string machineName, string lcid) {
            this.machineName = machineName;
            this.perfLcid = lcid;
        }

        /// <internalonly/>
        internal static string ComputerName {
            get {
                if (computerName == null) {
                    lock (InternalSyncObject) {
                        if (computerName == null) {
                            StringBuilder sb = new StringBuilder(256);
                            SafeNativeMethods.GetComputerName(sb, new int[] {sb.Capacity});
                            computerName = sb.ToString();
                        }
                    }
                }

                return computerName;
            }
        }

        private unsafe Hashtable CategoryTable {
            get {
                if (this.categoryTable == null) {
                    lock (this.CategoryTableLock) {
                        if (this.categoryTable == null) {
                            byte[] perfData =  GetPerformanceData("Global");

                            fixed (byte* perfDataPtr = perfData) {
                                IntPtr dataRef = new IntPtr( (void*) perfDataPtr);
                                Interop.Advapi32.PERF_DATA_BLOCK dataBlock = new Interop.Advapi32.PERF_DATA_BLOCK();
                                Marshal.PtrToStructure(dataRef, dataBlock);
                                dataRef = (IntPtr)((long)dataRef + dataBlock.HeaderLength);
                                int categoryNumber = dataBlock.NumObjectTypes;

                                // on some machines MSMQ claims to have 4 categories, even though it only has 2.
                                // This causes us to walk past the end of our data, potentially crashing or reading
                                // data we shouldn't.  We use endPerfData to make sure we don't go past the end
                                // of the perf data.  (ASURT 137097)
                                long endPerfData = (long)(new IntPtr((void*)perfDataPtr)) + dataBlock.TotalByteLength;
                                Hashtable tempCategoryTable = new Hashtable(categoryNumber, StringComparer.OrdinalIgnoreCase);
                                for (int index = 0; index < categoryNumber && ((long) dataRef < endPerfData); index++) {
                                    Interop.Advapi32.PERF_OBJECT_TYPE perfObject = new Interop.Advapi32.PERF_OBJECT_TYPE();

                                    Marshal.PtrToStructure(dataRef, perfObject);

                                    CategoryEntry newCategoryEntry = new CategoryEntry(perfObject);
                                    IntPtr nextRef =  (IntPtr)((long)dataRef + perfObject.TotalByteLength);
                                    dataRef = (IntPtr)((long)dataRef + perfObject.HeaderLength);

                                    int index3 = 0;
                                    int previousCounterIndex = -1;
                                    //Need to filter out counters that are repeated, some providers might
                                    //return several adyacent copies of the same counter.
                                    for (int index2 = 0; index2 < newCategoryEntry.CounterIndexes.Length; ++ index2) {
                                        Interop.Advapi32.PERF_COUNTER_DEFINITION perfCounter = new Interop.Advapi32.PERF_COUNTER_DEFINITION();
                                        Marshal.PtrToStructure(dataRef, perfCounter);
                                        if (perfCounter.CounterNameTitleIndex != previousCounterIndex) {
                                            newCategoryEntry.CounterIndexes[index3] =  perfCounter.CounterNameTitleIndex;
                                            newCategoryEntry.HelpIndexes[index3] = perfCounter.CounterHelpTitleIndex;
                                            previousCounterIndex = perfCounter.CounterNameTitleIndex;
                                            ++ index3;
                                        }
                                        dataRef = (IntPtr)((long)dataRef + perfCounter.ByteLength);
                                    }

                                    //Lets adjust the entry counter arrays in case there were repeated copies
                                    if (index3 <  newCategoryEntry.CounterIndexes.Length) {
                                        int[] adjustedCounterIndexes = new int[index3];
                                        int[] adjustedHelpIndexes = new int[index3];
                                        Array.Copy(newCategoryEntry.CounterIndexes, adjustedCounterIndexes, index3);
                                        Array.Copy(newCategoryEntry.HelpIndexes, adjustedHelpIndexes, index3);
                                        newCategoryEntry.CounterIndexes = adjustedCounterIndexes;
                                        newCategoryEntry.HelpIndexes = adjustedHelpIndexes;
                                    }

                                    string categoryName = (string)this.NameTable[newCategoryEntry.NameIndex];
                                    if (categoryName != null)
                                        tempCategoryTable[categoryName] = newCategoryEntry;

                                    dataRef = nextRef;
                                }

                                this.categoryTable = tempCategoryTable;
                            }
                        }
                    }
                }

                return this.categoryTable;
            }
        }

        internal Hashtable HelpTable {
            get {
                if (this.helpTable == null) {
                    lock(this.HelpTableLock) {
                        if (this.helpTable == null)
                            this.helpTable = GetStringTable(true);
                    }
                }

                return this.helpTable;
            }
        }

        // Returns a temp file name
        private static string IniFilePath {
            get {
                if (iniFilePath == null) {
                    lock (InternalSyncObject) {
                        if (iniFilePath == null) {
                            // Need to assert Environment permissions here
                            //                        the environment check is not exposed as a public
                            //                        method
                            EnvironmentPermission environmentPermission = new EnvironmentPermission(PermissionState.Unrestricted);
                            environmentPermission.Assert();
                            try {
                                iniFilePath = Path.GetTempFileName();
                            }
                            finally {
                                 EnvironmentPermission.RevertAssert();
                            }
                        }
                    }
                }

                return iniFilePath;
            }
        }

        internal Hashtable NameTable {
            get {
                if (this.nameTable == null) {
                    lock(this.NameTableLock) {
                        if (this.nameTable == null)
                            this.nameTable = GetStringTable(false);
                    }
                }

                return this.nameTable;
            }
        }

        // Returns a temp file name
        private static string SymbolFilePath {
            get {
                if (symbolFilePath == null) {
                    lock (InternalSyncObject) {
                        if (symbolFilePath == null) {
                            string tempPath;
                            
                            EnvironmentPermission environmentPermission = new EnvironmentPermission(PermissionState.Unrestricted);
                            environmentPermission.Assert();
                            tempPath = Path.GetTempPath();
                            EnvironmentPermission.RevertAssert();

                            // We need both FileIOPermission EvironmentPermission
                            PermissionSet ps = new PermissionSet(PermissionState.None);
                            ps.AddPermission(new EnvironmentPermission(PermissionState.Unrestricted));
                            ps.AddPermission(new FileIOPermission(FileIOPermissionAccess.Write, tempPath));
                            ps.Assert();
                            try {
                                symbolFilePath = Path.GetTempFileName();
                            }
                            finally {
                                 PermissionSet.RevertAssert();
                            }
                        }
                    }
                }

                return symbolFilePath;
            }
        }

        internal static bool CategoryExists(string machine, string category) {
            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            if (library.CategoryExists(category)) 
                return true;

            if (CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID) {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture) {
                    library = GetPerformanceCounterLib(machine, culture);
                    if (library.CategoryExists(category))
                        return true;
                    culture = culture.Parent;
                }
            }

            return false;
        }

        internal bool CategoryExists(string category) {
            return CategoryTable.ContainsKey(category);
        }

        internal static void CloseAllLibraries() {
            if (libraryTable != null) {
                foreach (PerformanceCounterLib library in libraryTable.Values)
                    library.Close();

                libraryTable = null;
            }
        }

        internal static void CloseAllTables() {
            if (libraryTable != null) {
                foreach (PerformanceCounterLib library in libraryTable.Values)
                    library.CloseTables();
            }
        }

        internal void CloseTables() {
            this.nameTable = null;
            this.helpTable = null;
            this.categoryTable = null;
            this.customCategoryTable = null;
        }

        internal void Close() {
            if (this.performanceMonitor != null) {
                this.performanceMonitor.Close();
                this.performanceMonitor = null;
            }

            CloseTables();
        }

        internal static bool CounterExists(string machine, string category, string counter) {
            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            bool categoryExists = false;
            bool counterExists = library.CounterExists(category, counter, ref categoryExists);

            if (!categoryExists && CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID) {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture) {
                    library = GetPerformanceCounterLib(machine, culture);
                    counterExists = library.CounterExists(category, counter, ref categoryExists);
                    if (counterExists)
                        break;

                    culture = culture.Parent;
                }
            }

            if (!categoryExists) {
                // Consider adding diagnostic logic here, may be we can dump the nameTable...
                throw new InvalidOperationException(SR.Format(SR.MissingCategory));
            }

            return counterExists;
        }

        private bool CounterExists(string category, string counter, ref bool categoryExists) {
            categoryExists = false;
            if (!CategoryTable.ContainsKey(category))
                return false;
            else
                categoryExists = true;

            CategoryEntry entry = (CategoryEntry)this.CategoryTable[category];
            for (int index = 0; index < entry.CounterIndexes.Length; ++ index) {
                int counterIndex = entry.CounterIndexes[index];
                string counterName = (string)this.NameTable[counterIndex];
                if (counterName == null)
                   counterName = String.Empty;

                if (String.Compare(counterName, counter, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }

            return false;
        }

        private static void CreateIniFile(string categoryName, string categoryHelp, CounterCreationDataCollection creationData, string[] languageIds) {
            //SECREVIEW: PerformanceCounterPermission must have been demanded before
            FileIOPermission permission = new FileIOPermission(PermissionState.Unrestricted);
            permission.Assert();
            try {
                StreamWriter iniWriter = new StreamWriter(IniFilePath, false, Encoding.Unicode);
                try {
                    //NT4 won't be able to parse Unicode ini files without this
                    //extra white space.
                    iniWriter.WriteLine("");
                    iniWriter.WriteLine(infoDefinition);
                    iniWriter.Write(driverNameKeyword);
                    iniWriter.Write("=");
                    iniWriter.WriteLine(categoryName);
                    iniWriter.Write(symbolFileKeyword);
                    iniWriter.Write("=");
                    iniWriter.WriteLine(Path.GetFileName(SymbolFilePath));
                    iniWriter.WriteLine("");

                    iniWriter.WriteLine(languageDefinition);
                    foreach (string languageId in languageIds) {
                        iniWriter.Write(languageId);
                        iniWriter.Write("=");
                        iniWriter.Write(languageKeyword);
                        iniWriter.WriteLine(languageId);
                    }
                    iniWriter.WriteLine("");

                    iniWriter.WriteLine(objectDefinition);
                    foreach (string languageId in languageIds) {
                        iniWriter.Write(categorySymbolPrefix);
                        iniWriter.Write("1_");
                        iniWriter.Write(languageId);
                        iniWriter.Write(nameSufix);
                        iniWriter.Write("=");
                        iniWriter.WriteLine(categoryName);
                    }
                    iniWriter.WriteLine("");

                    iniWriter.WriteLine(textDefinition);
                    foreach (string languageId in languageIds) {
                        iniWriter.Write(categorySymbolPrefix);
                        iniWriter.Write("1_");
                        iniWriter.Write(languageId);
                        iniWriter.Write(nameSufix);
                        iniWriter.Write("=");
                        iniWriter.WriteLine(categoryName);
                        iniWriter.Write(categorySymbolPrefix);
                        iniWriter.Write("1_");
                        iniWriter.Write(languageId);
                        iniWriter.Write(helpSufix);
                        iniWriter.Write("=");
                        if (categoryHelp == null || categoryHelp == String.Empty)
                            iniWriter.WriteLine(SR.Format(SR.HelpNotAvailable));
                        else
                            iniWriter.WriteLine(categoryHelp);


                        int counterIndex = 0;
                        foreach (CounterCreationData counterData in creationData) {
                            ++counterIndex;
                            iniWriter.WriteLine("");
                            iniWriter.Write(conterSymbolPrefix);
                            iniWriter.Write(counterIndex.ToString(CultureInfo.InvariantCulture));
                            iniWriter.Write("_");
                            iniWriter.Write(languageId);
                            iniWriter.Write(nameSufix);
                            iniWriter.Write("=");
                            iniWriter.WriteLine(counterData.CounterName);

                            iniWriter.Write(conterSymbolPrefix);
                            iniWriter.Write(counterIndex.ToString(CultureInfo.InvariantCulture));
                            iniWriter.Write("_");
                            iniWriter.Write(languageId);
                            iniWriter.Write(helpSufix);
                            iniWriter.Write("=");

                            Debug.Assert(!String.IsNullOrEmpty(counterData.CounterHelp), "CounterHelp should have been fixed up by the caller");
                            iniWriter.WriteLine(counterData.CounterHelp);
                        }
                    }

                    iniWriter.WriteLine("");
                }
                finally {
                    iniWriter.Close();
                }
            }
            finally {
                FileIOPermission.RevertAssert();
            }
        }

        private static void CreateRegistryEntry(string categoryName, PerformanceCounterCategoryType categoryType, CounterCreationDataCollection creationData, ref bool iniRegistered) {
            RegistryKey serviceParentKey = null;
            RegistryKey serviceKey = null;
            RegistryKey linkageKey = null;

            //SECREVIEW: Whoever is able to call this function, must already
            //                         have demmanded PerformanceCounterPermission
            //                         we can therefore assert the RegistryPermission.
            RegistryPermission registryPermission = new RegistryPermission(PermissionState.Unrestricted);
            registryPermission.Assert();
            try {
                serviceParentKey = Registry.LocalMachine.OpenSubKey(ServicePath, true);

                serviceKey = serviceParentKey.OpenSubKey(categoryName + "\\Performance", true);
                if (serviceKey == null)
                    serviceKey = serviceParentKey.CreateSubKey(categoryName + "\\Performance");

                serviceKey.SetValue("Open","OpenPerformanceData");
                serviceKey.SetValue("Collect", "CollectPerformanceData");
                serviceKey.SetValue("Close","ClosePerformanceData");
                serviceKey.SetValue("Library", DllName);
                serviceKey.SetValue("IsMultiInstance", (int) categoryType, RegistryValueKind.DWord);
                serviceKey.SetValue("CategoryOptions", 0x3, RegistryValueKind.DWord);

                string [] counters = new string[creationData.Count];
                string [] counterTypes = new string[creationData.Count];
                for (int i = 0; i < creationData.Count; i++) {
                    counters[i] = creationData[i].CounterName;
                    counterTypes[i] = ((int) creationData[i].CounterType).ToString(CultureInfo.InvariantCulture);
                }

                linkageKey = serviceParentKey.OpenSubKey(categoryName + "\\Linkage" , true);
                if (linkageKey == null)
                    linkageKey = serviceParentKey.CreateSubKey(categoryName + "\\Linkage" );

                linkageKey.SetValue("Export", new string[]{categoryName});

                serviceKey.SetValue("Counter Types", (object) counterTypes);
                serviceKey.SetValue("Counter Names", (object) counters);

                object firstID = serviceKey.GetValue("First Counter");
                if (firstID != null)
                    iniRegistered  = true;
                else
                    iniRegistered  = false;
            }
            finally {
                if (serviceKey != null)
                    serviceKey.Close();

                if (linkageKey != null)
                    linkageKey.Close();

                if (serviceParentKey != null)
                    serviceParentKey.Close();

                RegistryPermission.RevertAssert();
            }
        }

        private static void CreateSymbolFile(CounterCreationDataCollection creationData) {
            //SECREVIEW: PerformanceCounterPermission must have been demanded before
            FileIOPermission permission = new FileIOPermission(PermissionState.Unrestricted);
            permission.Assert();
            try {
                StreamWriter symbolWriter = new StreamWriter(SymbolFilePath);
                try {
                    symbolWriter.Write(defineKeyword);
                    symbolWriter.Write(" ");
                    symbolWriter.Write(categorySymbolPrefix);
                    symbolWriter.WriteLine("1 0;");

                    for (int counterIndex = 1; counterIndex <= creationData.Count; ++ counterIndex) {
                        symbolWriter.Write(defineKeyword);
                        symbolWriter.Write(" ");
                        symbolWriter.Write(conterSymbolPrefix);
                        symbolWriter.Write(counterIndex.ToString(CultureInfo.InvariantCulture));
                        symbolWriter.Write(" ");
                        symbolWriter.Write((counterIndex * 2).ToString(CultureInfo.InvariantCulture));
                        symbolWriter.WriteLine(";");
                    }

                    symbolWriter.WriteLine("");
                }
                finally {
                    symbolWriter.Close();
                }
            }
            finally {
                FileIOPermission.RevertAssert();
            }
        }

        private static void DeleteRegistryEntry(string categoryName) {
            RegistryKey serviceKey = null;

            //SECREVIEW: Whoever is able to call this function, must already
            //                         have demmanded PerformanceCounterPermission
            //                         we can therefore assert the RegistryPermission.
            RegistryPermission registryPermission = new RegistryPermission(PermissionState.Unrestricted);
            registryPermission.Assert();
            try {
                serviceKey = Registry.LocalMachine.OpenSubKey(ServicePath, true);

                bool deleteCategoryKey = false;
                using (RegistryKey categoryKey = serviceKey.OpenSubKey(categoryName, true)) {
                    if (categoryKey != null) {
                        if (categoryKey.GetValueNames().Length == 0) {
                            deleteCategoryKey = true;
                        }
                        else {
                            categoryKey.DeleteSubKeyTree("Linkage");
                            categoryKey.DeleteSubKeyTree("Performance");
                        }
                    }
                }
                if (deleteCategoryKey)
                    serviceKey.DeleteSubKeyTree(categoryName);
                
            }
            finally {
                if (serviceKey != null)
                    serviceKey.Close();

                RegistryPermission.RevertAssert();
            }
        }

        private static void DeleteTemporaryFiles() {
            try {
                File.Delete(IniFilePath);
            }
            catch {
            }

            try {
                File.Delete(SymbolFilePath);
            }
            catch {
            }
        }

        // Ensures that the customCategoryTable is initialized and decides whether the category passed in 
        //  1) is a custom category
        //  2) is a multi instance custom category
        // The return value is whether the category is a custom category or not. 
        internal bool FindCustomCategory(string category, out PerformanceCounterCategoryType categoryType) {
            RegistryKey key = null;
            RegistryKey baseKey = null;
            categoryType = PerformanceCounterCategoryType.Unknown;
            
            if (this.customCategoryTable == null) {
                Interlocked.CompareExchange(ref this.customCategoryTable, new Hashtable(StringComparer.OrdinalIgnoreCase), null);
            }

            if (this.customCategoryTable.ContainsKey(category)) {
                categoryType= (PerformanceCounterCategoryType) this.customCategoryTable[category];
                return true;
            }
            else {
                //SECREVIEW: Whoever is able to call this function, must already
                //                         have demanded PerformanceCounterPermission
                //                         we can therefore assert the RegistryPermission.
                PermissionSet ps = new PermissionSet(PermissionState.None);
                ps.AddPermission(new RegistryPermission(PermissionState.Unrestricted));
                ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.UnmanagedCode));
                ps.Assert();
                try {
                    string keyPath = ServicePath + "\\" + category + "\\Performance";
                    if (machineName == "." || String.Compare(this.machineName, ComputerName, StringComparison.OrdinalIgnoreCase) == 0) {
                        key = Registry.LocalMachine.OpenSubKey(keyPath);
                    }
                    else {
                        baseKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "\\\\" + this.machineName);
                        if (baseKey != null) {
                            try {
                                key = baseKey.OpenSubKey(keyPath);
                            } catch (SecurityException) {
                                // we may not have permission to read the registry key on the remote machine.  The security exception  
                                // is thrown when RegOpenKeyEx returns ERROR_ACCESS_DENIED or ERROR_BAD_IMPERSONATION_LEVEL
                                //
                                // In this case we return an 'Unknown' category type and 'false' to indicate the category is *not* custom.
                                //
                                categoryType = PerformanceCounterCategoryType.Unknown;
                                this.customCategoryTable[category] = categoryType;
                                return false;
                            }
                        }
                    }

                    if (key != null) {
                        object systemDllName = key.GetValue("Library", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                        if (systemDllName != null && systemDllName is string 
                            && (String.Compare((string)systemDllName, PerformanceCounterLib.PerfShimName, StringComparison.OrdinalIgnoreCase) == 0
                              || ((string)systemDllName).EndsWith(PerformanceCounterLib.PerfShimFullNameSuffix, StringComparison.OrdinalIgnoreCase))) {
                            
                            object isMultiInstanceObject = key.GetValue("IsMultiInstance");
                            if (isMultiInstanceObject != null) {
                                categoryType = (PerformanceCounterCategoryType) isMultiInstanceObject;
                                if (categoryType < PerformanceCounterCategoryType.Unknown || categoryType > PerformanceCounterCategoryType.MultiInstance)
                                    categoryType = PerformanceCounterCategoryType.Unknown;
                            }
                            else
                                categoryType = PerformanceCounterCategoryType.Unknown;
                                
                            object objectID = key.GetValue("First Counter");
                            if (objectID != null) {
                                int firstID = (int)objectID;

                                this.customCategoryTable[category] = categoryType;
                                return true;
                            }
                        }
                    }
                }
                finally {
                    if (key != null) key.Close();
                    if (baseKey != null) baseKey.Close();
                    PermissionSet.RevertAssert();
                }
            }
            return false;
        }

        internal static string[] GetCategories(string machineName) {
            PerformanceCounterLib library;
            CultureInfo culture = CultureInfo.CurrentCulture;
            while (culture != CultureInfo.InvariantCulture) {
                library = GetPerformanceCounterLib(machineName, culture);
                string[] categories = library.GetCategories();
                if (categories.Length != 0 ) 
                    return categories;
                culture = culture.Parent;
            }

            library = GetPerformanceCounterLib(machineName, new CultureInfo(EnglishLCID));
            return library.GetCategories();
        }

        internal string[] GetCategories() {
            ICollection keys = CategoryTable.Keys;
            string[] categories = new string[keys.Count];
            keys.CopyTo(categories, 0);
            return categories;
        }

        internal static string GetCategoryHelp(string machine, string category) {
            PerformanceCounterLib library;
            string help;

            //First check the current culture for the category. This will allow 
            //PerformanceCounterCategory.CategoryHelp to return localized strings.
            if(CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID) {
                CultureInfo culture = CultureInfo.CurrentCulture;
               
                while (culture != CultureInfo.InvariantCulture) {
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
                throw new InvalidOperationException(SR.Format(SR.MissingCategory));

            return help;
        }

        private string GetCategoryHelp(string category) {
            CategoryEntry entry = (CategoryEntry)this.CategoryTable[category];
            if (entry == null)
                return null;

            return (string)this.HelpTable[entry.HelpIndex];
        }

        internal static CategorySample GetCategorySample(string machine, string category) {
            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            CategorySample sample = library.GetCategorySample(category);
            if (sample == null && CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID) {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture) {
                    library = GetPerformanceCounterLib(machine, culture);
                    sample = library.GetCategorySample(category);
                    if (sample != null)
                        return sample;
                    culture = culture.Parent;
                }
            }
            if (sample == null)
                throw new InvalidOperationException(SR.Format(SR.MissingCategory));

            return sample;
        }

        private CategorySample GetCategorySample(string category) {
            CategoryEntry entry = (CategoryEntry)this.CategoryTable[category];
            if (entry == null)
                return null;

            CategorySample sample = null;
            byte[] dataRef = GetPerformanceData(entry.NameIndex.ToString(CultureInfo.InvariantCulture));
            if (dataRef == null)
                throw new InvalidOperationException(SR.Format(SR.CantReadCategory, category));

            sample = new CategorySample(dataRef, entry, this);
            return sample;
        }

        internal static string[] GetCounters(string machine, string category) {
            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            bool categoryExists = false;
            string[] counters = library.GetCounters(category, ref categoryExists);

            if (!categoryExists && CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID) {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture) {
                    library = GetPerformanceCounterLib(machine, culture);
                    counters = library.GetCounters(category, ref categoryExists);
                    if (categoryExists)
                        return counters;

                    culture = culture.Parent;
                }
            }
            
            if (!categoryExists)
                throw new InvalidOperationException(SR.Format(SR.MissingCategory));

            return counters;
        }

        private string[] GetCounters(string category, ref bool categoryExists) {
            categoryExists = false;
            CategoryEntry entry = (CategoryEntry)this.CategoryTable[category];
            if (entry == null)
                return null;
            else
                categoryExists = true;

            int index2 = 0;
            string[] counters = new string[entry.CounterIndexes.Length];
            for (int index = 0; index < counters.Length; ++ index) {
                int counterIndex = entry.CounterIndexes[index];
                string counterName = (string)this.NameTable[counterIndex];
                if (counterName != null && counterName != String.Empty) {
                    counters[index2] = counterName;
                    ++index2;
                }
            }

            //Lets adjust the array in case there were null entries
            if (index2 < counters.Length) {
                string[] adjustedCounters = new string[index2];
                Array.Copy(counters, adjustedCounters, index2);
                counters = adjustedCounters;
            }

            return counters;
        }

        internal static string GetCounterHelp(string machine, string category, string counter) {
            PerformanceCounterLib library;
            bool categoryExists = false;
            string help;

            //First check the current culture for the counter. This will allow 
            //PerformanceCounter.CounterHelp to return localized strings.            
            if (CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID) {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture) {
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

        private string GetCounterHelp(string category, string counter, ref bool categoryExists) {
            categoryExists = false;
            CategoryEntry entry = (CategoryEntry)this.CategoryTable[category];
            if (entry == null)
                return null;
            else
                categoryExists = true;

            int helpIndex = -1;
            for (int index = 0; index < entry.CounterIndexes.Length; ++ index) {
                int counterIndex = entry.CounterIndexes[index];
                string counterName = (string)this.NameTable[counterIndex];
                if (counterName == null)
                   counterName = String.Empty;

                if (String.Compare(counterName, counter, StringComparison.OrdinalIgnoreCase) == 0) {
                    helpIndex = entry.HelpIndexes[index];
                    break;
                }
            }

            if (helpIndex == -1)
                throw new InvalidOperationException(SR.Format(SR.MissingCounter, counter));

            string help = (string)this.HelpTable[helpIndex];
            if (help == null)
                return String.Empty;
            else
                return help;
        }

        internal string GetCounterName(int index) {
            if (this.NameTable.ContainsKey(index))
                return (string)this.NameTable[index];

            return "";
        }

        private static string[] GetLanguageIds() {
            RegistryKey libraryParentKey = null;
            string[] ids = new string[0];
            new RegistryPermission(PermissionState.Unrestricted).Assert();
            try {
                libraryParentKey = Registry.LocalMachine.OpenSubKey(PerflibPath);

                if (libraryParentKey != null)
                    ids = libraryParentKey.GetSubKeyNames();
            }
            finally {
                if (libraryParentKey != null)
                    libraryParentKey.Close();

                RegistryPermission.RevertAssert();
            }

            return ids;
        }

        internal static PerformanceCounterLib GetPerformanceCounterLib(string machineName, CultureInfo culture) {
            SharedUtils.CheckEnvironment();

            string lcidString = culture.LCID.ToString("X3", CultureInfo.InvariantCulture);
            if (machineName.CompareTo(".") == 0)
                machineName = ComputerName.ToLower(CultureInfo.InvariantCulture);
            else
                machineName = machineName.ToLower(CultureInfo.InvariantCulture);

            if (PerformanceCounterLib.libraryTable == null) {
                lock (InternalSyncObject) {
                    if (PerformanceCounterLib.libraryTable == null)
                        PerformanceCounterLib.libraryTable = new Hashtable();
                }
            }

            string libraryKey = machineName + ":" + lcidString;
            if (PerformanceCounterLib.libraryTable.Contains(libraryKey))
                return (PerformanceCounterLib)PerformanceCounterLib.libraryTable[libraryKey];
            else {
                PerformanceCounterLib library = new PerformanceCounterLib(machineName, lcidString);
                PerformanceCounterLib.libraryTable[libraryKey] = library;
                return library;
            }
        }

        internal byte[] GetPerformanceData(string item) {
            if (this.performanceMonitor == null) {
                lock (InternalSyncObject) {
                    if (this.performanceMonitor == null)
                        this.performanceMonitor = new PerformanceMonitor(this.machineName);
                }
            }

           return this.performanceMonitor.GetData(item);
        }

        private Hashtable GetStringTable(bool isHelp) {
            Hashtable stringTable;
            RegistryKey libraryKey;

            PermissionSet ps = new PermissionSet(PermissionState.None);
            ps.AddPermission(new RegistryPermission(PermissionState.Unrestricted));
            ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.UnmanagedCode));
            ps.Assert();

            if (String.Compare(this.machineName, ComputerName, StringComparison.OrdinalIgnoreCase) == 0)
                libraryKey = Registry.PerformanceData;
            else {
                libraryKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.PerformanceData, this.machineName);
            }

            try {
                string[] names = null;
                int waitRetries = 14;   //((2^13)-1)*10ms == approximately 1.4mins
                int waitSleep = 0;

                // In some stress situations, querying counter values from 
                // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\009 
                // often returns null/empty data back. We should build fault-tolerance logic to 
                // make it more reliable because getting null back once doesn't necessarily mean 
                // that the data is corrupted, most of the time we would get the data just fine 
                // in subsequent tries.
                while (waitRetries > 0) {
                    try {
                        if (!isHelp)
                            names = (string[])libraryKey.GetValue("Counter " + perfLcid);
                        else
                            names = (string[])libraryKey.GetValue("Explain " + perfLcid);

                        if ((names == null) || (names.Length == 0)) {
                            --waitRetries;
                            if (waitSleep == 0)
                                waitSleep = 10;
                            else {
                                System.Threading.Thread.Sleep(waitSleep);
                                waitSleep *= 2;
                            }
                        }
                        else
                            break;
                    }
                    catch (IOException) {
                        // RegistryKey throws if it can't find the value.  We want to return an empty table
                        // and throw a different exception higher up the stack. 
                        names = null;
                        break;
                    }
                    catch (InvalidCastException) {
                        // Unable to cast object of type 'System.Byte[]' to type 'System.String[]'.
                        // this happens when the registry data store is corrupt and the type is not even REG_MULTI_SZ
                        names = null;
                        break;
                    }
                }

                if (names == null)
                    stringTable = new Hashtable();
                else {
                    stringTable = new Hashtable(names.Length/2);
                    
                    for (int index = 0; index < (names.Length/2); ++ index) {
                        string nameString =  names[(index *2) + 1];
                        if (nameString == null)
                            nameString = String.Empty;
                        
                        int key;
                        if (!Int32.TryParse(names[index * 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out key)) {
                            if (isHelp) {
                                // Category Help Table
                                throw new InvalidOperationException(SR.Format(SR.CategoryHelpCorrupt, names[index * 2]));
                            }
                            else {
                                // Counter Name Table 
                                throw new InvalidOperationException(SR.Format(SR.CounterNameCorrupt, names[index * 2]));
                            }
                        }

                        stringTable[key] = nameString;
                    }
                }
            }
            finally {
                libraryKey.Close();
            }

            return stringTable;
        }
        
        internal static bool IsCustomCategory(string machine, string category) {
            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            if (library.IsCustomCategory(category)) 
                return true;

            if (CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID) {
                CultureInfo culture = CultureInfo.CurrentCulture;
                while (culture != CultureInfo.InvariantCulture) {
                    library = GetPerformanceCounterLib(machine, culture);
                    if (library.IsCustomCategory(category))
                        return true;
                    culture = culture.Parent;
                }
            }

            return false;
        }

        internal static bool IsBaseCounter(int type) {
            return (type == Interop.Kernel32.PerformanceCounterOptions.PERF_AVERAGE_BASE ||
                    type == Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_MULTI_BASE ||
                    type == Interop.Kernel32.PerformanceCounterOptions.PERF_RAW_BASE  ||
                    type == Interop.Kernel32.PerformanceCounterOptions.PERF_LARGE_RAW_BASE  ||
                    type == Interop.Kernel32.PerformanceCounterOptions.PERF_SAMPLE_BASE);
        }
        
        private bool IsCustomCategory(string category) {
            PerformanceCounterCategoryType categoryType;
            
            return FindCustomCategory(category, out categoryType);
        }

        internal static PerformanceCounterCategoryType GetCategoryType(string machine, string category) {
            PerformanceCounterCategoryType categoryType = PerformanceCounterCategoryType.Unknown;
            
            PerformanceCounterLib library = GetPerformanceCounterLib(machine, new CultureInfo(EnglishLCID));
            if (!library.FindCustomCategory(category, out categoryType)) {
                if (CultureInfo.CurrentCulture.Parent.LCID != EnglishLCID) {
                    CultureInfo culture = CultureInfo.CurrentCulture;
                    while (culture != CultureInfo.InvariantCulture) {
                        library = GetPerformanceCounterLib(machine, culture);
                        if (library.FindCustomCategory(category, out categoryType))
                            return categoryType;
                        culture = culture.Parent;
                    }
                }
            }
            return categoryType;
        }

        internal static void RegisterCategory(string categoryName, PerformanceCounterCategoryType categoryType, string categoryHelp, CounterCreationDataCollection creationData) {
            try {
                bool iniRegistered = false;
                CreateRegistryEntry(categoryName, categoryType, creationData, ref iniRegistered);
                if (!iniRegistered) {
                    string[] languageIds = GetLanguageIds();
                    CreateIniFile(categoryName, categoryHelp, creationData, languageIds);
                    CreateSymbolFile(creationData);
                    RegisterFiles(IniFilePath, false);
                }
                CloseAllTables();
                CloseAllLibraries();
            }
            finally {
                DeleteTemporaryFiles();
            }
        }

        private static void RegisterFiles(string arg0, bool unregister) {
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
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
            try {
                processStartInfo.Arguments = "\"" + arg0 + "\"";
                p = Process.Start(processStartInfo);
                p.WaitForExit();

                res = p.ExitCode;
            }
            finally {
                SecurityPermission.RevertAssert();
            }
            

            if (res == Interop.Errors.ERROR_ACCESS_DENIED) {
                throw new UnauthorizedAccessException(SR.Format(SR.CantChangeCategoryRegistration, arg0));
            }

            // Look at Q269225, unlodctr might return 2 when WMI is not installed.
            if (unregister && res == 2)
                res = 0;

            if (res != 0)
                throw SharedUtils.CreateSafeWin32Exception(res);
        }

        internal static void UnregisterCategory(string categoryName) {
            RegisterFiles(categoryName, true);
            DeleteRegistryEntry(categoryName);
            CloseAllTables();
            CloseAllLibraries();
        }
    }

    internal class PerformanceMonitor {
        private RegistryKey perfDataKey = null;
        private string machineName;

        internal PerformanceMonitor(string machineName) {
            this.machineName = machineName;
            Init();
        }

        private void Init() {
            try {
                if (machineName != "." && String.Compare(machineName, PerformanceCounterLib.ComputerName, StringComparison.OrdinalIgnoreCase) != 0) {
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
                    perfDataKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.PerformanceData, machineName);
                }
                else
                    perfDataKey = Registry.PerformanceData;
            }
            catch (UnauthorizedAccessException) {
                // we need to do this for compatibility with v1.1 and v1.0.
                throw new Win32Exception(Interop.Errors.ERROR_ACCESS_DENIED);
            }
            catch (IOException e) {
                // we need to do this for compatibility with v1.1 and v1.0.
                throw new Win32Exception(Marshal.GetHRForException(e));
            }
        }

        internal void Close() {
            if( perfDataKey != null)
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
        internal byte[] GetData(string item) {
            int waitRetries = 17;   //2^16*10ms == approximately 10mins
            int waitSleep = 0;
            byte[] data = null;
            int error = 0;

            // no need to revert here since we'll fall off the end of the method
            new RegistryPermission(PermissionState.Unrestricted).Assert();
            while (waitRetries > 0) {
                try {
                    data = (byte[]) perfDataKey.GetValue(item);
                    return data;
                }
                catch (IOException e) {
                    error = Marshal.GetHRForException(e);
                    switch (error) {
                        case Interop.Advapi32.RPCStatus.RPC_S_CALL_FAILED:
                        case Interop.Errors.ERROR_INVALID_HANDLE:
                        case Interop.Advapi32.RPCStatus.RPC_S_SERVER_UNAVAILABLE:
                            Init();
                            goto case Interop.Advapi32.WaitOptions.WAIT_TIMEOUT;

                        case Interop.Advapi32.WaitOptions.WAIT_TIMEOUT:
                        case Interop.Errors.ERROR_NOT_READY:
                        case Interop.Errors.ERROR_LOCK_FAILED:
                        case Interop.Errors.ERROR_BUSY:
                            --waitRetries;
                            if (waitSleep == 0) {
                                waitSleep = 10;
                            }
                            else {
                                System.Threading.Thread.Sleep(waitSleep);
                                waitSleep *= 2;
                            }
                            break;

                        default:
                            throw SharedUtils.CreateSafeWin32Exception(error);
                    }
                }
                catch (InvalidCastException e) {
                    throw new InvalidOperationException(SR.Format(SR.CounterDataCorrupt, perfDataKey.ToString()), e);
                }
            }

            throw SharedUtils.CreateSafeWin32Exception(error);
        }

    }

    internal class CategoryEntry {
        internal int NameIndex;
        internal int HelpIndex;
        internal int[] CounterIndexes;
        internal int[] HelpIndexes;

        internal CategoryEntry(Interop.Advapi32.PERF_OBJECT_TYPE perfObject) {
            this.NameIndex = perfObject.ObjectNameTitleIndex;
            this.HelpIndex = perfObject.ObjectHelpTitleIndex;
            this.CounterIndexes = new int[perfObject.NumCounters];
            this.HelpIndexes = new int[perfObject.NumCounters];
        }
    }

    internal class CategorySample {
        internal readonly long SystemFrequency;
        internal readonly long TimeStamp;
        internal readonly long TimeStamp100nSec;
        internal readonly long CounterFrequency;
        internal readonly long CounterTimeStamp;
        internal Hashtable CounterTable;
        internal Hashtable InstanceNameTable;
        internal bool IsMultiInstance;
        private CategoryEntry entry;
        private PerformanceCounterLib library;

        internal unsafe CategorySample(byte[] data, CategoryEntry entry, PerformanceCounterLib library) {
            this.entry = entry;
            this.library = library;
            int categoryIndex = entry.NameIndex;
            Interop.Advapi32.PERF_DATA_BLOCK dataBlock = new Interop.Advapi32.PERF_DATA_BLOCK();
            fixed (byte* dataPtr = data) {
                IntPtr dataRef = new IntPtr((void*) dataPtr);

                Marshal.PtrToStructure(dataRef, dataBlock);
                this.SystemFrequency = dataBlock.PerfFreq;
                this.TimeStamp = dataBlock.PerfTime;
                this.TimeStamp100nSec = dataBlock.PerfTime100nSec;
                dataRef = (IntPtr)((long)dataRef + dataBlock.HeaderLength);
                int numPerfObjects = dataBlock.NumObjectTypes;
                if (numPerfObjects == 0) {
                    this.CounterTable = new Hashtable();
                    this.InstanceNameTable = new Hashtable(StringComparer.OrdinalIgnoreCase);
                    return;
                }

                //Need to find the right category, GetPerformanceData might return
                //several of them.
                Interop.Advapi32.PERF_OBJECT_TYPE perfObject = null;
                bool foundCategory = false;
                for (int index = 0; index < numPerfObjects; index++) {
                    perfObject = new Interop.Advapi32.PERF_OBJECT_TYPE();
                    Marshal.PtrToStructure(dataRef, perfObject);

                   if (perfObject.ObjectNameTitleIndex == categoryIndex) {
                        foundCategory = true;
                        break;
                    }

                    dataRef = (IntPtr)((long)dataRef + perfObject.TotalByteLength);
                }

                if (!foundCategory)
                    throw new InvalidOperationException(SR.Format(SR.CantReadCategoryIndex, categoryIndex.ToString(CultureInfo.CurrentCulture)));

                this.CounterFrequency = perfObject.PerfFreq;
                this.CounterTimeStamp = perfObject.PerfTime;
                int counterNumber = perfObject.NumCounters;
                int instanceNumber = perfObject.NumInstances;

                if (instanceNumber == -1)
                    IsMultiInstance = false;
                else
                    IsMultiInstance = true;
                
                // Move pointer forward to end of PERF_OBJECT_TYPE
                dataRef = (IntPtr)((long)dataRef + perfObject.HeaderLength);

                CounterDefinitionSample[] samples = new CounterDefinitionSample[counterNumber];
                this.CounterTable = new Hashtable(counterNumber);
                for (int index = 0; index < samples.Length; ++ index) {
                    Interop.Advapi32.PERF_COUNTER_DEFINITION perfCounter = new Interop.Advapi32.PERF_COUNTER_DEFINITION();
                    Marshal.PtrToStructure(dataRef, perfCounter);
                    samples[index] = new CounterDefinitionSample(perfCounter, this, instanceNumber);
                    dataRef = (IntPtr)((long)dataRef + perfCounter.ByteLength);

                    int currentSampleType = samples[index].CounterType;
                    if (!PerformanceCounterLib.IsBaseCounter(currentSampleType)) {
                        // We'll put only non-base counters in the table. 
                        if (currentSampleType != Interop.Kernel32.PerformanceCounterOptions.PERF_COUNTER_NODATA)
                            this.CounterTable[samples[index].NameIndex] = samples[index];
                    }
                    else {
                        // it's a base counter, try to hook it up to the main counter. 
                        Debug.Assert(index > 0, "Index > 0 because base counters should never be at index 0");
                        if (index > 0)
                            samples[index-1].BaseCounterDefinitionSample = samples[index];
                    }
                }

                // now set up the InstanceNameTable.  
                if (!IsMultiInstance) {
                    this.InstanceNameTable = new Hashtable(1, StringComparer.OrdinalIgnoreCase);
                    this.InstanceNameTable[PerformanceCounterLib.SingleInstanceName] = 0;

                    for (int index = 0; index < samples.Length; ++ index)  {
                        samples[index].SetInstanceValue(0, dataRef);
                    }
                }
                else {
                    string[] parentInstanceNames = null;
                    this.InstanceNameTable = new Hashtable(instanceNumber, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < instanceNumber; i++) {
                        Interop.Advapi32.PERF_INSTANCE_DEFINITION perfInstance = new Interop.Advapi32.PERF_INSTANCE_DEFINITION();
                        Marshal.PtrToStructure(dataRef, perfInstance);
                        if (perfInstance.ParentObjectTitleIndex > 0 && parentInstanceNames == null)
                            parentInstanceNames = GetInstanceNamesFromIndex(perfInstance.ParentObjectTitleIndex);

                        string instanceName;
                        if (parentInstanceNames != null && perfInstance.ParentObjectInstance >= 0 && perfInstance.ParentObjectInstance < parentInstanceNames.Length - 1)
                            instanceName = parentInstanceNames[perfInstance.ParentObjectInstance] + "/" + Marshal.PtrToStringUni((IntPtr)((long)dataRef + perfInstance.NameOffset));
                        else
                            instanceName = Marshal.PtrToStringUni((IntPtr)((long)dataRef + perfInstance.NameOffset));

                        //In some cases instance names are not unique (Process), same as perfmon
                        //generate a unique name.
                        string newInstanceName = instanceName;
                        int newInstanceNumber = 1;
                        while (true) {
                            if (!this.InstanceNameTable.ContainsKey(newInstanceName)) {
                                this.InstanceNameTable[newInstanceName] = i;
                                break;
                            }
                            else {
                                newInstanceName =  instanceName + "#" + newInstanceNumber.ToString(CultureInfo.InvariantCulture);
                                ++  newInstanceNumber;
                            }
                        }


                        dataRef = (IntPtr)((long)dataRef + perfInstance.ByteLength);
                        for (int index = 0; index < samples.Length; ++ index)
                            samples[index].SetInstanceValue(i, dataRef);

                        dataRef = (IntPtr)((long)dataRef + Marshal.ReadInt32(dataRef));
                    }
                }
            }
        }

        internal unsafe string[] GetInstanceNamesFromIndex(int categoryIndex) {
            byte[] data = library.GetPerformanceData(categoryIndex.ToString(CultureInfo.InvariantCulture));
            fixed (byte* dataPtr = data) {
                IntPtr dataRef = new IntPtr((void*) dataPtr);

                Interop.Advapi32.PERF_DATA_BLOCK dataBlock = new Interop.Advapi32.PERF_DATA_BLOCK();
                Marshal.PtrToStructure(dataRef, dataBlock);
                dataRef = (IntPtr)((long)dataRef + dataBlock.HeaderLength);
                int numPerfObjects = dataBlock.NumObjectTypes;

                Interop.Advapi32.PERF_OBJECT_TYPE perfObject = null;
                bool foundCategory = false;
                for (int index = 0; index < numPerfObjects; index++) {
                    perfObject = new Interop.Advapi32.PERF_OBJECT_TYPE();
                    Marshal.PtrToStructure(dataRef, perfObject);

                    if (perfObject.ObjectNameTitleIndex == categoryIndex) {
                        foundCategory = true;
                        break;
                    }

                    dataRef = (IntPtr)((long)dataRef + perfObject.TotalByteLength);
                }

                if (!foundCategory)
                    return new string[0];

                int counterNumber = perfObject.NumCounters;
                int instanceNumber = perfObject.NumInstances;
                dataRef = (IntPtr)((long)dataRef + perfObject.HeaderLength);

                if (instanceNumber == -1)
                    return new string[0];

                CounterDefinitionSample[] samples = new CounterDefinitionSample[counterNumber];
                for (int index = 0; index < samples.Length; ++ index) {
                    Interop.Advapi32.PERF_COUNTER_DEFINITION perfCounter = new Interop.Advapi32.PERF_COUNTER_DEFINITION();
                    Marshal.PtrToStructure(dataRef, perfCounter);
                    dataRef = (IntPtr)((long)dataRef + perfCounter.ByteLength);
                }

                string[] instanceNames = new string[instanceNumber];
                for (int i = 0; i < instanceNumber; i++) {
                    Interop.Advapi32.PERF_INSTANCE_DEFINITION perfInstance = new Interop.Advapi32.PERF_INSTANCE_DEFINITION();
                    Marshal.PtrToStructure(dataRef, perfInstance);
                    instanceNames[i] =  Marshal.PtrToStringUni((IntPtr)((long)dataRef + perfInstance.NameOffset));
                    dataRef = (IntPtr)((long)dataRef + perfInstance.ByteLength);
                    dataRef = (IntPtr)((long)dataRef + Marshal.ReadInt32(dataRef));
                }

                return instanceNames;
            }
        }

        internal CounterDefinitionSample GetCounterDefinitionSample(string counter) {
            for (int index = 0; index < this.entry.CounterIndexes.Length; ++ index) {
                int counterIndex = entry.CounterIndexes[index];
                string counterName = (string)this.library.NameTable[counterIndex];
                if (counterName != null) {
                    if (String.Compare(counterName, counter, StringComparison.OrdinalIgnoreCase) == 0) {
                        CounterDefinitionSample sample = (CounterDefinitionSample)this.CounterTable[counterIndex];
                        if (sample == null) {
                            //This is a base counter and has not been added to the table
                            foreach (CounterDefinitionSample multiSample in this.CounterTable.Values) {
                                if (multiSample.BaseCounterDefinitionSample != null &&
                                    multiSample.BaseCounterDefinitionSample.NameIndex == counterIndex)
                                    return multiSample.BaseCounterDefinitionSample;
                            }

                            throw new InvalidOperationException(SR.Format(SR.CounterLayout));
                        }
                        return sample;
                    }
                }
            }

            throw new InvalidOperationException(SR.Format(SR.CantReadCounter, counter));
        }

        internal InstanceDataCollectionCollection ReadCategory() {

#pragma warning disable 618
            InstanceDataCollectionCollection data = new InstanceDataCollectionCollection();
#pragma warning restore 618
            for (int index = 0; index < this.entry.CounterIndexes.Length; ++ index) {
                int counterIndex = entry.CounterIndexes[index];

                string name = (string)library.NameTable[counterIndex];
                if (name != null && name != String.Empty) {
                    CounterDefinitionSample sample = (CounterDefinitionSample)this.CounterTable[counterIndex];
                    if (sample != null)
                        //If the current index refers to a counter base,
                        //the sample will be null
                        data.Add(name, sample.ReadInstanceData(name));
                }
            }

            return data;
        }
    }

    internal class CounterDefinitionSample {
        internal readonly int NameIndex;
        internal readonly int CounterType;
        internal CounterDefinitionSample BaseCounterDefinitionSample;

        private readonly int size;
        private readonly int offset;
        private long[] instanceValues;
        private CategorySample categorySample;

        internal CounterDefinitionSample(Interop.Advapi32.PERF_COUNTER_DEFINITION perfCounter, CategorySample categorySample, int instanceNumber) {
            this.NameIndex = perfCounter.CounterNameTitleIndex;
            this.CounterType = perfCounter.CounterType;
            this.offset = perfCounter.CounterOffset;
            this.size = perfCounter.CounterSize;
            if (instanceNumber == -1) {
                this.instanceValues = new long[1];
            }
            else
                this.instanceValues = new long[instanceNumber];

            this.categorySample = categorySample;
        }

        private long ReadValue(IntPtr pointer) {
            if (this.size == 4) {
                return (long)(uint)Marshal.ReadInt32((IntPtr)((long)pointer + this.offset));
            }
            else if (this.size == 8) {
                return (long)Marshal.ReadInt64((IntPtr)((long)pointer + this.offset));
            }

            return -1;
        }

        internal CounterSample GetInstanceValue(string instanceName) {

            if (!categorySample.InstanceNameTable.ContainsKey(instanceName)) { 
                // Our native dll truncates instance names to 128 characters.  If we can't find the instance
                // with the full name, try truncating to 128 characters. 
                if (instanceName.Length > SharedPerformanceCounter.InstanceNameMaxLength)
                    instanceName = instanceName.Substring(0, SharedPerformanceCounter.InstanceNameMaxLength);
                
                if (!categorySample.InstanceNameTable.ContainsKey(instanceName))
                    throw new InvalidOperationException(SR.Format(SR.CantReadInstance, instanceName));
            }

            int index = (int)categorySample.InstanceNameTable[instanceName];
            long rawValue = this.instanceValues[index];
            long baseValue = 0;
            if (this.BaseCounterDefinitionSample != null) {
                CategorySample baseCategorySample = this.BaseCounterDefinitionSample.categorySample;
                int baseIndex = (int)baseCategorySample.InstanceNameTable[instanceName];
                baseValue = this.BaseCounterDefinitionSample.instanceValues[baseIndex];
            }

            return new CounterSample(rawValue,
                                                        baseValue,
                                                        categorySample.CounterFrequency,
                                                        categorySample.SystemFrequency,
                                                        categorySample.TimeStamp,
                                                        categorySample.TimeStamp100nSec,
                                                        (PerformanceCounterType)this.CounterType,
                                                        categorySample.CounterTimeStamp);

        }

        internal InstanceDataCollection ReadInstanceData(string counterName) {
#pragma warning disable 618
            InstanceDataCollection data = new InstanceDataCollection(counterName);
#pragma warning restore 618

            string[] keys = new string[categorySample.InstanceNameTable.Count];
            categorySample.InstanceNameTable.Keys.CopyTo(keys, 0);
            int[] indexes = new int[categorySample.InstanceNameTable.Count];
            categorySample.InstanceNameTable.Values.CopyTo(indexes, 0);
            for (int index = 0; index < keys.Length; ++ index) {
                long baseValue = 0;
                if (this.BaseCounterDefinitionSample != null) {
                    CategorySample baseCategorySample = this.BaseCounterDefinitionSample.categorySample;
                    int baseIndex = (int)baseCategorySample.InstanceNameTable[keys[index]];
                    baseValue = this.BaseCounterDefinitionSample.instanceValues[baseIndex];
                }

                CounterSample sample = new CounterSample(this.instanceValues[indexes[index]],
                                                        baseValue,
                                                        categorySample.CounterFrequency,
                                                        categorySample.SystemFrequency,
                                                        categorySample.TimeStamp,
                                                        categorySample.TimeStamp100nSec,
                                                        (PerformanceCounterType)this.CounterType,
                                                        categorySample.CounterTimeStamp);

                data.Add(keys[index], new InstanceData(keys[index], sample));
            }

            return data;
        }

        internal CounterSample GetSingleValue() {
            long rawValue = this.instanceValues[0];
            long baseValue = 0;
            if (this.BaseCounterDefinitionSample != null)
                baseValue = this.BaseCounterDefinitionSample.instanceValues[0];

            return new CounterSample(rawValue,
                                                        baseValue,
                                                        categorySample.CounterFrequency,
                                                        categorySample.SystemFrequency,
                                                        categorySample.TimeStamp,
                                                        categorySample.TimeStamp100nSec,
                                                        (PerformanceCounterType)this.CounterType,
                                                        categorySample.CounterTimeStamp);
        }

        internal void SetInstanceValue(int index, IntPtr dataRef) {
            long rawValue = ReadValue(dataRef);
            this.instanceValues[index] = rawValue;
        }
    }
}
