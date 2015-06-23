// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                throw new ArgumentNullException("driveName");
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
