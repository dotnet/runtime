// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base type for RoDefinitionType and RoConstructedGenericType. These are the two types that can declare members backed by metadata.
    /// (Though Array types "declare" members too, those are not backed by actual metadata so there will never be a typespec that has to be resolved
    /// which is what an instantiation is for in the first place.)
    /// </summary>
    internal abstract partial class RoInstantiationProviderType : RoType
    {
        protected RoInstantiationProviderType()
            : base()
        {
        }

        internal abstract RoType[] Instantiation { get; }
    }
}
