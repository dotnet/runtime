// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.IO
{
    public sealed partial class DriveInfo
    {
        public DriveInfo(string driveName) { }
        public long AvailableFreeSpace { get { return default(long); } }
        public string DriveFormat { get { return default(string); } }
        public System.IO.DriveType DriveType { get { return default(System.IO.DriveType); } }
        public bool IsReady { get { return default(bool); } }
        public string Name { get { return default(string); } }
        public System.IO.DirectoryInfo RootDirectory { get { return default(System.IO.DirectoryInfo); } }
        public long TotalFreeSpace { get { return default(long); } }
        public long TotalSize { get { return default(long); } }
        public string VolumeLabel { get { return default(string); } set { } }
        public static System.IO.DriveInfo[] GetDrives() { return default(System.IO.DriveInfo[]); }
        public override string ToString() { return default(string); }
    }
    public partial class DriveNotFoundException : System.IO.IOException
    {
        public DriveNotFoundException() { }
        public DriveNotFoundException(string message) { }
        public DriveNotFoundException(string message, System.Exception innerException) { }
    }
    public enum DriveType
    {
        CDRom = 5,
        Fixed = 3,
        Network = 4,
        NoRootDirectory = 1,
        Ram = 6,
        Removable = 2,
        Unknown = 0,
    }
}
