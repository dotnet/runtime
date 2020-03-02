﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM
using System.Linq.Expressions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using ComTypes = System.Runtime.InteropServices.ComTypes;
using System.Dynamic;

namespace Microsoft.Scripting.ComInterop {

    /// <summary>
    /// An object that implements IDispatch
    /// 
    /// This currently has the following issues:
    /// 1. If we prefer ComObjectWithTypeInfo over IDispatchComObject, then we will often not
    ///    IDispatchComObject since implementations of IDispatch often rely on a registered type library. 
    ///    If we prefer IDispatchComObject over ComObjectWithTypeInfo, users get a non-ideal experience.
    /// 2. IDispatch cannot distinguish between properties and methods with 0 arguments (and non-0 
    ///    default arguments?). So obj.foo() is ambiguous as it could mean invoking method foo, 
    ///    or it could mean invoking the function pointer returned by property foo.
    ///    We are attempting to find whether we need to call a method or a property by examining
    ///    the ITypeInfo associated with the IDispatch. ITypeInfo tell's use what parameters the method
    ///    expects, is it a method or a property, what is the default property of the object, how to 
    ///    create an enumerator for collections etc.
    /// 3. IronPython processes the signature and converts ref arguments into return values. 
    ///    However, since the signature of a DispMethod is not available beforehand, this conversion 
    ///    is not possible. There could be other signature conversions that may be affected. How does 
    ///    VB6 deal with ref arguments and IDispatch?
    ///    
    /// We also support events for IDispatch objects:
    /// Background:
    /// COM objects support events through a mechanism known as Connect Points.
    /// Connection Points are separate objects created off the actual COM 
    /// object (this is to prevent circular references between event sink
    /// and event source). When clients want to sink events generated  by 
    /// COM object they would implement callback interfaces (aka source 
    /// interfaces) and hand it over (advise) to the Connection Point. 
    /// 
    /// Implementation details:
    /// When IDispatchComObject.TryGetMember request is received we first check
    /// whether the requested member is a property or a method. If this check
    /// fails we will try to determine whether an event is requested. To do 
    /// so we will do the following set of steps:
    /// 1. Verify the COM object implements IConnectionPointContainer
    /// 2. Attempt to find COM object's coclass's description
    ///    a. Query the object for IProvideClassInfo interface. Go to 3, if found
    ///    b. From object's IDispatch retrieve primary interface description
    ///    c. Scan coclasses declared in object's type library.
    ///    d. Find coclass implementing this particular primary interface 
    /// 3. Scan coclass for all its source interfaces.
    /// 4. Check whether to any of the methods on the source interfaces matches 
    /// the request name
    /// 
    /// Once we determine that TryGetMember requests an event we will return
    /// an instance of BoundDispEvent class. This class has InPlaceAdd and
    /// InPlaceSubtract operators defined. Calling InPlaceAdd operator will:
    /// 1. An instance of ComEventSinksContainer class is created (unless 
    /// RCW already had one). This instance is hanged off the RCW in attempt
    /// to bind the lifetime of event sinks to the lifetime of the RCW itself,
    /// meaning event sink will be collected once the RCW is collected (this
    /// is the same way event sinks lifetime is controlled by PIAs).
    /// Notice: ComEventSinksContainer contains a Finalizer which will go and
    /// unadvise all event sinks.
    /// Notice: ComEventSinksContainer is a list of ComEventSink objects. 
    /// 2. Unless we have already created a ComEventSink for the required 
    /// source interface, we will create and advise a new ComEventSink. Each
    /// ComEventSink implements a single source interface that COM object 
    /// supports. 
    /// 3. ComEventSink contains a map between method DISPIDs to  the 
    /// multicast delegate that will be invoked when the event is raised.
    /// 4. ComEventSink implements IReflect interface which is exposed as
    /// custom IDispatch to COM consumers. This allows us to intercept calls
    /// to IDispatch.Invoke and apply custom logic - in particular we will
    /// just find and invoke the multicast delegate corresponding to the invoked
    /// dispid.
    ///  </summary>

    internal sealed class IDispatchComObject : ComObject, IDynamicMetaObjectProvider {

        private readonly IDispatch _dispatchObject;
        private ComTypeDesc _comTypeDesc;
        private static readonly Dictionary<Guid, ComTypeDesc> _CacheComTypeDesc = new Dictionary<Guid, ComTypeDesc>();

        internal IDispatchComObject(IDispatch rcw)
            : base(rcw) {
            _dispatchObject = rcw;
        }

