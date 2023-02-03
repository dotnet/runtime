// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
**
**
** Eventbuilder is for client to define eevnts for a class
**
**
===========================================================*/

using System.Runtime.CompilerServices;

namespace System.Reflection.Emit
{
    //
    // A EventBuilder is always associated with a TypeBuilder.  The TypeBuilder.DefineEvent
    // method will return a new EventBuilder to a client.
    //
    internal sealed class RuntimeEventBuilder : EventBuilder
    {
        // Constructs a RuntimeEventBuilder.
        //
        internal RuntimeEventBuilder(
            RuntimeModuleBuilder mod,               // the module containing this EventBuilder
            string name,                            // Event name
            EventAttributes attr,                   // event attribute such as Public, Private, and Protected defined above
            RuntimeTypeBuilder type,                // containing type
            int evToken)
        {
            m_name = name;
            m_module = mod;
            m_attributes = attr;
            m_evToken = evToken;
            m_type = type;
        }

        // Return the Token for this event within the TypeBuilder that the
        // event is defined within.
        internal int GetEventToken()
        {
            return m_evToken;
        }

        private void SetMethodSemantics(MethodBuilder mdBuilder, MethodSemanticsAttributes semantics)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);

            m_type.ThrowIfCreated();
            RuntimeModuleBuilder module = m_module;
            RuntimeTypeBuilder.DefineMethodSemantics(
                new QCallModule(ref module),
                m_evToken,
                semantics,
                mdBuilder.MetadataToken);
        }

        protected override void SetAddOnMethodCore(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.AddOn);
        }

        protected override void SetRemoveOnMethodCore(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.RemoveOn);
        }

        protected override void SetRaiseMethodCore(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Fire);
        }

        protected override void AddOtherMethodCore(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
        }

        // Use this function if client decides to form the custom attribute blob themselves

        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute)
        {
            m_type.ThrowIfCreated();

            RuntimeTypeBuilder.DefineCustomAttribute(
                m_module,
                m_evToken,
                m_module.GetMethodMetadataToken(con),
                binaryAttribute);
        }

        // Use this function if client wishes to build CustomAttribute using CustomAttributeBuilder
        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder)
        {
            m_type.ThrowIfCreated();
            customBuilder.CreateCustomAttribute(m_module, m_evToken);
        }

        // These are package private so that TypeBuilder can access them.
        private string m_name;         // The name of the event
        private int m_evToken;      // The token of this event
        private RuntimeModuleBuilder m_module;
        private EventAttributes m_attributes;
        private RuntimeTypeBuilder m_type;
    }
}
