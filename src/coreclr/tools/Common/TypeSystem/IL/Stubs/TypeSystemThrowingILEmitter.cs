// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Helper class to generate method bodies that throw exceptions for methods that can't be compiled
    /// for various reasons. The exception thrown is same (or similar) to the one an attempt to JIT-compile
    /// (or interpret) the method on a just-in-time compiled runtime would generate.
    /// </summary>
    internal static class TypeSystemThrowingILEmitter
    {
        public static MethodIL EmitIL(MethodDesc methodThatShouldThrow, TypeSystemException exception)
        {
            TypeSystemContext context = methodThatShouldThrow.Context;

            MethodDesc helper;

            Type exceptionType = exception.GetType();
            if (exceptionType == typeof(TypeSystemException.TypeLoadException))
            {
                //
                // There are two ThrowTypeLoadException helpers. Find the one which matches the number of
                // arguments "exception" was initialized with.
                //
                helper = context.GetHelperEntryPoint("ThrowHelpers", "ThrowTypeLoadException");

                if (helper.Signature.Length != exception.Arguments.Count + 1)
                {
                    helper = context.GetHelperEntryPoint("ThrowHelpers", "ThrowTypeLoadExceptionWithArgument");
                }
            }
            else if (exceptionType == typeof(TypeSystemException.MissingFieldException))
            {
                helper = context.GetHelperEntryPoint("ThrowHelpers", "ThrowMissingFieldException");
            }
            else if (exceptionType == typeof(TypeSystemException.MissingMethodException))
            {
                helper = context.GetHelperEntryPoint("ThrowHelpers", "ThrowMissingMethodException");
            }
            else if (exceptionType == typeof(TypeSystemException.FileNotFoundException))
            {
                helper = context.GetHelperEntryPoint("ThrowHelpers", "ThrowFileNotFoundException");
            }
            else if (exceptionType == typeof(TypeSystemException.InvalidProgramException))
            {
                //
                // There are two ThrowInvalidProgramException helpers. Find the one which matches the number of
                // arguments "exception" was initialized with.
                //

                helper = context.GetHelperEntryPoint("ThrowHelpers", "ThrowInvalidProgramException");

                if (helper.Signature.Length != exception.Arguments.Count + 1)
                {
                    helper = context.GetHelperEntryPoint("ThrowHelpers", "ThrowInvalidProgramExceptionWithArgument");
                }
            }
            else if (exceptionType == typeof(TypeSystemException.BadImageFormatException))
            {
                helper = context.GetHelperEntryPoint("ThrowHelpers", "ThrowBadImageFormatException");
            }
            else if (exceptionType == typeof(TypeSystemException.MarshalDirectiveException))
            {
                helper = context.GetHelperEntryPoint("ThrowHelpers", "ThrowMarshalDirectiveException");
            }
            else
            {
                throw new NotImplementedException();
            }

            Debug.Assert(helper.Signature.Length == exception.Arguments.Count + 1);

            var emitter = new ILEmitter();
            var codeStream = emitter.NewCodeStream();

            var infinityLabel = emitter.NewCodeLabel();
            codeStream.EmitLabel(infinityLabel);

            codeStream.EmitLdc((int)exception.StringID);

            foreach (var arg in exception.Arguments)
            {
                codeStream.Emit(ILOpcode.ldstr, emitter.NewToken(arg));
            }

            codeStream.Emit(ILOpcode.call, emitter.NewToken(helper));

            // The call will never return, but we still need to emit something. Emit a jump so that
            // we don't have to bother balancing the stack if the method returns something.
            codeStream.Emit(ILOpcode.br, infinityLabel);

            return emitter.Link(methodThatShouldThrow);
        }
    }
}
