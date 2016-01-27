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
    public enum UIPermissionWindow
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
    public enum UIPermissionClipboard
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
    sealed public class UIPermission 
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
    
        public UIPermission(UIPermissionWindow windowFlag )
        {
            VerifyWindowFlag( windowFlag );
            
            m_windowFlag = windowFlag;
        }
    
        public UIPermission(UIPermissionClipboard clipboardFlag )
        {
            VerifyClipboardFlag( clipboardFlag );
            
            m_clipboardFlag = clipboardFlag;
        }
        
        
        //------------------------------------------------------
        //
        // PUBLIC ACCESSOR METHODS
        //
        //------------------------------------------------------
        
        public UIPermissionWindow Window
        {
            set
            {
                VerifyWindowFlag(value);
            
                m_windowFlag = value;
            }
            
            get
            {
                return m_windowFlag;
            }
        }
        
        public UIPermissionClipboard Clipboard
        {
            set
            {
                VerifyClipboardFlag(value);
            
                m_clipboardFlag = value;
            }
            
            get
            {
                return m_clipboardFlag;
            }
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

#if false    
        //------------------------------------------------------
        //
        // OBJECT METHOD OVERRIDES
        //
        //------------------------------------------------------
        public String ToString()
        {
    #if _DEBUG
            StringBuilder sb = new StringBuilder();
            sb.Append("UIPermission(");
            if (IsUnrestricted())
            {
                sb.Append("Unrestricted");
            }
            else
            {
                sb.Append(m_stateNameTableWindow[m_windowFlag]);
                sb.Append(", ");
                sb.Append(m_stateNameTableClipboard[m_clipboardFlag]);
            }
            
            sb.Append(")");
            return sb.ToString();
    #else
            return super.ToString();
    #endif
        }
#endif
        
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
    
#if FEATURE_CAS_POLICY
        public override SecurityElement ToXml()
        {
            SecurityElement esd = CodeAccessPermission.CreatePermissionElement( this, "System.Security.Permissions.UIPermission" );
            if (!IsUnrestricted())
            {
                if (m_windowFlag != UIPermissionWindow.NoWindows)
                {
                    esd.AddAttribute( "Window", Enum.GetName( typeof( UIPermissionWindow ), m_windowFlag ) );
                }
                if (m_clipboardFlag != UIPermissionClipboard.NoClipboard)
                {
                    esd.AddAttribute( "Clipboard", Enum.GetName( typeof( UIPermissionClipboard ), m_clipboardFlag ) );
                }
            }
            else
            {
                esd.AddAttribute( "Unrestricted", "true" );
            }
            return esd;
        }
            
        public override void FromXml(SecurityElement esd)
        {
            CodeAccessPermission.ValidateElement( esd, this );
            if (XMLUtil.IsUnrestricted( esd ))
            {
                SetUnrestricted( true );
                return;
            }
            
            m_windowFlag = UIPermissionWindow.NoWindows;
            m_clipboardFlag = UIPermissionClipboard.NoClipboard;

            String window = esd.Attribute( "Window" );
            if (window != null)
                m_windowFlag = (UIPermissionWindow)Enum.Parse( typeof( UIPermissionWindow ), window );

            String clipboard = esd.Attribute( "Clipboard" );
            if (clipboard != null)
                m_clipboardFlag = (UIPermissionClipboard)Enum.Parse( typeof( UIPermissionClipboard ), clipboard );
        }
#endif // FEATURE_CAS_POLICY

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
