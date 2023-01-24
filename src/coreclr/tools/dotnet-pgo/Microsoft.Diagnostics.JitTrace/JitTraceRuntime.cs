// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.JitTrace
{
    public static class JitTraceRuntime
    {
        /// <summary>
        /// When a jittrace entry caused a failure, it will call this event with the
        /// line in the jittrace file that triggered the failure. "" will be passed for stream reading failures.
        /// </summary>
        public static event Action<string> LogFailure;

        private static void LogOnFailure(string failure)
        {
            var log = LogFailure;
            if (log != null)
            {
                log(failure);
            }
        }

        /// <summary>
        /// Prepare all the methods specified in a .jittrace file for execution
        /// </summary>
        /// <param name="fileName">Filename of .jittrace file</param>
        /// <param name="successfulPrepares">count of successful prepare operations. May exceed the could of lines in the jittrace file due to fuzzy matching</param>
        /// <param name="failedPrepares">count of failed prepare operations. May exceed the could of lines in the jittrace file due to fuzzy matching</param>
        public static void Prepare(FileInfo fileName, out int successfulPrepares, out int failedPrepares)
        {
            using (StreamReader sr = new StreamReader(fileName.FullName))
            {
                Prepare(sr, out successfulPrepares, out failedPrepares);
            }
        }

        private static string UnescapeStr(string input, string separator)
        {
            return input.Replace("\\s", separator).Replace("\\\\", "\\");
        }

        private static string[] SplitAndUnescape(string input, string separator, char[] separatorCharArray)
        {
            string[] returnValue = input.Split(separatorCharArray);
            for (int i = 0; i < returnValue.Length; i++)
            {
                returnValue[i] = UnescapeStr(returnValue[i], separator);
            }
            return returnValue;
        }

        /// <summary>
        /// Prepare all the methods specified string that matches the .jittrace file format
        /// for execution. Useful for embedding via data via resource.
        /// </summary>
        /// <param name="jittraceString">string with .jittrace data</param>
        /// <param name="successfulPrepares">count of successful prepare operations. May exceed the could of lines in the jittrace file due to fuzzy matching</param>
        /// <param name="failedPrepares">count of failed prepare operations. May exceed the could of lines in the jittrace file due to fuzzy matching</param>
        public static void Prepare(string jittraceString, out int successfulPrepares, out int failedPrepares)
        {
            MemoryStream strStream = new MemoryStream();
            using (var writer = new StreamWriter(strStream, encoding: null, bufferSize: -1, leaveOpen: true))
            {
                writer.Write(jittraceString);
            }

            strStream.Position = 0;
            Prepare(new StreamReader(strStream), out successfulPrepares, out failedPrepares);
        }

        /// <summary>
        /// Prepare all the methods specified Stream that matches the .jittrace file format
        /// for execution. Handles general purpose stream data.
        /// </summary>
        /// <param name="jittraceStream">Stream with .jittrace data</param>
        /// <param name="successfulPrepares">count of successful prepare operations. May exceed the could of lines in the jittrace file due to fuzzy matching</param>
        /// <param name="failedPrepares">count of failed prepare operations. May exceed the could of lines in the jittrace file due to fuzzy matching</param>
        public static void Prepare(StreamReader jittraceStream, out int successfulPrepares, out int failedPrepares)
        {
            const string outerCsvEscapeChar = "~";
            const string innerCsvEscapeChar = ":";
            char[] outerCsvEscapeCharArray = new char[] { '~' };
            char[] innerCsvEscapeCharArray = new char[] { ':' };
            successfulPrepares = 0;
            failedPrepares = 0;

            while (true)
            {
                string methodString = string.Empty;
                try
                {
                    methodString = jittraceStream.ReadLine();
                    if (methodString == null)
                    {
                        break;
                    }
                    if (methodString.Trim() == string.Empty)
                    {
                        break;
                    }

                    string[] methodStrComponents = SplitAndUnescape(methodString, outerCsvEscapeChar, outerCsvEscapeCharArray);

                    Type owningType = Type.GetType(methodStrComponents[1], false);

                    // owningType failed to load above. Skip rest of method discovery
                    if (owningType == null)
                    {
                        failedPrepares++;
                        LogOnFailure(methodString);
                        continue;
                    }

                    int signatureLen = int.Parse(methodStrComponents[2]);
                    string[] methodInstantiationArgComponents = SplitAndUnescape(methodStrComponents[3], innerCsvEscapeChar, innerCsvEscapeCharArray);
                    int genericMethodArgCount = int.Parse(methodInstantiationArgComponents[0]);
                    Type[] methodArgs = genericMethodArgCount != 0 ? new Type[genericMethodArgCount] : Type.EmptyTypes;
                    bool abortMethodDiscovery = false;
                    for (int iMethodArg = 0; iMethodArg < genericMethodArgCount; iMethodArg++)
                    {
                        Type methodArg = Type.GetType(methodInstantiationArgComponents[1 + iMethodArg], false);
                        methodArgs[iMethodArg] = methodArg;

                        // methodArg failed to load above. Skip rest of method discovery
                        if (methodArg == null)
                        {
                            abortMethodDiscovery = true;
                            break;
                        }
                    }

                    if (abortMethodDiscovery)
                    {
                        failedPrepares++;
                        LogOnFailure(methodString);
                        continue;
                    }

                    string methodName = methodStrComponents[4];

                    // Now all data is parsed
                    // Find method
                    IEnumerable<RuntimeMethodHandle> membersFound;

                    BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                    if (methodName == ".ctor")
                    {
                        if (genericMethodArgCount != 0)
                        {
                            // Ctors with generic args don't make sense
                            failedPrepares++;
                            LogOnFailure(methodString);
                            continue;
                        }
                        membersFound = CtorMethodsThatMatch();
                        IEnumerable<RuntimeMethodHandle> CtorMethodsThatMatch()
                        {
                            ConstructorInfo[] constructors = owningType.GetConstructors(bindingFlags);
                            foreach (ConstructorInfo ci in constructors)
                            {
                                ConstructorInfo returnConstructorInfo = null;

                                try
                                {
                                    if (ci.GetParameters().Length == signatureLen)
                                    {
                                        returnConstructorInfo = ci;
                                    }
                                }
                                catch
                                {
                                }
                                if (returnConstructorInfo != null)
                                {
                                    yield return returnConstructorInfo.MethodHandle;
                                }
                            }
                        }
                    }
                    else if (methodName == ".cctor")
                    {
                        MemberInfo mi = owningType.TypeInitializer;
                        if (mi == null)
                        {
                            // This type no longer has a type initializer
                            failedPrepares++;
                            LogOnFailure(methodString);
                            continue;
                        }
                        membersFound = new RuntimeMethodHandle[] { owningType.TypeInitializer.MethodHandle };
                    }
                    else
                    {
                        membersFound = MethodsThatMatch();
                        IEnumerable<RuntimeMethodHandle> MethodsThatMatch()
                        {
                            MethodInfo[] methods = owningType.GetMethods(bindingFlags);
                            foreach (MethodInfo mi in methods)
                            {
                                MethodInfo returnMethodInfo = null;
                                try
                                {
                                    if (mi.Name != methodName)
                                    {
                                        continue;
                                    }

                                    if (mi.GetParameters().Length != signatureLen)
                                    {
                                        continue;
                                    }
                                    if (mi.GetGenericArguments().Length != genericMethodArgCount)
                                    {
                                        continue;
                                    }
                                    if (genericMethodArgCount != 0)
                                    {
                                        returnMethodInfo = mi.MakeGenericMethod(methodArgs);
                                    }
                                    else
                                    {
                                        returnMethodInfo = mi;
                                    }
                                }
                                catch
                                {
                                }

                                if (returnMethodInfo != null)
                                {
                                    yield return returnMethodInfo.MethodHandle;
                                }
                            }
                        }
                    }

                    bool foundAtLeastOneEntry = false;
                    foreach (RuntimeMethodHandle memberHandle in membersFound)
                    {
                        foundAtLeastOneEntry = true;
                        try
                        {
                            System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(memberHandle);
                            successfulPrepares++;
                        }
                        catch
                        {
                            failedPrepares++;
                            LogOnFailure(methodString);
                        }
                    }
                    if (!foundAtLeastOneEntry)
                    {
                        failedPrepares++;
                        LogOnFailure(methodString);
                    }
                }
                catch
                {
                    failedPrepares++;
                    LogOnFailure(methodString);
                }
            }
        }
    }
}
