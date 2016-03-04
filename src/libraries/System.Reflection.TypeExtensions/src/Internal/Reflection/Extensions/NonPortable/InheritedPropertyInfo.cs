// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

using Internal.Reflection.Extensibility;

namespace Internal.Reflection.Extensions.NonPortable
{
    //
    // This class exists for desktop compatibility. If one uses an api such as Type.GetProperty(string) to retrieve a member
    // from a base class, the desktop returns a special MemberInfo object that is blocked from seeing or invoking private
    // set or get methods on that property. That is, the type used to find the member is part of that member's object identity.
    //
    internal sealed class InheritedPropertyInfo : ExtensiblePropertyInfo
    {
        private readonly PropertyInfo _underlyingPropertyInfo;
        private readonly Type _reflectedType;

        internal InheritedPropertyInfo(PropertyInfo underlyingPropertyInfo, Type reflectedType)
        {
            // If the reflectedType is the declaring type, the caller should have used the original PropertyInfo.
            // This assert saves us from having to check this throughout.
            Debug.Assert(!(reflectedType.Equals(underlyingPropertyInfo.DeclaringType)), "reflectedType must be a proper base type of (and not equal to) underlyingPropertyInfo.DeclaringType.");

            _underlyingPropertyInfo = underlyingPropertyInfo;
            _reflectedType = reflectedType;
            return;
        }

        public sealed override PropertyAttributes Attributes
        {
            get { return _underlyingPropertyInfo.Attributes; }
        }

        public sealed override bool CanRead
        {
            get { return GetMethod != null; }
        }

        public sealed override bool CanWrite
        {
            get { return SetMethod != null; }
        }

        public sealed override ParameterInfo[] GetIndexParameters()
        {
            return _underlyingPropertyInfo.GetIndexParameters();
        }

        public sealed override Type PropertyType
        {
            get { return _underlyingPropertyInfo.PropertyType; }
        }

        public sealed override Type DeclaringType
        {
            get { return _underlyingPropertyInfo.DeclaringType; }
        }

        public sealed override String Name
        {
            get { return _underlyingPropertyInfo.Name; }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get { return _underlyingPropertyInfo.CustomAttributes; }
        }

        public sealed override bool Equals(Object obj)
        {
            InheritedPropertyInfo other = obj as InheritedPropertyInfo;
            if (other == null)
            {
                return false;
            }

            if (!(_underlyingPropertyInfo.Equals(other._underlyingPropertyInfo)))
            {
                return false;
            }

            if (!(_reflectedType.Equals(other._reflectedType)))
            {
                return false;
            }

            return true;
        }

        public sealed override int GetHashCode()
        {
            int hashCode = _reflectedType.GetHashCode();
            hashCode ^= _underlyingPropertyInfo.GetHashCode();
            return hashCode;
        }

        public sealed override Object GetConstantValue()
        {
            return _underlyingPropertyInfo.GetConstantValue();
        }

        public sealed override MethodInfo GetMethod
        {
            get
            {
                MethodInfo accessor = _underlyingPropertyInfo.GetMethod;
                return Filter(accessor);
            }
        }

        public sealed override Object GetValue(Object obj, Object[] index)
        {
            if (GetMethod == null)
            {
                throw new ArgumentException(SR.Arg_GetMethNotFnd);
            }

            return _underlyingPropertyInfo.GetValue(obj, index);
        }

        public sealed override Module Module
        {
            get { return _underlyingPropertyInfo.Module; }
        }

        public sealed override String ToString()
        {
            return _underlyingPropertyInfo.ToString();
        }

        public sealed override MethodInfo SetMethod
        {
            get
            {
                MethodInfo accessor = _underlyingPropertyInfo.SetMethod;
                return Filter(accessor);
            }
        }

        public sealed override void SetValue(Object obj, Object value, Object[] index)
        {
            if (SetMethod == null)
            {
                throw new ArgumentException(SR.Arg_SetMethNotFnd);
            }

            _underlyingPropertyInfo.SetValue(obj, value, index);
        }

        private MethodInfo Filter(MethodInfo accessor)
        {
            //
            // For desktop compat, hide inherited accessors that are marked private.
            //  
            //   Q: Why don't we also hide cross-assembly "internal" accessors?
            //   A: That inconsistency is also desktop-compatible.
            //
            if (accessor == null || accessor.IsPrivate)
            {
                return null;
            }

            return accessor;
        }

        public override MemberTypes MemberType { get { return MemberTypes.Property; } }
    }
}
 
