// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

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
        private int _switchSetting;
        private bool _initialized;
        private bool _initializing;
        private volatile string _switchValueString = string.Empty;
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
            // displayName is used as a hashtable key, so it can never
            // be null.
            _displayName = displayName ?? string.Empty;
            _description = description ?? string.Empty;

            // Add a weakreference to this switch and cleanup invalid references
            var switches = s_switches;
            lock (switches)
            {
                PruneCachedSwitches(switches);
                switches.Add(new WeakReference<Switch>(this));
            }

            _defaultValue = defaultSwitchValue;
        }

        private static void PruneCachedSwitches(List<WeakReference<Switch>> switches)
        {
            if (s_LastCollectionCount != GC.CollectionCount(2))
            {
                List<WeakReference<Switch>> buffer = new List<WeakReference<Switch>>(switches.Count);
                for (int i = 0; i < switches.Count; i++)
                {
                    if (switches[i].TryGetTarget(out _))
                    {
                        buffer.Add(switches[i]);
                    }
                }
                if (buffer.Count < switches.Count)
                {
                    switches.Clear();
                    switches.AddRange(buffer);
                    switches.TrimExcess();
                }
                s_LastCollectionCount = GC.CollectionCount(2);
            }
        }

        /// <devdoc>
        ///    <para>Gets a name used to identify the switch.</para>
        /// </devdoc>
        public string DisplayName => _displayName;

        /// <devdoc>
        ///    <para>Gets a description of the switch.</para>
        /// </devdoc>
        public string Description => _description;

        public StringDictionary Attributes => _attributes ??= new StringDictionary();

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
                    Init();
                }
                return _switchSetting;

                [MethodImpl(MethodImplOptions.NoInlining)]
                void Init()
                {
                    if (Initialize())
                        OnSwitchSettingChanged();
                }
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

        protected string Value
        {
            get
            {
                if (!_initialized)
                    Initialize();
                return _switchValueString;
            }
            set
            {
                if (!_initialized)
                    Initialize();
                _switchValueString = value;
                OnValueChanged();
            }
        }

        private bool Initialize()
        {
            lock (InitializedLock)
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
            var switches = s_switches;
            lock (switches)
            {
                PruneCachedSwitches(switches);
                for (int i = 0; i < switches.Count; i++)
                {
                    if (switches[i].TryGetTarget(out Switch? swtch))
                    {
                        swtch.Refresh();
                    }
                }
            }
        }

        internal void Refresh()
        {
            lock (InitializedLock)
            {
                _initialized = false;
                Initialize();
            }
        }
    }
}
