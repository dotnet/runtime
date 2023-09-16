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
        // fields used by VM
#pragma warning disable CA1823, 414, 169
        public IntPtr PEImage;
        private IntPtr m_pDomainAssembly;
        private AssemblyBinder? m_binder;
        private bool m_isCoreLib;
#pragma warning restore CA1823, 414, 169

        public AssemblyBinder? Binder
        {
            get => m_binder;
            set => m_binder = value;
        }

        public AssemblyName AssemblyName { get; }

        public bool IsInTPA { get; }


        public Assembly(nint pPEImage, bool isInTPA)
        {
            // Get assembly name def from meta data import and store it for later refs access
            AssemblyName = new AssemblyName(pPEImage)
            {
                IsDefinition = true
            };

            m_isCoreLib = AssemblyName.IsCoreLib;

            // validate architecture
            if (!AssemblyBinderCommon.IsValidArchitecture(AssemblyName.ProcessorArchitecture))
            {
                // Assembly image can't be executed on this platform
                throw new BadImageFormatException();
            }

            IsInTPA = isInTPA;
            PEImage = pPEImage;
        }
    }
}
