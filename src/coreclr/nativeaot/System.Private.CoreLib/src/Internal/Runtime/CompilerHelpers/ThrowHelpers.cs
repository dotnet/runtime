// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

namespace Internal.Runtime.CompilerHelpers
{
    internal static partial class ThrowHelpers
    {
        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowBodyRemoved()
        {
            throw new NotSupportedException(SR.NotSupported_BodyRemoved);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowFeatureBodyRemoved()
        {
            throw new NotSupportedException(SR.NotSupported_FeatureBodyRemoved);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowInstanceBodyRemoved()
        {
            throw new NotSupportedException(SR.NotSupported_InstanceBodyRemoved);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowUnavailableType()
        {
            throw new TypeLoadException(SR.Arg_UnavailableTypeLoadException);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowBadImageFormatException(ExceptionStringID id)
        {
            throw TypeLoaderExceptionHelper.CreateBadImageFormatException(id);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowTypeLoadException(ExceptionStringID id, string className, string typeName)
        {
            throw TypeLoaderExceptionHelper.CreateTypeLoadException(id, className, typeName);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowTypeLoadExceptionWithArgument(ExceptionStringID id, string className, string typeName, string messageArg)
        {
            throw TypeLoaderExceptionHelper.CreateTypeLoadException(id, className, typeName, messageArg);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowMissingMethodException(ExceptionStringID id, string methodName)
        {
            throw TypeLoaderExceptionHelper.CreateMissingMethodException(id, methodName);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowMissingFieldException(ExceptionStringID id, string fieldName)
        {
            throw TypeLoaderExceptionHelper.CreateMissingFieldException(id, fieldName);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowFileNotFoundException(ExceptionStringID id, string fileName)
        {
            throw TypeLoaderExceptionHelper.CreateFileNotFoundException(id, fileName);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowInvalidProgramException(ExceptionStringID id)
        {
            throw TypeLoaderExceptionHelper.CreateInvalidProgramException(id);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowInvalidProgramExceptionWithArgument(ExceptionStringID id, string methodName)
        {
            throw TypeLoaderExceptionHelper.CreateInvalidProgramException(id, methodName);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowMarshalDirectiveException(ExceptionStringID id)
        {
            throw TypeLoaderExceptionHelper.CreateMarshalDirectiveException(id);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowAmbiguousMatchException(ExceptionStringID id)
        {
            throw TypeLoaderExceptionHelper.CreateAmbiguousMatchException(id);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowNotSupportedInlineArrayEqualsGetHashCode()
        {
            throw new NotSupportedException(SR.NotSupported_InlineArrayEqualsGetHashCode);
        }
    }
}
