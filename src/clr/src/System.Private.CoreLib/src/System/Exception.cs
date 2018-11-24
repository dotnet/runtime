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

namespace System
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Diagnostics;
    using System.Security;
    using System.IO;
    using System.Text;
    using System.Reflection;
    using System.Collections;
    using System.Globalization;

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class Exception : ISerializable
    {
        private void Init()
        {
            _message = null;
            _stackTrace = null;
            _dynamicMethods = null;
            HResult = HResults.COR_E_EXCEPTION;
            _xcode = _COMPlusExceptionCode;
            _xptrs = (IntPtr)0;

            // Initialize the WatsonBuckets to be null
            _watsonBuckets = null;

            // Initialize the watson bucketing IP
            _ipForWatsonBuckets = UIntPtr.Zero;
        }

        public Exception()
        {
            Init();
        }

        public Exception(string message)
        {
            Init();
            _message = message;
        }

        // Creates a new Exception.  All derived classes should 
        // provide this constructor.
        // Note: the stack trace is not started until the exception 
        // is thrown
        // 
        public Exception(string message, Exception innerException)
        {
            Init();
            _message = message;
            _innerException = innerException;
        }

        protected Exception(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            _className = info.GetString("ClassName"); // Do not rename (binary serialization)
            _message = info.GetString("Message"); // Do not rename (binary serialization)
            _data = (IDictionary)(info.GetValueNoThrow("Data", typeof(IDictionary))); // Do not rename (binary serialization)
            _innerException = (Exception)(info.GetValue("InnerException", typeof(Exception))); // Do not rename (binary serialization)
            _helpURL = info.GetString("HelpURL"); // Do not rename (binary serialization)
            _stackTraceString = info.GetString("StackTraceString"); // Do not rename (binary serialization)
            _remoteStackTraceString = info.GetString("RemoteStackTraceString"); // Do not rename (binary serialization)
            _remoteStackIndex = info.GetInt32("RemoteStackIndex"); // Do not rename (binary serialization)

            HResult = info.GetInt32("HResult"); // Do not rename (binary serialization)
            _source = info.GetString("Source"); // Do not rename (binary serialization)

            // Get the WatsonBuckets that were serialized - this is particularly
            // done to support exceptions going across AD transitions.
            // 
            // We use the no throw version since we could be deserializing a pre-V4
            // exception object that may not have this entry. In such a case, we would
            // get null.
            _watsonBuckets = (object)info.GetValueNoThrow("WatsonBuckets", typeof(byte[])); // Do not rename (binary serialization)


            if (_className == null || HResult == 0)
                throw new SerializationException(SR.Serialization_InsufficientState);

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


        public virtual string Message
        {
            get
            {
                if (_message == null)
                {
                    if (_className == null)
                    {
                        _className = GetClassName();
                    }
                    return SR.Format(SR.Exception_WasThrown, _className);
                }
                else
                {
                    return _message;
                }
            }
        }

        public virtual IDictionary Data
        {
            get
            {
                if (_data == null)
                    if (IsImmutableAgileException(this))
                        _data = new EmptyReadOnlyDictionaryInternal();
                    else
                        _data = new ListDictionaryInternal();

                return _data;
            }
        }

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

            while (inner != null)
            {
                back = inner;
                inner = inner.InnerException;
            }

            return back;
        }

        // Returns the inner exception contained in this exception
        // 
        public Exception InnerException
        {
            get { return _innerException; }
        }


        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static private extern IRuntimeMethodInfo GetMethodFromStackTrace(object stackTrace);

        private MethodBase GetExceptionMethodFromStackTrace()
        {
            IRuntimeMethodInfo method = GetMethodFromStackTrace(_stackTrace);

            // Under certain race conditions when exceptions are re-used, this can be null
            if (method == null)
                return null;

            return RuntimeType.GetMethodBase(method);
        }

        public MethodBase TargetSite
        {
            get
            {
                return GetTargetSiteInternal();
            }
        }


        // this function is provided as a private helper to avoid the security demand
        private MethodBase GetTargetSiteInternal()
        {
            if (_exceptionMethod != null)
            {
                return _exceptionMethod;
            }
            if (_stackTrace == null)
            {
                return null;
            }
            
            _exceptionMethod = GetExceptionMethodFromStackTrace();
            return _exceptionMethod;
        }

        // Returns the stack trace as a string.  If no stack trace is
        // available, null is returned.
        public virtual string StackTrace
        {
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
        private string GetStackTrace(bool needFileInfo)
        {
            string stackTraceString = _stackTraceString;
            string remoteStackTraceString = _remoteStackTraceString;

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
            string tempStackTraceString = Environment.GetStackTrace(this, needFileInfo);
            return remoteStackTraceString + tempStackTraceString;
        }

        // Sets the help link for this exception.
        // This should be in a URL/URN form, such as:
        // "file:///C:/Applications/Bazzal/help.html#ErrorNum42"
        // Changed to be a read-write String and not return an exception
        public virtual string HelpLink
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

        public virtual string Source
        {
            get
            {
                if (_source == null)
                {
                    StackTrace st = new StackTrace(this, true);
                    if (st.FrameCount > 0)
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
                                throw new ArgumentException(SR.Argument_MustBeRuntimeReflectionObject);
                        }

                        _source = rtModule.GetRuntimeAssembly().GetSimpleName();
                    }
                }

                return _source;
            }
            set { _source = value; }
        }

        public override string ToString()
        {
            return ToString(true, true);
        }

        private string ToString(bool needFileLineInfo, bool needMessage)
        {
            string message = (needMessage ? Message : null);
            string s;

            if (message == null || message.Length <= 0)
            {
                s = GetClassName();
            }
            else
            {
                s = GetClassName() + ": " + message;
            }

            if (_innerException != null)
            {
                s = s + " ---> " + _innerException.ToString(needFileLineInfo, needMessage) + Environment.NewLine +
                "   " + SR.Exception_EndOfInnerExceptionStack;
            }

            string stackTrace = GetStackTrace(needFileLineInfo);
            if (stackTrace != null)
            {
                s += Environment.NewLine + stackTrace;
            }

            return s;
        }

        protected event EventHandler<SafeSerializationEventArgs> SerializeObjectState
        {
            add { throw new PlatformNotSupportedException(SR.PlatformNotSupported_SecureBinarySerialization); }
            remove { throw new PlatformNotSupportedException(SR.PlatformNotSupported_SecureBinarySerialization); }
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            string tempStackTraceString = _stackTraceString;

            if (_stackTrace != null)
            {
                if (tempStackTraceString == null)
                {
                    tempStackTraceString = Environment.GetStackTrace(this, true);
                }
                if (_exceptionMethod == null)
                {
                    _exceptionMethod = GetExceptionMethodFromStackTrace();
                }
            }

            if (_source == null)
            {
                _source = Source; // Set the Source information correctly before serialization
            }

            info.AddValue("ClassName", GetClassName(), typeof(string)); // Do not rename (binary serialization)
            info.AddValue("Message", _message, typeof(string)); // Do not rename (binary serialization)
            info.AddValue("Data", _data, typeof(IDictionary)); // Do not rename (binary serialization)
            info.AddValue("InnerException", _innerException, typeof(Exception)); // Do not rename (binary serialization)
            info.AddValue("HelpURL", _helpURL, typeof(string)); // Do not rename (binary serialization)
            info.AddValue("StackTraceString", tempStackTraceString, typeof(string)); // Do not rename (binary serialization)
            info.AddValue("RemoteStackTraceString", _remoteStackTraceString, typeof(string)); // Do not rename (binary serialization)
            info.AddValue("RemoteStackIndex", _remoteStackIndex, typeof(int)); // Do not rename (binary serialization)
            info.AddValue("ExceptionMethod", null, typeof(string)); // Do not rename (binary serialization)
            info.AddValue("HResult", HResult); // Do not rename (binary serialization)
            info.AddValue("Source", _source, typeof(string)); // Do not rename (binary serialization)

            // Serialize the Watson bucket details as well
            info.AddValue("WatsonBuckets", _watsonBuckets, typeof(byte[])); // Do not rename (binary serialization)
        }

        // This method will clear the _stackTrace of the exception object upon deserialization
        // to ensure that references from another AD/Process dont get accidentally used.
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
        }

        // This is used by the runtime when re-throwing a managed exception.  It will
        //  copy the stack trace to _remoteStackTraceString.
        internal void InternalPreserveStackTrace()
        {
            string tmpStackTraceString;

#if FEATURE_APPX
            if (ApplicationModel.IsUap)
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
            // Preinitialize _source on CoreSystem as well. The legacy behavior is not ideal and
            // we keep it for back compat but we can afford to make the change on the Phone.
            string source = Source;
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
            get
            {
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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void PrepareForForeignExceptionRaise();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetStackTracesDeepCopy(Exception exception, out object currentStackTrace, out object dynamicMethodArray);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SaveStackTracesFromDeepCopy(Exception exception, object currentStackTrace, object dynamicMethodArray);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object CopyStackTrace(object currentStackTrace);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object CopyDynamicMethods(object currentDynamicMethods);

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

        internal void GetStackTracesDeepCopy(out object currentStackTrace, out object dynamicMethodArray)
        {
            GetStackTracesDeepCopy(this, out currentStackTrace, out dynamicMethodArray);
        }

        // This is invoked by ExceptionDispatchInfo.Throw to restore the exception stack trace, corresponding to the original throw of the
        // exception, just before the exception is "rethrown".
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
                try { }
                finally
                {
                    // When restoring back the fields, we again create a copy and set reference to them
                    // in the exception object. This will ensure that when this exception is thrown and these
                    // fields are modified, then EDI's references remain intact.
                    //
                    // Since deep copying can throw on OOM, try to get the copies
                    // outside the lock.
                    object _stackTraceCopy = (exceptionDispatchInfo.BinaryStackTraceArray == null) ? null : DeepCopyStackTrace(exceptionDispatchInfo.BinaryStackTraceArray);
                    object _dynamicMethodsCopy = (exceptionDispatchInfo.DynamicMethodArray == null) ? null : DeepCopyDynamicMethods(exceptionDispatchInfo.DynamicMethodArray);

                    // Finally, restore the information. 
                    //
                    // Since EDI can be created at various points during exception dispatch (e.g. at various frames on the stack) for the same exception instance,
                    // they can have different data to be restored. Thus, to ensure atomicity of restoration from each EDI, perform the restore under a lock.
                    lock (Exception.s_EDILock)
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

        private string _className;  //Needed for serialization.  
        private MethodBase _exceptionMethod;  //Needed for serialization.  
        internal string _message;
        private IDictionary _data;
        private Exception _innerException;
        private string _helpURL;
        private object _stackTrace;
        [OptionalField] // This isnt present in pre-V4 exception objects that would be serialized.
        private object _watsonBuckets;
        private string _stackTraceString; //Needed for serialization.  
        private string _remoteStackTraceString;
        private int _remoteStackIndex;
#pragma warning disable 414  // Field is not used from managed.        
        // _dynamicMethods is an array of System.Resolver objects, used to keep
        // DynamicMethodDescs alive for the lifetime of the exception. We do this because
        // the _stackTrace field holds MethodDescs, and a DynamicMethodDesc can be destroyed
        // unless a System.Resolver object roots it.
        private object _dynamicMethods;
#pragma warning restore 414

        // @MANAGED: HResult is used from within the EE!  Rename with care - check VM directory
        internal int _HResult;     // HResult

        public int HResult
        {
            get
            {
                return _HResult;
            }
            set
            {
                _HResult = value;
            }
        }

        private string _source;         // Mainly used by VB. 
        // WARNING: Don't delete/rename _xptrs and _xcode - used by functions
        // on Marshal class.  Native functions are in COMUtilNative.cpp & AppDomain
        private IntPtr _xptrs;             // Internal EE stuff 
#pragma warning disable 414  // Field is not used from managed.
        private int _xcode;             // Internal EE stuff 
#pragma warning restore 414
        [OptionalField]
        private UIntPtr _ipForWatsonBuckets; // Used to persist the IP for Watson Bucketing


        // See src\inc\corexcep.h's EXCEPTION_COMPLUS definition:
        private const int _COMPlusExceptionCode = unchecked((int)0xe0434352);   // Win32 exception code for COM+ exceptions

        // InternalToString is called by the runtime to get the exception text.
        internal string InternalToString()
        {
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
            get
            {
                return nIsTransient(_HResult);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool nIsTransient(int hr);

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
        internal static string GetMessageFromNativeResources(ExceptionMessageKind kind)
        {
            string retMesg = null;
            GetMessageFromNativeResources(kind, JitHelpers.GetStringHandleOnStack(ref retMesg));
            return retMesg;
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetMessageFromNativeResources(ExceptionMessageKind kind, StringHandleOnStack retMesg);
    }
}

