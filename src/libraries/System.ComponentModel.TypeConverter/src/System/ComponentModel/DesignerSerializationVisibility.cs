// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies the visibility a property has to the design time
    ///          serializer.
    ///    </para>
    /// </devdoc>
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum DesignerSerializationVisibility
    {
        /// <devdoc>
        ///    <para>The code generator will not produce code for the object.</para>
        /// </devdoc>
        Hidden,

        /// <devdoc>
        ///    <para>The code generator will produce code for the object.</para>
        /// </devdoc>
        Visible,

        /// <devdoc>
        ///    <para>The code generator will produce code for the contents of the object, rather than for the object itself.</para>
        /// </devdoc>
        Content
    }
}
