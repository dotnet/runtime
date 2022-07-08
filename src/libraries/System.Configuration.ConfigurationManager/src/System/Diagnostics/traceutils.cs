//------------------------------------------------------------------------------
// <copyright file="TypedElement.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System.Configuration;
using System;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Versioning;

namespace System.Diagnostics {
    internal static class TraceUtils {

        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        internal static object GetRuntimeObject(string className, Type baseType, string initializeData) {
            Object newObject = null;
            Type objectType = null;

            if (className.Length == 0) {
                throw new ConfigurationErrorsException(SR.GetString(SR.EmptyTypeName_NotAllowed));
            }
            
            objectType = Type.GetType(className);

            if (objectType == null) {
                throw new ConfigurationErrorsException(SR.GetString(SR.Could_not_find_type, className));
            }

            if (!baseType.IsAssignableFrom(objectType))
                throw new ConfigurationErrorsException(SR.GetString(SR.Incorrect_base_type, className, baseType.FullName));
            
            Exception innerException = null;
            try {
                if (String.IsNullOrEmpty(initializeData)) {
                    if (IsOwnedTL(objectType))
                        throw new ConfigurationErrorsException(SR.GetString(SR.TL_InitializeData_NotSpecified));

                    // create an object with parameterless constructor
                    ConstructorInfo ctorInfo = objectType.GetConstructor(new Type[] {});
                    if (ctorInfo == null)
                        throw new ConfigurationErrorsException(SR.GetString(SR.Could_not_get_constructor, className));
                    newObject = SecurityUtils.ConstructorInfoInvoke(ctorInfo, new object[] { });
                }
                else {
                    // create an object with a one-string constructor
                    // first look for a string constructor
                    ConstructorInfo ctorInfo = objectType.GetConstructor(new Type[] { typeof(string) });
                    if (ctorInfo != null) {
                        // Special case to enable specifying relative path to trace file from config for 
                        // our own TextWriterTraceListener derivatives. We will prepend it with fullpath  
                        // prefix from config file location
                        if (IsOwnedTextWriterTL(objectType)) {
                            if ((initializeData[0] != Path.DirectorySeparatorChar) && (initializeData[0] != Path.AltDirectorySeparatorChar) && !Path.IsPathRooted(initializeData)) {
                                string filePath = DiagnosticsConfiguration.ConfigFilePath;

                                if (!String.IsNullOrEmpty(filePath)) {
                                    string dirPath = Path.GetDirectoryName(filePath);

                                    if (dirPath != null) 
                                        initializeData = Path.Combine(dirPath, initializeData);
                                }
                            }
                        }
                        newObject = SecurityUtils.ConstructorInfoInvoke(ctorInfo, new object[] { initializeData });
                    }
                    else {
                        // now look for another 1 param constructor.
                        ConstructorInfo[] ctorInfos = objectType.GetConstructors();
                        if (ctorInfos == null)
                            throw new ConfigurationErrorsException(SR.GetString(SR.Could_not_get_constructor, className));
                        for (int i=0; i<ctorInfos.Length; i++) {
                            ParameterInfo[] ctorparams = ctorInfos[i].GetParameters();
                            if (ctorparams.Length == 1) {
                                Type paramtype = ctorparams[0].ParameterType;
                                try {
                                    object convertedInitializeData = ConvertToBaseTypeOrEnum(initializeData, paramtype);
                                    newObject = SecurityUtils.ConstructorInfoInvoke(ctorInfos[i], new object[] { convertedInitializeData });
                                    break;
                                }
                                catch(TargetInvocationException tiexc) {
                                    Debug.Assert(tiexc.InnerException != null, "ill-formed TargetInvocationException!");
                                    innerException = tiexc.InnerException;
                                }
                                catch (Exception e) {
                                    innerException = e;
                                    // ignore exceptions for now.  If we don't have a newObject at the end, then we'll throw.
                                }
                            }
                        }
                    }
                }
            }
            catch(TargetInvocationException tiexc) {
                Debug.Assert(tiexc.InnerException != null, "ill-formed TargetInvocationException!");
                innerException = tiexc.InnerException;
            }

            if (newObject == null) {
                if (innerException != null)
                    throw new ConfigurationErrorsException(SR.GetString(SR.Could_not_create_type_instance, className), innerException);
                else
                    throw new ConfigurationErrorsException(SR.GetString(SR.Could_not_create_type_instance, className));
            }

            return newObject;
        }

        // Our own tracelisteners that needs extra config validation 
        internal static bool IsOwnedTL(Type type) {
            return (typeof(EventLogTraceListener) == type  
                    || IsOwnedTextWriterTL(type)); 
        }
        internal static bool IsOwnedTextWriterTL(Type type) {
            return (typeof(XmlWriterTraceListener) == type)  
                    || (typeof(DelimitedListTraceListener) == type)
                    || (typeof(TextWriterTraceListener) == type); 
        }
        
        private static object ConvertToBaseTypeOrEnum(string value, Type type) {
            if (type.IsEnum)
                return Enum.Parse(type, value, false);
            else
                return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        internal static void VerifyAttributes(IDictionary attributes, String[] supportedAttributes, object parent) {
            foreach (string key in attributes.Keys) {
                bool found = false;
                if (supportedAttributes != null) {
                    for (int i=0; i<supportedAttributes.Length; i++) {
                        if (supportedAttributes[i] == key)
                            found = true;
                    }
                }
                if (!found)
                    throw new ConfigurationErrorsException(SR.GetString(SR.AttributeNotSupported, key, parent.GetType().FullName));
            }
        }
        
    }
}

