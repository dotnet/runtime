// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace System.IO.MemoryMappedFiles
{
    public class MemoryMappedFileSecurity : ObjectSecurity<MemoryMappedFileRights>
    {
        public MemoryMappedFileSecurity()
            : base(false, ResourceType.KernelObject) => throw new PlatformNotSupportedException();

        internal MemoryMappedFileSecurity(SafeMemoryMappedFileHandle safeHandle, AccessControlSections includeSections)
            : base(false, ResourceType.KernelObject, safeHandle, includeSections) => throw new PlatformNotSupportedException();

        internal void PersistHandle(SafeHandle handle) => throw new PlatformNotSupportedException();
    }
}
