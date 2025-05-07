// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprPropertyInfo : ExprWithType
    {
        public ExprPropertyInfo(CType type, PropertySymbol propertySymbol, AggregateType propertyType)
            : base(ExpressionKind.PropertyInfo, type)
        {
            Debug.Assert(propertySymbol != null);
            Debug.Assert(propertyType != null);
            Property = new PropWithType(propertySymbol, propertyType);
        }

        public PropWithType Property { get; }

        public PropertyInfo PropertyInfo
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get
            {
                // To do this, we need to construct a type array of the parameter types,
                // get the parent constructed type, and get the property from it.
                AggregateType aggType = Property.Ats;
                PropertySymbol propSym = Property.Prop();

                TypeArray genericInstanceParams = TypeManager.SubstTypeArray(propSym.Params, aggType, null);

                Type type = aggType.AssociatedSystemType;
                PropertyInfo propertyInfo = propSym.AssociatedPropertyInfo;

                // This is to ensure that for embedded nopia types, we have the
                // appropriate local type from the member itself; this is possible
                // because nopia types are not generic or nested.
                if (!type.IsGenericType && !type.IsNested)
                {
                    type = propertyInfo.DeclaringType;
                }

                foreach (PropertyInfo p in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (!p.HasSameMetadataDefinitionAs(propertyInfo))
                    {
                        continue;
                    }
                    Debug.Assert((p.Name == propertyInfo.Name) &&
                        (p.GetIndexParameters() == null || p.GetIndexParameters().Length == genericInstanceParams.Count));

                    bool match = true;
                    ParameterInfo[] parameters = p.GetSetMethod(true) != null ?
                        p.GetSetMethod(true).GetParameters() : p.GetGetMethod(true).GetParameters();
                    for (int i = 0; i < genericInstanceParams.Count; i++)
                    {
                        if (!TypesAreEqual(parameters[i].ParameterType, genericInstanceParams[i].AssociatedSystemType))
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
                        return p;
                    }
                }

                throw Error.InternalCompilerError();
            }
        }
    }
}
