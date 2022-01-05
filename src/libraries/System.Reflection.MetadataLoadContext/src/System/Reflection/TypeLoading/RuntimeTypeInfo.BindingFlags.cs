// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Runtime.BindingFlagSupport;
using System.Reflection.Runtime.General;

namespace System.Reflection.TypeLoading
{
    internal abstract partial class RoType
    {
        public sealed override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => Query<ConstructorInfo>(bindingAttr).ToArray();

        protected sealed override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            Debug.Assert(types != null);

            QueryResult<ConstructorInfo> queryResult = Query<ConstructorInfo>(bindingAttr);
            ListBuilder<ConstructorInfo> candidates = default;
            foreach (ConstructorInfo candidate in queryResult)
            {
                if (candidate.QualifiesBasedOnParameterCount(bindingAttr, callConvention, types))
                    candidates.Add(candidate);
            }

            // For perf and .NET Framework compat, fast-path these specific checks before calling on the binder to break ties.
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
                return System.DefaultBinder.ExactBinding(candidates.ToArray(), types, modifiers) as ConstructorInfo;

            if (binder == null)
                binder = Loader.GetDefaultBinder();

            return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as ConstructorInfo;
        }

        public sealed override EventInfo[] GetEvents(BindingFlags bindingAttr) => Query<EventInfo>(bindingAttr).ToArray();
        public sealed override EventInfo? GetEvent(string name, BindingFlags bindingAttr) => Query<EventInfo>(name, bindingAttr).Disambiguate();

        public sealed override FieldInfo[] GetFields(BindingFlags bindingAttr) => Query<FieldInfo>(bindingAttr).ToArray();
        public sealed override FieldInfo? GetField(string name, BindingFlags bindingAttr) => Query<FieldInfo>(name, bindingAttr).Disambiguate();

        public sealed override MethodInfo[] GetMethods(BindingFlags bindingAttr) => Query<MethodInfo>(bindingAttr).ToArray();

        protected sealed override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            return GetMethodImplCommon(name, GenericParameterCountAny, bindingAttr, binder, callConvention, types, modifiers);
        }

        protected sealed override MethodInfo? GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            return GetMethodImplCommon(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers);
        }

        private MethodInfo? GetMethodImplCommon(string name, int genericParameterCount, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
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
                ListBuilder<MethodInfo> candidates = default;
                foreach (MethodInfo candidate in queryResult)
                {
                    if (genericParameterCount != GenericParameterCountAny && genericParameterCount != candidate.GetGenericParameterCount())
                        continue;
                    if (candidate.QualifiesBasedOnParameterCount(bindingAttr, callConvention, types))
                        candidates.Add(candidate);
                }

                if (candidates.Count == 0)
                    return null;

                // For perf and .NET Framework compat, fast-path these specific checks before calling on the binder to break ties.
                if (types.Length == 0 && candidates.Count == 1)
                    return candidates[0];

                if (binder == null)
                    binder = Loader.GetDefaultBinder();
                return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as MethodInfo;
            }
        }

        public sealed override Type[] GetNestedTypes(BindingFlags bindingAttr) => Query<Type>(bindingAttr).ToArray();
        public sealed override Type? GetNestedType(string name, BindingFlags bindingAttr) => Query<Type>(name, bindingAttr).Disambiguate();

        public sealed override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => Query<PropertyInfo>(bindingAttr).ToArray();

        protected sealed override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
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

                // For perf and .NET Framework compat, fast-path these specific checks before calling on the binder to break ties.
                if (types == null || types.Length == 0)
                {
                    // no arguments
                    if (candidates.Count == 1)
                    {
                        PropertyInfo firstCandidate = candidates[0];
                        if (!(returnType is null) && !returnType.IsEquivalentTo(firstCandidate.PropertyType))
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
                    return System.DefaultBinder.ExactPropertyBinding(candidates.ToArray(), returnType, types, modifiers);

                if (binder == null)
                    binder = Loader.GetDefaultBinder();

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

        private QueryResult<M> Query<M>(string? optionalName, BindingFlags bindingAttr, Func<M, bool>? optionalPredicate) where M : MemberInfo
        {
            MemberPolicies<M> policies = MemberPolicies<M>.Default;
            bindingAttr = policies.ModifyBindingFlags(bindingAttr);
            bool immediateTypeOnly = NeedToSearchImmediateTypeOnly(bindingAttr);
            bool ignoreCase = (bindingAttr & BindingFlags.IgnoreCase) != 0;

            TypeComponentsCache cache = Cache;
            QueriedMemberList<M> queriedMembers;
            if (optionalName == null)
                queriedMembers = cache.GetQueriedMembers<M>(immediateTypeOnly);
            else
                queriedMembers = cache.GetQueriedMembers<M>(optionalName, ignoreCase: ignoreCase, immediateTypeOnly: immediateTypeOnly);

            if (optionalPredicate != null)
                queriedMembers = queriedMembers.Filter(optionalPredicate);
            return new QueryResult<M>(bindingAttr, queriedMembers);
        }

        private static bool NeedToSearchImmediateTypeOnly(BindingFlags bf)
        {
            if ((bf & (BindingFlags.Static | BindingFlags.FlattenHierarchy)) == (BindingFlags.Static | BindingFlags.FlattenHierarchy))
                return false;

            if ((bf & (BindingFlags.Instance | BindingFlags.DeclaredOnly)) == BindingFlags.Instance)
                return false;

            return true;
        }

        private TypeComponentsCache Cache => _lazyCache ?? (_lazyCache = new TypeComponentsCache(this));

        private volatile TypeComponentsCache? _lazyCache;

        private const int GenericParameterCountAny = -1;
    }
}
