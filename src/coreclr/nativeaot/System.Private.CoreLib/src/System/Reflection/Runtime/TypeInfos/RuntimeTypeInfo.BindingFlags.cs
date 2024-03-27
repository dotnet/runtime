// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Runtime.BindingFlagSupport;
using System.Reflection.Runtime.General;

namespace System.Reflection.Runtime.TypeInfos
{
    internal abstract partial class RuntimeTypeInfo
    {
        public ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => Query<ConstructorInfo>(ConstructorPolicies.Instance, bindingAttr).ToArray();

        public ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            Debug.Assert(types != null);

            QueryResult<ConstructorInfo> queryResult = Query<ConstructorInfo>(ConstructorPolicies.Instance, bindingAttr);
            ListBuilder<ConstructorInfo> candidates = default;
            foreach (ConstructorInfo candidate in queryResult)
            {
                if (candidate.QualifiesBasedOnParameterCount(bindingAttr, callConvention, types))
                    candidates.Add(candidate);
            }

            // For perf and desktop compat, fast-path these specific checks before calling on the binder to break ties.
            if (candidates.Count == 0)
                return null;

            if (types.Length == 0 && candidates.Count == 1)
            {
                ConstructorInfo firstCandidate = candidates[0];
                if (firstCandidate.GetParametersAsSpan().Length == 0)
                    return firstCandidate;
            }

            if ((bindingAttr & BindingFlags.ExactBinding) != 0)
                return System.DefaultBinder.ExactBinding(candidates.ToArray(), types) as ConstructorInfo;

            binder ??= Type.DefaultBinder;

            return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as ConstructorInfo;
        }

        public EventInfo[] GetEvents(BindingFlags bindingAttr) => Query<EventInfo>(EventPolicies.Instance, bindingAttr).ToArray();

        public EventInfo GetEvent(string name, BindingFlags bindingAttr) => Query<EventInfo>(EventPolicies.Instance, name, bindingAttr).Disambiguate();

        public FieldInfo[] GetFields(BindingFlags bindingAttr) => Query<FieldInfo>(FieldPolicies.Instance, bindingAttr).ToArray();

        public FieldInfo GetField(string name, BindingFlags bindingAttr) => Query<FieldInfo>(FieldPolicies.Instance, name, bindingAttr).Disambiguate();

        public MethodInfo[] GetMethods(BindingFlags bindingAttr) => Query<MethodInfo>(MethodPolicies.Instance, bindingAttr).ToArray();

        public MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            Debug.Assert(name != null);

