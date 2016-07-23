// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// ReflectionTypeLoadException is thrown when multiple TypeLoadExceptions may occur.  
// 
//  Specifically, when you call Module.GetTypes() this causes multiple class loads to occur.
//  If there are failures, we continue to load classes and build an array of the successfully
//  loaded classes.  We also build an array of the errors that occur.  Then we throw this exception
//  which exposes both the array of classes and the array of TypeLoadExceptions. 
//
// 
// 
//
namespace System.Reflection {
    
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ReflectionTypeLoadException : SystemException, ISerializable {
        private Type[] _classes;
        private Exception[] _exceptions;

        // private constructor.  This is not called.
        private ReflectionTypeLoadException()
            : base(Environment.GetResourceString("ReflectionTypeLoad_LoadFailed")) {
            SetErrorCode(__HResults.COR_E_REFLECTIONTYPELOAD);
        }

        // private constructor.  This is called from inside the runtime.
        private ReflectionTypeLoadException(String message) : base(message) {
            SetErrorCode(__HResults.COR_E_REFLECTIONTYPELOAD);
        }

        public ReflectionTypeLoadException(Type[] classes, Exception[] exceptions) : base(null)
        {
            _classes = classes;
            _exceptions = exceptions;
            SetErrorCode(__HResults.COR_E_REFLECTIONTYPELOAD);
        }

        public ReflectionTypeLoadException(Type[] classes, Exception[] exceptions, String message) : base(message)
        {
            _classes = classes;
            _exceptions = exceptions;
            SetErrorCode(__HResults.COR_E_REFLECTIONTYPELOAD);
        }

        internal ReflectionTypeLoadException(SerializationInfo info, StreamingContext context) : base (info, context) {
            _classes = (Type[])(info.GetValue("Types", typeof(Type[])));
            _exceptions = (Exception[])(info.GetValue("Exceptions", typeof(Exception[])));
        }
    
        public Type[] Types {
            get {return _classes;}
        }
        
        public Exception[] LoaderExceptions {
            get {return _exceptions;}
        }    

        [System.Security.SecurityCritical]  // auto-generated_required
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info==null) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();
            base.GetObjectData(info, context);
            info.AddValue("Types", _classes, typeof(Type[]));
            info.AddValue("Exceptions", _exceptions, typeof(Exception[]));
        }

    }
}
