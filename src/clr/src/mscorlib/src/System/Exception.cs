// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: The base class for all exceptional conditions.
**
**
=============================================================================*/

namespace System {
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Diagnostics;
    using System.Security.Permissions;
    using System.Security;
    using System.IO;
    using System.Text;
    using System.Reflection;
    using System.Collections;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_Exception))]
    [Serializable]
    [ComVisible(true)]
    public class Exception : ISerializable, _Exception
    {
        private void Init()
        {
            _message = null;
            _stackTrace = null;
            _dynamicMethods = null;
            HResult = __HResults.COR_E_EXCEPTION;
            _xcode = _COMPlusExceptionCode;
            _xptrs = (IntPtr) 0;

            // Initialize the WatsonBuckets to be null
            _watsonBuckets = null;

            // Initialize the watson bucketing IP
            _ipForWatsonBuckets = UIntPtr.Zero;

#if FEATURE_SERIALIZATION
             _safeSerializationManager = new SafeSerializationManager();
#endif // FEATURE_SERIALIZATION
        }

        public Exception() {
            Init();
        }
    
        public Exception(String message) {
            Init();
            _message = message;
        }
    
        // Creates a new Exception.  All derived classes should 
        // provide this constructor.
        // Note: the stack trace is not started until the exception 
        // is thrown
        // 
        public Exception (String message, Exception innerException) {
            Init();
            _message = message;
            _innerException = innerException;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected Exception(SerializationInfo info, StreamingContext context) 
        {
            if (info==null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
    
            _className = info.GetString("ClassName");
            _message = info.GetString("Message");
            _data = (IDictionary)(info.GetValueNoThrow("Data",typeof(IDictionary)));
            _innerException = (Exception)(info.GetValue("InnerException",typeof(Exception)));
            _helpURL = info.GetString("HelpURL");
            _stackTraceString = info.GetString("StackTraceString");
            _remoteStackTraceString = info.GetString("RemoteStackTraceString");
            _remoteStackIndex = info.GetInt32("RemoteStackIndex");

            _exceptionMethodString = (String)(info.GetValue("ExceptionMethod",typeof(String)));
            HResult = info.GetInt32("HResult");
            _source = info.GetString("Source");
    
            // Get the WatsonBuckets that were serialized - this is particularly
            // done to support exceptions going across AD transitions.
            // 
            // We use the no throw version since we could be deserializing a pre-V4
            // exception object that may not have this entry. In such a case, we would
            // get null.
            _watsonBuckets = (Object)info.GetValueNoThrow("WatsonBuckets", typeof(byte[]));

#if FEATURE_SERIALIZATION
            _safeSerializationManager = info.GetValueNoThrow("SafeSerializationManager", typeof(SafeSerializationManager)) as SafeSerializationManager;
#endif // FEATURE_SERIALIZATION

            if (_className == null || HResult==0)
                throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
            
            // If we are constructing a new exception after a cross-appdomain call...
            if (context.State == StreamingContextStates.CrossAppDomain)
            {
                // ...this new exception may get thrown.  It is logically a re-throw, but 
                //  physically a brand-new exception.  Since the stack trace is cleared 
                //  on a new exception, the "_remoteStackTraceString" is provided to 
                //  effectively import a stack trace from a "remote" exception.  So,
                //  move the _stackTraceString into the _remoteStackTraceString.  Note
                //  that if there is an existing _remoteStackTraceString, it will be 
                //  preserved at the head of the new string, so everything works as 
                //  expected.
                // Even if this exception is NOT thrown, things will still work as expected
                //  because the StackTrace property returns the concatenation of the
                //  _remoteStackTraceString and the _stackTraceString.
                _remoteStackTraceString = _remoteStackTraceString + _stackTraceString;
                _stackTraceString = null;
            }
        }
        
        
        public virtual String Message {
               get {  
                if (_message == null) {
                    if (_className==null) {
                        _className = GetClassName();
                    }
                    return Environment.GetResourceString("Exception_WasThrown", _className);

                } else {
                    return _message;
                }
            }
        }

        public virtual IDictionary Data { 
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                if (_data == null)
                    if (IsImmutableAgileException(this))
                        _data = new EmptyReadOnlyDictionaryInternal();
                    else
                        _data = new ListDictionaryInternal();
                
                return _data;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool IsImmutableAgileException(Exception e);

#if FEATURE_COMINTEROP
        //
        // Exception requires anything to be added into Data dictionary is serializable
        // This wrapper is made serializable to satisfy this requirement but does NOT serialize 
        // the object and simply ignores it during serialization, because we only need 
        // the exception instance in the app to hold the error object alive.
        // Once the exception is serialized to debugger, debugger only needs the error reference string
        //
        [Serializable]
        internal class __RestrictedErrorObject
        {
            // Hold the error object instance but don't serialize/deserialize it
            [NonSerialized]
            private object _realErrorObject;

            internal __RestrictedErrorObject(object errorObject)
            {
                _realErrorObject = errorObject;    
            }

            public object RealErrorObject
            {
               get
               {
                   return _realErrorObject;
               }
            }
        }

        [FriendAccessAllowed]
        internal void AddExceptionDataForRestrictedErrorInfo(
            string restrictedError, 
            string restrictedErrorReference, 
            string restrictedCapabilitySid,
            object restrictedErrorObject,
            bool hasrestrictedLanguageErrorObject = false)
        {
            IDictionary dict = Data;
            if (dict != null)
            {
                dict.Add("RestrictedDescription", restrictedError);
                dict.Add("RestrictedErrorReference", restrictedErrorReference);
                dict.Add("RestrictedCapabilitySid", restrictedCapabilitySid);

                // Keep the error object alive so that user could retrieve error information
                // using Data["RestrictedErrorReference"]
                dict.Add("__RestrictedErrorObject", (restrictedErrorObject == null ? null : new __RestrictedErrorObject(restrictedErrorObject)));
                dict.Add("__HasRestrictedLanguageErrorObject", hasrestrictedLanguageErrorObject);
            }
        }

        internal bool TryGetRestrictedLanguageErrorObject(out object restrictedErrorObject)
        {
            restrictedErrorObject = null;
            if (Data != null && Data.Contains("__HasRestrictedLanguageErrorObject"))
            {
                if (Data.Contains("__RestrictedErrorObject"))
                {
                    __RestrictedErrorObject restrictedObject = Data["__RestrictedErrorObject"] as __RestrictedErrorObject;
                    if (restrictedObject != null)
                        restrictedErrorObject = restrictedObject.RealErrorObject;
                }
                return (bool)Data["__HasRestrictedLanguageErrorObject"];
            }

            return false;
        }
#endif // FEATURE_COMINTEROP

        private string GetClassName()
        {
            // Will include namespace but not full instantiation and assembly name.
            if (_className == null)
                _className = GetType().ToString();

            return _className;
        }
    
        // Retrieves the lowest exception (inner most) for the given Exception.
        // This will traverse exceptions using the innerException property.
        //
        public virtual Exception GetBaseException() 
        {
            Exception inner = InnerException;
            Exception back = this;
            
            while (inner != null) {
                back = inner;
                inner = inner.InnerException;
            }
            
            return back;
        }
        
        // Returns the inner exception contained in this exception
        // 
        public Exception InnerException {
            get { return _innerException; }
        }


        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static extern private IRuntimeMethodInfo GetMethodFromStackTrace(Object stackTrace);

        [System.Security.SecuritySafeCritical]  // auto-generated
        private MethodBase GetExceptionMethodFromStackTrace()
        {
            IRuntimeMethodInfo method = GetMethodFromStackTrace(_stackTrace);

            // Under certain race conditions when exceptions are re-used, this can be null
            if (method == null)
                return null;

            return RuntimeType.GetMethodBase(method);
        }
    
        public MethodBase TargetSite {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return GetTargetSiteInternal();
            }
        }
    

        // this function is provided as a private helper to avoid the security demand
        [System.Security.SecurityCritical]  // auto-generated
        private MethodBase GetTargetSiteInternal() {
            if (_exceptionMethod!=null) {
                return _exceptionMethod;
            }
            if (_stackTrace==null) {
                return null;
            }

            if (_exceptionMethodString!=null) {
                _exceptionMethod = GetExceptionMethodFromString();
            } else {
                _exceptionMethod = GetExceptionMethodFromStackTrace();
            }
            return _exceptionMethod;
        }
    
        // Returns the stack trace as a string.  If no stack trace is
        // available, null is returned.
        public virtual String StackTrace
        {
#if FEATURE_CORECLR
            [System.Security.SecuritySafeCritical] 
#endif
            get 
            {
                // By default attempt to include file and line number info
                return GetStackTrace(true);
            }
        }

        // Computes and returns the stack trace as a string
        // Attempts to get source file and line number information if needFileInfo
        // is true.  Note that this requires FileIOPermission(PathDiscovery), and so
        // will usually fail in CoreCLR.  To avoid the demand and resulting
        // SecurityException we can explicitly not even try to get fileinfo.
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        private string GetStackTrace(bool needFileInfo)
        {
            string stackTraceString = _stackTraceString;
            string remoteStackTraceString = _remoteStackTraceString;

#if !FEATURE_CORECLR
            if (!needFileInfo)
            {
                // Filter out file names/paths and line numbers from _stackTraceString and _remoteStackTraceString.
                // This is used only when generating stack trace for Watson where the strings must be PII-free.
                stackTraceString = StripFileInfo(stackTraceString, false);
                remoteStackTraceString = StripFileInfo(remoteStackTraceString, true);
            }
#endif // !FEATURE_CORECLR

            // if no stack trace, try to get one
            if (stackTraceString != null)
            {
                return remoteStackTraceString + stackTraceString;
            }
            if (_stackTrace == null)
            {
                return remoteStackTraceString;
            }

            // Obtain the stack trace string. Note that since Environment.GetStackTrace
            // will add the path to the source file if the PDB is present and a demand
            // for FileIOPermission(PathDiscovery) succeeds, we need to make sure we 
            // don't store the stack trace string in the _stackTraceString member variable.
            String tempStackTraceString = Environment.GetStackTrace(this, needFileInfo);
            return remoteStackTraceString + tempStackTraceString;
         }
    
        [FriendAccessAllowed]
        internal void SetErrorCode(int hr)
        {
            HResult = hr;
        }
        
        // Sets the help link for this exception.
        // This should be in a URL/URN form, such as:
        // "file:///C:/Applications/Bazzal/help.html#ErrorNum42"
        // Changed to be a read-write String and not return an exception
        public virtual String HelpLink
        {
            get
            {
                return _helpURL;
            }
            set
            {
                _helpURL = value;
            }
        }
    
        public virtual String Source {
#if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
#endif
            get { 
                if (_source == null)
                {
                    StackTrace st = new StackTrace(this,true);
                    if (st.FrameCount>0)
                    {
                        StackFrame sf = st.GetFrame(0);
                        MethodBase method = sf.GetMethod();

                        Module module = method.Module;

                        RuntimeModule rtModule = module as RuntimeModule;

                        if (rtModule == null)
                        {
                            System.Reflection.Emit.ModuleBuilder moduleBuilder = module as System.Reflection.Emit.ModuleBuilder;
                            if (moduleBuilder != null)
                                rtModule = moduleBuilder.InternalModule;
                            else
                                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeReflectionObject"));
                        }

                        _source = rtModule.GetRuntimeAssembly().GetSimpleName();
                    }
                }

                return _source;
            }
#if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
#endif
            set { _source = value; }
        }

#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] 
#endif
        public override String ToString()
        {
            return ToString(true, true);
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        private String ToString(bool needFileLineInfo, bool needMessage) {
            String message = (needMessage ? Message : null);
            String s;

            if (message == null || message.Length <= 0) {
                s = GetClassName();
            }
            else {
                s = GetClassName() + ": " + message;
            }

            if (_innerException!=null) {
                s = s + " ---> " + _innerException.ToString(needFileLineInfo, needMessage) + Environment.NewLine + 
                "   " + Environment.GetResourceString("Exception_EndOfInnerExceptionStack");

            }

            string stackTrace = GetStackTrace(needFileLineInfo);
            if (stackTrace != null)
            {
                s += Environment.NewLine + stackTrace;
            }

            return s;
        }
    
        [System.Security.SecurityCritical]  // auto-generated
        private String GetExceptionMethodString() {
            MethodBase methBase = GetTargetSiteInternal();
            if (methBase==null) {
                return null;
            }
            if (methBase is System.Reflection.Emit.DynamicMethod.RTDynamicMethod)
            {
                // DynamicMethods cannot be serialized
                return null;
            }

            // Note that the newline separator is only a separator, chosen such that
            //  it won't (generally) occur in a method name.  This string is used 
            //  only for serialization of the Exception Method.
            char separator = '\n';
            StringBuilder result = new StringBuilder();
            if (methBase is ConstructorInfo) {
                RuntimeConstructorInfo rci = (RuntimeConstructorInfo)methBase;
                Type t = rci.ReflectedType;
                result.Append((int)MemberTypes.Constructor);
                result.Append(separator);
                result.Append(rci.Name);
                if (t!=null)
                {
                    result.Append(separator);
                    result.Append(t.Assembly.FullName);
                    result.Append(separator);
                    result.Append(t.FullName);
                }
                result.Append(separator);
                result.Append(rci.ToString());
            } else {
                Contract.Assert(methBase is MethodInfo, "[Exception.GetExceptionMethodString]methBase is MethodInfo");
                RuntimeMethodInfo rmi = (RuntimeMethodInfo)methBase;
                Type t = rmi.DeclaringType;
                result.Append((int)MemberTypes.Method);
                result.Append(separator);
                result.Append(rmi.Name);
                result.Append(separator);
                result.Append(rmi.Module.Assembly.FullName);
                result.Append(separator);
                if (t != null)
                {
                    result.Append(t.FullName);
                    result.Append(separator);
                }
                result.Append(rmi.ToString());
            }
            
            return result.ToString();
        }

        [System.Security.SecurityCritical]  // auto-generated
        private MethodBase GetExceptionMethodFromString() {
            Contract.Assert(_exceptionMethodString != null, "Method string cannot be NULL!");
            String[] args = _exceptionMethodString.Split(new char[]{'\0', '\n'});
            if (args.Length!=5) {
                throw new SerializationException();
            }
            SerializationInfo si = new SerializationInfo(typeof(MemberInfoSerializationHolder), new FormatterConverter());
            si.AddValue("MemberType", (int)Int32.Parse(args[0], CultureInfo.InvariantCulture), typeof(Int32));
            si.AddValue("Name", args[1], typeof(String));
            si.AddValue("AssemblyName", args[2], typeof(String));
            si.AddValue("ClassName", args[3]);
            si.AddValue("Signature", args[4]);
            MethodBase result;
            StreamingContext sc = new StreamingContext(StreamingContextStates.All);
            try {
                result = (MethodBase)new MemberInfoSerializationHolder(si, sc).GetRealObject(sc);
            } catch (SerializationException) {
                result = null;
            }
            return result;
        }

#if FEATURE_SERIALIZATION
        protected event EventHandler<SafeSerializationEventArgs> SerializeObjectState
        {
            add { _safeSerializationManager.SerializeObjectState += value; }
            remove { _safeSerializationManager.SerializeObjectState -= value; }
        }
#endif // FEATURE_SERIALIZATION

        [System.Security.SecurityCritical]  // auto-generated_required
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) 
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();

            String tempStackTraceString = _stackTraceString;        
    
            if (_stackTrace!=null) 
            {
                if (tempStackTraceString==null) 
                {
                    tempStackTraceString = Environment.GetStackTrace(this, true);
                }
                if (_exceptionMethod==null) 
                {
                    _exceptionMethod = GetExceptionMethodFromStackTrace();
                }
            }

            if (_source == null) 
            {
                _source = Source; // Set the Source information correctly before serialization
            }
    
            info.AddValue("ClassName", GetClassName(), typeof(String));
            info.AddValue("Message", _message, typeof(String));
            info.AddValue("Data", _data, typeof(IDictionary));
            info.AddValue("InnerException", _innerException, typeof(Exception));
            info.AddValue("HelpURL", _helpURL, typeof(String));
            info.AddValue("StackTraceString", tempStackTraceString, typeof(String));
            info.AddValue("RemoteStackTraceString", _remoteStackTraceString, typeof(String));
            info.AddValue("RemoteStackIndex", _remoteStackIndex, typeof(Int32));
            info.AddValue("ExceptionMethod", GetExceptionMethodString(), typeof(String));
            info.AddValue("HResult", HResult);
            info.AddValue("Source", _source, typeof(String));
            
            // Serialize the Watson bucket details as well
            info.AddValue("WatsonBuckets", _watsonBuckets, typeof(byte[]));

#if FEATURE_SERIALIZATION
            if (_safeSerializationManager != null && _safeSerializationManager.IsActive)
            {
                info.AddValue("SafeSerializationManager", _safeSerializationManager, typeof(SafeSerializationManager));

                // User classes derived from Exception must have a valid _safeSerializationManager.
                // Exceptions defined in mscorlib don't use this field might not have it initalized (since they are 
                // often created in the VM with AllocateObject instead if the managed construtor)
                // If you are adding code to use a SafeSerializationManager from an mscorlib exception, update
                // this assert to ensure that it fails when that exception's _safeSerializationManager is NULL 
                Contract.Assert(((_safeSerializationManager != null) || (this.GetType().Assembly == typeof(object).Assembly)), 
                                "User defined exceptions must have a valid _safeSerializationManager");
            
                // Handle serializing any transparent or partial trust subclass data
                _safeSerializationManager.CompleteSerialization(this, info, context);
            }
#endif // FEATURE_SERIALIZATION
        }

        // This is used by remoting to preserve the server side stack trace
        // by appending it to the message ... before the exception is rethrown
        // at the client call site.
        internal Exception PrepForRemoting()
        {
            String tmp = null;

            if (_remoteStackIndex == 0)
            {
                tmp = Environment.NewLine+ "Server stack trace: " + Environment.NewLine
                    + StackTrace 
                    + Environment.NewLine + Environment.NewLine 
                    + "Exception rethrown at ["+_remoteStackIndex+"]: " + Environment.NewLine;
            }
            else
            {
                tmp = StackTrace 
                    + Environment.NewLine + Environment.NewLine 
                    + "Exception rethrown at ["+_remoteStackIndex+"]: " + Environment.NewLine;
            }

            _remoteStackTraceString = tmp;
            _remoteStackIndex++;

            return this;
        }

        // This method will clear the _stackTrace of the exception object upon deserialization
        // to ensure that references from another AD/Process dont get accidently used.
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _stackTrace = null;

            // We wont serialize or deserialize the IP for Watson bucketing since
            // we dont know where the deserialized object will be used in.
            // Using it across process or an AppDomain could be invalid and result
            // in AV in the runtime.
            //
            // Hence, we set it to zero when deserialization takes place. 
            _ipForWatsonBuckets = UIntPtr.Zero;

#if FEATURE_SERIALIZATION
            if (_safeSerializationManager == null)
            {
                _safeSerializationManager = new SafeSerializationManager();
            }
            else
            {
                _safeSerializationManager.CompleteDeserialization(this);
            }
#endif // FEATURE_SERIALIZATION
        }

        // This is used by the runtime when re-throwing a managed exception.  It will
        //  copy the stack trace to _remoteStackTraceString.
#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical] 
#endif
        internal void InternalPreserveStackTrace()
        {
            string tmpStackTraceString;

#if FEATURE_APPX
            if (AppDomain.IsAppXModel())
            {
                // Call our internal GetStackTrace in AppX so we can parse the result should
                // we need to strip file/line info from it to make it PII-free. Calling the
                // public and overridable StackTrace getter here was probably not intended.
                tmpStackTraceString = GetStackTrace(true);

                // Make sure that the _source field is initialized if Source is not overriden.
                // We want it to contain the original faulting point.
                string source = Source;
            }
            else
#else // FEATURE_APPX
#if FEATURE_CORESYSTEM
            // Preinitialize _source on CoreSystem as well. The legacy behavior is not ideal and
            // we keep it for back compat but we can afford to make the change on the Phone.
            string source = Source;
#endif // FEATURE_CORESYSTEM
#endif // FEATURE_APPX
            {
                // Call the StackTrace getter in classic for compat.
                tmpStackTraceString = StackTrace;
            }

            if (tmpStackTraceString != null && tmpStackTraceString.Length > 0)
            {
                _remoteStackTraceString = tmpStackTraceString + Environment.NewLine;
            }
            
            _stackTrace = null;
            _stackTraceString = null;
        }
        
#if FEATURE_EXCEPTIONDISPATCHINFO

        // This is the object against which a lock will be taken
        // when attempt to restore the EDI. Since its static, its possible
        // that unrelated exception object restorations could get blocked
        // for a small duration but that sounds reasonable considering
        // such scenarios are going to be extremely rare, where timing
        // matches precisely.
        [OptionalField]
        private static object s_EDILock = new object();

        internal UIntPtr IPForWatsonBuckets
        {
            get {
                return _ipForWatsonBuckets;
            }        
        }
    
        internal object WatsonBuckets
        {
            get 
            {
                return _watsonBuckets;
            }
        }

        internal string RemoteStackTrace
        {
            get
            {
                return _remoteStackTraceString;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void PrepareForForeignExceptionRaise();

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetStackTracesDeepCopy(Exception exception, out object currentStackTrace, out object dynamicMethodArray);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SaveStackTracesFromDeepCopy(Exception exception, object currentStackTrace, object dynamicMethodArray);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object CopyStackTrace(object currentStackTrace);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object CopyDynamicMethods(object currentDynamicMethods);

#if !FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern string StripFileInfo(string stackTrace, bool isRemoteStackTrace);
#endif // !FEATURE_CORECLR

        [SecuritySafeCritical]
        internal object DeepCopyStackTrace(object currentStackTrace)
        {
            if (currentStackTrace != null)
            {
                return CopyStackTrace(currentStackTrace);
            }
            else
            {
                return null;
            }
        }

        [SecuritySafeCritical]
        internal object DeepCopyDynamicMethods(object currentDynamicMethods)
        {
            if (currentDynamicMethods != null)
            {
                return CopyDynamicMethods(currentDynamicMethods);
            }
            else
            {
                return null;
            }
        }
        
        [SecuritySafeCritical]
        internal void GetStackTracesDeepCopy(out object currentStackTrace, out object dynamicMethodArray)
        {
            GetStackTracesDeepCopy(this, out currentStackTrace, out dynamicMethodArray);
        }

        // This is invoked by ExceptionDispatchInfo.Throw to restore the exception stack trace, corresponding to the original throw of the
        // exception, just before the exception is "rethrown".
        [SecuritySafeCritical]
        internal void RestoreExceptionDispatchInfo(System.Runtime.ExceptionServices.ExceptionDispatchInfo exceptionDispatchInfo)
        {
            bool fCanProcessException = !(IsImmutableAgileException(this));
            // Restore only for non-preallocated exceptions
            if (fCanProcessException)
            {
                // Take a lock to ensure only one thread can restore the details
                // at a time against this exception object that could have
                // multiple ExceptionDispatchInfo instances associated with it.
                //
                // We do this inside a finally clause to ensure ThreadAbort cannot
                // be injected while we have taken the lock. This is to prevent
                // unrelated exception restorations from getting blocked due to TAE.
                try{}
                finally
                {
                    // When restoring back the fields, we again create a copy and set reference to them
                    // in the exception object. This will ensure that when this exception is thrown and these
                    // fields are modified, then EDI's references remain intact.
                    //
                    // Since deep copying can throw on OOM, try to get the copies
                    // outside the lock.
                    object _stackTraceCopy = (exceptionDispatchInfo.BinaryStackTraceArray == null)?null:DeepCopyStackTrace(exceptionDispatchInfo.BinaryStackTraceArray);
                    object _dynamicMethodsCopy = (exceptionDispatchInfo.DynamicMethodArray == null)?null:DeepCopyDynamicMethods(exceptionDispatchInfo.DynamicMethodArray);
                    
                    // Finally, restore the information. 
                    //
                    // Since EDI can be created at various points during exception dispatch (e.g. at various frames on the stack) for the same exception instance,
                    // they can have different data to be restored. Thus, to ensure atomicity of restoration from each EDI, perform the restore under a lock.
                    lock(Exception.s_EDILock)
                    {
                        _watsonBuckets = exceptionDispatchInfo.WatsonBuckets;
                        _ipForWatsonBuckets = exceptionDispatchInfo.IPForWatsonBuckets;
                        _remoteStackTraceString = exceptionDispatchInfo.RemoteStackTrace;
                        SaveStackTracesFromDeepCopy(this, _stackTraceCopy, _dynamicMethodsCopy);
                    }
                    _stackTraceString = null;

                    // Marks the TES state to indicate we have restored foreign exception
                    // dispatch information.
                    Exception.PrepareForForeignExceptionRaise();
                }
            }
        }
#endif // FEATURE_EXCEPTIONDISPATCHINFO

        private String _className;  //Needed for serialization.  
        private MethodBase _exceptionMethod;  //Needed for serialization.  
        private String _exceptionMethodString; //Needed for serialization. 
        internal String _message;
        private IDictionary _data;
        private Exception _innerException;
        private String _helpURL;
        private Object _stackTrace;
        [OptionalField] // This isnt present in pre-V4 exception objects that would be serialized.
        private Object _watsonBuckets;
        private String _stackTraceString; //Needed for serialization.  
        private String _remoteStackTraceString;
        private int _remoteStackIndex;
#pragma warning disable 414  // Field is not used from managed.        
        // _dynamicMethods is an array of System.Resolver objects, used to keep
        // DynamicMethodDescs alive for the lifetime of the exception. We do this because
        // the _stackTrace field holds MethodDescs, and a DynamicMethodDesc can be destroyed
        // unless a System.Resolver object roots it.
        private Object _dynamicMethods; 
#pragma warning restore 414

        // @MANAGED: HResult is used from within the EE!  Rename with care - check VM directory
        internal int _HResult;     // HResult

        public int HResult
        {
            get
            {
                return _HResult;
            }
            protected set
            {
                _HResult = value;
            }
        }
        
        private String _source;         // Mainly used by VB. 
        // WARNING: Don't delete/rename _xptrs and _xcode - used by functions
        // on Marshal class.  Native functions are in COMUtilNative.cpp & AppDomain
        private IntPtr _xptrs;             // Internal EE stuff 
#pragma warning disable 414  // Field is not used from managed.
        private int _xcode;             // Internal EE stuff 
#pragma warning restore 414
        [OptionalField]
        private UIntPtr _ipForWatsonBuckets; // Used to persist the IP for Watson Bucketing

#if FEATURE_SERIALIZATION
        [OptionalField(VersionAdded = 4)]
        private SafeSerializationManager _safeSerializationManager;
#endif // FEATURE_SERIALIZATION

        // See src\inc\corexcep.h's EXCEPTION_COMPLUS definition:
        private const int _COMPlusExceptionCode = unchecked((int)0xe0434352);   // Win32 exception code for COM+ exceptions

        // InternalToString is called by the runtime to get the exception text 
        // and create a corresponding CrossAppDomainMarshaledException
        [System.Security.SecurityCritical]  // auto-generated
        internal virtual String InternalToString()
        {
            try 
            {
#pragma warning disable 618
                SecurityPermission sp= new SecurityPermission(SecurityPermissionFlag.ControlEvidence | SecurityPermissionFlag.ControlPolicy);
#pragma warning restore 618
                sp.Assert();
            }
            catch  
            {
                //under normal conditions there should be no exceptions
                //however if something wrong happens we still can call the usual ToString
            }

            // Get the current stack trace string. 
            return ToString(true, true);
        }
        
        // this method is required so Object.GetType is not made virtual by the compiler
        // _Exception.GetType()
        public new Type GetType()
        {
            return base.GetType();
        }

        internal bool IsTransient
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return nIsTransient(_HResult);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern static bool nIsTransient(int hr);


        // This piece of infrastructure exists to help avoid deadlocks 
        // between parts of mscorlib that might throw an exception while 
        // holding a lock that are also used by mscorlib's ResourceManager
        // instance.  As a special case of code that may throw while holding
        // a lock, we also need to fix our asynchronous exceptions to use
        // Win32 resources as well (assuming we ever call a managed 
        // constructor on instances of them).  We should grow this set of
        // exception messages as we discover problems, then move the resources
        // involved to native code.
        internal enum ExceptionMessageKind
        {
            ThreadAbort = 1,
            ThreadInterrupted = 2,
            OutOfMemory = 3
        }

        // See comment on ExceptionMessageKind
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static String GetMessageFromNativeResources(ExceptionMessageKind kind)
        {
            string retMesg = null;
            GetMessageFromNativeResources(kind, JitHelpers.GetStringHandleOnStack(ref retMesg));
            return retMesg;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void GetMessageFromNativeResources(ExceptionMessageKind kind, StringHandleOnStack retMesg);
    }



#if FEATURE_CORECLR

    //--------------------------------------------------------------------------
    // Telesto: Telesto doesn't support appdomain marshaling of objects so
    // managed exceptions that leak across appdomain boundaries are flatted to
    // its ToString() output and rethrown as an CrossAppDomainMarshaledException.
    // The Message field is set to the ToString() output of the original exception.
    //--------------------------------------------------------------------------

#if FEATURE_SERIALIZATION
    [Serializable]
#endif
    internal sealed class CrossAppDomainMarshaledException : SystemException 
    {
        public CrossAppDomainMarshaledException(String message, int errorCode) 
            : base(message) 
        {
            SetErrorCode(errorCode);
        }

        // Normally, only Telesto's UEF will see these exceptions.
        // This override prints out the original Exception's ToString()
        // output and hides the fact that it is wrapped inside another excepton.
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#endif
        internal override String InternalToString()
        {
            return Message;
        }
    
    }
#endif


}

