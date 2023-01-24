// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        /// <summary>
        /// Statistics for <see cref="IStream"/>.
        /// <see href="https://docs.microsoft.com/en-us/windows/desktop/api/objidl/ns-objidl-tagstatstg"/>
        /// </summary>
        /// <remarks>
        /// The definition in <see cref="System.Runtime.InteropServices.ComTypes"/> isn't blittable.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct STATSTG
        {
            /// <summary>
            /// Pointer to the name.
            /// </summary>
            private IntPtr pwcsName;
            public STGTY type;

            /// <summary>
            /// Size of the stream in bytes.
            /// </summary>
            public ulong cbSize;

            public FILETIME mtime;
            public FILETIME ctime;
            public FILETIME atime;

            /// <summary>
            /// The stream mode.
            /// </summary>
            public STGM grfMode;

            /// <summary>
            /// Supported locking modes.
            /// <see href="https://docs.microsoft.com/en-us/windows/desktop/api/objidl/ne-objidl-taglocktype"/>
            /// </summary>
            /// <remarks>
            /// '0' means does not support lock modes.
            /// </remarks>
            public uint grfLocksSupported;

            /// <remarks>
            /// Only for IStorage objects
            /// </remarks>
            public Guid clsid;

            /// <remarks>
            /// Only valid for IStorage objects.
            /// </remarks>
            public uint grfStateBits;
            public uint reserved;

            public string? GetName() => Marshal.PtrToStringUni(pwcsName);

            /// <summary>
            /// Caller is responsible for freeing the name memory.
            /// </summary>
            public void FreeName()
            {
                if (pwcsName != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pwcsName);

                pwcsName = IntPtr.Zero;
            }

            /// <summary>
            /// Callee is responsible for allocating the name memory.
            /// </summary>
            public void AllocName(string? name)
            {
                pwcsName = Marshal.StringToCoTaskMemUni(name);
            }
        }
    }
}
