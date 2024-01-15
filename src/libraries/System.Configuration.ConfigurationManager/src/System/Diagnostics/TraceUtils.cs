// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    internal static class TraceUtils
    {
        private const string SystemDiagnostics = "System.Diagnostics.";

        internal static object GetRuntimeObject(string className, Type baseType, string initializeData)
        {
            object newObject = null;
            Type objectType = null;

            if (className.Length == 0)
            {
                throw new ConfigurationErrorsException(SR.EmptyTypeName_NotAllowed);
            }

            if (className.StartsWith(SystemDiagnostics))
            {
                // Since the config file likely has just the FullName without the assembly name,
                // map the FullName to the built in types.
                objectType = MapToBuiltInTypes(className);
            }

            if (objectType == null)
            {
                objectType = Type.GetType(className);

                if (objectType == null)
                {
                    throw new ConfigurationErrorsException(SR.Format(SR.Could_not_find_type, className));
                }
            }

            if (!baseType.IsAssignableFrom(objectType))
                throw new ConfigurationErrorsException(SR.Format(SR.Incorrect_base_type, className, baseType.FullName));

            Exception innerException = null;
            try
            {
                if (string.IsNullOrEmpty(initializeData))
                {
                    if (IsOwnedTL(objectType))
                        throw new ConfigurationErrorsException(SR.TL_InitializeData_NotSpecified);

                    // Create an object with parameterless constructor.
                    ConstructorInfo ctorInfo = objectType.GetConstructor(Array.Empty<Type>());
                    if (ctorInfo == null)
                        throw new ConfigurationErrorsException(SR.Format(SR.Could_not_get_constructor, className));
                    newObject = ctorInfo.Invoke(Array.Empty<object>());
                }
                else
                {
                    // Create an object with a one-string constructor.
                    // First look for a string constructor.
                    ConstructorInfo ctorInfo = objectType.GetConstructor(new Type[] { typeof(string) });
                    if (ctorInfo != null)
                    {
                        // Special case to enable specifying relative path to trace file from config for
                        // our own TextWriterTraceListener derivatives. We will prepend it with fullpath
                        // prefix from config file location.
                        if (IsOwnedTextWriterTL(objectType))
                        {
                            if ((initializeData[0] != Path.DirectorySeparatorChar) && (initializeData[0] != Path.AltDirectorySeparatorChar) && !Path.IsPathRooted(initializeData))
                            {
                                string filePath = DiagnosticsConfiguration.ConfigFilePath;

                                if (!string.IsNullOrEmpty(filePath))
                                {
                                    string dirPath = Path.GetDirectoryName(filePath);

                                    if (dirPath != null)
                                        initializeData = Path.Combine(dirPath, initializeData);
                                }
                            }
                        }
                        newObject = ctorInfo.Invoke(new object[] { initializeData });
                    }
                    else
                    {
                        // Now look for another 1 param constructor.
                        ConstructorInfo[] ctorInfos = objectType.GetConstructors();
                        if (ctorInfos == null)
                        {
                            throw new ConfigurationErrorsException(SR.Format(SR.Could_not_get_constructor, className));
                        }

                        for (int i = 0; i < ctorInfos.Length; i++)
                        {
                            ParameterInfo[] ctorparams = ctorInfos[i].GetParameters();
                            if (ctorparams.Length == 1)
                            {
                                Type paramtype = ctorparams[0].ParameterType;
                                try
                                {
                                    object convertedInitializeData = ConvertToBaseTypeOrEnum(initializeData, paramtype);
                                    newObject = ctorInfos[i].Invoke(new object[] { convertedInitializeData });
                                    break;
                                }
                                catch (TargetInvocationException tiexc)
                                {
                                    Debug.Assert(tiexc.InnerException != null, "ill-formed TargetInvocationException!");
                                    innerException = tiexc.InnerException;
                                }
                                catch (Exception e)
                                {
                                    innerException = e;
                                    // Ignore exceptions for now.  If we don't have a newObject at the end, then we'll throw.
                                }
                            }
                        }
                    }
                }
            }
            catch (TargetInvocationException tiexc)
            {
                Debug.Assert(tiexc.InnerException != null, "ill-formed TargetInvocationException!");
                innerException = tiexc.InnerException;
            }

            if (newObject == null)
            {
                if (innerException != null)
                {
                    throw new ConfigurationErrorsException(SR.Format(SR.Could_not_create_type_instance, className), innerException);
                }

                throw new ConfigurationErrorsException(SR.Format(SR.Could_not_create_type_instance, className));
            }

            return newObject;
        }

        private static Type MapToBuiltInTypes(string className)
        {
            string name = className.Substring(SystemDiagnostics.Length);
            switch (name)
            {
                // Types in System.Diagnostics.TraceSource.dll:
                case nameof(DefaultTraceListener):
                    return typeof(DefaultTraceListener);
                case nameof(SourceFilter):
                    return typeof(SourceFilter);
                case nameof(EventTypeFilter):
                    return typeof(EventTypeFilter);
                case nameof(BooleanSwitch):
                    return typeof(BooleanSwitch);
                case nameof(TraceSwitch):
                    return typeof(TraceSwitch);
                case nameof(SourceSwitch):
                    return typeof(SourceSwitch);

                // Types in System.Diagnostics.TextWriterTraceListener:
                case nameof(ConsoleTraceListener):
                    return typeof(ConsoleTraceListener);
                case nameof(DelimitedListTraceListener):
                    return typeof(DelimitedListTraceListener);
                case nameof(XmlWriterTraceListener):
                    return typeof(XmlWriterTraceListener);
                case nameof(TextWriterTraceListener):
                    return typeof(TextWriterTraceListener);

                // Types in System.Diagnostics.EventLog.dll:
                case nameof(EventLogTraceListener):
                    return typeof(EventLogTraceListener);

                default:
                    return null;
            }
        }

        // Our own tracelisteners that needs extra config validation.
        internal static bool IsOwnedTL(Type type)
        {
            return typeof(EventLogTraceListener) == type
                    || IsOwnedTextWriterTL(type);
        }

        internal static bool IsOwnedTextWriterTL(Type type)
        {
            return (typeof(XmlWriterTraceListener) == type)
                    || (typeof(DelimitedListTraceListener) == type)
                    || (typeof(TextWriterTraceListener) == type);
        }

        private static object ConvertToBaseTypeOrEnum(string value, Type type)
        {
            return type.IsEnum ?
                Enum.Parse(type, value, false) :
                Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        // Copy the StringDictionary to another StringDictionary.
        // This is not as efficient as directly setting the property, but it avoids having to expose a public setter on the property.
        internal static void CopyStringDictionary(StringDictionary source, StringDictionary dest)
        {
            dest.Clear();
            foreach (string key in source.Keys)
            {
                dest[key] = source[key];
            }
        }
    }
}
