// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization
{
    using System;
#if smolloy_add_ext_surrogate
#if smolloy_codedom_stubbed
    using System.CodeDom.Stubs;
#elif smolloy_codedom_full_internalish
    using System.Runtime.Serialization.CodeDom;
#endif
#endif
    using System.Reflection;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;

    internal static class DataContractSurrogateCaller
    {
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static Type GetDataContractType(ISerializationSurrogateProvider surrogateProvider, Type type)
        {
            if (DataContract.GetBuiltInDataContract(type) != null)
                return type;
            return surrogateProvider.GetSurrogateType(type) ?? type;
        }

        [return: NotNullIfNotNull("obj")]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static object? GetObjectToSerialize(ISerializationSurrogateProvider surrogateProvider, object? obj, Type objType, Type membertype)
        {
            if (obj == null)
                return null;
            if (DataContract.GetBuiltInDataContract(objType) != null)
                return obj;
            return surrogateProvider.GetObjectToSerialize(obj, membertype);
        }

        [return: NotNullIfNotNull("obj")]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static object? GetDeserializedObject(ISerializationSurrogateProvider surrogateProvider, object? obj, Type objType, Type memberType)
        {
            if (obj == null)
                return null;
            if (DataContract.GetBuiltInDataContract(objType) != null)
                return obj;
            return surrogateProvider.GetDeserializedObject(obj, memberType);
        }

#if smolloy_add_ext_surrogate
        internal static object? GetCustomDataToExport(ISerializationExtendedSurrogateProvider surrogateProvider, MemberInfo memberInfo, Type dataContractType)
        {
            return surrogateProvider.GetCustomDataToExport(memberInfo, dataContractType);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static object? GetCustomDataToExport(ISerializationExtendedSurrogateProvider surrogateProvider, Type clrType, Type dataContractType)
        {
            if (DataContract.GetBuiltInDataContract(clrType) != null)
                return null;
            return surrogateProvider.GetCustomDataToExport(clrType, dataContractType);
        }

        internal static void GetKnownCustomDataTypes(ISerializationExtendedSurrogateProvider surrogate, Collection<Type> customDataTypes)
        {
            surrogate.GetKnownCustomDataTypes(customDataTypes);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static Type? GetReferencedTypeOnImport(ISerializationExtendedSurrogateProvider surrogateProvider, string typeName, string typeNamespace, object? customData)
        {
            if (DataContract.GetBuiltInDataContract(typeName, typeNamespace) != null)
                return null;
            return surrogateProvider.GetReferencedTypeOnImport(typeName, typeNamespace, customData);
        }

        internal static CodeTypeDeclaration? ProcessImportedType(ISerializationExtendedSurrogateProvider surrogateProvider, CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit)
        {
            return surrogateProvider.ProcessImportedType(typeDeclaration, compileUnit);
        }
#endif
    }
}
