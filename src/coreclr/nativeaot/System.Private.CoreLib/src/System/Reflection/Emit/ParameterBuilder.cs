// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public partial class ParameterBuilder
    {
        internal ParameterBuilder()
        {
            // Prevent generating a default constructor
        }

        public virtual int Attributes
        {
            get
            {
                return default;
            }
        }

        public bool IsIn
        {
            get
            {
                return default;
            }
        }

        public bool IsOptional
        {
            get
            {
                return default;
            }
        }

        public bool IsOut
        {
            get
            {
                return default;
            }
        }

        public virtual string Name
        {
            get
            {
                return default;
            }
        }

        public virtual int Position
        {
            get
            {
                return default;
            }
        }

        public virtual void SetConstant(object defaultValue)
        {
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
        }
    }
}
