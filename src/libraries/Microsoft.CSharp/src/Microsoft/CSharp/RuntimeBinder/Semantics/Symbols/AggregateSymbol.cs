// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // Name used for AGGDECLs in the symbol table.

    // AggregateSymbol - a symbol representing an aggregate type. These are classes,
    // interfaces, and structs. Parent is a namespace or class. Children are methods,
    // properties, and member variables, and types (including its own AGGTYPESYMs).

    internal sealed class AggregateSymbol : NamespaceOrAggregateSymbol
    {
        public Type AssociatedSystemType;
        public Assembly AssociatedAssembly;

        // The instance type. Created when first needed.
        private AggregateType _atsInst;

        private AggregateType _pBaseClass;     // For a class/struct/enum, the base class. For iface: unused.
        private AggregateType _pUnderlyingType; // For enum, the underlying type. For iface, the resolved CoClass. Not used for class/struct.

        private TypeArray _ifaces;         // The explicit base interfaces for a class or interface.
        private TypeArray _ifacesAll;      // Recursive closure of base interfaces ordered so an iface appears before all of its base ifaces.

        private TypeArray _typeVarsThis; // Type variables for this generic class, as declarations.
        private TypeArray _typeVarsAll;     // The type variables for this generic class and all containing classes.

        // First UD conversion operator. This chain is for this type only (not base types).
        // The hasConversion flag indicates whether this or any base types have UD conversions.
        private MethodSymbol _pConvFirst;

        // ------------------------------------------------------------------------
        //
        // Put members that are bits under here in a contiguous section.
        //
        // ------------------------------------------------------------------------

        private AggKindEnum _aggKind;

        // Where this came from - fabricated, source, import
        // Fabricated AGGs have isSource == true but hasParseTree == false.
        // N.B.: in incremental builds, it is quite possible for
        // isSource==TRUE and hasParseTree==FALSE. Be
        // sure you use the correct variable for what you are trying to do!

        // Predefined
        private bool _isPredefined;    // A special predefined type.
        private PredefinedType _iPredef;        // index of the predefined type, if isPredefined.

        // Flags
        private bool _isAbstract;      // Can it be instantiated?
        private bool _isSealed;        // Can it be derived from?

        // Constructors
        private bool _hasPubNoArgCtor; // Whether it has a public instance constructor taking no args

        // User defined operators

        private bool _isSkipUDOps; // Never check for user defined operators on this type (eg, decimal, string, delegate).

        // When this is unset we don't know if we have conversions.  When this
        // is set it indicates if this type or any base type has user defined
        // conversion operators
        private bool? _hasConversion;

        // ----------------------------------------------------------------------------
        // AggregateSymbol
        // ----------------------------------------------------------------------------

        public AggregateSymbol GetBaseAgg()
        {
            return _pBaseClass?.OwningAggregate;
        }

        public AggregateType getThisType()
        {
            if (_atsInst == null)
            {
                Debug.Assert(GetTypeVars() == GetTypeVarsAll() || isNested());

                AggregateType pOuterType = isNested() ? GetOuterAgg().getThisType() : null;

                _atsInst = TypeManager.GetAggregate(this, pOuterType, GetTypeVars());
            }

            //Debug.Assert(GetTypeVars().Size == atsInst.GenericArguments.Count);
            return _atsInst;
        }

        public bool FindBaseAgg(AggregateSymbol agg)
        {
            for (AggregateSymbol aggT = this; aggT != null; aggT = aggT.GetBaseAgg())
            {
                if (aggT == agg)
                    return true;
            }
            return false;
        }

        public NamespaceOrAggregateSymbol Parent => parent as NamespaceOrAggregateSymbol;

        public bool isNested() => parent is AggregateSymbol;

        public AggregateSymbol GetOuterAgg() => parent as AggregateSymbol;

        public bool isPredefAgg(PredefinedType pt)
        {
            return _isPredefined && (PredefinedType)_iPredef == pt;
        }

        // ----------------------------------------------------------------------------
        // The following are the Accessor functions for AggregateSymbol.
        // ----------------------------------------------------------------------------

        public AggKindEnum AggKind()
        {
            return (AggKindEnum)_aggKind;
        }

        public void SetAggKind(AggKindEnum aggKind)
        {
            // NOTE: When importing can demote types:
            //  - enums with no underlying type go to struct
            //  - delegates which are abstract or have no .ctor/Invoke method goto class
            _aggKind = aggKind;

            //An interface is always abstract
            if (aggKind == AggKindEnum.Interface)
            {
                SetAbstract(true);
            }
        }

        public bool IsClass()
        {
            return AggKind() == AggKindEnum.Class;
        }

        public bool IsDelegate()
        {
            return AggKind() == AggKindEnum.Delegate;
        }

        public bool IsInterface()
        {
            return AggKind() == AggKindEnum.Interface;
        }

        public bool IsStruct()
        {
            return AggKind() == AggKindEnum.Struct;
        }

        public bool IsEnum()
        {
            return AggKind() == AggKindEnum.Enum;
        }

        public bool IsValueType()
        {
            return AggKind() == AggKindEnum.Struct || AggKind() == AggKindEnum.Enum;
        }

        public bool IsRefType()
        {
            return AggKind() == AggKindEnum.Class ||
                AggKind() == AggKindEnum.Interface || AggKind() == AggKindEnum.Delegate;
        }

        public bool IsStatic()
        {
            return (_isAbstract && _isSealed);
        }

        public bool IsAbstract()
        {
            return _isAbstract;
        }

        public void SetAbstract(bool @abstract)
        {
            _isAbstract = @abstract;
        }

        public bool IsPredefined()
        {
            return _isPredefined;
        }

        public void SetPredefined(bool predefined)
        {
            _isPredefined = predefined;
        }

        public PredefinedType GetPredefType()
        {
            return (PredefinedType)_iPredef;
        }

        public void SetPredefType(PredefinedType predef)
        {
            _iPredef = predef;
        }

        public bool IsSealed()
        {
            return _isSealed;
        }

        public void SetSealed(bool @sealed)
        {
            _isSealed = @sealed;
        }

        ////////////////////////////////////////////////////////////////////////////////

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public bool HasConversion()
        {
            SymbolTable.AddConversionsForType(AssociatedSystemType);

            if (!_hasConversion.HasValue)
            {
                // ok, we tried defining all the conversions, and we didn't get anything
                // for this type.  However, we will still think this type has conversions
                // if it's base type has conversions.
                _hasConversion = GetBaseAgg() != null && GetBaseAgg().HasConversion();
            }

            return _hasConversion.Value;
        }

        ////////////////////////////////////////////////////////////////////////////////

        public void SetHasConversion()
        {
            _hasConversion = true;
        }

        ////////////////////////////////////////////////////////////////////////////////

        public bool HasPubNoArgCtor()
        {
            return _hasPubNoArgCtor;
        }

        public void SetHasPubNoArgCtor(bool hasPubNoArgCtor)
        {
            _hasPubNoArgCtor = hasPubNoArgCtor;
        }

        public bool IsSkipUDOps()
        {
            return _isSkipUDOps;
        }

        public void SetSkipUDOps(bool skipUDOps)
        {
            _isSkipUDOps = skipUDOps;
        }

        public TypeArray GetTypeVars()
        {
            return _typeVarsThis;
        }

        public void SetTypeVars(TypeArray typeVars)
        {
            if (typeVars == null)
            {
                _typeVarsThis = null;
                _typeVarsAll = null;
            }
            else
            {
                TypeArray outerTypeVars;
                if (GetOuterAgg() != null)
                {
                    Debug.Assert(GetOuterAgg().GetTypeVars() != null);
                    Debug.Assert(GetOuterAgg().GetTypeVarsAll() != null);

                    outerTypeVars = GetOuterAgg().GetTypeVarsAll();
                }
                else
                {
                    outerTypeVars = TypeArray.Empty;
                }

                _typeVarsThis = typeVars;
                _typeVarsAll = TypeArray.Concat(outerTypeVars, typeVars);
            }
        }

        public TypeArray GetTypeVarsAll()
        {
            return _typeVarsAll;
        }

        public AggregateType GetBaseClass()
        {
            return _pBaseClass;
        }

        public void SetBaseClass(AggregateType baseClass)
        {
            _pBaseClass = baseClass;
        }

        public AggregateType GetUnderlyingType()
        {
            return _pUnderlyingType;
        }

        public void SetUnderlyingType(AggregateType underlyingType)
        {
            _pUnderlyingType = underlyingType;
        }

        public TypeArray GetIfaces()
        {
            return _ifaces;
        }

        public void SetIfaces(TypeArray ifaces)
        {
            _ifaces = ifaces;
        }

        public TypeArray GetIfacesAll()
        {
            return _ifacesAll;
        }

        public void SetIfacesAll(TypeArray ifacesAll)
        {
            _ifacesAll = ifacesAll;
        }

        public MethodSymbol GetFirstUDConversion()
        {
            return _pConvFirst;
        }

        public void SetFirstUDConversion(MethodSymbol conv)
        {
            _pConvFirst = conv;
        }

        public bool InternalsVisibleTo(Assembly assembly) => TypeManager.InternalsVisibleTo(AssociatedAssembly, assembly);
    }
}
