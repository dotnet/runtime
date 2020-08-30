// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILVerify
{
    internal static class TypeSystemHelpers
    {
        /// <summary>
        /// Returns the "reduced type" based on the definition in the ECMA-335 standard (I.8.7).
        /// </summary>
        internal static TypeDesc GetReducedType(this TypeDesc type)
        {
            if (type == null)
                return null;

            var category = type.UnderlyingType.Category;

            switch (category)
            {
                case TypeFlags.Byte:
                    return type.Context.GetWellKnownType(WellKnownType.SByte);
                case TypeFlags.UInt16:
                    return type.Context.GetWellKnownType(WellKnownType.Int16);
                case TypeFlags.UInt32:
                    return type.Context.GetWellKnownType(WellKnownType.Int32);
                case TypeFlags.UInt64:
                    return type.Context.GetWellKnownType(WellKnownType.Int64);
                case TypeFlags.UIntPtr:
                    return type.Context.GetWellKnownType(WellKnownType.IntPtr);

                default:
                    return type.UnderlyingType; //Reduced type is type itself
            }
        }

        /// <summary>
        /// Returns the "verification type" based on the definition in the ECMA-335 standard (I.8.7).
        /// </summary>
        internal static TypeDesc GetVerificationType(this TypeDesc type)
        {
            if (type == null)
                return null;

            if (type.IsByRef)
            {
                var parameterVerificationType = GetVerificationType(type.GetParameterType());
                return type.Context.GetByRefType(parameterVerificationType);
            }
            else
            {
                var reducedType = GetReducedType(type);
                switch (reducedType.Category)
                {
                    case TypeFlags.Boolean:
                        return type.Context.GetWellKnownType(WellKnownType.SByte);

                    case TypeFlags.Char:
                        return type.Context.GetWellKnownType(WellKnownType.Int16);

                    default:
                        return reducedType; // Verification type is reduced type
                }
            }
        }

        /// <summary>
        /// Returns the "intermediate type" based on the definition in the ECMA-335 standard (I.8.7).
        /// </summary>
        internal static TypeDesc GetIntermediateType(this TypeDesc type)
        {
            var verificationType = GetVerificationType(type);

            if (verificationType == null)
                return null;

            switch (verificationType.Category)
            {
                case TypeFlags.SByte:
                case TypeFlags.Int16:
                case TypeFlags.Int32:
                    return type.Context.GetWellKnownType(WellKnownType.Int32);
                case TypeFlags.Single:
                case TypeFlags.Double:
                    return type.Context.GetWellKnownType(WellKnownType.Double);
                default:
                    return verificationType;
            }
        }
    }
}
