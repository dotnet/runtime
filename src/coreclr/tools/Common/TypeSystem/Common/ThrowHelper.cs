// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public static partial class ThrowHelper
    {
        [System.Diagnostics.DebuggerHidden]
        private static void ThrowTypeLoadException(ExceptionStringID id, string typeName, string assemblyName, string messageArg)
        {
            throw new TypeSystemException.TypeLoadException(id, typeName, assemblyName, messageArg);
        }

        [System.Diagnostics.DebuggerHidden]
        private static void ThrowTypeLoadException(ExceptionStringID id, string typeName, string assemblyName)
        {
            throw new TypeSystemException.TypeLoadException(id, typeName, assemblyName);
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowMissingMethodException(TypeDesc owningType, string methodName, MethodSignature signature)
        {
            throw new TypeSystemException.MissingMethodException(ExceptionStringID.MissingMethod, Format.Method(owningType, methodName, signature));
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowMissingFieldException(TypeDesc owningType, string fieldName)
        {
            throw new TypeSystemException.MissingFieldException(ExceptionStringID.MissingField, Format.Field(owningType, fieldName));
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowFileNotFoundException(ExceptionStringID id, string fileName)
        {
            throw new TypeSystemException.FileNotFoundException(id, fileName);
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowInvalidProgramException()
        {
            throw new TypeSystemException.InvalidProgramException();
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowInvalidProgramException(ExceptionStringID id)
        {
            throw new TypeSystemException.InvalidProgramException(id);
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowInvalidProgramException(ExceptionStringID id, MethodDesc method)
        {
            throw new TypeSystemException.InvalidProgramException(id, Format.Method(method));
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowBadImageFormatException()
        {
            throw new TypeSystemException.BadImageFormatException();
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowBadImageFormatException(string message)
        {
            throw new TypeSystemException.BadImageFormatException(message);
        }

        [System.Diagnostics.DebuggerHidden]
        public static void ThrowMarshalDirectiveException()
        {
            throw new TypeSystemException.MarshalDirectiveException(ExceptionStringID.MarshalDirectiveGeneric);
        }

        private static partial class Format
        {
            public static string OwningModule(TypeDesc type)
            {
                return Module((type as MetadataType)?.Module);
            }
        }
    }
}
