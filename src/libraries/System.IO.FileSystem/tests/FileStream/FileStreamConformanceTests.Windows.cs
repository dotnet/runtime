// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.ServiceProcess;
using System.Threading.Tasks;
using Xunit;
using System.Threading;

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
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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

    [PlatformSpecific(TestPlatforms.Windows)] // the test setup is Windows-specifc
    [OuterLoop("Has a very complex setup logic that in theory might have some side-effects")]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class DeviceInterfaceTests
    {
        [Fact]
        public async Task DeviceInterfaceCanBeOpenedForAsyncIO()
        {
            FileStream? fileStream = OpenFirstAvailableDeviceInterface();

            if (fileStream is null)
            {
                // it's OK to not have any such devices available
                // this test is just best effort
                return;
            }

            using (fileStream)
            {
                Assert.True(fileStream.CanRead);
                Assert.False(fileStream.CanWrite);
                Assert.False(fileStream.CanSeek); // #54143

                try
                {
                    CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(250));

                    await fileStream.ReadAsync(new byte[4096], cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // most likely there is no data available and the task is going to get cancelled
                    // which is fine, we just want to make sure that reading from devices is supported (#54143)
                }
            }
        }

        private static FileStream? OpenFirstAvailableDeviceInterface()
        {
            const int DIGCF_PRESENT = 0x2;
            const int DIGCF_DEVICEINTERFACE = 0x10;
            const int ERROR_NO_MORE_ITEMS = 259;

            HidD_GetHidGuid(out Guid HidGuid);
            IntPtr deviceInfoSet = SetupDiGetClassDevs(in HidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

            try
            {
                SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                uint deviceIndex = 0;
                while (SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex++, ref deviceInfoData))
                {
                    if (Marshal.GetLastWin32Error() == ERROR_NO_MORE_ITEMS)
                    {
                        break;
                    }

                    SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
                    deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                    if (!SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, in HidGuid, deviceIndex, ref deviceInterfaceData))
                    {
                        continue;
                    }

                    SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                    deviceInterfaceDetailData.cbSize = IntPtr.Size == 8 ? 8 : 6;

                    uint size = (uint)Marshal.SizeOf(deviceInterfaceDetailData);

                    if (!SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, ref deviceInterfaceDetailData, size, ref size, IntPtr.Zero))
                    {
                        continue;
                    }

                    string devicePath = deviceInterfaceDetailData.DevicePath;
                    Assert.StartsWith(@"\\?\hid", devicePath);

                    try
                    {
                        return new FileStream(devicePath, FileMode.Open, FileAccess.Read, FileShare.Read, 0, FileOptions.Asynchronous);
                    }
                    catch (IOException)
                    {
                        continue; // device has been locked by another process
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid interfaceClassGuid;
            public int flags;
            private nuint reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public nint Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] // 256 should be always enough for device interface path
            public string DevicePath;
        }

        [DllImport("hid.dll", SetLastError = true)]
        static extern void HidD_GetHidGuid(out Guid HidGuid);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern IntPtr SetupDiGetClassDevs(in Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, in Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, ref uint RequiredSize, IntPtr DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);
    }
}
