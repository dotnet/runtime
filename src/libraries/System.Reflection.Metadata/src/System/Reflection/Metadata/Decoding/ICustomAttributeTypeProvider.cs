// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if SRM
namespace System.Reflection.Metadata.Decoding
#else
namespace Roslyn.Reflection.Metadata.Decoding
#endif
{
#if SRM && FUTURE
    public
#endif
    interface ICustomAttributeTypeProvider<TType> : IPrimitiveTypeProvider<TType>, ISZArrayTypeProvider<TType>, ITypeProvider<TType>
    {
        /// <summary>
        /// Gets the TType representation for <see cref="System.Type"/>.
        /// </summary>
        TType GetSystemType();

        /// <summary>
        /// Returns true if the given type represents <see cref="System.Type"/>.
        /// </summary>
        bool IsSystemType(TType type);

        /// <summary>
        /// Get the type symbol for the given serialized type name.
        /// The serialized type name is in so-called "reflection notation" (i.e. as understood by <see cref="Type.GetType(string)"/>.)
        /// </summary>
        /// <exception cref="BadImageFormatException">The name is malformed.</exception>
        TType GetTypeFromSerializedName(string name);

        /// <summary>
        /// Gets the underlying type of the given enum type symbol.
        /// </summary>
        /// <exception cref="BadImageFormatException">The given type symbol does not represent an enum.</exception>
        PrimitiveTypeCode GetUnderlyingEnumType(TType type);
    }
}
