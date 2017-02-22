// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** ParameterBuilder is used to create/associate parameter information
**
** 
===========================================================*/

using System.Runtime.InteropServices;
using System;
using System.Reflection;
using System.Diagnostics.Contracts;

namespace System.Reflection.Emit
{
    public class ParameterBuilder
    {
        // Set the default value of the parameter
        public virtual void SetConstant(Object defaultValue)
        {
            TypeBuilder.SetConstantValue(
                m_methodBuilder.GetModuleBuilder(),
                m_pdToken.Token,
                m_iPosition == 0 ? m_methodBuilder.ReturnType : m_methodBuilder.m_parameterTypes[m_iPosition - 1],
                defaultValue);
        }

        // Use this function if client decides to form the custom attribute blob themselves

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException(nameof(con));
            if (binaryAttribute == null)
                throw new ArgumentNullException(nameof(binaryAttribute));
            Contract.EndContractBlock();

            TypeBuilder.DefineCustomAttribute(
                m_methodBuilder.GetModuleBuilder(),
                m_pdToken.Token,
                ((ModuleBuilder)m_methodBuilder.GetModule()).GetConstructorToken(con).Token,
                binaryAttribute,
                false, false);
        }

        // Use this function if client wishes to build CustomAttribute using CustomAttributeBuilder
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException(nameof(customBuilder));
            }
            Contract.EndContractBlock();
            customBuilder.CreateCustomAttribute((ModuleBuilder)(m_methodBuilder.GetModule()), m_pdToken.Token);
        }

        //*******************************
        // Make a private constructor so these cannot be constructed externally.
        //*******************************
        private ParameterBuilder() { }


        internal ParameterBuilder(
            MethodBuilder methodBuilder,
            int sequence,
            ParameterAttributes attributes,
            String strParamName)            // can be NULL string
        {
            m_iPosition = sequence;
            m_strParamName = strParamName;
            m_methodBuilder = methodBuilder;
            m_strParamName = strParamName;
            m_attributes = attributes;
            m_pdToken = new ParameterToken(TypeBuilder.SetParamInfo(
                        m_methodBuilder.GetModuleBuilder().GetNativeHandle(),
                        m_methodBuilder.GetToken().Token,
                        sequence,
                        attributes,
                        strParamName));
        }

        public virtual ParameterToken GetToken()
        {
            return m_pdToken;
        }

        public virtual String Name
        {
            get { return m_strParamName; }
        }

        public virtual int Position
        {
            get { return m_iPosition; }
        }

        public virtual int Attributes
        {
            get { return (int)m_attributes; }
        }

        public bool IsIn
        {
            get { return ((m_attributes & ParameterAttributes.In) != 0); }
        }
        public bool IsOut
        {
            get { return ((m_attributes & ParameterAttributes.Out) != 0); }
        }
        public bool IsOptional
        {
            get { return ((m_attributes & ParameterAttributes.Optional) != 0); }
        }

        private String m_strParamName;
        private int m_iPosition;
        private ParameterAttributes m_attributes;
        private MethodBuilder m_methodBuilder;
        private ParameterToken m_pdToken;
    }
}
