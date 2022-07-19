// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Resources
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class NeutralResourcesLanguageAttribute : Attribute
    {
        public NeutralResourcesLanguageAttribute(string cultureName)
        {
            ArgumentNullException.ThrowIfNull(cultureName);

            CultureName = cultureName;
            Location = UltimateResourceFallbackLocation.MainAssembly;
        }

        public NeutralResourcesLanguageAttribute(string cultureName, UltimateResourceFallbackLocation location)
        {
            ArgumentNullException.ThrowIfNull(cultureName);

            if (!Enum.IsDefined(typeof(UltimateResourceFallbackLocation), location))
                throw new ArgumentException(SR.Format(SR.Arg_InvalidNeutralResourcesLanguage_FallbackLoc, location));

            CultureName = cultureName;
            Location = location;
        }

        public string CultureName { get; }
        public UltimateResourceFallbackLocation Location { get; }
    }
}
