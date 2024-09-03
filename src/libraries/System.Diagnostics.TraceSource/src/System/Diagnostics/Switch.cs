// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics
{
    /// <devdoc>
    /// <para>Provides an <see langword='abstract '/>base class to
    ///    create new debugging and tracing switches.</para>
    /// </devdoc>
    public abstract class Switch
    {
        private readonly string? _description;
        private readonly string _displayName;
        private int _switchSetting;
        private volatile bool _initialized;
        private bool _initializing;
        private volatile string? _switchValueString = string.Empty;
        private readonly string _defaultValue;
        private object? _initializedLock;

        private static readonly List<WeakReference<Switch>> s_switches = new List<WeakReference<Switch>>();
        private static int s_LastCollectionCount;
        private StringDictionary? _attributes;

        private object InitializedLock
        {
            get
            {
                if (_initializedLock == null)
                {
                    object o = new object();
                    Interlocked.CompareExchange<object?>(ref _initializedLock, o, null);
                }

                return _initializedLock;
            }
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Diagnostics.Switch'/>
        /// class.</para>
        /// </devdoc>
        protected Switch(string displayName, string? description) : this(displayName, description, "0")
        {
        }

        protected Switch(string displayName, string? description, string defaultSwitchValue)
        {
            // displayName is used as a hashtable key, so it can never be null.
            _displayName = displayName ?? string.Empty;
            _description = description;

            // Add a weakreference to this switch and cleanup invalid references
            lock (s_switches)
            {
                _pruneCachedSwitches();
                s_switches.Add(new WeakReference<Switch>(this));
            }

            _defaultValue = defaultSwitchValue;
        }

        private static void _pruneCachedSwitches()
        {
            lock (s_switches)
            {
                if (s_LastCollectionCount != GC.CollectionCount(2))
                {
                    List<WeakReference<Switch>> buffer = new List<WeakReference<Switch>>(s_switches.Count);
                    for (int i = 0; i < s_switches.Count; i++)
                    {
                        if (s_switches[i].TryGetTarget(out _))
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
                return _description ?? string.Empty;
            }
        }

        public StringDictionary Attributes
        {
            get
            {
                Initialize();
                return _attributes ??= new StringDictionary();
            }
        }

        /// <devdoc>
        ///    <para>
        ///     Indicates the current setting for this switch.
        ///    </para>
        /// </devdoc>
        protected int SwitchSetting
        {
            get
            {
                if (!_initialized)
                {
                    if (InitializeWithStatus())
                    {
                        OnSwitchSettingChanged();
                    }
                }
                return _switchSetting;
            }
            set
            {
                bool didUpdate = false;
                lock (InitializedLock)
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

        protected internal virtual string[]? GetSupportedAttributes() => null;

        internal void SetSwitchValues(int switchSetting, string switchValueString)
        {
            Initialize();

            Debug.Assert(switchValueString is not null, "Unexpected 'switchValueString' null value");
            lock (InitializedLock)
            {
                _switchSetting = switchSetting;
                _switchValueString = switchValueString;
            }
        }

        /// <summary>
        /// The default value assigned in the constructor.
        /// </summary>
        public string DefaultValue => _defaultValue;

        public string Value
        {
            get
            {
                Initialize();
                return _switchValueString!;
            }
            set
            {
                Initialize();
                _switchValueString = value;
                OnValueChanged();
            }
        }

        /// <summary>
        ///  Occurs when a <see cref="Switch"/> needs to be initialized.
        /// </summary>
        public static event EventHandler<InitializingSwitchEventArgs>? Initializing;

        internal void OnInitializing()
        {
            Initializing?.Invoke(null, new InitializingSwitchEventArgs(this));
            TraceUtils.VerifyAttributes(Attributes, GetSupportedAttributes(), this);
        }

        private void Initialize()
        {
            InitializeWithStatus();
        }

        private bool InitializeWithStatus()
        {
            if (!_initialized)
            {
                lock (InitializedLock)
                {
                    if (_initialized || _initializing)
                    {
                        return false;
                    }

                    // This method is re-entrant during initialization, since calls to OnValueChanged() in subclasses could end up having InitializeWithStatus()
                    // called again, we don't want to get caught in an infinite loop.
                    _initializing = true;

                    _switchValueString = null;

                    try
                    {
                        OnInitializing();
                    }
                    catch (Exception)
                    {
                        _initialized = false;
                        _initializing = false;
                        throw;
                    }

                    if (_switchValueString == null)
                    {
                        _switchValueString = _defaultValue;
                        OnValueChanged();
                    }

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
            SwitchSetting = int.Parse(Value, CultureInfo.InvariantCulture);
        }

        internal static void RefreshAll()
        {
            lock (s_switches)
            {
                _pruneCachedSwitches();
                for (int i = 0; i < s_switches.Count; i++)
                {
                    if (s_switches[i].TryGetTarget(out Switch? swtch))
                    {
                        swtch.Refresh();
                    }
                }
            }
        }

        /// <summary>
        /// Refreshes the trace configuration data.
        /// </summary>
        public void Refresh()
        {
            lock (InitializedLock)
            {
                _initialized = false;
                Initialize();
            }
        }
    }
}
