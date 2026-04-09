// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase : FileCleanupTestBase
    {
        protected void SetRegularFile(UstarTarEntry regularFile)
        {
            SetCommonRegularFile(regularFile);
            SetPosixProperties(regularFile);
        }

        protected void SetDirectory(UstarTarEntry directory)
        {
            SetCommonDirectory(directory);
            SetPosixProperties(directory);
        }

        protected void SetHardLink(UstarTarEntry hardLink)
        {
            SetCommonHardLink(hardLink);
            SetPosixProperties(hardLink);
        }

        protected void SetSymbolicLink(UstarTarEntry symbolicLink)
        {
            SetCommonSymbolicLink(symbolicLink);
            SetPosixProperties(symbolicLink);
        }

        protected void SetCharacterDevice(UstarTarEntry characterDevice)
        {
            SetCharacterDeviceProperties(characterDevice);
        }

        protected void SetBlockDevice(UstarTarEntry blockDevice)
        {
            SetBlockDeviceProperties(blockDevice);
        }

        protected void SetFifo(UstarTarEntry fifo)
        {
            SetFifoProperties(fifo);
        }

        protected void VerifyRegularFile(UstarTarEntry regularFile, bool isWritable)
        {
            VerifyPosixRegularFile(regularFile, isWritable);
        }

        protected void VerifyDirectory(UstarTarEntry directory)
        {
            VerifyPosixDirectory(directory);
        }

        protected void VerifyHardLink(UstarTarEntry hardLink)
        {
            VerifyPosixHardLink(hardLink);
        }

        protected void VerifySymbolicLink(UstarTarEntry symbolicLink)
        {
            VerifyPosixSymbolicLink(symbolicLink);
        }

        protected void VerifyCharacterDevice(UstarTarEntry characterDevice)
        {
            VerifyPosixCharacterDevice(characterDevice);
        }

        protected void VerifyBlockDevice(UstarTarEntry blockDevice)
        {
            VerifyPosixBlockDevice(blockDevice);
        }

        protected void VerifyFifo(UstarTarEntry fifo)
        {
            VerifyPosixFifo(fifo);
        }
    }
}
