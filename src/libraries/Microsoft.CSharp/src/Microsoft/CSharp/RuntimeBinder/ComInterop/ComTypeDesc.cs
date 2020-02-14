// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace Microsoft.Scripting.ComInterop {

    public class ComTypeDesc : ComTypeLibMemberDesc {
        private string _typeName;
        private string _documentation;
        //Hashtable is threadsafe for multiple readers single writer. 
        //Enumerating and writing is mutually exclusive so require locking.
        private Hashtable _funcs;
        private Hashtable _puts;
        private Hashtable _putRefs;
        private ComMethodDesc _getItem;
        private ComMethodDesc _setItem;
        private Dictionary<string, ComEventDesc> _events;
        private static readonly Dictionary<string, ComEventDesc> _EmptyEventsDict = new Dictionary<string, ComEventDesc>();

        internal ComTypeDesc(ITypeInfo typeInfo, ComType memberType, ComTypeLibDesc typeLibDesc) : base(memberType) {
            if (typeInfo != null) {
                ComRuntimeHelpers.GetInfoFromType(typeInfo, out _typeName, out _documentation);
            }
            TypeLib = typeLibDesc;
        }

        
        internal static ComTypeDesc FromITypeInfo(ITypeInfo typeInfo, TYPEATTR typeAttr)
        {
            switch (typeAttr.typekind) {
                case TYPEKIND.TKIND_COCLASS:
                    return new ComTypeClassDesc(typeInfo, null);
                case TYPEKIND.TKIND_ENUM:
                    return new ComTypeEnumDesc(typeInfo, null);
                case TYPEKIND.TKIND_DISPATCH:
                case TYPEKIND.TKIND_INTERFACE:
                    ComTypeDesc typeDesc = new ComTypeDesc(typeInfo, ComType.Interface, null);
                    return typeDesc;
                default:
                    throw new InvalidOperationException("Attempting to wrap an unsupported enum type.");
            }
        }

        internal static ComTypeDesc CreateEmptyTypeDesc() {
            ComTypeDesc typeDesc = new ComTypeDesc(null, ComType.Interface, null);
            typeDesc._funcs = new Hashtable();
            typeDesc._puts = new Hashtable();
            typeDesc._putRefs = new Hashtable();
            typeDesc._events = _EmptyEventsDict;

            return typeDesc;
        }

        internal static Dictionary<string, ComEventDesc> EmptyEvents {
            get { return _EmptyEventsDict; }
        }

        internal Hashtable Funcs {
            get { return _funcs; }
            set { _funcs = value; }
        }

        internal Hashtable Puts {
            get { return _puts; }
            set { _puts = value; }
        }

        internal Hashtable PutRefs {
            set { _putRefs = value; }
        }

        internal Dictionary<string, ComEventDesc> Events {
            get { return _events; }
            set { _events = value; }
        }

        internal bool TryGetFunc(string name, out ComMethodDesc method) {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            if (_funcs.ContainsKey(name)) {
                method = _funcs[name] as ComMethodDesc;
                return true;
            }
            method = null;
            return false;
        }

        internal void AddFunc(string name, ComMethodDesc method) {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            lock (_funcs) {
                _funcs[name] = method;
            }
        }

        internal bool TryGetPut(string name, out ComMethodDesc method) {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            if (_puts.ContainsKey(name)) {
                method = _puts[name] as ComMethodDesc;
                return true;
            }
            method = null;
            return false;
        }

        internal void AddPut(string name, ComMethodDesc method) {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            lock (_puts) {
                _puts[name] = method;
            }
        }

        internal bool TryGetPutRef(string name, out ComMethodDesc method) {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            if (_putRefs.ContainsKey(name)) {
                method = _putRefs[name] as ComMethodDesc;
                return true;
            }
            method = null;
            return false;
        }
        internal void AddPutRef(string name, ComMethodDesc method) {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            lock (_putRefs) {
                _putRefs[name] = method;
            }
        }

        internal bool TryGetEvent(string name, out ComEventDesc @event) {
            name = name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            return _events.TryGetValue(name, out @event);
        }

        internal string[] GetMemberNames(bool dataOnly) {
            var names = new Dictionary<string, object>();

            lock (_funcs) {
                foreach (ComMethodDesc func in _funcs.Values) {
                    if (!dataOnly || func.IsDataMember) {
                        names.Add(func.Name, null);
                    }
                }
            }

            if (!dataOnly) {
                lock (_puts) {
                    foreach (ComMethodDesc func in _puts.Values) {
                        if (!names.ContainsKey(func.Name)) {
                            names.Add(func.Name, null);
                        }
                    }
                }

                lock (_putRefs) {
                    foreach (ComMethodDesc func in _putRefs.Values) {
                        if (!names.ContainsKey(func.Name)) {
                            names.Add(func.Name, null);
                        }
                    }
                }

                if (_events != null && _events.Count > 0) {
                    foreach (string name in _events.Keys) {
                        if (!names.ContainsKey(name)) {
                            names.Add(name, null);
                        }
                    }
                }
            }

            string[] result = new string[names.Keys.Count];
            names.Keys.CopyTo(result, 0);
            return result;
        }

        // this property is public - accessed by an AST
        public string TypeName {
            get { return _typeName; }
        }

        internal string Documentation {
            get { return _documentation; }
        }

        // this property is public - accessed by an AST
        public ComTypeLibDesc TypeLib { get; }

        internal Guid Guid { get; set; }

        internal ComMethodDesc GetItem {
            get { return _getItem; }
        }

        internal void EnsureGetItem(ComMethodDesc candidate) {
            Interlocked.CompareExchange(ref _getItem, candidate, null);
        }

        internal ComMethodDesc SetItem {
            get { return _setItem; }
        }

        internal void EnsureSetItem(ComMethodDesc candidate) {
            Interlocked.CompareExchange(ref _setItem, candidate, null);
        }
    }
}

#endif
