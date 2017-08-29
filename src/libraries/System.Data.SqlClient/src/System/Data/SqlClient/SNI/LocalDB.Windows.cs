// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Data.SqlClient.SNI
{
    internal sealed class LocalDB
    {
        private static readonly LocalDB Instance = new LocalDB();

        //HKEY_LOCAL_MACHINE
        private const string LocalDBInstalledVersionRegistryKey = "SOFTWARE\\Microsoft\\Microsoft SQL Server Local DB\\Installed Versions\\";

        private const string InstanceAPIPathValueName = "InstanceAPIPath";

        private const string ProcLocalDBStartInstance = "LocalDBStartInstance";

        private const int MAX_LOCAL_DB_CONNECTION_STRING_SIZE = 260;

        private IntPtr _startInstanceHandle = IntPtr.Zero;

        // Local Db api doc https://msdn.microsoft.com/en-us/library/hh217143.aspx
        // HRESULT LocalDBStartInstance( [Input ] PCWSTR pInstanceName, [Input ] DWORD dwFlags,[Output] LPWSTR wszSqlConnection,[Input/Output] LPDWORD lpcchSqlConnection);  
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int LocalDBStartInstance(
                [In] [MarshalAs(UnmanagedType.LPWStr)] string localDBInstanceName,
                [In]  int flags,
                [Out] [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sqlConnectionDataSource,
                [In, Out]ref int bufferLength);

        private LocalDBStartInstance localDBStartInstanceFunc = null;

        private volatile SafeLibraryHandle _sqlUserInstanceLibraryHandle;

        private LocalDB() { }

        internal static string GetLocalDBConnectionString(string localDbInstance) =>
            Instance.LoadUserInstanceDll() ? Instance.GetConnectionString(localDbInstance) : null;

        internal static IntPtr GetProcAddress(string functionName) =>
            Instance.LoadUserInstanceDll() ? Interop.Kernel32.GetProcAddress(LocalDB.Instance._sqlUserInstanceLibraryHandle, functionName) : IntPtr.Zero;

        private string GetConnectionString(string localDbInstance)
        {
            StringBuilder localDBConnectionString = new StringBuilder(MAX_LOCAL_DB_CONNECTION_STRING_SIZE + 1);
            int sizeOfbuffer = localDBConnectionString.Capacity;
            localDBStartInstanceFunc(localDbInstance, 0, localDBConnectionString, ref sizeOfbuffer);
            return localDBConnectionString.ToString();
        }

        internal enum LocalDBErrorState
        {
            NO_INSTALLATION, INVALID_CONFIG, NO_SQLUSERINSTANCEDLL_PATH, INVALID_SQLUSERINSTANCEDLL_PATH, NONE
        }

        internal static uint MapLocalDBErrorStateToCode(LocalDBErrorState errorState)
        {
            switch (errorState)
            {
                case LocalDBErrorState.NO_INSTALLATION:
                    return SNICommon.LocalDBNoInstallation;
                case LocalDBErrorState.INVALID_CONFIG:
                    return SNICommon.LocalDBInvalidConfig;
                case LocalDBErrorState.NO_SQLUSERINSTANCEDLL_PATH:
                    return SNICommon.LocalDBNoSqlUserInstanceDllPath;
                case LocalDBErrorState.INVALID_SQLUSERINSTANCEDLL_PATH:
                    return SNICommon.LocalDBInvalidSqlUserInstanceDllPath;
                case LocalDBErrorState.NONE:
                    return 0;
                default:
                    return SNICommon.LocalDBInvalidConfig;
            }
        }

        /// <summary>
        /// Loads the User Instance dll.
        /// </summary>
        private bool LoadUserInstanceDll()
        {
            // Check in a non thread-safe way if the handle is already set for performance.
            if (_sqlUserInstanceLibraryHandle != null)
            {
                return true;
            }

            lock (this)
            {
                if(_sqlUserInstanceLibraryHandle !=null)
                {
                    return true;
                }
                //Get UserInstance Dll path
                LocalDBErrorState registryQueryErrorState;

                // Get the LocalDB instance dll path from the registry
                string dllPath = GetUserInstanceDllPath(out registryQueryErrorState);

                // If there was no DLL path found, then there is an error.
                if (dllPath == null)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.INVALID_PROV, 0, MapLocalDBErrorStateToCode(registryQueryErrorState), string.Empty);
                    return false;
                }

                // In case the registry had an empty path for dll
                if (string.IsNullOrWhiteSpace(dllPath))
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.INVALID_PROV, 0, SNICommon.LocalDBInvalidSqlUserInstanceDllPath, string.Empty);
                    return false;
                }

                // Load the dll
                SafeLibraryHandle libraryHandle = Interop.Kernel32.LoadLibraryExW(dllPath.Trim(), IntPtr.Zero, 0);

                if (libraryHandle.IsInvalid)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.INVALID_PROV, 0, SNICommon.LocalDBFailedToLoadDll, string.Empty);
                    libraryHandle.Dispose();
                    return false;
                }

                // Load the procs from the DLLs
                _startInstanceHandle = Interop.Kernel32.GetProcAddress(libraryHandle, ProcLocalDBStartInstance);

                if (_startInstanceHandle == IntPtr.Zero)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.INVALID_PROV, 0, SNICommon.LocalDBBadRuntime, string.Empty);
                    libraryHandle.Dispose();
                    return false;
                }

                // Set the delegate the invoke.
                localDBStartInstanceFunc = (LocalDBStartInstance)Marshal.GetDelegateForFunctionPointer(_startInstanceHandle, typeof(LocalDBStartInstance));
                
                if (localDBStartInstanceFunc == null)
                {
                    SNILoadHandle.SingletonInstance.LastError = new SNIError(SNIProviders.INVALID_PROV, 0, SNICommon.LocalDBBadRuntime, string.Empty);
                    libraryHandle.Dispose();
                    _startInstanceHandle = IntPtr.Zero;
                    return false;
                }

                _sqlUserInstanceLibraryHandle = libraryHandle;

                return true;
            }
        }

        /// <summary>
        /// Retrieves the part of the sqlUserInstance.dll from the registry
        /// </summary>
        /// <param name="errorState">In case the dll path is not found, the error is set here.</param>
        /// <returns></returns>
        private string GetUserInstanceDllPath(out LocalDBErrorState errorState)
        {
            string dllPath = null;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(LocalDBInstalledVersionRegistryKey))
            {
                if (key == null)
                {
                    errorState = LocalDBErrorState.NO_INSTALLATION;
                    return null;
                }

                Version zeroVersion = new Version();

                Version latestVersion = zeroVersion;

                foreach (string subKey in key.GetSubKeyNames())
                {
                    Version currentKeyVersion;

                    if (!Version.TryParse(subKey, out currentKeyVersion))
                    {
                        errorState = LocalDBErrorState.INVALID_CONFIG;
                        return null;
                    }

                    if (latestVersion.CompareTo(currentKeyVersion) < 0)
                    {
                        latestVersion = currentKeyVersion;
                    }
                }

                // If no valid versions are found, then error out
                if (latestVersion.Equals(zeroVersion))
                {
                    errorState = LocalDBErrorState.INVALID_CONFIG;
                    return null;
                }

                // Use the latest version to get the DLL path
                using (RegistryKey latestVersionKey = key.OpenSubKey(latestVersion.ToString()))
                {

                    object instanceAPIPathRegistryObject = latestVersionKey.GetValue(InstanceAPIPathValueName);

                    if (instanceAPIPathRegistryObject == null)
                    {
                        errorState = LocalDBErrorState.NO_SQLUSERINSTANCEDLL_PATH;
                        return null;
                    }

                    RegistryValueKind valueKind = latestVersionKey.GetValueKind(InstanceAPIPathValueName);

                    if (valueKind != RegistryValueKind.String)
                    {
                        errorState = LocalDBErrorState.INVALID_SQLUSERINSTANCEDLL_PATH;
                        return null;
                    }
                    
                    dllPath = (string)instanceAPIPathRegistryObject;
                    
                    errorState = LocalDBErrorState.NONE;
                    return dllPath;
                }
            }
        }
    }
}
