// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Context;
using System.Threading;

namespace System.ComponentModel.Composition.Registration
{
    public class RegistrationBuilder : CustomReflectionContext
    {
        internal class InnerRC : ReflectionContext
        {
            public override TypeInfo MapType(TypeInfo t) { return t; }
            public override Assembly MapAssembly(Assembly a) { return a; }
        }

        private static readonly ReflectionContext s_inner = new InnerRC();
        private static readonly List<object> s_emptyList = new List<object>();

        private readonly Lock _lock = new Lock();
        private readonly List<PartBuilder> _conventions = new List<PartBuilder>();

        private readonly Dictionary<MemberInfo, List<Attribute>> _memberInfos = new Dictionary<MemberInfo, List<Attribute>>();
        private readonly Dictionary<ParameterInfo, List<Attribute>> _parameters = new Dictionary<ParameterInfo, List<Attribute>>();

        public RegistrationBuilder() : base(s_inner)
        {
        }

        public PartBuilder<T> ForTypesDerivedFrom<T>()
        {
            var partBuilder = new PartBuilder<T>((t) => typeof(T) != t && typeof(T).IsAssignableFrom(t));
            _conventions.Add(partBuilder);

            return partBuilder;
        }

        public PartBuilder ForTypesDerivedFrom(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var partBuilder = new PartBuilder((t) => type != t && type.IsAssignableFrom(t));
            _conventions.Add(partBuilder);

            return partBuilder;
        }

        public PartBuilder<T> ForType<T>()
        {
            var partBuilder = new PartBuilder<T>((t) => t == typeof(T));
            _conventions.Add(partBuilder);

            return partBuilder;
        }

        public PartBuilder ForType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var partBuilder = new PartBuilder((t) => t == type);
            _conventions.Add(partBuilder);

            return partBuilder;
        }

        public PartBuilder<T> ForTypesMatching<T>(Predicate<Type> typeFilter)
        {
            if (typeFilter == null)
                throw new ArgumentNullException(nameof(typeFilter));

            var partBuilder = new PartBuilder<T>(typeFilter);
            _conventions.Add(partBuilder);

            return partBuilder;
        }

        public PartBuilder ForTypesMatching(Predicate<Type> typeFilter)
        {
            if (typeFilter == null)
                throw new ArgumentNullException(nameof(typeFilter));

            var partBuilder = new PartBuilder(typeFilter);
            _conventions.Add(partBuilder);

            return partBuilder;
        }

        private IEnumerable<Tuple<object, List<Attribute>>> EvaluateThisTypeAgainstTheConvention(Type type)
        {
            List<Tuple<object, List<Attribute>>> results = new List<Tuple<object, List<Attribute>>>();
            List<Attribute> attributes = new List<Attribute>();

            var configuredMembers = new List<Tuple<object, List<Attribute>>>();
            bool specifiedConstructor = false;
            bool matchedConvention = false;

            foreach (PartBuilder builder in _conventions.Where(c => c.SelectType(type.UnderlyingSystemType)))
            {
                attributes.AddRange(builder.BuildTypeAttributes(type));

                specifiedConstructor |= builder.BuildConstructorAttributes(type, ref configuredMembers);
                builder.BuildPropertyAttributes(type, ref configuredMembers);
                matchedConvention = true;
            }

            if (matchedConvention && !specifiedConstructor)
            {
                // DefaultConstructor
                PartBuilder.BuildDefaultConstructorAttributes(type, ref configuredMembers);
            }

            configuredMembers.Add(Tuple.Create((object)type, attributes));

            return configuredMembers;
        }

