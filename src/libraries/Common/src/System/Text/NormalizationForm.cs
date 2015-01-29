// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Text
{
    // This is the enumeration for Normalization Forms
#if INTERNAL_GLOBALIZATION_EXTENSIONS
    internal
#else
    [System.Runtime.InteropServices.ComVisible(true)]
    public
#endif
    enum NormalizationForm
    {
        FormC = 1,
        FormD = 2,
        FormKC = 5,
        FormKD = 6
    }
}
