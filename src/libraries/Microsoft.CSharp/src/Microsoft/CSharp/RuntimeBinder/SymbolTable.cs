// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder.Semantics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal static class SymbolTable
    {
        private const BindingFlags EverythingBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static readonly HashSet<Type> s_typesWithConversionsLoaded = new HashSet<Type>();
        private static readonly HashSet<NameHashKey> s_namesLoadedForEachType = new HashSet<NameHashKey>();

        private readonly struct NameHashKey : IEquatable<NameHashKey>
        {
            internal Type Type { get; }
            internal string Name { get; }

            public NameHashKey(Type type, string name)
            {
                Type = type;
                Name = name;
            }

            public bool Equals(NameHashKey other) => Type.Equals(other.Type) && Name.Equals(other.Name);

#if  DEBUG
            [ExcludeFromCodeCoverage(Justification = "Typed overload should always be the method called")]
#endif
            public override bool Equals(object obj)
            {
                Debug.Fail("Sub-optimal overload called. Check if this can be avoided.");
                return obj is NameHashKey key && Equals(key);
            }

            public override int GetHashCode() => Type.GetHashCode() ^ Name.GetHashCode();
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal static void PopulateSymbolTableWithName(
            string name,
            IEnumerable<Type> typeArguments,
            Type callingType)
        {
            // The first argument is the object that we're calling off of.
            if (callingType.IsGenericType)
            {
                callingType = callingType.GetGenericTypeDefinition();
            }

            if (name == SpecialNames.Indexer)
            {
                // If we don't find an indexer name for this type, use SpecialNames.Indexer as a key on the
                // empty results we'll get, so that those empty results gets cached.
                name = callingType.GetIndexerName() ?? SpecialNames.Indexer;
            }

            NameHashKey key = new NameHashKey(callingType, name);

            // If we've already populated this name/type pair, then just leave.
            if (s_namesLoadedForEachType.Contains(key))
            {
                return;
            }

            // Add the names.
            AddNamesOnType(key);

            // Take each type argument and load its conversions into the symbol table.
            if (typeArguments != null)
            {
                foreach (Type o in typeArguments)
                {
                    AddConversionsForType(o);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal static SymWithType LookupMember(
            string name,
            Expr callingObject,
            ParentSymbol context,
            int arity,
            MemberLookup mem,
            bool allowSpecialNames,
            bool requireInvocable)
        {
            CType type = callingObject.Type;

            if (type is ArrayType)
            {
                type = SymbolLoader.GetPredefindType(PredefinedType.PT_ARRAY);
            }
            if (type is NullableType nub)
            {
                type = nub.GetAts();
            }

            if (!mem.Lookup(
                type,
                callingObject,
                context,
                GetName(name),
                arity,
                (allowSpecialNames ? 0 : MemLookFlags.UserCallable) |
                    (name == SpecialNames.Indexer ? MemLookFlags.Indexer : 0) |
                    (name == SpecialNames.Constructor ? MemLookFlags.Ctor : 0) |
                    (requireInvocable ? MemLookFlags.MustBeInvocable : 0)))
            {
                return null;
            }
            return mem.SwtFirst();
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static void AddParameterConversions(MethodBase method)
        {
            foreach (ParameterInfo param in method.GetParameters())
            {
                AddConversionsForType(param.ParameterType);
            }
        }

        #region InheritanceHierarchy
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static void AddNamesOnType(NameHashKey key)
        {
            Debug.Assert(!s_namesLoadedForEachType.Contains(key));

            // We need to declare all of its inheritance hierarchy.
            List<Type> inheritance = CreateInheritanceHierarchyList(key.Type);

            // Now add every method as it appears in the inheritance hierarchy.
            AddNamesInInheritanceHierarchy(key.Name, inheritance);
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static void AddNamesInInheritanceHierarchy(string name, List<Type> inheritance)
        {
            for (int i = inheritance.Count - 1; i >= 0; --i)
            {
                Type type = inheritance[i];
                if (type.IsGenericType)
                {
                    type = type.GetGenericTypeDefinition();
                }

                if (!s_namesLoadedForEachType.Add(new NameHashKey(type, name)))
                {
                    continue;
                }

                // Now loop over all methods and add them.
                IEnumerator<MemberInfo> memberEn = type
                    .GetMembers(EverythingBindingFlags)
                    .Where(member => member.DeclaringType == type && member.Name == name).GetEnumerator();
                if (memberEn.MoveNext())
                {
                    List<EventInfo> events = null;
                    CType cType = GetCTypeFromType(type);
                    if (!(cType is AggregateType aggType))
                        continue;
                    AggregateSymbol aggregate = aggType.OwningAggregate;
                    FieldSymbol addedField = null;

                    // We need to add fields before the actual events, so do the first iteration
                    // excluding events.
                    do
                    {
                        MemberInfo member = memberEn.Current;
                        if (member is MethodInfo method)
                        {
                            MethodKindEnum kind = member.Name switch
                            {
                                SpecialNames.Invoke => MethodKindEnum.Invoke,
                                SpecialNames.ImplicitConversion => MethodKindEnum.ImplicitConv,
                                SpecialNames.ExplicitConversion => MethodKindEnum.ExplicitConv,
                                _ => MethodKindEnum.Actual,
                            };
                            AddMethodToSymbolTable(method, aggregate, kind);
                            AddParameterConversions(method);
                        }
                        else if (member is ConstructorInfo ctor)
                        {
                            AddMethodToSymbolTable(ctor, aggregate, MethodKindEnum.Constructor);
                            AddParameterConversions(ctor);
                        }
                        else if (member is PropertyInfo prop)
                        {
                            AddPropertyToSymbolTable(prop, aggregate);
                        }
                        else if (member is FieldInfo field)
                        {
                            // Store this field so that if we also find an event, we can
                            // mark it as the backing field of the event.
                            Debug.Assert(addedField == null);
                            addedField = AddFieldToSymbolTable(field, aggregate);
                        }
                        else if (member is EventInfo e)
                        {
                            // Store events until after all fields
                            (events ??= new List<EventInfo>()).Add(e);
                        }
                    } while (memberEn.MoveNext());

                    if (events != null)
                    {
                        foreach (EventInfo e in events)
                        {
                            AddEventToSymbolTable(e, aggregate, addedField);
                        }
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static List<Type> CreateInheritanceHierarchyList(Type type)
        {
            List<Type> list;
            if (type.IsInterface)
            {
                Type[] ifaces = type.GetInterfaces();

                list = new List<Type>(ifaces.Length + 2)
                {
                    type
                };
                foreach (Type iface in type.GetInterfaces())
                {
                    LoadSymbolsFromType(iface);
                    list.Add(iface);
                }

                Type obj = typeof(object);
                LoadSymbolsFromType(obj);
                list.Add(obj);
            }
            else
            {
                list = new List<Type> { type };
                for (Type parent = type.BaseType; parent != null; parent = parent.BaseType)
                {
                    // Load it in the symbol table.
                    LoadSymbolsFromType(parent);

                    // Insert into our list of Types.
                    list.Add(parent);
                }
            }
            return list;
        }
        #endregion

        #region GetName
        /////////////////////////////////////////////////////////////////////////////////

        private static Name GetName(string p) => NameManager.Add(p ?? "");

        /////////////////////////////////////////////////////////////////////////////////

        private static Name GetName(Type type)
        {
            string name = type.Name;
            if (type.IsGenericType)
            {
                int idx = name.IndexOf('`');
                if (idx >= 0)
                {
                    return NameManager.Add(name, idx);
                }
            }

            return NameManager.Add(name);
        }

        #endregion

        #region TypeParameters
        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static TypeArray GetMethodTypeParameters(MethodInfo method, MethodSymbol parent)
        {
            if (method.IsGenericMethod)
            {
                Type[] genericArguments = method.GetGenericArguments();
                CType[] ctypes = new CType[genericArguments.Length];
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    Type t = genericArguments[i];
                    ctypes[i] = LoadMethodTypeParameter(parent, t);
                }

                // After we load the type parameters, we need to resolve their bounds.
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    Type t = genericArguments[i];
                    ((TypeParameterType)ctypes[i]).Symbol.SetBounds(TypeArray.Allocate(GetCTypeArrayFromTypes(t.GetGenericParameterConstraints())));
                }

                return TypeArray.Allocate(ctypes);
            }

            return TypeArray.Empty;
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static TypeArray GetAggregateTypeParameters(Type type, AggregateSymbol agg)
        {
            if (type.IsGenericType)
            {
                Type genericDefinition = type.GetGenericTypeDefinition();
                Type[] genericArguments = genericDefinition.GetGenericArguments();
                List<CType> ctypes = new List<CType>();
                int outerParameters = agg.isNested() ? agg.GetOuterAgg().GetTypeVarsAll().Count : 0;

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    // Suppose we have the following:
                    //
                    // class A<A1, A2, ..., An>
                    // {
                    //     class B<B1, B2, ..., Bm>
                    //     {
                    //     }
                    // }
                    //
                    // B will have m+n generic arguments - { A1, A2, ..., An, B1, B2, ..., Bn }.
                    // As we enumerate these, we need to skip type parameters whose GenericParameterPosition
                    // is less than n, since the first n type parameters are { A1, A2, ..., An }.

                    Type t = genericArguments[i];

                    if (t.GenericParameterPosition < outerParameters)
                    {
                        continue;
                    }

                    CType ctype;
                    if (t.IsGenericParameter && t.DeclaringType == genericDefinition)
                    {
                        ctype = LoadClassTypeParameter(agg, t);
                    }
                    else
                    {
                        ctype = GetCTypeFromType(t);
                    }

                    // We check to make sure we own the type parameter - this is because we're
                    // currently calculating TypeArgsThis, NOT TypeArgsAll.
                    if (((TypeParameterType)ctype).OwningSymbol == agg)
                    {
                        ctypes.Add(ctype);
                    }
                }

                return TypeArray.Allocate(ctypes.ToArray());
            }

            return TypeArray.Empty;
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static TypeParameterType LoadClassTypeParameter(AggregateSymbol parent, Type t)
        {
            for (AggregateSymbol p = parent; p != null; p = p.parent as AggregateSymbol)
            {
                for (TypeParameterSymbol typeParam = SymbolStore.LookupSym(
                        GetName(t), p, symbmask_t.MASK_TypeParameterSymbol) as TypeParameterSymbol;
                    typeParam != null;
                    typeParam = typeParam.LookupNext(symbmask_t.MASK_TypeParameterSymbol) as TypeParameterSymbol)
                {
                    if (AreTypeParametersEquivalent(typeParam.GetTypeParameterType().AssociatedSystemType, t))
                    {
                        return typeParam.GetTypeParameterType();
                    }
                }
            }
            return AddTypeParameterToSymbolTable(parent, null, t, true);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static bool AreTypeParametersEquivalent(Type t1, Type t2)
        {
            Debug.Assert(t1.IsGenericParameter && t2.IsGenericParameter);

            if (t1 == t2)
            {
                return true;
            }

            Type t1Original = GetOriginalTypeParameterType(t1);
            Type t2Original = GetOriginalTypeParameterType(t2);

            return t1Original == t2Original;
        }

        /////////////////////////////////////////////////////////////////////////////////

        // GetOriginalTypeParameterType
        // This was added so that LoadClassTypeParameter would not fail to find outer
        // type parameters when given a System.Type from an outer class and a matching
        // type parameter from in an inner class. In Reflection type parameters are
        // always declared by their inner most declaring class. For example, given:
        //
        //   class A<T> {
        //     class B<U> { }
        //   }
        //
        // in Reflection there are two Ts, A<T>'s T, and B<U>'s T. In our world there is
        // only A<T>'s T.
        //
        // So this method here drills down and finds the type parameter type corresponding
        // to the position of the given type parameter, from the outer most containing
        // type. So in the above example, given B<U>'s T from reflection, this will return
        // A<T>'s T, so that you can make a reference comparison of type parameters coming
        // from different nesting levels.
        //
        // There is an exception, we don't handle the case where you have type parameters
        // coming from different partially constructed methods. E.g.
        //
        //   class A<T,S> {
        //     public void M<U> { }
        //   }
        //
        //   A<T,int>.M<U>
        //   A<T,string>.M<U>
        //
        // In the above two methods, the two U's are different in Reflection. Here we just
        // return the type parameter type given if it is in a method, we do not try to
        // generalize these occurrences for reference equality.
        //
        private static Type GetOriginalTypeParameterType(Type t)
        {
            Debug.Assert(t.IsGenericParameter);

            int pos = t.GenericParameterPosition;

            Type parentType = t.DeclaringType;
            if (parentType != null && parentType.IsGenericType)
            {
                parentType = parentType.GetGenericTypeDefinition();
            }

            if (t.DeclaringMethod != null)
            {
                if (parentType.GetGenericArguments() == null || pos >= parentType.GetGenericArguments().Length)
                {
                    return t;
                }
            }

            while (parentType.GetGenericArguments().Length > pos)
            {
                Type nextParent = parentType.DeclaringType;
                if (nextParent != null && nextParent.IsGenericType)
                {
                    nextParent = nextParent.GetGenericTypeDefinition();
                }

                if (nextParent?.GetGenericArguments()?.Length > pos)
                {
                    parentType = nextParent;
                }
                else
                {
                    break;
                }
            }

            return parentType.GetGenericArguments()[pos];
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static TypeParameterType LoadMethodTypeParameter(MethodSymbol parent, Type t)
        {
            for (Symbol sym = parent.firstChild; sym != null; sym = sym.nextChild)
            {
                if (sym is TypeParameterSymbol parSym)
                {
                    TypeParameterType type = parSym.GetTypeParameterType();
                    if (AreTypeParametersEquivalent(type.AssociatedSystemType, t))
                    {
                        return type;
                    }
                }
            }

            return AddTypeParameterToSymbolTable(null, parent, t, false);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static TypeParameterType AddTypeParameterToSymbolTable(
                AggregateSymbol agg,
                MethodSymbol meth,
                Type t,
                bool bIsAggregate)
        {
            Debug.Assert((agg != null && bIsAggregate) || (meth != null && !bIsAggregate));

            TypeParameterSymbol typeParam;
            if (bIsAggregate)
            {
                typeParam = SymFactory.CreateClassTypeParameter(
                    GetName(t),
                    agg,
                    t.GenericParameterPosition,
                    t.GenericParameterPosition);
            }
            else
            {
                typeParam = SymFactory.CreateMethodTypeParameter(
                    GetName(t),
                    meth,
                    t.GenericParameterPosition,
                    t.GenericParameterPosition);
            }

            if ((t.GenericParameterAttributes & GenericParameterAttributes.Covariant) != 0)
            {
                typeParam.Covariant = true;
            }
            if ((t.GenericParameterAttributes & GenericParameterAttributes.Contravariant) != 0)
            {
                typeParam.Contravariant = true;
            }

            SpecCons cons = SpecCons.None;

            if ((t.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
            {
                cons |= SpecCons.New;
            }
            if ((t.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            {
                cons |= SpecCons.Ref;
            }
            if ((t.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            {
                cons |= SpecCons.Val;
            }

            typeParam.SetConstraints(cons);
            typeParam.SetAccess(ACCESS.ACC_PUBLIC);
            TypeParameterType typeParamType = TypeManager.GetTypeParameter(typeParam);

            return typeParamType;
        }

        #endregion

        #region LoadTypeChain
        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static CType LoadSymbolsFromType(Type type)
        {
            List<object> declarationChain = BuildDeclarationChain(type);

            NamespaceOrAggregateSymbol current = NamespaceSymbol.Root;

            // Go through the declaration chain and add namespaces and types for
            // each element in the chain.
            for (int i = 0; i < declarationChain.Count; i++)
            {
                object o = declarationChain[i];
                if (o is Type t)
                {
                    if (t.IsNullableType())
                    {
                        return TypeManager.GetNullable(GetCTypeFromType(t.GetGenericArguments()[0]));
                    }

                    AggregateSymbol next = FindSymForType(
                        SymbolStore.LookupSym(GetName(t), current, symbmask_t.MASK_AggregateSymbol), t);

                    // If we haven't found this type yet, then add it to our symbol table.
                    if (next == null)
                    {
                        // Note that if we have anything other than an AggregateSymbol,
                        // we must be at the end of the line - that is, nothing else can
                        // have children.
                        CType ctype = ProcessSpecialTypeInChain(current, t);
                        if (ctype != null)
                        {
                            Debug.Assert(!(ctype is AggregateType));
                            return ctype;
                        }

                        // This is a regular class.
                        next = AddAggregateToSymbolTable(current, t);
                    }

                    if (t == type)
                    {
                        return GetConstructedType(type, next);
                    }

                    current = next;
                }
                else if (o is MethodInfo m)
                {
                    // We cant be at the end.
                    Debug.Assert(i + 1 < declarationChain.Count);
                    return ProcessMethodTypeParameter(m, declarationChain[++i] as Type, current as AggregateSymbol);
                }
                else
                {
                    Debug.Assert(o is string);
                    current = AddNamespaceToSymbolTable(current, o as string);
                }
            }

            Debug.Fail("Should be unreachable");
            return null;
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static TypeParameterType ProcessMethodTypeParameter(MethodInfo methinfo, Type t, AggregateSymbol parent)
        {
            MethodSymbol meth = FindMatchingMethod(methinfo, parent);
            if (meth == null)
            {
                meth = AddMethodToSymbolTable(methinfo, parent, MethodKindEnum.Actual);

                // Because we return null from AddMethodToSymbolTable when we have a MethodKindEnum.Actual
                // and the method that we're trying to add is a special name, we need to assert that
                // we indeed have added a method. This is because no special name should have a method
                // type parameter on it.
                Debug.Assert(meth != null);
            }

            return LoadMethodTypeParameter(meth, t);
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static CType GetConstructedType(Type type, AggregateSymbol agg)
        {
            // We've found the one we want, so return it.
            if (type.IsGenericType)
            {
                // If we're a generic type, then we need to add the type arguments.
                List<CType> types = new List<CType>();

                foreach (Type argument in type.GetGenericArguments())
                {
                    types.Add(GetCTypeFromType(argument));
                }

                TypeArray typeArray = TypeArray.Allocate(types.ToArray());
                AggregateType aggType = TypeManager.GetAggregate(agg, typeArray);
                return aggType;
            }
            CType ctype = agg.getThisType();
            return ctype;
        }

        /////////////////////////////////////////////////////////////////////////////////
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static CType ProcessSpecialTypeInChain(NamespaceOrAggregateSymbol parent, Type t)
        {
            if (t.IsGenericParameter)
            {
                AggregateSymbol agg = parent as AggregateSymbol;
                Debug.Assert(agg != null);
                return LoadClassTypeParameter(agg, t);
            }

            if (t.IsArray)
            {
                // Now we return an array of nesting level corresponding to the rank.
                return TypeManager.GetArray(
                    GetCTypeFromType(t.GetElementType()),
                    t.GetArrayRank(),
                    t.GetElementType().MakeArrayType() == t
                    );
            }

            if (t.IsPointer)
            {
                // Now we return the pointer type that we want.
                return TypeManager.GetPointer(GetCTypeFromType(t.GetElementType()));
            }

            return null;
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static List<object> BuildDeclarationChain(Type callingType)
        {
            // We need to build the parent chain of the calling type. Since we only
            // have the type itself, first we need to build the chain up from the
            // type down to the root namespace, then we need to ensure that
            // the chain exists in our symbol table by searching from the root namespace
            // back down to the calling type. Also note that if we have a method type
            // parameter, then we'll also add the MethodBase to the chain.
            //
            // Note that we'll populate this list in a hybrid way - we'll add the
            // types for the type part of the chain, and we'll just add the string names
            // of the namespaces.

            // Strip off the ref-ness.
            if (callingType.IsByRef)
            {
                callingType = callingType.GetElementType();
            }

            List<object> callChain = new List<object>();
            for (Type t = callingType; t != null; t = t.DeclaringType)
            {
                callChain.Add(t);

                if (t.IsGenericParameter && t.DeclaringMethod != null)
                {
                    MethodBase methodBase = t.DeclaringMethod;
                    bool bAdded = false;
                    foreach (MethodInfo methinfo in t.DeclaringType.GetMethods(EverythingBindingFlags))
                    {
                        if (!methinfo.HasSameMetadataDefinitionAs(methodBase) || !methinfo.IsGenericMethod)
                        {
                            continue;
                        }

                        Debug.Assert(!bAdded);
                        callChain.Add(methinfo);
                        bAdded = true;
                    }
                    Debug.Assert(bAdded);
                }
            }

            callChain.Reverse();

            // Now take out the namespaces and add them to the end of the chain.
            if (callingType.Namespace != null)
            {
                callChain.InsertRange(0, callingType.Namespace.Split('.'));
            }
            return callChain;
        }

        // We have an aggregate symbol of the correct parent and full name, but it may have the wrong arity, or, due to
        // dynamic loading or creation, two different types can exist that have the same name.

        // In the static compiler, this would have been an error and name lookup would be ambiguous, but here we never have
        // to lookup names of types for real (only names of members).

        // For either case, move onto the next symbol in the chain, and check again for appropriate type.
        private static AggregateSymbol FindSymForType(Symbol sym, Type t)
        {
            while (sym != null)
            {
                // We use "IsEquivalentTo" so that unified local types match.
                if (sym is AggregateSymbol agg)
                    if (agg.AssociatedSystemType.IsEquivalentTo(t.IsGenericType ? t.GetGenericTypeDefinition() : t))
                    {
                        return agg;
                    }

                sym = sym.nextSameName;
            }

            return null;
        }

        private static NamespaceSymbol AddNamespaceToSymbolTable(NamespaceOrAggregateSymbol parent, string sz)
        {
            Name name = GetName(sz);
            return SymbolStore.LookupSym(name, parent, symbmask_t.MASK_NamespaceSymbol) as NamespaceSymbol
                ?? SymFactory.CreateNamespace(name, parent as NamespaceSymbol);
        }
        #endregion

        #region CTypeFromType
        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal static CType[] GetCTypeArrayFromTypes(Type[] types)
        {
            Debug.Assert(types != null);

            int length = types.Length;
            if (length == 0)
            {
                return Array.Empty<CType>();
            }

            CType[] ctypes = new CType[length];
            for (int i = 0; i < types.Length; i++)
            {
                Type t = types[i];
                Debug.Assert(t != null);
                ctypes[i] = GetCTypeFromType(t);
            }

            return ctypes;
        }

        /////////////////////////////////////////////////////////////////////////////////
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal static CType GetCTypeFromType(Type type) => type.IsByRef
            ? TypeManager.GetParameterModifier(LoadSymbolsFromType(type.GetElementType()), false)
            : LoadSymbolsFromType(type);

        #endregion

        #region Aggregates
        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static AggregateSymbol AddAggregateToSymbolTable(
            NamespaceOrAggregateSymbol parent,
            Type type)
        {
            AggregateSymbol agg = SymFactory.CreateAggregate(GetName(type), parent);
            agg.AssociatedSystemType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            agg.AssociatedAssembly = type.Assembly;

            // We have to set the TypeVars, access, and the AggKind before we can set the aggState
            // because of the assertion checking the compiler does.
            AggKindEnum kind;
            if (type.IsInterface)
            {
                kind = AggKindEnum.Interface;
            }
            else if (type.IsEnum)
            {
                kind = AggKindEnum.Enum;
                agg.SetUnderlyingType((AggregateType)GetCTypeFromType(Enum.GetUnderlyingType(type)));
            }
            else if (type.IsValueType)
            {
                kind = AggKindEnum.Struct;
            }
            else
            {
                // If it derives from Delegate or MulticastDelegate, then its
                // a delegate type. However, MuticastDelegate itself is not a
                // delegate type.
                if (type.BaseType != null &&
                    (type.BaseType.FullName == "System.MulticastDelegate" ||
                    type.BaseType.FullName == "System.Delegate") &&
                    type.FullName != "System.MulticastDelegate")
                {
                    kind = AggKindEnum.Delegate;
                }
                else
                {
                    kind = AggKindEnum.Class;
                }
            }
            agg.SetAggKind(kind);
            agg.SetTypeVars(TypeArray.Empty);

            ACCESS access;
            if (type.IsPublic)
            {
                access = ACCESS.ACC_PUBLIC;
            }
            else if (type.IsNested)
            {
                // If its nested, we may have other accessibility options.
                if (type.IsNestedAssembly)
                {
                    access = ACCESS.ACC_INTERNAL;
                }
                else if (type.IsNestedFamORAssem)
                {
                    access = ACCESS.ACC_INTERNALPROTECTED;
                }
                else if (type.IsNestedPrivate)
                {
                    access = ACCESS.ACC_PRIVATE;
                }
                else if (type.IsNestedFamily)
                {
                    access = ACCESS.ACC_PROTECTED;
                }
                else if (type.IsNestedFamANDAssem)
                {
                    access = ACCESS.ACC_INTERNAL_AND_PROTECTED;
                }
                else
                {
                    Debug.Assert(type.IsPublic || type.IsNestedPublic);
                    access = ACCESS.ACC_PUBLIC;
                }
            }
            else
            {
                // We're not public and we're not nested - we must be internal.
                access = ACCESS.ACC_INTERNAL;
            }
            agg.SetAccess(access);

            if (!type.IsGenericParameter)
            {
                agg.SetTypeVars(GetAggregateTypeParameters(type, agg));
            }

            if (type.IsGenericType)
            {
                Type genericDefinition = type.GetGenericTypeDefinition();
                Type[] genericArguments = genericDefinition.GetGenericArguments();

                // After we load the type parameters, we need to resolve their bounds.
                for (int i = 0; i < agg.GetTypeVars().Count; i++)
                {
                    Type t = genericArguments[i];
                    if (agg.GetTypeVars()[i] is TypeParameterType typeVar)
                    {
                        typeVar.Symbol.SetBounds(TypeArray.Allocate(GetCTypeArrayFromTypes(t.GetGenericParameterConstraints())));
                    }
                }
            }

            agg.SetAbstract(type.IsAbstract);

            {
                string typeName = type.FullName;
                if (type.IsGenericType)
                {
                    typeName = type.GetGenericTypeDefinition().FullName;
                }
                if (typeName != null)
                {
                    PredefinedType predefinedType = PredefinedTypeFacts.TryGetPredefTypeIndex(typeName);
                    if (predefinedType != PredefinedType.PT_UNDEFINEDINDEX)
                    {
                        PredefinedTypes.InitializePredefinedType(agg, predefinedType);
                    }
                }
            }

            agg.SetSealed(type.IsSealed);
            if (type.BaseType != null)
            {
                // type.BaseType can be null for Object or for interface types.
                Type t = type.BaseType;
                if (t.IsGenericType)
                {
                    t = t.GetGenericTypeDefinition();
                }
                agg.SetBaseClass((AggregateType)GetCTypeFromType(t));
            }

            agg.SetFirstUDConversion(null);
            SetInterfacesOnAggregate(agg, type);
            agg.SetHasPubNoArgCtor(type.GetConstructor(Type.EmptyTypes) != null);

            // If we have a delegate, get its invoke and constructor methods as well.
            if (agg.IsDelegate())
            {
                PopulateSymbolTableWithName(SpecialNames.Constructor, null, type);
                PopulateSymbolTableWithName(SpecialNames.Invoke, null, type);
            }

            return agg;
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static void SetInterfacesOnAggregate(AggregateSymbol aggregate, Type type)
        {
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }
            Type[] interfaces = type.GetInterfaces();

            // We won't be able to find the difference between Ifaces and
            // IfacesAll anymore - at runtime, the class implements all of its
            // Ifaces and IfacesAll, so theres no way to differentiate.
            //
            // This actually doesn't matter though - for conversions and methodcalls,
            // we don't really care where they've come from as long as we know the overall
            // set of IfacesAll.

            aggregate.SetIfaces(TypeArray.Allocate(GetCTypeArrayFromTypes(interfaces)));
            aggregate.SetIfacesAll(aggregate.GetIfaces());
        }
        #endregion

        #region Field
        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static FieldSymbol AddFieldToSymbolTable(FieldInfo fieldInfo, AggregateSymbol aggregate)
        {
            FieldSymbol field = SymbolStore.LookupSym(
                GetName(fieldInfo.Name),
                aggregate,
                symbmask_t.MASK_FieldSymbol) as FieldSymbol;
            if (field != null)
            {
                return field;
            }

            field = SymFactory.CreateMemberVar(GetName(fieldInfo.Name), aggregate);
            field.AssociatedFieldInfo = fieldInfo;

            field.isStatic = fieldInfo.IsStatic;
            ACCESS access;
            if (fieldInfo.IsPublic)
            {
                access = ACCESS.ACC_PUBLIC;
            }
            else if (fieldInfo.IsPrivate)
            {
                access = ACCESS.ACC_PRIVATE;
            }
            else if (fieldInfo.IsFamily)
            {
                access = ACCESS.ACC_PROTECTED;
            }
            else if (fieldInfo.IsAssembly)
            {
                access = ACCESS.ACC_INTERNAL;
            }
            else if (fieldInfo.IsFamilyOrAssembly)
            {
                access = ACCESS.ACC_INTERNALPROTECTED;
            }
            else
            {
                Debug.Assert(fieldInfo.IsFamilyAndAssembly);
                access = ACCESS.ACC_INTERNAL_AND_PROTECTED;
            }
            field.SetAccess(access);
            field.isReadOnly = fieldInfo.IsInitOnly;
            field.isEvent = false;
            field.SetType(GetCTypeFromType(fieldInfo.FieldType));

            return field;
        }
        #endregion

        #region Events

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static void AddEventToSymbolTable(EventInfo eventInfo, AggregateSymbol aggregate, FieldSymbol addedField)
        {
            EventSymbol ev = SymbolStore.LookupSym(
                GetName(eventInfo.Name),
                aggregate,
                symbmask_t.MASK_EventSymbol) as EventSymbol;
            if (ev != null)
            {
                Debug.Assert(ev.AssociatedEventInfo == eventInfo);
                return;
            }

            ev = SymFactory.CreateEvent(GetName(eventInfo.Name), aggregate);
            ev.AssociatedEventInfo = eventInfo;

            // EventSymbol
            ACCESS access = ACCESS.ACC_PRIVATE;
            if (eventInfo.AddMethod != null)
            {
                ev.methAdd = AddMethodToSymbolTable(eventInfo.AddMethod, aggregate, MethodKindEnum.EventAccessor);
                ev.methAdd.SetEvent(ev);
                ev.isOverride = ev.methAdd.IsOverride();

                access = ev.methAdd.GetAccess();
            }
            if (eventInfo.RemoveMethod != null)
            {
                ev.methRemove = AddMethodToSymbolTable(eventInfo.RemoveMethod, aggregate, MethodKindEnum.EventAccessor);
                ev.methRemove.SetEvent(ev);
                ev.isOverride = ev.methRemove.IsOverride();

                access = ev.methRemove.GetAccess();
            }
            Debug.Assert(ev.methAdd != null || ev.methRemove != null);
            ev.isStatic = false;
            ev.type = GetCTypeFromType(eventInfo.EventHandlerType);

            // Symbol
            ev.SetAccess(access);

            // If we imported a field on the same aggregate, with the same name, and it also
            // has the same type, then that field is the backing field for this event, and
            // we mark it as such. This is used for the CSharpIsEventBinder.
            CType addedFieldType = addedField?.GetType();
            if (addedFieldType != null)
            {
                if (addedFieldType == ev.type)
                {
                    addedField.isEvent = true;
                }
            }
        }
        #endregion

        #region Properties
        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal static void AddPredefinedPropertyToSymbolTable(AggregateSymbol type, Name property)
        {
            AggregateType aggtype = type.getThisType();
            Type t = aggtype.AssociatedSystemType;

            foreach (PropertyInfo pi in t.GetProperties(EverythingBindingFlags))
            {
                if (pi.Name == property.Text)
                {
                    AddPropertyToSymbolTable(pi, type);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static void AddPropertyToSymbolTable(PropertyInfo property, AggregateSymbol aggregate)
        {
            Name name;
            bool isIndexer = property.GetIndexParameters().Length != 0
                             && property.DeclaringType?.GetCustomAttribute<DefaultMemberAttribute>()
                             ?.MemberName == property.Name;

            if (isIndexer)
            {
                name = GetName(SpecialNames.Indexer);
            }
            else
            {
                name = GetName(property.Name);
            }
            PropertySymbol prop = SymbolStore.LookupSym(
                name,
                aggregate,
                symbmask_t.MASK_PropertySymbol) as PropertySymbol;

            // If we already had one, see if it matches.
            if (prop != null)
            {
                PropertySymbol prevProp = null;

                // We'll have multiple properties with the same name if we have indexers.
                // In that case, we need to look at every indexer to see if we find one with
                // the matching associated sym that we want.
                while (prop != null)
                {
                    if (prop.AssociatedPropertyInfo.IsEquivalentTo(property))
                    {
                        return;
                    }

                    prevProp = prop;
                    prop = prop.LookupNext(symbmask_t.MASK_PropertySymbol) as PropertySymbol;
                }

                prop = prevProp;
                if (isIndexer)
                {
                    // We have an indexer for a different property info, so
                    // create a new symbol for it.
                    prop = null;
                }
            }

            // If we already had a property but its associated info doesn't match,
            // then we repurpose the property that we've found. This can happen
            // in the case of generic instantiations.
            //
            // Note that this is a bit of a hack - the best way to fix this is
            // by not depending on the instantiated properties at all, but rather depending
            // on their non-instantiated generic form, which can be gotten from the
            // parent's generic type definition's member. From there, we'll also need to
            // keep track of the instantiation as we move along, so that when we need the
            // associated property, we can instantiate it correctly.
            //
            // This seems far too heavyweight - since we know we will never bind to more
            // than one property per payload, lets just blast it each time.
            if (prop == null)
            {
                if (isIndexer)
                {
                    prop = SymFactory.CreateIndexer(name, aggregate);
                    prop.Params = CreateParameterArray(null, property.GetIndexParameters());
                }
                else
                {
                    prop = SymFactory.CreateProperty(GetName(property.Name), aggregate);
                    prop.Params = TypeArray.Empty;
                }
            }
            prop.AssociatedPropertyInfo = property;

            prop.isStatic = property.GetGetMethod(true) != null ? property.GetGetMethod(true).IsStatic : property.GetSetMethod(true).IsStatic;
            prop.isParamArray = DoesMethodHaveParameterArray(property.GetIndexParameters());
            prop.swtSlot = null;
            prop.RetType = GetCTypeFromType(property.PropertyType);
            prop.isOperator = isIndexer;

            // Determine if its an override. We should always have an accessor, unless
            // the metadata was bogus.
            if (property.GetMethod != null || property.SetMethod != null)
            {
                MethodInfo accessor = property.GetMethod ?? property.SetMethod; // Must have at least one.
                prop.isOverride = accessor.IsVirtual && accessor.IsHideBySig && accessor.GetBaseDefinition() != accessor;
                prop.isHideByName = !accessor.IsHideBySig;
            }

            SetParameterDataForMethProp(prop, property.GetIndexParameters());

            // Get and set.
            MethodInfo methGet = property.GetMethod;
            MethodInfo methSet = property.SetMethod;
            ACCESS access = ACCESS.ACC_PRIVATE;
            if (methGet != null)
            {
                prop.GetterMethod = AddMethodToSymbolTable(methGet, aggregate, MethodKindEnum.PropAccessor);

                // If we have an indexed property, leave the method as a method we can call,
                // and mark the property as bogus.
                if (isIndexer || prop.GetterMethod.Params.Count == 0)
                {
                    prop.GetterMethod.SetProperty(prop);
                }
                else
                {
                    prop.Bogus = true;
                    prop.GetterMethod.SetMethKind(MethodKindEnum.Actual);
                }

                if (prop.GetterMethod.GetAccess() > access)
                {
                    access = prop.GetterMethod.GetAccess();
                }
            }
            if (methSet != null)
            {
                prop.SetterMethod = AddMethodToSymbolTable(methSet, aggregate, MethodKindEnum.PropAccessor);

                // If we have an indexed property, leave the method as a method we can call,
                // and mark the property as bogus.
                if (isIndexer || prop.SetterMethod.Params.Count == 1)
                {
                    prop.SetterMethod.SetProperty(prop);
                }
                else
                {
                    prop.Bogus = true;
                    prop.SetterMethod.SetMethKind(MethodKindEnum.Actual);
                }

                if (prop.SetterMethod.GetAccess() > access)
                {
                    access = prop.SetterMethod.GetAccess();
                }
            }

            // The access of the property is the least restrictive access of its getter/setter.
            prop.SetAccess(access);
        }

        #endregion

        #region Methods
        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal static void AddPredefinedMethodToSymbolTable(AggregateSymbol type, Name methodName)
        {
            Type t = type.getThisType().AssociatedSystemType;

            // If we got here, it means we couldn't find it in our initial lookup. Means we haven't loaded it from reflection yet.
            // Lets go and do that now.
            // Check if we have constructors or not.
            if (methodName == NameManager.GetPredefinedName(PredefinedName.PN_CTOR))
            {
                foreach (ConstructorInfo c in t.GetConstructors())
                {
                    AddMethodToSymbolTable(
                        c,
                        type,
                        MethodKindEnum.Constructor);
                }
            }
            else
            {
                foreach (MethodInfo m in t.GetMethods(EverythingBindingFlags))
                {
                    if (m.Name == methodName.Text && m.DeclaringType == t)
                    {
                        AddMethodToSymbolTable(
                            m,
                            type,
                            m.Name == SpecialNames.Invoke ? MethodKindEnum.Invoke : MethodKindEnum.Actual);
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static MethodSymbol AddMethodToSymbolTable(MethodBase member, AggregateSymbol callingAggregate, MethodKindEnum kind)
        {
            MethodInfo method = member as MethodInfo;

            Debug.Assert(method != null || member is ConstructorInfo);

            // If we are trying to add an actual method via MethodKindEnum.Actual, and
            // the memberinfo is a special name, and its not static, then return null.
            // We'll re-add the thing later with some other method kind.
            //
            // This will happen for things like indexers and properties. The ones that have
            // special names that we DO want to allow adding are things like operators, which
            // are static and will not be added again later.

            if (kind == MethodKindEnum.Actual && // MethKindEnum.Actual
                (method == null || // Not a ConstructorInfo
                    (!method.IsStatic && method.IsSpecialName))) // Not static and is a special name
            {
                return null;
            }

            MethodSymbol methodSymbol = FindMatchingMethod(member, callingAggregate);
            if (methodSymbol != null)
            {
                return methodSymbol;
            }

            ParameterInfo[] parameters = member.GetParameters();
            // First create the method.
            methodSymbol = SymFactory.CreateMethod(GetName(member.Name), callingAggregate);
            methodSymbol.AssociatedMemberInfo = member;
            methodSymbol.SetMethKind(kind);
            if (kind == MethodKindEnum.ExplicitConv || kind == MethodKindEnum.ImplicitConv)
            {
                callingAggregate.SetHasConversion();
                methodSymbol.SetConvNext(callingAggregate.GetFirstUDConversion());
                callingAggregate.SetFirstUDConversion(methodSymbol);
            }

            ACCESS access;
            if (member.IsPublic)
            {
                access = ACCESS.ACC_PUBLIC;
            }
            else if (member.IsPrivate)
            {
                access = ACCESS.ACC_PRIVATE;
            }
            else if (member.IsFamily)
            {
                access = ACCESS.ACC_PROTECTED;
            }
            else if (member.IsFamilyOrAssembly)
            {
                access = ACCESS.ACC_INTERNALPROTECTED;
            }
            else if (member.IsAssembly)
            {
                access = ACCESS.ACC_INTERNAL;
            }
            else
            {
                Debug.Assert(member.IsFamilyAndAssembly);
                access = ACCESS.ACC_INTERNAL_AND_PROTECTED;
            }

            methodSymbol.SetAccess(access);
            methodSymbol.isVirtual = member.IsVirtual;
            methodSymbol.isStatic = member.IsStatic;

            if (method != null)
            {
                methodSymbol.typeVars = GetMethodTypeParameters(method, methodSymbol);
                methodSymbol.isOverride = method.IsVirtual && method.IsHideBySig && method.GetBaseDefinition() != method;
                methodSymbol.isOperator = IsOperator(method);
                methodSymbol.swtSlot = GetSlotForOverride(method);
                methodSymbol.RetType = GetCTypeFromType(method.ReturnType);
            }
            else
            {
                methodSymbol.typeVars = TypeArray.Empty;
                methodSymbol.isOverride = false;
                methodSymbol.isOperator = false;
                methodSymbol.swtSlot = null;
                methodSymbol.RetType = VoidType.Instance;
            }

            methodSymbol.modOptCount = 0;

            methodSymbol.isParamArray = DoesMethodHaveParameterArray(parameters);
            methodSymbol.isHideByName = false;

            methodSymbol.Params = CreateParameterArray(methodSymbol.AssociatedMemberInfo, parameters);

            SetParameterDataForMethProp(methodSymbol, parameters);

            return methodSymbol;
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static void SetParameterDataForMethProp(MethodOrPropertySymbol methProp, ParameterInfo[] parameters)
        {
            if (parameters.Length > 0)
            {
                // See if we have a param array.
                if (parameters[parameters.Length - 1].GetCustomAttribute(typeof(ParamArrayAttribute), false) != null)
                {
                    methProp.isParamArray = true;
                }

                // Mark the names of the parameters, and their default values.
                for (int i = 0; i < parameters.Length; i++)
                {
                    SetParameterAttributes(methProp, parameters, i);

                    // Insert the name.
                    methProp.ParameterNames.Add(GetName(parameters[i].Name));
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static void SetParameterAttributes(MethodOrPropertySymbol methProp, ParameterInfo[] parameters, int i)
        {
            ParameterInfo parameter = parameters[i];
            if ((parameter.Attributes & ParameterAttributes.Optional) != 0 && !parameter.ParameterType.IsByRef)
            {
                methProp.SetOptionalParameter(i);
                PopulateSymbolTableWithName("Value", new Type[] { typeof(Missing) }, typeof(Missing)); // We might need this later
            }

            // Get MarshalAsAttribute
            if ((parameter.Attributes & ParameterAttributes.HasFieldMarshal) != 0)
            {
                MarshalAsAttribute attr = parameter.GetCustomAttribute<MarshalAsAttribute>(false);
                if (attr != null)
                {
                    methProp.SetMarshalAsParameter(i, attr.Value);
                }
            }

            DateTimeConstantAttribute dateAttr = parameter.GetCustomAttribute<DateTimeConstantAttribute>(false);
            // Get the various kinds of default values
            if (dateAttr != null)
            {
                // Get DateTimeConstant
                ConstVal cv = ConstVal.Get(((DateTime)dateAttr.Value).Ticks);
                CType cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_DATETIME);
                methProp.SetDefaultParameterValue(i, cvType, cv);
            }
            else
            {
                DecimalConstantAttribute decAttr = parameter.GetCustomAttribute<DecimalConstantAttribute>();
                if (decAttr != null)
                {
                    // Get DecimalConstant
                    ConstVal cv = ConstVal.Get(decAttr.Value);
                    CType cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_DECIMAL);
                    methProp.SetDefaultParameterValue(i, cvType, cv);
                }
                else if ((parameter.Attributes & ParameterAttributes.HasDefault) != 0 && !parameter.ParameterType.IsByRef)
                {
                    // Only set a default value if we have one, and the type that we're
                    // looking at isn't a by ref type or a type parameter.

                    ConstVal cv = default(ConstVal);
                    CType cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_OBJECT);

                    // We need to use RawDefaultValue, because DefaultValue is too clever.
                    if (parameter.DefaultValue != null)
                    {
                        object defValue = parameter.DefaultValue;
                        Debug.Assert(Type.GetTypeCode(defValue.GetType()) != TypeCode.Decimal); // Handled above
                        switch (Type.GetTypeCode(defValue.GetType()))
                        {

                            case TypeCode.Byte:
                                cv = ConstVal.Get((byte)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_BYTE);
                                break;

                            case TypeCode.Int16:
                                cv = ConstVal.Get((short)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_SHORT);
                                break;

                            case TypeCode.Int32:
                                cv = ConstVal.Get((int)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_INT);
                                break;

                            case TypeCode.Int64:
                                cv = ConstVal.Get((long)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_LONG);
                                break;

                            case TypeCode.Single:
                                cv = ConstVal.Get((float)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_FLOAT);
                                break;

                            case TypeCode.Double:
                                cv = ConstVal.Get((double)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_DOUBLE);
                                break;

                            case TypeCode.Char:
                                cv = ConstVal.Get((char)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_CHAR);
                                break;

                            case TypeCode.Boolean:
                                cv = ConstVal.Get((bool)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_BOOL);
                                break;

                            case TypeCode.SByte:
                                cv = ConstVal.Get((sbyte)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_SBYTE);
                                break;

                            case TypeCode.UInt16:
                                cv = ConstVal.Get((ushort)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_USHORT);
                                break;

                            case TypeCode.UInt32:
                                cv = ConstVal.Get((uint)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_UINT);
                                break;

                            case TypeCode.UInt64:
                                cv = ConstVal.Get((ulong)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_ULONG);
                                break;

                            case TypeCode.String:
                                cv = ConstVal.Get((string)defValue);
                                cvType = SymbolLoader.GetPredefindType(PredefinedType.PT_STRING);
                                break;
                        }

                        // if we hit no case in the switch, we get object/null
                        // because that's how we initialized the constval.
                    }

                    methProp.SetDefaultParameterValue(i, cvType, cv);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static MethodSymbol FindMatchingMethod(MemberInfo method, AggregateSymbol callingAggregate)
        {
            MethodSymbol meth = SymbolStore.LookupSym(GetName(method.Name), callingAggregate, symbmask_t.MASK_MethodSymbol) as MethodSymbol;
            while (meth != null)
            {
                if (meth.AssociatedMemberInfo.IsEquivalentTo(method))
                {
                    return meth;
                }

                meth = meth.LookupNext(symbmask_t.MASK_MethodSymbol) as MethodSymbol;
            }

            return null;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static TypeArray CreateParameterArray(MemberInfo associatedInfo, ParameterInfo[] parameters)
        {
            bool isVarArg = associatedInfo is MethodBase mb && (mb.CallingConvention & CallingConventions.VarArgs) != 0;
            CType[] types = new CType[isVarArg ? parameters.Length + 1 : parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                types[i] = GetTypeOfParameter(parameters[i], associatedInfo);
            }

            if (isVarArg)
            {
                types[types.Length - 1] = ArgumentListType.Instance;
            }

            return TypeArray.Allocate(types);
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static CType GetTypeOfParameter(ParameterInfo p, MemberInfo m)
        {
            Type t = p.ParameterType;
            CType ctype;
            if (t.IsGenericParameter && t.DeclaringMethod != null && t.DeclaringMethod == m)
            {
                // If its a method type parameter from ourselves, just find it.
                ctype = LoadMethodTypeParameter(FindMethodFromMemberInfo(m), t);
            }
            else
            {
                ctype = GetCTypeFromType(t);
            }

            // Check if we have an out parameter.
            if (ctype is ParameterModifierType mod && p.IsOut && !p.IsIn)
            {
                CType parameterType = mod.ParameterType;
                ctype = TypeManager.GetParameterModifier(parameterType, true);
            }

            return ctype;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static bool DoesMethodHaveParameterArray(ParameterInfo[] parameters)
        {
            if (parameters.Length == 0)
            {
                return false;
            }

            ParameterInfo p = parameters[parameters.Length - 1];
            var attributes = p.GetCustomAttributes(false);

            foreach (object o in attributes)
            {
                if (o is ParamArrayAttribute)
                {
                    return true;
                }
            }
            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static SymWithType GetSlotForOverride(MethodInfo method)
        {
            if (method.IsVirtual && method.IsHideBySig)
            {
                MethodInfo baseMethodInfo = method.GetBaseDefinition();
                if (baseMethodInfo == method)
                {
                    // We just found ourselves, so we don't care here.
                    return null;
                }

                // We have the base class method that we're overriding. We can assume
                // that all the parent aggregate symbols were added, and that we added
                // the methods in order. As such, our parent methods should be in the
                // symbol table at this point.

                AggregateSymbol aggregate = ((AggregateType)GetCTypeFromType(baseMethodInfo.DeclaringType)).OwningAggregate;
                MethodSymbol baseMethod = FindMethodFromMemberInfo(baseMethodInfo);
                Debug.Assert(baseMethod != null);
                return new SymWithType(baseMethod, aggregate.getThisType());
            }

            return null;
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static MethodSymbol FindMethodFromMemberInfo(MemberInfo baseMemberInfo)
        {
            CType t = GetCTypeFromType(baseMemberInfo.DeclaringType);
            Debug.Assert(t is AggregateType);
            AggregateSymbol aggregate = ((AggregateType)t).OwningAggregate;
            Debug.Assert(aggregate != null);

            MethodSymbol meth = SymbolLoader.LookupAggMember(
                GetName(baseMemberInfo.Name),
                aggregate,
                symbmask_t.MASK_MethodSymbol) as MethodSymbol;
            for (;
                    meth != null && !meth.AssociatedMemberInfo.IsEquivalentTo(baseMemberInfo);
                    meth = meth.LookupNext(symbmask_t.MASK_MethodSymbol) as MethodSymbol)
                ;

            return meth;
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal static bool AggregateContainsMethod(AggregateSymbol agg, string szName, symbmask_t mask) =>
            SymbolLoader.LookupAggMember(GetName(szName), agg, mask) != null;

        #endregion

        #region Conversions
        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal static void AddConversionsForType(Type type)
        {
            if (type.IsInterface)
            {
                AddConversionsForOneType(type);
            }
            for (Type t = type; t.BaseType != null; t = t.BaseType)
            {
                AddConversionsForOneType(t);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static void AddConversionsForOneType(Type type)
        {
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }

            if (!s_typesWithConversionsLoaded.Add(type))
            {
                return;
            }

            // Always make the aggregate for the type, regardless of whether or not
            // there are any conversions.
            CType t = GetCTypeFromType(type);

            if (!(t is AggregateType))
            {
                CType endT;
                while ((endT = t.BaseOrParameterOrElementType) != null)
                {
                    t = endT;
                }
            }

            if (t is TypeParameterType paramType)
            {
                // Add conversions for the bounds.
                foreach (CType bound in paramType.Bounds.Items)
                {
                    AddConversionsForType(bound.AssociatedSystemType);
                }
                return;
            }

            Debug.Assert(t is AggregateType);
            AggregateSymbol aggregate = ((AggregateType)t).OwningAggregate;

            // Now find all the conversions and make them.
            foreach (MethodInfo conversion in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (conversion.DeclaringType == type && conversion.IsSpecialName && !conversion.IsGenericMethod)
                {
                    MethodKindEnum methodKind;
                    switch (conversion.Name)
                    {
                        case SpecialNames.ImplicitConversion:
                            methodKind = MethodKindEnum.ImplicitConv;
                            break;
                        case SpecialNames.ExplicitConversion:
                            methodKind = MethodKindEnum.ExplicitConv;
                            break;
                        default:
                            continue;
                    }

                    AddMethodToSymbolTable(conversion, aggregate, methodKind);
                }
            }
        }
        #endregion

        #region Operators
        /////////////////////////////////////////////////////////////////////////////////

        private static bool IsOperator(MethodInfo method)
        {
            if (method.IsSpecialName && method.IsStatic)
            {
                switch (method.Name)
                {
                    case SpecialNames.ImplicitConversion:
                    case SpecialNames.ExplicitConversion:
                    case SpecialNames.CLR_Add:
                    case SpecialNames.CLR_Subtract:
                    case SpecialNames.CLR_Multiply:
                    case SpecialNames.CLR_Division:
                    case SpecialNames.CLR_Modulus:
                    case SpecialNames.CLR_LShift:
                    case SpecialNames.CLR_RShift:
                    case SpecialNames.CLR_LT:
                    case SpecialNames.CLR_GT:
                    case SpecialNames.CLR_LTE:
                    case SpecialNames.CLR_GTE:
                    case SpecialNames.CLR_Equality:
                    case SpecialNames.CLR_Inequality:
                    case SpecialNames.CLR_BitwiseAnd:
                    case SpecialNames.CLR_ExclusiveOr:
                    case SpecialNames.CLR_BitwiseOr:
                    case SpecialNames.CLR_LogicalNot:
                    case SpecialNames.CLR_UnaryNegation:
                    case SpecialNames.CLR_UnaryPlus:
                    case SpecialNames.CLR_OnesComplement:
                    case SpecialNames.CLR_True:
                    case SpecialNames.CLR_False:
                    case SpecialNames.CLR_Increment:
                    case SpecialNames.CLR_Decrement:
                        return true;
                }
            }

            return false;
        }

        #endregion
    }
}
