﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM
#pragma warning disable 612, 618

using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop {

    internal class DispatchArgBuilder : SimpleArgBuilder {
        private readonly bool _isWrapper;

        internal DispatchArgBuilder(Type parameterType)
            : base(parameterType) {

            _isWrapper = parameterType == typeof(DispatchWrapper);
        }

        internal override Expression Marshal(Expression parameter) {
            parameter = base.Marshal(parameter);

            // parameter.WrappedObject
            if (_isWrapper) {
                parameter = Expression.Property(
                    Helpers.Convert(parameter, typeof(DispatchWrapper)),
                    typeof(DispatchWrapper).GetProperty("WrappedObject")
                );
            }

            return Helpers.Convert(parameter, typeof(object));
        }

        internal override Expression MarshalToRef(Expression parameter) {
            parameter = Marshal(parameter);

            // parameter == null ? IntPtr.Zero : Marshal.GetIDispatchForObject(parameter);
            return Expression.Condition(
                Expression.Equal(parameter, Expression.Constant(null)),
                Expression.Constant(IntPtr.Zero),
                Expression.Call(
                    ComRuntimeHelpers.GetGetIDispatchForObjectMethod(),
                    parameter
                )
            );
        }

        internal override Expression UnmarshalFromRef(Expression value) {
            // value == IntPtr.Zero ? null : Marshal.GetObjectForIUnknown(value);
            Expression unmarshal = Expression.Condition(
                Expression.Equal(value, Expression.Constant(IntPtr.Zero)),
                Expression.Constant(null),
                Expression.Call(
                    typeof(Marshal).GetMethod("GetObjectForIUnknown"),
                    value
                )
            );

            if (_isWrapper) {
                unmarshal = Expression.New(
                    typeof(DispatchWrapper).GetConstructor(new Type[] { typeof(object) }),
                    unmarshal
                );
            }

            return base.UnmarshalFromRef(unmarshal);

        }
    }
}

#endif
