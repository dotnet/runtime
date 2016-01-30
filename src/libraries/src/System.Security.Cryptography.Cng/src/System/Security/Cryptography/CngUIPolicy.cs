// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Configuration parameters for the UI displayed by CNG when accessing a protected key
    /// </summary>
    public sealed class CngUIPolicy
    {
        public CngUIPolicy(CngUIProtectionLevels protectionLevel)
            : this(protectionLevel, friendlyName: null)
        {
        }

        public CngUIPolicy(CngUIProtectionLevels protectionLevel, string friendlyName)
            : this(protectionLevel, friendlyName, description: null)
        {
        }

        public CngUIPolicy(CngUIProtectionLevels protectionLevel, string friendlyName, string description)
            : this(protectionLevel, friendlyName, description, useContext: null)
        {
        }

        public CngUIPolicy(CngUIProtectionLevels protectionLevel, string friendlyName, string description, string useContext)
            : this(protectionLevel, friendlyName, description, useContext, creationTitle: null)
        {
        }

        public CngUIPolicy(CngUIProtectionLevels protectionLevel, string friendlyName, string description, string useContext, string creationTitle)
        {
            ProtectionLevel = protectionLevel;
            FriendlyName = friendlyName;
            Description = description;
            UseContext = useContext;
            CreationTitle = creationTitle;
        }

        /// <summary>
        ///     Level of UI protection to apply to the key
        /// </summary>
        public CngUIProtectionLevels ProtectionLevel { get; private set; }

        /// <summary>
        ///     Friendly name to describe the key with in the dialog box that appears when the key is accessed,
        ///     null for default name
        /// </summary>
        public string FriendlyName { get; private set; }

        /// <summary>
        ///     Description text displayed in the dialog box when the key is accessed, null for the default text
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        ///     Description of how the key will be used
        /// </summary>
        public string UseContext { get; private set; }

        /// <summary>
        ///     Title of the dialog box displayed when a newly created key is finalized, null for the default title
        /// </summary>
        public string CreationTitle { get; private set; }
    }
}

