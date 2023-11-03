// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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
            string pipeName = GetNamedPipeServerStreamName();
            string pipePath = Path.GetFullPath($@"\\.\pipe\{pipeName}");

            var server = new NamedPipeServerStream(pipeName, PipeDirection.In);
            var clienStream = new FileStream(pipePath, FileMode.Open, FileAccess.Write, FileShare.None);

            await server.WaitForConnectionAsync();

            var serverStrean = new FileStream(new SafeFileHandle(server.SafePipeHandle.DangerousGetHandle(), true), FileAccess.Read);

            server.SafePipeHandle.SetHandleAsInvalid();

            return (serverStrean, clienStream);
        }

        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override bool UsableAfterCanceledReads => false;
        protected override bool FullyCancelableOperations => OperatingSystem.IsWindows();
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

    [PlatformSpecific(TestPlatforms.Windows)] // the test setup is Windows-specific
    [Collection(nameof(DisableParallelization))] // don't run in parallel, as file sharing logic is not thread-safe
    [OuterLoop("Requires admin privileges to create a file share")]
    [ConditionalClass(typeof(WindowsTestFileShare), nameof(WindowsTestFileShare.CanShareFiles))]
    public class UncFilePathFileStreamStandaloneConformanceTests : UnbufferedAsyncFileStreamStandaloneConformanceTests
    {
        private WindowsTestFileShare _testShare;

        protected override string GetTestFilePath(int? index = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string testDirectoryPath = Path.GetFullPath(TestDirectory);
            string shareName = new DirectoryInfo(testDirectoryPath).Name;
            string fileName = GetTestFileName(index, memberName, lineNumber);
            _testShare = new WindowsTestFileShare(shareName, testDirectoryPath);

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
            try
            {
                _testShare?.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }

    [PlatformSpecific(TestPlatforms.Windows)] // the test setup is Windows-specifc
    [OuterLoop("Has a very complex setup logic that in theory might have some side-effects")]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
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
                    if (Marshal.GetLastPInvokeError() == ERROR_NO_MORE_ITEMS)
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
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        continue; // device has been locked by another process or we don't have permissions to access it
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
