// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop.UnitTests
{
    public interface ICustomMarshallingSignatureTestProviderStatic
    {
        public static abstract string BasicParameterByValue(string type, string preDeclaration = "");

        public static abstract string BasicParameterWithByRefModifier(string byRefModifier, string type, string preDeclaration = "");

        public static abstract string BasicReturnType(string type, string preDeclaration = "");

        public static abstract string BasicParametersAndModifiers(string typeName, string preDeclaration = "");

        public static abstract string BasicParametersAndModifiersNoRef(string typeName, string preDeclaration = "");

        public static abstract string MarshalUsingParametersAndModifiers(string type, string marshallerType, string preDeclaration = "");

        public static abstract string MarshalUsingCollectionCountInfoParametersAndModifiers(string collectionType);

        public static abstract string MarshalUsingCollectionParametersAndModifiers(string type, string marshallerType);

        public static abstract string MarshalUsingCollectionOutConstantLength(string type, string predeclaration);

        public static abstract string MarshalUsingCollectionReturnConstantLength(string type, string predeclaration);

        public static abstract string MarshalUsingCollectionReturnValueLength(string type, string marshallerType);

        public static abstract string CustomElementMarshalling(string type, string marshallerType, string preDeclaration = "");
    }

    public interface ICustomMarshallingSignatureTestProvider
    {
        public string BasicParameterByValue(string type, string preDeclaration = "");

        public string BasicParameterWithByRefModifier(string byRefModifier, string type, string preDeclaration = "");

        public string BasicReturnType(string type, string preDeclaration = "");

        public string BasicParametersAndModifiers(string typeName, string preDeclaration = "");

        public string BasicParametersAndModifiersNoRef(string typeName, string preDeclaration = "");

        public string MarshalUsingParametersAndModifiers(string type, string marshallerType, string preDeclaration = "");

        public string MarshalUsingCollectionCountInfoParametersAndModifiers(string collectionType);

        public string MarshalUsingCollectionParametersAndModifiers(string type, string marshallerType);

        public string MarshalUsingCollectionOutConstantLength(string type, string predeclaration);

        public string MarshalUsingCollectionReturnConstantLength(string type, string predeclaration);

        public string MarshalUsingCollectionReturnValueLength(string type, string marshallerType);

        public string CustomElementMarshalling(string type, string marshallerType, string preDeclaration = "");
    }
}
