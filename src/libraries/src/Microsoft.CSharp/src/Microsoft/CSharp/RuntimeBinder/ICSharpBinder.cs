using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq.Expressions;

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
    }
}