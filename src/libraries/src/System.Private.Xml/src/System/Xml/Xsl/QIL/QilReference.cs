// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// View over a Qil node which is the target of a reference (functions, variables, parameters).
    /// </summary>
    internal class QilReference : QilNode
    {
        // Names longer than 1023 characters cause AV in cscompee.dll, see VSWhidbey 485526
        // So we set the internal limit to 1000. Needs to be lower since we might later append
        //   few characters (for example "(2)") if we end up with two same named methods after
        //   the truncation.
        private const int MaxDebugNameLength = 1000;

        private string _debugName;

        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Construct a reference
        /// </summary>
        public QilReference(QilNodeType nodeType) : base(nodeType)
        {
        }


        //-----------------------------------------------
        // QilReference methods
        //-----------------------------------------------

        /// <summary>
        /// Name of this reference, preserved for debugging (may be null).
        /// </summary>
        public string DebugName
        {
            get { return _debugName; }
            set
            {
                if (value.Length > MaxDebugNameLength)
                    value = value.Substring(0, MaxDebugNameLength);

                _debugName = value;
            }
        }
    }
}
