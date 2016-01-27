// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*============================================================
**
**
** Purpose: part of ComEventHelpers APIs which allow binding 
** managed delegates to COM's connection point based events.
**
**/
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;


namespace System.Runtime.InteropServices {

    // see code:ComEventsHelper#ComEventsArchitecture
    internal class ComEventsMethod {

        // This delegate wrapper class handles dynamic invocation of delegates. The reason for the wrapper's
        // existence is that under certain circumstances we need to coerce arguments to types expected by the
        // delegates signature. Normally, reflection (Delegate.DynamicInvoke) handles types coercion
        // correctly but one known case is when the expected signature is 'ref Enum' - in this case
        // reflection by design does not do the coercion. Since we need to be compatible with COM interop
        // handling of this scenario - we are pre-processing delegate's signature by looking for 'ref enums'
        // and cache the types required for such coercion.
        internal class DelegateWrapper {
            private Delegate _d;

            private bool _once = false;
            private int _expectedParamsCount;
            private Type[] _cachedTargetTypes;

            public DelegateWrapper(Delegate d) {
                _d = d;
            }

            public Delegate Delegate {
                get { return _d; }
                set { _d = value; }
            }

            public object Invoke(object[] args) {
                if (_d == null)
                    return null;

                if (_once == false) {
                    PreProcessSignature();
                    _once = true;
                }

                if (_cachedTargetTypes != null && _expectedParamsCount == args.Length) {
                    for (int i = 0; i < _expectedParamsCount; i++) {
                        if (_cachedTargetTypes[i] != null) {
                            args[i] = Enum.ToObject(_cachedTargetTypes[i], args[i]);
                        }
                    }
                }

                return _d.DynamicInvoke(args);
            }

            private void PreProcessSignature() {
                ParameterInfo[] parameters = _d.Method.GetParameters();
                _expectedParamsCount = parameters.Length;

                Type[] enumTypes = new Type[_expectedParamsCount];

                bool needToHandleCoercion = false;

                for (int i = 0; i < _expectedParamsCount; i++) {
                    ParameterInfo pi = parameters[i];
                    // recognize only 'ref Enum' signatures and cache
                    // both enum type and the underlying type.
                    if (pi.ParameterType.IsByRef && 
                        pi.ParameterType.HasElementType && 
                        pi.ParameterType.GetElementType().IsEnum) {

                        needToHandleCoercion = true;
                        enumTypes[i] = pi.ParameterType.GetElementType();
                    }
                }

                if (needToHandleCoercion == true) {
                    _cachedTargetTypes = enumTypes;
                }
            }
        }

        #region private fields

        /// <summary>
        /// Invoking ComEventsMethod means invoking a multi-cast delegate attached to it.
        /// Since multicast delegate's built-in chaining supports only chaining instances of the same type,
        /// we need to complement this design by using an explicit linked list data structure.
        /// </summary>
        private DelegateWrapper [] _delegateWrappers;

        private int _dispid;
        private ComEventsMethod _next;

        #endregion

        
        #region ctor
        
        internal ComEventsMethod(int dispid) {
            _delegateWrappers = null;
            _dispid = dispid;
        }
        
        #endregion

        
        #region static internal methods
        
        internal static ComEventsMethod Find(ComEventsMethod methods, int dispid) {
            while (methods != null && methods._dispid != dispid) {
                methods = methods._next;
            }
            return methods;
        }

        internal static ComEventsMethod Add(ComEventsMethod methods, ComEventsMethod method) {
            method._next = methods;
            return method;
        }

        internal static ComEventsMethod Remove(ComEventsMethod methods, ComEventsMethod method) {
            if (methods == method) {
                methods = methods._next;
            } else {
                ComEventsMethod current = methods;
                while (current != null && current._next != method)
                    current = current._next;
                if (current != null)
                    current._next = method._next;
            }

            return methods;
        }
 
        #endregion

        
        #region public properties / methods

        internal int DispId {
            get { return _dispid; }
        }

        internal bool Empty {
            get { return _delegateWrappers == null || _delegateWrappers.Length == 0; }
        }

        internal void AddDelegate(Delegate d) {
            int count = 0;
            if (_delegateWrappers != null) {
                count = _delegateWrappers.Length;
            }

            for (int i = 0; i < count; i++) {
                if (_delegateWrappers[i].Delegate.GetType() == d.GetType()) {
                    _delegateWrappers[i].Delegate = Delegate.Combine(_delegateWrappers[i].Delegate, d);
                    return;
                }
            }

            DelegateWrapper [] newDelegateWrappers = new DelegateWrapper[count + 1];
            if (count > 0) {
                _delegateWrappers.CopyTo(newDelegateWrappers, 0);
            }

            DelegateWrapper wrapper = new DelegateWrapper(d);
            newDelegateWrappers[count] = wrapper;

            _delegateWrappers = newDelegateWrappers;
        }

        internal void RemoveDelegate(Delegate d) {

            int count = _delegateWrappers.Length;
            int removeIdx = -1;

            for (int i = 0; i < count; i++) {
                if (_delegateWrappers[i].Delegate.GetType() == d.GetType()) {
                    removeIdx = i;
                    break;
                }
            }

            if (removeIdx < 0)
                return;

            Delegate newDelegate = Delegate.Remove(_delegateWrappers[removeIdx].Delegate, d);
            if (newDelegate != null) {
                _delegateWrappers[removeIdx].Delegate = newDelegate;
                return;
            }

            // now remove the found entry from the _delegates array

            if (count == 1) {
                _delegateWrappers = null;
                return;
            }

            DelegateWrapper [] newDelegateWrappers = new DelegateWrapper[count - 1];
            int j = 0;
            while (j < removeIdx) {
                newDelegateWrappers[j] = _delegateWrappers[j];
                j++;
            }
            while (j < count-1) {
                newDelegateWrappers[j] = _delegateWrappers[j + 1];
                j++;
            }

            _delegateWrappers = newDelegateWrappers;
        }

        internal object Invoke(object[] args) {
            BCLDebug.Assert(Empty == false, "event sink is executed but delegates list is empty");

            // Issue: see code:ComEventsHelper#ComEventsRetValIssue
            object result = null;
            DelegateWrapper[] invocationList = _delegateWrappers;
            foreach (DelegateWrapper wrapper in invocationList) {
                if (wrapper == null || wrapper.Delegate == null)
                    continue;

                result = wrapper.Invoke(args);
            }

            return result;
        }

        #endregion
    }
}
