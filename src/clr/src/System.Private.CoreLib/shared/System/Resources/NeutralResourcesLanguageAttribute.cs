// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Resources
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class NeutralResourcesLanguageAttribute : Attribute
    {
        public NeutralResourcesLanguageAttribute(string cultureName)
        {
            if (cultureName == null)
                throw new ArgumentNullException(nameof(cultureName));

            CultureName = cultureName;
            Location = UltimateResourceFallbackLocation.MainAssembly;
        }

        public NeutralResourcesLanguageAttribute(string cultureName, UltimateResourceFallbackLocation location)
        {
            if (cultureName == null)
                throw new ArgumentNullException(nameof(cultureName));
            if (!Enum.IsDefined(typeof(UltimateResourceFallbackLocation), location))
                throw new ArgumentException(SR.Format(SR.Arg_InvalidNeutralResourcesLanguage_FallbackLoc, location));

            CultureName = cultureName;
            Location = location;
        }

        public string CultureName { get; }
        public UltimateResourceFallbackLocation Location { get; }
    }
}
