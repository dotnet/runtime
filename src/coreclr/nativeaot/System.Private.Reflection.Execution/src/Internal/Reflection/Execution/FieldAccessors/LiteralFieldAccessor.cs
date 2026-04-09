// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Internal.Reflection.Execution.FieldAccessors
{
    internal sealed class LiteralFieldAccessor : StaticFieldAccessor
    {
        public LiteralFieldAccessor(object value, RuntimeTypeHandle fieldTypeHandle)
            : base(IntPtr.Zero, fieldTypeHandle)
        {
            _value = value;
        }

        protected sealed override object GetFieldBypassCctor() => _value;

        protected sealed override void SetFieldBypassCctor(object value, BinderBundle binderBundle)
        {
            throw new FieldAccessException(SR.Acc_ReadOnly);
        }

        protected sealed override void SetFieldDirectBypassCctor(object value)
        {
            throw new FieldAccessException(SR.Acc_ReadOnly);
        }

        private readonly object _value;
    }
}
