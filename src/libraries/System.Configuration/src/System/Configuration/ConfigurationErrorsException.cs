// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Configuration.Internal;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security;
using System.Xml;

namespace System.Configuration
{
    [Serializable]
    public class ConfigurationErrorsException : ConfigurationException
    {
        // Constants
        private const string HttpPrefix = "http:";
        private const string SerializationParamFilename = "firstFilename";
        private const string SerializationParamLine = "firstLine";
        private const string SerializationParamErrorCount = "count";
        private const string SerializationParamErrorData = "_errors";
        private const string SerializationParamErrorType = "_errors_type";

        private readonly ConfigurationException[] _errors;

        private string _firstFilename;
        private int _firstLine;

        // The ConfigurationException class is obsolete, but we still need to derive from it and call the base ctor, so we
        // just disable the obsoletion warning.
#pragma warning disable 0618
        public ConfigurationErrorsException(string message, Exception inner, string filename, int line)
            : base(message, inner)
        {
#pragma warning restore 0618
            Init(filename, line);
        }

        public ConfigurationErrorsException() :
            this(null, null, null, 0)
        { }

        public ConfigurationErrorsException(string message) :
            this(message, null, null, 0)
        { }

        public ConfigurationErrorsException(string message, Exception inner) :
            this(message, inner, null, 0)
        { }

        public ConfigurationErrorsException(string message, string filename, int line) :
            this(message, null, filename, line)
        { }

        public ConfigurationErrorsException(string message, XmlNode node) :
            this(message, null, GetUnsafeFilename(node), GetLineNumber(node))
        { }

        public ConfigurationErrorsException(string message, Exception inner, XmlNode node) :
            this(message, inner, GetUnsafeFilename(node), GetLineNumber(node))
        { }

        public ConfigurationErrorsException(string message, XmlReader reader) :
            this(message, null, GetUnsafeFilename(reader), GetLineNumber(reader))
        { }

        public ConfigurationErrorsException(string message, Exception inner, XmlReader reader) :
            this(message, inner, GetUnsafeFilename(reader), GetLineNumber(reader))
        { }

        internal ConfigurationErrorsException(string message, IConfigErrorInfo errorInfo) :
            this(message, null, GetUnsafeConfigErrorInfoFilename(errorInfo), GetConfigErrorInfoLineNumber(errorInfo))
        { }

        internal ConfigurationErrorsException(string message, Exception inner, IConfigErrorInfo errorInfo) :
            this(message, inner, GetUnsafeConfigErrorInfoFilename(errorInfo), GetConfigErrorInfoLineNumber(errorInfo))
        { }

        internal ConfigurationErrorsException(ConfigurationException e) :
            this(GetBareMessage(e), GetInnerException(e), GetUnsafeFilename(e), GetLineNumber(e))
        { }


        [ResourceExposure(ResourceScope.None)]
        internal ConfigurationErrorsException(ICollection<ConfigurationException> coll) :
            this(GetFirstException(coll))
        {
            if (coll.Count > 1)
            {
                _errors = new ConfigurationException[coll.Count];
                coll.CopyTo(_errors, 0);
            }
        }

        internal ConfigurationErrorsException(ArrayList coll) :
            this((ConfigurationException)(coll.Count > 0 ? coll[0] : null))
        {
            if (coll.Count <= 1) return;
            _errors = new ConfigurationException[coll.Count];
            coll.CopyTo(_errors, 0);
        }


        // Serialization methods
        protected ConfigurationErrorsException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
            int firstLine;
            int count;

            // Retrieve out members
            string firstFilename = info.GetString(SerializationParamFilename);
            firstLine = info.GetInt32(SerializationParamLine);

            Init(firstFilename, firstLine);

            // Retrieve errors for _errors object
            count = info.GetInt32(SerializationParamErrorCount);

            if (count == 0) return;
            _errors = new ConfigurationException[count];

            for (int i = 0; i < count; i++)
            {
                string numPrefix = i.ToString(CultureInfo.InvariantCulture);

                string currentType = info.GetString(numPrefix + SerializationParamErrorType);
                Type currentExceptionType = Type.GetType(currentType, true);

                // Only allow our exception types
                if ((currentExceptionType != typeof(ConfigurationException)) &&
                    (currentExceptionType != typeof(ConfigurationErrorsException)))
                    throw ExceptionUtil.UnexpectedError("ConfigurationErrorsException");

                _errors[i] = (ConfigurationException)
                    info.GetValue(numPrefix + SerializationParamErrorData,
                        currentExceptionType);
            }
        }

        // The message includes the file/line number information.  
        // To get the message without the extra information, use BareMessage.
        public override string Message
        {
            get
            {
                string file = Filename;
                if (!string.IsNullOrEmpty(file))
                {
                    return Line != 0
                        ? BareMessage + " (" + file + " line " + Line.ToString(CultureInfo.CurrentCulture) + ")"
                        : BareMessage + " (" + file + ")";
                }

                return Line != 0
                    ? BareMessage + " (line " + Line.ToString("G", CultureInfo.CurrentCulture) + ")"
                    : BareMessage;
            }
        }

        public override string Filename => SafeFilename(_firstFilename);

        public override int Line => _firstLine;

        public ICollection Errors
        {
            get
            {
                if (_errors != null) return _errors;

                ConfigurationErrorsException e = new ConfigurationErrorsException(BareMessage, InnerException,
                    _firstFilename, _firstLine);

                ConfigurationException[] a = { e };
                return a;
            }
        }

