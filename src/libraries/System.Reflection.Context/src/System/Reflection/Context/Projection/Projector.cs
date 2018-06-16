// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.Context.Projection
{
    internal abstract class Projector
    {
        public IList<T> Project<T>(IList<T> values, Func<T, T> project)
        {
            if (values == null || values.Count == 0)
                return values;

            T[] projected = ProjectAll(values, project);

            return Array.AsReadOnly(projected);
        }

        public T[] Project<T>(T[] values, Func<T, T> project)
        {
            if (values == null || values.Length == 0)
                return values;

            return ProjectAll(values, project);
        }

        public T Project<T>(T value, Func<T, T> project)
        {
            if (NeedsProjection(value))
            {
                // NeedsProjection should guarantee this.
                Debug.Assert(!(value is IProjectable) || ((IProjectable)value).Projector != this);

                return project(value);
            }

            return value;
        }

        public abstract TypeInfo ProjectType(Type value);
        public abstract Assembly ProjectAssembly(Assembly value);
        public abstract Module ProjectModule(Module value);
        public abstract FieldInfo ProjectField(FieldInfo value);
        public abstract EventInfo ProjectEvent(EventInfo value);
        public abstract ConstructorInfo ProjectConstructor(ConstructorInfo value);
        public abstract MethodInfo ProjectMethod(MethodInfo value);
        public abstract MethodBase ProjectMethodBase(MethodBase value);
        public abstract PropertyInfo ProjectProperty(PropertyInfo value);
        public abstract ParameterInfo ProjectParameter(ParameterInfo value);
        public abstract MethodBody ProjectMethodBody(MethodBody value);
        public abstract LocalVariableInfo ProjectLocalVariable(LocalVariableInfo value);
        public abstract ExceptionHandlingClause ProjectExceptionHandlingClause(ExceptionHandlingClause value);
        public abstract CustomAttributeData ProjectCustomAttributeData(CustomAttributeData value);
        public abstract ManifestResourceInfo ProjectManifestResource(ManifestResourceInfo value);
        public abstract CustomAttributeTypedArgument ProjectTypedArgument(CustomAttributeTypedArgument value);
        public abstract CustomAttributeNamedArgument ProjectNamedArgument(CustomAttributeNamedArgument value);
        public abstract InterfaceMapping ProjectInterfaceMapping(InterfaceMapping value);
        public abstract MemberInfo ProjectMember(MemberInfo value);

        public Type[] Unproject(Type[] values)
        {
            if (values == null)
                return null;

            Type[] newTypes = new Type[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                newTypes[i] = Unproject(values[i]);
            }

            return newTypes;
        }

        public Type Unproject(Type value)
        {
            if (value is ProjectingType projectingType)
                return projectingType.UnderlyingType;
            else
                return value;
        }

        public bool NeedsProjection(object value)
        {
            Debug.Assert(value != null);

            if (value == null)
                return false;

            if (value is IProjectable projector && projector == this)
                return false;   // Already projected

            // Different context, so we need to project it
            return true;
        }

        //protected abstract object ExecuteProjection<T>(object value);

        //protected abstract IProjection GetProjector(Type t);

        private T[] ProjectAll<T>(IList<T> values, Func<T, T> project)
        {
            Debug.Assert(null != project);
            Debug.Assert(values != null && values.Count > 0);

            var projected = new T[values.Count];

            for (int i = 0; i < projected.Length; i++)
            {
                T value = values[i];

                Debug.Assert(NeedsProjection(value));
                projected[i] = project(value);
            }

            return projected;
        }
    }
}
