// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    using System;
    using System.Security;
    using System.Security.Util;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Reflection;
    using System.Collections;
    using System.Globalization;
    using System.Diagnostics.Contracts;
    
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    internal enum UIPermissionWindow
    {
        // No window use allowed at all.
        NoWindows = 0x0,
    
        // Only allow safe subwindow use (for embedded components).
        SafeSubWindows = 0x01,
    
        // Safe top-level window use only (see specification for details).
        SafeTopLevelWindows = 0x02,
    
        // All windows and all event may be used.
        AllWindows = 0x03,
    
    }
    
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    internal enum UIPermissionClipboard
    {
        // No clipboard access is allowed.
        NoClipboard = 0x0,
    
        // Paste from the same app domain only.
        OwnClipboard = 0x1,
    
        // Any clipboard access is allowed.
        AllClipboard = 0x2,
    
    }
    
    
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    sealed internal class UIPermission 
           : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
        //------------------------------------------------------
        //
        // PRIVATE STATE DATA
        //
        //------------------------------------------------------
        
        private UIPermissionWindow m_windowFlag;
        private UIPermissionClipboard m_clipboardFlag;
        
        //------------------------------------------------------
        //
        // PUBLIC CONSTRUCTORS
        //
        //------------------------------------------------------
    
        public UIPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                SetUnrestricted( true );
            }
            else if (state == PermissionState.None)
            {
                SetUnrestricted( false );
                Reset();
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }    
        
        public UIPermission(UIPermissionWindow windowFlag, UIPermissionClipboard clipboardFlag )
        {
            VerifyWindowFlag( windowFlag );
            VerifyClipboardFlag( clipboardFlag );
            
            m_windowFlag = windowFlag;
            m_clipboardFlag = clipboardFlag;
        }
    
        //------------------------------------------------------
        //
        // PRIVATE AND PROTECTED HELPERS FOR ACCESSORS AND CONSTRUCTORS
        //
        //------------------------------------------------------
        
        private static void VerifyWindowFlag(UIPermissionWindow flag)
        {
            if (flag < UIPermissionWindow.NoWindows || flag > UIPermissionWindow.AllWindows)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)flag));
            }
            Contract.EndContractBlock();
        }
        
        private static void VerifyClipboardFlag(UIPermissionClipboard flag)
        {
            if (flag < UIPermissionClipboard.NoClipboard || flag > UIPermissionClipboard.AllClipboard)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)flag));
            }
            Contract.EndContractBlock();
        }
        
        private void Reset()
        {
            m_windowFlag = UIPermissionWindow.NoWindows;
            m_clipboardFlag = UIPermissionClipboard.NoClipboard;
        }
        
        private void SetUnrestricted( bool unrestricted )
        {
            if (unrestricted)
            {
                m_windowFlag = UIPermissionWindow.AllWindows;
                m_clipboardFlag = UIPermissionClipboard.AllClipboard;
            }
        }

        
        //------------------------------------------------------
        //
        // CODEACCESSPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        public bool IsUnrestricted()
        {
            return m_windowFlag == UIPermissionWindow.AllWindows && m_clipboardFlag == UIPermissionClipboard.AllClipboard;
        }
        
        //------------------------------------------------------
        //
        // IPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                // Only safe subset if this is empty
                return m_windowFlag == UIPermissionWindow.NoWindows && m_clipboardFlag == UIPermissionClipboard.NoClipboard;
            }

            try
            {
                UIPermission operand = (UIPermission)target;
                if (operand.IsUnrestricted())
                    return true;
                else if (this.IsUnrestricted())
                    return false;
                else 
                    return this.m_windowFlag <= operand.m_windowFlag && this.m_clipboardFlag <= operand.m_clipboardFlag;
            }
            catch (InvalidCastException)
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }
            
        }
        
        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
            {
                return null;
            }
            else if (!VerifyType(target))
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }
            
            UIPermission operand = (UIPermission)target;
            UIPermissionWindow isectWindowFlags = m_windowFlag < operand.m_windowFlag ? m_windowFlag : operand.m_windowFlag;
            UIPermissionClipboard isectClipboardFlags = m_clipboardFlag < operand.m_clipboardFlag ? m_clipboardFlag : operand.m_clipboardFlag;
            if (isectWindowFlags == UIPermissionWindow.NoWindows && isectClipboardFlags == UIPermissionClipboard.NoClipboard)
                return null;
            else
                return new UIPermission(isectWindowFlags, isectClipboardFlags);
        }
        
        public override IPermission Union(IPermission target)
        {
            if (target == null)
            {
                return this.Copy();
            }
            else if (!VerifyType(target))
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }
            
            UIPermission operand = (UIPermission)target;
            UIPermissionWindow isectWindowFlags = m_windowFlag > operand.m_windowFlag ? m_windowFlag : operand.m_windowFlag;
            UIPermissionClipboard isectClipboardFlags = m_clipboardFlag > operand.m_clipboardFlag ? m_clipboardFlag : operand.m_clipboardFlag;
            if (isectWindowFlags == UIPermissionWindow.NoWindows && isectClipboardFlags == UIPermissionClipboard.NoClipboard)
                return null;
            else
                return new UIPermission(isectWindowFlags, isectClipboardFlags);
        }        
        
        public override IPermission Copy()
        {
            return new UIPermission(this.m_windowFlag, this.m_clipboardFlag);
        }

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return UIPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.UIPermissionIndex;
        }
            
    }


}
