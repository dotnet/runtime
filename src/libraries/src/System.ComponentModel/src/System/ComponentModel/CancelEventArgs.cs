// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel
{
    /// <summary>
    /// EventArgs used to describe a cancel event.
    /// </summary>
    public class CancelEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether we should cancel the operation or not
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public CancelEventArgs()
        {
        }

        /// <summary>
        /// Helper constructor
        /// </summary>
        /// <param name="cancel"></param>
        public CancelEventArgs(bool cancel)
        {
            this.Cancel = cancel;
        }
    }
}