// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM
using System.Linq.Expressions;

using System;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop {

    internal class ConversionArgBuilder : ArgBuilder {
        private readonly SimpleArgBuilder _innerBuilder;
        private readonly Type _parameterType;

        internal ConversionArgBuilder(Type parameterType, SimpleArgBuilder innerBuilder) {
            _parameterType = parameterType;
            _innerBuilder = innerBuilder;
        }

        internal override Expression Marshal(Expression parameter) {
            return _innerBuilder.Marshal(Helpers.Convert(parameter, _parameterType));
        }

        internal override Expression MarshalToRef(Expression parameter) {
            //we are not supporting conversion InOut
            throw Assert.Unreachable;
        }
    }
}

#endif
