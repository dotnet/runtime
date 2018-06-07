// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    /// This service may be provided by a designer loader to provide
    /// a way for the designer to fabricate new names for objects.
    /// If this service isn't available the designer will choose a 
    /// default implementation.
    /// </summary>
    public interface INameCreationService
    {
        /// <summary>
        /// Creates a new name that is unique to all the components
        /// in the given container. The name will be used to create
        /// an object of the given data type, so the service may
        /// derive a name from the data type's name. The container
        /// parameter can be null if no container search is needed.
        /// </summary>
        string CreateName(IContainer container, Type dataType);

        /// <summary>
        /// Determines if the given name is valid. A name 
        /// creation service may have rules defining a valid
        /// name, and this method allows the service to enforce
        /// those rules.
        /// </summary>
        bool IsValidName(string name);

        /// <summary>
        /// Determines if the given name is valid. A name 
        /// creation service may have rules defining a valid
        /// name, and this method allows the service to enforce
        /// those rules. It is similar to IsValidName, except
        /// that this method will throw an exception if the
        /// name is invalid. This allows implementors to provide
        /// rich information in the exception message.
        /// </summary>
        void ValidateName(string name);
    }
}

