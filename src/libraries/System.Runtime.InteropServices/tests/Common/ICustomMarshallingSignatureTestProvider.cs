// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop.UnitTests
{
    public interface ICustomMarshallingSignatureTestProvider
    {
        public abstract static string BasicParameterByValue(string type, string preDeclaration = "");

        public abstract static string BasicParameterWithByRefModifier(string byRefModifier, string type, string preDeclaration = "");

        public abstract static string BasicReturnType(string type, string preDeclaration = "");

        public abstract static string BasicParametersAndModifiers(string typeName, string preDeclaration = "");

        public abstract static string BasicParametersAndModifiersNoRef(string typeName, string preDeclaration = "");

        public abstract static string MarshalUsingParametersAndModifiers(string type, string marshallerType, string preDeclaration = "");

        public abstract static string MarshalUsingCollectionCountInfoParametersAndModifiers(string collectionType);

        public abstract static string MarshalUsingCollectionParametersAndModifiers(string type, string marshallerType);

        public abstract static string MarshalUsingCollectionOutConstantLength(string type, string predeclaration);

        public abstract static string MarshalUsingCollectionReturnConstantLength(string type, string predeclaration);

        public abstract static string MarshalUsingCollectionReturnValueLength(string type, string marshallerType);

        public abstract static string CustomElementMarshalling(string type, string marshallerType, string preDeclaration = "");
    }
}
