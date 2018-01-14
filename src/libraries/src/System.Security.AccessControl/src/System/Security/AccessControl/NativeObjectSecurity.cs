// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Classes:  NativeObjectSecurity class
**
**
===========================================================*/

using Microsoft.Win32;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using FileNotFoundException = System.IO.FileNotFoundException;

namespace System.Security.AccessControl
{
    public abstract class NativeObjectSecurity : CommonObjectSecurity
    {
        #region Private Members

        private readonly ResourceType _resourceType;
        private ExceptionFromErrorCode _exceptionFromErrorCode = null;
        private object _exceptionContext = null;
        private readonly uint ProtectedDiscretionaryAcl = 0x80000000;
        private readonly uint ProtectedSystemAcl = 0x40000000;
        private readonly uint UnprotectedDiscretionaryAcl = 0x20000000;
        private readonly uint UnprotectedSystemAcl = 0x10000000;



        #endregion

        #region Delegates

        protected internal delegate System.Exception ExceptionFromErrorCode(int errorCode, string name, SafeHandle handle, object context);

        #endregion

        #region Constructors

        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType)
            : base(isContainer)
        {
            _resourceType = resourceType;
        }

        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
            : this(isContainer, resourceType)
        {
            _exceptionContext = exceptionContext;
            _exceptionFromErrorCode = exceptionFromErrorCode;
        }

        internal NativeObjectSecurity(ResourceType resourceType, CommonSecurityDescriptor securityDescriptor)
            : this(resourceType, securityDescriptor, null)
        {
        }

