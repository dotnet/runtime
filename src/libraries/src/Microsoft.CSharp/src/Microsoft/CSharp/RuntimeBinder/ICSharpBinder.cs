using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal interface ICSharpBinder
    {
        DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args);
        Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel);
        DynamicMetaObject Defer(DynamicMetaObject target, params DynamicMetaObject[] args);
        DynamicMetaObject Defer(params DynamicMetaObject[] args);
        Expression GetUpdateExpression(Type type);
        Type ReturnType { get; }
        CSharpArgumentInfo GetArgumentInfo(int index);
        Type CallingContext { get; }
        bool IsChecked { get; }

        // This is true for any binder that is eligible to take value type receiver 
        // objects as a ref (for mutable operations). Such as calls ("v.M(d)"),
        // and indexers ("v[d] = v[d]"). Note that properties are not here because they
        // are only dispatched dynamically when the receiver is dynamic, and hence boxed.
        bool IsBinderThatCanHaveRefReceiver { get; }

        void PopulateSymbolTableWithName(SymbolTable symbolTable, Type callingType, ArgumentObject[] arguments);

        EXPR DispatchPayload(RuntimeBinder runtimeBinder, ArgumentObject[] arguments, Dictionary<int, LocalVariableSymbol> dictionary);
        BindingFlag BindingFlags { get; }
        string Name { get; }
    }
}