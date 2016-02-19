// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Resources
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class NeutralResourcesLanguageAttribute : Attribute
    {
        private readonly string _culture;

        public NeutralResourcesLanguageAttribute(string cultureName)
        {
            if (cultureName == null)
            {
                throw new ArgumentNullException(nameof(cultureName));
            }

            _culture = cultureName;
        }

        public string CultureName
        {
            get { return _culture; }
        }
    }
}
