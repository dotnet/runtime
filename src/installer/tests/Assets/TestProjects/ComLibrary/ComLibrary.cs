// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace ComLibrary
{
    // User defined attribute on types. Ensure that user defined
    // attributes don't break us from parsing metadata.
    public class UserDefinedAttribute : Attribute
    {
    }

    [ComVisible(true)]
    [Guid("27293cc8-7933-4fdf-9fde-653cbf9b55df")]
    public interface IServer
    {
    }

    [UserDefined]
    [ComVisible(true)]
    [Guid("438968CE-5950-4FBC-90B0-E64691350DF5")]
    public class Server : IServer
    {
        public Server()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Console.WriteLine($"{asm.GetName().Name}: AssemblyLoadContext = {AssemblyLoadContext.GetLoadContext(asm)}");
            Console.WriteLine($"New instance of {nameof(Server)} created");
        }
    }

    [Guid("6e30943e-b8ab-4e02-a904-9f1b5bb1c97d")]
    public class NotComVisible
    {
    }

    [ComVisible(true)]
    [Guid("f7199267-9821-4f5b-924b-ab5246b455cd")]
    public interface INested
    {
    }

    [ComVisible(true)]
    [Guid("36e75747-aecd-43bf-9082-1a605889c762")]
    public class ComVisible
    {
        [UserDefined]
        [ComVisible(true)]
        [Guid("c82e4585-58bd-46e0-a76d-c0b6975e5984")]
        public class Nested : INested
        {
        }
    }

    [ComVisible(true)]
    [Guid("cf55ff0a-19a6-45a6-9aea-52597be13fb5")]
    internal class ComVisibleNonPublic
    {
        [ComVisible(true)]
        [Guid("8a0a7085-aca4-4651-9878-ca42747e2206")]
        public class Nested : INested
        {
        }
    }

    [ComVisible(true)]
    [Guid("f5ad253b-845e-4c91-95a7-3ff2fa0c91cd")]
    [ProgId("CustomProgId")]
    public class ComVisibleCustomProgId
    {
    }

    [ComVisible(true)]
    [Guid("4c8bd844-593d-43cb-b605-f0bc52f674fa")]
    [ProgId("")]
    public class ExplicitNoProgId
    {
    }
}
