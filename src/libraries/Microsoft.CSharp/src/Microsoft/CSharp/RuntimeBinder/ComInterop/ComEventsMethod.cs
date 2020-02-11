// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Scripting.ComInterop {
    /// <summary>
    /// Part of ComEventHelpers APIs which allow binding
    /// managed delegates to COM's connection point based events.
    /// </summary>
    internal class ComEventsMethod {
        /// <summary>
        /// Invoking ComEventsMethod means invoking a multi-cast delegate attached to it.
        /// Since multicast delegate's built-in chaining supports only chaining instances of the same type,
        /// we need to complement this design by using an explicit linked list data structure.
        /// </summary>
        private Func<object[], object> _delegate;
        private object lockObject = new object();

        private readonly int _dispid;
        private ComEventsMethod _next;

        public ComEventsMethod(int dispid) {
            _dispid = dispid;
        }

        public static ComEventsMethod Find(ComEventsMethod methods, int dispid) {
            while (methods != null && methods._dispid != dispid) {
                methods = methods._next;
            }

            return methods;
        }

        public static ComEventsMethod Add(ComEventsMethod methods, ComEventsMethod method) {
            method._next = methods;
            return method;
        }

        public static ComEventsMethod Remove(ComEventsMethod methods, ComEventsMethod method) {
            Debug.Assert(methods != null, "removing method from empty methods collection");
            Debug.Assert(method != null, "specify method is null");

            if (methods == method) {
                return methods._next;
            } else {
                ComEventsMethod current = methods;

                while (current != null && current._next != method) {
                    current = current._next;
                }

                if (current != null) {
                    current._next = method._next;
                }

                return methods;
            }
        }

        public bool Empty {
            get {
                lock (lockObject) {
                    return _delegate is null;
                }
            }
        }

        public void AddDelegate(Func<object[], object> d) {
            lock (lockObject) {
                _delegate += d;
            }
        }

        internal void RemoveDelegates(Func<Func<object[], object>, bool> condition)
        {
            lock (lockObject) {
                Delegate[] invocationList = _delegate.GetInvocationList();
                for (int i = 0; i < invocationList.Length; i++) {
                    Func<object[], object> delegateMaybe = (Func<object[], object>)invocationList[i];
                    if (condition(delegateMaybe)) {
                        _delegate -= delegateMaybe;
                    }
                }
            }
        }

        public object Invoke(object[] args) {
            Debug.Assert(!Empty);
            object result = null;

            lock (lockObject) {
                result = _delegate?.Invoke(args);
            }

            return result;
        }
    }
}

#endif