        // Handle Type Exports and Parts
        protected override IEnumerable<object> GetCustomAttributes(System.Reflection.MemberInfo member, IEnumerable<object> declaredAttributes)
        {
            IEnumerable<object> attributes = base.GetCustomAttributes(member, declaredAttributes);

            // Now edit the attributes returned from the base type
            List<Attribute> cachedAttributes = null;

            if (member.MemberType == MemberTypes.TypeInfo || member.MemberType == MemberTypes.NestedType)
            {
                MemberInfo underlyingMemberType = ((Type)member).UnderlyingSystemType;
                using (new ReadLock(_lock))
                {
                    _memberInfos.TryGetValue(underlyingMemberType, out cachedAttributes);
                }

                if (cachedAttributes == null)
                {
                    using (new WriteLock(_lock))
                    {
                        //Double check locking another thread may have inserted one while we were away.
                        if (!_memberInfos.TryGetValue(underlyingMemberType, out cachedAttributes))
                        {
                            List<Attribute> attributeList;
                            foreach (Tuple<object, List<Attribute>> element in EvaluateThisTypeAgainstTheConvention((Type)member))
                            {
                                attributeList = element.Item2;
                                if (attributeList != null)
                                {
                                    if (element.Item1 is MemberInfo)
                                    {
                                        List<Attribute> memberAttributes;
                                        switch (((MemberInfo)element.Item1).MemberType)
                                        {
                                            case MemberTypes.Constructor:
                                                if (!_memberInfos.TryGetValue((MemberInfo)element.Item1, out memberAttributes))
                                                {
                                                    _memberInfos.Add((MemberInfo)element.Item1, element.Item2);
                                                }
                                                else
                                                {
                                                    memberAttributes.AddRange(attributeList);
                                                }
                                                break;
                                            case MemberTypes.TypeInfo:
                                            case MemberTypes.NestedType:
                                            case MemberTypes.Property:
                                                if (!_memberInfos.TryGetValue((MemberInfo)element.Item1, out memberAttributes))
                                                {
                                                    _memberInfos.Add((MemberInfo)element.Item1, element.Item2);
                                                }
                                                else
                                                {
                                                    memberAttributes.AddRange(attributeList);
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        if (!(element.Item1 is ParameterInfo))
                                            throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                                        // Item contains as Constructor parameter to configure
                                        if (!_parameters.TryGetValue((ParameterInfo)element.Item1, out List<Attribute> parameterAttributes))
                                        {
                                            _parameters.Add((ParameterInfo)element.Item1, element.Item2);
                                        }
                                        else
                                        {
                                            parameterAttributes.AddRange(cachedAttributes);
                                        }
                                    }
                                }
                            }
                        }

                        // We will have updated all of the MemberInfos by now so lets reload cachedAttributes wiuth the current store
                        _memberInfos.TryGetValue(underlyingMemberType, out cachedAttributes);
                    }
                }
            }
            else if (member.MemberType == System.Reflection.MemberTypes.Constructor || member.MemberType == System.Reflection.MemberTypes.Property)
            {
                cachedAttributes = ReadMemberCustomAttributes(member);
            }

            return cachedAttributes == null ? attributes : attributes.Concat(cachedAttributes);
        }

        //This is where ParameterImports will be handled
        protected override IEnumerable<object> GetCustomAttributes(System.Reflection.ParameterInfo parameter, IEnumerable<object> declaredAttributes)
        {
            IEnumerable<object> attributes = base.GetCustomAttributes(parameter, declaredAttributes);
            List<Attribute> cachedAttributes = ReadParameterCustomAttributes(parameter);

            return cachedAttributes == null ? attributes : attributes.Concat(cachedAttributes);
        }

        private List<Attribute> ReadMemberCustomAttributes(MemberInfo member)
        {
            List<Attribute> cachedAttributes = null;
            bool getMemberAttributes = false;

            // Now edit the attributes returned from the base type
            using (new ReadLock(_lock))
            {
                if (!_memberInfos.TryGetValue(member, out cachedAttributes))
                {
                    // If there is nothing for this member Cache any attributes for the DeclaringType
                    if (!_memberInfos.TryGetValue(member.DeclaringType.UnderlyingSystemType, out cachedAttributes))
                    {
                        // If there is nothing for this parameter look to see if the declaring Member has been cached yet?
                        // need to do it outside of the lock, so set the flag we'll check it in a bit
                        getMemberAttributes = true;
                    }

                    cachedAttributes = null;
                }
            }

            if (getMemberAttributes)
            {
                GetCustomAttributes(member.DeclaringType, s_emptyList);

                // We should have run the rules for the enclosing parameter so we can again
                using (new ReadLock(_lock))
                {
                    _memberInfos.TryGetValue(member, out cachedAttributes);
                }
            }

            return cachedAttributes;
        }

        private List<Attribute> ReadParameterCustomAttributes(ParameterInfo parameter)
        {
            List<Attribute> cachedAttributes = null;
            bool getMemberAttributes = false;

            // Now edit the attributes returned from the base type
            using (new ReadLock(_lock))
            {
                if (!_parameters.TryGetValue(parameter, out cachedAttributes))
                {
                    // If there is nothing for this parameter Cache any attributes for the DeclaringType
                    if (!_memberInfos.TryGetValue(parameter.Member.DeclaringType, out cachedAttributes))
                    {
                        // If there is nothing for this parameter look to see if the declaring Member has been cached yet?
                        // need to do it outside of the lock, so set the flag we'll check it in a bit
                        getMemberAttributes = true;
                    }
                    cachedAttributes = null;
                }
            }

            if (getMemberAttributes)
            {
                GetCustomAttributes(parameter.Member.DeclaringType, s_emptyList);

                // We should have run the rules for the enclosing parameter so we can again
                using (new ReadLock(_lock))
                {
                    _parameters.TryGetValue(parameter, out cachedAttributes);
                }
            }

            return cachedAttributes;
        }
    }
}
