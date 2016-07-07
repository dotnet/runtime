// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    /// <devdoc>
    ///    <para>
    ///       Specifies member attributes used for class members.
    ///    </para>
    /// </devdoc>
    internal enum MemberAttributes
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Abstract = 0x0001,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Final = 0x0002,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Static = 0x0003,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Override = 0x0004,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Const = 0x0005,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        New = 0x0010,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Overloaded = 0x0100,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Assembly = 0x1000,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        FamilyAndAssembly = 0x2000,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Family = 0x3000,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        FamilyOrAssembly = 0x4000,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Private = 0x5000,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Public = 0x6000,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        AccessMask = 0xF000,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ScopeMask = 0x000F,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        VTableMask = 0x00F0,
    }
}
