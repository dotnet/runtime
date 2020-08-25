// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.IO
{
    public sealed partial class DriveInfo : ISerializable
    {
        private readonly string _name;

        public DriveInfo(string driveName)
        {
            if (driveName == null)
            {
                throw new ArgumentNullException(nameof(driveName));
            }

            _name = NormalizeDriveName(driveName);
        }

        [UnsupportedOSPlatform("browser")]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public string Name => _name;

        public bool IsReady => Directory.Exists(Name);

        public DirectoryInfo RootDirectory => new DirectoryInfo(Name);

        public override string ToString() => Name;
    }
}
