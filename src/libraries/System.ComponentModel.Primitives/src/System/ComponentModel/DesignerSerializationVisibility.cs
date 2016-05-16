// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies the visibility a property has to the design time
    ///          serializer.
    ///    </para>
    /// </summary>
    public enum DesignerSerializationVisibility
    {
        /// <summary>
        ///    <para>The code generator will not produce code for the object.</para>
        /// </summary>
        Hidden,

        /// <summary>
        ///    <para>The code generator will produce code for the object.</para>
        /// </summary>
        Visible,

        /// <summary>
        ///    <para>The code generator will produce code for the contents of the object, rather than for the object itself.</para>
        /// </summary>
        Content
    }
}
