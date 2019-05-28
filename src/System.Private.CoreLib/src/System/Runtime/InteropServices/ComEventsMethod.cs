// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Part of ComEventHelpers APIs which allow binding
    /// managed delegates to COM's connection point based events.
    /// </summary>
    internal class ComEventsMethod
    {
        /// <summary>
        /// This delegate wrapper class handles dynamic invocation of delegates. The reason for the wrapper's
        /// existence is that under certain circumstances we need to coerce arguments to types expected by the
        /// delegates signature. Normally, reflection (Delegate.DynamicInvoke) handles type coercion
        /// correctly but one known case is when the expected signature is 'ref Enum' - in this case
        /// reflection by design does not do the coercion. Since we need to be compatible with COM interop
        /// handling of this scenario - we are pre-processing delegate's signature by looking for 'ref enums'
        /// and cache the types required for such coercion.
        /// </summary>
        public class DelegateWrapper
        {
            private bool _once = false;
            private int _expectedParamsCount;
            private Type?[]? _cachedTargetTypes;

            public DelegateWrapper(Delegate d)
            {
                Delegate = d;
            }

            public Delegate Delegate { get; set; }

            public object? Invoke(object[] args)
            {
                if (Delegate == null)
                {
                    return null;
                }

                if (_once == false)
                {
                    PreProcessSignature();
                    _once = true;
                }

                if (_cachedTargetTypes != null && _expectedParamsCount == args.Length)
                {
                    for (int i = 0; i < _expectedParamsCount; i++)
                    {
                        if (_cachedTargetTypes[i] != null)
                        {
                            args[i] = Enum.ToObject(_cachedTargetTypes[i]!, args[i]); // TODO-NULLABLE: Indexer nullability tracked (https://github.com/dotnet/roslyn/issues/34644)
                        }
                    }
                }

                return Delegate.DynamicInvoke(args);
            }

            private void PreProcessSignature()
            {
                ParameterInfo[] parameters = Delegate.Method.GetParameters();
                _expectedParamsCount = parameters.Length;

                bool needToHandleCoercion = false;

                var targetTypes = new List<Type?>();
                foreach (ParameterInfo pi in parameters)
                {
                    Type? targetType = null;

                    // recognize only 'ref Enum' signatures and cache
                    // both enum type and the underlying type.
                    if (pi.ParameterType.IsByRef
                        && pi.ParameterType.HasElementType
                        && pi.ParameterType.GetElementType()!.IsEnum)
                    {
                        needToHandleCoercion = true;
                        targetType = pi.ParameterType.GetElementType();
                    }

                    targetTypes.Add(targetType);
                }

                if (needToHandleCoercion)
                {
                    _cachedTargetTypes = targetTypes.ToArray();
                }
            }
        }

        /// <summary>
        /// Invoking ComEventsMethod means invoking a multi-cast delegate attached to it.
        /// Since multicast delegate's built-in chaining supports only chaining instances of the same type,
        /// we need to complement this design by using an explicit linked list data structure.
        /// </summary>
        private List<DelegateWrapper> _delegateWrappers = new List<DelegateWrapper>();

        private readonly int _dispid;
        private ComEventsMethod? _next;

        public ComEventsMethod(int dispid)
        {
            _dispid = dispid;
        }

        public static ComEventsMethod? Find(ComEventsMethod? methods, int dispid)
        {
            while (methods != null && methods._dispid != dispid)
            {
                methods = methods._next;
            }

            return methods;
        }

        public static ComEventsMethod Add(ComEventsMethod? methods, ComEventsMethod method)
        {
            method._next = methods;
            return method;
        }

        public static ComEventsMethod? Remove(ComEventsMethod methods, ComEventsMethod method)
        {
            Debug.Assert(methods != null, "removing method from empty methods collection");
            Debug.Assert(method != null, "specify method is null");

            if (methods == method)
            {
                return methods._next;
            }
            else
            {
                ComEventsMethod? current = methods;

                while (current != null && current._next != method)
                {
                    current = current._next;
                }

                if (current != null)
                {
                    current._next = method._next;
                }

                return methods;
            }
        }

        public bool Empty
        {
            get
            {
                lock (_delegateWrappers)
                {
                    return _delegateWrappers.Count == 0;
                }
            }
        }

        public void AddDelegate(Delegate d)
        {
            lock (_delegateWrappers)
            {
                // Update an existing delegate wrapper
                foreach (DelegateWrapper wrapper in _delegateWrappers)
                {
                    if (wrapper.Delegate.GetType() == d.GetType())
                    {
                        wrapper.Delegate = Delegate.Combine(wrapper.Delegate, d)!; // TODO-NULLABLE: Remove ! when nullable attributes are respected
                        return;
                    }
                }

                var newWrapper = new DelegateWrapper(d);
                _delegateWrappers.Add(newWrapper);
            }
        }

        public void RemoveDelegate(Delegate d)
        {
            lock (_delegateWrappers)
            {
                // Find delegate wrapper index
                int removeIdx = -1;
                DelegateWrapper? wrapper = null;
                for (int i = 0; i < _delegateWrappers.Count; i++)
                {
                    DelegateWrapper wrapperMaybe = _delegateWrappers[i];
                    if (wrapperMaybe.Delegate.GetType() == d.GetType())
                    {
                        removeIdx = i;
                        wrapper = wrapperMaybe;
                        break;
                    }
                }

                if (removeIdx < 0)
                {
                    // Not present in collection
                    return;
                }

                // Update wrapper or remove from collection
                Delegate? newDelegate = Delegate.Remove(wrapper!.Delegate, d);
                if (newDelegate != null)
                {
                    wrapper.Delegate = newDelegate;
                }
                else
                {
                    _delegateWrappers.RemoveAt(removeIdx);
                }
            }
        }

        public object? Invoke(object[] args)
        {
            Debug.Assert(!Empty);
            object? result = null;

            lock (_delegateWrappers)
            {
                foreach (DelegateWrapper wrapper in _delegateWrappers)
                {
                    result = wrapper.Invoke(args);
                }
            }

            return result;
        }
    }
}