            // GetMethodImpl() is a funnel for two groups of api. We can distinguish by comparing "types" to null.
            if (types == null)
            {
                // Group #1: This group of api accept only a name and BindingFlags. The other parameters are hard-wired by the non-virtual api entrypoints.
                Debug.Assert(genericParameterCount == GenericParameterCountAny);
                Debug.Assert(binder == null);
                Debug.Assert(callConvention == CallingConventions.Any);
                Debug.Assert(modifiers == null);
                return Query<MethodInfo>(MethodPolicies.Instance, name, bindingAttr).Disambiguate();
            }
            else
            {
                // Group #2: This group of api takes a set of parameter types and an optional binder.
                QueryResult<MethodInfo> queryResult = Query<MethodInfo>(MethodPolicies.Instance, name, bindingAttr);
                ListBuilder<MethodInfo> candidates = default;
                foreach (MethodInfo candidate in queryResult)
                {
                    if (genericParameterCount != GenericParameterCountAny && genericParameterCount != candidate.GenericParameterCount)
                        continue;
                    if (candidate.QualifiesBasedOnParameterCount(bindingAttr, callConvention, types))
                        candidates.Add(candidate);
                }

                if (candidates.Count == 0)
                    return null;

                // For perf and desktop compat, fast-path these specific checks before calling on the binder to break ties.
                if (types.Length == 0 && candidates.Count == 1)
                    return candidates[0];

                binder ??= Type.DefaultBinder;

                return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as MethodInfo;
            }
        }

        public Type[] GetNestedTypes(BindingFlags bindingAttr) => Query<Type>(NestedTypePolicies.Instance, bindingAttr).ToArray();

        public Type GetNestedType(string name, BindingFlags bindingAttr) => Query<Type>(NestedTypePolicies.Instance, name, bindingAttr).Disambiguate();

        public PropertyInfo[] GetProperties(BindingFlags bindingAttr) => Query<PropertyInfo>(PropertyPolicies.Instance, bindingAttr).ToArray();

        public PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            Debug.Assert(name != null);

            // GetPropertyImpl() is a funnel for two groups of api. We can distinguish by comparing "types" to null.
            if (types == null && returnType == null)
            {
                // Group #1: This group of api accept only a name and BindingFlags. The other parameters are hard-wired by the non-virtual api entrypoints.
                Debug.Assert(binder == null);
                Debug.Assert(modifiers == null);
                return Query<PropertyInfo>(PropertyPolicies.Instance, name, bindingAttr).Disambiguate();
            }
            else
            {
                // Group #2: This group of api takes a set of parameter types, a return type (both cannot be null) and an optional binder.
                QueryResult<PropertyInfo> queryResult = Query<PropertyInfo>(PropertyPolicies.Instance, name, bindingAttr);
                ListBuilder<PropertyInfo> candidates = default;
                foreach (PropertyInfo candidate in queryResult)
                {
                    if (types == null || (candidate.GetIndexParameters().Length == types.Length))
                    {
                        candidates.Add(candidate);
                    }
                }

                if (candidates.Count == 0)
                    return null;

                // For perf and desktop compat, fast-path these specific checks before calling on the binder to break ties.
                if (types == null || types.Length == 0)
                {
                    // no arguments
                    PropertyInfo firstCandidate = candidates[0];

                    if (candidates.Count == 1)
                    {
                        if (returnType is not null && !returnType.IsEquivalentTo(firstCandidate.PropertyType))
                            return null;
                        return firstCandidate;
                    }
                    else
                    {
                        if (returnType is null)
                        {
                            // if we are here we have no args or property type to select over and we have more than one property with that name
                            throw ThrowHelper.GetAmbiguousMatchException(firstCandidate);
                        }
                    }
                }

                if ((bindingAttr & BindingFlags.ExactBinding) != 0)
                    return DefaultBinder.ExactPropertyBinding(candidates.ToArray(), returnType, types);

                binder ??= Type.DefaultBinder;

                return binder.SelectProperty(bindingAttr, candidates.ToArray(), returnType, types, modifiers);
            }
        }

        private QueryResult<M> Query<M>(MemberPolicies<M> policies, BindingFlags bindingAttr) where M : MemberInfo
        {
            return Query<M>(policies, null, bindingAttr, null);
        }

        private QueryResult<M> Query<M>(MemberPolicies<M> policies, string name, BindingFlags bindingAttr) where M : MemberInfo
        {
            ArgumentNullException.ThrowIfNull(name);
            return Query<M>(policies, name, bindingAttr, null);
        }

        private QueryResult<M> Query<M>(MemberPolicies<M> policies, string optionalName, BindingFlags bindingAttr, Func<M, bool> optionalPredicate) where M : MemberInfo
        {
            bindingAttr = policies.ModifyBindingFlags(bindingAttr);
            bool ignoreCase = (bindingAttr & BindingFlags.IgnoreCase) != 0;

            TypeComponentsCache cache = Cache;
            QueriedMemberList<M> queriedMembers;
            if (optionalName == null)
                queriedMembers = cache.GetQueriedMembers(policies);
            else
                queriedMembers = cache.GetQueriedMembers<M>(policies, optionalName, ignoreCase: ignoreCase);

            if (optionalPredicate != null)
                queriedMembers = queriedMembers.Filter(optionalPredicate);
            return new QueryResult<M>(policies, bindingAttr, queriedMembers);
        }

        private TypeComponentsCache Cache => _lazyCache ??= new TypeComponentsCache(this);

        // Generic cache for scenario specific data. For example, it is used to cache Enum names and values.
        internal object? GenericCache
        {
            get => _lazyCache?._genericCache;
            set => Cache._genericCache = value;
        }

        private volatile TypeComponentsCache? _lazyCache;

        public const int GenericParameterCountAny = -1;
    }
}