        internal ICollection<ConfigurationException> ErrorsGeneric => (ICollection<ConfigurationException>)Errors;

        private void Init(string filename, int line)
        {
            HResult = HResults.Configuration;

            // BaseConfigurationRecord.cs uses -1 as uninitialized line number.
            if (line == -1) line = 0;

            _firstFilename = filename;
            _firstLine = line;
        }

        private static ConfigurationException GetFirstException(ICollection<ConfigurationException> coll)
        {
            foreach (ConfigurationException e in coll) return e;

            return null;
        }

        private static string GetBareMessage(ConfigurationException e)
        {
            return e?.BareMessage;
        }

        private static Exception GetInnerException(ConfigurationException e)
        {
            return e?.InnerException;
        }

        // We assert PathDiscovery so that we get the full filename when calling ConfigurationException.Filename
        private static string GetUnsafeFilename(ConfigurationException e)
        {
            return e?.Filename;
        }

        private static int GetLineNumber(ConfigurationException e)
        {
            return e?.Line ?? 0;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            int subErrors = 0;

            // call base implementation
            base.GetObjectData(info, context);

            // Serialize our members
            info.AddValue(SerializationParamFilename, Filename);
            info.AddValue(SerializationParamLine, Line);

            // Serialize rest of errors, along with count
            // (since first error duplicates this error, only worry if
            //  there is more than one)
            if ((_errors != null) &&
                (_errors.Length > 1))
            {
                subErrors = _errors.Length;

                for (int i = 0; i < _errors.Length; i++)
                {
                    string numPrefix = i.ToString(CultureInfo.InvariantCulture);

                    info.AddValue(numPrefix + SerializationParamErrorData,
                        _errors[i]);
                    info.AddValue(numPrefix + SerializationParamErrorType,
                        _errors[i].GetType());
                }
            }

            info.AddValue(SerializationParamErrorCount, subErrors);
        }

        // 
        // Get file and linenumber from an XML Node in a DOM
        //
        public static int GetLineNumber(XmlNode node)
        {
            return GetConfigErrorInfoLineNumber(node as IConfigErrorInfo);
        }

        public static string GetFilename(XmlNode node)
        {
            return SafeFilename(GetUnsafeFilename(node));
        }

        private static string GetUnsafeFilename(XmlNode node)
        {
            return GetUnsafeConfigErrorInfoFilename(node as IConfigErrorInfo);
        }

        // Get file and linenumber from an XML Reader
        public static int GetLineNumber(XmlReader reader)
        {
            return GetConfigErrorInfoLineNumber(reader as IConfigErrorInfo);
        }

        public static string GetFilename(XmlReader reader)
        {
            return SafeFilename(GetUnsafeFilename(reader));
        }

        private static string GetUnsafeFilename(XmlReader reader)
        {
            return GetUnsafeConfigErrorInfoFilename(reader as IConfigErrorInfo);
        }

        // Get file and linenumber from an IConfigErrorInfo
        private static int GetConfigErrorInfoLineNumber(IConfigErrorInfo errorInfo)
        {
            return errorInfo?.LineNumber ?? 0;
        }

        private static string GetUnsafeConfigErrorInfoFilename(IConfigErrorInfo errorInfo)
        {
            return errorInfo?.Filename;
        }

        private static string ExtractFileNameWithAssert(string filename)
        {
            // This method can throw; callers should wrap in try / catch.
            string fullPath = Path.GetFullPath(filename);
            return Path.GetFileName(fullPath);
        }

        // Internal Helper to strip a full path to just filename.ext when caller 
        // does not have path discovery to the path (used for sane error handling).
        internal static string SafeFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return filename;

            // configuration file can be an http URL in IE
            if (StringUtil.StartsWithOrdinalIgnoreCase(filename, HttpPrefix)) return filename;

            // If it is a relative path, return it as is. 
            // This could happen if the exception was constructed from the serialization constructor,
            // and the caller did not have PathDiscoveryPermission for the file.
            try
            {
                if (!Path.IsPathRooted(filename)) return filename;
            }
            catch
            {
                return null;
            }

            try
            {
                // Confirm that it is a full path.
                // GetFullPath will also Demand PathDiscovery for the resulting path
                Path.GetFullPath(filename);
            }
            catch (SecurityException)
            {
                // Get just the name of the file without the directory part.
                try
                {
                    filename = ExtractFileNameWithAssert(filename);
                }
                catch
                {
                    filename = null;
                }
            }
            catch
            {
                filename = null;
            }

            return filename;
        }

        // Internal Helper to always strip a full path to just filename.ext.
        internal static string AlwaysSafeFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return filename;

            // configuration file can be an http URL in IE
            if (StringUtil.StartsWithOrdinalIgnoreCase(filename, HttpPrefix)) return filename;

            // If it is a relative path, return it as is. 
            // This could happen if the exception was constructed from the serialization constructor,
            // and the caller did not have PathDiscoveryPermission for the file.
            try
            {
                if (!Path.IsPathRooted(filename)) return filename;
            }
            catch
            {
                return null;
            }

            // Get just the name of the file without the directory part.
            try
            {
                filename = ExtractFileNameWithAssert(filename);
            }
            catch
            {
                filename = null;
            }

            return filename;
        }
    }
}