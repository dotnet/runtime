// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given module
    internal class ProjectingModule : DelegatingModule, IProjectable
    {
        public ProjectingModule(Module module, Projector projector)
            : base(module)
        {
            Debug.Assert(null != projector);

            Projector = projector;
        }

        public Projector Projector { get; }

        public override Assembly Assembly
        {
            get { return Projector.ProjectAssembly(base.Assembly); }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            attributeType = Projector.Unproject(attributeType);

            return base.GetCustomAttributes(attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return Projector.Project(base.GetCustomAttributesData(), Projector.ProjectCustomAttributeData);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            attributeType = Projector.Unproject(attributeType);

            return base.IsDefined(attributeType, inherit);
        }

        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            return Projector.ProjectField(base.GetField(name, bindingAttr));
        }

        public override FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            return Projector.Project(base.GetFields(bindingFlags), Projector.ProjectField);
        }

        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            types = Projector.Unproject(types);
            return Projector.ProjectMethod(base.GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers));
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            return Projector.Project(base.GetMethods(bindingFlags), Projector.ProjectMethod);
        }

        public override Type? GetType(string className, bool throwOnError, bool ignoreCase)
        {
            return Projector.ProjectType(base.GetType(className, throwOnError, ignoreCase));
        }

        public override Type[] GetTypes()
        {
            return Projector.Project(base.GetTypes(), Projector.ProjectType);
        }

        public override FieldInfo? ResolveField(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            genericTypeArguments = Projector.Unproject(genericTypeArguments);
            genericMethodArguments = Projector.Unproject(genericMethodArguments);

            return Projector.ProjectField(base.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments));
        }

        public override MemberInfo? ResolveMember(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            genericTypeArguments = Projector.Unproject(genericTypeArguments);
            genericMethodArguments = Projector.Unproject(genericMethodArguments);

            return Projector.ProjectMember(base.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments));
        }

        public override MethodBase? ResolveMethod(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            genericTypeArguments = Projector.Unproject(genericTypeArguments);
            genericMethodArguments = Projector.Unproject(genericMethodArguments);

            return Projector.ProjectMethodBase(base.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments));
        }

        public override Type ResolveType(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            genericTypeArguments = Projector.Unproject(genericTypeArguments);
            genericMethodArguments = Projector.Unproject(genericMethodArguments);

            return Projector.ProjectType(base.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments));
        }

        public override bool Equals([NotNullWhen(true)] object? o)
        {
            return o is ProjectingModule other &&
                   Projector == other.Projector &&
                   UnderlyingModule.Equals(other.UnderlyingModule);
        }

        public override int GetHashCode()
        {
            return Projector.GetHashCode() ^ UnderlyingModule.GetHashCode();
        }
    }
}
