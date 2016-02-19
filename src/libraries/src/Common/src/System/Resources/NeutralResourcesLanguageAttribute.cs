// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                throw new ArgumentNullException(nameof(cultureName));

            _culture = cultureName;
        }

        public string CultureName
        {
            get { return _culture; }
        }
    }
}
