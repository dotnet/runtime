// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public sealed class PropertyBuilder : PropertyInfo
    {
        internal PropertyBuilder()
        {
            // Prevent generating a default constructor
        }

        public override PropertyAttributes Attributes
        {
            get
            {
                return default;
            }
        }

        public override bool CanRead
        {
            get
            {
                return default;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return default;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return default;
            }
        }

        public override Module Module
        {
            get
            {
                return default;
            }
        }

        public override string Name
        {
            get
            {
                return default;
            }
        }

        public override Type PropertyType
        {
            get
            {
                return default;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return default;
            }
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
        {
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            return default;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return default;
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return default;
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            return default;
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            return default;
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            return default;
        }

        public override object? GetValue(object? obj, object?[]? index)
        {
            return default;
        }

        public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, Globalization.CultureInfo? culture)
        {
            return default;
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return default;
        }

        public void SetConstant(object defaultValue)
        {
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
        }

        public void SetGetMethod(MethodBuilder mdBuilder)
        {
        }

        public void SetSetMethod(MethodBuilder mdBuilder)
        {
        }

        public override void SetValue(object? obj, object? value, object?[]? index)
        {
        }

        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, Globalization.CultureInfo? culture)
        {
        }
    }
}
