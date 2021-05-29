// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
** Purpose: Searches for resources on disk, used for file-
** based resource lookup.
**
**
===========================================================*/

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace System.Resources
{
    internal sealed class FileBasedResourceGroveler : IResourceGroveler
    {
        private readonly ResourceManager.ResourceManagerMediator _mediator;

        public FileBasedResourceGroveler(ResourceManager.ResourceManagerMediator mediator)
        {
            Debug.Assert(mediator != null, "mediator shouldn't be null; check caller");
            _mediator = mediator;
        }

        // Consider modifying IResourceGroveler interface (hence this method signature) when we figure out
        // serialization compat story for moving ResourceManager members to either file-based or
        // manifest-based classes. Want to continue tightening the design to get rid of unused params.
        public ResourceSet? GrovelForResourceSet(CultureInfo culture, Dictionary<string, ResourceSet> localResourceSets, bool tryParents, bool createIfNotExists)
        {
            Debug.Assert(culture != null, "culture shouldn't be null; check caller");

            ResourceSet? rs = null;

            // Don't use Assembly manifest, but grovel on disk for a file.
            // Create new ResourceSet, if a file exists on disk for it.
            string tempFileName = _mediator.GetResourceFileName(culture);
            string? fileName = FindResourceFile(culture, tempFileName);
            if (fileName == null)
            {
                if (tryParents)
                {
                    // If we've hit top of the Culture tree, return.
                    if (culture.HasInvariantCultureName)
                    {
                        // We really don't think this should happen - we always
                        // expect the neutral locale's resources to be present.
                        throw new MissingManifestResourceException(SR.MissingManifestResource_NoNeutralDisk + Environment.NewLineConst + "baseName: " + _mediator.BaseNameField + "  locationInfo: " + (_mediator.LocationInfo == null ? "<null>" : _mediator.LocationInfo.FullName) + "  fileName: " + _mediator.GetResourceFileName(culture));
                    }
                }
            }
            else
            {
                rs = CreateResourceSet(fileName);
            }
            return rs;
        }

        // Given a CultureInfo, it generates the path &; file name for
        // the .resources file for that CultureInfo.  This method will grovel
        // the disk looking for the correct file name & path.  Uses CultureInfo's
        // Name property.  If the module directory was set in the ResourceManager
        // constructor, we'll look there first.  If it couldn't be found in the module
        // diretory or the module dir wasn't provided, look in the current
        // directory.
        private string? FindResourceFile(CultureInfo culture, string fileName)
        {
            Debug.Assert(culture != null, "culture shouldn't be null; check caller");
            Debug.Assert(fileName != null, "fileName shouldn't be null; check caller");

            // If we have a moduleDir, check there first.  Get module fully
            // qualified name, append path to that.
            if (_mediator.ModuleDir != null)
            {
                string path = Path.Combine(_mediator.ModuleDir, fileName);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // look in .
            if (File.Exists(fileName))
                return fileName;

            return null;  // give up.
        }

        // Constructs a new ResourceSet for a given file name.
        private ResourceSet CreateResourceSet(string file)
        {
            Debug.Assert(file != null, "file shouldn't be null; check caller");

            if (_mediator.UserResourceSet == null)
            {
                return new RuntimeResourceSet(file);
            }
            else
            {
                object[] args = new object[1];
                args[0] = file;
                try
                {
                    return (ResourceSet)Activator.CreateInstance(_mediator.UserResourceSet, args)!;
                }
                catch (MissingMethodException e)
                {
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResMgrBadResSet_Type, _mediator.UserResourceSet.AssemblyQualifiedName), e);
                }
            }
        }
    }
}
