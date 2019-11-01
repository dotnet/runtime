// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class ComCompatibleVersionAttribute : Attribute
    {
        public ComCompatibleVersionAttribute(int major, int minor, int build, int revision)
        {
            MajorVersion = major;
            MinorVersion = minor;
            BuildNumber = build;
            RevisionNumber = revision;
        }

        public int MajorVersion { get; }
        public int MinorVersion { get; }
        public int BuildNumber { get; }
        public int RevisionNumber { get; }
    }
}
