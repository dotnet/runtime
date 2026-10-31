// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern signaturedependency {}
.assembly methodtokensignaturehelper {}
.module methodtokensignaturehelper.dll

.class public auto ansi MethodTokenMissingSignature extends [mscorlib]System.Object
{
    .method public static void MissingParameter(valuetype [signaturedependency]SignatureDependency.SignatureValue) cil managed
    {
        ret
    }

    .method public static valuetype [signaturedependency]SignatureDependency.SignatureValue MissingReturn() cil managed
    {
        ldnull
        throw
    }

    .method public static valuetype [mscorlib]System.RuntimeMethodHandle GetParameterMethodHandle() cil managed noinlining
    {
        ldtoken method void MethodTokenMissingSignature::MissingParameter(valuetype [signaturedependency]SignatureDependency.SignatureValue)
        ret
    }

    .method public static valuetype [mscorlib]System.RuntimeMethodHandle GetReturnMethodHandle() cil managed noinlining
    {
        ldtoken method valuetype [signaturedependency]SignatureDependency.SignatureValue MethodTokenMissingSignature::MissingReturn()
        ret
    }
}
