// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    /// This interface is passed to a designer serializer to provide
    /// assistance in the serialization process.
    /// </summary>
    public interface IDesignerSerializationManager : IServiceProvider
    {
        /// <summary>
        /// The Context property provides a user-defined storage area
        /// implemented as a stack. This storage area is a useful way
        /// to provide communication across serializers, as serialization
        /// is a generally hierarchical process.
        /// </summary>
        ContextStack Context { get; }

        /// <summary>
        /// The Properties property provides a set of custom properties
        /// the serialization manager may surface. The set of properties
        /// exposed here is defined by the implementor of 
        /// IDesignerSerializationManager. 
        /// </summary>
        PropertyDescriptorCollection Properties { get; }

        /// <summary>
        /// ResolveName event. This event
        /// is raised when GetName is called, but the name is not found
        /// in the serialization manager's name table. It provides a 
        /// way for a serializer to demand-create an object so the serializer
        /// does not have to order object creation by dependency. This
        /// delegate is cleared immediately after serialization or deserialization
        /// is complete.
        /// </summary>
        event ResolveNameEventHandler ResolveName;

        /// <summary>
        /// This event is raised when serialization or deserialization
        /// has been completed. Generally, serialization code should
        /// be written to be stateless. Should some sort of state
        /// be necessary to maintain, a serializer can listen to
        /// this event to know when that state should be cleared.
        /// An example of this is if a serializer needs to write
        /// to another file, such as a resource file. In this case
        /// it would be inefficient to design the serializer
        /// to close the file when finished because serialization of
        /// an object graph generally requires several serializers.
        /// The resource file would be opened and closed many times.
        /// Instead, the resource file could be accessed through
        /// an object that listened to the SerializationComplete
        /// event, and that object could close the resource file
        /// at the end of serialization.
        /// </summary>
        event EventHandler SerializationComplete;

        /// <summary>
        /// This method adds a custom serialization provider to the 
        /// serialization manager. A custom serialization provider will
        /// get the opportunity to return a serializer for a data type
        /// before the serialization manager looks in the type's
        /// metadata. 
        /// </summary>
        void AddSerializationProvider(IDesignerSerializationProvider provider);

        /// <summary>                
        /// Creates an instance of the given type and adds it to a collection
        /// of named instances. Objects that implement IComponent will be
        /// added to the design time container if addToContainer is true.
        /// </summary>
        object CreateInstance(Type type, ICollection arguments, string name, bool addToContainer);

        /// <summary>
        /// Retrieves an instance of a created object of the given name, or
        /// null if that object does not exist.
        /// </summary>
        object GetInstance(string name);

        /// <summary>
        /// Retrieves a name for the specified object, or null if the object
        /// has no name.
        /// </summary>
        string GetName(object value);

        /// <summary>
        /// Retrieves a serializer of the requested type for the given
        /// object type.
        /// </summary>
        object GetSerializer(Type objectType, Type serializerType);

        /// <summary>
        /// Retrieves a type of the given name.
        /// </summary>
        Type GetType(string typeName);

        /// <summary>
        /// Removes a previously added serialization provider.
        /// </summary>
        void RemoveSerializationProvider(IDesignerSerializationProvider provider);

        /// <summary>
        /// Reports a non-fatal error in serialization. The serialization
        /// manager may implement a logging scheme to alert the caller
        /// to all non-fatal errors at once. If it doesn't, it should
        /// immediately throw in this method, which should abort
        /// serialization. 
        /// Serialization may continue after calling this function.
        /// </summary>
        void ReportError(object errorInformation);

        /// <summary>
        /// Provides a way to set the name of an existing object.
        /// This is useful when it is necessary to create an 
        /// instance of an object without going through CreateInstance.
        /// An exception will be thrown if you try to rename an existing
        /// object or if you try to give a new object a name that
        /// is already taken.
        /// </summary>
        void SetName(object instance, string name);
    }
}

