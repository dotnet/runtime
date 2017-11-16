// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Test.Data.SqlClient
{
    /// <summary>
    /// allows user to manipulate %windir%\system32\drivers\etc\hosts
    /// the hosts file must be reverted if changed even if test application crashes, thus inherit from CriticalFinalizerObject. Make sure the instance is disposed after its use.
    /// The last dispose call on the active instance reverts the hosts file.
    /// 
    /// Usage:
    /// using (var hostsFile = new HostsFileManager())
    /// {
    ///     // use the hostsFile methods to add/remove entries
    ///     // simultaneous usage of HostsFileManager in two app domains or processes on the same machine is not allowed
    /// }
    /// </summary>
    public sealed class HostsFileManager : IDisposable
    {
        // define global (machine-wide) lock instance
        private static EventWaitHandle s_globalLock = new EventWaitHandle(true /* create as signalled */, EventResetMode.AutoReset, @"Global\HostsFileManagerLock");
        private static bool s_globalLockTaken; // set when global (machine-wide) lock is in use

        private static int s_localUsageRefCount;
        private static object s_localLock = new object();

        private static string s_hostsFilePath;
        private static string s_backupPath;
        private static bool s_hasBackup;
        private static TextReader s_activeReader;
        private static TextWriter s_activeWriter;
        private static List<HostEntry> s_entriesCache;

        private const string HostsFilePathUnderSystem32 = @"C:\Windows\System32\drivers\etc\hosts";
        private const string HostsFilePathUnderLinux = "/etc/hosts";
        private const string HostsFilePathUnderMacOS = "/private/etc/hosts";


        private static void InitializeGlobal(ref bool mustRelease)
        {
            if (mustRelease)
            {
                // already initialized
                return;
            }

            lock (s_localLock)
            {
                if (mustRelease)
                {
                    // check again under lock
                    return;
                }

                if (s_localUsageRefCount > 0)
                {
                    // initialized by another thread
                    ++s_localUsageRefCount;
                    return;
                }

                // first call to initialize in this app domain
                // note: simultanious use of HostsFileManager is currently supported only within single AppDomain scope

                // non-critical initialization goes first
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    s_hostsFilePath = HostsFilePathUnderSystem32;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    s_hostsFilePath = HostsFilePathUnderLinux;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    s_hostsFilePath = HostsFilePathUnderMacOS;
                }

                s_backupPath = Path.Combine(Path.GetTempPath(), string.Format("Hosts_{0}.bak", Guid.NewGuid().ToString("N")));

                // try to get global lock
                // note that once global lock is aquired, it must be released
                try { }
                finally
                {
                    if (s_globalLock.WaitOne(0))
                    {
                        s_globalLockTaken = true;
                        mustRelease = true;
                        ++s_localUsageRefCount; // increment ref count for the first thread using the manager
                    }
                }

                if (!s_globalLockTaken)
                {
                    throw new InvalidOperationException("HostsFileManager cannot initialize because hosts file is in use by another instance of the manager in the same or a different process (concurrent access is not allowed)");
                }

                // locked now, take snapshot of hosts file and save it as a backup
                File.Copy(s_hostsFilePath, s_backupPath);
                s_hasBackup = true;

                // load the current entries
                InternalRefresh();
            }
        }

        private static void TerminateGlobal(ref bool originalMustRelease)
        {
            if (!originalMustRelease)
            {
                // already disposed
                return;
            }

            lock (s_localLock)
            {
                if (!originalMustRelease)
                {
                    // check again under lock
                    return;
                }

                // not yet disposed, do it now
                if (s_localUsageRefCount > 1)
                {
                    // still in use by another thread(s)
                    --s_localUsageRefCount;
                    return;
                }

                if (s_activeReader != null)
                {
                    s_activeReader.Dispose();
                    s_activeReader = null;
                }
                if (s_activeWriter != null)
                {
                    s_activeWriter.Dispose();
                    s_activeWriter = null;
                }
                bool deleteBackup = false;
                if (s_hasBackup)
                {
                    // revert the hosts file
                    File.Copy(s_backupPath, s_hostsFilePath, overwrite: true);
                    s_hasBackup = false;
                    deleteBackup = true;
                }

                // Note: if critical finalizer fails to revert the hosts file, the global lock might remain reset until the machine is rebooted.
                // if this happens, Hosts file in unpredictable state so there is no point in running tests anyway
                if (s_globalLockTaken)
                {
                    try { }
                    finally
                    {
                        s_globalLock.Set();
                        s_globalLockTaken = false;
                        --s_localUsageRefCount; // decrement local ref count
                        originalMustRelease = false;
                    }
                }

                // now we can destroy the backup
                if (deleteBackup)
                {
                    File.Delete(s_backupPath);
                }
            }
        }

        private bool _mustRelease;
        private bool _disposed;

        public HostsFileManager()
        {
            // lazy initialization
            _mustRelease = false;
            _disposed = false;
        }

        ~HostsFileManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                TerminateGlobal(ref _mustRelease);
            }
        }

        public class HostEntry
        {
            public HostEntry(string name, IPAddress address)
            {
                ValidateName(name);
                ValidateAddress(address);

                this.Name = name;
                this.Address = address;
            }

            public readonly string Name;
            public readonly IPAddress Address;
        }

        // helper methods

        // must be called under lock(_localLock) from each public API that uses static fields
        private void InitializeLocal()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            InitializeGlobal(ref _mustRelease);
        }

        private static readonly char[] s_whiteSpaceChars = new char[] { ' ', '\t' };

        private static void ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.IndexOfAny(s_whiteSpaceChars) >= 0)
            {
                throw new ArgumentException("name cannot be null or empty or have whitespace characters in it");
            }
        }

        private static void ValidateAddress(IPAddress address)
        {
            ValidateNonNull(address, "address");

            if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork &&
                address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException("only IPv4 or IPv6 addresses are allowed");
            }
        }

        private static void ValidateNonNull<T>(T value, string argName) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(argName);
            }
        }

        private static HostEntry TryParseLine(string line)
        {
            line = line.Trim();
            if (line.StartsWith("#"))
            {
                // comment, ignore
                return null;
            }

            string[] items = line.Split(s_whiteSpaceChars, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length == 0)
            {
                // empty or white-space only line - ignore
                return null;
            }

            if (items.Length != 2)
            {
                Trace.WriteLine("Wrong entry in the hosts file (exactly two columns expected): \"" + line + "\"");
                return null;
            }

            string name = items[1];
            IPAddress address;
            if (!IPAddress.TryParse(items[0], out address))
            {
                Trace.WriteLine("Wrong entry in the hosts file (cannot parse the IP address): \"" + line + "\"");
                return null;
            }

            try
            {
                return new HostEntry(name, address);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Wrong entry in the hosts file, cannot create host entry: " + e.Message);
                return null;
            }
        }

        private bool NameMatch(HostEntry entry, string name)
        {
            ValidateNonNull(entry, "entry");
            ValidateName(name);

            return string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase);
        }

        // hosts file manipulation methods

        // reloads the hosts file, must be called under lock(_localLock)
        private static void InternalRefresh()
        {
            List<HostEntry> entries = new List<HostEntry>();

            try
            {
                s_activeReader = new StreamReader(new FileStream(s_hostsFilePath, FileMode.Open));

                string line;
                while ((line = s_activeReader.ReadLine()) != null)
                {
                    HostEntry nextEntry = TryParseLine(line);
                    if (nextEntry != null)
                    {
                        entries.Add(nextEntry);
                    }
                }
            }
            finally
            {
                if (s_activeReader != null)
                {
                    s_activeReader.Dispose();
                    s_activeReader = null;
                }
            }

            s_entriesCache = entries;
        }

        // reloads the hosts file, must be called while still under lock(_localLock)
        private void InternalSave()
        {
            try
            {
                s_activeWriter = new StreamWriter(new FileStream(s_hostsFilePath, FileMode.Create));

                foreach (HostEntry entry in s_entriesCache)
                {
                    s_activeWriter.WriteLine("  {0}  {1}", entry.Address, entry.Name);
                }

                s_activeWriter.Flush();
            }
            finally
            {
                if (s_activeWriter != null)
                {
                    s_activeWriter.Dispose();
                    s_activeWriter = null;
                }
            }
        }

        public int RemoveAll(string name)
        {
            lock (s_localLock)
            {
                InitializeLocal();
                ValidateName(name);

                int removed = s_entriesCache.RemoveAll(entry => NameMatch(entry, name));

                if (removed > 0)
                {
                    InternalSave();
                }

                return removed;
            }
        }

        public IEnumerable<IPAddress> EnumerateAddresses(string name)
        {
            lock (s_localLock)
            {
                InitializeLocal();
                ValidateName(name);

                return from entry in s_entriesCache where NameMatch(entry, name) select entry.Address;
            }
        }

        public void Add(string name, IPAddress address)
        {
            lock (s_localLock)
            {
                InitializeLocal();

                HostEntry entry = new HostEntry(name, address); // c-tor validates the arguments
                s_entriesCache.Add(entry);

                InternalSave();
            }
        }

        public void Add(HostEntry entry)
        {
            lock (s_localLock)
            {
                InitializeLocal();
                ValidateNonNull(entry, "entry");

                s_entriesCache.Add(entry);

                InternalSave();
            }
        }

        public void AddRange(string name, IEnumerable<IPAddress> addresses)
        {
            lock (s_localLock)
            {
                InitializeLocal();
                ValidateName(name);
                ValidateNonNull(addresses, "addresses");

                foreach (IPAddress address in addresses)
                {
                    HostEntry entry = new HostEntry(name, address);

                    s_entriesCache.Add(entry);
                }

                InternalSave();
            }
        }

        public void AddRange(IEnumerable<HostEntry> entries)
        {
            lock (s_localLock)
            {
                InitializeLocal();
                ValidateNonNull(entries, "entries");

                foreach (HostEntry entry in entries)
                {
                    ValidateNonNull(entry, "entries element");

                    s_entriesCache.Add(entry);
                }

                InternalSave();
            }
        }

        public void Clear()
        {
            lock (s_localLock)
            {
                InitializeLocal();

                s_entriesCache.Clear();

                InternalSave();
            }
        }
    }
}
