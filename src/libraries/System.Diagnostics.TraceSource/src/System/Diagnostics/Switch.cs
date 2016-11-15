// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    /// <devdoc>
    /// <para>Provides an <see langword='abstract '/>base class to
    ///    create new debugging and tracing switches.</para>
    /// </devdoc>
    public abstract class Switch
    {
        private readonly string _description;
        private readonly string _displayName;
        private int _switchSetting = 0;
        private volatile bool _initialized = false;
        private bool _initializing = false;
        private volatile string _switchValueString = String.Empty;
        private string _defaultValue;
        private object _intializedLock;

        private static List<WeakReference> s_switches = new List<WeakReference>();
        private static int s_LastCollectionCount;
        private StringDictionary _attributes;

        private object IntializedLock
        {
            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread-safety")]
            get
            {
                if (_intializedLock == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange<Object>(ref _intializedLock, o, null);
                }

                return _intializedLock;
            }
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.Switch'/>
        /// class.</para>
        /// </devdoc>
        protected Switch(string displayName, string description) : this(displayName, description, "0")
        {
        }

        protected Switch(string displayName, string description, string defaultSwitchValue)
        {
            // displayName is used as a hashtable key, so it can never
            // be null.
            if (displayName == null) displayName = string.Empty;

            _displayName = displayName;
            _description = description;

            // Add a weakreference to this switch and cleanup invalid references
            lock (s_switches)
            {
                _pruneCachedSwitches();
                s_switches.Add(new WeakReference(this));
            }

            _defaultValue = defaultSwitchValue;
        }

        private static void _pruneCachedSwitches()
        {
            lock (s_switches)
            {
                if (s_LastCollectionCount != GC.CollectionCount(2))
                {
                    List<WeakReference> buffer = new List<WeakReference>(s_switches.Count);
                    for (int i = 0; i < s_switches.Count; i++)
                    {
                        Switch s = ((Switch)s_switches[i].Target);
                        if (s != null)
                        {
                            buffer.Add(s_switches[i]);
                        }
                    }
                    if (buffer.Count < s_switches.Count)
                    {
                        s_switches.Clear();
                        s_switches.AddRange(buffer);
                        s_switches.TrimExcess();
                    }
                    s_LastCollectionCount = GC.CollectionCount(2);
                }
            }
        }

        /// <devdoc>
        ///    <para>Gets a name used to identify the switch.</para>
        /// </devdoc>
        public string DisplayName
        {
            get
            {
                return _displayName;
            }
        }

        /// <devdoc>
        ///    <para>Gets a description of the switch.</para>
        /// </devdoc>
        public string Description
        {
            get
            {
                return (_description == null) ? string.Empty : _description;
            }
        }

        public StringDictionary Attributes 
        {
            get 
            {
                Initialize();
                if (_attributes == null)
                    _attributes = new StringDictionary();
                return _attributes;
            }
        }

        /// <devdoc>
        ///    <para>
        ///     Indicates the current setting for this switch.
        ///    </para>
        /// </devdoc>
        protected int SwitchSetting
        {
            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "reviewed for thread-safety")]
            get
            {
                if (!_initialized)
                {
                    if (InitializeWithStatus())
                        OnSwitchSettingChanged();
                }
                return _switchSetting;
            }
            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "reviewed for thread-safety")]
            set
            {
                bool didUpdate = false;
                lock (IntializedLock)
                {
                    _initialized = true;
                    if (_switchSetting != value)
                    {
                        _switchSetting = value;
                        didUpdate = true;
                    }
                }

                if (didUpdate)
                {
                    OnSwitchSettingChanged();
                }
            }
        }

        protected internal virtual string[] GetSupportedAttributes() => null;

        protected string Value
        {
            get
            {
                Initialize();
                return _switchValueString;
            }
            set
            {
                Initialize();
                _switchValueString = value;
                OnValueChanged();
            }
        }

        private void Initialize()
        {
            InitializeWithStatus();
        }

        private bool InitializeWithStatus()
        {
            if (!_initialized)
            {
                lock (IntializedLock)
                {
                    if (_initialized || _initializing)
                    {
                        return false;
                    }

                    // This method is re-entrent during initialization, since calls to OnValueChanged() in subclasses could end up having InitializeWithStatus()
                    // called again, we don't want to get caught in an infinite loop.
                    _initializing = true;

                    _switchValueString = _defaultValue;
                    OnValueChanged();
                    _initialized = true;
                    _initializing = false;
                }
            }

            return true;
        }

        /// <devdoc>
        ///     This method is invoked when a switch setting has been changed.  It will
        ///     be invoked the first time a switch reads its value from the registry
        ///     or environment, and then it will be invoked each time the switch's
        ///     value is changed.
        /// </devdoc>
        protected virtual void OnSwitchSettingChanged()
        {
        }

        protected virtual void OnValueChanged()
        {
            SwitchSetting = Int32.Parse(Value, CultureInfo.InvariantCulture);
        }

        internal static void RefreshAll()
        {
            lock (s_switches)
            {
                _pruneCachedSwitches();
                for (int i = 0; i < s_switches.Count; i++)
                {
                    Switch swtch = ((Switch)s_switches[i].Target);
                    if (swtch != null)
                    {
                        swtch.Refresh();
                    }
                }
            }
        }

        internal void Refresh()
        {
            lock (IntializedLock)
            {
                _initialized = false;
                Initialize();
            }
        }
    }
}
