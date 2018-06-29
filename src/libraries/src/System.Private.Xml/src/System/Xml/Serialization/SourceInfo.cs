// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Xml.Extensions;

#if !FEATURE_SERIALIZATION_UAPAOT
namespace System.Xml.Serialization
{
    internal class SourceInfo
    {
        //a[ia]
        //((global::System.Xml.Serialization.XmlSerializerNamespaces)p[0])
        private static Regex s_regex = new Regex("([(][(](?<t>[^)]+)[)])?(?<a>[^[]+)[[](?<ia>.+)[]][)]?");
        //((global::Microsoft.CFx.Test.Common.TypeLibrary.IXSType_9)o), @"IXSType_9", @"", true, true);
        private static Regex s_regex2 = new Regex("[(][(](?<cast>[^)]+)[)](?<arg>[^)]+)[)]");

        private static readonly Lazy<MethodInfo> s_iListGetItemMethod = new Lazy<MethodInfo>(
            () =>
            {
                return typeof(IList).GetMethod(
                    "get_Item",
                    new Type[] { typeof(int) }
                );
            });

        public string Source;
        public readonly string Arg;
        public readonly MemberInfo MemberInfo;
        public readonly Type Type;
        public readonly CodeGenerator ILG;

        public SourceInfo(string source, string arg, MemberInfo memberInfo, Type type, CodeGenerator ilg)
        {
            this.Source = source;
            this.Arg = arg ?? source;
            this.MemberInfo = memberInfo;
            this.Type = type;
            this.ILG = ilg;
        }

        public SourceInfo CastTo(TypeDesc td)
        {
            return new SourceInfo("((" + td.CSharpName + ")" + Source + ")", Arg, MemberInfo, td.Type, ILG);
        }

        public void LoadAddress(Type elementType)
        {
            InternalLoad(elementType, asAddress: true);
        }

        public void Load(Type elementType)
        {
            InternalLoad(elementType);
        }

