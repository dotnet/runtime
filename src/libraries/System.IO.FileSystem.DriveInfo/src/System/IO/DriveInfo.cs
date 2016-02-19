// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace System.IO
{
    public sealed partial class DriveInfo
    {
        private readonly String _name;

        public DriveInfo(String driveName)
        {
            if (driveName == null)
            {
                throw new ArgumentNullException(nameof(driveName));
            }
            Contract.EndContractBlock();

            _name = NormalizeDriveName(driveName);
        }

        public String Name
        {
            get { return _name; }
        }

        public bool IsReady
        {
            get { return Directory.Exists(Name); }
        }

        public DirectoryInfo RootDirectory
        {
            get { return new DirectoryInfo(Name); }
        }

        public override String ToString()
        {
            return Name;
        }
    }
}
