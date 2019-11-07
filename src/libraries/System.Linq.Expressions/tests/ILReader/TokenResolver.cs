// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code adapted from https://blogs.msdn.microsoft.com/haibo_luo/2010/04/19/ilvisualizer-2010-solution

using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    public interface ITokenResolver
    {
        MethodBase AsMethod(int token);
        FieldInfo AsField(int token);
        Type AsType(int token);
        string AsString(int token);
        MemberInfo AsMember(int token);
        byte[] AsSignature(int token);
    }

    public sealed class ModuleScopeTokenResolver : ITokenResolver
    {
        private readonly Module _module;
        private readonly MethodBase _enclosingMethod;
        private readonly Type[] _methodContext;
        private readonly Type[] _typeContext;

        public ModuleScopeTokenResolver(MethodBase method)
        {
            _enclosingMethod = method;
            _module = method.Module;
            _methodContext = (method is ConstructorInfo) ? null : method.GetGenericArguments();
            _typeContext = (method.DeclaringType == null) ? null : method.DeclaringType.GetGenericArguments();
        }

        public MethodBase AsMethod(int token) => _module.ResolveMethod(token, _typeContext, _methodContext);
        public FieldInfo AsField(int token) => _module.ResolveField(token, _typeContext, _methodContext);
        public Type AsType(int token) => _module.ResolveType(token, _typeContext, _methodContext);
        public MemberInfo AsMember(int token) => _module.ResolveMember(token, _typeContext, _methodContext);
        public string AsString(int token) => _module.ResolveString(token);
        public byte[] AsSignature(int token) => _module.ResolveSignature(token);
    }

    internal static class ModuleExtensions
    {
        private static readonly MethodInfo s_resolveMethod = GetMethodInfo(nameof(ResolveMethod));
        private static readonly MethodInfo s_resolveField = GetMethodInfo(nameof(ResolveField));
        private static readonly MethodInfo s_resolveType = GetMethodInfo(nameof(ResolveType));
        private static readonly MethodInfo s_resolveMember = GetMethodInfo(nameof(ResolveMember));
        private static readonly MethodInfo s_resolveString = GetMethodInfo(nameof(ResolveString));
        private static readonly MethodInfo s_resolveSignature = GetMethodInfo(nameof(ResolveSignature));

        public static MethodBase ResolveMethod(this Module module, int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) => Invoke<MethodBase>(s_resolveMethod, module, metadataToken, genericTypeArguments, genericMethodArguments);
        public static FieldInfo ResolveField(this Module module, int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) => Invoke<FieldInfo>(s_resolveField, module, metadataToken, genericTypeArguments, genericMethodArguments);
        public static Type ResolveType(this Module module, int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) => Invoke<Type>(s_resolveType, module, metadataToken, genericTypeArguments, genericMethodArguments);
        public static MemberInfo ResolveMember(this Module module, int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments) => Invoke<MemberInfo>(s_resolveMember, module, metadataToken, genericTypeArguments, genericMethodArguments);
        public static byte[] ResolveSignature(this Module module, int metadataToken) => Invoke<byte[]>(s_resolveSignature, module, metadataToken);
        public static string ResolveString(this Module module, int metadataToken) => Invoke<string>(s_resolveString, module, metadataToken);

        private static MethodInfo GetMethodInfo(string name)
        {
            Type[] parameterTypes = typeof(ModuleExtensions).GetMethod(name).GetParameters().Skip(1).Select(p => p.ParameterType).ToArray();
            return typeof(Module).GetMethod(name, parameterTypes);
        }

        private static T Invoke<T>(MethodInfo method, Module module, params object[] args) => (T)method.Invoke(module, args);
    }
}
