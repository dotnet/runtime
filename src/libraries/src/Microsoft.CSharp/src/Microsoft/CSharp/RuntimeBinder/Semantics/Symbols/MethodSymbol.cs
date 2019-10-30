// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    ////////////////////////////////////////////////////////////////////////////////
    //
    // MethodSymbol - a symbol representing a method. Parent is a struct, interface
    // or class (aggregate). No children.

    internal sealed class MethodSymbol : MethodOrPropertySymbol
    {
        private MethodKindEnum _methKind; // An extra bit to prevent sign-extension
        private bool _inferenceMustFail; // Inference must fail if there are no type variables or if
        private bool _checkedInfMustFail; // there is a type variable used in no parameter.

        private MethodSymbol _convNext; // For linked list of conversion operators.
        private PropertySymbol _prop;     // For property accessors, this is the PropertySymbol.
        private EventSymbol _evt;     // For event accessors, this is the EventSymbol.

        public bool isVirtual;              // Virtual member?
        public MemberInfo AssociatedMemberInfo;

        public TypeArray typeVars;          // All the type variables for a generic method, as declarations.

        // If there is a type variable in the method which is used in no parameter,
        // then inference must fail. Since this is expensive to check, we cache
        // the result of the first call.

        public bool InferenceMustFail()
        {
            if (_checkedInfMustFail)
            {
                return _inferenceMustFail;
            }
            Debug.Assert(!_inferenceMustFail);
            _checkedInfMustFail = true;
            for (int ivar = 0; ivar < typeVars.Count; ivar++)
            {
                TypeParameterType var = (TypeParameterType)typeVars[ivar];
                // See if type var is used in a parameter.
                for (int ipar = 0; ; ipar++)
                {
                    if (ipar >= Params.Count)
                    {
                        // This type variable is not in any parameter.
                        _inferenceMustFail = true;
                        return true;
                    }
                    if (TypeManager.TypeContainsType(Params[ipar], var))
                    {
                        break;
                    }
                }
            }
            // All type variables are used in a parameter.
            return false;
        }

        public MethodKindEnum MethKind => _methKind;

        public bool IsConstructor()
        {
            return _methKind == MethodKindEnum.Constructor;
        }

        public bool IsNullableConstructor()
        {
            return getClass().isPredefAgg(PredefinedType.PT_G_OPTIONAL) &&
                Params.Count == 1 &&
                Params[0] is TypeParameterType &&
                IsConstructor();
        }

        public bool isPropertyAccessor()  // true if this method is a property set or get method
        {
            return _methKind == MethodKindEnum.PropAccessor;
        }

        public bool isEventAccessor()     // true if this method is an event add/remove method
        {
            return _methKind == MethodKindEnum.EventAccessor;
        }

        public bool isImplicit()          // is user defined implicit conversion operator
        {
            return _methKind == MethodKindEnum.ImplicitConv;
        }

        public void SetMethKind(MethodKindEnum mk)
        {
            _methKind = mk;
        }

        public MethodSymbol ConvNext()
        {
            AssertIsConversionOperator();
            return _convNext;
        }

        public void SetConvNext(MethodSymbol conv)
        {
            AssertIsConversionOperator();
            conv?.AssertIsConversionOperator();
            _convNext = conv;
        }

        public PropertySymbol getProperty()
        {
            Debug.Assert(isPropertyAccessor());
            return _prop;
        }

        public void SetProperty(PropertySymbol prop)
        {
            Debug.Assert(isPropertyAccessor());
            _prop = prop;
        }

        public EventSymbol getEvent()
        {
            Debug.Assert(isEventAccessor());
            return _evt;
        }

        public void SetEvent(EventSymbol evt)
        {
            Debug.Assert(isEventAccessor());
            _evt = evt;
        }

        [Conditional("DEBUG")]
        private void AssertIsConversionOperator()
        {
            Debug.Assert(MethKind == MethodKindEnum.ExplicitConv || MethKind == MethodKindEnum.ImplicitConv);
        }

        public new bool isUserCallable()
        {
            return !isOperator && !isAnyAccessor();
        }

        private bool isAnyAccessor()
        {
            return isPropertyAccessor() || isEventAccessor();
        }

        /*
         * returns true if this property is a set accessor
         */
        public bool isSetAccessor()
        {
            if (!isPropertyAccessor())
            {
                return false;
            }

            PropertySymbol property = getProperty();

            if (property == null)
            {
                Debug.Fail("cannot find property for accessor");
                return false;
            }

            return (this == property.SetterMethod);
        }
    }
}
