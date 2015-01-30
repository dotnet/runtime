// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.CompilerServices
{
[Serializable]
[AttributeUsage(AttributeTargets.Enum)]
    public sealed class ScopelessEnumAttribute : Attribute
    {
        public ScopelessEnumAttribute()
        {}
    }
}
