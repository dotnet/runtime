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
            foreach (ParameterInfo p in parameters)
            {
                Console.WriteLine("\t{0}, {1}, {2}",
                    p.Name, p.ParameterType, p.Attributes);
            }
            #endregion

            #region ReturnType
            // If the method has no return type, ReturnType is System.Void.
            Console.WriteLine("\r\nReturn type: {0}", hello.ReturnType);
            #endregion

            #region ReturnParameter
            if (hello.ReturnParameter == null)
            {
                Console.WriteLine("\r\nMethod has no return parameter.");
            }
            else
            {
                Console.WriteLine("\r\nReturn parameter: {0}", hello.ReturnParameter);
            }
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
                    foreach (object attr in returnAttributes)
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

            #region GetILGenerator
            // Create an array that specifies the parameter types of the
            // overload of Console.WriteLine to be used in Hello.
            Type[] writeStringArgs = {typeof(string)};
            // Get the overload of Console.WriteLine that has one
            // String parameter.
            MethodInfo writeString = typeof(Console).GetMethod("WriteLine",
                writeStringArgs);

            // Get an ILGenerator and emit a body for the dynamic method,
            // using a stream size larger than the IL that will be
            // emitted.
            ILGenerator il = hello.GetILGenerator(256);
            // Load the first argument, which is a string, onto the stack.
            il.Emit(OpCodes.Ldarg_0);
            // Call the overload of Console.WriteLine that prints a string.
            il.EmitCall(OpCodes.Call, writeString, null);
            // The Hello method returns the value of the second argument;
            // to do this, load the onto the stack and return.
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ret);
            #endregion

            #region CreateDelegate
            // Create a delegate that represents the dynamic method. This
            // action completes the method. Any further attempts to
            // change the method are ignored.
            HelloDelegate hi =
                (HelloDelegate) hello.CreateDelegate(typeof(HelloDelegate));

            // Use the delegate to execute the dynamic method.
            Console.WriteLine("\r\nUse the delegate to execute the dynamic method:");
            int retval = hi("\r\nHello, World!", 42);
            Console.WriteLine("Invoking delegate hi(\"Hello, World!\", 42) returned: " + retval);

            // Execute it again, with different arguments.
            retval = hi("\r\nHi, Mom!", 5280);
            Console.WriteLine("Invoking delegate hi(\"Hi, Mom!\", 5280) returned: " + retval);
            #endregion

            #region Invoke
            Console.WriteLine("\r\nUse the Invoke method to execute the dynamic method:");
            // Create an array of arguments to use with the Invoke method.
            object[] invokeArgs = {"\r\nHello, World!", 42};
            // Invoke the dynamic method using the arguments. This is much
            // slower than using the delegate, because you must create an
            // array to contain the arguments, and value-type arguments
            // must be boxed.
            object objRet = hello.Invoke(null, BindingFlags.ExactBinding, null, invokeArgs, new System.Globalization.CultureInfo("en-us"));
            Console.WriteLine("hello.Invoke returned: " + objRet);
            #endregion
        }

        // Placeholder for the DynamicMethod variable used in examples
        private static DynamicMethod hello = null!;

        // Declare a delegate type that can be used to execute the completed
        // dynamic method.
        private delegate int HelloDelegate(string msg, int ret);
    }
}
