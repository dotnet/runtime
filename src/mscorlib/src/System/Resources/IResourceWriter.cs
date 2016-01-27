// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Default way to write strings to a COM+ resource 
** file.
**
** 
===========================================================*/
namespace System.Resources {
    using System;
    using System.IO;
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IResourceWriter : IDisposable
    {
        // Interface does not need to be marked with the serializable attribute
        // Adds a string resource to the list of resources to be written to a file.
        // They aren't written until WriteFile() is called.
        // 
        void AddResource(String name, String value);
    
        // Adds a resource to the list of resources to be written to a file.
        // They aren't written until WriteFile() is called.
        // 
        void AddResource(String name, Object value);
    
        // Adds a named byte array as a resource to the list of resources to 
        // be written to a file. They aren't written until WriteFile() is called.
        // 
        void AddResource(String name, byte[] value);
    
        // Closes the underlying resource file.
        void Close();

        // After calling AddResource, this writes all resources to the output
        // stream.  This does NOT close the output stream.
        void Generate();
    }
}
