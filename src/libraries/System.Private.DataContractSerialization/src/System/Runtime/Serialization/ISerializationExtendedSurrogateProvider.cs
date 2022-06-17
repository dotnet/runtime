// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.CodeDom;
using System.Reflection;

namespace System.Runtime.Serialization
{
    public interface ISerializationExtendedSurrogateProvider : ISerializationSurrogateProvider
    {
        // From ISerializationSurrogateProvider
        //Type GetSurrogateType(Type type);
        //object GetObjectToSerialize(object obj, Type targetType);
        //object GetDeserializedObject(object obj, Type targetType);

        object? GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType);
        object? GetCustomDataToExport(Type clrType, Type dataContractType);
        void GetKnownCustomDataTypes(Collection<Type> customDataTypes);
        Type? GetReferencedTypeOnImport(string typeName, string typeNamespace, object? customData);
        CodeTypeDeclaration? ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit);
    }
}
