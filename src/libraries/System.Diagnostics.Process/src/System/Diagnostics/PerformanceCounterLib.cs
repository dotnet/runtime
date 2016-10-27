// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics
{
    internal sealed class PerformanceCounterLib
    {
        private static volatile string s_computerName;

        private PerformanceMonitor _performanceMonitor;
        private string _machineName;
        private string _perfLcid;

        private static volatile Dictionary<String, PerformanceCounterLib> s_libraryTable;
        private Dictionary<int, string> _nameTable;
        private readonly object _nameTableLock = new Object();

        private static Object s_internalSyncObject;
        private static Object InternalSyncObject
        {
            get
            {
                if (s_internalSyncObject == null)
                {
                    Object o = new Object();
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
                            s_computerName = Interop.mincore.GetComputerName();
                        }
                    }
                }

                return s_computerName;
            }
        }

        internal Dictionary<int, string> NameTable
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

        internal string GetCounterName(int index)
        {
            string result;
            return NameTable.TryGetValue(index, out result) ? result : "";
        }

        internal static PerformanceCounterLib GetPerformanceCounterLib(string machineName, CultureInfo culture)
        {
            string lcidString = culture.Name.ToLowerInvariant();
            if (machineName.CompareTo(".") == 0)
                machineName = ComputerName.ToLowerInvariant();
            else
                machineName = machineName.ToLowerInvariant();

            if (PerformanceCounterLib.s_libraryTable == null)
            {
                lock (InternalSyncObject)
                {
                    if (PerformanceCounterLib.s_libraryTable == null)
                        PerformanceCounterLib.s_libraryTable = new Dictionary<string, PerformanceCounterLib>();
                }
            }

            string libraryKey = machineName + ":" + lcidString;
            PerformanceCounterLib library;
            if (!PerformanceCounterLib.s_libraryTable.TryGetValue(libraryKey, out library))
            {
                library = new PerformanceCounterLib(machineName, lcidString);
                PerformanceCounterLib.s_libraryTable[libraryKey] = library;
            }
            return library;
        }

        internal byte[] GetPerformanceData(string item)
        {
            if (_performanceMonitor == null)
            {
                lock (InternalSyncObject)
                {
                    if (_performanceMonitor == null)
                        _performanceMonitor = new PerformanceMonitor(_machineName);
                }
            }

            return _performanceMonitor.GetData(item);
        }

        private Dictionary<int, string> GetStringTable(bool isHelp)
        {
            Dictionary<int, string> stringTable;
            RegistryKey libraryKey;

            libraryKey = Registry.PerformanceData;

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
                    stringTable = new Dictionary<int, string>();
                else
                {
                    stringTable = new Dictionary<int, string>(names.Length / 2);

                    for (int index = 0; index < (names.Length / 2); ++index)
                    {
                        string nameString = names[(index * 2) + 1];
                        if (nameString == null)
                            nameString = String.Empty;

                        int key;
                        if (!Int32.TryParse(names[index * 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out key))
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
                libraryKey.Dispose();
            }

            return stringTable;
        }

        internal class PerformanceMonitor
        {
            private RegistryKey _perfDataKey = null;
            private string _machineName;

            internal PerformanceMonitor(string machineName)
            {
                _machineName = machineName;
                Init();
            }

            private void Init()
            {
                _perfDataKey = Registry.PerformanceData;
            }

            // Win32 RegQueryValueEx for perf data could deadlock (for a Mutex) up to 2mins in some 
            // scenarios before they detect it and exit gracefully. In the mean time, ERROR_BUSY, 
            // ERROR_NOT_READY etc can be seen by other concurrent calls (which is the reason for the 
            // wait loop and switch case below). We want to wait most certainly more than a 2min window. 
            // The current wait time of up to 10mins takes care of the known stress deadlock issues. In most 
            // cases we wouldn't wait for more than 2mins anyways but in worst cases how much ever time 
            // we wait may not be sufficient if the Win32 code keeps running into this deadlock again 
            // and again. A condition very rare but possible in theory. We would get back to the user 
            // in this case with InvalidOperationException after the wait time expires.
            internal byte[] GetData(string item)
            {
                int waitRetries = 17;   //2^16*10ms == approximately 10mins
                int waitSleep = 0;
                byte[] data = null;
                int error = 0;

                while (waitRetries > 0)
                {
                    try
                    {
                        data = (byte[])_perfDataKey.GetValue(item);
                        return data;
                    }
                    catch (IOException e)
                    {
                        error = e.HResult;
                        switch (error)
                        {
                            case Interop.mincore.RPCStatus.RPC_S_CALL_FAILED:
                            case Interop.mincore.Errors.ERROR_INVALID_HANDLE:
                            case Interop.mincore.RPCStatus.RPC_S_SERVER_UNAVAILABLE:
                                Init();
                                goto case Interop.mincore.WaitOptions.WAIT_TIMEOUT;

                            case Interop.mincore.WaitOptions.WAIT_TIMEOUT:
                            case Interop.mincore.Errors.ERROR_NOT_READY:
                            case Interop.mincore.Errors.ERROR_LOCK_FAILED:
                            case Interop.mincore.Errors.ERROR_BUSY:
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
                        throw new InvalidOperationException(SR.Format(SR.CounterDataCorrupt, _perfDataKey.ToString()), e);
                    }
                }

                throw new Win32Exception(error);
            }
        }
    }
}
