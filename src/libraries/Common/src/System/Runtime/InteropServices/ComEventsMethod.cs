// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Part of ComEventHelpers APIs which allow binding
    /// managed delegates to COM's connection point based events.
    /// </summary>
    internal sealed class ComEventsMethod
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
        public sealed class DelegateWrapper
        {
            private bool _once;
            private int _expectedParamsCount;
            private Type?[]? _cachedTargetTypes;

            public DelegateWrapper(Delegate d, bool wrapArgs)
            {
                Delegate = d;
                WrapArgs = wrapArgs;
            }

            public Delegate Delegate { get; set; }

            public bool WrapArgs { get; }

            public object? Invoke(object[] args)
            {
                if (Delegate == null)
                {
                    return null;
                }

                if (!_once)
                {
                    PreProcessSignature();
                    _once = true;
                }

                if (_cachedTargetTypes != null && _expectedParamsCount == args.Length)
                {
                    for (int i = 0; i < _expectedParamsCount; i++)
                    {
                        if (_cachedTargetTypes[i] is Type t)
                        {
                            args[i] = Enum.ToObject(t, args[i]);
                        }
                    }
                }

                return Delegate.DynamicInvoke(WrapArgs ? new object[] { args } : args);
            }

            private void PreProcessSignature()
            {
                ParameterInfo[] parameters = Delegate.Method.GetParameters();
                _expectedParamsCount = parameters.Length;

                Type?[]? targetTypes = null;
                for (int i = 0; i < _expectedParamsCount; i++)
                {
                    ParameterInfo pi = parameters[i];

                    // recognize only 'ref Enum' signatures and cache
                    // both enum type and the underlying type.
                    if (pi.ParameterType.IsByRef
                        && pi.ParameterType.HasElementType
                        && pi.ParameterType.GetElementType()!.IsEnum)
                    {
                        targetTypes ??= new Type?[_expectedParamsCount];

                        targetTypes[i] = pi.ParameterType.GetElementType();
                    }
                }

                if (targetTypes != null)
                {
                    _cachedTargetTypes = targetTypes;
                }
            }
        }

        /// <summary>
        /// Invoking ComEventsMethod means invoking a multi-cast delegate attached to it.
        /// Since multicast delegate's built-in chaining supports only chaining instances of the same type,
        /// we need to complement this design by using an explicit linked list data structure.
        /// </summary>
        private DelegateWrapper[] _delegateWrappers = Array.Empty<DelegateWrapper>();

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
            get => _delegateWrappers.Length == 0;
        }

        public void AddDelegate(Delegate d, bool wrapArgs = false)
        {
            DelegateWrapper[] wrappers, newWrappers;
            do
            {
                wrappers = _delegateWrappers;
                newWrappers = new DelegateWrapper[wrappers.Length + 1];
                wrappers.CopyTo(newWrappers, 0);
                newWrappers[^1] = new DelegateWrapper(d, wrapArgs);
            } while (!PublishNewWrappers(newWrappers, wrappers));
        }

        public void RemoveDelegate(Delegate d, bool wrapArgs = false)
        {
            DelegateWrapper[] wrappers, newWrappers;
            do
            {
                wrappers = _delegateWrappers;

                // Find delegate wrapper index
                int removeIdx = -1;
                for (int i = 0; i < wrappers.Length; i++)
                {
                    DelegateWrapper wrapperMaybe = wrappers[i];
                    if (wrapperMaybe.Delegate == d && wrapperMaybe.WrapArgs == wrapArgs)
                    {
                        removeIdx = i;
                        break;
                    }
                }

                if (removeIdx < 0)
                {
                    // Not present in collection
                    return;
                }

                newWrappers = new DelegateWrapper[wrappers.Length - 1];
                wrappers.AsSpan(0, removeIdx).CopyTo(newWrappers);
                wrappers.AsSpan(removeIdx + 1).CopyTo(newWrappers.AsSpan(removeIdx));
            } while (!PublishNewWrappers(newWrappers, wrappers));
        }

        public void RemoveDelegates(Func<Delegate, bool> condition)
        {
            DelegateWrapper[] wrappers, newWrappers;
            do
            {
                wrappers = _delegateWrappers;
                List<DelegateWrapper> tmp = new(wrappers);
                tmp.RemoveAll(w => condition(w.Delegate));
                newWrappers = tmp.ToArray();
            }
            while (!PublishNewWrappers(newWrappers, wrappers));
        }

        public object? Invoke(object[] args)
        {
            Debug.Assert(!Empty);
            object? result = null;

            foreach (DelegateWrapper wrapper in _delegateWrappers)
            {
                result = wrapper.Invoke(args);
            }

            return result;
        }

        // Attempt to update the member wrapper field
        private bool PublishNewWrappers(DelegateWrapper[] newWrappers, DelegateWrapper[] currentMaybe)
        {
            return Interlocked.CompareExchange(ref _delegateWrappers, newWrappers, currentMaybe) == currentMaybe;
        }
    }
}
