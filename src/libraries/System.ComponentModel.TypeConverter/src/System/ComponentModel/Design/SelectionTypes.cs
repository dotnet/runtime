// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Specifies identifiers that indicate the type of selection for a component or
    /// group of components that are selected.
    /// </summary>
    [Flags]
    public enum SelectionTypes
    {
        /// <summary>
        /// A Normal selection. With this type of selection, the selection service responds
        /// to the control and shift keys to support appending or toggling components into the
        /// selection as needed.
        /// </summary>
        Auto = 0x0001,

        /// <summary>
        /// A Normal selection. With this type of selection, the selection service responds
        /// to the control and shift keys to support appending or toggling components into the
        /// selection as needed.
        /// </summary>
        [Obsolete("SelectionTypes.Normal has been deprecated. Use SelectionTypes.Auto instead.")]
        Normal = 0x0001,

        /// <summary>
        /// A Replace selection. This causes the selection service to always replace the
        /// current selection with the replacement.
        /// </summary>
        Replace = 0x0002,

        /// <summary>
        /// A MouseDown selection. Happens when the user presses down on
        /// the mouse button when the pointer is over a control (or component). If a
        /// component in the selection list is already selected, it does not remove the
        /// existing selection, but promotes that component to be the primary selection.
        /// </summary>
        [Obsolete("SelectionTypes.MouseDown has been deprecated and is not supported.")]
        MouseDown = 0x0004,

        /// <summary>
        /// A MouseUp selection. Happens when the user releases the
        /// mouse button when a control (or component) has been selected. If a component
        /// in the selection list is already selected, it does not remove the
        /// existing selection, but promotes that component to be the primary selection.
        /// </summary>
        [Obsolete("SelectionTypes.MouseUp has been deprecated and is not supported.")]
        MouseUp = 0x0008,

        /// <summary>
        /// A Click selection.
        /// Happens when a user clicks on a component. If a component in the selection list is already
        /// selected, it does not remove the existing selection, but promotes that component to be the
        /// primary selection.
        /// </summary>
        [Obsolete("SelectionTypes.Click has been deprecated. Use SelectionTypes.Primary instead.")]
        Click = 0x0010,

        /// <summary>
        /// A Primary selection.
        /// Happens when a user clicks on a component. If a component in the selection list is already
        /// selected, it does not remove the existing selection, but promotes that component to be the
        /// primary selection.
        /// </summary>
        Primary = 0x0010,

        /// <summary>
        /// A toggle selection.
        /// This selection toggles the current selection with the provided selection. So, if
        /// a component is already selected and is passed into SetSelectedComponents with a
        /// selection type of Toggle, it will be unselected.
        /// </summary>
        Toggle = 0x0020,

        /// <summary>
        /// An Add selection.
        /// This selection adds the selected components to the current selection,
        /// maintaining the current set of selected components.
        /// </summary>
        Add = 0x0040,

        /// <summary>
        /// A Remove selection.
        /// This selection removes the selected components from the current selection,
        /// maintaining the current set of selected components.
        /// </summary>
        Remove = 0x0080,

        /// <summary>
        /// Limits valid selection types to Normal, Replace, MouseDown, MouseUp,
        /// Click, Toggle or Add.
        /// </summary>
        [Obsolete("SelectionTypes.Valid has been deprecated. Use Enum class methods to determine valid values, or use a type converter instead.")]
        Valid = 0x1F,
    }
}
