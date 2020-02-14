// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Scripting.ComInterop {
    /// <summary>
    /// Part of ComEventHelpers APIs which allow binding
    /// managed delegates to COM's connection point based events.
    /// </summary>
    internal class ComEventSink : IDispatch, ICustomQueryInterface, IDisposable {
        private Guid _iidSourceItf;
        private IConnectionPoint _connectionPoint;
        private int _cookie;
        private ComEventsMethod _methods;

        public ComEventSink(object rcw, Guid iid) {
            _iidSourceItf = iid;
            this.Advise(rcw);
        }

        private void Initialize(object rcw, Guid iid) {
            _iidSourceItf = iid;
            this.Advise(rcw);
        }

        public static ComEventSink FromRuntimeCallableWrapper(object rcw, Guid sourceIid, bool createIfNotFound) {
            List<ComEventSink> comEventSinks = ComEventSinksContainer.FromRuntimeCallableWrapper(rcw, createIfNotFound);

            if (comEventSinks == null) {
                return null;
            }

            ComEventSink comEventSink = null;
            lock (comEventSinks) {
                foreach (ComEventSink sink in comEventSinks) {
                    if (sink._iidSourceItf == sourceIid) {
                        comEventSink = sink;
                        break;
                    }

                    if (sink._iidSourceItf == Guid.Empty) {
                        // we found a ComEventSink object that 
                        // was previously disposed. Now we will reuse it.
                        sink.Initialize(rcw, sourceIid);
                        comEventSink = sink;
                    }
                }

                if (comEventSink == null && createIfNotFound) {
                    comEventSink = new ComEventSink(rcw, sourceIid);
                    comEventSinks.Add(comEventSink);
                }
            }

            return comEventSink;
        }

        public ComEventsMethod RemoveMethod(ComEventsMethod method) {
            _methods = ComEventsMethod.Remove(_methods, method);
            return _methods;
        }

        public ComEventsMethod FindMethod(int dispid) {
            return ComEventsMethod.Find(_methods, dispid);
        }

        public ComEventsMethod AddMethod(int dispid) {
            ComEventsMethod method = new ComEventsMethod(dispid);
            _methods = ComEventsMethod.Add(_methods, method);
            return method;
        }


        public void AddHandler(int dispid, object func) {
            ComEventsMethod method = FindMethod(dispid);
            if (method == null) {
                method = AddMethod(dispid);
            }
            method.AddDelegate(new SplatCallSite(func).Invoke);
        }

        public void RemoveHandler(int dispid, object func) {
            ComEventsMethod sinkEntry = FindMethod(dispid);
            if (sinkEntry == null) {
                return;
            }

            // Remove the delegate from multicast delegate chain.
            // We will need to find the delegate that corresponds
            // to the func handler we want to remove. This will be
            // easy since we Target property of the delegate object
            // is a ComEventCallContext object.
            sinkEntry.RemoveDelegates(d => d.Target is SplatCallSite callContext && callContext._callable.Equals(func));

            // If the delegates chain is empty - we can remove 
            // corresponding ComEvenSinkEntry
            if (sinkEntry.Empty)
                RemoveMethod(sinkEntry);

            if (_methods.Empty) {
                Dispose();
            }
        }


        int IDispatch.TryGetTypeInfoCount(out uint pctinfo) {
            pctinfo = 0;
            return ComHresults.S_OK;
        }

        int IDispatch.TryGetTypeInfo(uint iTInfo, int lcid, out IntPtr info) {
            info = IntPtr.Zero;
            return ComHresults.E_NOTIMPL;
        }

        int IDispatch.TryGetIDsOfNames(
            ref Guid iid,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)]
                    string[] names,
            uint cNames,
            int lcid,
            [Out]
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4, SizeParamIndex = 2)]
            int[] rgDispId) {
            return ComHresults.E_NOTIMPL;
        }

        private const VarEnum VT_BYREF_VARIANT = VarEnum.VT_BYREF | VarEnum.VT_VARIANT;
        private const VarEnum VT_TYPEMASK = (VarEnum)0x0fff;
        private const VarEnum VT_BYREF_TYPEMASK = VT_TYPEMASK | VarEnum.VT_BYREF;

        private static unsafe ref Variant GetVariant(ref Variant pSrc) {
            if (pSrc.VariantType == VT_BYREF_VARIANT) {
                // For VB6 compatibility reasons, if the VARIANT is a VT_BYREF | VT_VARIANT that
                // contains another VARIANT with VT_BYREF | VT_VARIANT, then we need to extract the
                // inner VARIANT and use it instead of the outer one. Note that if the inner VARIANT
                // is VT_BYREF | VT_VARIANT | VT_ARRAY, it will pass the below test too.
                Span<Variant> pByRefVariant = new Span<Variant>(pSrc.AsByRefVariant.ToPointer(), 1);
                if ((pByRefVariant[0].VariantType & VT_BYREF_TYPEMASK) == VT_BYREF_VARIANT) {
                    return ref pByRefVariant[0];
                }
            }

            return ref pSrc;
        }

        unsafe int IDispatch.TryInvoke(
            int dispIdMember,
            ref Guid riid,
            int lcid,
            INVOKEKIND wFlags,
            ref DISPPARAMS pDispParams,
            IntPtr VarResult,
            IntPtr pExcepInfo,
            IntPtr puArgErr) {

            ComEventsMethod method = FindMethod(dispIdMember);
            if (method == null) {
                return ComHresults.DISP_E_MEMBERNOTFOUND;
            }

            try {
                // notice the unsafe pointers we are using. This is to avoid unnecessary
                // arguments marshalling. see code:ComEventsHelper#ComEventsArgsMarshalling

                const int InvalidIdx = -1;
                object[] args = new object[pDispParams.cArgs];
                int[] byrefsMap = new int[pDispParams.cArgs];
                bool[] usedArgs = new bool[pDispParams.cArgs];

                int totalCount = pDispParams.cNamedArgs + pDispParams.cArgs;
                var vars = new Span<Variant>(pDispParams.rgvarg.ToPointer(), totalCount);
                var namedArgs = new Span<int>(pDispParams.rgdispidNamedArgs.ToPointer(), totalCount);

                // copy the named args (positional) as specified
                int i;
                int pos;
                for (i = 0; i < pDispParams.cNamedArgs; i++) {
                    pos = namedArgs[i];
                    ref Variant pvar = ref GetVariant(ref vars[i]);
                    args[pos] = pvar.ToObject();
                    usedArgs[pos] = true;

                    int byrefIdx = InvalidIdx;
                    if ((pvar.VariantType & VarEnum.VT_BYREF) != 0) {
                        byrefIdx = i;
                    }

                    byrefsMap[pos] = byrefIdx;
                }

                // copy the rest of the arguments in the reverse order
                pos = 0;
                for (; i < pDispParams.cArgs; i++) {
                    // find the next unassigned argument
                    while (usedArgs[pos]) {
                        pos++;
                    }

                    ref Variant pvar = ref GetVariant(ref vars[pDispParams.cArgs - 1 - i]);
                    args[pos] = pvar.ToObject();

                    int byrefIdx = InvalidIdx;
                    if ((pvar.VariantType & VarEnum.VT_BYREF) != 0) {
                        byrefIdx = pDispParams.cArgs - 1 - i;
                    }

                    byrefsMap[pos] = byrefIdx;

                    pos++;
                }

                // Do the actual delegate invocation
                object result = method.Invoke(args);

                if (VarResult != IntPtr.Zero) {
                    Marshal.GetNativeVariantForObject(result, VarResult);
                }

                // Now we need to marshal all the byrefs back
                for (i = 0; i < pDispParams.cArgs; i++) {
                    int idxToPos = byrefsMap[i];
                    if (idxToPos == InvalidIdx) {
                        continue;
                    }

                    ref Variant pvar = ref GetVariant(ref vars[idxToPos]);
                    pvar.CopyFromIndirect(args[i]);
                }

                return ComHresults.S_OK;
            } catch (Exception e) {
                return e.HResult;
            }
        }

        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv) {
            ppv = IntPtr.Zero;
            if (iid == _iidSourceItf || iid == typeof(IDispatch).GUID) {
                ppv = Marshal.GetComInterfaceForObject(this, typeof(IDispatch), CustomQueryInterfaceMode.Ignore);
                return CustomQueryInterfaceResult.Handled;
            }

            return CustomQueryInterfaceResult.NotHandled;
        }

        private void Advise(object rcw) {
            Debug.Assert(_connectionPoint == null, "comevent sink is already advised");

            IConnectionPointContainer cpc = (IConnectionPointContainer)rcw;
            cpc.FindConnectionPoint(ref _iidSourceItf, out IConnectionPoint cp);

            object sinkObject = this;

            cp.Advise((IDispatch)sinkObject, out _cookie);

            _connectionPoint = cp;
        }

        public void Dispose() {
            if (_connectionPoint == null) {
                return;
            }

            if (_cookie == -1) {
                return;
            }

            try {
                _connectionPoint.Unadvise(_cookie);

                // _connectionPoint has entered the CLR in the constructor
                // for this object and hence its ref counter has been increased
                // by us. We have not exposed it to other components and
                // hence it is safe to call RCO on it w/o worrying about
                // killing the RCW for other objects that link to it.
                Marshal.ReleaseComObject(_connectionPoint);
            } catch (Exception ex) {
                // if something has gone wrong, and the object is no longer attached to the CLR,
                // the Unadvise is going to throw.  In this case, since we're going away anyway,
                // we'll ignore the failure and quietly go on our merry way.
                if (ex is COMException exCOM && exCOM.ErrorCode == ComHresults.CONNECT_E_NOCONNECTION) {
                    Debug.Assert(false, "IConnectionPoint::Unadvise returned CONNECT_E_NOCONNECTION.");
                    throw;
                }
            } finally {
                _connectionPoint = null;
                _cookie = -1;
                _iidSourceItf = Guid.Empty;
            }
        }
    }
}

#endif
