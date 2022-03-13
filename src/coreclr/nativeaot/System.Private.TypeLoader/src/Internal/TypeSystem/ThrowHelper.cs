// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CoreLibThrow = Internal.Runtime.CompilerHelpers.ThrowHelpers;

namespace Internal.TypeSystem
{
    // This implementation forwards to the throw helpers targeted by the compiler in CoreLib.
    // That way we can share the exception string resources.
    public static partial class ThrowHelper
    {
        private static void ThrowTypeLoadException(ExceptionStringID id, string typeName, string assemblyName, string messageArg)
        {
            CoreLibThrow.ThrowTypeLoadExceptionWithArgument(id, typeName, assemblyName, messageArg);
        }

        private static void ThrowTypeLoadException(ExceptionStringID id, string typeName, string assemblyName)
        {
            CoreLibThrow.ThrowTypeLoadException(id, typeName, assemblyName);
        }

        public static void ThrowMissingMethodException(TypeDesc owningType, string methodName, MethodSignature signature)
        {
            CoreLibThrow.ThrowMissingMethodException(ExceptionStringID.MissingMethod, Format.Method(owningType, methodName, signature));
        }

        public static void ThrowMissingFieldException(TypeDesc owningType, string fieldName)
        {
            CoreLibThrow.ThrowMissingFieldException(ExceptionStringID.MissingField, Format.Field(owningType, fieldName));
        }

        public static void ThrowFileNotFoundException(ExceptionStringID id, string fileName)
        {
            CoreLibThrow.ThrowFileNotFoundException(id, fileName);
        }

        public static void ThrowInvalidProgramException()
        {
            CoreLibThrow.ThrowInvalidProgramException(ExceptionStringID.InvalidProgramDefault);
        }

        public static void ThrowInvalidProgramException(ExceptionStringID id, MethodDesc method)
        {
            CoreLibThrow.ThrowInvalidProgramExceptionWithArgument(id, Format.Method(method));
        }

        public static void ThrowBadImageFormatException()
        {
            CoreLibThrow.ThrowBadImageFormatException(ExceptionStringID.BadImageFormatGeneric);
        }

        private static partial class Format
        {
            public static string OwningModule(TypeDesc type)
            {
                if (type is NoMetadata.NoMetadataType)
                    return ((NoMetadata.NoMetadataType)type).DiagnosticModuleName;

                return Module((type as MetadataType)?.Module);
            }
        }
    }
}
