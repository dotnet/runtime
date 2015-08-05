// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Contains advanced properties for key creation.
    /// </summary>
    public sealed class CngKeyCreationParameters
    {
        public CngKeyCreationParameters()
        {
            Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider;
            Parameters = new CngPropertyCollection();
        }

        /// <summary>
        ///     How many times can this key be exported from the KSP
        /// </summary>
        public CngExportPolicies? ExportPolicy { get; set; }

        /// <summary>
        ///     Flags controlling how to create the key
        /// </summary>
        public CngKeyCreationOptions KeyCreationOptions { get; set; }

        /// <summary>
        ///     Which cryptographic operations are valid for use with this key
        /// </summary>
        public CngKeyUsages? KeyUsage { get; set; }

        /// <summary>
        ///     Extra parameter values to set before the key is finalized
        /// </summary>
        public CngPropertyCollection Parameters { get; private set; }
 
        /// <summary>
        ///     Window handle to use as the parent for the dialog shown when the key is created
        /// </summary>
        public IntPtr ParentWindowHandle { get; set; }

        /// <summary>
        ///     KSP to create the key in
        /// </summary>
        public CngProvider Provider
        {
            get
            {
                return _provider;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _provider = value;
            }
        }

        /// <summary>
        ///     Settings for UI shown on access to the key
        /// </summary>
        public CngUIPolicy UIPolicy { get; set; }

        private CngProvider _provider;
    }
}

