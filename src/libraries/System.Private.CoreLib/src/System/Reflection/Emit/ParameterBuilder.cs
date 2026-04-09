// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Reflection.Emit
{
    public abstract partial class ParameterBuilder
    {
        protected ParameterBuilder() { }
        public virtual int Attributes => throw new NotImplementedException();
        public bool IsIn => ((ParameterAttributes)Attributes & ParameterAttributes.In) != 0;
        public bool IsOptional => ((ParameterAttributes)Attributes & ParameterAttributes.Optional) != 0;
        public bool IsOut => ((ParameterAttributes)Attributes & ParameterAttributes.Out) != 0;
        public virtual string? Name => throw new NotImplementedException();
        public virtual int Position => throw new NotImplementedException();
        public virtual void SetConstant(object? defaultValue) => throw new NotImplementedException();
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            SetCustomAttributeCore(con, binaryAttribute);
        }
        protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
        }
    }
}
