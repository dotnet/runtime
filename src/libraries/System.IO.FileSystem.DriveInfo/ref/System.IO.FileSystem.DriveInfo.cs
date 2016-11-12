// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.IO
{
    public sealed partial class DriveInfo : System.Runtime.Serialization.ISerializable
    {
        public DriveInfo(string driveName) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public long AvailableFreeSpace { get { throw null; } }
        public string DriveFormat { get { throw null; } }
        public System.IO.DriveType DriveType { get { throw null; } }
        public bool IsReady { get { throw null; } }
        public string Name { get { throw null; } }
        public System.IO.DirectoryInfo RootDirectory { get { throw null; } }
        public long TotalFreeSpace { get { throw null; } }
        public long TotalSize { get { throw null; } }
        public string VolumeLabel { get { throw null; } set { } }
        public static System.IO.DriveInfo[] GetDrives() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class DriveNotFoundException : System.IO.IOException
    {
        public DriveNotFoundException() { }
        public DriveNotFoundException(string message) { }
        public DriveNotFoundException(string message, System.Exception innerException) { }
        protected DriveNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
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
