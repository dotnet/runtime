// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder.Semantics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal class SymbolTable
    {
        /////////////////////////////////////////////////////////////////////////////////
        // Members
        private HashSet<Type> _typesWithConversionsLoaded;
        private HashSet<NameHashKey> _namesLoadedForEachType;

        // Members from the managed binder.
        private readonly SYMTBL _symbolTable;
        private readonly SymFactory _symFactory;
        private readonly NameManager _nameManager;
        private readonly TypeManager _typeManager;
        private readonly BSYMMGR _bsymmgr;
        private readonly CSemanticChecker _semanticChecker;

        private NamespaceSymbol _rootNamespace;
        private readonly InputFile _infile;

        /////////////////////////////////////////////////////////////////////////////////

        private sealed class NameHashKey
        {
            internal readonly Type type;
            internal readonly string name;

            public NameHashKey(Type type, string name)
            {
                this.type = type;
                this.name = name;
            }

            public override bool Equals(object obj)
            {
                NameHashKey h = obj as NameHashKey;
                return h != null && type.Equals(h.type) && name.Equals(h.name);
            }

            public override int GetHashCode()
            {
                return type.GetHashCode() ^ name.GetHashCode();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal SymbolTable(
            SYMTBL symTable,
            SymFactory symFactory,
            NameManager nameManager,
            TypeManager typeManager,
            BSYMMGR bsymmgr,
            CSemanticChecker semanticChecker,

            InputFile infile)
        {
            _symbolTable = symTable;
            _symFactory = symFactory;
            _nameManager = nameManager;
            _typeManager = typeManager;
            _bsymmgr = bsymmgr;
            _semanticChecker = semanticChecker;

            _infile = infile;

            ClearCache();
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal void ClearCache()
        {
            _typesWithConversionsLoaded = new HashSet<Type>();
            _namesLoadedForEachType = new HashSet<NameHashKey>();
            _rootNamespace = _bsymmgr.GetRootNS();

            // Now populate object.
            LoadSymbolsFromType(typeof(object));
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal void PopulateSymbolTableWithName(
            string name,
            IEnumerable<Type> typeArguments,
            Type callingType)
        {
            // The first argument is the object that we're calling off of.
            if (callingType.GetTypeInfo().IsGenericType)
            {
                callingType = callingType.GetTypeInfo().GetGenericTypeDefinition();
            }
            if (name == SpecialNames.Indexer)
            {
                // What about named indexers?
                if (callingType == typeof(string))
                {
                    name = "Chars";
                }
                else
                {
                    name = "Item";
                }
            }
            NameHashKey key = new NameHashKey(callingType, name);

            // If we've already populated this name/type pair, then just leave.
            if (_namesLoadedForEachType.Contains(key))
            {
                return;
            }

            // Add the names.
            IEnumerable<MemberInfo> members = AddNamesOnType(key);

            // Take each member and load each type's conversions into the symbol table.
            if (members != null)
            {
                foreach (MemberInfo member in members)
                {
                    if (member is MethodInfo)
                    {
                        foreach (ParameterInfo param in (member as MethodInfo).GetParameters())
                        {
                            AddConversionsForType(param.ParameterType);
                        }
                    }
                    else if (member is ConstructorInfo)
                    {
                        foreach (ParameterInfo param in (member as ConstructorInfo).GetParameters())
                        {
                            AddConversionsForType(param.ParameterType);
                        }
                    }
                }
            }

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

        internal SymWithType LookupMember(
            string name,
            EXPR callingObject,
            ParentSymbol context,
            int arity,
            MemberLookup mem,
            bool allowSpecialNames,
            bool requireInvocable)
        {
            CType type = callingObject.type;

            if (type.IsArrayType())
            {
                type = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_ARRAY);
            }
            if (type.IsNullableType())
            {
                type = type.AsNullableType().GetAts(_semanticChecker.GetSymbolLoader().GetErrorContext());
            }

            if (!mem.Lookup(
                _semanticChecker,
                type,
                callingObject,
                context,
                GetName(name),
                arity,
                MemLookFlags.TypeVarsAllowed |
                    (allowSpecialNames ? 0 : MemLookFlags.UserCallable) |
                    (name == SpecialNames.Indexer ? MemLookFlags.Indexer : 0) |
                    (name == SpecialNames.Constructor ? MemLookFlags.Ctor : 0) |
                    (requireInvocable ? MemLookFlags.MustBeInvocable : 0)))
            {
                return null;
            }
            return mem.SwtFirst();
        }

        /////////////////////////////////////////////////////////////////////////////////

        #region InheritanceHierarchy
        private IEnumerable<MemberInfo> AddNamesOnType(NameHashKey key)
        {
            Debug.Assert(!_namesLoadedForEachType.Contains(key));

            // We need to declare all of its inheritance hierarchy.
            List<Type> inheritance = CreateInheritanceHierarchyList(key.type);

            // Now add every method as it appears in the inheritance hierarchy.
            return AddNamesInInheritanceHierarchy(key.name, inheritance);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private IEnumerable<MemberInfo> AddNamesInInheritanceHierarchy(string name, List<Type> inheritance)
        {
            IEnumerable<MemberInfo> result = Array.Empty<MemberInfo>();

            foreach (Type t in inheritance)
            {
                Type type = t;
                if (type.GetTypeInfo().IsGenericType)
                {
                    type = type.GetTypeInfo().GetGenericTypeDefinition();
                }
                NameHashKey key = new NameHashKey(type, name);

                // Now loop over all methods and add them.
                IEnumerable<MemberInfo> members = Enumerable.Where(type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
                                                                   member => member.Name == name && member.DeclaringType == type);
                IEnumerable<MemberInfo> events = Enumerable.Where(type.GetRuntimeEvents(),
                                                                  member => member.Name == name && member.DeclaringType == type);

                if (members.Any())
                {
                    CType cType = GetCTypeFromType(type);
                    if (!(cType is AggregateType))
                        continue;
                    AggregateSymbol aggregate = (cType as AggregateType).getAggregate();
                    FieldSymbol addedField = null;

                    // We need to add fields before the actual events, so do the first iteration 
                    // excludint events.
                    foreach (MemberInfo member in members)
                    {
                        if (member is MethodInfo)
                        {
                            MethodKindEnum kind = MethodKindEnum.Actual;
                            if (member.Name == SpecialNames.Invoke)
                            {
                                kind = MethodKindEnum.Invoke;
                            }
                            else if (member.Name == SpecialNames.ImplicitConversion)
                            {
                                kind = MethodKindEnum.ImplicitConv;
                            }
                            else if (member.Name == SpecialNames.ExplicitConversion)
                            {
                                kind = MethodKindEnum.ExplicitConv;
                            }
                            AddMethodToSymbolTable(
                                member,
                                aggregate,
                                kind);
                        }
                        else if (member is ConstructorInfo)
                        {
                            AddMethodToSymbolTable(
                                member,
                                aggregate,
                                MethodKindEnum.Constructor);
                        }
                        else if (member is PropertyInfo)
                        {
                            AddPropertyToSymbolTable(member as PropertyInfo, aggregate);
                        }
                        else if (member is FieldInfo)
                        {
                            // Store this field so that if we also find an event, we can
                            // mark it as the backing field of the event.
                            Debug.Assert(addedField == null);
                            addedField = AddFieldToSymbolTable(member as FieldInfo, aggregate);
                        }
                    }
                    foreach (EventInfo e in events)
                    {
                        AddEventToSymbolTable(e, aggregate, addedField);
                    }

                    result = result.Concat(members);
                }

                _namesLoadedForEachType.Add(key);
            }

            return result;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private List<Type> CreateInheritanceHierarchyList(Type type)
        {
            List<Type> list = new List<Type>();
            list.Insert(0, type);
            for (Type parent = type.GetTypeInfo().BaseType; parent != null; parent = parent.GetTypeInfo().BaseType)
            {
                // Load it in the symbol table.
                LoadSymbolsFromType(parent);

                // Insert into our list of Types.
                list.Insert(0, parent);
            }

            // If we have a WinRT type then we should load the members of it's collection interfaces
            // as well as those members are on this type as far as the user is concerned.
            CType ctype = GetCTypeFromType(type);
            if (ctype.IsWindowsRuntimeType())
            {
                TypeArray collectioniFaces = ctype.AsAggregateType().GetWinRTCollectionIfacesAll(_semanticChecker.GetSymbolLoader());

                for (int i = 0; i < collectioniFaces.size; i++)
                {
                    CType collectionType = collectioniFaces.Item(i);
                    Debug.Assert(collectionType.isInterfaceType());

                    // Insert into our list of Types.
                    list.Insert(0, collectionType.AssociatedSystemType);
                }
            }
            return list;
        }
        #endregion

        #region GetName
        /////////////////////////////////////////////////////////////////////////////////

        private Name GetName(string p)
        {
            if (p == null)
            {
                p = string.Empty;
            }
            return GetName(p, _nameManager);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Name GetName(Type type)
        {
            string name = type.Name;
            if (type.GetTypeInfo().IsGenericType)
            {
                // Trim the name to remove the ` at the end.
                name = name.Split('`')[0];
            }
            return GetName(name, _nameManager);
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal static Name GetName(string p, NameManager nameManager)
        {
            Name name = nameManager.Lookup(p);
            if (name == null)
            {
                return nameManager.Add(p);
            }
            return name;
        }
        #endregion

        #region TypeParameters
        /////////////////////////////////////////////////////////////////////////////////

        private TypeArray GetMethodTypeParameters(MethodInfo method, MethodSymbol parent)
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
                    ctypes[i].AsTypeParameterType().GetTypeParameterSymbol().SetBounds(
                        _bsymmgr.AllocParams(
                        GetCTypeArrayFromTypes(t.GetTypeInfo().GetGenericParameterConstraints())));
                }
                return _bsymmgr.AllocParams(ctypes.Length, ctypes);
            }
            return BSYMMGR.EmptyTypeArray();
        }

        /////////////////////////////////////////////////////////////////////////////////

        private TypeArray GetAggregateTypeParameters(Type type, AggregateSymbol agg)
        {
            if (type.GetTypeInfo().IsGenericType)
            {
                Type genericDefinition = type.GetTypeInfo().GetGenericTypeDefinition();
                Type[] genericArguments = genericDefinition.GetGenericArguments();
                List<CType> ctypes = new List<CType>();
                int outerParameters = agg.isNested() ? agg.GetOuterAgg().GetTypeVarsAll().size : 0;

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

                    CType ctype = null;
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
                    if (ctype.AsTypeParameterType().GetOwningSymbol() == agg)
                    {
                        ctypes.Add(ctype);
                    }
                }
                return _bsymmgr.AllocParams(ctypes.Count, ctypes.ToArray());
            }
            return BSYMMGR.EmptyTypeArray();
        }

        /////////////////////////////////////////////////////////////////////////////////

        private TypeParameterType LoadClassTypeParameter(AggregateSymbol parent, Type t)
        {
            for (AggregateSymbol p = parent; p != null; p = p.parent.IsAggregateSymbol() ? p.parent.AsAggregateSymbol() : null)
            {
                for (TypeParameterSymbol typeParam = _bsymmgr.LookupAggMember(
                        GetName(t), p, symbmask_t.MASK_TypeParameterSymbol) as TypeParameterSymbol;
                    typeParam != null;
                    typeParam = BSYMMGR.LookupNextSym(typeParam, p, symbmask_t.MASK_TypeParameterSymbol) as TypeParameterSymbol)
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

        private bool AreTypeParametersEquivalent(Type t1, Type t2)
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
        private Type GetOriginalTypeParameterType(Type t)
        {
            Debug.Assert(t.IsGenericParameter);

            int pos = t.GenericParameterPosition;

            Type parentType = t.DeclaringType;
            if (parentType != null && parentType.GetTypeInfo().IsGenericType)
            {
                parentType = parentType.GetTypeInfo().GetGenericTypeDefinition();
            }

            if (t.GetTypeInfo().DeclaringMethod != null)
            {
                MethodBase parentMethod = t.GetTypeInfo().DeclaringMethod;

                if (parentType.GetGenericArguments() == null || pos >= parentType.GetGenericArguments().Length)
                {
                    return t;
                }
            }

            while (parentType.GetGenericArguments().Length > pos)
            {
                Type nextParent = parentType.DeclaringType;
                if (nextParent != null && nextParent.GetTypeInfo().IsGenericType)
                {
                    nextParent = nextParent.GetTypeInfo().GetGenericTypeDefinition();
                }

                if (nextParent != null && nextParent.GetGenericArguments() != null && nextParent.GetGenericArguments().Length > pos)
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

        private TypeParameterType LoadMethodTypeParameter(MethodSymbol parent, Type t)
        {
            for (Symbol sym = parent.firstChild; sym != null; sym = sym.nextChild)
            {
                if (!sym.IsTypeParameterSymbol())
                {
                    continue;
                }

                if (AreTypeParametersEquivalent(sym.AsTypeParameterSymbol().GetTypeParameterType().AssociatedSystemType, t))
                {
                    return sym.AsTypeParameterSymbol().GetTypeParameterType();
                }
            }
            return AddTypeParameterToSymbolTable(null, parent, t, false);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private TypeParameterType AddTypeParameterToSymbolTable(
                AggregateSymbol agg,
                MethodSymbol meth,
                Type t,
                bool bIsAggregate)
        {
            Debug.Assert((agg != null && bIsAggregate) || (meth != null && !bIsAggregate));

            TypeParameterSymbol typeParam;
            if (bIsAggregate)
            {
                typeParam = _symFactory.CreateClassTypeParameter(
                    GetName(t),
                    agg,
                    t.GenericParameterPosition,
                    t.GenericParameterPosition);
            }
            else
            {
                typeParam = _symFactory.CreateMethodTypeParameter(
                    GetName(t),
                    meth,
                    t.GenericParameterPosition,
                    t.GenericParameterPosition);
            }

            if ((t.GetTypeInfo().GenericParameterAttributes & GenericParameterAttributes.Covariant) != 0)
            {
                typeParam.Covariant = true;
            }
            if ((t.GetTypeInfo().GenericParameterAttributes & GenericParameterAttributes.Contravariant) != 0)
            {
                typeParam.Contravariant = true;
            }

            SpecCons cons = SpecCons.None;

            if ((t.GetTypeInfo().GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
            {
                cons |= SpecCons.New;
            }
            if ((t.GetTypeInfo().GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            {
                cons |= SpecCons.Ref;
            }
            if ((t.GetTypeInfo().GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            {
                cons |= SpecCons.Val;
            }

            typeParam.SetConstraints(cons);
            typeParam.SetAccess(ACCESS.ACC_PUBLIC);
            TypeParameterType typeParamType = _typeManager.GetTypeParameter(typeParam);

            return typeParamType;
        }

        #endregion

        #region LoadTypeChain
        /////////////////////////////////////////////////////////////////////////////////

        private CType LoadSymbolsFromType(Type originalType)
        {
            List<object> declarationChain = BuildDeclarationChain(originalType);

            Type type = originalType;
            CType ret = null;
            bool bIsByRef = type.IsByRef;
            if (bIsByRef)
            {
                type = type.GetElementType();
            }

            NamespaceOrAggregateSymbol current = _rootNamespace;

            // Go through the declaration chain and add namespaces and types for 
            // each element in the chain.
            for (int i = 0; i < declarationChain.Count; i++)
            {
                object o = declarationChain[i];
                NamespaceOrAggregateSymbol next;
                if (o is Type)
                {
                    Type t = o as Type;
                    Name name = null;
                    name = GetName(t);
                    next = _symbolTable.LookupSym(name, current, symbmask_t.MASK_AggregateSymbol).AsAggregateSymbol();

                    // Make sure we match arity as well when we find an aggregate.
                    if (next != null)
                    {
                        next = FindSymWithMatchingArity(next as AggregateSymbol, t);
                    }

                    // In the event that two different types exist that have the same name, they
                    // cannot both have entries in the symbol table with our current architecture.
                    // This can happen in dynamic, since the runtime binder lives across all
                    // call sites in an appdomain, and assemblies can have been loaded at runtime
                    // that have different types with the same name. 

                    // In the real compiler, this would have been an error and name lookup would
                    // be ambiguous, but here we never have to lookup names of types for real (only
                    // names of members).

                    // The tactical fix is this: if we encounter this situation, where we have
                    // identically named types that are not the same, then we are going to clear
                    // the entire symbol table and restart this binding. This solution is not
                    // without its own problems, since it is possible to conceive of a single
                    // dynamic binding that needs to simultaneously know about both of the 
                    // similarly named types, but we are not going to try to solve that
                    // scenario here.

                    if (next != null && next is AggregateSymbol)
                    {
                        Type existingType = (next as AggregateSymbol).AssociatedSystemType;
                        Type newType = t.GetTypeInfo().IsGenericType ? t.GetTypeInfo().GetGenericTypeDefinition() : t;

                        // We use "IsEquivalentTo" so that unified local types for NoPIA do
                        // not trigger a reset. There are other mechanisms to make those sorts
                        // of types work in some scenarios.
                        if (!existingType.IsEquivalentTo(newType))
                        {
                            throw new ResetBindException();
                        }
                    }

                    // If we haven't found this type yet, then add it to our symbol table.
                    if (next == null || t.IsNullableType())
                    {
                        // Note that if we have anything other than an AggregateSymbol, 
                        // we must be at the end of the line - that is, nothing else can
                        // have children.

                        CType ctype = ProcessSpecialTypeInChain(current, t);
                        if (ctype != null)
                        {
                            // If we had an aggregate type, its possible we're not at the end.
                            // This will happen for nullable<T> for instance.
                            if (ctype.IsAggregateType())
                            {
                                next = ctype.AsAggregateType().GetOwningAggregate();
                            }
                            else
                            {
                                ret = ctype;
                                break;
                            }
                        }
                        else
                        {
                            // This is a regular class.
                            next = AddAggregateToSymbolTable(current, t);
                        }
                    }

                    if (t == type)
                    {
                        ret = GetConstructedType(type, next.AsAggregateSymbol());
                        break;
                    }
                }
                else if (o is MethodInfo)
                {
                    // We cant be at the end.
                    Debug.Assert(i + 1 < declarationChain.Count);
                    ret = ProcessMethodTypeParameter(o as MethodInfo, declarationChain[++i] as Type, current as AggregateSymbol);
                    break;
                }
                else
                {
                    Debug.Assert(o is string);
                    next = AddNamespaceToSymbolTable(current, o as string);
                }
                current = next;
            }

            Debug.Assert(ret != null);
            if (bIsByRef)
            {
                ret = _typeManager.GetParameterModifier(ret, false);
            }
            return ret;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private TypeParameterType ProcessMethodTypeParameter(MethodInfo methinfo, Type t, AggregateSymbol parent)
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

        private CType GetConstructedType(Type type, AggregateSymbol agg)
        {
            // We've found the one we want, so return it.
            if (type.GetTypeInfo().IsGenericType)
            {
                // If we're a generic type, then we need to add the type arguments.
                List<CType> types = new List<CType>();

                foreach (Type argument in type.GetGenericArguments())
                {
                    types.Add(GetCTypeFromType(argument));
                }

                TypeArray typeArray = _bsymmgr.AllocParams(types.ToArray());
                AggregateType aggType = _typeManager.GetAggregate(agg, typeArray);
                return aggType;
            }
            CType ctype = agg.getThisType();
            return ctype;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private CType ProcessSpecialTypeInChain(NamespaceOrAggregateSymbol parent, Type t)
        {
            CType ctype;
            if (t.IsGenericParameter)
            {
                AggregateSymbol agg = parent as AggregateSymbol;
                Debug.Assert(agg != null);
                ctype = LoadClassTypeParameter(agg, t);
                return ctype;
            }
            else if (t.IsArray)
            {
                // Now we return an array of nesting level corresponding to the rank.
                ctype = _typeManager.GetArray(GetCTypeFromType(t.GetElementType()), t.GetArrayRank());
                return ctype;
            }
            else if (t.IsPointer)
            {
                // Now we return the pointer type that we want.
                ctype = _typeManager.GetPointer(GetCTypeFromType(t.GetElementType()));
                return ctype;
            }
            else if (t.IsNullableType())
            {
                // Get a nullable type of the underlying type.
                if (t.GetGenericArguments()[0].DeclaringType == t)
                {
                    // If the generic argument for nullable is our child, then we're 
                    // declaring the initial Nullable<T>.
                    AggregateSymbol agg = _symbolTable.LookupSym(
                        GetName(t), parent, symbmask_t.MASK_AggregateSymbol).AsAggregateSymbol();
                    if (agg != null)
                    {
                        agg = FindSymWithMatchingArity(agg, t);
                        if (agg != null)
                        {
                            Debug.Assert(agg.getThisType().AssociatedSystemType == t);
                            return agg.getThisType();
                        }
                    }
                    return AddAggregateToSymbolTable(parent, t).getThisType();
                }
                ctype = _typeManager.GetNullable(GetCTypeFromType(t.GetGenericArguments()[0]));
                return ctype;
            }
            return null;
        }

        /////////////////////////////////////////////////////////////////////////////////

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

                if (t.IsGenericParameter && t.GetTypeInfo().DeclaringMethod != null)
                {
                    MethodBase methodBase = t.GetTypeInfo().DeclaringMethod;
                    ParameterInfo[] parameters = methodBase.GetParameters();

                    bool bAdded = false;
#if UNSUPPORTEDAPI
                    foreach (MethodInfo methinfo in Enumerable.Where(t.DeclaringType.GetRuntimeMethods(), m => m.MetadataToken == methodBase.MetadataToken))
#else
                    foreach (MethodInfo methinfo in Enumerable.Where(t.DeclaringType.GetRuntimeMethods(), m => m.HasSameMetadataDefinitionAs(methodBase)))
#endif
                    {
                        if (!methinfo.IsGenericMethod)
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
                string[] namespaces = callingType.Namespace.Split('.');
                int index = 0;
                foreach (string s in namespaces)
                {
                    callChain.Insert(index++, s);
                }
            }
            return callChain;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private AggregateSymbol FindSymWithMatchingArity(AggregateSymbol aggregateSymbol, Type type)
        {
            for (AggregateSymbol agg = aggregateSymbol;
                agg != null;
                agg = BSYMMGR.LookupNextSym(agg, agg.Parent, symbmask_t.MASK_AggregateSymbol) as AggregateSymbol)
            {
                if (agg.GetTypeVarsAll().size == type.GetGenericArguments().Length)
                {
                    return agg;
                }
            }
            return null;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private NamespaceSymbol AddNamespaceToSymbolTable(NamespaceOrAggregateSymbol parent, string sz)
        {
            Name name = GetName(sz);
            NamespaceSymbol ns = _symbolTable.LookupSym(name, parent, symbmask_t.MASK_NamespaceSymbol).AsNamespaceSymbol();
            if (ns == null)
            {
                ns = _symFactory.CreateNamespace(name, parent as NamespaceSymbol);
            }
            ns.AddAid(KAID.kaidGlobal);
            ns.AddAid(KAID.kaidThisAssembly);
            ns.AddAid(_infile.GetAssemblyID());

            return ns;
        }
        #endregion

        #region CTypeFromType
        /////////////////////////////////////////////////////////////////////////////////

        internal CType[] GetCTypeArrayFromTypes(IList<Type> types)
        {
            if (types == null)
            {
                return null;
            }

            CType[] ctypes = new CType[types.Count];

            int i = 0;
            foreach (Type t in types)
            {
                Debug.Assert(t != null);
                ctypes[i++] = GetCTypeFromType(t);
            }

            return ctypes;
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal CType GetCTypeFromType(Type t)
        {
            return LoadSymbolsFromType(t);
        }
        #endregion

        #region Aggregates
        /////////////////////////////////////////////////////////////////////////////////

        private AggregateSymbol AddAggregateToSymbolTable(
            NamespaceOrAggregateSymbol parent,
            Type type)
        {
            AggregateSymbol agg = _symFactory.CreateAggregate(GetName(type), parent, _infile, _typeManager);
            agg.AssociatedSystemType = type.GetTypeInfo().IsGenericType ? type.GetTypeInfo().GetGenericTypeDefinition() : type;
            agg.AssociatedAssembly = type.GetTypeInfo().Assembly;

            // We have to set the TypeVars, access, and the AggKind before we can set the aggState
            // because of the assertion checking the compiler does.
            AggKindEnum kind;
            if (type.GetTypeInfo().IsInterface)
            {
                kind = AggKindEnum.Interface;
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                kind = AggKindEnum.Enum;
                agg.SetUnderlyingType(GetCTypeFromType(Enum.GetUnderlyingType(type)).AsAggregateType());
            }
            else if (type.GetTypeInfo().IsValueType)
            {
                kind = AggKindEnum.Struct;
            }
            else
            {
                // If it derives from Delegate or MulticastDelegate, then its
                // a delegate type. However, MuticastDelegate itself is not a 
                // delegate type.
                if (type.GetTypeInfo().BaseType != null &&
                    (type.GetTypeInfo().BaseType.FullName == "System.MulticastDelegate" ||
                    type.GetTypeInfo().BaseType.FullName == "System.Delegate") &&
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
            agg.SetTypeVars(BSYMMGR.EmptyTypeArray());

            ACCESS access;
            if (type.GetTypeInfo().IsPublic)
            {
                access = ACCESS.ACC_PUBLIC;
            }
            else if (type.GetTypeInfo().IsNested)
            {
                // If its nested, we may have other accessibility options.
                if (type.GetTypeInfo().IsNestedAssembly || type.GetTypeInfo().IsNestedFamANDAssem)
                {
                    // Note that we don't directly support NestedFamANDAssem, but we're just
                    // going to default to internal.
                    access = ACCESS.ACC_INTERNAL;
                }
                else if (type.GetTypeInfo().IsNestedFamORAssem)
                {
                    access = ACCESS.ACC_INTERNALPROTECTED;
                }
                else if (type.GetTypeInfo().IsNestedPrivate)
                {
                    access = ACCESS.ACC_PRIVATE;
                }
                else if (type.GetTypeInfo().IsNestedFamily)
                {
                    access = ACCESS.ACC_PROTECTED;
                }
                else
                {
                    Debug.Assert(type.GetTypeInfo().IsPublic || type.GetTypeInfo().IsNestedPublic);
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

            if (type.GetTypeInfo().IsGenericType)
            {
                Type genericDefinition = type.GetTypeInfo().GetGenericTypeDefinition();
                Type[] genericArguments = genericDefinition.GetGenericArguments();

                // After we load the type parameters, we need to resolve their bounds.
                for (int i = 0; i < agg.GetTypeVars().size; i++)
                {
                    Type t = genericArguments[i];
                    if (agg.GetTypeVars().Item(i).IsTypeParameterType())
                    {
                        agg.GetTypeVars().Item(i).AsTypeParameterType().GetTypeParameterSymbol().SetBounds(
                            _bsymmgr.AllocParams(
                            GetCTypeArrayFromTypes(t.GetTypeInfo().GetGenericParameterConstraints())));
                    }
                }
            }

            agg.SetAnonymousType(false);
            agg.SetAbstract(type.GetTypeInfo().IsAbstract);

            {
                string typeName = type.FullName;
                if (type.GetTypeInfo().IsGenericType)
                {
                    typeName = type.GetTypeInfo().GetGenericTypeDefinition().FullName;
                }
                if (typeName != null && PredefinedTypeFacts.IsPredefinedType(typeName))
                {
                    PredefinedTypes.InitializePredefinedType(agg, PredefinedTypeFacts.GetPredefTypeIndex(typeName));
                }
            }
            agg.SetLayoutError(false);
            agg.SetSealed(type.GetTypeInfo().IsSealed);
            agg.SetUnmanagedStruct(false);
            agg.SetManagedStruct(false);
            agg.SetHasExternReference(false);

            agg.SetComImport(type.GetTypeInfo().IsImport);

            AggregateType baseAggType = agg.getThisType();
            if (type.GetTypeInfo().BaseType != null)
            {
                // type.GetTypeInfo().BaseType can be null for Object or for interface types.
                Type t = type.GetTypeInfo().BaseType;
                if (t.GetTypeInfo().IsGenericType)
                {
                    t = t.GetTypeInfo().GetGenericTypeDefinition();
                }
                agg.SetBaseClass(GetCTypeFromType(t).AsAggregateType());
            }
            agg.SetTypeManager(_typeManager);
            agg.SetFirstUDConversion(null);
            SetInterfacesOnAggregate(agg, type);
            agg.SetHasPubNoArgCtor(Enumerable.Any(type.GetConstructors(), c => c.GetParameters().Length == 0));

            // If we have a delegate, get its invoke and constructor methods as well.
            if (agg.IsDelegate())
            {
                PopulateSymbolTableWithName(SpecialNames.Constructor, null, type);
                PopulateSymbolTableWithName(SpecialNames.Invoke, null, type);
            }

            return agg;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private void SetInterfacesOnAggregate(AggregateSymbol aggregate, Type type)
        {
            if (type.GetTypeInfo().IsGenericType)
            {
                type = type.GetTypeInfo().GetGenericTypeDefinition();
            }
            Type[] interfaces = type.GetTypeInfo().ImplementedInterfaces.ToArray();

            // We wont be able to find the difference between Ifaces and 
            // IfacesAll anymore - at runtime, the class implements all of its
            // Ifaces and IfacesAll, so theres no way to differentiate.
            //
            // This actually doesn't matter though - for conversions and methodcalls,
            // we don't really care where they've come from as long as we know the overall
            // set of IfacesAll.

            aggregate.SetIfaces(_bsymmgr.AllocParams(interfaces.Length, GetCTypeArrayFromTypes(interfaces)));
            aggregate.SetIfacesAll(aggregate.GetIfaces());
        }
        #endregion

        #region Field
        /////////////////////////////////////////////////////////////////////////////////

        private FieldSymbol AddFieldToSymbolTable(FieldInfo fieldInfo, AggregateSymbol aggregate)
        {
            FieldSymbol field = _symbolTable.LookupSym(
                GetName(fieldInfo.Name),
                aggregate,
                symbmask_t.MASK_FieldSymbol) as FieldSymbol;
            if (field != null)
            {
                return field;
            }

            field = _symFactory.CreateMemberVar(GetName(fieldInfo.Name), aggregate, null, 0);
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
            else if (fieldInfo.IsAssembly || fieldInfo.IsFamilyAndAssembly)
            {
                access = ACCESS.ACC_INTERNAL;
            }
            else
            {
                Debug.Assert(fieldInfo.IsFamilyOrAssembly);
                access = ACCESS.ACC_INTERNALPROTECTED;
            }
            field.SetAccess(access);
            field.isReadOnly = fieldInfo.IsInitOnly;
            field.isEvent = false;
            field.isAssigned = true;
            field.SetType(GetCTypeFromType(fieldInfo.FieldType));

            return field;
        }
        #endregion

        #region Events

        /////////////////////////////////////////////////////////////////////////////////

        private static readonly Type s_Sentinel = typeof(SymbolTable);
        private static Type s_EventRegistrationTokenType = s_Sentinel;
        private static Type s_WindowsRuntimeMarshal = s_Sentinel;
        private static Type s_EventRegistrationTokenTable = s_Sentinel;

        internal static Type EventRegistrationTokenType
        {
            get
            {
                return GetTypeByName(ref s_EventRegistrationTokenType, "System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken, System.Runtime.InteropServices.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            }
        }

        internal static Type WindowsRuntimeMarshalType
        {
            get
            {
                return GetTypeByName(ref s_WindowsRuntimeMarshal, "System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal, System.Runtime.InteropServices.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            }
        }

        private static Type EventRegistrationTokenTableType
        {
            get
            {
                return GetTypeByName(ref s_EventRegistrationTokenTable, "System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1, System.Runtime.InteropServices.WindowsRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            }
        }

        private static Type GetTypeByName(ref Type cachedResult, string name)
        {
            if ((object)cachedResult == s_Sentinel)
            {
                System.Threading.Interlocked.CompareExchange(ref cachedResult, Type.GetType(name, throwOnError: false), s_Sentinel);
            }

            return cachedResult;
        }

        private EventSymbol AddEventToSymbolTable(EventInfo eventInfo, AggregateSymbol aggregate, FieldSymbol addedField)
        {
            EventSymbol ev = _symbolTable.LookupSym(
                GetName(eventInfo.Name),
                aggregate,
                symbmask_t.MASK_EventSymbol) as EventSymbol;
            if (ev != null)
            {
                Debug.Assert(ev.AssociatedEventInfo == eventInfo);
                return ev;
            }

            ev = _symFactory.CreateEvent(GetName(eventInfo.Name), aggregate, null);
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

            Type eventRegistrationTokenType = EventRegistrationTokenType;
            if ((object)eventRegistrationTokenType != null && (object)WindowsRuntimeMarshalType != null &&
                ev.methAdd.RetType.AssociatedSystemType == eventRegistrationTokenType &&
                ev.methRemove.Params.Item(0).AssociatedSystemType == eventRegistrationTokenType)
            {
                ev.IsWindowsRuntimeEvent = true;
            }

            // If we imported a field on the same aggregate, with the same name, and it also
            // has the same type, then that field is the backing field for this event, and
            // we mark it as such. This is used for the CSharpIsEventBinder.
            // In the case of a WindowsRuntime event, the field will be of type
            // EventRegistrationTokenTable<delegateType>.
            Type eventRegistrationTokenTableType;
            if (addedField != null && addedField.GetType() != null &&
                (addedField.GetType() == ev.type ||
                (
                    addedField.GetType().AssociatedSystemType.IsConstructedGenericType &&
                    (object)(eventRegistrationTokenTableType = EventRegistrationTokenTableType) != null &&
                    addedField.GetType().AssociatedSystemType.GetGenericTypeDefinition() == eventRegistrationTokenTableType &&
                    addedField.GetType().AssociatedSystemType.GenericTypeArguments[0] == ev.type.AssociatedSystemType)
                ))
            {
                addedField.isEvent = true;
            }

            return ev;
        }
        #endregion

        #region Properties
        /////////////////////////////////////////////////////////////////////////////////

        internal void AddPredefinedPropertyToSymbolTable(AggregateSymbol type, Name property)
        {
            AggregateType aggtype = type.getThisType();
            Type t = aggtype.AssociatedSystemType;

            var props = Enumerable.Where(t.GetRuntimeProperties(), x => x.Name == property.Text);

            foreach (PropertyInfo pi in props)
            {
                AddPropertyToSymbolTable(pi, type);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private PropertySymbol AddPropertyToSymbolTable(PropertyInfo property, AggregateSymbol aggregate)
        {
            Name name;
            bool isIndexer = property.GetIndexParameters() != null && property.GetIndexParameters().Length != 0;

            if (isIndexer)
            {
                name = GetName(SpecialNames.Indexer);
            }
            else
            {
                name = GetName(property.Name);
            }
            PropertySymbol prop = _symbolTable.LookupSym(
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
                        return prop;
                    }

                    prevProp = prop;
                    prop = _semanticChecker.SymbolLoader.LookupNextSym(prop, prop.parent, symbmask_t.MASK_PropertySymbol).AsPropertySymbol();
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
                    prop = _semanticChecker.GetSymbolLoader().GetGlobalMiscSymFactory().CreateIndexer(name, aggregate, GetName(property.Name), null);
                    prop.Params = CreateParameterArray(null, property.GetIndexParameters());
                }
                else
                {
                    prop = _symFactory.CreateProperty(GetName(property.Name), aggregate, null);
                    prop.Params = BSYMMGR.EmptyTypeArray();
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
                prop.isOverride = accessor.IsVirtual && accessor.IsHideBySig && accessor.GetRuntimeBaseDefinition() != accessor;
                prop.isHideByName = !accessor.IsHideBySig;
            }

            SetParameterDataForMethProp(prop, property.GetIndexParameters());

            // Get and set.
            MethodInfo methGet = property.GetMethod;
            MethodInfo methSet = property.SetMethod;
            ACCESS access = ACCESS.ACC_PRIVATE;
            if (methGet != null)
            {
                prop.methGet = AddMethodToSymbolTable(methGet, aggregate, MethodKindEnum.PropAccessor);

                // If we have an indexed property, leave the method as a method we can call,
                // and mark the property as bogus.
                if (isIndexer || prop.methGet.Params.size == 0)
                {
                    prop.methGet.SetProperty(prop);
                }
                else
                {
                    prop.setBogus(true);
                    prop.methGet.SetMethKind(MethodKindEnum.Actual);
                }

                if (prop.methGet.GetAccess() > access)
                {
                    access = prop.methGet.GetAccess();
                }
            }
            if (methSet != null)
            {
                prop.methSet = AddMethodToSymbolTable(methSet, aggregate, MethodKindEnum.PropAccessor);

                // If we have an indexed property, leave the method as a method we can call,
                // and mark the property as bogus.
                if (isIndexer || prop.methSet.Params.size == 1)
                {
                    prop.methSet.SetProperty(prop);
                }
                else
                {
                    prop.setBogus(true);
                    prop.methSet.SetMethKind(MethodKindEnum.Actual);
                }

                if (prop.methSet.GetAccess() > access)
                {
                    access = prop.methSet.GetAccess();
                }
            }

            // The access of the property is the least restrictive access of its getter/setter.
            prop.SetAccess(access);

            return prop;
        }

        #endregion

        #region Methods
        /////////////////////////////////////////////////////////////////////////////////

        internal void AddPredefinedMethodToSymbolTable(AggregateSymbol type, Name methodName)
        {
            Type t = type.getThisType().AssociatedSystemType;

            // If we got here, it means we couldn't find it in our initial lookup. Means we haven't loaded it from reflection yet.
            // Lets go and do that now.
            // Check if we have constructors or not.
            if (methodName == _nameManager.GetPredefinedName(PredefinedName.PN_CTOR))
            {
                var ctors = Enumerable.Where(t.GetConstructors(), m => m.Name == methodName.Text);

                foreach (ConstructorInfo c in ctors)
                {
                    AddMethodToSymbolTable(
                        c,
                        type,
                        MethodKindEnum.Constructor);
                }
            }
            else
            {
                var methods = Enumerable.Where(t.GetRuntimeMethods(), m => m.Name == methodName.Text && m.DeclaringType == t);

                foreach (MethodInfo m in methods)
                {
                    AddMethodToSymbolTable(
                        m,
                        type,
                        m.Name == SpecialNames.Invoke ? MethodKindEnum.Invoke : MethodKindEnum.Actual);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private MethodSymbol AddMethodToSymbolTable(MemberInfo member, AggregateSymbol callingAggregate, MethodKindEnum kind)
        {
            MethodInfo method = member as MethodInfo;
            ConstructorInfo ctor = member as ConstructorInfo;

            Debug.Assert(method != null || ctor != null);
#if UNSUPPORTEDAPI
            Debug.Assert(member.DeclaringType == member.ReflectedType);
#endif
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

            ParameterInfo[] parameters = method != null ? method.GetParameters() : ctor.GetParameters();
            // First create the method.
            methodSymbol = _symFactory.CreateMethod(GetName(member.Name), callingAggregate, null);
            methodSymbol.AssociatedMemberInfo = member;
            methodSymbol.SetMethKind(kind);
            if (kind == MethodKindEnum.ExplicitConv || kind == MethodKindEnum.ImplicitConv)
            {
                callingAggregate.SetHasConversion();
                methodSymbol.SetConvNext(callingAggregate.GetFirstUDConversion());
                callingAggregate.SetFirstUDConversion(methodSymbol);
            }
            ACCESS access;
            if (method != null)
            {
                if (method.IsPublic)
                {
                    access = ACCESS.ACC_PUBLIC;
                }
                else if (method.IsPrivate)
                {
                    access = ACCESS.ACC_PRIVATE;
                }
                else if (method.IsFamily)
                {
                    access = ACCESS.ACC_PROTECTED;
                }
                else if (method.IsAssembly || method.IsFamilyAndAssembly)
                {
                    access = ACCESS.ACC_INTERNAL;
                }
                else
                {
                    Debug.Assert(method.IsFamilyOrAssembly);
                    access = ACCESS.ACC_INTERNALPROTECTED;
                }
            }
            else
            {
                Debug.Assert(ctor != null);
                if (ctor.IsPublic)
                {
                    access = ACCESS.ACC_PUBLIC;
                }
                else if (ctor.IsPrivate)
                {
                    access = ACCESS.ACC_PRIVATE;
                }
                else if (ctor.IsFamily)
                {
                    access = ACCESS.ACC_PROTECTED;
                }
                else if (ctor.IsAssembly || ctor.IsFamilyAndAssembly)
                {
                    access = ACCESS.ACC_INTERNAL;
                }
                else
                {
                    Debug.Assert(ctor.IsFamilyOrAssembly);
                    access = ACCESS.ACC_INTERNALPROTECTED;
                }
            }
            methodSymbol.SetAccess(access);

            methodSymbol.isExtension = false; // We don't support extension methods.
            methodSymbol.isExternal = false;

            if (method != null)
            {
                methodSymbol.typeVars = GetMethodTypeParameters(method, methodSymbol);
                methodSymbol.isVirtual = method.IsVirtual;
                methodSymbol.isAbstract = method.IsAbstract;
                methodSymbol.isStatic = method.IsStatic;
                methodSymbol.isOverride = method.IsVirtual && method.IsHideBySig && method.GetRuntimeBaseDefinition() != method;
                methodSymbol.isOperator = IsOperator(method);
                methodSymbol.swtSlot = GetSlotForOverride(method);
                methodSymbol.isVarargs = (method.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs;
                methodSymbol.RetType = GetCTypeFromType(method.ReturnType);
            }
            else
            {
                methodSymbol.typeVars = BSYMMGR.EmptyTypeArray();
                methodSymbol.isVirtual = ctor.IsVirtual;
                methodSymbol.isAbstract = ctor.IsAbstract;
                methodSymbol.isStatic = ctor.IsStatic;
                methodSymbol.isOverride = false;
                methodSymbol.isOperator = false;
                methodSymbol.swtSlot = null;
                methodSymbol.isVarargs = false;
                methodSymbol.RetType = _typeManager.GetVoid();
            }
            methodSymbol.modOptCount = GetCountOfModOpts(parameters);

            methodSymbol.useMethInstead = false;
            methodSymbol.isParamArray = DoesMethodHaveParameterArray(parameters);
            methodSymbol.isHideByName = false;

            methodSymbol.errExpImpl = null;
            methodSymbol.Params = CreateParameterArray(methodSymbol.AssociatedMemberInfo, parameters);
            methodSymbol.declaration = null;

            SetParameterDataForMethProp(methodSymbol, parameters);

            return methodSymbol;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private void SetParameterDataForMethProp(MethodOrPropertySymbol methProp, ParameterInfo[] parameters)
        {
            if (parameters.Length > 0)
            {
                // See if we have a param array.
                var attributes = parameters[parameters.Length - 1].GetCustomAttributes(false);
                if (attributes != null)
                {
                    foreach (object o in attributes)
                    {
                        if (o is ParamArrayAttribute)
                        {
                            methProp.isParamArray = true;
                        }
                    }
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

        private void SetParameterAttributes(MethodOrPropertySymbol methProp, ParameterInfo[] parameters, int i)
        {
            if (((parameters[i].Attributes & ParameterAttributes.Optional) != 0) &&
                !parameters[i].ParameterType.IsByRef)
            {
                methProp.SetOptionalParameter(i);
                PopulateSymbolTableWithName("Value", new Type[] { typeof(Missing) }, typeof(Missing)); // We might need this later
            }

            object[] attrs;

            // Get MarshalAsAttribute
            if ((parameters[i].Attributes & ParameterAttributes.HasFieldMarshal) != 0)
            {
                if ((attrs = parameters[i].GetCustomAttributes(typeof(MarshalAsAttribute), false).ToArray()) != null
                    && attrs.Length > 0)
                {
                    MarshalAsAttribute attr = (MarshalAsAttribute)attrs[0];
                    methProp.SetMarshalAsParameter(i, attr.Value);
                }
            }

            // Get the various kinds of default values
            if ((attrs = parameters[i].GetCustomAttributes(typeof(DateTimeConstantAttribute), false).ToArray()) != null
                && attrs.Length > 0)
            {
                // Get DateTimeConstant

                DateTimeConstantAttribute attr = (DateTimeConstantAttribute)attrs[0];

                ConstValFactory factory = new ConstValFactory();
                CONSTVAL cv = factory.Create(((DateTime)attr.Value).Ticks);
                CType cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_DATETIME);
                methProp.SetDefaultParameterValue(i, cvType, cv);
            }
            else if ((attrs = parameters[i].GetCustomAttributes(typeof(DecimalConstantAttribute), false).ToArray()) != null
                && attrs.Length > 0)
            {
                // Get DecimalConstant

                DecimalConstantAttribute attr = (DecimalConstantAttribute)attrs[0];

                ConstValFactory factory = new ConstValFactory();
                CONSTVAL cv = factory.Create(attr.Value);
                CType cvType = _semanticChecker.GetSymbolLoader().GetOptPredefType(PredefinedType.PT_DECIMAL);
                methProp.SetDefaultParameterValue(i, cvType, cv);
            }
            else if (((parameters[i].Attributes & ParameterAttributes.HasDefault) != 0) &&
                !parameters[i].ParameterType.IsByRef)
            {
                // Only set a default value if we have one, and the type that we're
                // looking at isn't a by ref type or a type parameter.

                ConstValFactory factory = new ConstValFactory();
                CONSTVAL cv = cv = ConstValFactory.GetNullRef();
                CType cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_OBJECT);

                // We need to use RawDefaultValue, because DefaultValue is too clever.
#if UNSUPPORTEDAPI
                if (parameters[i].RawDefaultValue != null)
                {
                    object defValue = parameters[i].RawDefaultValue;
#else
                if (parameters[i].DefaultValue != null)
                {
                    object defValue = parameters[i].DefaultValue;
#endif
                    Type defType = defValue.GetType();

                    if (defType == typeof(Byte))
                    {
                        cv = factory.Create((Byte)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_BYTE);
                    }
                    else if (defType == typeof(Int16))
                    {
                        cv = factory.Create((Int16)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_SHORT);
                    }
                    else if (defType == typeof(Int32))
                    {
                        cv = factory.Create((Int32)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_INT);
                    }
                    else if (defType == typeof(Int64))
                    {
                        cv = factory.Create((Int64)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_LONG);
                    }
                    else if (defType == typeof(Single))
                    {
                        cv = factory.Create((Single)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_FLOAT);
                    }
                    else if (defType == typeof(Double))
                    {
                        cv = factory.Create((Double)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_DOUBLE);
                    }
                    else if (defType == typeof(Decimal))
                    {
                        cv = factory.Create((Decimal)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_DECIMAL);
                    }
                    else if (defType == typeof(Char))
                    {
                        cv = factory.Create((Char)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_CHAR);
                    }
                    else if (defType == typeof(Boolean))
                    {
                        cv = factory.Create((Boolean)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_BOOL);
                    }
                    else if (defType == typeof(SByte))
                    {
                        cv = factory.Create((SByte)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_SBYTE);
                    }
                    else if (defType == typeof(UInt16))
                    {
                        cv = factory.Create((UInt16)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_USHORT);
                    }
                    else if (defType == typeof(UInt32))
                    {
                        cv = factory.Create((UInt32)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_UINT);
                    }
                    else if (defType == typeof(UInt64))
                    {
                        cv = factory.Create((UInt64)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_ULONG);
                    }
                    else if (defType == typeof(String))
                    {
                        cv = factory.Create((String)defValue);
                        cvType = _semanticChecker.GetSymbolLoader().GetReqPredefType(PredefinedType.PT_STRING);
                    }
                    // if we fall off the end of this cascading if, we get Object/null
                    // because that's how we initialized the constval.
                }
                methProp.SetDefaultParameterValue(i, cvType, cv);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private MethodSymbol FindMatchingMethod(MemberInfo method, AggregateSymbol callingAggregate)
        {
            MethodSymbol meth = _bsymmgr.LookupAggMember(GetName(method.Name), callingAggregate, symbmask_t.MASK_MethodSymbol).AsMethodSymbol();
            while (meth != null)
            {
                if (meth.AssociatedMemberInfo.IsEquivalentTo(method))
                {
                    return meth;
                }
                meth = BSYMMGR.LookupNextSym(meth, callingAggregate, symbmask_t.MASK_MethodSymbol).AsMethodSymbol();
            }
            return null;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private uint GetCountOfModOpts(ParameterInfo[] parameters)
        {
            uint count = 0;
#if UNSUPPORTEDAPI
            foreach (ParameterInfo p in parameters)
            {
                if (p.GetOptionalCustomModifiers() != null)
                {
                    count += (uint)p.GetOptionalCustomModifiers().Length;
                }
            }
#endif
            return count;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private TypeArray CreateParameterArray(MemberInfo associatedInfo, ParameterInfo[] parameters)
        {
            List<CType> types = new List<CType>();

            foreach (ParameterInfo p in parameters)
            {
                types.Add(GetTypeOfParameter(p, associatedInfo));
            }

            MethodInfo mi = associatedInfo as MethodInfo;

            if (mi != null && (mi.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
            {
                types.Add(_typeManager.GetArgListType());
            }

            return _bsymmgr.AllocParams(types.Count, types.ToArray());
        }

        /////////////////////////////////////////////////////////////////////////////////

        private CType GetTypeOfParameter(ParameterInfo p, MemberInfo m)
        {
            Type t = p.ParameterType;
            CType ctype;
            if (t.IsGenericParameter && t.GetTypeInfo().DeclaringMethod != null && t.GetTypeInfo().DeclaringMethod == m)
            {
                // If its a method type parameter from ourselves, just find it.
                ctype = LoadMethodTypeParameter(FindMethodFromMemberInfo(m), t);
            }
            else
            {
                ctype = GetCTypeFromType(t);
            }

            // Check if we have an out parameter.
            if (ctype.IsParameterModifierType() && p.IsOut && !p.IsIn)
            {
                CType parameterType = ctype.AsParameterModifierType().GetParameterType();
                ctype = _typeManager.GetParameterModifier(parameterType, true);
            }

            return ctype;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private bool DoesMethodHaveParameterArray(ParameterInfo[] parameters)
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

        private SymWithType GetSlotForOverride(MethodInfo method)
        {
            if (method.IsVirtual && method.IsHideBySig)
            {
                MethodInfo baseMethodInfo = method.GetRuntimeBaseDefinition();
                if (baseMethodInfo == method)
                {
                    // We just found ourselves, so we don't care here.
                    return null;
                }

                // We have the base class method that we're overriding. We can assume
                // that all the parent aggregate symbols were added, and that we added
                // the methods in order. As such, our parent methods should be in the 
                // symbol table at this point.

                AggregateSymbol aggregate = GetCTypeFromType(baseMethodInfo.DeclaringType).getAggregate();
                MethodSymbol baseMethod = FindMethodFromMemberInfo(baseMethodInfo);

                // This assert is temporarily disabled to improve testability of the area on .NetNative
                //Debug.Assert(baseMethod != null);
                if ((object)baseMethod == null)
                {
                    throw Error.InternalCompilerError();
                }

                return new SymWithType(baseMethod, aggregate.getThisType());
            }
            return null;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private MethodSymbol FindMethodFromMemberInfo(MemberInfo baseMemberInfo)
        {
            CType t = GetCTypeFromType(baseMemberInfo.DeclaringType);
            Debug.Assert(t.IsAggregateType());
            AggregateSymbol aggregate = t.getAggregate();
            Debug.Assert(aggregate != null);

            MethodSymbol meth = _semanticChecker.SymbolLoader.LookupAggMember(
                GetName(baseMemberInfo.Name),
                aggregate,
                symbmask_t.MASK_MethodSymbol).AsMethodSymbol();
            for (;
                    meth != null && !meth.AssociatedMemberInfo.IsEquivalentTo(baseMemberInfo);
                    meth = _semanticChecker.SymbolLoader.LookupNextSym(meth, aggregate, symbmask_t.MASK_MethodSymbol).AsMethodSymbol())
                ;

            return meth;
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal bool AggregateContainsMethod(AggregateSymbol agg, string szName, symbmask_t mask)
        {
            return _semanticChecker.SymbolLoader.LookupAggMember(GetName(szName), agg, mask) != null;
        }
        #endregion

        #region Conversions
        /////////////////////////////////////////////////////////////////////////////////

        internal void AddConversionsForType(Type type)
        {
            for (Type t = type; t.GetTypeInfo().BaseType != null; t = t.GetTypeInfo().BaseType)
            {
                AddConversionsForOneType(t);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private void AddConversionsForOneType(Type type)
        {
            if (type.GetTypeInfo().IsGenericType)
            {
                type = type.GetTypeInfo().GetGenericTypeDefinition();
            }

            if (_typesWithConversionsLoaded.Contains(type))
            {
                return;
            }
            _typesWithConversionsLoaded.Add(type);

            // Always make the aggregate for the type, regardless of whether or not
            // there are any conversions.
            CType t = GetCTypeFromType(type);

            if (!t.IsAggregateType())
            {
                CType endT;
                while ((endT = t.GetBaseOrParameterOrElementType()) != null)
                {
                    t = endT;
                }
            }

            if (t.IsTypeParameterType())
            {
                // Add conversions for the bounds.
                foreach (CType bound in t.AsTypeParameterType().GetBounds().ToArray())
                {
                    AddConversionsForType(bound.AssociatedSystemType);
                }
                return;
            }

            Debug.Assert(t is AggregateType);
            AggregateSymbol aggregate = t.AsAggregateType().getAggregate();

            // Now find all the conversions and make them.
            IEnumerable<MethodInfo> conversions = Enumerable.Where(type.GetRuntimeMethods(),
                                                    conversion => (conversion.IsPublic && conversion.IsStatic)
                                                      && (conversion.Name == SpecialNames.ImplicitConversion || conversion.Name == SpecialNames.ExplicitConversion)
                                                      && conversion.DeclaringType == type
                                                      && conversion.IsSpecialName
                                                      && !conversion.IsGenericMethod);

            foreach (MethodInfo conversion in conversions)
            {
                MethodSymbol method = AddMethodToSymbolTable(
                    conversion,
                    aggregate,
                    conversion.Name == SpecialNames.ImplicitConversion ?
                        MethodKindEnum.ImplicitConv :
                        MethodKindEnum.ExplicitConv);
            }
        }
        #endregion

        #region Operators
        /////////////////////////////////////////////////////////////////////////////////

        private bool IsOperator(MethodInfo method)
        {
            return method.IsSpecialName &&
                method.IsStatic &&
                (method.Name == SpecialNames.ImplicitConversion ||
                method.Name == SpecialNames.ExplicitConversion ||

                // Binary Operators
                method.Name == SpecialNames.CLR_Add ||
                method.Name == SpecialNames.CLR_Subtract ||
                method.Name == SpecialNames.CLR_Multiply ||
                method.Name == SpecialNames.CLR_Division ||
                method.Name == SpecialNames.CLR_Modulus ||
                method.Name == SpecialNames.CLR_LShift ||
                method.Name == SpecialNames.CLR_RShift ||
                method.Name == SpecialNames.CLR_LT ||
                method.Name == SpecialNames.CLR_GT ||
                method.Name == SpecialNames.CLR_LTE ||
                method.Name == SpecialNames.CLR_GTE ||
                method.Name == SpecialNames.CLR_Equality ||
                method.Name == SpecialNames.CLR_Inequality ||
                method.Name == SpecialNames.CLR_BitwiseAnd ||
                method.Name == SpecialNames.CLR_ExclusiveOr ||
                method.Name == SpecialNames.CLR_BitwiseOr ||
                method.Name == SpecialNames.CLR_LogicalNot ||

                // Binary inplace operators.
                method.Name == SpecialNames.CLR_InPlaceAdd ||
                method.Name == SpecialNames.CLR_InPlaceSubtract ||
                method.Name == SpecialNames.CLR_InPlaceMultiply ||
                method.Name == SpecialNames.CLR_InPlaceDivide ||
                method.Name == SpecialNames.CLR_InPlaceModulus ||
                method.Name == SpecialNames.CLR_InPlaceBitwiseAnd ||
                method.Name == SpecialNames.CLR_InPlaceExclusiveOr ||
                method.Name == SpecialNames.CLR_InPlaceBitwiseOr ||
                method.Name == SpecialNames.CLR_InPlaceLShift ||
                method.Name == SpecialNames.CLR_InPlaceRShift ||

                // Unary Operators
                method.Name == SpecialNames.CLR_UnaryNegation ||
                method.Name == SpecialNames.CLR_UnaryPlus ||
                method.Name == SpecialNames.CLR_OnesComplement ||
                method.Name == SpecialNames.CLR_True ||
                method.Name == SpecialNames.CLR_False ||

                method.Name == SpecialNames.CLR_PreIncrement ||
                method.Name == SpecialNames.CLR_PostIncrement ||
                method.Name == SpecialNames.CLR_PreDecrement ||
                method.Name == SpecialNames.CLR_PostDecrement);
        }
        #endregion
    }
}
