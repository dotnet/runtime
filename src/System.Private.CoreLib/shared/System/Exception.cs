// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class Exception : ISerializable
    {
        public Exception()
        {
            _HResult = HResults.COR_E_EXCEPTION;
        }

        public Exception(string? message)
            : this()
        {
            _message = message;
        }

        // Creates a new Exception.  All derived classes should 
        // provide this constructor.
        // Note: the stack trace is not started until the exception 
        // is thrown
        // 
        public Exception(string? message, Exception? innerException)
            : this()
        {
            _message = message;
            _innerException = innerException;
        }

        protected Exception(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            _message = info.GetString("Message"); // Do not rename (binary serialization)
            _data = (IDictionary?)(info.GetValueNoThrow("Data", typeof(IDictionary))); // Do not rename (binary serialization)
            _innerException = (Exception?)(info.GetValue("InnerException", typeof(Exception))); // Do not rename (binary serialization)
            _helpURL = info.GetString("HelpURL"); // Do not rename (binary serialization)
            _stackTraceString = info.GetString("StackTraceString"); // Do not rename (binary serialization)
            _HResult = info.GetInt32("HResult"); // Do not rename (binary serialization)
            _source = info.GetString("Source"); // Do not rename (binary serialization)

            RestoreRemoteStackTrace(info, context);
        }

        public virtual string Message
        {
            get
            {
                return _message ?? SR.Format(SR.Exception_WasThrown, GetClassName());
            }
        }

        public virtual IDictionary Data
        {
            get
            {
                return _data ?? (_data = CreateDataContainer());
            }
        }

        private string GetClassName() => GetType().ToString();

        // Retrieves the lowest exception (inner most) for the given Exception.
        // This will traverse exceptions using the innerException property.
        public virtual Exception GetBaseException()
        {
            Exception? inner = InnerException;
            Exception back = this;

            while (inner != null)
            {
                back = inner;
                inner = inner.InnerException;
            }

            return back;
        }

        public Exception? InnerException => _innerException;

        // Sets the help link for this exception.
        // This should be in a URL/URN form, such as:
        // "file:///C:/Applications/Bazzal/help.html#ErrorNum42"
        public virtual string? HelpLink
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

        public virtual string? Source
        {
            get
            {
                return _source ?? (_source = CreateSourceName());
            }
            set
            {
                _source = value;
            }
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
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
            info.AddValue("StackTraceString", SerializationStackTraceString, typeof(string)); // Do not rename (binary serialization)
            info.AddValue("RemoteStackTraceString", SerializationRemoteStackTraceString, typeof(string)); // Do not rename (binary serialization)
            info.AddValue("RemoteStackIndex", 0, typeof(int)); // Do not rename (binary serialization)
            info.AddValue("ExceptionMethod", null, typeof(string)); // Do not rename (binary serialization)
            info.AddValue("HResult", _HResult); // Do not rename (binary serialization)
            info.AddValue("Source", _source, typeof(string)); // Do not rename (binary serialization)
            info.AddValue("WatsonBuckets", SerializationWatsonBuckets, typeof(byte[])); // Do not rename (binary serialization)
        }

        public override string ToString()
        {
            string s = GetClassName();

            string? message = Message;
            if (!string.IsNullOrEmpty(message))
            {
                s += ": " + message;
            }

            if (_innerException != null)
            {
                s = s + " ---> " + _innerException.ToString() + Environment.NewLine +
                "   " + SR.Exception_EndOfInnerExceptionStack;
            }

            string? stackTrace = GetStackTrace(needFileInfo: true);
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

        // this method is required so Object.GetType is not made virtual by the compiler
        // _Exception.GetType()
        public new Type GetType() => base.GetType();

        partial void RestoreRemoteStackTrace(SerializationInfo info, StreamingContext context);
    }
}
