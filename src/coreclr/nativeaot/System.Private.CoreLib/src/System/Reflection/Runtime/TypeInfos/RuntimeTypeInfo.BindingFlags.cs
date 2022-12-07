// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.BindingFlagSupport;

namespace System.Reflection.Runtime.TypeInfos
{
    internal abstract partial class RuntimeTypeInfo
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public sealed override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => Query<ConstructorInfo>(bindingAttr).ToArray();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected sealed override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            Debug.Assert(types != null);

            QueryResult<ConstructorInfo> queryResult = Query<ConstructorInfo>(bindingAttr);
            ListBuilder<ConstructorInfo> candidates = new ListBuilder<ConstructorInfo>();
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
                ParameterInfo[] parameters = firstCandidate.GetParametersNoCopy();
                if (parameters.Length == 0)
                    return firstCandidate;
            }

            if ((bindingAttr & BindingFlags.ExactBinding) != 0)
                return System.DefaultBinder.ExactBinding(candidates.ToArray(), types) as ConstructorInfo;

            binder ??= DefaultBinder;

            return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as ConstructorInfo;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public sealed override EventInfo[] GetEvents(BindingFlags bindingAttr) => Query<EventInfo>(bindingAttr).ToArray();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public sealed override EventInfo GetEvent(string name, BindingFlags bindingAttr) => Query<EventInfo>(name, bindingAttr).Disambiguate();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public sealed override FieldInfo[] GetFields(BindingFlags bindingAttr) => Query<FieldInfo>(bindingAttr).ToArray();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public sealed override FieldInfo GetField(string name, BindingFlags bindingAttr) => Query<FieldInfo>(name, bindingAttr).Disambiguate();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public sealed override MethodInfo[] GetMethods(BindingFlags bindingAttr) => Query<MethodInfo>(bindingAttr).ToArray();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected sealed override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            return GetMethodImplCommon(name, GenericParameterCountAny, bindingAttr, binder, callConvention, types, modifiers);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected sealed override MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            return GetMethodImplCommon(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers);
        }

        private MethodInfo GetMethodImplCommon(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
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
                return Query<MethodInfo>(name, bindingAttr).Disambiguate();
            }
            else
            {
                // Group #2: This group of api takes a set of parameter types and an optional binder.
                QueryResult<MethodInfo> queryResult = Query<MethodInfo>(name, bindingAttr);
                ListBuilder<MethodInfo> candidates = new ListBuilder<MethodInfo>();
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

                binder ??= DefaultBinder;

                return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as MethodInfo;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public sealed override Type[] GetNestedTypes(BindingFlags bindingAttr) => Query<Type>(bindingAttr).ToArray();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public sealed override Type GetNestedType(string name, BindingFlags bindingAttr) => Query<Type>(name, bindingAttr).Disambiguate();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public sealed override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => Query<PropertyInfo>(bindingAttr).ToArray();

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected sealed override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            Debug.Assert(name != null);

            // GetPropertyImpl() is a funnel for two groups of api. We can distinguish by comparing "types" to null.
            if (types == null && returnType == null)
            {
                // Group #1: This group of api accept only a name and BindingFlags. The other parameters are hard-wired by the non-virtual api entrypoints.
                Debug.Assert(binder == null);
                Debug.Assert(modifiers == null);
                return Query<PropertyInfo>(name, bindingAttr).Disambiguate();
            }
            else
            {
                // Group #2: This group of api takes a set of parameter types, a return type (both cannot be null) and an optional binder.
                QueryResult<PropertyInfo> queryResult = Query<PropertyInfo>(name, bindingAttr);
                ListBuilder<PropertyInfo> candidates = new ListBuilder<PropertyInfo>();
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
                    if (candidates.Count == 1)
                    {
                        PropertyInfo firstCandidate = candidates[0];
                        if (returnType is not null && !returnType.IsEquivalentTo(firstCandidate.PropertyType))
                            return null;
                        return firstCandidate;
                    }
                    else
                    {
                        if (returnType is null)
                            // if we are here we have no args or property type to select over and we have more than one property with that name
                            throw new AmbiguousMatchException();
                    }
                }

                if ((bindingAttr & BindingFlags.ExactBinding) != 0)
                    return System.DefaultBinder.ExactPropertyBinding(candidates.ToArray(), returnType, types);

                binder ??= DefaultBinder;

                return binder.SelectProperty(bindingAttr, candidates.ToArray(), returnType, types, modifiers);
            }
        }

        private QueryResult<M> Query<M>(BindingFlags bindingAttr) where M : MemberInfo
        {
            return Query<M>(null, bindingAttr, null);
        }

        private QueryResult<M> Query<M>(string name, BindingFlags bindingAttr) where M : MemberInfo
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            return Query<M>(name, bindingAttr, null);
        }

        private QueryResult<M> Query<M>(string optionalName, BindingFlags bindingAttr, Func<M, bool> optionalPredicate) where M : MemberInfo
        {
            MemberPolicies<M> policies = MemberPolicies<M>.Default;
            bindingAttr = policies.ModifyBindingFlags(bindingAttr);
            bool ignoreCase = (bindingAttr & BindingFlags.IgnoreCase) != 0;

            TypeComponentsCache cache = Cache;
            QueriedMemberList<M> queriedMembers;
            if (optionalName == null)
                queriedMembers = cache.GetQueriedMembers<M>();
            else
                queriedMembers = cache.GetQueriedMembers<M>(optionalName, ignoreCase: ignoreCase);

            if (optionalPredicate != null)
                queriedMembers = queriedMembers.Filter(optionalPredicate);
            return new QueryResult<M>(bindingAttr, queriedMembers);
        }

        private TypeComponentsCache Cache => _lazyCache ??= new TypeComponentsCache(this);

        // Generic cache for scenario specific data. For example, it is used to cache Enum names and values.
        // TODO: This cache should be attached to the RuntimeType via weak reference, similar to how it is done in CoreCLR.
        internal object? GenericCache
        {
            get => _lazyCache?._genericCache;
            set => Cache._genericCache = value;
        }

        private volatile TypeComponentsCache? _lazyCache;

        private const int GenericParameterCountAny = -1;
    }
}
