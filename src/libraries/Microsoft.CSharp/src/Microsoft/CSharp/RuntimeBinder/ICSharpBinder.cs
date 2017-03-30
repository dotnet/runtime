using System;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal interface ICSharpBinder
    {
        CSharpArgumentInfo GetArgumentInfo(int index);
        Type CallingContext { get; }
        bool IsChecked { get; }

        // This is true for any binder that is eligible to take value type receiver 
        // objects as a ref (for mutable operations). Such as calls ("v.M(d)"),
        // and indexers ("v[d] = v[d]"). Note that properties are not here because they
        // are only dispatched dynamically when the receiver is dynamic, and hence boxed.
        bool IsBinderThatCanHaveRefReceiver { get; }

        void PopulateSymbolTableWithName(SymbolTable symbolTable, Type callingType, ArgumentObject[] arguments);

        Expr DispatchPayload(RuntimeBinder runtimeBinder, ArgumentObject[] arguments, LocalVariableSymbol[] locals);
        BindingFlag BindingFlags { get; }
        string Name { get; }
    }
}