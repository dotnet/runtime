// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // Name used for AGGDECLs in the symbol table.

    // AggregateSymbol - a symbol representing an aggregate type. These are classes,
    // interfaces, and structs. Parent is a namespace or class. Children are methods,
    // properties, and member variables, and types (including its own AGGTYPESYMs).

    internal class AggregateSymbol : NamespaceOrAggregateSymbol
    {
        public Type AssociatedSystemType;
        public Assembly AssociatedAssembly;

        // This InputFile is some infile for the assembly containing this AggregateSymbol.
        // It is used for fast access to the filter BitSet and assembly ID.
        private InputFile _infile;

        // The instance type. Created when first needed.
        private AggregateType _atsInst;

        private AggregateType _pBaseClass;     // For a class/struct/enum, the base class. For iface: unused.
        private AggregateType _pUnderlyingType; // For enum, the underlying type. For iface, the resolved CoClass. Not used for class/struct.

        private TypeArray _ifaces;         // The explicit base interfaces for a class or interface.
        private TypeArray _ifacesAll;      // Recursive closure of base interfaces ordered so an iface appears before all of its base ifaces.

        private TypeArray _typeVarsThis; // Type variables for this generic class, as declarations.
        private TypeArray _typeVarsAll;     // The type variables for this generic class and all containing classes.

        private TypeManager _pTypeManager;     // This is so AGGTYPESYMs can instantiate their baseClass and ifacesAll members on demand.

        // First UD conversion operator. This chain is for this type only (not base types).
        // The hasConversion flag indicates whether this or any base types have UD conversions.
        private MethodSymbol _pConvFirst;

        // ------------------------------------------------------------------------
        // 
        // Put members that are bits under here in a contiguous section.
        //
        // ------------------------------------------------------------------------

        private AggKindEnum _aggKind;

        private bool _isLayoutError; // Whether there is a cycle in the layout for the struct

        // Where this came from - fabricated, source, import
        // Fabricated AGGs have isSource == true but hasParseTree == false.
        // N.B.: in incremental builds, it is quite possible for
        // isSource==TRUE and hasParseTree==FALSE. Be
        // sure you use the correct variable for what you are trying to do!
        private bool _isSource;    // This class is defined in source, although the 
        // source might not be being read during this compile.

        // Predefined
        private bool _isPredefined;    // A special predefined type.
        private PredefinedType _iPredef;        // index of the predefined type, if isPredefined.

        // Flags
        private bool _isAbstract;      // Can it be instantiated?
        private bool _isSealed;        // Can it be derived from?

        // Attribute

        private bool _isUnmanagedStruct; // Set if the struct is known to be un-managed (for unsafe code). Set in FUNCBREC.
        private bool _isManagedStruct; // Set if the struct is known to be managed (for unsafe code). Set during import.

        // Constructors
        private bool _hasPubNoArgCtor; // Whether it has a public instance constructor taking no args

        // private struct members should not be checked for assignment or references
        private bool _hasExternReference;

        // User defined operators

        private bool _isSkipUDOps; // Never check for user defined operators on this type (eg, decimal, string, delegate).

        private bool _isComImport;     // Does it have [ComImport]

        private bool _isAnonymousType;    // true if the class is an anonymous type
        // When this is unset we don't know if we have conversions.  When this 
        // is set it indicates if this type or any base type has user defined 
        // conversion operators
        private bool? _hasConversion;

        // ----------------------------------------------------------------------------
        // AggregateSymbol
        // ----------------------------------------------------------------------------

        public AggregateSymbol GetBaseAgg()
        {
            return _pBaseClass == null ? null : _pBaseClass.getAggregate();
        }

        public AggregateType getThisType()
        {
            if (_atsInst == null)
            {
                Debug.Assert(GetTypeVars() == GetTypeVarsAll() || isNested());

                AggregateType pOuterType = isNested() ? GetOuterAgg().getThisType() : null;

                _atsInst = _pTypeManager.GetAggregate(this, pOuterType, GetTypeVars());
            }

            //Debug.Assert(GetTypeVars().Size == atsInst.GenericArguments.Count);
            return _atsInst;
        }

        public void InitFromInfile(InputFile infile)
        {
            _infile = infile;
            _isSource = infile.isSource;
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

        public NamespaceOrAggregateSymbol Parent
        {
            get { return parent.AsNamespaceOrAggregateSymbol(); }
        }

        public new AggregateDeclaration DeclFirst()
        {
            return (AggregateDeclaration)base.DeclFirst();
        }

        public AggregateDeclaration DeclOnly()
        {
            //Debug.Assert(DeclFirst() != null && DeclFirst().DeclNext() == null);
            return DeclFirst();
        }

        public bool InAlias(KAID aid)
        {
            Debug.Assert(_infile != null);
            //Debug.Assert(DeclFirst() == null || DeclFirst().GetAssemblyID() == infile.GetAssemblyID());
            Debug.Assert(0 <= aid);
            if (aid < KAID.kaidMinModule)
                return _infile.InAlias(aid);
            return (aid == GetModuleID());
        }

        public KAID GetModuleID()
        {
            return 0;
        }

        public KAID GetAssemblyID()
        {
            Debug.Assert(_infile != null);
            //Debug.Assert(DeclFirst() == null || DeclFirst().GetAssemblyID() == infile.GetAssemblyID());
            return _infile.GetAssemblyID();
        }

        public bool IsUnresolved()
        {
            return _infile != null && _infile.GetAssemblyID() == KAID.kaidUnresolved;
        }

        public bool isNested()
        {
            return parent != null && parent.IsAggregateSymbol();
        }

        public AggregateSymbol GetOuterAgg()
        {
            return parent != null && parent.IsAggregateSymbol() ? parent.AsAggregateSymbol() : null;
        }

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



        public bool IsAnonymousType()
        {
            return _isAnonymousType;
        }

        public void SetAnonymousType(bool isAnonymousType)
        {
            _isAnonymousType = isAnonymousType;
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

        public bool IsLayoutError()
        {
            return _isLayoutError == true;
        }

        public void SetLayoutError(bool layoutError)
        {
            _isLayoutError = layoutError;
        }

        public bool IsSealed()
        {
            return _isSealed == true;
        }

        public void SetSealed(bool @sealed)
        {
            _isSealed = @sealed;
        }

        ////////////////////////////////////////////////////////////////////////////////

        public bool HasConversion(SymbolLoader pLoader)
        {
            pLoader.RuntimeBinderSymbolTable.AddConversionsForType(AssociatedSystemType);

            if (!_hasConversion.HasValue)
            {
                // ok, we tried defining all the conversions, and we didn't get anything
                // for this type.  However, we will still think this type has conversions
                // if it's base type has conversions.
                _hasConversion = GetBaseAgg() != null && GetBaseAgg().HasConversion(pLoader);
            }

            return _hasConversion.Value;
        }

        ////////////////////////////////////////////////////////////////////////////////

        public void SetHasConversion()
        {
            _hasConversion = true;
        }

        ////////////////////////////////////////////////////////////////////////////////

        public bool IsUnmanagedStruct()
        {
            return _isUnmanagedStruct == true;
        }

        public void SetUnmanagedStruct(bool unmanagedStruct)
        {
            _isUnmanagedStruct = unmanagedStruct;
        }

        public bool IsManagedStruct()
        {
            return _isManagedStruct == true;
        }

        public void SetManagedStruct(bool managedStruct)
        {
            _isManagedStruct = managedStruct;
        }

        public bool IsKnownManagedStructStatus()
        {
            Debug.Assert(IsStruct());
            Debug.Assert(!IsManagedStruct() || !IsUnmanagedStruct());
            return IsManagedStruct() || IsUnmanagedStruct();
        }

        public bool HasPubNoArgCtor()
        {
            return _hasPubNoArgCtor == true;
        }

        public void SetHasPubNoArgCtor(bool hasPubNoArgCtor)
        {
            _hasPubNoArgCtor = hasPubNoArgCtor;
        }

        public bool HasExternReference()
        {
            return _hasExternReference == true;
        }

        public void SetHasExternReference(bool hasExternReference)
        {
            _hasExternReference = hasExternReference;
        }


        public bool IsSkipUDOps()
        {
            return _isSkipUDOps == true;
        }

        public void SetSkipUDOps(bool skipUDOps)
        {
            _isSkipUDOps = skipUDOps;
        }

        public void SetComImport(bool comImport)
        {
            _isComImport = comImport;
        }

        public bool IsSource()
        {
            return _isSource == true;
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
                    outerTypeVars = BSYMMGR.EmptyTypeArray();
                }

                _typeVarsThis = typeVars;
                _typeVarsAll = _pTypeManager.ConcatenateTypeArrays(outerTypeVars, typeVars);
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

        public TypeManager GetTypeManager()
        {
            return _pTypeManager;
        }

        public void SetTypeManager(TypeManager typeManager)
        {
            _pTypeManager = typeManager;
        }

        public MethodSymbol GetFirstUDConversion()
        {
            return _pConvFirst;
        }

        public void SetFirstUDConversion(MethodSymbol conv)
        {
            _pConvFirst = conv;
        }

        public new bool InternalsVisibleTo(Assembly assembly)
        {
            return _pTypeManager.InternalsVisibleTo(AssociatedAssembly, assembly);
        }
    }
}
