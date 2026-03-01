// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains code examples for XML documentation.
// The examples are referenced by the DynamicMethod class documentation using #region names.

#pragma warning disable CS8321 // Local function is declared but never used

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Reflection.Emit.Examples
{
    internal static class DynamicMethodExamples
    {
        private static void Examples()
        {
            // Note: 'hello' represents a DynamicMethod instance used in the examples below

            #region Name
            // Display the name specified when the dynamic method was created.
            // Note that the name can be blank.
            Console.WriteLine("\r\nName: {0}", hello.Name);
            #endregion

            #region DeclaringType
            // Display the declaring type, which is always null for dynamic
            // methods.
            if (hello.DeclaringType == null)
            {
                Console.WriteLine("\r\nDeclaringType is always null for dynamic methods.");
            }
            else
            {
                Console.WriteLine("DeclaringType: {0}", hello.DeclaringType);
            }
            #endregion

            #region ReflectedType
            // For dynamic methods, the reflected type is always null.
            if (hello.ReflectedType == null)
            {
                Console.WriteLine("\r\nReflectedType is null.");
            }
            else
            {
                Console.WriteLine("\r\nReflectedType: {0}", hello.ReflectedType);
            }
            #endregion

            #region Module
            // Display the module specified when the dynamic method was created.
            Console.WriteLine("\r\nModule: {0}", hello.Module);
            #endregion

            #region Attributes
            // Display MethodAttributes for the dynamic method, set when
            // the dynamic method was created.
            Console.WriteLine("\r\nMethod Attributes: {0}", hello.Attributes);
            #endregion

            #region CallingConvention
            // Display the calling convention of the dynamic method, set when the
            // dynamic method was created.
            Console.WriteLine("\r\nCalling convention: {0}", hello.CallingConvention);
            #endregion

            #region GetParameters
            // Display parameter information.
            ParameterInfo[] parameters = hello.GetParameters();
            Console.WriteLine("\r\nParameters: name, type, ParameterAttributes");
            foreach( ParameterInfo p in parameters )
            {
                Console.WriteLine("\t{0}, {1}, {2}",
                    p.Name, p.ParameterType, p.Attributes);
            }
            #endregion

            #region ReturnType
            // If the method has no return type, ReturnType is System.Void.
            Console.WriteLine("\r\nReturn type: {0}", hello.ReturnType);
            #endregion

            #region ReturnTypeCustomAttributes
            // ReturnTypeCustomAttributes returns an ICustomAttributeProvider
            // that can be used to enumerate the custom attributes of the
            // return value. At present, there is no way to set such custom
            // attributes, so the list is empty.
            if (hello.ReturnType == typeof(void))
            {
                Console.WriteLine("The method has no return type.");
            }
            else
            {
                ICustomAttributeProvider caProvider = hello.ReturnTypeCustomAttributes;
                object[] returnAttributes = caProvider.GetCustomAttributes(true);
                if (returnAttributes.Length == 0)
                {
                    Console.WriteLine("\r\nThe return type has no custom attributes.");
                }
                else
                {
                    Console.WriteLine("\r\nThe return type has the following custom attributes:");
                    foreach( object attr in returnAttributes )
                    {
                        Console.WriteLine("\t{0}", attr.ToString());
                    }
                }
            }
            #endregion

            #region DefineParameter
            // Add parameter information to the dynamic method. (This is not
            // necessary, but can be useful for debugging.) For each parameter,
            // identified by position, supply the parameter attributes and a
            // parameter name.
            hello.DefineParameter(1, ParameterAttributes.In, "message");
            hello.DefineParameter(2, ParameterAttributes.In, "valueToReturn");
            #endregion

            #region ToString
            Console.WriteLine("\r\nToString: {0}", hello.ToString());
            #endregion

            #region InitLocals
            // Display the default value for InitLocals.
            if (hello.InitLocals)
            {
                Console.Write("\r\nThis method contains verifiable code.");
            }
            else
            {
                Console.Write("\r\nThis method contains unverifiable code.");
            }
            Console.WriteLine(" (InitLocals = {0})", hello.InitLocals);
            #endregion
        }

        // Placeholder for the DynamicMethod variable used in examples
        private static DynamicMethod hello = null!;
    }
}
