// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Text;
using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    internal static partial class NetFrameworkUtils
    {
        internal static void EnterMutex(string name, ref Mutex mutex)
        {
            string mutexName = "Global\\" + name;
            EnterMutexWithoutGlobal(mutexName, ref mutex);
        }

        internal static void EnterMutexWithoutGlobal(string mutexName, ref Mutex mutex)
        {
            bool createdNew;

            Mutex tmpMutex = new Mutex(false, mutexName, out createdNew);

            SafeWaitForMutex(tmpMutex, ref mutex);
        }

        // We need to atomically attempt to acquire the mutex and record whether we took it (because we require thread affinity
        // while the mutex is held and the two states must be kept in lock step). We can get atomicity with a CER, but we don't want
        // to hold a CER over a call to WaitOne (this could cause deadlocks). The final solution is to provide a new API out of
        // mscorlib that performs the wait and lets us know if it succeeded. But at this late stage we don't want to expose a new
        // API out of mscorlib, so we'll build our own solution.
        // We'll P/Invoke out to the WaitForSingleObject inside a CER, but use a timeout to ensure we can't block a thread abort for
        // an unlimited time (we do this in an infinite loop so the semantic of acquiring the mutex is unchanged, the timeout is
        // just to allow us to poll for abort). A limitation of CERs in Whidbey (and part of the problem that put us in this
        // position in the first place) is that a CER root in a method will cause the entire method to delay thread aborts. So we
        // need to carefully partition the real CER part of out logic in a sub-method (and ensure the jit doesn't inline on us).
        private static bool SafeWaitForMutex(Mutex mutexIn, ref Mutex mutexOut)
        {
            Debug.Assert(mutexOut == null, "You must pass in a null ref Mutex");

            // Wait as long as necessary for the mutex.
            while (true)
            {
                // Attempt to acquire the mutex but timeout quickly if we can't.
                if (!SafeWaitForMutexOnce(mutexIn, ref mutexOut))
                    return false;
                if (mutexOut != null)
                    return true;

                // We come out here to the outer method every so often so we're not in a CER and a thread abort can interrupt us.
                // But the abort logic itself is poll based (in the this case) so we really need to check for a pending abort
                // explicitly else the two timing windows will virtually never line up and we'll still end up stalling the abort
                // attempt. Thread.Sleep checks for pending abort for us.
                Thread.Sleep(0);
            }
        }

        // The portion of SafeWaitForMutex that runs under a CER and thus must not block for a arbitrary period of time.
        // This method must not be inlined (to stop the CER accidently spilling into the calling method).
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private static bool SafeWaitForMutexOnce(Mutex mutexIn, ref Mutex mutexOut)
        {
            bool ret;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                // Wait for the mutex for half a second (long enough to gain the mutex in most scenarios and short enough to avoid
                // impacting a thread abort for too long).
                // Holding a mutex requires us to keep thread affinity and announce ourselves as a critical region.
                Thread.BeginCriticalRegion();
                Thread.BeginThreadAffinity();
                int result = Interop.Kernel32.WaitForSingleObject(mutexIn.SafeWaitHandle, 500);
                switch (result)
                {
                    case Interop.Kernel32.WAIT_OBJECT_0:
                    case Interop.Kernel32.WAIT_ABANDONED:
                        // Mutex was obtained, atomically record that fact.
                        mutexOut = mutexIn;
                        ret = true;
                        break;

                    case Interop.Kernel32.WAIT_TIMEOUT:
                        // Couldn't get mutex yet, simply return and we'll try again later.
                        ret = true;
                        break;

                    default:
                        // Some sort of failure return immediately all the way to the caller of SafeWaitForMutex.
                        ret = false;
                        break;
                }

                // If we're not leaving with the Mutex we don't require thread affinity and we're not a critical region any more.
                if (mutexOut == null)
                {
                    Thread.EndThreadAffinity();
                    Thread.EndCriticalRegion();
                }
            }

            return ret;
        }

        // What if an app is locked back?  Why would we use this?
        internal static string GetLatestBuildDllDirectory(string machineName)
        {
            string dllDir = "";
            RegistryKey baseKey = null;
            RegistryKey complusReg = null;

            try
            {
                if (machineName == ".")
                    baseKey = Registry.LocalMachine;
                else
                    baseKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, machineName);

                if (baseKey == null)
                    throw new InvalidOperationException(SR.Format(SR.RegKeyMissingShort, "HKEY_LOCAL_MACHINE", machineName));

                complusReg = baseKey.OpenSubKey("SOFTWARE\\Microsoft\\.NETFramework");
                if (complusReg != null)
                {
                    string installRoot = (string)complusReg.GetValue("InstallRoot");
                    if (installRoot != null && installRoot != string.Empty)
                    {
                        // the "policy" subkey contains a v{major}.{minor} subkey for each version installed.  There are also
                        // some extra subkeys like "standards" and "upgrades" we want to ignore.

                        // first we figure out what version we are...
                        string versionPrefix = "v" + Environment.Version.Major + "." + Environment.Version.Minor;
                        RegistryKey policyKey = complusReg.OpenSubKey("policy");

                        // This is the full version string of the install on the remote machine we want to use (for example "v2.0.50727")
                        string version = null;

                        if (policyKey != null)
                        {
                            try
                            {
                                // First check to see if there is a version of the runtime with the same minor and major number:
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
                                    // There isn't an exact match for our version, so we will look for the largest version
                                    // installed.
                                    string[] majorVersions = policyKey.GetSubKeyNames();
                                    int[] largestVersion = new int[] { -1, -1, -1 };
                                    for (int i = 0; i < majorVersions.Length; i++)
                                    {
                                        string majorVersion = majorVersions[i];

                                        // If this looks like a key of the form v{something}.{something}, we should see if it's a usable build.
                                        if (majorVersion.Length > 1 && majorVersion[0] == 'v' && majorVersion.Contains(".")) // string.Contains(char) is .NetCore2.1+ specific
                                        {
                                            int[] currentVersion = new int[] { -1, -1, -1 };

                                            string[] splitVersion = majorVersion.Substring(1).Split('.');

                                            if (splitVersion.Length != 2)
                                            {
                                                continue;
                                            }

                                            if (!int.TryParse(splitVersion[0], out currentVersion[0]) || !int.TryParse(splitVersion[1], out currentVersion[1]))
                                            {
                                                continue;
                                            }

                                            RegistryKey k = policyKey.OpenSubKey(majorVersion);
                                            if (k == null)
                                            {
                                                // We may be able to use another subkey
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

                            if (version != null && version != string.Empty)
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
                // ignore
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
                if (int.TryParse(minorVersions[i], out o))
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
