// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO
{
    public sealed partial class DriveInfo : System.Runtime.Serialization.ISerializable
    {
        public DriveInfo(string driveName) { }
        public long AvailableFreeSpace { get { throw null; } }
        public string DriveFormat { get { throw null; } }
        public System.IO.DriveType DriveType { get { throw null; } }
        public bool IsReady { get { throw null; } }
        public string Name { get { throw null; } }
        public System.IO.DirectoryInfo RootDirectory { get { throw null; } }
        public long TotalFreeSpace { get { throw null; } }
        public long TotalSize { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string VolumeLabel { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public static System.IO.DriveInfo[] GetDrives() { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    public partial class DriveNotFoundException : System.IO.IOException
    {
        public DriveNotFoundException() { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected DriveNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DriveNotFoundException(string? message) { }
        public DriveNotFoundException(string? message, System.Exception? innerException) { }
    }
    public enum DriveType
    {
        Unknown = 0,
        NoRootDirectory = 1,
        Removable = 2,
        Fixed = 3,
        Network = 4,
        CDRom = 5,
        Ram = 6,
    }
}
