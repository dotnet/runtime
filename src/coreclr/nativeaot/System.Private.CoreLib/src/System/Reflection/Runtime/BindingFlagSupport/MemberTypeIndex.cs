// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Runtime.BindingFlagSupport
{
    //
    // When we want to store an object of type Foo<M> for each possible M, it's convenient to use
    // an array of length MemberTypeIndex.Count, which each possible M assigned an index.
    //
    // This is defined as a set of consts rather than enum to avoid having to cast to int.
    //
    internal static class MemberTypeIndex
    {
        public const int Constructor = 0;
        public const int Event = 1;
        public const int Field = 2;
        public const int Method = 3;
        public const int NestedType = 4;
        public const int Property = 5;

        public const int Count = 6;
    }
}
