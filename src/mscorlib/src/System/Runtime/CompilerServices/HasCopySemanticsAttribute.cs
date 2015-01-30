// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.CompilerServices
{
[Serializable]
[AttributeUsage(AttributeTargets.Struct)]
    public sealed class HasCopySemanticsAttribute : Attribute
    {
        public HasCopySemanticsAttribute()
        {}
    }
}
