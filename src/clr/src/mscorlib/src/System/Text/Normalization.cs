// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    // This is the enumeration for Normalization Forms
[System.Runtime.InteropServices.ComVisible(true)]
    public enum NormalizationForm
    {
        FormC    = 1,
        FormD    = 2,
        FormKC   = 5,
        FormKD   = 6
    }

    internal enum ExtendedNormalizationForms
    {
        FormC    = 1,
        FormD    = 2,
        FormKC   = 5,
        FormKD   = 6,
        FormIdna = 0xd,
        FormCDisallowUnassigned     = 0x101,
        FormDDisallowUnassigned     = 0x102,
        FormKCDisallowUnassigned    = 0x105,
        FormKDDisallowUnassigned    = 0x106,
        FormIdnaDisallowUnassigned  = 0x10d
    }
}
