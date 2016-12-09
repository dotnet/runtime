// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Security.Permissions;

namespace System.Timers
{
    /// <summary>
    ///    <para>Handles recurring events in an application.</para>
    /// </summary>
    [DefaultProperty("Interval"), DefaultEvent("Elapsed")]
    public partial class Timer : Component, ISupportInitialize
    {
        private double _interval;
        private bool _enabled;
        private bool _initializing;
        private bool _delayedEnable;
        private ElapsedEventHandler _onIntervalElapsed;
        private bool _autoReset;
        private ISynchronizeInvoke _synchronizingObject;
        private bool _disposed;
        private Threading.Timer _timer;
        private TimerCallback _callback;
        private object _cookie;

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Timers.Timer'/> class, with the properties
        ///    set to initial values.</para>
        /// </summary>
        public Timer()
        : base()
        {
            _interval = 100;
            _enabled = false;
            _autoReset = true;
            _initializing = false;
            _delayedEnable = false;
            _callback = new TimerCallback(MyTimerCallback);
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Timers.Timer'/> class, setting the <see cref='System.Timers.Timer.Interval'/> property to the specified period.
        ///    </para>
        /// </summary>
        public Timer(double interval)
        : this()
        {
            if (interval <= 0)
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(interval), interval));

            double roundedInterval = Math.Ceiling(interval);
            if (roundedInterval > int.MaxValue || roundedInterval <= 0)
            {
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(interval), interval));
            }

            _interval = (int)roundedInterval;
        }

        /// <summary>
        /// <para>Gets or sets a value indicating whether the Timer raises the Tick event each time the specified
        /// Interval has elapsed,
        ///    when Enabled is set to true.</para>
        /// </summary>
        [TimersDescription(nameof(SR.TimerAutoReset), null), DefaultValue(true)]
        public bool AutoReset
        {
            get
            {
                return _autoReset;
            }

            set
            {
                if (DesignMode)
                    _autoReset = value;
                else if (_autoReset != value)
                {
                    _autoReset = value;
                    if (_timer != null)
                    {
                        UpdateTimer();
                    }
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets a value indicating whether the <see cref='System.Timers.Timer'/>
        /// is able
        /// to raise events at a defined interval.</para>
        /// </summary>
        // The default value by design is false, don't change it.
        [TimersDescription(nameof(SR.TimerEnabled), null), DefaultValue(false)]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                if (DesignMode)
                {
                    _delayedEnable = value;
                    _enabled = value;
                }
                else if (_initializing)
                    _delayedEnable = value;
                else if (_enabled != value)
                {
                    if (!value)
                    {
                        if (_timer != null)
                        {
                            _cookie = null;
                            _timer.Dispose();
                            _timer = null;
                        }
                        _enabled = value;
                    }
                    else
                    {
                        _enabled = value;
                        if (_timer == null)
                        {
                            if (_disposed)
                            {
                                throw new ObjectDisposedException(GetType().Name);
                            }

                            int i = (int)Math.Ceiling(_interval);
                            _cookie = new object();
                            _timer = new Threading.Timer(_callback, _cookie, Timeout.Infinite, Timeout.Infinite);
                            _timer.Change(i, _autoReset ? i : Timeout.Infinite);
                        }
                        else
                        {
                            UpdateTimer();
                        }
                    }
                }
            }
        }

        private void UpdateTimer()
        {
            int i = (int)Math.Ceiling(_interval);
            _timer.Change(i, _autoReset ? i : Timeout.Infinite);
        }

        /// <summary>
        ///    <para>Gets or
        ///       sets the interval on which
        ///       to raise events.</para>
        /// </summary>
        [TimersDescription(nameof(SR.TimerInterval), null), DefaultValue(100d)]
        public double Interval
        {
            get
            {
                return _interval;
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentException(SR.Format(SR.TimerInvalidInterval, value, 0));

                _interval = value;
                if (_timer != null)
                {
                    UpdateTimer();
                }
            }
        }


        /// <summary>
        /// <para>Occurs when the <see cref='System.Timers.Timer.Interval'/> has
        ///    elapsed.</para>
        /// </summary>
        [TimersDescription(nameof(SR.TimerIntervalElapsed), null)]
        public event ElapsedEventHandler Elapsed
        {
            add
            {
                _onIntervalElapsed += value;
            }
            remove
            {
                _onIntervalElapsed -= value;
            }
        }

        /// <summary>
        ///    <para>
        ///       Sets the enable property in design mode to true by default.
        ///    </para>
        /// </summary>                              
        /// <internalonly/>
        public override ISite Site
        {
            get
            {
                return base.Site;
            }
            set
            {
                base.Site = value;
                if (DesignMode)
                    _enabled = true;
            }
        }


        /// <summary>
        ///    <para>Gets or sets the object used to marshal event-handler calls that are issued when
        ///       an interval has elapsed.</para>
        /// </summary>
        [DefaultValue(null), TimersDescription(nameof(SR.TimerSynchronizingObject), null)]
        public ISynchronizeInvoke SynchronizingObject
        {
            get
            {
                if (_synchronizingObject == null && DesignMode)
                {
                    IDesignerHost host = (IDesignerHost)GetService(typeof(IDesignerHost));
                    object baseComponent = host?.RootComponent;
                    if (baseComponent != null && baseComponent is ISynchronizeInvoke)
                        _synchronizingObject = (ISynchronizeInvoke)baseComponent;
                }

                return _synchronizingObject;
            }

            set
            {
                _synchronizingObject = value;
            }
        }

        /// <summary>
        ///    <para>
        ///       Notifies
        ///       the object that initialization is beginning and tells it to stand by.
        ///    </para>
        /// </summary>
        public void BeginInit()
        {
            Close();
            _initializing = true;
        }

        /// <summary>
        ///    <para>Disposes of the resources (other than memory) used by
        ///       the <see cref='System.Timers.Timer'/>.</para>
        /// </summary>
        public void Close()
        {
            _initializing = false;
            _delayedEnable = false;
            _enabled = false;

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            Close();
            _disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        ///    <para>
        ///       Notifies the object that initialization is complete.
        ///    </para>
        /// </summary>
        public void EndInit()
        {
            _initializing = false;
            Enabled = _delayedEnable;
        }

        /// <summary>
        /// <para>Starts the timing by setting <see cref='System.Timers.Timer.Enabled'/> to <see langword='true'/>.</para>
        /// </summary>
        public void Start()
        {
            Enabled = true;
        }

        /// <summary>
        ///    <para>
        ///       Stops the timing by setting <see cref='System.Timers.Timer.Enabled'/> to <see langword='false'/>.
        ///    </para>
        /// </summary>
        public void Stop()
        {
            Enabled = false;
        }

        private void MyTimerCallback(object state)
        {
            // System.Threading.Timer will not cancel the work item queued before the timer is stopped.
            // We don't want to handle the callback after a timer is stopped.
            if (state != _cookie)
            {
                return;
            }

            if (!_autoReset)
            {
                _enabled = false;
            }

            ElapsedEventArgs elapsedEventArgs = new ElapsedEventArgs(DateTime.UtcNow.ToFileTime());
            try
            {
                // To avoid race between remove handler and raising the event
                ElapsedEventHandler intervalElapsed = _onIntervalElapsed;
                if (intervalElapsed != null)
                {
                    if (SynchronizingObject != null && SynchronizingObject.InvokeRequired)
                        SynchronizingObject.BeginInvoke(intervalElapsed, new object[] { this, elapsedEventArgs });
                    else
                        intervalElapsed(this, elapsedEventArgs);
                }
            }
            catch
            {
            }
        }
    }
}

