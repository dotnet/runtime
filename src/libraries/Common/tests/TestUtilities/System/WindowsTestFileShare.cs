// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace System
{
    public sealed partial class WindowsTestFileShare : IDisposable
    {
        private static readonly Lazy<bool> _canShareFiles = new Lazy<bool>(() =>
        {
            if (!PlatformDetection.IsWindows || !PlatformDetection.IsPrivilegedProcess)
            {
                return false;
            }

            try
            {
                // the "Server Service" allows for file sharing. It can be disabled on some machines.
                using (ServiceController sharingService = new ServiceController("Server"))
                {
                    return sharingService.Status == ServiceControllerStatus.Running;
                }
            }
            catch (InvalidOperationException)
            {
                // The service is not installed.
                return false;
            }
        });

        private readonly string _shareName;

        private readonly string _path;

        private bool _disposedValue;

        public WindowsTestFileShare(string shareName, string path)
        {
            _shareName = shareName;
            _path = path;
            Initialize();
        }

        public static bool CanShareFiles => _canShareFiles.Value;

        private void Initialize()
        {
            SHARE_INFO_502 shareInfo = default;
            shareInfo.shi502_netname = _shareName;
            shareInfo.shi502_path = _path;
            shareInfo.shi502_remark = "folder created to test UNC file paths";
            shareInfo.shi502_max_uses = -1;

            int infoSize = Marshal.SizeOf(shareInfo);
            IntPtr infoBuffer = Marshal.AllocCoTaskMem(infoSize);

            try
            {
                Marshal.StructureToPtr(shareInfo, infoBuffer, false);

                const int NERR_DuplicateShare = 2118;
                int shareResult = NetShareAdd(string.Empty, 502, infoBuffer, IntPtr.Zero);
                if (shareResult == NERR_DuplicateShare)
                {
                    NetShareDel(string.Empty, _shareName, 0);
                    shareResult = NetShareAdd(string.Empty, 502, infoBuffer, IntPtr.Zero);
                }

                if (shareResult != 0 && shareResult != NERR_DuplicateShare)
                {
                    throw new Exception($"Failed to create a file share, NetShareAdd returned {shareResult}");
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(infoBuffer);
            }
        }

        [LibraryImport(Interop.Libraries.Netapi32)]
        private static partial int NetShareAdd([MarshalAs(UnmanagedType.LPWStr)] string servername, int level, IntPtr buf, IntPtr parm_err);

        [LibraryImport(Interop.Libraries.Netapi32)]
        private static partial int NetShareDel([MarshalAs(UnmanagedType.LPWStr)] string servername, [MarshalAs(UnmanagedType.LPWStr)] string netname, int reserved);

        public void Dispose()
        {
            if (_disposedValue)
            {
                return;
            }

            NetShareDel(string.Empty, _shareName, 0);
            _disposedValue = true;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SHARE_INFO_502
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_netname;
            public uint shi502_type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_remark;
            public int shi502_permissions;
            public int shi502_max_uses;
            public int shi502_current_uses;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string shi502_path;
            public IntPtr shi502_passwd;
            public int shi502_reserved;
            public IntPtr shi502_security_descriptor;
        }
    }
}

