// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

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

        protected PropertyInfo CreateProperty(Type propertyType, string name, Func<object, object> getter, Action<object, object> setter)
        {
            throw new PlatformNotSupportedException();
        }

        protected PropertyInfo CreateProperty(Type propertyType, string name, Func<object, object> getter, Action<object, object> setter, IEnumerable<Attribute> propertyCustomAttributes, IEnumerable<Attribute> getterCustomAttributes, IEnumerable<Attribute> setterCustomAttributes)
        {
            throw new PlatformNotSupportedException();
        }

        protected virtual IEnumerable<object> GetCustomAttributes(MemberInfo member, IEnumerable<object> declaredAttributes)
        {
            throw new PlatformNotSupportedException();
        }

        protected virtual IEnumerable<object> GetCustomAttributes(ParameterInfo parameter, IEnumerable<object> declaredAttributes)
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
