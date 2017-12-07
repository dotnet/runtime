// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Primitives
{
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class ExportedDelegate
    {
        private object _instance;
        private MethodInfo _method;

        protected ExportedDelegate() { }

        public ExportedDelegate(object instance, MethodInfo method)
        {
            Requires.NotNull(method, nameof(method));

            _instance = instance;
            _method = method;
        }

        public virtual Delegate CreateDelegate(Type delegateType) 
        {
            Requires.NotNull(delegateType, nameof(delegateType));

            if (delegateType == typeof(Delegate) || delegateType == typeof(MulticastDelegate))
            {
                delegateType = CreateStandardDelegateType();
            }
            try
            { 
                return _method.CreateDelegate(delegateType, _instance);
            }
            catch(ArgumentException)
            {
                //Bind failure occurs return null;
                return null;
            }
        }

        private Type CreateStandardDelegateType()
        {
            ParameterInfo[] parameters = _method.GetParameters();

            // This array should contains a lit of all argument types, and the last one is the return type (could be void)
            Type[] parameterTypes = new Type[parameters.Length + 1];
            parameterTypes[parameters.Length] = _method.ReturnType;
            for (int i = 0; i < parameters.Length; i++ )
            {
                parameterTypes[i] = parameters[i].ParameterType;
            }

            return Expression.GetDelegateType(parameterTypes);
        }
    }
}
