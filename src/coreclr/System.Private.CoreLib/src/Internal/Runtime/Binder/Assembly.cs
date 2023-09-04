// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime.Binder
{
    // BINDER_SPACE::Assembly represents a result of binding to an actual assembly (PEImage)
    // It is basically a tuple of 1) physical assembly and 2) binder which created/owns this binding
    // We also store whether it was bound using TPA list
    internal sealed class Assembly
    {
        public IntPtr PEImage;

        public AssemblyName AssemblyName { get; }

        // private IntPtr _pBinder; // PTR_AssemblyBinder

        public bool IsInTPA { get; }

        // private IntPtr _domainAssembly; // DomainAssembly*

        public Assembly(nint pPEImage, bool isInTPA)
        {
            // Get assembly name def from meta data import and store it for later refs access
            AssemblyName = new AssemblyName(pPEImage)
            {
                IsDefinition = true
            };

            // validate architecture
            if (!AssemblyBinderCommon.IsValidArchitecture(AssemblyName.Architecture))
            {
                // Assembly image can't be executed on this platform
                throw new BadImageFormatException();
            }

            IsInTPA = isInTPA;
            PEImage = pPEImage;
        }
    }
}
