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
    public sealed class EventBuilder
    {
        // Constructs a EventBuilder.
        //
        internal EventBuilder(
            ModuleBuilder mod,                    // the module containing this EventBuilder
            string name,                    // Event name
            EventAttributes attr,                    // event attribute such as Public, Private, and Protected defined above
                                                     // int            eventType,                // event type
            TypeBuilder type,                    // containing type
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
            ModuleBuilder module = m_module;
            TypeBuilder.DefineMethodSemantics(
                new QCallModule(ref module),
                m_evToken,
                semantics,
                mdBuilder.MetadataToken);
        }

        public void SetAddOnMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.AddOn);
        }

        public void SetRemoveOnMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.RemoveOn);
        }

        public void SetRaiseMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Fire);
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
        }

        // Use this function if client decides to form the custom attribute blob themselves

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            m_type.ThrowIfCreated();

            TypeBuilder.DefineCustomAttribute(
                m_module,
                m_evToken,
                m_module.GetConstructorToken(con),
                binaryAttribute);
        }

        // Use this function if client wishes to build CustomAttribute using CustomAttributeBuilder
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            m_type.ThrowIfCreated();
            customBuilder.CreateCustomAttribute(m_module, m_evToken);
        }

        // These are package private so that TypeBuilder can access them.
        private string m_name;         // The name of the event
        private int m_evToken;      // The token of this event
        private ModuleBuilder m_module;
        private EventAttributes m_attributes;
        private TypeBuilder m_type;
    }
}
