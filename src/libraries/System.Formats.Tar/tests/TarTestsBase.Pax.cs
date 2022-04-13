// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase : FileCleanupTestBase
    {
        protected void SetRegularFile(PaxTarEntry regularFile)
        {
            SetCommonRegularFile(regularFile);
            SetPosixProperties(regularFile);
        }

        protected void SetDirectory(PaxTarEntry directory)
        {
            SetCommonDirectory(directory);
            SetPosixProperties(directory);
        }

        protected void SetHardLink(PaxTarEntry hardLink)
        {
            SetCommonHardLink(hardLink);
            SetPosixProperties(hardLink);
        }

        protected void SetSymbolicLink(PaxTarEntry symbolicLink)
        {
            SetCommonSymbolicLink(symbolicLink);
            SetPosixProperties(symbolicLink);
        }

        protected void SetCharacterDevice(PaxTarEntry characterDevice)
        {
            SetCharacterDeviceProperties(characterDevice);
        }

        protected void SetBlockDevice(PaxTarEntry blockDevice)
        {
            SetBlockDeviceProperties(blockDevice);
        }

        protected void SetFifo(PaxTarEntry fifo)
        {
            SetFifoProperties(fifo);
        }

        protected void VerifyRegularFile(PaxTarEntry regularFile, bool isWritable)
        {
            VerifyPosixRegularFile(regularFile, isWritable);
            // TODO: Here and elsewhere, verify pax ext attrs
        }

        protected void VerifyDirectory(PaxTarEntry directory)
        {
            VerifyPosixDirectory(directory);
        }

        protected void VerifyHardLink(PaxTarEntry hardLink)
        {
            VerifyPosixHardLink(hardLink);
        }

        protected void VerifySymbolicLink(PaxTarEntry symbolicLink)
        {
            VerifyPosixSymbolicLink(symbolicLink);
        }

        protected void VerifyCharacterDevice(PaxTarEntry characterDevice)
        {
            VerifyPosixCharacterDevice(characterDevice);
        }

        protected void VerifyBlockDevice(PaxTarEntry blockDevice)
        {
            VerifyPosixBlockDevice(blockDevice);
        }

        protected void VerifyFifo(PaxTarEntry fifo)
        {
            VerifyPosixFifo(fifo);
        }
    }
}
