// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.IO
{
    [Serializable]
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

        private DriveInfo(SerializationInfo info, StreamingContext context) :
            this((string)info.GetValue("_name", typeof(string)))
        {
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("_name", _name, typeof(String));
        }

        public string Name => _name;

        public bool IsReady => Directory.Exists(Name);

        public DirectoryInfo RootDirectory => new DirectoryInfo(Name);

        public override string ToString() => Name;
    }
}
