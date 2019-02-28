// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection.Context
{
    public abstract partial class CustomReflectionContext : System.Reflection.ReflectionContext
    {
        protected CustomReflectionContext() { }
        protected CustomReflectionContext(System.Reflection.ReflectionContext source) { }
        protected virtual System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> AddProperties(System.Type type) { throw null; }
        protected System.Reflection.PropertyInfo CreateProperty(System.Type propertyType, string name, System.Func<object, object> getter, System.Action<object, object> setter) { throw null; }
        protected System.Reflection.PropertyInfo CreateProperty(System.Type propertyType, string name, System.Func<object, object> getter, System.Action<object, object> setter, System.Collections.Generic.IEnumerable<System.Attribute> propertyCustomAttributes, System.Collections.Generic.IEnumerable<System.Attribute> getterCustomAttributes, System.Collections.Generic.IEnumerable<System.Attribute> setterCustomAttributes) { throw null; }
        protected virtual System.Collections.Generic.IEnumerable<object> GetCustomAttributes(System.Reflection.MemberInfo member, System.Collections.Generic.IEnumerable<object> declaredAttributes) { throw null; }
        protected virtual System.Collections.Generic.IEnumerable<object> GetCustomAttributes(System.Reflection.ParameterInfo parameter, System.Collections.Generic.IEnumerable<object> declaredAttributes) { throw null; }
        public override System.Reflection.Assembly MapAssembly(System.Reflection.Assembly assembly) { throw null; }
        public override System.Reflection.TypeInfo MapType(System.Reflection.TypeInfo type) { throw null; }
    }
}
