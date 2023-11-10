// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Loader
{
    // BINDER_SPACE::Assembly represents a result of binding to an actual assembly (PEImage)
    // It is basically a tuple of 1) physical assembly and 2) binder which created/owns this binding
    // We also store whether it was bound using TPA list
    internal sealed class BinderAssembly
    {
        // fields used by VM
#pragma warning disable CA1823, 414, 169
        private AssemblyLoadContext? m_binder;
        private BinderAssemblyName m_assemblyName;
        private IntPtr m_peImage;
        private IntPtr m_pDomainAssembly;
        private bool m_isInTPA;
        private bool m_isCoreLib;
#pragma warning restore CA1823, 414, 169

        public AssemblyLoadContext? Binder
        {
            get => m_binder;
            set => m_binder = value;
        }

        public BinderAssemblyName AssemblyName => m_assemblyName;

        public IntPtr PEImage => m_peImage;

        public bool IsInTPA => m_isInTPA;

        public BinderAssembly(nint pPEImage, bool isInTPA)
        {
            // Get assembly name def from meta data import and store it for later refs access
            m_assemblyName = new BinderAssemblyName(pPEImage)
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

            m_isInTPA = isInTPA;
            m_peImage = pPEImage;
        }
    }
}
