// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection.Runtime.BindingFlagSupport;

namespace System.Reflection.Runtime.TypeInfos
{
    internal abstract partial class RuntimeTypeInfo
    {
        [DynamicallyAccessedMembers(GetAllMembers)]
        public sealed override MemberInfo[] GetMembers(BindingFlags bindingAttr) => GetMemberImpl(null, MemberTypes.All, bindingAttr);

        [DynamicallyAccessedMembers(GetAllMembers)]
        public sealed override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            return GetMemberImpl(name, MemberTypes.All, bindingAttr);
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public sealed override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            return GetMemberImpl(name, type, bindingAttr);
        }

        private MemberInfo[] GetMemberImpl(string optionalNameOrPrefix, MemberTypes type, BindingFlags bindingAttr)
        {
            bool prefixSearch = optionalNameOrPrefix != null && optionalNameOrPrefix.EndsWith("*", StringComparison.Ordinal);
            string? optionalName = prefixSearch ? null : optionalNameOrPrefix;

            Func<MemberInfo, bool>? predicate = null;
            if (prefixSearch)
            {
                bool ignoreCase = (bindingAttr & BindingFlags.IgnoreCase) != 0;
                StringComparison comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                string prefix = optionalNameOrPrefix.Substring(0, optionalNameOrPrefix.Length - 1);

                predicate = (member => member.Name.StartsWith(prefix, comparisonType));
            }

            QueryResult<MethodInfo> methods;
            QueryResult<ConstructorInfo> constructors;
            QueryResult<PropertyInfo> properties;
            QueryResult<EventInfo> events;
            QueryResult<FieldInfo> fields;
            QueryResult<Type> nestedTypes;

            MemberInfo[] results;

            if ((results = QuerySpecificMemberTypeIfRequested(type, optionalName, bindingAttr, predicate, MemberTypes.Method, out methods)) != null)
                return results;
            if ((results = QuerySpecificMemberTypeIfRequested(type, optionalName, bindingAttr, predicate, MemberTypes.Constructor, out constructors)) != null)
                return results;
            if ((results = QuerySpecificMemberTypeIfRequested(type, optionalName, bindingAttr, predicate, MemberTypes.Property, out properties)) != null)
                return results;
            if ((results = QuerySpecificMemberTypeIfRequested(type, optionalName, bindingAttr, predicate, MemberTypes.Event, out events)) != null)
                return results;
            if ((results = QuerySpecificMemberTypeIfRequested(type, optionalName, bindingAttr, predicate, MemberTypes.Field, out fields)) != null)
                return results;
            if ((results = QuerySpecificMemberTypeIfRequested(type, optionalName, bindingAttr, predicate, MemberTypes.NestedType, out nestedTypes)) != null)
                return results;
            if ((type & (MemberTypes.NestedType | MemberTypes.TypeInfo)) == MemberTypes.TypeInfo)
            {
                if ((results = QuerySpecificMemberTypeIfRequested(type, optionalName, bindingAttr, predicate, MemberTypes.TypeInfo, out nestedTypes)) != null)
                    return results;
            }

            int numMatches = methods.Count + constructors.Count + properties.Count + events.Count + fields.Count + nestedTypes.Count;
            results = (type == (MemberTypes.Method | MemberTypes.Constructor)) ? new MethodBase[numMatches] : new MemberInfo[numMatches];
            int numCopied = 0;

            methods.CopyTo(results, numCopied);
            numCopied += methods.Count;

            constructors.CopyTo(results, numCopied);
            numCopied += constructors.Count;

            properties.CopyTo(results, numCopied);
            numCopied += properties.Count;

            events.CopyTo(results, numCopied);
            numCopied += events.Count;

            fields.CopyTo(results, numCopied);
            numCopied += fields.Count;

            nestedTypes.CopyTo(results, numCopied);
            numCopied += nestedTypes.Count;

            Debug.Assert(numCopied == numMatches);

            return results;
        }

        private M[] QuerySpecificMemberTypeIfRequested<M>(MemberTypes memberType, string optionalName, BindingFlags bindingAttr, Func<MemberInfo, bool> optionalPredicate, MemberTypes targetMemberType, out QueryResult<M> queryResult) where M : MemberInfo
        {
            if ((memberType & targetMemberType) == 0)
            {
                // This type of member was not requested.
                queryResult = default(QueryResult<M>);
                return null;
            }

            queryResult = Query<M>(optionalName, bindingAttr, optionalPredicate);

            // Desktop compat: If exactly one type of member was requested, the returned array has to be of that specific type (M[], not MemberInfo[]). Create it now and return it
            // to cause GetMember() to short-cut the search.
            if ((memberType & ~targetMemberType) == 0)
                return queryResult.ToArray();

            // Desktop compat: If we got here, than one MemberType was requested. Return null to signal GetMember() to keep querying the other member types and concatenate the results.
            return null;
        }

        public sealed override MemberInfo GetMemberWithSameMetadataDefinitionAs(MemberInfo member)
        {
            if (member is null)
                throw new ArgumentNullException(nameof(member));

            // Need to walk up the inheritance chain if member is not found
            // Leverage the existing cache mechanism on per type to store members
            RuntimeTypeInfo? runtimeType = this;
            while (runtimeType != null)
            {
                MemberInfo result = runtimeType.GetDeclaredMemberWithSameMetadataDefinitionAs(member);
                if (result != null)
                    return result;
                runtimeType = runtimeType.BaseType as RuntimeTypeInfo;
            }
            throw new ArgumentException(SR.Format(SR.Arg_MemberInfoNotFound, member.Name), nameof(member));
        }

        private MemberInfo GetDeclaredMemberWithSameMetadataDefinitionAs(MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.Method => QueryMemberWithSameMetadataDefinitionAs<MethodInfo>(member),
                MemberTypes.Constructor => QueryMemberWithSameMetadataDefinitionAs<ConstructorInfo>(member),
                MemberTypes.Property => QueryMemberWithSameMetadataDefinitionAs<PropertyInfo>(member),
                MemberTypes.Field => QueryMemberWithSameMetadataDefinitionAs<FieldInfo>(member),
                MemberTypes.Event => QueryMemberWithSameMetadataDefinitionAs<EventInfo>(member),
                MemberTypes.NestedType => QueryMemberWithSameMetadataDefinitionAs<Type>(member),
                _ => null,
            };
        }

        private M QueryMemberWithSameMetadataDefinitionAs<M>(MemberInfo member) where M : MemberInfo
        {
            QueryResult<M> members = Query<M>(member.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (M candidate in members)
            {
                if (candidate.HasSameMetadataDefinitionAs(member))
                    return candidate;
            }
            return null;
        }
    }
}
