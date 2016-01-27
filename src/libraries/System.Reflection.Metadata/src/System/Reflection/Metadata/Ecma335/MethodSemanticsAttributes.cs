// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata.Ecma335
{
    [Flags]
    public enum MethodSemanticsAttributes
    {
        /// <summary>
        /// Used to modify the value of the property.
        /// CLS-compliant setters are named with set_ prefix.
        /// </summary>
        Setter = 0x0001,

        /// <summary>
        /// Used to read the value of the property.
        /// CLS-compliant getters are named with get_ prefix.
        /// </summary>
        Getter = 0x0002,

        /// <summary>
        /// Other method for property (not getter or setter) or event (not adder, remover, or raiser).
        /// </summary>
        Other = 0x0004,

        /// <summary>
        /// Used to add a handler for an event.
        /// Corresponds to the AddOn flag in the Ecma 335 CLI specification.
        /// CLS-compliant adders are named with add_ prefix.
        /// </summary>
        Adder = 0x0008,

        /// <summary>
        /// Used to remove a handler for an event.
        /// Corresponds to the RemoveOn flag in the Ecma 335 CLI specification.
        /// CLS-compliant removers are named with remove_ prefix.
        /// </summary>
        Remover = 0x0010,

        /// <summary>
        /// Used to indicate that an event has occurred.
        /// Corresponds to the Fire flag in the Ecma 335 CLI specification.
        /// CLS-compliant raisers are named with raise_ prefix.
        /// </summary>
        Raiser = 0x0020,
    }
}
