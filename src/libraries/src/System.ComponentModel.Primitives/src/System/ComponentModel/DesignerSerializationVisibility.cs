// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the visibility a property has to the design time serializer.
    /// </summary>
    public enum DesignerSerializationVisibility
    {
        /// <summary>
        /// The code generator will not produce code for the object.
        /// </summary>
        Hidden,

        /// <summary>
        /// The code generator will produce code for the object.
        /// </summary>
        Visible,

        /// <summary>
        /// The code generator will produce code for the contents of the object, rather than for the object itself.
        /// </summary>
        Content
    }
}
