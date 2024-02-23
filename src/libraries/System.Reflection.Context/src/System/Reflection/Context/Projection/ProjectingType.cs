// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given type
    internal class ProjectingType : DelegatingType, IProjectable
    {
        private readonly Projector _projector;

        public ProjectingType(Type type, Projector projector)
            : base(type)
        {
            Debug.Assert(null != projector);

            _projector = projector;
        }

        public Projector Projector
        {
            get { return _projector; }
        }

        public override Assembly Assembly
        {
            get { return _projector.ProjectAssembly(base.Assembly); }
        }

        public override Type? BaseType
        {
            get { return _projector.ProjectType(base.BaseType); }
        }

        public override MethodBase? DeclaringMethod
        {
            get { return _projector.ProjectMethodBase(base.DeclaringMethod); }
        }

        public override Type? DeclaringType
        {
            get { return _projector.ProjectType(base.DeclaringType); }
        }

        public override Module Module
        {
            get { return _projector.ProjectModule(base.Module); }
        }

        public override Type? ReflectedType
        {
            get { return _projector.ProjectType(base.ReflectedType); }
        }

        public override MemberInfo[] GetDefaultMembers()
        {
            return _projector.Project(base.GetDefaultMembers(), _projector.ProjectMember);
        }

        public override Type GetEnumUnderlyingType()
        {
            return _projector.ProjectType(base.GetEnumUnderlyingType());
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            attributeType = Projector.Unproject(attributeType);

            return base.GetCustomAttributes(attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return _projector.Project(base.GetCustomAttributesData(), _projector.ProjectCustomAttributeData);
        }

        public override EventInfo[] GetEvents()
        {
            return _projector.Project(base.GetEvents(), _projector.ProjectEvent);
        }

        public override Type[] GetGenericArguments()
        {
            return _projector.Project(base.GetGenericArguments(), _projector.ProjectType);
        }

        public override Type[] GetGenericParameterConstraints()
        {
            return _projector.Project(base.GetGenericParameterConstraints(), _projector.ProjectType);
        }

        public override Type GetGenericTypeDefinition()
        {
            return _projector.ProjectType(base.GetGenericTypeDefinition());
        }

        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            interfaceType = Projector.Unproject(interfaceType);

            return _projector.ProjectInterfaceMapping(base.GetInterfaceMap(interfaceType));
        }

        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            StringComparison comparisonType = (bindingAttr & BindingFlags.IgnoreCase) == BindingFlags.IgnoreCase
                ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            List<MemberInfo> matchingMembers = new List<MemberInfo>();

            if ((type & MemberTypes.Constructor) != 0)
                matchingMembers.AddRange(GetConstructors(bindingAttr));

            if ((type & MemberTypes.Event) != 0)
                matchingMembers.AddRange(GetEvents(bindingAttr));

            if ((type & MemberTypes.Field) != 0)
                matchingMembers.AddRange(GetFields(bindingAttr));

            if ((type & MemberTypes.Method) != 0)
                matchingMembers.AddRange(GetMethods(bindingAttr));

            if ((type & MemberTypes.NestedType) != 0)
                matchingMembers.AddRange(GetNestedTypes(bindingAttr));

            if ((type & MemberTypes.Property) != 0)
                matchingMembers.AddRange(GetProperties(bindingAttr));

            matchingMembers.RemoveAll(member => !string.Equals(member.Name, name, comparisonType));

            return matchingMembers.ToArray();
        }

        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c)
        {
            ProjectingType? otherType = c as ProjectingType;
            if (otherType == null || Projector != otherType.Projector)
                return false;

            return UnderlyingType.IsAssignableFrom(otherType.UnderlyingType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            attributeType = Projector.Unproject(attributeType);

            return base.IsDefined(attributeType, inherit);
        }

        public override bool IsEquivalentTo([NotNullWhen(true)] Type? other)
        {
            ProjectingType? otherType = other as ProjectingType;
            if (otherType == null || Projector != otherType.Projector)
                return false;

            return UnderlyingType.IsEquivalentTo(otherType.UnderlyingType);
        }

        public override bool IsInstanceOfType([NotNullWhen(true)] object? o)
        {
            Type? objectType = _projector.ProjectType(o?.GetType());

            return IsAssignableFrom(objectType);
        }

        // We could have used the default implementation of this on Type
        // if it handled special cases like generic type constraints
        // and interfaces->objec.
        public override bool IsSubclassOf(Type c)
        {
            ProjectingType? otherType = c as ProjectingType;
            if (otherType == null || Projector != otherType.Projector)
                return false;

            return UnderlyingType.IsSubclassOf(otherType.UnderlyingType);
        }

        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
        {
            types = Projector.Unproject(types);

            return _projector.ProjectConstructor(base.GetConstructorImpl(bindingAttr, binder, callConvention, types, modifiers));
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return _projector.Project(base.GetConstructors(bindingAttr), _projector.ProjectConstructor);
        }

        public override Type? GetElementType()
        {
            return _projector.ProjectType(base.GetElementType());
        }

        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
        {
            return _projector.ProjectEvent(base.GetEvent(name, bindingAttr));
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return _projector.Project(base.GetEvents(bindingAttr), _projector.ProjectEvent);
        }

        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            return _projector.ProjectField(base.GetField(name, bindingAttr));
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return _projector.Project(base.GetFields(bindingAttr), _projector.ProjectField);
        }

        public override Type? GetInterface(string name, bool ignoreCase)
        {
            return _projector.ProjectType(base.GetInterface(name, ignoreCase));
        }

        public override Type[] GetInterfaces()
        {
            return _projector.Project(base.GetInterfaces(), _projector.ProjectType);
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            MethodInfo[] methods = GetMethods(bindingAttr);
            ConstructorInfo[] constructors = GetConstructors(bindingAttr);
            PropertyInfo[] properties = GetProperties(bindingAttr);
            EventInfo[] events = GetEvents(bindingAttr);
            FieldInfo[] fields = GetFields(bindingAttr);
            Type[] nestedTypes = GetNestedTypes(bindingAttr);
            // Interfaces are excluded from the result of GetMembers

            MemberInfo[] members = new MemberInfo[
                methods.Length +
                constructors.Length +
                properties.Length +
                events.Length +
                fields.Length +
                nestedTypes.Length];

            int i = 0;
            Array.Copy(methods, 0, members, i, methods.Length); i += methods.Length;
            Array.Copy(constructors, 0, members, i, constructors.Length); i += constructors.Length;
            Array.Copy(properties, 0, members, i, properties.Length); i += properties.Length;
            Array.Copy(events, 0, members, i, events.Length); i += events.Length;
            Array.Copy(fields, 0, members, i, fields.Length); i += fields.Length;
            Array.Copy(nestedTypes, 0, members, i, nestedTypes.Length); i += nestedTypes.Length;

            Debug.Assert(i == members.Length);

            return members;
        }

        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            types = Projector.Unproject(types);

            return _projector.ProjectMethod(base.GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers));
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return _projector.Project(base.GetMethods(bindingAttr), _projector.ProjectMethod);
        }

        public override Type? GetNestedType(string name, BindingFlags bindingAttr)
        {
            return _projector.ProjectType(base.GetNestedType(name, bindingAttr));
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return _projector.Project(base.GetNestedTypes(bindingAttr), _projector.ProjectType);
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return _projector.Project(base.GetProperties(bindingAttr), _projector.ProjectProperty);
        }

        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            returnType = Projector.Unproject(returnType);
            types = Projector.Unproject(types);

            return _projector.ProjectProperty(base.GetPropertyImpl(name, bindingAttr, binder, returnType, types, modifiers));
        }

        public override Type MakeArrayType()
        {
            return _projector.ProjectType(base.MakeArrayType());
        }

        public override Type MakeArrayType(int rank)
        {
            return _projector.ProjectType(base.MakeArrayType(rank));
        }

        public override Type MakePointerType()
        {
            return _projector.ProjectType(base.MakePointerType());
        }

        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(params Type[] typeArguments)
        {
            typeArguments = Projector.Unproject(typeArguments);

            return _projector.ProjectType(base.MakeGenericType(typeArguments));
        }

        public override Type MakeByRefType()
        {
            return _projector.ProjectType(base.MakeByRefType());
        }

        public override bool Equals([NotNullWhen(true)] object? o)
        {
            return o is ProjectingType other &&
                Projector == other.Projector &&
                UnderlyingType.Equals(other.UnderlyingType);
        }

        public override int GetHashCode()
        {
            return Projector.GetHashCode() ^ UnderlyingType.GetHashCode();
        }
    }
}
