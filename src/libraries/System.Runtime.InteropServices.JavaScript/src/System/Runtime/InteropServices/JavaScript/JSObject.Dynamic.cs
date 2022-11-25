// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic;
using System.Linq.Expressions;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JSObject : IDynamicMetaObjectProvider
    {
        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new MetaDynamic(parameter, this);
        }

        private sealed class MetaDynamic : DynamicMetaObject
        {
            private JSObject proxy;

            internal MetaDynamic(Expression expression, JSObject value)
                : base(expression, BindingRestrictions.GetTypeRestriction(expression, typeof(JSObject)), value)
            {
                proxy = value;
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var typeOf = proxy.GetTypeOfProperty(binder.Name);
                switch (typeOf)
                {
                    case "undefined":
                    case "null":
                        return new DynamicMetaObject(Expression.Constant(null), Restrictions);
                    case "string":
                        var str = proxy.GetPropertyAsString(binder.Name);
                        return new DynamicMetaObject(Expression.Constant(str), Restrictions);
                    case "number":
                        var num = proxy.GetPropertyAsDouble(binder.Name);
                        return new DynamicMetaObject(Expression.Constant(num), Restrictions);
                    case "object":
                        var obj = proxy.GetPropertyAsJSObject(binder.Name)!;
                        return new MetaDynamic(Expression.Constant(obj), obj);
                    case "function":
                    default:
                        throw new NotImplementedException(typeOf);
                }
            }
        }
    }
}
