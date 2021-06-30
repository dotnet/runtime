// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.ServiceProcess;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)] // DOS device paths (\\.\ and \\?\) are a Windows concept
    public class UnseekableDeviceFileStreamConnectedConformanceTests : ConnectedStreamConformanceTests
    {
        protected override async Task<StreamPair> CreateConnectedStreamsAsync()
        {
            string pipeName = FileSystemTest.GetNamedPipeServerStreamName();
            string pipePath = Path.GetFullPath($@"\\.\pipe\{pipeName}");

            var server = new NamedPipeServerStream(pipeName, PipeDirection.In);
            var clienStream = new FileStream(File.OpenHandle(pipePath, FileMode.Open, FileAccess.Write, FileShare.None), FileAccess.Write);

            await server.WaitForConnectionAsync();

            var serverStrean = new FileStream(new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), true), FileAccess.Read);

            server.SafePipeHandle.SetHandleAsInvalid();

            return (serverStrean, clienStream);
        }

        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override bool UsableAfterCanceledReads => false;
        protected override bool FullyCancelableOperations => false;
        protected override bool BlocksOnZeroByteReads => OperatingSystem.IsWindows();
        protected override bool SupportsConcurrentBidirectionalUse => false;
    }

    [PlatformSpecific(TestPlatforms.Windows)] // DOS device paths (\\.\ and \\?\) are a Windows concept
    public class SeekableDeviceFileStreamStandaloneConformanceTests : UnbufferedAsyncFileStreamStandaloneConformanceTests
    {
        protected override string GetTestFilePath(int? index = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string filePath = Path.GetFullPath(base.GetTestFilePath(index, memberName, lineNumber));
            string drive = Path.GetPathRoot(filePath);
            StringBuilder volumeNameBuffer = new StringBuilder(filePath.Length + 1024);

            // the following method maps drive letter like "C:\" to a DeviceID (a DOS device path)
            // example: "\\?\Volume{724edb31-eaa5-4728-a4e3-f2474fd34ae2}\"
            if (!GetVolumeNameForVolumeMountPoint(drive, volumeNameBuffer, volumeNameBuffer.Capacity))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "GetVolumeNameForVolumeMountPoint failed");
            }

            // instead of:
            // 'C:\Users\x\AppData\Local\Temp\y\z
            // we want something like:
            // '\\.\Volume{724edb31-eaa5-4728-a4e3-f2474fd34ae2}\Users\x\AppData\Local\Temp\y\z
            string devicePath = filePath.Replace(drive, volumeNameBuffer.ToString());
            Assert.StartsWith(@"\\?\", devicePath);
#if DEBUG
            // we do want to test \\.\ prefix as well
            devicePath = devicePath.Replace(@"\\?\", @"\\.\");
#endif

            return devicePath;
        }

        [DllImport(Interop.Libraries.Kernel32, EntryPoint = "GetVolumeNameForVolumeMountPointW", CharSet = CharSet.Unicode, BestFitMapping = false, SetLastError = true)]
        private static extern bool GetVolumeNameForVolumeMountPoint(string volumeName, StringBuilder uniqueVolumeName, int uniqueNameBufferCapacity);
    }

    [PlatformSpecific(TestPlatforms.Windows)] // the test setup is Windows-specifc
    [Collection("NoParallelTests")] // don't run in parallel, as file sharing logic is not thread-safe
    [OuterLoop("Requires admin privileges to create a file share")]
    [ConditionalClass(typeof(UncFilePathFileStreamStandaloneConformanceTests), nameof(CanShareFiles))]
    public class UncFilePathFileStreamStandaloneConformanceTests : UnbufferedAsyncFileStreamStandaloneConformanceTests
    {
        public static bool CanShareFiles => _canShareFiles.Value;

        private static Lazy<bool> _canShareFiles = new Lazy<bool>(() =>
        {
            if (!PlatformDetection.IsWindowsAndElevated || PlatformDetection.IsWindowsNanoServer)
            {
                return false;
            }

            // the "Server Service" allows for file sharing. It can be disabled on some of our CI machines.    
            using (ServiceController sharingService = new ServiceController("Server"))
            {
                return sharingService.Status == ServiceControllerStatus.Running;
            }
        });

        protected override string GetTestFilePath(int? index = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string testDirectoryPath = Path.GetFullPath(TestDirectory);
            string shareName = new DirectoryInfo(testDirectoryPath).Name;
            string fileName = GetTestFileName(index, memberName, lineNumber);

            SHARE_INFO_502 shareInfo = default;
            shareInfo.shi502_netname = shareName;
            shareInfo.shi502_path = testDirectoryPath;
            shareInfo.shi502_remark = "folder created to test UNC file paths";
            shareInfo.shi502_max_uses = -1;

            int infoSize = Marshal.SizeOf(shareInfo);
            IntPtr infoBuffer = Marshal.AllocCoTaskMem(infoSize);

            try
            {
                Marshal.StructureToPtr(shareInfo, infoBuffer, false);

                int shareResult = NetShareAdd(string.Empty, 502, infoBuffer, IntPtr.Zero);

                if (shareResult != 0 && shareResult != 2118) // is a failure that is not a NERR_DuplicateShare
                {
                    throw new Exception($"Failed to create a file share, NetShareAdd returned {shareResult}");
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(infoBuffer);
            }

            // now once the folder has been shared we can use "localhost" to access it:
            // both type of slashes are valid, so let's test one for Debug and another for other configs
#if DEBUG
            return @$"//localhost/{shareName}/{fileName}";
#else
            return @$"\\localhost\{shareName}\{fileName}";
#endif
        }

        protected override void Dispose(bool disposing)
        {
            string testDirectoryPath = Path.GetFullPath(TestDirectory);
            string shareName = new DirectoryInfo(testDirectoryPath).Name;

            try
            {
                NetShareDel(string.Empty, shareName, 0);
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SHARE_INFO_502
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

        [DllImport(Interop.Libraries.Netapi32)]
        public static extern int NetShareAdd([MarshalAs(UnmanagedType.LPWStr)]string servername, int level, IntPtr buf, IntPtr parm_err);

        [DllImport(Interop.Libraries.Netapi32)]
        public static extern int NetShareDel([MarshalAs(UnmanagedType.LPWStr)] string servername, [MarshalAs(UnmanagedType.LPWStr)] string netname, int reserved);
    }
}
