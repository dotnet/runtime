// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace JavaScript.MarshalerGenerator
{
    public class JSMarshalerSig
    {
        public ITypeSymbol MarshaledType;
        public ITypeSymbol MarshalerType;
        public string ToManagedMethod;
        public string ToJsMethod;
        public string AfterToJsMethod;
        public bool IsAuto;
        public bool NeedsCast;

        public override string ToString() => $"MarshaledType:{MarshaledType} MarshalerType:{MarshalerType} ToManagedMethod:{ToManagedMethod} ToJsMethod:{ToJsMethod} IsAuto:{IsAuto}";
    }
}
