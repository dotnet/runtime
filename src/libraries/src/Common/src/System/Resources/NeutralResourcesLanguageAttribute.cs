// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Resources
{
    // Intentionally excluding visibility so it defaults to internal except for
    // the one public version in System.Resources.ResourceManager which defines
    // another version of this partial class with the public visibility 
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    sealed partial class NeutralResourcesLanguageAttribute : Attribute
    {
        private readonly string _culture;

        public NeutralResourcesLanguageAttribute(string cultureName)
        {
            if (cultureName == null)
                throw new ArgumentNullException("cultureName");

            _culture = cultureName;
        }

        public string CultureName
        {
            get { return _culture; }
        }
    }
}
