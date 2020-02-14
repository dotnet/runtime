// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM
using System.Linq.Expressions;

using System;
using System.Diagnostics;

namespace Microsoft.Scripting.ComInterop {
    /// <summary>
    /// SimpleArgBuilder produces the value produced by the user as the argument value.  It
    /// also tracks information about the original parameter and is used to create extended
    /// methods for params arrays and param dictionary functions.
    /// </summary>
    internal class SimpleArgBuilder : ArgBuilder {
        private readonly Type _parameterType;

        internal SimpleArgBuilder(Type parameterType) {
            _parameterType = parameterType;
        }

        internal Type ParameterType {
            get { return _parameterType; }
        }

        internal override Expression Marshal(Expression parameter) {
            Debug.Assert(parameter != null);
            return Helpers.Convert(parameter, _parameterType);
        }

        internal override Expression UnmarshalFromRef(Expression newValue) {
            Debug.Assert(newValue != null && newValue.Type.IsAssignableFrom(_parameterType));

            return base.UnmarshalFromRef(newValue);
        }
    }
}

#endif
