// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Management
{

    internal sealed class IdentifierChangedEventArgs : EventArgs
    {
        internal IdentifierChangedEventArgs() { }
    }

    internal sealed class InternalObjectPutEventArgs : EventArgs
    {
        private readonly ManagementPath path;

        internal InternalObjectPutEventArgs(ManagementPath path)
        {
            this.path = path.Clone();
        }

        internal ManagementPath Path
        {
            get { return path; }
        }
    }


    /// <summary>
    ///    <para>Represents the virtual base class to hold event data for WMI events.</para>
    /// </summary>
    public abstract class ManagementEventArgs : EventArgs
    {
        private readonly object context;

        /// <summary>
        /// Constructor. This is not callable directly by applications.
        /// </summary>
        /// <param name="context">The operation context which is echoed back
        /// from the operation which trigerred the event.</param>
        internal ManagementEventArgs(object context)
        {
            this.context = context;
        }

        /// <summary>
        ///    <para> Gets the operation context echoed back
        ///       from the operation that triggered the event.</para>
        /// </summary>
        /// <value>
        ///    A WMI context object containing
        ///    context information provided by the operation that triggered the event.
        /// </value>
        public object Context
        {
            get { return context; }
        }
    }

    /// <summary>
    /// <para>Holds event data for the <see cref='System.Management.ManagementOperationObserver.ObjectReady'/> event.</para>
    /// </summary>
    public class ObjectReadyEventArgs : ManagementEventArgs
    {
        private readonly ManagementBaseObject wmiObject;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">The operation context which is echoed back
        /// from the operation which triggerred the event.</param>
        /// <param name="wmiObject">The newly arrived WmiObject.</param>
        internal ObjectReadyEventArgs(object context, ManagementBaseObject wmiObject) : base(context)
        {
            this.wmiObject = wmiObject;
        }

        /// <summary>
        ///    <para> Gets the newly-returned object.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.ManagementBaseObject'/> representing the
        ///    newly-returned object.</para>
        /// </value>
        public ManagementBaseObject NewObject
        {
            get
            {
                return wmiObject;
            }
        }
    }

    /// <summary>
    /// <para> Holds event data for the <see cref='System.Management.ManagementOperationObserver.Completed'/> event.</para>
    /// </summary>
    public class CompletedEventArgs : ManagementEventArgs
    {
        private readonly int status;
        private readonly ManagementBaseObject wmiObject;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">The operation context which is echoed back
        /// from the operation which trigerred the event.</param>
        /// <param name="status">The completion status of the operation.</param>
        /// <param name="wmiStatusObject">Additional status information
        /// encapsulated within a WmiObject. This may be null.</param>
        internal CompletedEventArgs(object context, int status, ManagementBaseObject wmiStatusObject) : base(context)
        {
            wmiObject = wmiStatusObject;
            this.status = status;
        }

        /// <summary>
        ///    <para>Gets or sets additional status information
        ///       within a WMI object. This may be null.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='null '/> if an error did not occur. Otherwise, may be non-null if the provider
        ///    supports extended error information.</para>
        /// </value>
        public ManagementBaseObject StatusObject
        {
            get
            {
                return wmiObject;
            }
        }

        /// <summary>
        ///    <para>Gets the completion status of the operation.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.ManagementStatus'/> value
        ///    indicating the return code of the operation.</para>
        /// </value>
        public ManagementStatus Status
        {
            get
            {
                return (ManagementStatus)status;
            }
        }
    }

    /// <summary>
    /// <para>Holds event data for the <see cref='System.Management.ManagementOperationObserver.ObjectPut'/> event.</para>
    /// </summary>
    public class ObjectPutEventArgs : ManagementEventArgs
    {
        private readonly ManagementPath wmiPath;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">The operation context which is echoed back
        /// from the operation which trigerred the event.</param>
        /// <param name="path">The WmiPath representing the identity of the
        /// object that has been put.</param>
        internal ObjectPutEventArgs(object context, ManagementPath path) : base(context)
        {
            wmiPath = path;
        }

        /// <summary>
        ///    <para> Gets the identity of the
        ///       object that has been put.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.ManagementPath'/> containing the path of the object that has
        ///    been put.</para>
        /// </value>
        public ManagementPath Path
        {
            get
            {
                return wmiPath;
            }
        }
    }

    /// <summary>
    /// <para>Holds event data for the <see cref='System.Management.ManagementOperationObserver.Progress'/> event.</para>
    /// </summary>
    public class ProgressEventArgs : ManagementEventArgs
    {
        private readonly int upperBound;
        private readonly int current;
        private readonly string message;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context">The operation context which is echoed back
        /// from the operation which trigerred the event.</param>
        /// <param name="upperBound">A quantity representing the total
        /// amount of work required to be done by the operation.</param>
        /// <param name="current">A quantity representing the current
        /// amount of work required to be done by the operation. This is
        /// always less than or equal to upperBound.</param>
        /// <param name="message">Optional additional information regarding
        /// operation progress.</param>
        internal ProgressEventArgs(object context, int upperBound, int current, string message) : base(context)
        {
            this.upperBound = upperBound;
            this.current = current;
            this.message = message;
        }

        /// <summary>
        ///    <para> Gets the total
        ///       amount of work required to be done by the operation.</para>
        /// </summary>
        /// <value>
        ///    An integer representing the total
        ///    amount of work for the operation.
        /// </value>
        public int UpperBound
        {
            get
            {
                return upperBound;
            }
        }

        /// <summary>
        ///    <para> Gets the current amount of work
        ///       done by the operation. This is always less than or equal to <see cref='System.Management.ProgressEventArgs.UpperBound'/>.</para>
        /// </summary>
        /// <value>
        ///    <para>An integer representing the current amount of work
        ///       already completed by the operation.</para>
        /// </value>
        public int Current
        {
            get
            {
                return current;
            }
        }

        /// <summary>
        ///    <para>Gets or sets optional additional information regarding the operation's progress.</para>
        /// </summary>
        /// <value>
        ///    A string containing additional
        ///    information regarding the operation's progress.
        /// </value>
        public string Message
        {
            get
            {
                return message ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// <para>Holds event data for the <see cref='System.Management.ManagementEventWatcher.EventArrived'/> event.</para>
    /// </summary>
    public class EventArrivedEventArgs : ManagementEventArgs
    {
        private readonly ManagementBaseObject eventObject;

        internal EventArrivedEventArgs(object context, ManagementBaseObject eventObject) : base(context)
        {
            this.eventObject = eventObject;
        }

        /// <summary>
        ///    <para> Gets the WMI event that was delivered.</para>
        /// </summary>
        /// <value>
        ///    The object representing the WMI event.
        /// </value>
        public ManagementBaseObject NewEvent
        {
            get { return this.eventObject; }
        }
    }

    /// <summary>
    /// <para>Holds event data for the <see cref='System.Management.ManagementEventWatcher.Stopped'/> event.</para>
    /// </summary>
    public class StoppedEventArgs : ManagementEventArgs
    {
        private readonly int status;

        internal StoppedEventArgs(object context, int status) : base(context)
        {
            this.status = status;
        }

        /// <summary>
        ///    <para> Gets the completion status of the operation.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.ManagementStatus'/> value representing the status of the
        ///    operation.</para>
        /// </value>
        public ManagementStatus Status
        {
            get
            {
                return (ManagementStatus)status;
            }
        }
    }

}
