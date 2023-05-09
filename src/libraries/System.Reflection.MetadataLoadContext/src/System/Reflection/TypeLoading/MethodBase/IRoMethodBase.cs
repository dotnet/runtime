// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.TypeLoading
{
    // Implemented by RoMethod and RoConstructor. Because it's impossible for those two types to have a common base type we control,
    // we use this interface when we want to talk about them collectively.
    internal interface IRoMethodBase
    {
        MethodBase MethodBase { get; }
        MetadataLoadContext Loader { get; }
        TypeContext TypeContext { get; }
        string GetMethodSigString(int position);
    }
}