        public override string ToString() {
            ComTypeDesc ctd = _comTypeDesc;
            string typeName = null;

            if (ctd != null) {
                typeName = ctd.TypeName;
            }

            if (String.IsNullOrEmpty(typeName)) {
                typeName = "IDispatch";
            }

            return String.Format(CultureInfo.CurrentCulture, "{0} ({1})", RuntimeCallableWrapper.ToString(), typeName);
        }

        public ComTypeDesc ComTypeDesc {
            get {
                EnsureScanDefinedMethods();
                return _comTypeDesc;
            }
        }

        public IDispatch DispatchObject {
            get {
                return _dispatchObject;
            }
        }

        private static int GetIDsOfNames(IDispatch dispatch, string name, out int dispId) {
            int[] dispIds = new int[1];
            Guid emtpyRiid = Guid.Empty;
            int hresult = dispatch.TryGetIDsOfNames(
                ref emtpyRiid,
                new string[] { name },
                1,
                0,
                dispIds);

            dispId = dispIds[0];
            return hresult;
        }

        private static unsafe int Invoke(IDispatch dispatch, int memberDispId, out object result) {
            Guid emtpyRiid = Guid.Empty;
            ComTypes.DISPPARAMS dispParams = new ComTypes.DISPPARAMS();
            Variant res = default;
            int hresult = dispatch.TryInvoke(
                memberDispId,
                ref emtpyRiid,
                0,
                ComTypes.INVOKEKIND.INVOKE_PROPERTYGET,
                ref dispParams,
                (IntPtr)(&res),
                IntPtr.Zero,
                IntPtr.Zero);

            result = res.ToObject();

            return hresult;
        }

        internal bool TryGetGetItem(out ComMethodDesc value) {
            ComMethodDesc methodDesc = _comTypeDesc.GetItem;
            if (methodDesc != null) {
                value = methodDesc;
                return true;
            }

            return SlowTryGetGetItem(out value);
        }

        private bool SlowTryGetGetItem(out ComMethodDesc value) {
            EnsureScanDefinedMethods();

            ComMethodDesc methodDesc = _comTypeDesc.GetItem;

            // Without type information, we really don't know whether or not we have a property getter.
            if (methodDesc == null) {
                string name = "[PROPERTYGET, DISPID(0)]";

                _comTypeDesc.EnsureGetItem(new ComMethodDesc(name, ComDispIds.DISPID_VALUE, ComTypes.INVOKEKIND.INVOKE_PROPERTYGET));
                methodDesc = _comTypeDesc.GetItem;
            }

            value = methodDesc;
            return true;
        }

        internal bool TryGetSetItem(out ComMethodDesc value) {
            ComMethodDesc methodDesc = _comTypeDesc.SetItem;
            if (methodDesc != null) {
                value = methodDesc;
                return true;
            }

            return SlowTryGetSetItem(out value);
        }

        private bool SlowTryGetSetItem(out ComMethodDesc value) {
            EnsureScanDefinedMethods();

            ComMethodDesc methodDesc = _comTypeDesc.SetItem;

            // Without type information, we really don't know whether or not we have a property setter.
            if (methodDesc == null) {
                string name = "[PROPERTYPUT, DISPID(0)]";

                _comTypeDesc.EnsureSetItem(new ComMethodDesc(name, ComDispIds.DISPID_VALUE, ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT));
                methodDesc = _comTypeDesc.SetItem;
            }

            value = methodDesc;
            return true;
        }

        internal bool TryGetMemberMethod(string name, out ComMethodDesc method) {
            EnsureScanDefinedMethods();
            return _comTypeDesc.TryGetFunc(name, out method);
        }

        internal bool TryGetMemberEvent(string name, out ComEventDesc @event) {
            EnsureScanDefinedEvents();
            return _comTypeDesc.TryGetEvent(name, out @event);
        }

        internal bool TryGetMemberMethodExplicit(string name, out ComMethodDesc method) {
            EnsureScanDefinedMethods();

            int hresult = GetIDsOfNames(_dispatchObject, name, out int dispId);

            if (hresult == ComHresults.S_OK) {
                ComMethodDesc cmd = new ComMethodDesc(name, dispId, ComTypes.INVOKEKIND.INVOKE_FUNC);
                _comTypeDesc.AddFunc(name, cmd);
                method = cmd;
                return true;
            }

            if (hresult == ComHresults.DISP_E_UNKNOWNNAME) {
                method = null;
                return false;
            }

            throw Error.CouldNotGetDispId(name, String.Format(CultureInfo.InvariantCulture, "0x{0:X})", hresult));
        }