        internal NativeObjectSecurity(ResourceType resourceType, CommonSecurityDescriptor securityDescriptor, ExceptionFromErrorCode exceptionFromErrorCode)
            : base(securityDescriptor)
        {
            _resourceType = resourceType;
            _exceptionFromErrorCode = exceptionFromErrorCode;
        }

        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections, ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
            : this(resourceType, CreateInternal(resourceType, isContainer, name, null, includeSections, true, exceptionFromErrorCode, exceptionContext), exceptionFromErrorCode)
        {
        }

        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, string name, AccessControlSections includeSections)
            : this(isContainer, resourceType, name, includeSections, null, null)
        {
        }

        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle handle, AccessControlSections includeSections, ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
            : this(resourceType, CreateInternal(resourceType, isContainer, null, handle, includeSections, false, exceptionFromErrorCode, exceptionContext), exceptionFromErrorCode)
        {
        }

        protected NativeObjectSecurity(bool isContainer, ResourceType resourceType, SafeHandle handle, AccessControlSections includeSections)
            : this(isContainer, resourceType, handle, includeSections, null, null)
        {
        }

        #endregion

        #region Private Methods

        private static CommonSecurityDescriptor CreateInternal(ResourceType resourceType, bool isContainer, string name, SafeHandle handle, AccessControlSections includeSections, bool createByName, ExceptionFromErrorCode exceptionFromErrorCode, object exceptionContext)
        {
            int error;
            RawSecurityDescriptor rawSD;

            if (createByName && name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            else if (!createByName && handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            error = Win32.GetSecurityInfo(resourceType, name, handle, includeSections, out rawSD);

            if (error != Interop.Errors.ERROR_SUCCESS)
            {
                System.Exception exception = null;

                if (exceptionFromErrorCode != null)
                {
                    exception = exceptionFromErrorCode(error, name, handle, exceptionContext);
                }

                if (exception == null)
                {
                    if (error == Interop.Errors.ERROR_ACCESS_DENIED)
                    {
                        exception = new UnauthorizedAccessException();
                    }
                    else if (error == Interop.Errors.ERROR_INVALID_OWNER)
                    {
                        exception = new InvalidOperationException(SR.AccessControl_InvalidOwner);
                    }
                    else if (error == Interop.Errors.ERROR_INVALID_PRIMARY_GROUP)
                    {
                        exception = new InvalidOperationException(SR.AccessControl_InvalidGroup);
                    }
                    else if (error == Interop.Errors.ERROR_INVALID_PARAMETER)
                    {
                        exception = new InvalidOperationException(SR.Format(SR.AccessControl_UnexpectedError, error));
                    }
                    else if (error == Interop.Errors.ERROR_INVALID_NAME)
                    {
                        exception = new ArgumentException(
                             SR.Argument_InvalidName,
nameof(name));
                    }
                    else if (error == Interop.Errors.ERROR_FILE_NOT_FOUND)
                    {
                        exception = (name == null ? new FileNotFoundException() : new FileNotFoundException(name));
                    }
                    else if (error == Interop.Errors.ERROR_NO_SECURITY_ON_OBJECT)
                    {
                        exception = new NotSupportedException(SR.AccessControl_NoAssociatedSecurity);
                    }
                    else if (error == Interop.Errors.ERROR_PIPE_NOT_CONNECTED)
                    {
                        exception = new InvalidOperationException(SR.InvalidOperation_DisconnectedPipe);
                    }
                    else
                    {
                        Debug.Assert(false, string.Format(CultureInfo.InvariantCulture, "Win32GetSecurityInfo() failed with unexpected error code {0}", error));
                        exception = new InvalidOperationException(SR.Format(SR.AccessControl_UnexpectedError, error));
                    }
                }

                throw exception;
            }

            return new CommonSecurityDescriptor(isContainer, false /* isDS */, rawSD, true);
        }

        //
        // Attempts to persist the security descriptor onto the object
        //

        private void Persist(string name, SafeHandle handle, AccessControlSections includeSections, object exceptionContext)
        {
            WriteLock();

            try
            {
                int error;
                SecurityInfos securityInfo = 0;

                SecurityIdentifier owner = null, group = null;
                SystemAcl sacl = null;
                DiscretionaryAcl dacl = null;

                if ((includeSections & AccessControlSections.Owner) != 0 && _securityDescriptor.Owner != null)
                {
                    securityInfo |= SecurityInfos.Owner;
                    owner = _securityDescriptor.Owner;
                }

                if ((includeSections & AccessControlSections.Group) != 0 && _securityDescriptor.Group != null)
                {
                    securityInfo |= SecurityInfos.Group;
                    group = _securityDescriptor.Group;
                }

                if ((includeSections & AccessControlSections.Audit) != 0)
                {
                    securityInfo |= SecurityInfos.SystemAcl;
                    if (_securityDescriptor.IsSystemAclPresent &&
                         _securityDescriptor.SystemAcl != null &&
                         _securityDescriptor.SystemAcl.Count > 0)
                    {
                        sacl = _securityDescriptor.SystemAcl;
                    }
                    else
                    {
                        sacl = null;
                    }

                    if ((_securityDescriptor.ControlFlags & ControlFlags.SystemAclProtected) != 0)
                    {
                        securityInfo = (SecurityInfos)((uint)securityInfo | ProtectedSystemAcl);
                    }
                    else
                    {
                        securityInfo = (SecurityInfos)((uint)securityInfo | UnprotectedSystemAcl);
                    }
                }

                if ((includeSections & AccessControlSections.Access) != 0 && _securityDescriptor.IsDiscretionaryAclPresent)
                {
                    securityInfo |= SecurityInfos.DiscretionaryAcl;

                    // if the DACL is in fact a crafted replaced for NULL replacement, then we will persist it as NULL
                    if (_securityDescriptor.DiscretionaryAcl.EveryOneFullAccessForNullDacl)
                    {
                        dacl = null;
                    }
                    else
                    {
                        dacl = _securityDescriptor.DiscretionaryAcl;
                    }

                    if ((_securityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclProtected) != 0)
                    {
                        securityInfo = unchecked((SecurityInfos)((uint)securityInfo | ProtectedDiscretionaryAcl));
                    }
                    else
                    {
                        securityInfo = (SecurityInfos)((uint)securityInfo | UnprotectedDiscretionaryAcl);
                    }
                }

                if (securityInfo == 0)
                {
                    //
                    // Nothing to persist
                    //

                    return;
                }

                error = Win32.SetSecurityInfo(_resourceType, name, handle, securityInfo, owner, group, sacl, dacl);

                if (error != Interop.Errors.ERROR_SUCCESS)
                {
                    System.Exception exception = null;

                    if (_exceptionFromErrorCode != null)
                    {
                        exception = _exceptionFromErrorCode(error, name, handle, exceptionContext);
                    }

                    if (exception == null)
                    {
                        if (error == Interop.Errors.ERROR_ACCESS_DENIED)
                        {
                            exception = new UnauthorizedAccessException();
                        }
                        else if (error == Interop.Errors.ERROR_INVALID_OWNER)
                        {
                            exception = new InvalidOperationException(SR.AccessControl_InvalidOwner);
                        }
                        else if (error == Interop.Errors.ERROR_INVALID_PRIMARY_GROUP)
                        {
                            exception = new InvalidOperationException(SR.AccessControl_InvalidGroup);
                        }
                        else if (error == Interop.Errors.ERROR_INVALID_NAME)
                        {
                            exception = new ArgumentException(
                                 SR.Argument_InvalidName,
nameof(name));
                        }
                        else if (error == Interop.Errors.ERROR_INVALID_HANDLE)
                        {
                            exception = new NotSupportedException(SR.AccessControl_InvalidHandle);
                        }
                        else if (error == Interop.Errors.ERROR_FILE_NOT_FOUND)
                        {
                            exception = new FileNotFoundException();
                        }
                        else if (error == Interop.Errors.ERROR_NO_SECURITY_ON_OBJECT)
                        {
                            exception = new NotSupportedException(SR.AccessControl_NoAssociatedSecurity);
                        }
                        else
                        {
                            Debug.Assert(false, string.Format(CultureInfo.InvariantCulture, "Unexpected error code {0}", error));
                            exception = new InvalidOperationException(SR.Format(SR.AccessControl_UnexpectedError, error));
                        }
                    }

                    throw exception;
                }

                //
                // Everything goes well, let us clean the modified flags.
                // We are in proper write lock, so just go ahead
                //

                this.OwnerModified = false;
                this.GroupModified = false;
                this.AccessRulesModified = false;
                this.AuditRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        #endregion

        #region Protected Methods

        //
        // Persists the changes made to the object
        // by calling the underlying Windows API
        //
        // This overloaded method takes a name of an existing object
        //

        protected sealed override void Persist(string name, AccessControlSections includeSections)
        {
            Persist(name, includeSections, _exceptionContext);
        }

        protected void Persist(string name, AccessControlSections includeSections, object exceptionContext)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Persist(name, null, includeSections, exceptionContext);
        }

        //
        // Persists the changes made to the object
        // by calling the underlying Windows API
        //
        // This overloaded method takes a handle to an existing object
        //
        protected sealed override void Persist(SafeHandle handle, AccessControlSections includeSections)
        {
            Persist(handle, includeSections, _exceptionContext);
        }

        protected void Persist(SafeHandle handle, AccessControlSections includeSections, object exceptionContext)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            Persist(null, handle, includeSections, exceptionContext);
        }
        #endregion
    }
}
