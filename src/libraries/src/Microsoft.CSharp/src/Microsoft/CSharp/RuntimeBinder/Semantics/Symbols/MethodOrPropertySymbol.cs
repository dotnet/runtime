// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // MethodOrPropertySymbol
    //
    // MethodOrPropertySymbol - abstract class representing a method or a property. There
    // are a bunch of algorithms in the compiler (e.g., override and overload 
    // resolution) that want to treat methods and properties the same. This 
    // abstract base class has the common parts. 
    //
    // Changed to a ParentSymbol to allow generic methods to parent their type
    // variables.
    // ----------------------------------------------------------------------------

    internal abstract class MethodOrPropertySymbol : ParentSymbol
    {
        public uint modOptCount;              // number of CMOD_OPTs in signature and return type

        public new bool isStatic;               // Static member?
        public bool isOverride;             // Overrides an inherited member. Only valid if isVirtual is set.
                                            // false implies that a new vtable slot is required for this method.
        public bool isOperator;             // a user defined operator (or default indexed property)
        public bool isParamArray;           // new style varargs
        public bool isHideByName;           // this property hides all below it regardless of signature
        public List<Name> ParameterNames { get; private set; }
        private bool[] _optionalParameterIndex;
        private bool[] _defaultParameterIndex;
        private ConstVal[] _defaultParameters;
        private CType[] _defaultParameterConstValTypes;
        private bool[] _marshalAsIndex;
        private UnmanagedType[] _marshalAsBuffer;

        // This indicates the base member that this member overrides or implements.
        // For an explicit interface member implementation, this is the interface member (and type)
        // that the member implements. For an override member, this is the base member that is
        // being overridden. This is not affected by implicit interface member implementation.
        // If this symbol is a property and an explicit interface member implementation, the swtSlot
        // may be an event. This is filled in during prepare.
        public SymWithType swtSlot;
        public CType RetType;            // Return type.

        private TypeArray _Params;
        public TypeArray Params
        {
            get
            {
                return _Params;
            }
            set
            {
                Debug.Assert(_Params == null, "Should only be set once");
                _Params = value;
                int count = value.Count;
                if (count == 0)
                {
                    _optionalParameterIndex = _defaultParameterIndex = _marshalAsIndex = Array.Empty<bool>();
                    _defaultParameters = Array.Empty<ConstVal>();
                    _defaultParameterConstValTypes = Array.Empty<CType>();
                    _marshalAsBuffer = Array.Empty<UnmanagedType>();
                }
                else
                {
                    _optionalParameterIndex = new bool[count];
                    _defaultParameterIndex = new bool[count];
                    _defaultParameters = new ConstVal[count];
                    _defaultParameterConstValTypes = new CType[count];
                    _marshalAsIndex = new bool[count];
                    _marshalAsBuffer = new UnmanagedType[count];
                }
            }
        }

        public MethodOrPropertySymbol()
        {
            ParameterNames = new List<Name>();
        }

        /////////////////////////////////////////////////////////////////////////////////

        public bool IsParameterOptional(int index)
        {
            Debug.Assert(index < Params.Count);

            if (_optionalParameterIndex == null)
            {
                return false;
            }
            return _optionalParameterIndex[index];
        }

        public void SetOptionalParameter(int index)
        {
            Debug.Assert(_optionalParameterIndex != null);
            _optionalParameterIndex[index] = true;
        }

        public bool HasOptionalParameters()
        {
            if (_optionalParameterIndex == null)
            {
                return false;
            }
            foreach (bool b in _optionalParameterIndex)
            {
                if (b)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasDefaultParameterValue(int index)
        {
            Debug.Assert(index < Params.Count);
            Debug.Assert(_defaultParameterIndex != null);
            return _defaultParameterIndex[index];
        }

        public void SetDefaultParameterValue(int index, CType type, ConstVal cv)
        {
            Debug.Assert(_defaultParameterIndex != null);
            _defaultParameterIndex[index] = true;
            _defaultParameters[index] = cv;
            _defaultParameterConstValTypes[index] = type;
        }

        public ConstVal GetDefaultParameterValue(int index)
        {
            Debug.Assert(HasDefaultParameterValue(index));
            Debug.Assert(_defaultParameterIndex != null);
            return _defaultParameters[index];
        }

        public CType GetDefaultParameterValueConstValType(int index)
        {
            Debug.Assert(HasDefaultParameterValue(index));
            return _defaultParameterConstValTypes[index];
        }

        private bool IsMarshalAsParameter(int index)
        {
            return _marshalAsIndex[index];
        }

        public void SetMarshalAsParameter(int index, UnmanagedType umt)
        {
            _marshalAsIndex[index] = true;
            _marshalAsBuffer[index] = umt;
        }

        private UnmanagedType GetMarshalAsParameterValue(int index)
        {
            Debug.Assert(IsMarshalAsParameter(index));
            return _marshalAsBuffer[index];
        }

        public bool MarshalAsObject(int index)
        {
            if (IsMarshalAsParameter(index))
            {
                UnmanagedType marshalAsType = GetMarshalAsParameterValue(index);
                return marshalAsType == UnmanagedType.Interface || marshalAsType == UnmanagedType.IUnknown || marshalAsType == UnmanagedType.IDispatch;
            }

            return false;
        }

        public AggregateSymbol getClass() => parent as AggregateSymbol;

        public bool IsExpImpl()
        {
            return name == null;
        }
    }
}
