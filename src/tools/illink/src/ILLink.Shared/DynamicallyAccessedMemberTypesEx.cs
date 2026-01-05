// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Diagnostics.CodeAnalysis
{
    public static class DynamicallyAccessedMemberTypesEx
    {
        /// <summary>
        /// Specifies all non-public constructors, including those inherited from base classes.
        /// </summary>
        public const DynamicallyAccessedMemberTypes NonPublicConstructorsWithInherited = DynamicallyAccessedMemberTypes.NonPublicConstructors | (DynamicallyAccessedMemberTypes)0x4000;

        /// <summary>
        /// Specifies all non-public methods, including those inherited from base classes.
        /// </summary>
        public const DynamicallyAccessedMemberTypes NonPublicMethodsWithInherited = DynamicallyAccessedMemberTypes.NonPublicMethods | (DynamicallyAccessedMemberTypes)0x8000;

        /// <summary>
        /// Specifies all non-public fields, including those inherited from base classes.
        /// </summary>
        public const DynamicallyAccessedMemberTypes NonPublicFieldsWithInherited = DynamicallyAccessedMemberTypes.NonPublicFields | (DynamicallyAccessedMemberTypes)0x10000;

        /// <summary>
        /// Specifies all non-public nested types, including those inherited from base classes.
        /// </summary>
        public const DynamicallyAccessedMemberTypes NonPublicNestedTypesWithInherited = DynamicallyAccessedMemberTypes.NonPublicNestedTypes | (DynamicallyAccessedMemberTypes)0x20000;

        /// <summary>
        /// Specifies all non-public properties, including those inherited from base classes.
        /// </summary>
        public const DynamicallyAccessedMemberTypes NonPublicPropertiesWithInherited = DynamicallyAccessedMemberTypes.NonPublicProperties | (DynamicallyAccessedMemberTypes)0x40000;

        /// <summary>
        /// Specifies all non-public events, including those inherited from base classes.
        /// </summary>
        public const DynamicallyAccessedMemberTypes NonPublicEventsWithInherited = DynamicallyAccessedMemberTypes.NonPublicEvents | (DynamicallyAccessedMemberTypes)0x80000;

        /// <summary>
        /// Specifies all public constructors, including those inherited from base classes.
        /// </summary>
        public const DynamicallyAccessedMemberTypes PublicConstructorsWithInherited = DynamicallyAccessedMemberTypes.PublicConstructors | (DynamicallyAccessedMemberTypes)0x100000;

        /// <summary>
        /// Specifies all public nested types, including those inherited from base classes.
        /// </summary>
        public const DynamicallyAccessedMemberTypes PublicNestedTypesWithInherited = DynamicallyAccessedMemberTypes.PublicNestedTypes | (DynamicallyAccessedMemberTypes)0x200000;
    }
}
