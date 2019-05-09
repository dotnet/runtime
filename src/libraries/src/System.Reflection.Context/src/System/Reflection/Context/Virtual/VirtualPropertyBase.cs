// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Reflection.Context.Virtual
{
    // Provides the base class for func-based properties and indexers
    internal abstract partial class VirtualPropertyBase : PropertyInfo
    {
        private readonly string _name;
        private readonly Type _propertyType;
        private Type _declaringType;
        private ParameterInfo[] _indexedParameters;

        protected VirtualPropertyBase(Type propertyType, string name, CustomReflectionContext context)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0)
                throw new ArgumentException("", nameof(name));

            if (propertyType == null)
                throw new ArgumentNullException(nameof(propertyType));

            Debug.Assert(context != null);

            _propertyType = propertyType;
            _name = name;
            ReflectionContext = context;
        }

        public CustomReflectionContext ReflectionContext { get; }

        public override sealed PropertyAttributes Attributes
        {
            get { return PropertyAttributes.None; }
        }

        public override sealed Type DeclaringType
        {
            get { return _declaringType; }
        }

        public override sealed string Name
        {
            get { return _name; }
        }

        public override sealed Type PropertyType
        {
            get { return _propertyType; }
        }

        public override sealed bool CanRead
        {
            get { return GetGetMethod(true) != null; }
        }

        public override sealed bool CanWrite
        {
            get { return GetSetMethod(true) != null; }
        }

        public override sealed int MetadataToken
        {
            get { throw new InvalidOperationException(); }
        }

        public override sealed Module Module
        {
            get { return DeclaringType.Module; }
        }

        public override sealed Type ReflectedType
        {
            get { return DeclaringType; }
        }

        public override sealed MethodInfo[] GetAccessors(bool nonPublic)
        {
            MethodInfo getMethod = GetGetMethod(nonPublic);
            MethodInfo setMethod = GetSetMethod(nonPublic);

            Debug.Assert(getMethod != null || setMethod != null);

            if (getMethod == null || setMethod == null)
            {
                return new MethodInfo[] { getMethod ?? setMethod };
            }

            return new MethodInfo[] { getMethod, setMethod };
        }

        public override sealed ParameterInfo[] GetIndexParameters()
        {
            return (ParameterInfo[])GetIndexParametersNoCopy().Clone();
        }

        public override sealed object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            MethodInfo getMethod = GetGetMethod(true);
            if (getMethod == null)
                throw new ArgumentException(SR.Argument_GetMethNotFnd);

            return getMethod.Invoke(obj, invokeAttr, binder, index, culture);
        }

        public override sealed void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
        {
            MethodInfo setMethod = GetSetMethod(true);
            if (setMethod == null)
                throw new ArgumentException(SR.Argument_GetMethNotFnd);

            object[] args = null;

            if (index != null)
            {
                args = new object[index.Length + 1];

                Array.Copy(index, 0, args, 0, index.Length);

                args[index.Length] = value;
            }
            else
            {
                args = new object[1];
                args[0] = value;
            }   

            setMethod.Invoke(obj, invokeAttr, binder, args, culture);
        }

        public override sealed object GetConstantValue()
        {
            throw new InvalidOperationException(SR.InvalidOperation_EnumLitValueNotFound);
        }

        public override sealed object GetRawConstantValue()
        {
            throw new InvalidOperationException(SR.InvalidOperation_EnumLitValueNotFound);
        }

        public override sealed Type[] GetOptionalCustomModifiers()
        {
            return CollectionServices.Empty<Type>();
        }

        public override sealed Type[] GetRequiredCustomModifiers()
        {
            return CollectionServices.Empty<Type>();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return CollectionServices.Empty<Attribute>();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return CollectionServices.Empty<Attribute>();
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CollectionServices.Empty<CustomAttributeData>();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            // We don't need to compare the getters and setters.
            // But do we need to compare the contexts and return types?
            return obj is VirtualPropertyBase other &&
                _name == other._name &&
                _declaringType.Equals(other._declaringType) &&
                _propertyType == other._propertyType &&
                CollectionServices.CompareArrays(GetIndexParametersNoCopy(), other.GetIndexParametersNoCopy());
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode() ^
                _declaringType.GetHashCode() ^
                _propertyType.GetHashCode() ^
                CollectionServices.GetArrayHashCode(GetIndexParametersNoCopy());
        }

        public override string ToString()
        {
            return base.ToString();
        }

        internal void SetDeclaringType(Type declaringType)
        {
            _declaringType = declaringType;
        }

        private ParameterInfo[] GetIndexParametersNoCopy()
        {
            if (_indexedParameters == null)
            {
                MethodInfo method = GetGetMethod(true);
                if (method != null)
                {
                    _indexedParameters = VirtualParameter.CloneParameters(this, method.GetParameters(), skipLastParameter: false);
                }
                else
                {
                    method = GetSetMethod(true);

                    Debug.Assert(null != method);
                    _indexedParameters = VirtualParameter.CloneParameters(this, method.GetParameters(), skipLastParameter: true);
                }
            }

            return _indexedParameters;
        }
    }
}
