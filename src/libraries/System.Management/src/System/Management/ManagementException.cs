// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Management
{

    /// <summary>
    ///    <para>Represents the enumeration of all WMI error codes that are currently defined.</para>
    /// </summary>
    public enum ManagementStatus
    {
        /// <summary>
        ///    The operation was successful.
        /// </summary>
        NoError                         = 0,
        /// <summary>
        ///    <para> This value is returned when no more objects
        ///       are available, the number of objects returned is less than the number requested,
        ///       or at the end of an enumeration. It is also returned when the method is called
        ///       with a value of 0 for the "uCount" parameter.</para>
        /// </summary>
        False                           = 1,
        /// <summary>
        ///    <para>An overridden property was deleted. This value is
        ///       returned to signal that the original, non-overridden value has been restored as a
        ///       result of the deletion.</para>
        /// </summary>
        ResetToDefault                  = 0x40002,
        /// <summary>
        ///    <para> The compared items (such as objects and classes)
        ///       are not identical.</para>
        /// </summary>
        Different                       = 0x40003,
        /// <summary>
        ///    <para> A call timed out. This is not an
        ///       error condition; therefore, some results may have been returned.</para>
        /// </summary>
        Timedout                        = 0x40004,
        /// <summary>
        ///    <para> No more data is available from the enumeration; the
        ///       user should terminate the enumeration. </para>
        /// </summary>
        NoMoreData                      = 0x40005,
        /// <summary>
        ///    <para> The operation was
        ///       canceled.</para>
        /// </summary>
        OperationCanceled               = 0x40006,
        /// <summary>
        ///    <para>A request is still in progress; however, the results are not
        ///       yet available.</para>
        /// </summary>
        Pending                         = 0x40007,
        /// <summary>
        ///    <para> More than one copy of the same object was detected in
        ///       the result set of an enumeration. </para>
        /// </summary>
        DuplicateObjects                = 0x40008,
        /// <summary>
        ///    <para>The user did not receive all of the requested objects
        ///       because of inaccessible resources (other than security violations).</para>
        /// </summary>
        PartialResults                  = 0x40010,
        /// <summary>
        ///    <para>The call failed.</para>
        /// </summary>
        Failed                          = unchecked((int)0x80041001),
        /// <summary>
        ///    <para> The object could not be found. </para>
        /// </summary>
        NotFound                        = unchecked((int)0x80041002),
        /// <summary>
        ///    The current user does not have permission to perform the
        ///    action.
        /// </summary>
        AccessDenied                    = unchecked((int)0x80041003),
        /// <summary>
        ///    <para> The provider failed after
        ///       initialization. </para>
        /// </summary>
        ProviderFailure                 = unchecked((int)0x80041004),
        /// <summary>
        ///    A type mismatch occurred.
        /// </summary>
        TypeMismatch                    = unchecked((int)0x80041005),
        /// <summary>
        ///    There was not enough memory for the operation.
        /// </summary>
        OutOfMemory                     = unchecked((int)0x80041006),
        /// <summary>
        ///    <para>The context object is not valid.</para>
        /// </summary>
        InvalidContext                  = unchecked((int)0x80041007),
        /// <summary>
        ///    <para> One of the parameters to the call is not correct.
        ///    </para>
        /// </summary>
        InvalidParameter                = unchecked((int)0x80041008),
        /// <summary>
        ///    <para> The resource, typically a remote server, is not
        ///       currently available. </para>
        /// </summary>
        NotAvailable                    = unchecked((int)0x80041009),
        /// <summary>
        ///    <para>An internal, critical, and unexpected error occurred.
        ///       Report this error to Microsoft Product Support Services.</para>
        /// </summary>
        CriticalError                   = unchecked((int)0x8004100A),
        /// <summary>
        ///    <para>One or more network packets were corrupted during a remote session.</para>
        /// </summary>
        InvalidStream                   = unchecked((int)0x8004100B),
        /// <summary>
        ///    <para> The feature or operation is not supported. </para>
        /// </summary>
        NotSupported                    = unchecked((int)0x8004100C),
        /// <summary>
        ///    The specified base class is not valid.
        /// </summary>
        InvalidSuperclass               = unchecked((int)0x8004100D),
        /// <summary>
        ///    <para> The specified namespace could not be found. </para>
        /// </summary>
        InvalidNamespace                = unchecked((int)0x8004100E),
        /// <summary>
        ///    The specified instance is not valid.
        /// </summary>
        InvalidObject                   = unchecked((int)0x8004100F),
        /// <summary>
        ///    The specified class is not valid.
        /// </summary>
        InvalidClass                    = unchecked((int)0x80041010),
        /// <summary>
        ///    A provider referenced in the schema does not have a
        ///    corresponding registration.
        /// </summary>
        ProviderNotFound                = unchecked((int)0x80041011),
        /// <summary>
        ///    A provider referenced in the schema has an incorrect or
        ///    incomplete registration.
        /// </summary>
        InvalidProviderRegistration     = unchecked((int)0x80041012),
        /// <summary>
        ///    COM cannot locate a provider referenced in the schema.
        /// </summary>
        ProviderLoadFailure             = unchecked((int)0x80041013),
        /// <summary>
        ///  A component, such as a provider, failed to initialize for internal reasons.
        /// </summary>
        InitializationFailure           = unchecked((int)0x80041014),
        /// <summary>
        ///    A networking error that prevents normal operation has
        ///    occurred.
        /// </summary>
        TransportFailure                = unchecked((int)0x80041015),
        /// <summary>
        ///    <para> The requested operation is not valid. This error usually
        ///       applies to invalid attempts to delete classes or properties. </para>
        /// </summary>
        InvalidOperation                = unchecked((int)0x80041016),
        /// <summary>
        ///    The query was not syntactically valid.
        /// </summary>
        InvalidQuery                    = unchecked((int)0x80041017),
        /// <summary>
        ///    <para>The requested query language is not supported.</para>
        /// </summary>
        InvalidQueryType                = unchecked((int)0x80041018),
        /// <summary>
        /// <para>In a put operation, the <see langword='wbemChangeFlagCreateOnly'/>
        /// flag was specified, but the instance already exists.</para>
        /// </summary>
        AlreadyExists                   = unchecked((int)0x80041019),
        /// <summary>
        ///    <para>The add operation cannot be performed on the qualifier
        ///       because the owning object does not permit overrides.</para>
        /// </summary>
        OverrideNotAllowed               = unchecked((int)0x8004101A),
        /// <summary>
        ///    <para> The user attempted to delete a qualifier that was not
        ///       owned. The qualifier was inherited from a parent class. </para>
        /// </summary>
        PropagatedQualifier             = unchecked((int)0x8004101B),
        /// <summary>
        ///    <para> The user attempted to delete a property that was not
        ///       owned. The property was inherited from a parent class. </para>
        /// </summary>
        PropagatedProperty              = unchecked((int)0x8004101C),
        /// <summary>
        ///    The client made an unexpected and illegal sequence of
        ///    calls.
        /// </summary>
        Unexpected                      = unchecked((int)0x8004101D),
        /// <summary>
        ///    <para>The user requested an illegal operation, such as
        ///       spawning a class from an instance.</para>
        /// </summary>
        IllegalOperation                = unchecked((int)0x8004101E),
        /// <summary>
        ///    <para> There was an illegal attempt to specify a key qualifier
        ///       on a property that cannot be a key. The keys are specified in the class
        ///       definition for an object and cannot be altered on a per-instance basis.</para>
        /// </summary>
        CannotBeKey                     = unchecked((int)0x8004101F),
        /// <summary>
        ///    <para>The current object is not a valid class definition.
        ///       Either it is incomplete, or it has not been registered with WMI using
        ///    <see cref='System.Management.ManagementObject.Put()'/>().</para>
        /// </summary>
        IncompleteClass                 = unchecked((int)0x80041020),
        /// <summary>
        ///    Reserved for future use.
        /// </summary>
        InvalidSyntax                   = unchecked((int)0x80041021),
        /// <summary>
        ///    Reserved for future use.
        /// </summary>
        NondecoratedObject              = unchecked((int)0x80041022),
        /// <summary>
        ///    <para>The property that you are attempting to modify is read-only.</para>
        /// </summary>
        ReadOnly                        = unchecked((int)0x80041023),
        /// <summary>
        ///    <para> The provider cannot perform the requested operation, such
        ///       as requesting a query that is too complex, retrieving an instance, creating or
        ///       updating a class, deleting a class, or enumerating a class. </para>
        /// </summary>
        ProviderNotCapable               = unchecked((int)0x80041024),
        /// <summary>
        ///    <para>An attempt was made to make a change that would
        ///       invalidate a derived class.</para>
        /// </summary>
        ClassHasChildren                 = unchecked((int)0x80041025),
        /// <summary>
        ///    <para> An attempt has been made to delete or modify a class that
        ///       has instances. </para>
        /// </summary>
        ClassHasInstances                = unchecked((int)0x80041026),
        /// <summary>
        ///    Reserved for future use.
        /// </summary>
        QueryNotImplemented              = unchecked((int)0x80041027),
        /// <summary>
        ///    <para> A value of null was specified for a property that may
        ///       not be null, such as one that is marked by a <see langword='Key'/>, <see langword='Indexed'/>, or
        ///    <see langword='Not_Null'/> qualifier.</para>
        /// </summary>
        IllegalNull                     = unchecked((int)0x80041028),
        /// <summary>
        ///    <para> The value provided for a qualifier was not a
        ///       legal qualifier type.</para>
        /// </summary>
        InvalidQualifierType             = unchecked((int)0x80041029),
        /// <summary>
        ///    The CIM type specified for a property is not valid.
        /// </summary>
        InvalidPropertyType              = unchecked((int)0x8004102A),
        /// <summary>
        ///    <para> The request was made with an out-of-range value, or is
        ///       incompatible with the type. </para>
        /// </summary>
        ValueOutOfRange                  = unchecked((int)0x8004102B),
        /// <summary>
        ///    <para>An illegal attempt was made to make a class singleton,
        ///       such as when the class is derived from a non-singleton class.</para>
        /// </summary>
        CannotBeSingleton                = unchecked((int)0x8004102C),
        /// <summary>
        ///    The CIM type specified is not valid.
        /// </summary>
        InvalidCimType                   = unchecked((int)0x8004102D),
        /// <summary>
        ///    The requested method is not available.
        /// </summary>
        InvalidMethod                   = unchecked((int)0x8004102E),
        /// <summary>
        ///    <para> The parameters provided for the method are not valid.
        ///    </para>
        /// </summary>
        InvalidMethodParameters          = unchecked((int)0x8004102F),
        /// <summary>
        ///    There was an attempt to get qualifiers on a system
        ///    property.
        /// </summary>
        SystemProperty                  = unchecked((int)0x80041030),
        /// <summary>
        ///    The property type is not recognized.
        /// </summary>
        InvalidProperty                 = unchecked((int)0x80041031),
        /// <summary>
        ///    <para> An asynchronous process has been canceled internally or
        ///       by the user. Note that because of the timing and nature of the asynchronous
        ///       operation, the operation may not have been truly canceled. </para>
        /// </summary>
        CallCanceled                   = unchecked((int)0x80041032),
        /// <summary>
        ///    <para>The user has requested an operation while WMI is in the
        ///       process of quitting.</para>
        /// </summary>
        ShuttingDown                    = unchecked((int)0x80041033),
        /// <summary>
        ///    <para> An attempt was made to reuse an existing method name from
        ///       a base class, and the signatures did not match. </para>
        /// </summary>
        PropagatedMethod                = unchecked((int)0x80041034),
        /// <summary>
        ///    <para> One or more parameter values, such as a query text, is
        ///       too complex or unsupported. WMI is requested to retry the operation
        ///       with simpler parameters. </para>
        /// </summary>
        UnsupportedParameter            = unchecked((int)0x80041035),
        /// <summary>
        ///    A parameter was missing from the method call.
        /// </summary>
        MissingParameterID              = unchecked((int)0x80041036),
        /// <summary>
        ///    A method parameter has an invalid <see langword='ID'/> qualifier.
        /// </summary>
        InvalidParameterID              = unchecked((int)0x80041037),
        /// <summary>
        /// <para> One or more of the method parameters have <see langword='ID'/>
        /// qualifiers that are out of sequence. </para>
        /// </summary>
        NonconsecutiveParameterIDs      = unchecked((int)0x80041038),
        /// <summary>
        /// <para> The return value for a method has an <see langword='ID'/> qualifier.
        /// </para>
        /// </summary>
        ParameterIDOnRetval             = unchecked((int)0x80041039),
        /// <summary>
        ///    The specified object path was invalid.
        /// </summary>
        InvalidObjectPath               = unchecked((int)0x8004103A),
        /// <summary>
        ///    <para> There is not enough free disk space to continue the
        ///       operation. </para>
        /// </summary>
        OutOfDiskSpace                  = unchecked((int)0x8004103B),
        /// <summary>
        ///    <para> The supplied buffer was too small to hold all the objects
        ///       in the enumerator or to read a string property. </para>
        /// </summary>
        BufferTooSmall                  = unchecked((int)0x8004103C),
        /// <summary>
        ///    The provider does not support the requested put
        ///    operation.
        /// </summary>
        UnsupportedPutExtension         = unchecked((int)0x8004103D),
        /// <summary>
        ///    <para> An object with an incorrect type or version was
        ///       encountered during marshaling. </para>
        /// </summary>
        UnknownObjectType               = unchecked((int)0x8004103E),
        /// <summary>
        ///    <para> A packet with an incorrect type or version was
        ///       encountered during marshaling. </para>
        /// </summary>
        UnknownPacketType               = unchecked((int)0x8004103F),
        /// <summary>
        ///    The packet has an unsupported version.
        /// </summary>
        MarshalVersionMismatch          = unchecked((int)0x80041040),
        /// <summary>
        ///    <para>The packet is corrupted.</para>
        /// </summary>
        MarshalInvalidSignature         = unchecked((int)0x80041041),
        /// <summary>
        ///    An attempt has been made to mismatch qualifiers, such as
        ///    putting [key] on an object instead of a property.
        /// </summary>
        InvalidQualifier                = unchecked((int)0x80041042),
        /// <summary>
        ///    A duplicate parameter has been declared in a CIM method.
        /// </summary>
        InvalidDuplicateParameter       = unchecked((int)0x80041043),
        /// <summary>
        ///    <para> Reserved for future use. </para>
        /// </summary>
        TooMuchData                     = unchecked((int)0x80041044),
        /// <summary>
        ///    <para>The delivery of an event has failed. The provider may
        ///       choose to re-raise the event.</para>
        /// </summary>
        ServerTooBusy                   = unchecked((int)0x80041045),
        /// <summary>
        ///    The specified flavor was invalid.
        /// </summary>
        InvalidFlavor                   = unchecked((int)0x80041046),
        /// <summary>
        ///    <para> An attempt has been made to create a reference that is
        ///       circular (for example, deriving a class from itself). </para>
        /// </summary>
        CircularReference               = unchecked((int)0x80041047),
        /// <summary>
        ///    The specified class is not supported.
        /// </summary>
        UnsupportedClassUpdate          = unchecked((int)0x80041048),
        /// <summary>
        ///    <para> An attempt was made to change a key when instances or derived
        ///       classes are already using the key. </para>
        /// </summary>
        CannotChangeKeyInheritance      = unchecked((int)0x80041049),
        /// <summary>
        ///    <para> An attempt was made to change an index when instances or derived
        ///       classes are already using the index. </para>
        /// </summary>
        CannotChangeIndexInheritance    = unchecked((int)0x80041050),
        /// <summary>
        ///    <para> An attempt was made to create more properties than the
        ///       current version of the class supports. </para>
        /// </summary>
        TooManyProperties               = unchecked((int)0x80041051),
        /// <summary>
        ///    <para> A property was redefined with a conflicting type in a
        ///       derived class. </para>
        /// </summary>
        UpdateTypeMismatch              = unchecked((int)0x80041052),
        /// <summary>
        ///    <para> An attempt was made in a derived class to override a
        ///       non-overrideable qualifier. </para>
        /// </summary>
        UpdateOverrideNotAllowed        = unchecked((int)0x80041053),
        /// <summary>
        ///    <para> A method was redeclared with a conflicting signature in a
        ///       derived class. </para>
        /// </summary>
        UpdatePropagatedMethod          = unchecked((int)0x80041054),
        /// <summary>
        ///    An attempt was made to execute a method not marked with
        ///    [implemented] in any relevant class.
        /// </summary>
        MethodNotImplemented            = unchecked((int)0x80041055),
        /// <summary>
        ///    <para> An attempt was made to execute a method marked with
        ///       [disabled]. </para>
        /// </summary>
        MethodDisabled                  = unchecked((int)0x80041056),
        /// <summary>
        ///    <para> The refresher is busy with another operation. </para>
        /// </summary>
        RefresherBusy                   = unchecked((int)0x80041057),
        /// <summary>
        ///    <para> The filtering query is syntactically invalid. </para>
        /// </summary>
        UnparsableQuery                 = unchecked((int)0x80041058),
        /// <summary>
        ///    The FROM clause of a filtering query references a class
        ///    that is not an event class.
        /// </summary>
        NotEventClass                   = unchecked((int)0x80041059),
        /// <summary>
        ///    A GROUP BY clause was used without the corresponding
        ///    GROUP WITHIN clause.
        /// </summary>
        MissingGroupWithin              = unchecked((int)0x8004105A),
        /// <summary>
        ///    A GROUP BY clause was used. Aggregation on all properties
        ///    is not supported.
        /// </summary>
        MissingAggregationList          = unchecked((int)0x8004105B),
        /// <summary>
        ///    <para> Dot notation was used on a property that is not an
        ///       embedded object. </para>
        /// </summary>
        PropertyNotAnObject             = unchecked((int)0x8004105C),
        /// <summary>
        ///    A GROUP BY clause references a property that is an
        ///    embedded object without using dot notation.
        /// </summary>
        AggregatingByObject             = unchecked((int)0x8004105D),
        /// <summary>
        ///    An event provider registration query
        ///    (<see langword='__EventProviderRegistration'/>) did not specify the classes for which
        ///    events were provided.
        /// </summary>
        UninterpretableProviderQuery    = unchecked((int)0x8004105F),
        /// <summary>
        ///    <para> An request was made to back up or restore the repository
        ///       while WinMgmt.exe was using it. </para>
        /// </summary>
        BackupRestoreWinmgmtRunning     = unchecked((int)0x80041060),
        /// <summary>
        ///    <para> The asynchronous delivery queue overflowed from the
        ///       event consumer being too slow. </para>
        /// </summary>
        QueueOverflow                   = unchecked((int)0x80041061),
        /// <summary>
        ///    The operation failed because the client did not have the
        ///    necessary security privilege.
        /// </summary>
        PrivilegeNotHeld                = unchecked((int)0x80041062),
        /// <summary>
        ///    <para>The operator is not valid for this property type.</para>
        /// </summary>
        InvalidOperator                 = unchecked((int)0x80041063),
        /// <summary>
        ///    <para> The user specified a username, password, or authority on a
        ///       local connection. The user must use an empty user name and password and rely on
        ///       default security. </para>
        /// </summary>
        LocalCredentials                = unchecked((int)0x80041064),
        /// <summary>
        ///    <para> The class was made abstract when its base class is not
        ///       abstract. </para>
        /// </summary>
        CannotBeAbstract                = unchecked((int)0x80041065),
        /// <summary>
        ///    <para> An amended object was used in a put operation without the
        ///       WBEM_FLAG_USE_AMENDED_QUALIFIERS flag being specified. </para>
        /// </summary>
        AmendedObject                   = unchecked((int)0x80041066),
        /// <summary>
        ///    The client was not retrieving objects quickly enough from
        ///    an enumeration.
        /// </summary>
        ClientTooSlow                   = unchecked((int)0x80041067),

        /// <summary>
        ///    <para> The provider registration overlaps with the system event
        ///       domain. </para>
        /// </summary>
        RegistrationTooBroad            = unchecked((int)0x80042001),
        /// <summary>
        ///    <para> A WITHIN clause was not used in this query. </para>
        /// </summary>
        RegistrationTooPrecise          = unchecked((int)0x80042002)
    }

    /// <summary>
    ///    <para> Represents management exceptions.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample demonstrates how to display error
    /// // information stored in a ManagementException object.
    /// class Sample_ManagementException
    /// {
    ///     public static int Main(string[] args)
    ///     {
    ///         try
    ///         {
    ///             ManagementObject disk =
    ///                 new ManagementObject("Win32_LogicalDisk.DeviceID='BAD:'");
    ///             disk.Get(); // throws ManagementException
    ///             Console.WriteLine("This shouldn't be displayed.");
    ///         }
    ///         catch (ManagementException e)
    ///         {
    ///           Console.WriteLine("ErrorCode " + e.ErrorCode);
    ///           Console.WriteLine("Message " + e.Message);
    ///           Console.WriteLine("Source " + e.Source);
    ///           if (e.ErrorInformation) //extended error object
    ///               Console.WriteLine("Extended Description : " + e.ErrorInformation["Description"]);
    ///         }
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrates how to display error
    /// ' information stored in a ManagementException object.
    /// Class Sample_ManagementException
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Try
    ///             Dim disk As New ManagementObject("Win32_LogicalDisk.DeviceID='BAD:'")
    ///             disk.Get() ' throws ManagementException
    ///             Console.WriteLine("This shouldn't be displayed.")
    ///         Catch e As ManagementException
    ///             Console.WriteLine("ErrorCode " &amp; e.ErrorCode)
    ///             Console.WriteLine("Message " &amp; e.Message)
    ///             Console.WriteLine("Source " &amp; e.Source)
    ///             If e.ErrorInformation != Nothing Then 'extended error object
    ///                 Console.WriteLine("Extended Description : " &amp; e.ErrorInformation("Description"))
    ///             End If
    ///         End Try
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    [Serializable]
    public class ManagementException : SystemException
    {
        private readonly ManagementBaseObject errorObject;
        private readonly ManagementStatus     errorCode;

        internal static void ThrowWithExtendedInfo(ManagementStatus errorCode)
        {
            ManagementBaseObject errObj = null;
            string msg = null;

            //Try to get extended error info first, and save in errorObject member
            IWbemClassObjectFreeThreaded obj = WbemErrorInfo.GetErrorInfo();
            if (obj != null)
                errObj = new ManagementBaseObject(obj);

            //If the error code is not a WMI one and there's an extended error object available, stick the message
            //from the extended error object in.
            if (((msg = GetMessage(errorCode)) == null) && (errObj != null))
                try
                {
                    msg = (string)errObj["Description"];
                }
                catch { }

            throw new ManagementException(errorCode, msg, errObj);
        }


        internal static void ThrowWithExtendedInfo(Exception e)
        {
            ManagementBaseObject errObj = null;
            string msg = null;

            //Try to get extended error info first, and save in errorObject member
            IWbemClassObjectFreeThreaded obj = WbemErrorInfo.GetErrorInfo();
            if (obj != null)
                errObj = new ManagementBaseObject(obj);

            //If the error code is not a WMI one and there's an extended error object available, stick the message
            //from the extended error object in.
            if (((msg = GetMessage(e)) == null) && (errObj != null))
                try
                {
                    msg = (string)errObj["Description"];
                }
                catch { }

            throw new ManagementException(e, msg, errObj);
        }


        internal ManagementException(ManagementStatus errorCode, string msg, ManagementBaseObject errObj) : base(msg)
        {
            this.errorCode = errorCode;
            this.errorObject = errObj;
        }

        internal ManagementException(Exception e, string msg, ManagementBaseObject errObj) : base(msg, e)
        {
            try
            {
                if (e is ManagementException)
                {
                    errorCode = ((ManagementException)e).ErrorCode;

                    // May/may not have extended error info.
                    //
                    if (errorObject != null)
                        errorObject = (ManagementBaseObject)((ManagementException)e).errorObject.Clone();
                    else
                        errorObject = null;
                }
                else if (e is COMException)
                    errorCode = (ManagementStatus)((COMException)e).ErrorCode;
                else
                    errorCode = (ManagementStatus)this.HResult;
            }
            catch { }
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementException'/> class that is serializable.</para>
        /// </summary>
        /// <param name='info'>The <see cref='System.Runtime.Serialization.SerializationInfo'/> to populate with data.</param>
        /// <param name='context'>The destination (see <see cref='System.Runtime.Serialization.StreamingContext'/> ) for this serialization.</param>
#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected ManagementException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            errorCode = (ManagementStatus)info.GetValue("errorCode", typeof(ManagementStatus));
        }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementException'/> class</para>
        /// </summary>
        public ManagementException() : this(ManagementStatus.Failed, "", null)
        {

        }


        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementException'/>
        /// class with a specified error message.</para>
        /// </summary>
        /// <param name='message'>The message that describes the error.</param>
        public ManagementException(string message) : this(ManagementStatus.Failed, message, null)
        {

        }

        /// <summary>
        /// <para>Initializes a empty new instance of the <see cref='System.Management.ManagementException'/> class </para>
        /// </summary>
        /// <param name='message'>The message that describes the error.</param>
        /// <param name='innerException'>The exception that is the cause of the current exception. If the innerException
        /// parameter is not a null reference (Nothing in Visual Basic), the current exception is raised in a catch
        /// block that handles the inner exception.</param>
        public ManagementException(string message, Exception innerException) : this(innerException, message, null)
        {
            // if the exception passed is not a ManagementException, then initialize the ErrorCode to Failed
            if (!(innerException is ManagementException))
                errorCode = ManagementStatus.Failed;
        }

        /// <summary>
        /// <para>Populates the <see cref='System.Runtime.Serialization.SerializationInfo'/> object with the data needed to
        ///    serialize the <see cref='System.Management.ManagementException'/>
        ///    object.</para>
        /// </summary>
        /// <param name='info'>The <see cref='System.Runtime.Serialization.SerializationInfo'/> to populate with data.</param>
        /// <param name='context'>The destination (see <see cref='System.Runtime.Serialization.StreamingContext'/> ) for this serialization.</param>
#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("errorCode", errorCode);

            // For .NET Framework compat we need to set `errorObject` as null
            info.AddValue("errorObject", null);
        }

        private static string GetMessage(Exception e)
        {
            string msg = null;

            if (e is COMException)
            {
                // Try and get WMI error message. If not use the one in
                // the exception
                msg = GetMessage((ManagementStatus)((COMException)e).ErrorCode);
            }

            if (null == msg)
                msg = e.Message;

            return msg;
        }

        private static string GetMessage(ManagementStatus errorCode)
        {
            string msg = null;
            IWbemStatusCodeText statusCode = null;
            int hr;

            statusCode = (IWbemStatusCodeText)new WbemStatusCodeText();
            if (statusCode != null)
            {
                try
                {
                    hr = statusCode.GetErrorCodeText_((int)errorCode, 0, 1, out msg);

                    // Just in case it didn't like the flag=1, try it again
                    // with flag=0.
                    if (hr != 0)
                        hr = statusCode.GetErrorCodeText_((int)errorCode, 0, 0, out msg);
                }
                catch { }
            }

            return msg;
        }

        /// <summary>
        ///    <para>Gets the extended error object provided by WMI.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.ManagementBaseObject'/> representing the
        ///    extended error object provided by WMI, if available; <see langword='null'/>
        ///    otherwise.</para>
        /// </value>
        public ManagementBaseObject ErrorInformation
        {
            get
            { return errorObject; }
        }

        /// <summary>
        ///    <para>Gets the error code reported by WMI, which caused this exception.</para>
        /// </summary>
        /// <value>
        ///    A <see cref='System.Management.ManagementStatus'/> value representing the error code returned by
        ///    the WMI operation.
        /// </value>
        public ManagementStatus ErrorCode
        {
            get
            { return errorCode; }
        }

    }
}
