// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;
using System.Text;

namespace System
{
    static partial class StringNormalizationExtensions
    {
        [SecurityCritical]
        public static bool IsNormalized(this string value, NormalizationForm normalizationForm)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        [SecurityCritical]
        public static string Normalize(this string value, NormalizationForm normalizationForm)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }
    }
}

