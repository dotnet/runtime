// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace System.Composition.Convention
{
    /// <summary>
    /// Entry point for defining rules that configure plain-old-CLR-objects as MEF parts.
    /// </summary>
    public class ConventionBuilder : AttributedModelProvider
    {
        private static readonly List<object> s_emptyList = new List<object>();

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly List<PartConventionBuilder> _conventions = new List<PartConventionBuilder>();

        private readonly Dictionary<MemberInfo, List<Attribute>> _memberInfos = new Dictionary<MemberInfo, List<Attribute>>();
        private readonly Dictionary<ParameterInfo, List<Attribute>> _parameters = new Dictionary<ParameterInfo, List<Attribute>>();

        /// <summary>
        /// Construct a new <see cref="ConventionBuilder"/>.
        /// </summary>
        public ConventionBuilder()
        {
        }

        /// <summary>
        /// Define a rule that will apply to all types that
        /// derive from (or implement) the specified type.
        /// </summary>
        /// <typeparam name="T">The type from which matching types derive.</typeparam>
        /// <returns>A <see cref="PartConventionBuilder{T}"/> that must be used to specify the rule.</returns>
        public PartConventionBuilder<T> ForTypesDerivedFrom<T>()
        {
            var partBuilder = new PartConventionBuilder<T>((t) => IsDescendentOf(t, typeof(T)));
            _conventions.Add(partBuilder);
            return partBuilder;
        }

        /// <summary>
        /// Define a rule that will apply to all types that
        /// derive from (or implement) the specified type.
        /// </summary>
        /// <param name="type">The type from which matching types derive.</param>
        /// <returns>A <see cref="PartConventionBuilder"/> that must be used to specify the rule.</returns>
        public PartConventionBuilder ForTypesDerivedFrom(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var partBuilder = new PartConventionBuilder((t) => IsDescendentOf(t, type));
            _conventions.Add(partBuilder);
            return partBuilder;
        }

        /// <summary>
        /// Define a rule that will apply to the types <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to which the rule applies.</typeparam>
        /// <returns>A <see cref="PartConventionBuilder{T}"/> that must be used to specify the rule.</returns>
        public PartConventionBuilder<T> ForType<T>()
        {
            var partBuilder = new PartConventionBuilder<T>((t) => t == typeof(T));
            _conventions.Add(partBuilder);
            return partBuilder;
        }

        /// <summary>
        /// Define a rule that will apply to the types <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to which the rule applies.</param>
        /// <returns>A <see cref="PartConventionBuilder"/> that must be used to specify the rule.</returns>
        public PartConventionBuilder ForType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var partBuilder = new PartConventionBuilder((t) => t == type);
            _conventions.Add(partBuilder);
            return partBuilder;
        }

        /// <summary>
        /// Define a rule that will apply to types assignable to <typeparamref name="T"/> that
        /// match the supplied predicate.
        /// </summary>
        /// <param name="typeFilter">A predicate that selects matching types.</param>
        /// <typeparam name="T">The type to which the rule applies.</typeparam>
        /// <returns>A <see cref="PartConventionBuilder{T}"/> that must be used to specify the rule.</returns>
        public PartConventionBuilder<T> ForTypesMatching<T>(Predicate<Type> typeFilter)
        {
            if (typeFilter == null)
            {
                throw new ArgumentNullException(nameof(typeFilter));
            }

            var partBuilder = new PartConventionBuilder<T>(typeFilter);
            _conventions.Add(partBuilder);
            return partBuilder;
        }

        /// <summary>
        /// Define a rule that will apply to types that
        /// match the supplied predicate.
        /// </summary>
        /// <param name="typeFilter">A predicate that selects matching types.</param>
        /// <returns>A <see cref="PartConventionBuilder{T}"/> that must be used to specify the rule.</returns>
        public PartConventionBuilder ForTypesMatching(Predicate<Type> typeFilter)
        {
            if (typeFilter == null)
            {
                throw new ArgumentNullException(nameof(typeFilter));
            }

            var partBuilder = new PartConventionBuilder(typeFilter);
            _conventions.Add(partBuilder);
            return partBuilder;
        }

        private IEnumerable<Tuple<object, List<Attribute>>> EvaluateThisTypeInfoAgainstTheConvention(TypeInfo typeInfo)
        {
            List<Tuple<object, List<Attribute>>> results = new List<Tuple<object, List<Attribute>>>();
            List<Attribute> attributes = new List<Attribute>();
            var configuredMembers = new List<Tuple<object, List<Attribute>>>();
            bool specifiedConstructor = false;
            bool matchedConvention = false;
            Type type = typeInfo.AsType();

            foreach (PartConventionBuilder builder in _conventions.Where(c => c.SelectType(type)))
            {
                attributes.AddRange(builder.BuildTypeAttributes(type));

                specifiedConstructor |= builder.BuildConstructorAttributes(type, ref configuredMembers);
                builder.BuildPropertyAttributes(type, ref configuredMembers);
                builder.BuildOnImportsSatisfiedNotification(type, ref configuredMembers);

                matchedConvention = true;
            }
            if (matchedConvention && !specifiedConstructor)
            {
                // DefaultConstructor
                PartConventionBuilder.BuildDefaultConstructorAttributes(type, ref configuredMembers);
            }
            configuredMembers.Add(Tuple.Create((object)type.GetTypeInfo(), attributes));
            return configuredMembers;
        }

        /// <summary>
        /// Provide the list of attributes applied to the specified member.
        /// </summary>
        /// <param name="reflectedType">The reflectedType the type used to retrieve the memberInfo.</param>
        /// <param name="member">The member to supply attributes for.</param>
        /// <returns>The list of applied attributes.</returns>
        public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, System.Reflection.MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            // Now edit the attributes returned from the base type
            List<Attribute> cachedAttributes = null;
            var typeInfo = member as TypeInfo;
            if (typeInfo != null)
            {
                var memberInfo = typeInfo as MemberInfo;
                _lock.EnterReadLock();
                try
                {
                    _memberInfos.TryGetValue(memberInfo, out cachedAttributes);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
                if (cachedAttributes == null)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        //Double check locking another thread may have inserted one while we were away.
                        if (!_memberInfos.TryGetValue(memberInfo, out cachedAttributes))
                        {
                            List<Attribute> attributeList;
                            foreach (Tuple<object, List<Attribute>> element in EvaluateThisTypeInfoAgainstTheConvention(typeInfo))
                            {
                                attributeList = element.Item2;
                                if (attributeList != null)
                                {
                                    var mi = element.Item1 as MemberInfo;
                                    if (mi != null)
                                    {
                                        if (mi != null && (mi is ConstructorInfo || mi is TypeInfo || mi is PropertyInfo || mi is MethodInfo))
                                        {
                                            if (!_memberInfos.TryGetValue(mi, out List<Attribute> memberAttributes))
                                            {
                                                _memberInfos.Add(mi, element.Item2);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var pi = element.Item1 as ParameterInfo;
                                        if (pi == null)
                                        {
                                            throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                                        }

                                        // Item contains as Constructor parameter to configure
                                        if (!_parameters.TryGetValue(pi, out List<Attribute> parameterAttributes))
                                        {
                                            _parameters.Add(pi, element.Item2);
                                        }
                                    }
                                }
                            }
                        }

                        // We will have updated all of the MemberInfos by now so lets reload cachedAttributes with the current store
                        _memberInfos.TryGetValue(memberInfo, out cachedAttributes);
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            else if (member is PropertyInfo || member is ConstructorInfo || member is MethodInfo)
            {
                cachedAttributes = ReadMemberCustomAttributes(reflectedType, member);
            }

            IEnumerable<Attribute> appliedAttributes;
            if (!(member is TypeInfo) && member.DeclaringType != reflectedType)
                appliedAttributes = Enumerable.Empty<Attribute>();
            else
                appliedAttributes = member.GetCustomAttributes<Attribute>(false);

            return cachedAttributes == null ? appliedAttributes : appliedAttributes.Concat(cachedAttributes);
        }

        private List<Attribute> ReadMemberCustomAttributes(Type reflectedType, System.Reflection.MemberInfo member)
        {
            List<Attribute> cachedAttributes = null;
            bool getMemberAttributes = false;

            // Now edit the attributes returned from the base type
            _lock.EnterReadLock();
            try
            {
                if (!_memberInfos.TryGetValue(member, out cachedAttributes))
                {
                    // If there is nothing for this member Cache any attributes for the DeclaringType
                    if (reflectedType != null
                        && !_memberInfos.TryGetValue(member.DeclaringType.GetTypeInfo() as MemberInfo, out cachedAttributes))
                    {
                        // If there is nothing for this parameter look to see if the declaring Member has been cached yet?
                        // need to do it outside of the lock, so set the flag we'll check it in a bit
                        getMemberAttributes = true;
                    }
                    cachedAttributes = null;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (getMemberAttributes)
            {
                GetCustomAttributes(null, reflectedType.GetTypeInfo() as MemberInfo);

                // We should have run the rules for the enclosing parameter so we can again
                _lock.EnterReadLock();
                try
                {
                    _memberInfos.TryGetValue(member, out cachedAttributes);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return cachedAttributes;
        }

        /// <summary>
        /// Provide the list of attributes applied to the specified parameter.
        /// </summary>
        /// <param name="reflectedType">The reflectedType the type used to retrieve the parameterInfo.</param>
        /// <param name="parameter">The parameter to supply attributes for.</param>
        /// <returns>The list of applied attributes.</returns>
        public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, System.Reflection.ParameterInfo parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            IEnumerable<Attribute> attributes = parameter.GetCustomAttributes<Attribute>(false);
            List<Attribute> cachedAttributes = ReadParameterCustomAttributes(reflectedType, parameter);
            return cachedAttributes == null ? attributes : attributes.Concat(cachedAttributes);
        }

        private List<Attribute> ReadParameterCustomAttributes(Type reflectedType, System.Reflection.ParameterInfo parameter)
        {
            List<Attribute> cachedAttributes = null;
            bool getMemberAttributes = false;

            // Now edit the attributes returned from the base type
            _lock.EnterReadLock();
            try
            {
                if (!_parameters.TryGetValue(parameter, out cachedAttributes))
                {
                    // If there is nothing for this parameter Cache any attributes for the DeclaringType
                    if (reflectedType != null
                     && !_memberInfos.TryGetValue(reflectedType.GetTypeInfo() as MemberInfo, out cachedAttributes))
                    {
                        // If there is nothing for this parameter look to see if the declaring Member has been cached yet?
                        // need to do it outside of the lock, so set the flag we'll check it in a bit
                        getMemberAttributes = true;
                    }
                    cachedAttributes = null;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (getMemberAttributes)
            {
                GetCustomAttributes(null, reflectedType.GetTypeInfo() as MemberInfo);

                // We should have run the rules for the enclosing parameter so we can again
                _lock.EnterReadLock();
                try
                {
                    _parameters.TryGetValue(parameter, out cachedAttributes);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            return cachedAttributes;
        }

        private static bool IsGenericDescendentOf(TypeInfo derivedType, TypeInfo baseType)
        {
            if (derivedType.BaseType == null)
                return false;

            if (derivedType.BaseType == baseType.AsType())
                return true;

            foreach (Type iface in derivedType.ImplementedInterfaces)
            {
                if (iface.IsConstructedGenericType &&
                    iface.GetGenericTypeDefinition() == baseType.AsType())
                    return true;
            }

            return IsGenericDescendentOf(derivedType.BaseType.GetTypeInfo(), baseType);
        }

        private static bool IsDescendentOf(Type type, Type baseType)
        {
            if (type == baseType || type == typeof(object) || type == null)
            {
                return false;
            }

            TypeInfo ti = type.GetTypeInfo();
            TypeInfo bti = baseType.GetTypeInfo();

            // The baseType can be an open generic, in that case this ensures
            // that the derivedType is checked against it
            if (ti.IsGenericTypeDefinition || bti.IsGenericTypeDefinition)
            {
                return IsGenericDescendentOf(ti, bti);
            }

            return bti.IsAssignableFrom(ti);
        }
    }
}
