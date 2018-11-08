// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.TypeLoading
{
    // Lazy-cached storage of group identification apis such as IsEnum and IsValueType
    internal abstract partial class RoType
    {
        //
        // Splitting these bits into two latches - those that requires acquiring the BaseType (which could trigger a FileNotFoundException)
        // and those that don't.
        //

        [Flags]
        private enum TypeClassification
        {
            Computed = 0x00000001,    // Always set (to indicate that the lazy evaluation has occurred)
            IsByRefLike = 0x00000004,
        }

        [Flags]
        private enum BaseTypeClassification 
        {
            Computed = 0x00000001,    // Always set (to indicate that the lazy evaluation has occurred)
            IsValueType = 0x00000002,
            IsEnum = 0x00000004,
        }

        //
        // Returns a latched set of flags indicating the value of IsValueType, IsEnum, etc.
        //
        private TypeClassification GetClassification() => (_lazyClassification != 0) ? _lazyClassification : (_lazyClassification = ComputeClassification());
        private TypeClassification ComputeClassification()
        {
            TypeClassification classification = TypeClassification.Computed;

            if (IsCustomAttributeDefined(Utf8Constants.SystemRuntimeCompilerServices, Utf8Constants.IsByRefLikeAttribute))
                classification |= TypeClassification.IsByRefLike;

            return classification;
        }
        private volatile TypeClassification _lazyClassification;

        private BaseTypeClassification GetBaseTypeClassification() => (_lazyBaseTypeClassification != 0) ? _lazyBaseTypeClassification : (_lazyBaseTypeClassification = ComputeBaseTypeClassification());
        private BaseTypeClassification ComputeBaseTypeClassification()
        {
            BaseTypeClassification classification = BaseTypeClassification.Computed;

            Type baseType = BaseType;
            if (baseType != null)
            {
                CoreTypes coreTypes = Loader.GetAllFoundCoreTypes();

                Type enumType = coreTypes[CoreType.Enum];
                Type valueType = coreTypes[CoreType.ValueType];

                if (baseType == enumType)
                    classification |= BaseTypeClassification.IsEnum | BaseTypeClassification.IsValueType;

                if (baseType == valueType && this != enumType)
                {
                    classification |= BaseTypeClassification.IsValueType;
                }
            }

            return classification;
        }
        private volatile BaseTypeClassification _lazyBaseTypeClassification;

        // Keep this separate from the other TypeClassification computations as it locks in the core assembly name.
        protected sealed override bool IsPrimitiveImpl()
        {
            CoreTypes coreTypes = Loader.GetAllFoundCoreTypes();
            foreach (CoreType primitiveType in s_primitiveTypes)
            {
                if (this == coreTypes[primitiveType])
                {
                    return true;
                }
            }
            return false;
        }

        // The exact set of types for which IsPrimitive is supposed to return true.
        private static readonly CoreType[] s_primitiveTypes = new CoreType[]
        {
            CoreType.Boolean,
            CoreType.Char,
            CoreType.SByte,
            CoreType.Byte,
            CoreType.Int16,
            CoreType.UInt16,
            CoreType.Int32,
            CoreType.UInt32,
            CoreType.Int64,
            CoreType.UInt64,
            CoreType.Single,
            CoreType.Double,
            CoreType.IntPtr,
            CoreType.UIntPtr,
        };
    }
}
