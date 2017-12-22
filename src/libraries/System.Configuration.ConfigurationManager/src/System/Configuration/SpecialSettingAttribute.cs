// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Configuration
{
    /// <summary>
    /// Indicates the SpecialSetting for a group of/individual settings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public sealed class SpecialSettingAttribute : Attribute
    {
        private readonly SpecialSetting _specialSetting;

        public SpecialSettingAttribute(SpecialSetting specialSetting)
        {
            _specialSetting = specialSetting;
        }

        /// <summary>
        /// SpecialSetting value to use
        /// </summary>
        public SpecialSetting SpecialSetting
        {
            get
            {
                return _specialSetting;
            }
        }
    }
}
