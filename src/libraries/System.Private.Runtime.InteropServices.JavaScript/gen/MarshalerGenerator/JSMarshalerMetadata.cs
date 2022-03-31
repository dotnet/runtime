// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace JavaScript.MarshalerGenerator
{
    public class JSMarshalerMetadata
    {
        public ITypeSymbol MarshaledType;
        public ITypeSymbol MarshalerType;
        public string ToManagedMethod;
        public string ToJsMethod;
        public string AfterToJsMethod;
        public bool IsAuto;

        public bool IsExactMatch(ITypeSymbol other)
        {
            Debug.Assert(MarshaledType != null);
            return SymbolEqualityComparer.Default.Equals(MarshaledType, other);
        }

        public bool IsAssignableFrom(Compilation compilation, ITypeSymbol argType)
        {
            Debug.Assert(compilation != null);
            Debug.Assert(argType != null);
            Debug.Assert(MarshaledType != null);
            // TODO what about VB ?
            return ((CSharpCompilation)compilation).ClassifyConversion(argType, MarshaledType).IsImplicit;
        }

        public JSMarshalerSig ToSignature(bool needsCast)
        {
            var sig = new JSMarshalerSig
            {
                MarshaledType = MarshaledType,
                MarshalerType = MarshalerType,
                ToManagedMethod = ToManagedMethod,
                ToJsMethod = ToJsMethod,
                AfterToJsMethod = AfterToJsMethod,
                IsAuto = IsAuto,
                NeedsCast = needsCast,
            };

            return sig;
        }

        public override string ToString() => $"MarshaledType:{MarshaledType} MarshalerType:{MarshalerType} ToManagedMethod:{ToManagedMethod} ToJsMethod:{ToJsMethod} IsAuto:{IsAuto}";
    }
}
