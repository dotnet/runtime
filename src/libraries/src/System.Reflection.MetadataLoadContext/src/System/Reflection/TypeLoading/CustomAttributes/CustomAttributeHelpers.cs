// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace System.Reflection.TypeLoading
{
    internal static class CustomAttributeHelpers
    {
        /// <summary>
        /// Helper for creating a CustomAttributeNamedArgument.
        /// </summary>
        public static CustomAttributeNamedArgument ToCustomAttributeNamedArgument(this Type attributeType, string name, Type argumentType, object value)
        {
            MemberInfo[] members = attributeType.GetMember(name, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance);
            if (members.Length == 0)
                throw new MissingMemberException(attributeType.FullName, name);
            if (members.Length > 1)
                throw new AmbiguousMatchException();

            return new CustomAttributeNamedArgument(members[0], new CustomAttributeTypedArgument(argumentType, value));
        }

        /// <summary>
        /// Clones a cached CustomAttributeTypedArgument list into a freshly allocated one suitable for direct return through an api.
        /// </summary>
        public static ReadOnlyCollection<CustomAttributeTypedArgument> CloneForApiReturn(this IList<CustomAttributeTypedArgument> cats)
        {
            int count = cats.Count;
            CustomAttributeTypedArgument[] clones = new CustomAttributeTypedArgument[count];
            for (int i = 0; i < count; i++)
            {
                clones[i] = cats[i].CloneForApiReturn();
            }
            return clones.ToReadOnlyCollection();
        }

        /// <summary>
        /// Clones a cached CustomAttributeNamedArgument list into a freshly allocated one suitable for direct return through an api.
        /// </summary>
        public static ReadOnlyCollection<CustomAttributeNamedArgument> CloneForApiReturn(this IList<CustomAttributeNamedArgument> cans)
        {
            int count = cans.Count;
            CustomAttributeNamedArgument[] clones = new CustomAttributeNamedArgument[count];
            for (int i = 0; i < count; i++)
            {
                clones[i] = cans[i].CloneForApiReturn();
            }
            return clones.ToReadOnlyCollection();
        }

        /// <summary>
        /// Clones a cached CustomAttributeTypedArgument into a freshly allocated one suitable for direct return through an api.
        /// </summary>
        private static CustomAttributeTypedArgument CloneForApiReturn(this CustomAttributeTypedArgument cat)
        {
            Type type = cat.ArgumentType;
            object value = cat.Value;

            if (!(value is IList<CustomAttributeTypedArgument> cats))
                return cat;

            int count = cats.Count;
            CustomAttributeTypedArgument[] cads = new CustomAttributeTypedArgument[count];
            for (int i = 0; i < count; i++)
            {
                cads[i] = cats[i].CloneForApiReturn();
            }
            return new CustomAttributeTypedArgument(type, cads.ToReadOnlyCollection());
        }

        /// <summary>
        /// Clones a cached CustomAttributeNamedArgument into a freshly allocated one suitable for direct return through an api.
        /// </summary>
        private static CustomAttributeNamedArgument CloneForApiReturn(this CustomAttributeNamedArgument can)
        {
            return new CustomAttributeNamedArgument(can.MemberInfo, can.TypedValue.CloneForApiReturn());
        }

        /// <summary>
        /// Convert MarshalAsAttribute data into CustomAttributeData form. Returns null if the core assembly cannot be loaded or if the necessary
        /// types aren't in the core assembly.
        /// </summary>
        public static CustomAttributeData TryComputeMarshalAsCustomAttributeData(Func<MarshalAsAttribute> marshalAsAttributeComputer, MetadataLoadContext loader)
        {
            // Make sure all the necessary framework types exist in this MetadataLoadContext's core assembly. If one doesn't, skip.
            CoreTypes ct = loader.GetAllFoundCoreTypes();
            if (ct[CoreType.String] == null ||
                ct[CoreType.Boolean] == null ||
                ct[CoreType.UnmanagedType] == null ||
                ct[CoreType.VarEnum] == null ||
                ct[CoreType.Type] == null ||
                ct[CoreType.Int16] == null ||
                ct[CoreType.Int32] == null)
                return null;
            ConstructorInfo ci = loader.TryGetMarshalAsCtor();
            if (ci == null)
                return null;

            Func<CustomAttributeArguments> argumentsPromise =
                () =>
                {
                    // The expensive work goes in here. It will not execute unless someone invokes the Constructor/NamedArguments properties on
                    // the CustomAttributeData.

                    MarshalAsAttribute ma = marshalAsAttributeComputer();

                    Type attributeType = ci.DeclaringType;

                    CustomAttributeTypedArgument[] cats = { new CustomAttributeTypedArgument(ct[CoreType.UnmanagedType], (int)(ma.Value)) };
                    List<CustomAttributeNamedArgument> cans = new List<CustomAttributeNamedArgument>();
                    cans.AddRange(new CustomAttributeNamedArgument[]
                    {
                        attributeType.ToCustomAttributeNamedArgument(nameof(MarshalAsAttribute.ArraySubType), ct[CoreType.UnmanagedType], (int)ma.ArraySubType),
                        attributeType.ToCustomAttributeNamedArgument(nameof(MarshalAsAttribute.IidParameterIndex), ct[CoreType.Int32], ma.IidParameterIndex),
                        attributeType.ToCustomAttributeNamedArgument(nameof(MarshalAsAttribute.SafeArraySubType), ct[CoreType.VarEnum], (int)ma.SafeArraySubType),
                        attributeType.ToCustomAttributeNamedArgument(nameof(MarshalAsAttribute.SizeConst), ct[CoreType.Int32], ma.SizeConst),
                        attributeType.ToCustomAttributeNamedArgument(nameof(MarshalAsAttribute.SizeParamIndex), ct[CoreType.Int16], ma.SizeParamIndex),
                    });

                    if (ma.SafeArrayUserDefinedSubType != null)
                    {
                        cans.Add(attributeType.ToCustomAttributeNamedArgument(nameof(MarshalAsAttribute.SafeArrayUserDefinedSubType), ct[CoreType.Type], ma.SafeArrayUserDefinedSubType));
                    }

                    if (ma.MarshalType != null)
                    {
                        cans.Add(attributeType.ToCustomAttributeNamedArgument(nameof(MarshalAsAttribute.MarshalType), ct[CoreType.String], ma.MarshalType));
                    }

                    if (ma.MarshalTypeRef != null)
                    {
                        cans.Add(attributeType.ToCustomAttributeNamedArgument(nameof(MarshalAsAttribute.MarshalTypeRef), ct[CoreType.Type], ma.MarshalTypeRef));
                    }

                    if (ma.MarshalCookie != null)
                    {
                        cans.Add(attributeType.ToCustomAttributeNamedArgument(nameof(MarshalAsAttribute.MarshalCookie), ct[CoreType.String], ma.MarshalCookie));
                    }

                    return new CustomAttributeArguments(cats, cans);
                };

            return new RoPseudoCustomAttributeData(ci, argumentsPromise);
        }
    }
}
