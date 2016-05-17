// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Globalization {
    using System;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.IO;
    using System.Diagnostics.Contracts;

    
    /*=================================GlobalizationAssembly==========================
    **
    ** This class provides the table loading wrapper that calls GetManifestResourceStream
    **
    ** It used to provide an idea for sort versioning, but that proved to not work
    **
    ============================================================================*/
    internal sealed class GlobalizationAssembly
    {
        // ----------------------------------------------------------------------------------------------------
        //
        // Instance data members and instance methods.
        //
        // ----------------------------------------------------------------------------------------------------
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe static byte* GetGlobalizationResourceBytePtr(Assembly assembly, String tableName) {
            Contract.Assert(assembly != null, "assembly can not be null.  This should be generally the "+System.CoreLib.Name+" assembly.");
            Contract.Assert(tableName != null, "table name can not be null");
            
            Stream stream = assembly.GetManifestResourceStream(tableName);
            UnmanagedMemoryStream bytesStream = stream as UnmanagedMemoryStream;
            if (bytesStream != null) {
                byte* bytes = bytesStream.PositionPointer;
                if (bytes != null) {
                    return (bytes);
                }
            }
            
            Contract.Assert(
                    false, 
                    String.Format(
                        CultureInfo.CurrentCulture,
                        "Didn't get the resource table {0} for System.Globalization from {1}", 
                        tableName, 
                        assembly));
            
            // We can not continue if we can't get the resource.
            throw new InvalidOperationException();
        }

    }
}

