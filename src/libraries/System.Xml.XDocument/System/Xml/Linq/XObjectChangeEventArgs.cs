// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

namespace System.Xml.Linq
{
    /// <summary>
    /// Provides data for the <see cref="XObject.Changing"/> and <see cref="XObject.Changed"/> events.
    /// </summary>
    public class XObjectChangeEventArgs : EventArgs
    {
        XObjectChange objectChange;

        /// <summary>
        /// Event argument for a <see cref="XObjectChange.Add"/> change event.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "XObjectChangeEventArgs is in fact immutable.")]
        public static readonly XObjectChangeEventArgs Add = new XObjectChangeEventArgs(XObjectChange.Add);

        /// <summary>
        /// Event argument for a <see cref="XObjectChange.Remove"/> change event.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "XObjectChangeEventArgs is in fact immutable.")]
        public static readonly XObjectChangeEventArgs Remove = new XObjectChangeEventArgs(XObjectChange.Remove);

        /// <summary>
        /// Event argument for a <see cref="XObjectChange.Name"/> change event.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "XObjectChangeEventArgs is in fact immutable.")]
        public static readonly XObjectChangeEventArgs Name = new XObjectChangeEventArgs(XObjectChange.Name);

        /// <summary>
        /// Event argument for a <see cref="XObjectChange.Value"/> change event.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "XObjectChangeEventArgs is in fact immutable.")]
        public static readonly XObjectChangeEventArgs Value = new XObjectChangeEventArgs(XObjectChange.Value);

        /// <summary>
        /// Initializes a new instance of the <see cref="XObjectChangeEventArgs"/> class.
        /// </summary>
        public XObjectChangeEventArgs(XObjectChange objectChange)
        {
            this.objectChange = objectChange;
        }

        /// <summary>
        /// Gets the type (<see cref="XObjectChange"/>) of change.
        /// </summary>
        public XObjectChange ObjectChange
        {
            get { return objectChange; }
        }
    }
}
