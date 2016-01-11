// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;

namespace System.Resources
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class NeutralResourcesLanguageAttribute : Attribute
    {
        private String _culture;

        public NeutralResourcesLanguageAttribute(String cultureName)
        {
            if (cultureName == null)
                throw new ArgumentNullException("cultureName");
            Contract.EndContractBlock();

            _culture = cultureName;
        }

        public String CultureName
        {
            get { return _culture; }
        }
    }
}
