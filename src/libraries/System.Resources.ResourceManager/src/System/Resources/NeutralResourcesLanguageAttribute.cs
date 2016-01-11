// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                throw new ArgumentNullException("cultureName");
            }

            _culture = cultureName;
        }

        public string CultureName
        {
            get { return _culture; }
        }
    }
}
