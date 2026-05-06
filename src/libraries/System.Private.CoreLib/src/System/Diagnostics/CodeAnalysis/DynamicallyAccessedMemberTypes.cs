// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Specifies the types of members that are dynamically accessed.
    ///
    /// This enumeration has a <see cref="FlagsAttribute"/> attribute that allows a
    /// bitwise combination of its member values.
    /// </summary>
    [Flags]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    enum DynamicallyAccessedMemberTypes
    {
        /// <summary>
        /// Specifies no members.
        /// </summary>
        None = 0,

        /// <summary>
        /// Specifies the default, parameterless public constructor.
        /// </summary>
        PublicParameterlessConstructor = 0x0001,

        /// <summary>
        /// Specifies all public constructors.
        /// </summary>
        PublicConstructors = 0x0002 | PublicParameterlessConstructor,

        /// <summary>
        /// Specifies all non-public constructors.
        /// </summary>
        NonPublicConstructors = 0x0004,

        /// <summary>
        /// Specifies all public methods.
        /// </summary>
        PublicMethods = 0x0008,

        /// <summary>
        /// Specifies all non-public methods.
        /// </summary>
        NonPublicMethods = 0x0010,

        /// <summary>
        /// Specifies all public fields.
        /// </summary>
        PublicFields = 0x0020,

        /// <summary>
        /// Specifies all non-public fields.
        /// </summary>
        NonPublicFields = 0x0040,

        /// <summary>
        /// Specifies all public nested types.
        /// </summary>
        PublicNestedTypes = 0x0080,

        /// <summary>
        /// Specifies all non-public nested types.
        /// </summary>
        NonPublicNestedTypes = 0x0100,

        /// <summary>
        /// Specifies all public properties.
        /// </summary>
        PublicProperties = 0x0200,

        /// <summary>
        /// Specifies all non-public properties.
        /// </summary>
        NonPublicProperties = 0x0400,

        /// <summary>
        /// Specifies all public events.
        /// </summary>
        PublicEvents = 0x0800,

        /// <summary>
        /// Specifies all non-public events.
        /// </summary>
        NonPublicEvents = 0x1000,

        /// <summary>
        /// Specifies all interfaces implemented by the type.
        /// </summary>
        Interfaces = 0x2000,

        /// <summary>
        /// Specifies all non-public constructors, including those inherited from base classes.
        /// </summary>
        NonPublicConstructorsWithInherited = NonPublicConstructors | 0x4000,

        /// <summary>
        /// Specifies all non-public methods, including those inherited from base classes.
        /// </summary>
        NonPublicMethodsWithInherited = NonPublicMethods | 0x8000,

        /// <summary>
        /// Specifies all non-public fields, including those inherited from base classes.
        /// </summary>
        NonPublicFieldsWithInherited = NonPublicFields | 0x10000,

        /// <summary>
        /// Specifies all non-public nested types, including those inherited from base classes.
        /// </summary>
        NonPublicNestedTypesWithInherited = NonPublicNestedTypes | 0x20000,

        /// <summary>
        /// Specifies all non-public properties, including those inherited from base classes.
        /// </summary>
        NonPublicPropertiesWithInherited = NonPublicProperties | 0x40000,

        /// <summary>
        /// Specifies all non-public events, including those inherited from base classes.
        /// </summary>
        NonPublicEventsWithInherited = NonPublicEvents | 0x80000,

        /// <summary>
        /// Specifies all public constructors, including those inherited from base classes.
        /// </summary>
        PublicConstructorsWithInherited = PublicConstructors | 0x100000,

        /// <summary>
        /// Specifies all public nested types, including those inherited from base classes.
        /// </summary>
        PublicNestedTypesWithInherited = PublicNestedTypes | 0x200000,

        /// <summary>
        /// Specifies all constructors, including those inherited from base classes.
        /// </summary>
        AllConstructors = PublicConstructorsWithInherited | NonPublicConstructorsWithInherited,

        /// <summary>
        /// Specifies all methods, including those inherited from base classes.
        /// </summary>
        AllMethods = PublicMethods | NonPublicMethodsWithInherited,

        /// <summary>
        /// Specifies all fields, including those inherited from base classes.
        /// </summary>
        AllFields = PublicFields | NonPublicFieldsWithInherited,

        /// <summary>
        /// Specifies all nested types, including those inherited from base classes.
        /// </summary>
        AllNestedTypes = PublicNestedTypesWithInherited | NonPublicNestedTypesWithInherited,

        /// <summary>
        /// Specifies all properties, including those inherited from base classes.
        /// </summary>
        AllProperties = PublicProperties | NonPublicPropertiesWithInherited,

        /// <summary>
        /// Specifies all events, including those inherited from base classes.
        /// </summary>
        AllEvents = PublicEvents | NonPublicEventsWithInherited,

        /// <summary>
        /// Specifies all members.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        All = ~None
    }
}
