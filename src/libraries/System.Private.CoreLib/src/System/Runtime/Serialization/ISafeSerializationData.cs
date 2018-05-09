// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    //
    // #SafeSerialization
    // 
    // Types which are serializable via the ISerializable interface have a problem when it comes to allowing
    // transparent subtypes which can allow themselves to serialize since the GetObjectData method is
    // SecurityCritical.
    // 
    // For instance, System.Exception implements ISerializable, however it is also desirable to have
    // transparent exceptions with their own fields that need to be serialized.  (For instance, in transparent
    // assemblies such as the DLR and F#, or even in partial trust application code).  Since overriding
    // GetObjectData requires that the overriding method be security critical, this won't work directly.
    //
    // SafeSerializationManager solves this problem by allowing any partial trust code to contribute
    // individual chunks of serializable data to be included in the serialized version of the derived class. 
    // These chunks are then deserialized back out of the serialized type and notified that they should
    // populate the fields of the deserialized object when serialization is complete.  This allows partial
    // trust or transparent code to participate in serialization of an ISerializable type without having to
    // override GetObjectData or implement the ISerializable constructor.
    //
    // On the serialization side, SafeSerializationManager has an event SerializeObjectState which it will
    // fire in response to serialization in order to gather the units of serializable data that should be
    // stored with the rest of the object during serialization.  Methods which respond to these events
    // create serializable objects which implement the ISafeSerializationData interface and add them to the
    // collection of other serialized data by calling AddSerializedState on the SafeSerializationEventArgs
    // passed into the event.
    // 
    // By using an event rather than a virtual method on the base ISerializable object, we allow multiple
    // potentially untrusted subclasses to participate in serialization, without each one having to ensure
    // that it calls up to the base type in order for the whole system to work.  (For instance Exception :
    // TrustedException : UntrustedException, in this scenario UntrustedException would be able to override
    // the virtual method an prevent TrustedException from ever seeing the method call, either accidentally
    // or maliciously).
    // 
    // Further, by only allowing additions of new chunks of serialization state rather than exposing the
    // whole underlying list, we avoid exposing potentially sensitive serialized state to any of the
    // potentially untrusted subclasses.
    // 
    // At deserialization time, SafeSerializationManager performs the reverse operation.  It deserializes the
    // chunks of serialized state, and then notifies them that the object they belong to is deserialized by
    // calling their CompleteSerialization method. In repsonse to this call, the state objects populate the
    // fields of the object being deserialized with the state that they held.
    // 
    // From a security perspective, the chunks of serialized state can only contain data that the specific
    // subclass itself had access to read (otherwise it wouldn't be able to populate the type with that
    // data), as opposed to having access to far more data in the SerializationInfo that GetObjectData uses.
    // Similarly, at deserialization time, the serialized state can only modify fields that the type itself
    // has access to (again, as opposed to the full SerializationInfo which could be modified).
    // 
    // Individual types which wish to participate in safe serialization do so by containing an instance of a
    // SafeSerializationManager and exposing its serialization event.  During GetObjectData, the
    // SafeSerializationManager is serialized just like any other field of the containing type.  However, at
    // the end of serialization it is called back one last time to CompleteSerialization.
    // 
    // In CompleteSerialization, if the SafeSerializationManager detects that it has extra chunks of
    // data to handle, it substitutes the root type being serialized (formerly the real type hosting the
    // SafeSerializationManager) with itself.  This allows it to gain more control over the deserialization
    // process.  It also saves away an extra bit of state in the serialization info indicating the real type
    // of object that should be recreated during deserialization.
    //
    // At this point the serialized state looks like this:
    //   Data:
    //     realSerializedData1
    //       ...
    //     realSerializedDataN
    //     safeSerializationData     -> this is the serialization data member of the parent type
    //       _serializedState        -> list of saved serialized states from subclasses responding to the safe
    //                                  serialization event
    //     RealTypeSerializationName -> type which is using safe serialization
    //   Type:
    //     SafeSerializationManager
    //
    //  That is, the serialized data claims to be of type SafeSerializationManager, however contains only the
    //  data from the real object being serialized along with one bit of safe serialization metadata.
    //  
    //  At deserialization time, since the serialized data claims to be of type SafeSerializationManager, the
    //  root object being created is an instance of the SafeSerializationManager class.  However, it detects
    //  that this isn't a real SafeSerializationManager (by looking for the real type field in the metadata),
    //  and simply saves away the SerializationInfo and the real type being deserialized.
    //  
    //  Since SafeSerializationManager implements IObjectReference, the next step of deserialization is the
    //  GetRealObject callback.  This callback is the one responsible for getting the
    //  SafeSerializationManager out of the way and instead creating an instance of the actual type which was
    //  serialized.
    //  
    //  It does this by first creating an instance of the real type being deserialzed (saved away in the
    //  deserialzation constructor), but not running any of its constructors.  Instead, it walks the
    //  inheritance hierarchy (moving toward the most derived type) looking for the last full trust type to
    //  implement the standard ISerializable constructor before any type does not implement the constructor. 
    //  It is this last type's deserialization constructor which is then invoked, passing in the saved
    //  SerializationInfo.  Once the constructors are run, we return this object as the real deserialized
    //  object.
    //  
    //  The reason that we do this walk is so that ISerializable types can protect themselves from malicious
    //  input during deserialization by making their deserialization constructors unavailable to partial
    //  trust code.  By not requiring every type have a copy of this constructor, partial trust code can
    //  participate in safe serialization and not be required to have access to the parent's constructor. 
    //  
    //  It should be noted however, that this heuristic means that if a full trust type does derive from
    //  a transparent or partial trust type using this safe serialization mechanism, that full trust type
    //  will not have its constructor called. Further, the protection of not invoking partial trust
    //  deserialization constructors only comes into play if SafeSerializationManager is in control of
    //  deserialization, which means there must be at least one (even empty) safe serialization event
    //  handler registered.
    //  
    //  Another interesting note is that at this point there are now two SafeSerializationManagers alive for
    //  this deserialization.  The first object is the one which is controlling the deserialization and was
    //  created as the root object of the deserialization.  The second one is the object which contains the
    //  serialized data chunks and is a data member of the real object being deserialized.  For this reason,
    //  the data objects cannot be notified that the deserialization is complete during GetRealObject since
    //  the ISafeSerializationData objects are not members of the active SafeSerializationManager instance.
    //  
    //  The next step is the OnDeserialized callback, which comes to SafeSerializableObject since it was
    //  pretending to be the root object of the deserialization.  It responds to this callback by calling
    //  any existing OnDeserialized callback on the real type that was deserialized.
    //  
    //  The real type needs to call its data member SafeSerializationData object's CompleteDeserialization
    //  method in response to the OnDeserialized call.  This CompleteDeserialization call will then iterate
    //  through the ISafeSerializationData objects calling each of their CompleteDeserialization methods so
    //  that they can plug the nearly-complete object with their saved data.
    //  
    //  The reason for having a new ISafeSerializationData interface which is basically identical to
    //  IDeserializationCallback is that IDeserializationCallback will be called on the stored data chunks
    //  by the serialization code when they are deserialized, and that's not a desirable behavior. 
    //  Essentially, we need to change the meaning of the object parameter to mean "parent object which
    //  participated in safe serialization", rather than "this object".
    //  
    //  Implementing safe serialization on an ISerialiable type is relatively straight forward.  (For an
    //  example, see System.Exception):
    //  
    //    1. Include a data member of type SafeSerializationManager:
    //  
    //       private SafeSerializationManager _safeSerializationManager;
    //     
    //    2. Add a protected SerializeObjectState event, which passes through to the SafeSerializationManager:
    //  
    //       protected event EventHandler<SafeSerializationEventArgs> SerializeObjectState
    //       {
    //           add { _safeSerializationManager.SerializeObjectState += value; }
    //           remove { _safeSerializationManager.SerializeObjectState -= value; }
    //       }
    //
    //    3. Serialize the safe serialization object in GetObjectData, and call its CompleteSerialization method:
    //  
    //       {
    //           info.AddValue("_safeSerializationManager", _safeSerializationManager, typeof(SafeSerializationManager));
    //           _safeSerializationManager.CompleteSerialization(this, info, context);
    //       }
    //
    //    4. Add an OnDeserialized handler if one doesn't already exist, and call CompleteDeserialization in it:
    //  
    //       [OnDeserialized]
    //       private void OnDeserialized(StreamingContext context)
    //       {
    //           _safeSerializationManager.CompleteDeserialization(this);
    //       }
    //
    // On the client side, using safe serialization is also pretty easy.  For example:
    // 
    //   [Serializable]
    //   public class TransparentException : Exception
    //   {
    //       [Serializable]
    //       private struct TransparentExceptionState : ISafeSerializationData
    //       {
    //           public string _extraData;
    //
    //           void ISafeSerializationData.CompleteDeserialization(object obj)
    //           {
    //               TransparentException exception = obj as TransparentException;
    //               exception._state = this;
    //           }
    //       }
    //
    //       [NonSerialized]
    //       private TransparentExceptionState _state = new TransparentExceptionState();
    //
    //       public TransparentException()
    //       {
    //           SerializeObjectState += delegate(object exception, SafeSerializationEventArgs eventArgs)
    //           {
    //               eventArgs.AddSerializedState(_state);
    //           };
    //       }
    //
    //       public string ExtraData
    //       {
    //           get { return _state._extraData; }
    //           set { _state._extraData = value; }
    //       }
    //   }
    // 

    // Interface to be supported by objects which are stored in safe serialization stores
    public interface ISafeSerializationData
    {
        // CompleteDeserialization is called when the object to which the extra serialized data was attached
        // has completed its deserialization, and now needs to be populated with the extra data stored in
        // this object.
        void CompleteDeserialization(object deserialized);
    }
}