        internal bool TryGetPropertySetterExplicit(string name, out ComMethodDesc method, Type limitType, bool holdsNull) {
            EnsureScanDefinedMethods();

            int hresult = GetIDsOfNames(_dispatchObject, name, out int dispId);

            if (hresult == ComHresults.S_OK) {
                // we do not know whether we have put or putref here
                // and we will not guess and pretend we found both.
                ComMethodDesc put = new ComMethodDesc(name, dispId, ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT);
                _comTypeDesc.AddPut(name, put);

                ComMethodDesc putref = new ComMethodDesc(name, dispId, ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF);
                _comTypeDesc.AddPutRef(name, putref);

                if (ComBinderHelpers.PreferPut(limitType, holdsNull)) {
                    method = put;
                } else {
                    method = putref;
                }
                return true;
            }

            if (hresult == ComHresults.DISP_E_UNKNOWNNAME) {
                method = null;
                return false;
            }

            throw Error.CouldNotGetDispId(name, String.Format(CultureInfo.InvariantCulture, "0x{0:X})", hresult));
        }

        internal override IList<string> GetMemberNames(bool dataOnly) {
            EnsureScanDefinedMethods();
            EnsureScanDefinedEvents();

            return ComTypeDesc.GetMemberNames(dataOnly);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal override IList<KeyValuePair<string, object>> GetMembers(IEnumerable<string> names) {
            if (names == null) {
                names = GetMemberNames(true);
            }

            Type comType = RuntimeCallableWrapper.GetType();

            var members = new List<KeyValuePair<string, object>>();
            foreach (string name in names) {
                if (name == null) {
                    continue;
                }

                if (ComTypeDesc.TryGetFunc(name, out ComMethodDesc method) && method.IsDataMember) {
                    try {
                        object value = comType.InvokeMember(
                            method.Name,
                            BindingFlags.GetProperty,
                            null,
                            RuntimeCallableWrapper,
                            new object[0],
                            CultureInfo.InvariantCulture
                        );
                        members.Add(new KeyValuePair<string, object>(method.Name, value));

                        //evaluation failed for some reason. pass exception out 
                    } catch (Exception ex) {
                        members.Add(new KeyValuePair<string, object>(method.Name, ex));
                    }
                }
            }

            return members.ToArray();
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) {
            EnsureScanDefinedMethods();
            return new IDispatchMetaObject(parameter, this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        private static void GetFuncDescForDescIndex(ComTypes.ITypeInfo typeInfo, int funcIndex, out ComTypes.FUNCDESC funcDesc, out IntPtr funcDescHandle) {
            IntPtr pFuncDesc = IntPtr.Zero;
            typeInfo.GetFuncDesc(funcIndex, out pFuncDesc);

            // GetFuncDesc should never return null, this is just to be safe
            if (pFuncDesc == IntPtr.Zero) {
                throw Error.CannotRetrieveTypeInformation();
            }

            funcDesc = (ComTypes.FUNCDESC)Marshal.PtrToStructure(pFuncDesc, typeof(ComTypes.FUNCDESC));
            funcDescHandle = pFuncDesc;
        }

        private void EnsureScanDefinedEvents() {
            // _comTypeDesc.Events is null if we have not yet attempted
            // to scan the object for events.
            if (_comTypeDesc?.Events != null) {
                return;
            }

            // check type info in the type descriptions cache
            ComTypes.ITypeInfo typeInfo = ComRuntimeHelpers.GetITypeInfoFromIDispatch(_dispatchObject, true);
            if (typeInfo == null) {
                _comTypeDesc = ComTypeDesc.CreateEmptyTypeDesc();
                return;
            }

            ComTypes.TYPEATTR typeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(typeInfo);

            if (_comTypeDesc == null) {
                lock (_CacheComTypeDesc) {
                    if (_CacheComTypeDesc.TryGetValue(typeAttr.guid, out _comTypeDesc) &&
                        _comTypeDesc.Events != null) {
                        return;
                    }
                }
            }

            ComTypeDesc typeDesc = ComTypeDesc.FromITypeInfo(typeInfo, typeAttr);

            ComTypes.ITypeInfo classTypeInfo = null;
            Dictionary<string, ComEventDesc> events = null;

            var cpc = RuntimeCallableWrapper as ComTypes.IConnectionPointContainer;
            if (cpc == null) {
                // No ICPC - this object does not support events
                events = ComTypeDesc.EmptyEvents;
            } else if ((classTypeInfo = GetCoClassTypeInfo(RuntimeCallableWrapper, typeInfo)) == null) {
                // no class info found - this object may support events
                // but we could not discover those
                events = ComTypeDesc.EmptyEvents;
            } else {
                events = new Dictionary<string, ComEventDesc>();

                ComTypes.TYPEATTR classTypeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(classTypeInfo);
                for (int i = 0; i < classTypeAttr.cImplTypes; i++) {
                    classTypeInfo.GetRefTypeOfImplType(i, out int hRefType);

                    classTypeInfo.GetRefTypeInfo(hRefType, out ComTypes.ITypeInfo interfaceTypeInfo);

                    classTypeInfo.GetImplTypeFlags(i, out ComTypes.IMPLTYPEFLAGS flags);
                    if ((flags & ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FSOURCE) != 0) {
                        ScanSourceInterface(interfaceTypeInfo, ref events);
                    }
                }

                if (events.Count == 0) {
                    events = ComTypeDesc.EmptyEvents;
                }
            }

            lock (_CacheComTypeDesc) {
                if (_CacheComTypeDesc.TryGetValue(typeAttr.guid, out ComTypeDesc cachedTypeDesc)) {
                    _comTypeDesc = cachedTypeDesc;
                } else {
                    _comTypeDesc = typeDesc;
                    _CacheComTypeDesc.Add(typeAttr.guid, _comTypeDesc);
                }
                _comTypeDesc.Events = events;
            }
        }

        private static void ScanSourceInterface(ComTypes.ITypeInfo sourceTypeInfo, ref Dictionary<string, ComEventDesc> events) {
            ComTypes.TYPEATTR sourceTypeAttribute = ComRuntimeHelpers.GetTypeAttrForTypeInfo(sourceTypeInfo);

            for (int index = 0; index < sourceTypeAttribute.cFuncs; index++) {
                IntPtr funcDescHandleToRelease = IntPtr.Zero;

                try {
                    GetFuncDescForDescIndex(sourceTypeInfo, index, out ComTypes.FUNCDESC funcDesc, out funcDescHandleToRelease);

                    // we are not interested in hidden or restricted functions for now.
                    if ((funcDesc.wFuncFlags & (int)ComTypes.FUNCFLAGS.FUNCFLAG_FHIDDEN) != 0) {
                        continue;
                    }
                    if ((funcDesc.wFuncFlags & (int)ComTypes.FUNCFLAGS.FUNCFLAG_FRESTRICTED) != 0) {
                        continue;
                    }

                    string name = ComRuntimeHelpers.GetNameOfMethod(sourceTypeInfo, funcDesc.memid);
                    name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);

                    // Sometimes coclass has multiple source interfaces. Usually this is caused by
                    // adding new events and putting them on new interfaces while keeping the
                    // old interfaces around. This may cause name collisioning which we are
                    // resolving by keeping only the first event with the same name.
                    if (events.ContainsKey(name) == false) {
                        ComEventDesc eventDesc = new ComEventDesc();
                        eventDesc.dispid = funcDesc.memid;
                        eventDesc.sourceIID = sourceTypeAttribute.guid;
                        events.Add(name, eventDesc);
                    }
                } finally {
                    if (funcDescHandleToRelease != IntPtr.Zero) {
                        sourceTypeInfo.ReleaseFuncDesc(funcDescHandleToRelease);
                    }
                }
            }
        }

        private static ComTypes.ITypeInfo GetCoClassTypeInfo(object rcw, ComTypes.ITypeInfo typeInfo) {
            Debug.Assert(typeInfo != null);

            if (rcw is IProvideClassInfo provideClassInfo) {
                IntPtr typeInfoPtr = IntPtr.Zero;
                try {
                    provideClassInfo.GetClassInfo(out typeInfoPtr);
                    if (typeInfoPtr != IntPtr.Zero) {
                        return Marshal.GetObjectForIUnknown(typeInfoPtr) as ComTypes.ITypeInfo;
                    }
                } finally {
                    if (typeInfoPtr != IntPtr.Zero) {
                        Marshal.Release(typeInfoPtr);
                    }
                }
            }

            // retrieving class information through IPCI has failed - 
            // we can try scanning the typelib to find the coclass

            typeInfo.GetContainingTypeLib(out ComTypes.ITypeLib typeLib, out int _);
            string typeName = ComRuntimeHelpers.GetNameOfType(typeInfo);

            ComTypeLibDesc typeLibDesc = ComTypeLibDesc.GetFromTypeLib(typeLib);
            ComTypeClassDesc coclassDesc = typeLibDesc.GetCoClassForInterface(typeName);
            if (coclassDesc == null) {
                return null;
            }

            Guid coclassGuid = coclassDesc.Guid;
            typeLib.GetTypeInfoOfGuid(ref coclassGuid, out ComTypes.ITypeInfo typeInfoCoClass);
            return typeInfoCoClass;
        }

        private void EnsureScanDefinedMethods() {
            if (_comTypeDesc?.Funcs != null) {
                return;
            }

            ComTypes.ITypeInfo typeInfo = ComRuntimeHelpers.GetITypeInfoFromIDispatch(_dispatchObject, true);
            if (typeInfo == null) {
                _comTypeDesc = ComTypeDesc.CreateEmptyTypeDesc();
                return;
            }

            ComTypes.TYPEATTR typeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(typeInfo);

            if (_comTypeDesc == null) {
                lock (_CacheComTypeDesc) {
                    if (_CacheComTypeDesc.TryGetValue(typeAttr.guid, out _comTypeDesc) &&
                        _comTypeDesc.Funcs != null) {
                        return;
                    }
                }
            }

            ComTypeDesc typeDesc = ComTypeDesc.FromITypeInfo(typeInfo, typeAttr);

            ComMethodDesc getItem = null;
            ComMethodDesc setItem = null;
            Hashtable funcs = new Hashtable(typeAttr.cFuncs);
            Hashtable puts = new Hashtable();
            Hashtable putrefs = new Hashtable();

            for (int definedFuncIndex = 0; definedFuncIndex < typeAttr.cFuncs; definedFuncIndex++) {
                IntPtr funcDescHandleToRelease = IntPtr.Zero;

                try {
                    GetFuncDescForDescIndex(typeInfo, definedFuncIndex, out ComTypes.FUNCDESC funcDesc, out funcDescHandleToRelease);

                    if ((funcDesc.wFuncFlags & (int)ComTypes.FUNCFLAGS.FUNCFLAG_FRESTRICTED) != 0) {
                        // This function is not meant for the script user to use.
                        continue;
                    }

                    ComMethodDesc method = new ComMethodDesc(typeInfo, funcDesc);
                    string name = method.Name.ToUpper(CultureInfo.InvariantCulture);

                    if ((funcDesc.invkind & ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT) != 0) {
                        puts.Add(name, method);

                        // for the special dispId == 0, we need to store
                        // the method descriptor for the Do(SetItem) binder. 
                        if (method.DispId == ComDispIds.DISPID_VALUE && setItem == null) {
                            setItem = method;
                        }
                        continue;
                    }
                    if ((funcDesc.invkind & ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF) != 0) {
                        putrefs.Add(name, method);
                        // for the special dispId == 0, we need to store
                        // the method descriptor for the Do(SetItem) binder. 
                        if (method.DispId == ComDispIds.DISPID_VALUE && setItem == null) {
                            setItem = method;
                        }
                        continue;
                    }

                    if (funcDesc.memid == ComDispIds.DISPID_NEWENUM) {
                        funcs.Add("GETENUMERATOR", method);
                        continue;
                    }

                    funcs.Add(name, method);

                    // for the special dispId == 0, we need to store the method descriptor 
                    // for the Do(GetItem) binder. 
                    if (funcDesc.memid == ComDispIds.DISPID_VALUE) {
                        getItem = method;
                    }
                } finally {
                    if (funcDescHandleToRelease != IntPtr.Zero) {
                        typeInfo.ReleaseFuncDesc(funcDescHandleToRelease);
                    }
                }
            }

            lock (_CacheComTypeDesc) {
                if (_CacheComTypeDesc.TryGetValue(typeAttr.guid, out ComTypeDesc cachedTypeDesc)) {
                    _comTypeDesc = cachedTypeDesc;
                } else {
                    _comTypeDesc = typeDesc;
                    _CacheComTypeDesc.Add(typeAttr.guid, _comTypeDesc);
                }
                _comTypeDesc.Funcs = funcs;
                _comTypeDesc.Puts = puts;
                _comTypeDesc.PutRefs = putrefs;
                _comTypeDesc.EnsureGetItem(getItem);
                _comTypeDesc.EnsureSetItem(setItem);
            }
        }

        internal bool TryGetPropertySetter(string name, out ComMethodDesc method, Type limitType, bool holdsNull) {
            EnsureScanDefinedMethods();

            if (ComBinderHelpers.PreferPut(limitType, holdsNull)) {
                return _comTypeDesc.TryGetPut(name, out method) ||
                    _comTypeDesc.TryGetPutRef(name, out method);
            }

            return _comTypeDesc.TryGetPutRef(name, out method) ||
                _comTypeDesc.TryGetPut(name, out method);
        }
    }
}

#endif
