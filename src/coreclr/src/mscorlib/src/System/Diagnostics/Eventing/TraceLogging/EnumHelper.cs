// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Reflection;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// Provides support for casting enums to their underlying type
    /// from within generic context.
    /// </summary>
    /// <typeparam name="UnderlyingType">
    /// The underlying type of the enum.
    /// </typeparam>
    internal static class EnumHelper<UnderlyingType>
    {
        private delegate UnderlyingType Transformer<ValueType>(ValueType value);

        private static readonly MethodInfo IdentityInfo =
            Statics.GetDeclaredStaticMethod(typeof(EnumHelper<UnderlyingType>), "Identity");

        public static UnderlyingType Cast<ValueType>(ValueType value)
        {
            return Caster<ValueType>.Instance(value);
        }

        internal static UnderlyingType Identity(UnderlyingType value)
        {
            return value;
        }

        private static class Caster<ValueType>
        {
            public static readonly Transformer<ValueType> Instance =
                (Transformer<ValueType>)Statics.CreateDelegate(
                typeof(Transformer<ValueType>),
                IdentityInfo);
        }
    }
}
