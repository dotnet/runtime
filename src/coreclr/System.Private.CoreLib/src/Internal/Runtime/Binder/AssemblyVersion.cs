// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime.Binder
{
    internal record struct AssemblyVersion
    {
        public int Major;
        public int Minor;
        public int Build;
        public int Revision;

        public const int Unspecified = -1;

        public AssemblyVersion()
        {
            Major = Unspecified;
            Minor = Unspecified;
            Build = Unspecified;
            Revision = Unspecified;
        }
    }
}
