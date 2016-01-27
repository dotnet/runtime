// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Searches for resources on disk, used for file-
** based resource lookup.
**
** 
===========================================================*/
namespace System.Resources {    
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Threading;
    using System.Diagnostics.Contracts;

    internal class FileBasedResourceGroveler : IResourceGroveler
    {
        private ResourceManager.ResourceManagerMediator _mediator;

        public FileBasedResourceGroveler(ResourceManager.ResourceManagerMediator mediator)
        {
            Contract.Assert(mediator != null, "mediator shouldn't be null; check caller");
            _mediator = mediator;
        }

        // Consider modifying IResourceGroveler interface (hence this method signature) when we figure out 
        // serialization compat story for moving ResourceManager members to either file-based or 
        // manifest-based classes. Want to continue tightening the design to get rid of unused params.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public ResourceSet GrovelForResourceSet(CultureInfo culture, Dictionary<String, ResourceSet> localResourceSets, bool tryParents, bool createIfNotExists, ref StackCrawlMark stackMark) 
        {
            Contract.Assert(culture != null, "culture shouldn't be null; check caller");

            String fileName = null;
            ResourceSet rs = null;

            // Don't use Assembly manifest, but grovel on disk for a file.
            try
            {
                new System.Security.Permissions.FileIOPermission(System.Security.Permissions.PermissionState.Unrestricted).Assert();

                // Create new ResourceSet, if a file exists on disk for it.
                String tempFileName = _mediator.GetResourceFileName(culture);
                fileName = FindResourceFile(culture, tempFileName);
                if (fileName == null)
                {
                    if (tryParents)
                    {
                        // If we've hit top of the Culture tree, return.
                        if (culture.HasInvariantCultureName)
                        {
                            // We really don't think this should happen - we always
                            // expect the neutral locale's resources to be present.
                            throw new MissingManifestResourceException(Environment.GetResourceString("MissingManifestResource_NoNeutralDisk") + Environment.NewLine + "baseName: " + _mediator.BaseNameField + "  locationInfo: " + (_mediator.LocationInfo == null ? "<null>" : _mediator.LocationInfo.FullName) + "  fileName: " + _mediator.GetResourceFileName(culture));
                        }
                    }
                }
                else
                {
                    rs = CreateResourceSet(fileName);
                }
                return rs;
            }
            finally
            {
                System.Security.CodeAccessPermission.RevertAssert();
            }
        }

#if !FEATURE_CORECLR   // PAL doesn't support eventing, and we don't compile event providers for coreclr
        public bool HasNeutralResources(CultureInfo culture, String defaultResName)
        {
            // Detect missing neutral locale resources.
            String defaultResPath = FindResourceFile(culture, defaultResName);
            if (defaultResPath == null || !File.Exists(defaultResPath))
            {
                String dir = _mediator.ModuleDir;
                if (defaultResPath != null)
                {
                    dir = Path.GetDirectoryName(defaultResPath);
                }
                return false;
            }
            return true;
        }
#endif

        // Given a CultureInfo, it generates the path &; file name for 
        // the .resources file for that CultureInfo.  This method will grovel
        // the disk looking for the correct file name & path.  Uses CultureInfo's
        // Name property.  If the module directory was set in the ResourceManager 
        // constructor, we'll look there first.  If it couldn't be found in the module
        // diretory or the module dir wasn't provided, look in the current
        // directory.

        private String FindResourceFile(CultureInfo culture, String fileName)
        {
            Contract.Assert(culture != null, "culture shouldn't be null; check caller");
            Contract.Assert(fileName != null, "fileName shouldn't be null; check caller");

            // If we have a moduleDir, check there first.  Get module fully 
            // qualified name, append path to that.
            if (_mediator.ModuleDir != null)
            {
#if _DEBUG
                if (ResourceManager.DEBUG >= 3)
                    BCLDebug.Log("FindResourceFile: checking module dir: \""+_mediator.ModuleDir+'\"');
#endif

                String path = Path.Combine(_mediator.ModuleDir, fileName);
                if (File.Exists(path))
                {
#if _DEBUG
                    if (ResourceManager.DEBUG >= 3)
                        BCLDebug.Log("Found resource file in module dir!  "+path);
#endif
                    return path;
                }
            }

#if _DEBUG
            if (ResourceManager.DEBUG >= 3)
                BCLDebug.Log("Couldn't find resource file in module dir, checking .\\"+fileName);
#endif

            // look in .
            if (File.Exists(fileName))
                return fileName;

            return null;  // give up.
        }

        // Constructs a new ResourceSet for a given file name.  The logic in
        // here avoids a ReflectionPermission check for our RuntimeResourceSet
        // for perf and working set reasons.
        [System.Security.SecurityCritical]
        private ResourceSet CreateResourceSet(String file)
        {
            Contract.Assert(file != null, "file shouldn't be null; check caller");

            if (_mediator.UserResourceSet == null)
            {
                // Explicitly avoid CreateInstance if possible, because it
                // requires ReflectionPermission to call private & protected
                // constructors.  
                return new RuntimeResourceSet(file);
            }
            else
            {
                Object[] args = new Object[1];
                args[0] = file;
                try
                {
                    return (ResourceSet)Activator.CreateInstance(_mediator.UserResourceSet, args);
                }
                catch (MissingMethodException e)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResMgrBadResSet_Type", _mediator.UserResourceSet.AssemblyQualifiedName), e);
                }
            }
        }
    }
}
