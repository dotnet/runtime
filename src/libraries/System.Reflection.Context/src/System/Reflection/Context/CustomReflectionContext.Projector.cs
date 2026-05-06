// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Context.Custom;
using System.Reflection.Context.Projection;

namespace System.Reflection.Context
{
    public partial class CustomReflectionContext
    {
        private sealed class ReflectionContextProjector : Projector
        {
            public ReflectionContextProjector(CustomReflectionContext context)
            {
                ReflectionContext = context;
            }

            public CustomReflectionContext ReflectionContext { get; }

            public TypeInfo ProjectTypeIfNeeded(TypeInfo value)
            {
                if (NeedsProjection(value))
                {
                    // Map the assembly to the underlying context first
                    Debug.Assert(ReflectionContext.SourceContext != null);
                    value = ReflectionContext.SourceContext.MapType(value);
                    return ProjectType(value);
                }
                else
                    return value;
            }

            public Assembly ProjectAssemblyIfNeeded(Assembly value)
            {
                if (NeedsProjection(value))
                {
                    // Map the assembly to the underlying context first
                    Debug.Assert(ReflectionContext.SourceContext != null);
                    value = ReflectionContext.SourceContext.MapAssembly(value);

                    return ProjectAssembly(value);
                }
                else
                    return value;
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override TypeInfo? ProjectType(Type? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new CustomType(value, ReflectionContext);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override Assembly? ProjectAssembly(Assembly? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new CustomAssembly(value, ReflectionContext);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override Module? ProjectModule(Module? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new CustomModule(value, ReflectionContext);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override FieldInfo? ProjectField(FieldInfo? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new CustomFieldInfo(value, ReflectionContext);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override EventInfo? ProjectEvent(EventInfo? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new CustomEventInfo(value, ReflectionContext);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override ConstructorInfo? ProjectConstructor(ConstructorInfo? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new CustomConstructorInfo(value, ReflectionContext);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override MethodInfo? ProjectMethod(MethodInfo? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new CustomMethodInfo(value, ReflectionContext);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override MethodBase? ProjectMethodBase(MethodBase? value)
            {
                if (value == null)
                    return null;

                MethodInfo? method = value as MethodInfo;
                if (method != null)
                    return ProjectMethod(method);

                ConstructorInfo? constructor = value as ConstructorInfo;
                if (constructor != null)
                    return ProjectConstructor(constructor);

                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_InvalidMethodType, value.GetType()));
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override PropertyInfo? ProjectProperty(PropertyInfo? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new CustomPropertyInfo(value, ReflectionContext);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override ParameterInfo? ProjectParameter(ParameterInfo? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new CustomParameterInfo(value, ReflectionContext);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override MethodBody? ProjectMethodBody(MethodBody? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new ProjectingMethodBody(value, this);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override LocalVariableInfo? ProjectLocalVariable(LocalVariableInfo? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new ProjectingLocalVariableInfo(value, this);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override ExceptionHandlingClause? ProjectExceptionHandlingClause(ExceptionHandlingClause? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new ProjectingExceptionHandlingClause(value, this);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override CustomAttributeData? ProjectCustomAttributeData(CustomAttributeData? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new ProjectingCustomAttributeData(value, this);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override ManifestResourceInfo? ProjectManifestResource(ManifestResourceInfo? value)
            {
                if (value == null)
                    return null;

                Debug.Assert(NeedsProjection(value));

                return new ProjectingManifestResourceInfo(value, this);
            }

            [return: NotNullIfNotNull(nameof(value))]
            public override MemberInfo? ProjectMember(MemberInfo? value)
            {
                if (value == null)
                    return null;

                MemberInfo? output;
                switch (value.MemberType)
                {
                    case MemberTypes.TypeInfo:
                    case MemberTypes.NestedType:
                        output = ProjectType((Type)value);
                        break;

                    case MemberTypes.Constructor:
                        output = ProjectConstructor((ConstructorInfo)value);
                        break;

                    case MemberTypes.Event:
                        output = ProjectEvent((EventInfo)value);
                        break;

                    case MemberTypes.Field:
                        output = ProjectField((FieldInfo)value);
                        break;

                    case MemberTypes.Method:
                        output = ProjectMethod((MethodInfo)value);
                        break;

                    case MemberTypes.Property:
                        output = ProjectProperty((PropertyInfo)value);
                        break;

                    default:
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_InvalidMemberType, value.Name, value.MemberType));
                }

                return output;
            }

            public override CustomAttributeTypedArgument ProjectTypedArgument(CustomAttributeTypedArgument value)
            {
                Type? argumentType = ProjectType(value.ArgumentType);

                return new CustomAttributeTypedArgument(argumentType, value.Value);
            }

            public override CustomAttributeNamedArgument ProjectNamedArgument(CustomAttributeNamedArgument value)
            {
                MemberInfo member = ProjectMember(value.MemberInfo);
                CustomAttributeTypedArgument typedArgument = ProjectTypedArgument(value.TypedValue);

                return new CustomAttributeNamedArgument(member, typedArgument);
            }

            public override InterfaceMapping ProjectInterfaceMapping(InterfaceMapping value)
            {
                return new InterfaceMapping
                {
                    InterfaceMethods = Project(value.InterfaceMethods, ProjectMethod),
                    InterfaceType = ProjectType(value.InterfaceType),
                    TargetMethods = Project(value.TargetMethods, ProjectMethod),
                    TargetType = ProjectType(value.TargetType)
                };
            }
        }
    }
}
