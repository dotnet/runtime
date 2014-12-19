// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.IO
{
    /// <devdoc>
    ///   <para>File attribute flags correspondiong to NT's flags.</para>
    /// <devdoc>
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum FileAttributes
    {
        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        ReadOnly = 0x0001,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        Hidden = 0x0002,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        System = 0x0004,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        Directory = 0x0010,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        Archive = 0x0020,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        Device = 0x0040,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        Normal = 0x0080,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        Temporary = 0x0100,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        SparseFile = 0x0200,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        ReparsePoint = 0x0400,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        Compressed = 0x0800,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        Offline = 0x1000,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        NotContentIndexed = 0x2000,

        /// <devdoc>
        ///   <para>[To be supplied.]<para>
        /// </devdoc>
        Encrypted = 0x4000
    }
}