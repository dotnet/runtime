// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>Wraps io_uring_setup(2): creates an io_uring instance.</summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoUringShimSetup")]
        internal static unsafe partial Error IoUringShimSetup(
            uint entries, void* parms, int* ringFd);

        /// <summary>Wraps io_uring_enter(2): submits SQEs and/or waits for CQEs.</summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoUringShimEnter")]
        internal static unsafe partial Error IoUringShimEnter(
            int ringFd, uint toSubmit, uint minComplete, uint flags, int* result);

        /// <summary>Wraps io_uring_enter2(2) with IORING_ENTER_EXT_ARG for bounded waits.</summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoUringShimEnterExt")]
        internal static unsafe partial Error IoUringShimEnterExt(
            int ringFd, uint toSubmit, uint minComplete, uint flags, void* arg, int* result);

        /// <summary>Wraps io_uring_register(2): registers resources (files, buffers, ring fds).</summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoUringShimRegister")]
        internal static unsafe partial Error IoUringShimRegister(
            int ringFd, uint opcode, void* arg, uint nrArgs, int* result);

        /// <summary>Wraps mmap(2): maps io_uring SQ/CQ ring memory.</summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoUringShimMmap")]
        internal static unsafe partial Error IoUringShimMmap(
            int ringFd, ulong size, ulong offset, void** mappedPtr);

        /// <summary>Wraps munmap(2): unmaps io_uring ring memory.</summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoUringShimMunmap")]
        internal static unsafe partial Error IoUringShimMunmap(
            void* addr, ulong size);

        /// <summary>Creates an eventfd for io_uring wakeup signaling.</summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoUringShimCreateEventFd")]
        internal static unsafe partial Error IoUringShimCreateEventFd(
            int* eventFd);

        /// <summary>Writes to an eventfd to wake the io_uring event loop.</summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoUringShimWriteEventFd")]
        internal static partial Error IoUringShimWriteEventFd(int eventFd);

        /// <summary>Reads from an eventfd to consume a wakeup signal.</summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoUringShimReadEventFd")]
        internal static unsafe partial Error IoUringShimReadEventFd(
            int eventFd, ulong* value);

        /// <summary>Wraps close(2): closes a file descriptor.</summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IoUringShimCloseFd")]
        internal static partial Error IoUringShimCloseFd(int fd);
    }
}