        private void InternalLoad(Type elementType, bool asAddress = false)
        {
            Match match = s_regex.Match(Arg);
            if (match.Success)
            {
                object varA = ILG.GetVariable(match.Groups["a"].Value);
                Type varType = ILG.GetVariableType(varA);
                object varIA = ILG.GetVariable(match.Groups["ia"].Value);
                if (varType.IsArray)
                {
                    ILG.Load(varA);
                    ILG.Load(varIA);
                    Type eType = varType.GetElementType();
                    if (CodeGenerator.IsNullableGenericType(eType))
                    {
                        ILG.Ldelema(eType);
                        ConvertNullableValue(eType, elementType);
                    }
                    else
                    {
                        if (eType.IsValueType)
                        {
                            ILG.Ldelema(eType);
                            if (!asAddress)
                            {
                                ILG.Ldobj(eType);
                            }
                        }
                        else
                            ILG.Ldelem(eType);
                        if (elementType != null)
                            ILG.ConvertValue(eType, elementType);
                    }
                }
                else
                {
                    ILG.Load(varA);
                    ILG.Load(varIA);
                    MethodInfo get_Item = varType.GetMethod(
                        "get_Item",
                        CodeGenerator.InstanceBindingFlags,
                        new Type[] { typeof(int) }
                        );

                    if (get_Item == null && typeof(IList).IsAssignableFrom(varType))
                    {
                        get_Item = s_iListGetItemMethod.Value;
                    }

                    Debug.Assert(get_Item != null);
                    ILG.Call(get_Item);
                    Type eType = get_Item.ReturnType;
                    if (CodeGenerator.IsNullableGenericType(eType))
                    {
                        LocalBuilder localTmp = ILG.GetTempLocal(eType);
                        ILG.Stloc(localTmp);
                        ILG.Ldloca(localTmp);
                        ConvertNullableValue(eType, elementType);
                    }
                    else if ((elementType != null) && !(eType.IsAssignableFrom(elementType) || elementType.IsAssignableFrom(eType)))
                    {
                        throw new CodeGeneratorConversionException(eType, elementType, asAddress, "IsNotAssignableFrom");
                    }
                    else
                    {
                        Convert(eType, elementType, asAddress);
                    }
                }
            }
            else if (Source == "null")
            {
                ILG.Load(null);
            }
            else
            {
                object var;
                Type varType;
                if (Arg.StartsWith("o.@", StringComparison.Ordinal) || MemberInfo != null)
                {
                    var = ILG.GetVariable(Arg.StartsWith("o.@", StringComparison.Ordinal) ? "o" : Arg);
                    varType = ILG.GetVariableType(var);
                    if (varType.IsValueType)
                        ILG.LoadAddress(var);
                    else
                        ILG.Load(var);
                }
                else
                {
                    var = ILG.GetVariable(Arg);
                    varType = ILG.GetVariableType(var);

                    if (CodeGenerator.IsNullableGenericType(varType) &&
                        varType.GetGenericArguments()[0] == elementType)
                    {
                        ILG.LoadAddress(var);
                        ConvertNullableValue(varType, elementType);
                    }
                    else
                    {
                        if (asAddress)
                            ILG.LoadAddress(var);
                        else
                            ILG.Load(var);
                    }
                }

                if (MemberInfo != null)
                {
                    Type memberType = (MemberInfo is FieldInfo) ?
                        ((FieldInfo)MemberInfo).FieldType : ((PropertyInfo)MemberInfo).PropertyType;
                    if (CodeGenerator.IsNullableGenericType(memberType))
                    {
                        ILG.LoadMemberAddress(MemberInfo);
                        ConvertNullableValue(memberType, elementType);
                    }
                    else
                    {
                        ILG.LoadMember(MemberInfo);
                        Convert(memberType, elementType, asAddress);
                    }
                }
                else
                {
                    match = s_regex2.Match(Source);
                    if (match.Success)
                    {
                        Debug.Assert(match.Groups["arg"].Value == Arg);
                        Debug.Assert(match.Groups["cast"].Value == CodeIdentifier.GetCSharpName(Type));
                        if (asAddress)
                            ILG.ConvertAddress(varType, Type);
                        else
                            ILG.ConvertValue(varType, Type);
                        varType = Type;
                    }
                    Convert(varType, elementType, asAddress);
                }
            }
        }

        private void Convert(Type sourceType, Type targetType, bool asAddress)
        {
            if (targetType != null)
            {
                if (asAddress)
                    ILG.ConvertAddress(sourceType, targetType);
                else
                    ILG.ConvertValue(sourceType, targetType);
            }
        }

        private void ConvertNullableValue(Type nullableType, Type targetType)
        {
            System.Diagnostics.Debug.Assert(targetType == nullableType || targetType.IsAssignableFrom(nullableType.GetGenericArguments()[0]));
            if (targetType != nullableType)
            {
                MethodInfo Nullable_get_Value = nullableType.GetMethod(
                    "get_Value",
                    CodeGenerator.InstanceBindingFlags,
                    Array.Empty<Type>()
                    );
                ILG.Call(Nullable_get_Value);
                if (targetType != null)
                {
                    ILG.ConvertValue(Nullable_get_Value.ReturnType, targetType);
                }
            }
        }

        public static implicit operator string (SourceInfo source)
        {
            return source.Source;
        }

        public static bool operator !=(SourceInfo a, SourceInfo b)
        {
            if ((object)a != null)
                return !a.Equals(b);
            return (object)b != null;
        }

        public static bool operator ==(SourceInfo a, SourceInfo b)
        {
            if ((object)a != null)
                return a.Equals(b);
            return (object)b == null;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return Source == null;
            SourceInfo info = obj as SourceInfo;
            if (info != null)
                return Source == info.Source;
            return false;
        }

        public override int GetHashCode()
        {
            return (Source == null) ? 0 : Source.GetHashCode();
        }
    }
}
#endif
