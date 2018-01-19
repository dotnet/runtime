// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection
{
    public abstract partial class MemberInfo : ICustomAttributeProvider
    {
        protected MemberInfo() { }

        public abstract MemberTypes MemberType { get; }
        public abstract string Name { get; }
        public abstract Type DeclaringType { get; }
        public abstract Type ReflectedType { get; }

        public virtual Module Module
        {
            get
            {
                // This check is necessary because for some reason, Type adds a new "Module" property that hides the inherited one instead 
                // of overriding.

                Type type = this as Type;
                if (type != null)
                    return type.Module;

                throw NotImplemented.ByDesign;
            }
        }

        public virtual bool HasSameMetadataDefinitionAs(MemberInfo other) { throw NotImplemented.ByDesign; }

        public abstract bool IsDefined(Type attributeType, bool inherit);
        public abstract object[] GetCustomAttributes(bool inherit);
        public abstract object[] GetCustomAttributes(Type attributeType, bool inherit);

        public virtual IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();
        public virtual IList<CustomAttributeData> GetCustomAttributesData() { throw NotImplemented.ByDesign; }

        public virtual int MetadataToken { get { throw new InvalidOperationException(); } }

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(MemberInfo left, MemberInfo right)
        {
            if (object.ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null)
                return false;

            Type type1, type2;
            MethodBase method1, method2;
            FieldInfo field1, field2;
            EventInfo event1, event2;
            PropertyInfo property1, property2;

            if ((type1 = left as Type) != null && (type2 = right as Type) != null)
                return type1 == type2;
            else if ((method1 = left as MethodBase) != null && (method2 = right as MethodBase) != null)
                return method1 == method2;
            else if ((field1 = left as FieldInfo) != null && (field2 = right as FieldInfo) != null)
                return field1 == field2;
            else if ((event1 = left as EventInfo) != null && (event2 = right as EventInfo) != null)
                return event1 == event2;
            else if ((property1 = left as PropertyInfo) != null && (property2 = right as PropertyInfo) != null)
                return property1 == property2;

            return false;
        }

        public static bool operator !=(MemberInfo left, MemberInfo right) => !(left == right);
    }
}
