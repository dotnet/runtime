// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace System.Diagnostics
{
    internal static class SharedUtils
    {
        internal static Win32Exception CreateSafeWin32Exception()
        {
            return CreateSafeWin32Exception(0);
        }

        internal static Win32Exception CreateSafeWin32Exception(int error)
        {
            Win32Exception newException = null;
            if (error == 0)
                newException = new Win32Exception();
            else
                newException = new Win32Exception(error);

            return newException;
        }

        internal static void EnterMutex(string name, ref Mutex mutex)
        {
            string mutexName = null;
            mutexName = "Global\\" + name;
            EnterMutexWithoutGlobal(mutexName, ref mutex);
        }

        internal static void EnterMutexWithoutGlobal(string mutexName, ref Mutex mutex)
        {
            bool createdNew;
            MutexSecurity sec = new MutexSecurity();
            SecurityIdentifier everyoneSid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            //sec.AddAccessRule(new MutexAccessRule(everyoneSid, MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow));
            //Mutex tmpMutex = new Mutex(false, mutexName, out createdNew, sec);
            Mutex tmpMutex = new Mutex(false, mutexName, out createdNew);
            SafeWaitForMutex(tmpMutex, ref mutex);
        }

        private static bool SafeWaitForMutex(Mutex mutexIn, ref Mutex mutexOut)
        {
            Debug.Assert(mutexOut == null, "You must pass in a null ref Mutex");
            while (true)
            {
                if (!SafeWaitForMutexOnce(mutexIn, ref mutexOut))
                    return false;
                if (mutexOut != null)
                    return true;
                Thread.Sleep(0);
            }
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static bool SafeWaitForMutexOnce(Mutex mutexIn, ref Mutex mutexOut)
        {
            bool ret;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                Thread.BeginCriticalRegion();
                Thread.BeginThreadAffinity();
                int result = Interop.Kernel32.WaitForSingleObject(mutexIn.SafeWaitHandle, 500);
                switch (result)
                {
                    case NativeMethods.WAIT_OBJECT_0:
                    case NativeMethods.WAIT_ABANDONED:
                        mutexOut = mutexIn;
                        ret = true;
                        break;

                    case NativeMethods.WAIT_TIMEOUT:
                        ret = true;
                        break;

                    default:
                        ret = false;
                        break;
                }
                if (mutexOut == null)
                {
                    Thread.EndThreadAffinity();
                    Thread.EndCriticalRegion();
                }
            }

            return ret;
        }

        internal static string GetLatestBuildDllDirectory(string machineName)
        {
            string dllDir = "";
            RegistryKey baseKey = null;
            RegistryKey complusReg = null;

            try
            {
                if (machineName.Equals("."))
                {
                    return GetLocalBuildDirectory();
                }
                else
                {
                    baseKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machineName);
                }
                if (baseKey == null)
                    throw new InvalidOperationException(SR.Format(SR.RegKeyMissingShort, "HKEY_LOCAL_MACHINE", machineName));

                complusReg = baseKey.OpenSubKey("SOFTWARE\\Microsoft\\.NETFramework");
                if (complusReg != null)
                {
                    string installRoot = (string)complusReg.GetValue("InstallRoot");
                    if (installRoot != null && installRoot != String.Empty)
                    {
                        string versionPrefix = "v" + Environment.Version.Major + "." + Environment.Version.Minor;
                        RegistryKey policyKey = complusReg.OpenSubKey("policy");

                        string version = null;

                        if (policyKey != null)
                        {
                            try
                            {
                                RegistryKey bestKey = policyKey.OpenSubKey(versionPrefix);

                                if (bestKey != null)
                                {
                                    try
                                    {
                                        version = versionPrefix + "." + GetLargestBuildNumberFromKey(bestKey);
                                    }
                                    finally
                                    {
                                        bestKey.Close();
                                    }
                                }
                                else
                                {
                                    string[] majorVersions = policyKey.GetSubKeyNames();
                                    int[] largestVersion = new int[] { -1, -1, -1 };
                                    for (int i = 0; i < majorVersions.Length; i++)
                                    {

                                        string majorVersion = majorVersions[i];
                                        if (majorVersion.Length > 1 && majorVersion[0] == 'v' && majorVersion.Contains("."))
                                        {
                                            int[] currentVersion = new int[] { -1, -1, -1 };

                                            string[] splitVersion = majorVersion.Substring(1).Split('.');

                                            if (splitVersion.Length != 2)
                                            {
                                                continue;
                                            }

                                            if (!Int32.TryParse(splitVersion[0], out currentVersion[0]) || !Int32.TryParse(splitVersion[1], out currentVersion[1]))
                                            {
                                                continue;
                                            }

                                            RegistryKey k = policyKey.OpenSubKey(majorVersion);
                                            if (k == null)
                                            {
                                                continue;
                                            }
                                            try
                                            {
                                                currentVersion[2] = GetLargestBuildNumberFromKey(k);

                                                if (currentVersion[0] > largestVersion[0]
                                                    || ((currentVersion[0] == largestVersion[0]) && (currentVersion[1] > largestVersion[1])))
                                                {
                                                    largestVersion = currentVersion;
                                                }
                                            }
                                            finally
                                            {
                                                k.Close();
                                            }
                                        }
                                    }

                                    version = "v" + largestVersion[0] + "." + largestVersion[1] + "." + largestVersion[2];
                                }
                            }
                            finally
                            {
                                policyKey.Close();
                            }

                            if (version != null && version != String.Empty)
                            {
                                StringBuilder installBuilder = new StringBuilder();
                                installBuilder.Append(installRoot);
                                if (!installRoot.EndsWith("\\", StringComparison.Ordinal))
                                    installBuilder.Append("\\");
                                installBuilder.Append(version);
                                dllDir = installBuilder.ToString();
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                complusReg?.Close();
                baseKey?.Close();
            }

            return dllDir;
        }

        private static int GetLargestBuildNumberFromKey(RegistryKey rootKey)
        {
            int largestBuild = -1;

            string[] minorVersions = rootKey.GetValueNames();
            for (int i = 0; i < minorVersions.Length; i++)
            {
                int o;
                if (Int32.TryParse(minorVersions[i], out o))
                {
                    largestBuild = (largestBuild > o) ? largestBuild : o;
                }
            }

            return largestBuild;
        }

        private static string GetLocalBuildDirectory()
        {
            return RuntimeEnvironment.GetRuntimeDirectory();
        }
    }
}
