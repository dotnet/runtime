// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

// Exception message from CoreLib
const string expectedNullRefMessage =
#if RESOURCE_KEYS
    "Arg_NullReferenceException";
#else
    "Object reference not set to an instance of an object.";
#endif
string actualNullRefMessage = new NullReferenceException().Message;
Console.WriteLine(expectedNullRefMessage);
Console.WriteLine(actualNullRefMessage);
if (actualNullRefMessage != expectedNullRefMessage)
{
    throw new Exception();
}

// Some exception message from the reflection library.
const string expectedReflectionMessage =
#if RESOURCE_KEYS
    "Argument_ArrayGetInterfaceMap";
#else
    "Interface maps for generic interfaces on arrays cannot be retrieved.";
#endif
string actualReflectionMessage;
try
{
    typeof(int[]).GetInterfaceMap(typeof(IEnumerable<int>));
    actualReflectionMessage = "I guess we need to update the test";
}
catch (Exception ex)
{
    actualReflectionMessage = ex.Message;
}
Console.WriteLine(expectedReflectionMessage);
Console.WriteLine(actualReflectionMessage);
if (expectedReflectionMessage != actualReflectionMessage)
{
    throw new Exception(actualReflectionMessage);
}

Console.WriteLine("Resources in CoreLib:");
string[] coreLibNames = typeof(object).Assembly.GetManifestResourceNames();
foreach (var name in coreLibNames)
    Console.WriteLine(name);

#if RESOURCE_KEYS
if (coreLibNames.Length != 0)
    throw new Exception();
#endif

Console.WriteLine("Resources in reflection library:");
string[] refNames;
const string reflectionAssembly = "System.Private.Reflection.Execution";
#if RESOURCE_KEYS
try
{
    refNames = System.Reflection.Assembly.Load(reflectionAssembly).GetManifestResourceNames();
}
catch (System.IO.FileNotFoundException)
{
    refNames = Array.Empty<string>();
}
#else
refNames = System.Reflection.Assembly.Load(reflectionAssembly).GetManifestResourceNames();
#endif
foreach (var name in refNames)
    Console.WriteLine(name);

#if RESOURCE_KEYS
if (refNames.Length != 0)
    throw new Exception();
#endif

return 100;
