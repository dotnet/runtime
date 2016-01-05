// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using global::System;
using global::System.Reflection;
using global::System.Diagnostics;
using global::System.Collections.Generic;

namespace System.Reflection.Context
{
    public abstract class CustomReflectionContext : ReflectionContext
    {
        protected CustomReflectionContext()
        {
            throw new PlatformNotSupportedException();
        }

        protected CustomReflectionContext(ReflectionContext source)
        {
            throw new PlatformNotSupportedException();
        }

        protected virtual IEnumerable<PropertyInfo> AddProperties(Type type)
        {
            throw new PlatformNotSupportedException();
        }

        protected PropertyInfo CreateProperty(Type propertyType, String name, Func<Object, Object> getter, Action<Object, Object> setter)
        {
            throw new PlatformNotSupportedException();
        }

        protected PropertyInfo CreateProperty(Type propertyType, String name, Func<Object, Object> getter, Action<Object, Object> setter, IEnumerable<Attribute> propertyCustomAttributes, IEnumerable<Attribute> getterCustomAttributes, IEnumerable<Attribute> setterCustomAttributes)
        {
            throw new PlatformNotSupportedException();
        }

        protected virtual IEnumerable<Object> GetCustomAttributes(MemberInfo member, IEnumerable<Object> declaredAttributes)
        {
            throw new PlatformNotSupportedException();
        }

        protected virtual IEnumerable<Object> GetCustomAttributes(ParameterInfo parameter, IEnumerable<Object> declaredAttributes)
        {
            throw new PlatformNotSupportedException();
        }

        public override Assembly MapAssembly(Assembly assembly)
        {
            throw new PlatformNotSupportedException();
        }

        public override TypeInfo MapType(TypeInfo type)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
