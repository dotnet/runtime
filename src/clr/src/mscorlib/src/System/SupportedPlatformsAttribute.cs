// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Purpose: Annotates items as existing only on certain
**          platforms.
**
**
===========================================================*/
#if FEATURE_CORECLR
using System.Diagnostics.Contracts;

namespace System
{
    [Serializable]
    [AttributeUsage(AttributeTargets.All, Inherited=true)]
    public sealed class SupportedPlatformsAttribute : Attribute
    {
        internal Platforms m_platforms = Platforms.All;

        internal static SupportedPlatformsAttribute Default = new SupportedPlatformsAttribute(Platforms.All);

        public SupportedPlatformsAttribute(Platforms platforms)
        {
            if ((platforms & ~Platforms.All) != 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "platforms");
            Contract.EndContractBlock();
            m_platforms = platforms;
        }

        public Platforms Platforms 
        {
            get { return m_platforms; }
        }
    }

}
#endif // FEATURE_CORECLR