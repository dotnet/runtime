// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.Serialization
{
    [Obsolete(Obsoletions.LegacyFormatterMessage, DiagnosticId = Obsoletions.LegacyFormatterDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public class ObjectManager
    {
        private const int DefaultInitialSize = 16;
        private const int MaxArraySize = 0x100000; //MUST BE A POWER OF 2!
        private const int ArrayMask = MaxArraySize - 1;
        private const int MaxReferenceDepth = 100;

        private const string ObjectManagerUnreferencedCodeMessage = "ObjectManager is not trim compatible because the type of objects being managed cannot be statically discovered.";

        private static readonly FieldInfo s_nullableValueField = typeof(Nullable<>).GetField("value", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private DeserializationEventHandler? _onDeserializationHandler;
        private SerializationEventHandler? _onDeserializedHandler;

        internal ObjectHolder[] _objects;
        internal object? _topObject;
        internal ObjectHolderList? _specialFixupObjects; //This is IObjectReference, ISerializable, or has a Surrogate.
        internal long _fixupCount;
        internal readonly ISurrogateSelector? _selector;
        internal readonly StreamingContext _context;

        public ObjectManager(ISurrogateSelector? selector, StreamingContext context)
        {
            _objects = new ObjectHolder[DefaultInitialSize];
            _selector = selector;
            _context = context;
        }

        private bool CanCallGetType(object? obj) => true;

        internal object? TopObject
        {
            get { return _topObject; }
            set { _topObject = value; }
        }

        internal ObjectHolderList SpecialFixupObjects =>
            _specialFixupObjects ??= new ObjectHolderList();

        internal ObjectHolder? FindObjectHolder(long objectID)
        {
            // The  index of the bin in which we live is rightmost n bits of the objectID.
            int index = (int)(objectID & ArrayMask);
            if (index >= _objects.Length)
            {
                return null;
            }

            // Find the bin in which we live.
            ObjectHolder? temp = _objects[index];

            // Walk the chain in that bin.  Return the ObjectHolder if we find it, otherwise
            // return null.
            while (temp != null)
            {
                if (temp._id == objectID)
                {
                    return temp;
                }
                temp = temp._next;
            }

            return temp;
        }

        internal ObjectHolder FindOrCreateObjectHolder(long objectID)
        {
            ObjectHolder? holder = FindObjectHolder(objectID);
            if (holder == null)
            {
                holder = new ObjectHolder(objectID);
                AddObjectHolder(holder);
            }
            return holder;
        }

        private void AddObjectHolder(ObjectHolder holder)
        {
            Debug.Assert(holder != null, "holder!=null");
            Debug.Assert(holder._id >= 0, "holder.m_id>=0");

            //If the id that we need to place is greater than our current length, and less
            //than the maximum allowable size of the array.  We need to double the size
            //of the array.  If the array has already reached it's maximum allowable size,
            //we chain elements off of the buckets.
            if (holder._id >= _objects.Length && _objects.Length != MaxArraySize)
            {
                int newSize = MaxArraySize;

                if (holder._id < (MaxArraySize / 2))
                {
                    newSize = (_objects.Length * 2);

                    //Keep doubling until we're larger than our target size.
                    //We could also do this with log operations, but that would
                    //be slower than the brute force approach.
                    while (newSize <= holder._id && newSize < MaxArraySize)
                    {
                        newSize *= 2;
                    }

                    if (newSize > MaxArraySize)
                    {
                        newSize = MaxArraySize;
                    }
                }

                ObjectHolder[] temp = new ObjectHolder[newSize];
                Array.Copy(_objects, temp, _objects.Length);
                _objects = temp;
            }

            //Find the bin in which we live and make this new element the first element in the bin.
            int index = (int)(holder._id & ArrayMask);

            ObjectHolder tempHolder = _objects[index];
            holder._next = tempHolder;
            _objects[index] = holder;
        }

        private bool GetCompletionInfo(FixupHolder fixup, [NotNullWhen(true)] out ObjectHolder? holder, out object member, bool bThrowIfMissing)
        {
            //Set the member id (String or MemberInfo) for the member being fixed up.
            member = fixup._fixupInfo;

            //Find the object required for the fixup.  Throw if we can't find it.
            holder = FindObjectHolder(fixup._id);

            if (holder == null || holder.CanObjectValueChange || holder.ObjectValue == null)
            {
                if (bThrowIfMissing)
                {
                    if (holder == null)
                    {
                        throw new SerializationException(SR.Format(SR.Serialization_NeverSeen, fixup._id));
                    }
                    if (holder.IsIncompleteObjectReference)
                    {
                        throw new SerializationException(SR.Format(SR.Serialization_IORIncomplete, fixup._id));
                    }
                    throw new SerializationException(SR.Format(SR.Serialization_ObjectNotSupplied, fixup._id));
                }
                return false;
            }

            // CompletelyFixed is our poorly named property which indicates if something requires a SerializationInfo fixup
            // or is an incomplete object reference.  We have this particular branch to handle valuetypes which implement
            // ISerializable.  In that case, we can't do any fixups on them later, so we need to delay the fixups further.
            if (!holder.CompletelyFixed)
            {
                if (holder.ObjectValue != null && holder.ObjectValue is ValueType)
                {
                    SpecialFixupObjects.Add(holder);
                    return false;
                }
            }

            return true;
        }

        [RequiresUnreferencedCode(ObjectManagerUnreferencedCodeMessage)]
        private void FixupSpecialObject(ObjectHolder holder)
        {
            ISurrogateSelector? uselessSelector = null;

            Debug.Assert(holder.RequiresSerInfoFixup, "[ObjectManager.FixupSpecialObject]holder.HasSurrogate||holder.HasISerializable");
            if (holder.HasSurrogate)
            {
                ISerializationSurrogate? surrogate = holder.Surrogate;
                Debug.Assert(surrogate != null, "surrogate!=null");
                Debug.Assert(holder.SerializationInfo != null);
                object? returnValue = surrogate.SetObjectData(holder.ObjectValue!, holder.SerializationInfo, _context, uselessSelector);
                if (returnValue != null)
                {
                    if (!holder.CanSurrogatedObjectValueChange && returnValue != holder.ObjectValue)
                    {
                        throw new SerializationException(SR.Format(SR.Serialization_NotCyclicallyReferenceableSurrogate, surrogate.GetType().FullName));
                    }
                    holder.SetObjectValue(returnValue, this);
                }
                holder._surrogate = null;
                holder.SetFlags();
            }
            else
            {
                //Set the object data
                Debug.Assert(holder.ObjectValue is ISerializable, "holder.m_object is ISerializable");
                CompleteISerializableObject(holder.ObjectValue, holder.SerializationInfo, _context);
            }
            //Clear anything that we know that we're not going to need.
            holder.SerializationInfo = null;
            holder.RequiresSerInfoFixup = false;

            // For value types, fixups would have been done. So the newly fixed object must be copied
            // to its container.
            if (holder.RequiresValueTypeFixup && holder.ValueTypeFixupPerformed)
            {
                DoValueTypeFixup(null, holder, holder.ObjectValue);
            }
            DoNewlyRegisteredObjectFixups(holder);
        }

        /// <summary>
        /// Unfortunately, an ObjectReference could actually be a reference to another
        /// object reference and we don't know how far we have to tunnel until we can find the real object.  While
        /// we're still getting instances of IObjectReference back and we're still getting new objects, keep calling
        /// GetRealObject.  Once we've got the new object, take care of all of the fixups
        /// that we can do now that we've got it.
        /// </summary>
        /// <param name="holder"></param>
        private bool ResolveObjectReference(ObjectHolder holder)
        {
            object? tempObject;
            Debug.Assert(holder.IsIncompleteObjectReference, "holder.IsIncompleteObjectReference");

            //In the pathological case, an Object implementing IObjectReference could return a reference
            //to a different object which implements IObjectReference.  This makes us vulnerable to a
            //denial of service attack and stack overflow.  If the depthCount becomes greater than
            //MaxReferenceDepth, we'll throw a SerializationException.
            int depthCount = 0;

            //We wrap this in a try/catch block to handle the case where we're trying to resolve a chained
            //list of object reference (e.g. an IObjectReference can't resolve itself without some information
            //that's currently missing from the graph).  We'll catch the NullReferenceException and come back
            //and try again later.  The downside of this scheme is that if the object actually needed to throw
            //a NullReferenceException, it's being caught and turned into a SerializationException with a
            //fairly cryptic message.
            try
            {
                do
                {
                    tempObject = holder.ObjectValue;
                    Debug.Assert(holder.ObjectValue != null);
                    holder.SetObjectValue(((IObjectReference)(holder.ObjectValue)).GetRealObject(_context), this);
                    //The object didn't yet have enough information to resolve the reference, so we'll
                    //return false and the graph walker should call us back again after more objects have
                    //been resolved.
                    if (holder.ObjectValue == null)
                    {
                        holder.SetObjectValue(tempObject, this);
                        return false;
                    }
                    if (depthCount++ == MaxReferenceDepth)
                    {
                        throw new SerializationException(SR.Serialization_TooManyReferences);
                    }
                } while ((holder.ObjectValue is IObjectReference) && (tempObject != holder.ObjectValue));
            }
            catch (NullReferenceException)
            {
                return false;
            }

            holder.IsIncompleteObjectReference = false;
            DoNewlyRegisteredObjectFixups(holder);
            return true;
        }

        /*===============================DoValueTypeFixup===============================
        **Arguments:
        ** memberToFix -- the member in the object contained in holder being fixed up.
        ** holder -- the ObjectHolder for the object (a value type in this case) being completed.
        ** value  -- the data to set into the field.
        ==============================================================================*/
        private bool DoValueTypeFixup(FieldInfo? memberToFix, ObjectHolder holder, object? value)
        {
            var fieldsTemp = new FieldInfo[4];
            FieldInfo[] fields;
            int currentFieldIndex = 0;
            int[]? arrayIndex = null;
            ValueTypeFixupInfo currFixup;
            object? fixupObj = holder.ObjectValue;

            Debug.Assert(holder != null, "[TypedReferenceBuilder.ctor]holder!=null");
            Debug.Assert(holder.RequiresValueTypeFixup, "[TypedReferenceBuilder.ctor]holder.RequiresValueTypeFixup");

            //In order to get a TypedReference, we need to get a list of all of the FieldInfos to
            //create the path from our outermost containing object down to the actual field which
            //we'd like to set.  This loop is used to build up that list.
            while (holder.RequiresValueTypeFixup)
            {
                //Enlarge the array if required (this is actually fairly unlikely as it would require that we
                //be nested more than 4 deep.
                if ((currentFieldIndex + 1) >= fieldsTemp.Length)
                {
                    var temp = new FieldInfo[fieldsTemp.Length * 2];
                    Array.Copy(fieldsTemp, temp, fieldsTemp.Length);
                    fieldsTemp = temp;
                }

                //Get the fixup information.  If we have data for our parent field, add it to our list
                //and continue the walk up to find the next outermost containing object.  We cache the
                //object that we have.  In most cases, we could have just grabbed it after this loop finished.
                //However, if the outermost containing object is an array, we need the object one further
                //down the chain, so we have to do a lot of caching.
                currFixup = holder.ValueFixup!;
                fixupObj = holder.ObjectValue;  //Save the most derived
                if (currFixup.ParentField != null)
                {
                    FieldInfo parentField = currFixup.ParentField;

                    ObjectHolder? tempHolder = FindObjectHolder(currFixup.ContainerID);
                    Debug.Assert(tempHolder != null);
                    if (tempHolder.ObjectValue == null)
                    {
                        break;
                    }

                    FieldInfo? nullableValueField = GetNullableValueField(parentField.FieldType);
                    if (nullableValueField != null)
                    {
                        fieldsTemp[currentFieldIndex] = nullableValueField;
                        currentFieldIndex++;
                    }

                    fieldsTemp[currentFieldIndex] = parentField;
                    holder = tempHolder;
                    currentFieldIndex++;
                }
                else
                {
                    //If we find an index into an array, save that information.
                    Debug.Assert(currFixup.ParentIndex != null, "[ObjectManager.DoValueTypeFixup]currFixup.ParentIndex!=null");
                    holder = FindObjectHolder(currFixup.ContainerID)!; //find the array to fix.
                    arrayIndex = currFixup.ParentIndex;
                    break;
                }
            }

            //If the outermost container isn't an array, we need to grab it.  Otherwise, we just need to hang onto
            //the boxed object that we already grabbed.  We'll assign the boxed object back into the array as the
            //last step.
            if (!(holder.ObjectValue is Array) && holder.ObjectValue != null)
            {
                fixupObj = holder.ObjectValue;
                Debug.Assert(fixupObj != null, "[ObjectManager.DoValueTypeFixup]FixupObj!=null");
            }

            if (currentFieldIndex != 0)
            {
                //MakeTypedReference requires an array of exactly the correct size that goes from the outermost object
                //in to the innermost field.  We currently have an array of arbitrary size that goes from the innermost
                //object outwards.  We create an array of the right size and do the copy.
                fields = new FieldInfo[currentFieldIndex];
                for (int i = 0; i < currentFieldIndex; i++)
                {
                    FieldInfo? fieldInfo = fieldsTemp[(currentFieldIndex - 1 - i)];
                    SerializationFieldInfo? serInfo = fieldInfo as SerializationFieldInfo;
                    fields[i] = serInfo == null ? fieldInfo : serInfo.FieldInfo;
                }

                Debug.Assert(fixupObj != null, "[ObjectManager.DoValueTypeFixup]fixupObj!=null");
                //Make the TypedReference and use it to set the value.
                TypedReference typedRef = TypedReference.MakeTypedReference(fixupObj, fields);
                if (memberToFix != null)
                {
                    memberToFix.SetValueDirect(typedRef, value!);
                }
                else
                {
                    TypedReference.SetTypedReference(typedRef, value);
                }
            }
            else if (memberToFix != null)
            {
                FormatterServices.SerializationSetValue(memberToFix, fixupObj, value);
            }

            //If we have an array index, it means that our outermost container was an array.  We don't have
            //any way to build a TypedReference into an array, so we'll use the array functions to set the value.
            if (arrayIndex != null && holder.ObjectValue != null)
            {
                ((Array)(holder.ObjectValue)).SetValue(fixupObj, arrayIndex);
            }

            return true;
        }

        private static FieldInfo? GetNullableValueField(Type type)
        {
            if (Nullable.GetUnderlyingType(type) != null)
            {
                return (FieldInfo)type.GetMemberWithSameMetadataDefinitionAs(s_nullableValueField);
            }

            return null;
        }

        internal void CompleteObject(ObjectHolder holder, bool bObjectFullyComplete)
        {
            FixupHolderList? fixups = holder._missingElements;
            FixupHolder? currentFixup;
            SerializationInfo? si;
            object? fixupInfo;
            ObjectHolder? tempObjectHolder;
            int fixupsPerformed = 0;

            Debug.Assert(holder != null, "[ObjectManager.CompleteObject]holder.m_object!=null");
            if (holder.ObjectValue == null)
            {
                throw new SerializationException(SR.Format(SR.Serialization_MissingObject, holder._id));
            }

            if (fixups == null)
            {
                return;
            }
            //If either one of these conditions is true, we need to update the data in the
            //SerializationInfo before calling SetObjectData.
            if (holder.HasSurrogate || holder.HasISerializable)
            {
                si = holder._serInfo;
                if (si == null)
                {
                    throw new SerializationException(SR.Serialization_InvalidFixupDiscovered);
                }

                //Walk each of the fixups and complete the name-value pair in the SerializationInfo.
                if (fixups != null)
                {
                    for (int i = 0; i < fixups._count; i++)
                    {
                        if (fixups._values[i] == null)
                        {
                            continue;
                        }
                        Debug.Assert(fixups._values[i]!._fixupType == FixupHolder.DelayedFixup, "fixups.m_values[i].m_fixupType==FixupHolder.DelayedFixup");
                        if (GetCompletionInfo(fixups._values[i]!, out tempObjectHolder, out fixupInfo, bObjectFullyComplete))
                        {
                            //Walk the SerializationInfo and find the member needing completion.  All we have to do
                            //at this point is set the member into the Object
                            object? holderValue = tempObjectHolder.ObjectValue;
                            Debug.Assert(holderValue != null);
                            if (CanCallGetType(holderValue))
                            {
                                si.UpdateValue((string)fixupInfo, holderValue, holderValue.GetType());
                            }
                            else
                            {
                                si.UpdateValue((string)fixupInfo, holderValue, typeof(MarshalByRefObject));
                            }
                            //Decrement our total number of fixups left to do.
                            fixupsPerformed++;
                            fixups._values[i] = null;
                            if (!bObjectFullyComplete)
                            {
                                holder.DecrementFixupsRemaining(this);
                                tempObjectHolder.RemoveDependency(holder._id);
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < fixups._count; i++)
                {
                    currentFixup = fixups._values[i];
                    if (currentFixup == null)
                    {
                        continue;
                    }
                    if (GetCompletionInfo(currentFixup, out tempObjectHolder, out fixupInfo, bObjectFullyComplete))
                    {
                        // Check to make sure we are not both reachable from the topObject
                        // and there was a typeloadexception
                        if (tempObjectHolder.TypeLoadExceptionReachable)
                        {
                            holder.TypeLoadException = tempObjectHolder.TypeLoadException;
                            // If the holder is both reachable and typeloadexceptionreachable
                            // throw an exception with the type name
                            if (holder.Reachable)
                            {
                                throw new SerializationException(SR.Format(SR.Serialization_TypeLoadFailure, holder.TypeLoadException!.TypeName));
                            }
                        }

                        // If the current holder is reachable, mark the dependant reachable as well
                        if (holder.Reachable)
                        {
                            tempObjectHolder.Reachable = true;
                        }

                        //There are two types of fixups that we could be doing: array or member.
                        //Delayed Fixups should be handled by the above branch.
                        switch (currentFixup._fixupType)
                        {
                            case FixupHolder.ArrayFixup:
                                Debug.Assert(holder.ObjectValue is Array, "holder.ObjectValue is Array");
                                if (holder.RequiresValueTypeFixup)
                                {
                                    throw new SerializationException(SR.Serialization_ValueTypeFixup);
                                }
                                else
                                {
                                    ((Array)(holder.ObjectValue)).SetValue(tempObjectHolder.ObjectValue, ((int[])fixupInfo));
                                }
                                break;
                            case FixupHolder.MemberFixup:
                                Debug.Assert(fixupInfo is MemberInfo, "fixupInfo is MemberInfo");
                                //Fixup the member directly.
                                MemberInfo tempMember = (MemberInfo)fixupInfo;
                                if (tempMember is FieldInfo)
                                {
                                    // If we have a valuetype that's been boxed to an object and requires a fixup,
                                    // there are two possible states:
                                    // (a)The valuetype has never been fixed up into it's container.  In this case, we should
                                    // just fix up the boxed valuetype.  The task of pushing that valuetype into it's container
                                    // will be handled later.  This case is handled by the else clause of the following statement.
                                    // (b)The valuetype has already been inserted into it's container.  In that case, we need
                                    // to go through the more complicated path laid out in DoValueTypeFixup. We can tell that the
                                    // valuetype has already been inserted into it's container because we set ValueTypeFixupPerformed
                                    // to true when we do this.
                                    if (holder.RequiresValueTypeFixup && holder.ValueTypeFixupPerformed)
                                    {
                                        if (!DoValueTypeFixup((FieldInfo)tempMember, holder, tempObjectHolder.ObjectValue))
                                        {
                                            throw new SerializationException(SR.Serialization_PartialValueTypeFixup);
                                        }
                                    }
                                    else
                                    {
                                        FormatterServices.SerializationSetValue(tempMember, holder.ObjectValue, tempObjectHolder.ObjectValue);
                                    }
                                    if (tempObjectHolder.RequiresValueTypeFixup)
                                    {
                                        tempObjectHolder.ValueTypeFixupPerformed = true;
                                    }
                                }
                                else
                                {
                                    throw new SerializationException(SR.Serialization_UnableToFixup);
                                }
                                break;
                            default:
                                throw new SerializationException(SR.Serialization_UnableToFixup);
                        }
                        //Decrement our total number of fixups left to do.
                        fixupsPerformed++;
                        fixups._values[i] = null;
                        if (!bObjectFullyComplete)
                        {
                            holder.DecrementFixupsRemaining(this);
                            tempObjectHolder.RemoveDependency(holder._id);
                        }
                    }
                }
            }

            _fixupCount -= fixupsPerformed;

            if (fixups!._count == fixupsPerformed)
            {
                holder._missingElements = null;
            }
        }

        /// <summary>
        /// This is called immediately after we register a new object.  Walk that objects
        /// dependency list (if it has one) and decrement the counters on each object for
        /// the number of unsatisfiable references.  If the count reaches 0, go ahead
        /// and process the object.
        /// </summary>
        /// <param name="holder">dependencies The list of dependent objects</param>
        private void DoNewlyRegisteredObjectFixups(ObjectHolder holder)
        {
            if (holder.CanObjectValueChange)
            {
                return;
            }

            //If we don't have any dependencies, we're done.
            LongList? dependencies = holder.DependentObjects;
            if (dependencies == null)
            {
                return;
            }

            //Walk all of the dependencies and decrement the counter on each of uncompleted objects.
            //If one of the counters reaches 0, all of it's fields have been completed and we should
            //go take care of its fixups.
            dependencies.StartEnumeration();
            while (dependencies.MoveNext())
            {
                ObjectHolder? temp = FindObjectHolder(dependencies.Current);
                Debug.Assert(temp != null);
                Debug.Assert(temp.DirectlyDependentObjects > 0, "temp.m_missingElementsRemaining>0");
                temp.DecrementFixupsRemaining(this);
                if (((temp.DirectlyDependentObjects)) == 0)
                {
                    // If this is null, we have the case where a fixup was registered for a child, the object
                    // required by the fixup was provided, and the object to be fixed hasn't yet been seen.
                    if (temp.ObjectValue != null)
                    {
                        CompleteObject(temp, true);
                    }
                    else
                    {
                        temp.MarkForCompletionWhenAvailable();
                    }
                }
            }
        }

        public virtual object? GetObject(long objectID)
        {
            if (objectID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(objectID), SR.ArgumentOutOfRange_ObjectID);
            }

            //Find the bin in which we're interested.  IObjectReference's shouldn't be returned -- the graph
            //needs to link to the objects to which they refer, not to the references themselves.
            ObjectHolder? holder = FindObjectHolder(objectID);
            if (holder == null || holder.CanObjectValueChange)
            {
                return null;
            }

            return holder.ObjectValue;
        }

        [RequiresUnreferencedCode(ObjectManagerUnreferencedCodeMessage)]
        public virtual void RegisterObject(object obj, long objectID)
        {
            RegisterObject(obj, objectID, null, 0, null);
        }

        [RequiresUnreferencedCode(ObjectManagerUnreferencedCodeMessage)]
        public void RegisterObject(object obj, long objectID, SerializationInfo info)
        {
            RegisterObject(obj, objectID, info, 0, null);
        }

        [RequiresUnreferencedCode(ObjectManagerUnreferencedCodeMessage)]
        public void RegisterObject(object obj, long objectID, SerializationInfo? info, long idOfContainingObj, MemberInfo? member)
        {
            RegisterObject(obj, objectID, info, idOfContainingObj, member, null);
        }

        internal void RegisterString(string? obj, long objectID, SerializationInfo? info, long idOfContainingObj, MemberInfo? member)
        {
            ObjectHolder temp;
            Debug.Assert(member == null || member is FieldInfo, "RegisterString - member is FieldInfo");

            temp = new ObjectHolder(obj, objectID, info, null, idOfContainingObj, (FieldInfo?)member, null);
            AddObjectHolder(temp);
            return;
        }

        [RequiresUnreferencedCode(ObjectManagerUnreferencedCodeMessage)]
        public void RegisterObject(object obj, long objectID, SerializationInfo? info, long idOfContainingObj, MemberInfo? member, int[]? arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(obj);

            if (objectID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(objectID), SR.ArgumentOutOfRange_ObjectID);
            }
            if (member != null && !(member is FieldInfo)) // .NET Framework checks specifically for RuntimeFieldInfo and SerializationFieldInfo, but the former is an implementation detail in corelib
            {
                throw new SerializationException(SR.Serialization_UnknownMemberInfo);
            }

            ObjectHolder? temp;
            ISerializationSurrogate? surrogate = null;
            ISurrogateSelector useless;

            if (_selector != null)
            {
                Type selectorType = CanCallGetType(obj) ?
                    obj.GetType() :
                    typeof(MarshalByRefObject);

                //If we need a surrogate for this object, lets find it now.
                surrogate = _selector.GetSurrogate(selectorType, _context, out useless);
            }

            //The object is interested in DeserializationEvents so lets register it.
            if (obj is IDeserializationCallback)
            {
                DeserializationEventHandler d = new DeserializationEventHandler(((IDeserializationCallback)obj).OnDeserialization);
                AddOnDeserialization(d);
            }

            //Formatter developers may cache and reuse arrayIndex in their code.
            //So that we don't get bitten by this, take a copy up front.
            if (arrayIndex != null)
            {
                arrayIndex = (int[])arrayIndex.Clone();
            }

            //This is the first time which we've seen the object, we need to create a new holder.
            temp = FindObjectHolder(objectID);
            if (temp == null)
            {
                temp = new ObjectHolder(obj, objectID, info, surrogate, idOfContainingObj, (FieldInfo?)member, arrayIndex);
                AddObjectHolder(temp);
                if (temp.RequiresDelayedFixup)
                {
                    SpecialFixupObjects.Add(temp);
                }

                // We cannot compute whether this has any fixups required or not
                AddOnDeserialized(obj);
                return;
            }

            //If the object isn't null, we've registered this before.  Not good.
            if (temp.ObjectValue != null)
            {
                throw new SerializationException(SR.Serialization_RegisterTwice);
            }

            //Complete the data in the ObjectHolder
            temp.UpdateData(obj, info, surrogate, idOfContainingObj, (FieldInfo?)member, arrayIndex, this);

            // The following case will only be true when somebody has registered a fixup on an object before
            // registering the object itself.  I don't believe that most well-behaved formatters will do this,
            // but we need to allow it anyway.  We will walk the list of fixups which have been recorded on
            // the new object and fix those that we can.  Because the user could still register later fixups
            // on this object, we won't call any implementations of ISerializable now.  If that's required,
            // it will have to be handled by the code in DoFixups.
            // README README: We have to do the UpdateData before
            if (temp.DirectlyDependentObjects > 0)
            {
                CompleteObject(temp, false);
            }

            if (temp.RequiresDelayedFixup)
            {
                SpecialFixupObjects.Add(temp);
            }

            if (temp.CompletelyFixed)
            {
                //Here's where things get tricky.  If this isn't an instance of IObjectReference, we need to walk it's fixup
                //chain and decrement the counters on anything that has reached 0.  Once we've notified all of the dependencies,
                //we can simply clear the list of dependent objects.
                DoNewlyRegisteredObjectFixups(temp);
                temp.DependentObjects = null;
            }

            //Register the OnDeserialized methods to be invoked after deserialization is complete
            if (temp.TotalDependentObjects > 0)
            {
                AddOnDeserialized(obj);
            }
            else
            {
                RaiseOnDeserializedEvent(obj);
            }
        }

        /// <summary>
        /// Completes an object implementing ISerializable.  This will involve calling that
        /// objects constructor which takes an instance of ISerializable and a StreamingContext.
        /// </summary>
        /// <param name="obj">The object to be completed.</param>
        /// <param name="info">The SerializationInfo containing all info for obj.</param>
        /// <param name="context">The streaming context in which the serialization is taking place.</param>
        [RequiresUnreferencedCode(ObjectManagerUnreferencedCodeMessage)]
        internal void CompleteISerializableObject(object obj, SerializationInfo? info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(obj);

            if (!(obj is ISerializable))
            {
                throw new ArgumentException(SR.Serialization_NotISer);
            }

            ConstructorInfo constInfo;
            Type t = obj.GetType();
            try
            {
                constInfo = GetDeserializationConstructor(t);
            }
            catch (Exception e)
            {
                throw new SerializationException(SR.Format(SR.Serialization_ConstructorNotFound, t), e);
            }

            constInfo.Invoke(obj, new object?[] { info, context });
        }

        internal static ConstructorInfo GetDeserializationConstructor(
           [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type t)
        {
            foreach (ConstructorInfo ci in t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                ParameterInfo[] parameters = ci.GetParameters();
                if (parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(SerializationInfo) &&
                    parameters[1].ParameterType == typeof(StreamingContext))
                {
                    return ci;
                }
            }

            throw new SerializationException(SR.Format(SR.Serialization_ConstructorNotFound, t.FullName));
        }

        [RequiresUnreferencedCode(ObjectManagerUnreferencedCodeMessage)]
        public virtual void DoFixups()
        {
            ObjectHolder? temp;
            int fixupCount = -1;

            //The first thing that we need to do is fixup all of the objects which implement
            //IObjectReference.  This is complicated by the fact that we need to deal with IReferenceObjects
            //objects that have a reference to an object implementing IObjectReference.  We continually
            //walk over the list of objects until we've completed all of the object references or until
            //we can't resolve any more (which may happen if we have two objects implementing IObjectReference
            //which have a circular dependency on each other).  We don't explicitly catch the later case here,
            //it will be caught when we try to do the rest of the fixups and discover that we have some that
            //can't be completed.
            while (fixupCount != 0)
            {
                fixupCount = 0;
                //Walk all of the IObjectReferences and ensure that they've been properly completed.
                ObjectHolderListEnumerator fixupObjectsEnum = SpecialFixupObjects.GetFixupEnumerator();
                while (fixupObjectsEnum.MoveNext())
                {
                    temp = fixupObjectsEnum.Current;
                    if (temp.ObjectValue == null)
                    {
                        throw new SerializationException(SR.Format(SR.Serialization_ObjectNotSupplied, temp._id));
                    }
                    if (temp.TotalDependentObjects == 0)
                    {
                        if (temp.RequiresSerInfoFixup)
                        {
                            FixupSpecialObject(temp);
                            fixupCount++;
                        }
                        else if (!temp.IsIncompleteObjectReference)
                        {
                            CompleteObject(temp, true);
                        }

                        if (temp.IsIncompleteObjectReference && ResolveObjectReference(temp))
                        {
                            fixupCount++;
                        }
                    }
                }
            }

            Debug.Assert(_fixupCount >= 0, "[ObjectManager.DoFixups]m_fixupCount>=0");

            //If our count is 0, we're done and should just return
            if (_fixupCount == 0)
            {
                if (TopObject is TypeLoadExceptionHolder)
                {
                    throw new SerializationException(SR.Format(SR.Serialization_TypeLoadFailure, ((TypeLoadExceptionHolder)TopObject).TypeName));
                }
                return;
            }

            //If our count isn't 0, we had at least one case where an object referenced another object twice.
            //Walk the entire list until the count is 0 or until we find an object which we can't complete.
            for (int i = 0; i < _objects.Length; i++)
            {
                temp = _objects[i];
                while (temp != null)
                {
                    if (temp.TotalDependentObjects > 0 /*|| temp.m_missingElements!=null*/)
                    {
                        CompleteObject(temp, true);
                    }
                    temp = temp._next;
                }
                if (_fixupCount == 0)
                {
                    return;
                }
            }

            // this assert can be trigered by user code that manages fixups manually
            throw new SerializationException(SR.Serialization_IncorrectNumberOfFixups);
        }

        /// <summary>
        /// Do the actual grunt work of recording a fixup and registering the dependency.
        /// Create the necessary ObjectHolders and use them to do the addition.
        /// </summary>
        /// <param name="fixup">The FixupHolder to be added.</param>
        /// <param name="objectRequired">The id of the object required to do the fixup.</param>
        /// <param name="objectToBeFixed">The id of the object requiring the fixup.</param>
        private void RegisterFixup(FixupHolder fixup, long objectToBeFixed, long objectRequired)
        {
            //Record the fixup with the object that needs it.
            ObjectHolder ohToBeFixed = FindOrCreateObjectHolder(objectToBeFixed);
            ObjectHolder ohRequired;

            if (ohToBeFixed.RequiresSerInfoFixup && fixup._fixupType == FixupHolder.MemberFixup)
            {
                throw new SerializationException(SR.Serialization_InvalidFixupType);
            }

            //Add the fixup to the list.
            ohToBeFixed.AddFixup(fixup, this);

            //Find the object on which we're dependent and note the dependency.
            //These dependencies will be processed when the object is supplied.
            ohRequired = FindOrCreateObjectHolder(objectRequired);

            ohRequired.AddDependency(objectToBeFixed);

            _fixupCount++;
        }

        public virtual void RecordFixup(long objectToBeFixed, MemberInfo member, long objectRequired)
        {
            //Verify our arguments
            if (objectToBeFixed <= 0 || objectRequired <= 0)
            {
                throw new ArgumentOutOfRangeException(objectToBeFixed <= 0 ? nameof(objectToBeFixed) : nameof(objectRequired), SR.Serialization_IdTooSmall);
            }
            ArgumentNullException.ThrowIfNull(member);
            if (!(member is FieldInfo)) // .NET Framework checks specifically for RuntimeFieldInfo and SerializationFieldInfo, but the former is an implementation detail in corelib
            {
                throw new SerializationException(SR.Format(SR.Serialization_InvalidType, member.GetType()));
            }

            //Create a new fixup holder
            FixupHolder fixup = new FixupHolder(objectRequired, member, FixupHolder.MemberFixup);
            RegisterFixup(fixup, objectToBeFixed, objectRequired);
        }

        public virtual void RecordDelayedFixup(long objectToBeFixed, string memberName, long objectRequired)
        {
            //Verify our arguments
            if (objectToBeFixed <= 0 || objectRequired <= 0)
            {
                throw new ArgumentOutOfRangeException(objectToBeFixed <= 0 ? nameof(objectToBeFixed) : nameof(objectRequired), SR.Serialization_IdTooSmall);
            }
            ArgumentNullException.ThrowIfNull(memberName);

            //Create a new fixup holder
            FixupHolder fixup = new FixupHolder(objectRequired, memberName, FixupHolder.DelayedFixup);
            RegisterFixup(fixup, objectToBeFixed, objectRequired);
        }

        public virtual void RecordArrayElementFixup(long arrayToBeFixed, int index, long objectRequired)
        {
            int[] indexArray = new int[1];
            indexArray[0] = index;
            RecordArrayElementFixup(arrayToBeFixed, indexArray, objectRequired);
        }

        public virtual void RecordArrayElementFixup(long arrayToBeFixed, int[] indices, long objectRequired)
        {
            //Verify our arguments
            if (arrayToBeFixed <= 0 || objectRequired <= 0)
            {
                throw new ArgumentOutOfRangeException(arrayToBeFixed <= 0 ? nameof(arrayToBeFixed) : nameof(objectRequired), SR.Serialization_IdTooSmall);
            }
            ArgumentNullException.ThrowIfNull(indices);

            FixupHolder fixup = new FixupHolder(objectRequired, indices, FixupHolder.ArrayFixup);
            RegisterFixup(fixup, arrayToBeFixed, objectRequired);
        }

        public virtual void RaiseDeserializationEvent()
        {
            // Invoke OnDerserialized event if applicable
            _onDeserializedHandler?.Invoke(_context);
            _onDeserializationHandler?.Invoke(null);
        }

        internal virtual void AddOnDeserialization(DeserializationEventHandler handler)
        {
            _onDeserializationHandler = (DeserializationEventHandler)Delegate.Combine(_onDeserializationHandler, handler);
        }

        [RequiresUnreferencedCode(ObjectManagerUnreferencedCodeMessage)]
        internal virtual void AddOnDeserialized(object obj)
        {
            SerializationEvents cache = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
            _onDeserializedHandler = cache.AddOnDeserialized(obj, _onDeserializedHandler);
        }

        [RequiresUnreferencedCode(ObjectManagerUnreferencedCodeMessage)]
        internal virtual void RaiseOnDeserializedEvent(object obj)
        {
            SerializationEvents cache = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
            cache.InvokeOnDeserialized(obj, _context);
        }

        [RequiresUnreferencedCode(ObjectManagerUnreferencedCodeMessage)]
        public void RaiseOnDeserializingEvent(object obj)
        {
            // Run the OnDeserializing methods
            SerializationEvents cache = SerializationEventsCache.GetSerializationEventsForType(obj.GetType());
            cache.InvokeOnDeserializing(obj, _context);
        }
    }

    internal sealed class ObjectHolder
    {
        internal const int IncompleteObjectReference = 0x0001;
        internal const int HAS_ISERIALIZABLE = 0x0002;
        internal const int HAS_SURROGATE = 0x0004;
        internal const int REQUIRES_VALUETYPE_FIXUP = 0x0008;
        internal const int REQUIRES_DELAYED_FIXUP = HAS_ISERIALIZABLE | HAS_SURROGATE | IncompleteObjectReference;
        internal const int SER_INFO_FIXED = 0x4000;
        internal const int VALUETYPE_FIXUP_PERFORMED = 0x8000;

        private object? _object;
        internal readonly long _id;
        private int _missingElementsRemaining;
        private int _missingDecendents;
        internal SerializationInfo? _serInfo;
        internal ISerializationSurrogate? _surrogate;
        internal FixupHolderList? _missingElements;
        internal LongList? _dependentObjects;
        internal ObjectHolder? _next;
        internal int _flags;
        private bool _markForFixupWhenAvailable;
        private ValueTypeFixupInfo? _valueFixup;
        private TypeLoadExceptionHolder? _typeLoad;
        private bool _reachable;

        internal ObjectHolder(long objID) : this(null, objID, null, null, 0, null, null)
        {
        }

        internal ObjectHolder(
            object? obj, long objID, SerializationInfo? info, ISerializationSurrogate? surrogate,
            long idOfContainingObj, FieldInfo? field, int[]? arrayIndex)
        {
            Debug.Assert(objID >= 0, "objID>=0");

            _object = obj; //May be null;
            _id = objID;

            _flags = 0;
            _missingElementsRemaining = 0;
            _missingDecendents = 0;
            _dependentObjects = null;
            _next = null;

            _serInfo = info;
            _surrogate = surrogate;
            _markForFixupWhenAvailable = false;

            if (obj is TypeLoadExceptionHolder)
            {
                _typeLoad = (TypeLoadExceptionHolder)obj;
            }

            if (idOfContainingObj != 0 && ((field != null && field.FieldType.IsValueType) || arrayIndex != null))
            {
                if (idOfContainingObj == objID)
                {
                    throw new SerializationException(SR.Serialization_ParentChildIdentical);
                }

                _valueFixup = new ValueTypeFixupInfo(idOfContainingObj, field, arrayIndex);
            }

            SetFlags();
        }

        internal ObjectHolder(
            string? obj, long objID, SerializationInfo? info, ISerializationSurrogate? surrogate,
            long idOfContainingObj, FieldInfo? field, int[]? arrayIndex)
        {
            Debug.Assert(objID >= 0, "objID>=0");

            _object = obj; //May be null;
            _id = objID;

            _flags = 0;
            _missingElementsRemaining = 0;
            _missingDecendents = 0;
            _dependentObjects = null;
            _next = null;

            _serInfo = info;
            _surrogate = surrogate;
            _markForFixupWhenAvailable = false;

            if (idOfContainingObj != 0 && arrayIndex != null)
            {
                _valueFixup = new ValueTypeFixupInfo(idOfContainingObj, field, arrayIndex);
            }

            if (_valueFixup != null)
            {
                _flags |= REQUIRES_VALUETYPE_FIXUP;
            }
        }

        private void IncrementDescendentFixups(int amount) => _missingDecendents += amount;

        internal void DecrementFixupsRemaining(ObjectManager manager)
        {
            _missingElementsRemaining--;
            if (RequiresValueTypeFixup)
            {
                UpdateDescendentDependencyChain(-1, manager);
            }
        }

        /// <summary>
        /// Removes a dependency of the object represented in this holder.
        /// This is normally the result of the dependency having been filled when
        /// the object is going to be only partially completed.  If we plan to fully
        /// update the object, we do not take the work to do this.
        /// </summary>
        /// <param name="id">The id of the object for which to remove the dependency.</param>
        internal void RemoveDependency(long id)
        {
            Debug.Assert(_dependentObjects != null, "[ObjectHolder.RemoveDependency]m_dependentObjects!=null");
            Debug.Assert(id >= 0, "[ObjectHolder.RemoveDependency]id>=0");
            _dependentObjects.RemoveElement(id);
        }

        /// <summary>
        /// Note a fixup that has to be done before this object can be completed.
        /// Fixups are things that need to happen when other objects in the graph
        /// are added.  Dependencies are things that need to happen when this object
        /// is added.
        /// </summary>
        /// <param name="fixup">The fixup holder containing enough information to complete the fixup.</param>
        /// <param name="manager">The associated object manager.</param>
        internal void AddFixup(FixupHolder fixup, ObjectManager manager)
        {
            _missingElements ??= new FixupHolderList();
            _missingElements.Add(fixup);
            _missingElementsRemaining++;

            if (RequiresValueTypeFixup)
            {
                UpdateDescendentDependencyChain(1, manager);
            }
        }

        /// <summary>
        /// Updates the total list of dependencies to account for a fixup being added
        /// or completed in a child value class.  This will update all value classes
        /// containing that child and the object which contains all of them.
        /// </summary>
        /// <param name="amount">the amount by which to increment (or decrement) the dependency chain.</param>
        /// <param name="manager">The ObjectManager used to lookup other objects up the chain.</param>
        private void UpdateDescendentDependencyChain(int amount, ObjectManager manager)
        {
            ObjectHolder holder = this;

            //This loop walks one more object up the chain than there are valuetypes.  This
            //is because we need to increment the TotalFixups in the holders as well.
            do
            {
                holder = manager.FindOrCreateObjectHolder(holder.ContainerID);
                Debug.Assert(holder != null, "[ObjectHolder.UpdateTotalDependencyChain]holder!=null");
                holder.IncrementDescendentFixups(amount);
            } while (holder.RequiresValueTypeFixup);
        }

        /// <summary>
        /// Note an object which is dependent on the one which will be contained in
        /// this ObjectHolder.  Dependencies should only be added if the object hasn't
        /// yet been added.  NB: An incomplete object counts as having no object.
        /// </summary>
        /// <param name="dependentObject">the id of the object which is dependent on this object being provided.</param>
        internal void AddDependency(long dependentObject)
        {
            _dependentObjects ??= new LongList();
            _dependentObjects.Add(dependentObject);
        }

        /// <summary>
        /// Update the data in the object holder.  This should be called when the object
        /// is finally registered.  Presumably the ObjectHolder was created to track
        /// some dependencies or preregistered fixups and we now need to actually record the
        /// object and other associated data.  We take this opportunity to set the flags
        /// so that we can do some faster processing in the future.
        /// </summary>
        /// <param name="info">The optional serialization info.</param>
        /// <param name="obj">The object being held by this object holder. (This should no longer be null).</param>
        /// <param name="field">The SerializationInfo associated with this object, only required if we're doing delayed fixups.</param>
        /// <param name="surrogate">The surrogate handling this object.  May be null.</param>
        /// <param name="idOfContainer">The id of the object containing this one if this is a valuetype.</param>
        /// <param name="arrayIndex">The array index.</param>
        /// <param name="manager">the ObjectManager being used to track these ObjectHolders.</param>
        internal void UpdateData(
            object obj, SerializationInfo? info, ISerializationSurrogate? surrogate, long idOfContainer,
            FieldInfo? field, int[]? arrayIndex, ObjectManager manager)
        {
            Debug.Assert(obj != null, "obj!=null");
            Debug.Assert(_id > 0, "m_id>0");

            //Record the fields that we can.
            SetObjectValue(obj, manager);
            _serInfo = info;
            _surrogate = surrogate;

            if (idOfContainer != 0 && ((field != null && field.FieldType.IsValueType) || arrayIndex != null))
            {
                if (idOfContainer == _id)
                {
                    throw new SerializationException(SR.Serialization_ParentChildIdentical);
                }
                _valueFixup = new ValueTypeFixupInfo(idOfContainer, field, arrayIndex);
            }

            SetFlags();

            if (RequiresValueTypeFixup)
            {
                UpdateDescendentDependencyChain(_missingElementsRemaining, manager);
            }
        }

        internal void MarkForCompletionWhenAvailable() => _markForFixupWhenAvailable = true;

        /// <summary>
        /// An internal-only routine to set the flags based upon the data contained in
        /// the ObjectHolder
        /// </summary>
        internal void SetFlags()
        {
            if (_object is IObjectReference)
            {
                _flags |= IncompleteObjectReference;
            }

            _flags &= ~(HAS_ISERIALIZABLE | HAS_SURROGATE);
            if (_surrogate != null)
            {
                _flags |= HAS_SURROGATE;
            }
            else if (_object is ISerializable)
            {
                _flags |= HAS_ISERIALIZABLE;
            }

            if (_valueFixup != null)
            {
                _flags |= REQUIRES_VALUETYPE_FIXUP;
            }
        }

        internal bool IsIncompleteObjectReference
        {
            get { return (_flags & IncompleteObjectReference) != 0; }
            set
            {
                if (value)
                {
                    _flags |= IncompleteObjectReference;
                }
                else
                {
                    _flags &= ~IncompleteObjectReference;
                }
            }
        }

        internal bool RequiresDelayedFixup => (_flags & REQUIRES_DELAYED_FIXUP) != 0;

        internal bool RequiresValueTypeFixup => (_flags & REQUIRES_VALUETYPE_FIXUP) != 0;

        // ValueTypes which require fixups are initially handed to the ObjectManager
        // as boxed objects.  When they're still boxed objects, we should just do fixups
        // on them like we would any other object.  As soon as they're pushed into their
        // containing object we set ValueTypeFixupPerformed to true and have to go through
        // a more complicated path to set fixed up valuetype objects.
        // We check whether or not there are any dependent objects.
        internal bool ValueTypeFixupPerformed
        {
            get
            {
                return (((_flags & VALUETYPE_FIXUP_PERFORMED) != 0) ||
                        (_object != null && ((_dependentObjects == null) || _dependentObjects.Count == 0)));
            }
            set
            {
                if (value)
                {
                    _flags |= VALUETYPE_FIXUP_PERFORMED;
                }
            }
        }

        internal bool HasISerializable => (_flags & HAS_ISERIALIZABLE) != 0;

        internal bool HasSurrogate => (_flags & HAS_SURROGATE) != 0;

        internal bool CanSurrogatedObjectValueChange =>
            (_surrogate == null || _surrogate.GetType() != typeof(SurrogateForCyclicalReference));

        internal bool CanObjectValueChange =>
            IsIncompleteObjectReference ? true :
            HasSurrogate ? CanSurrogatedObjectValueChange :
            false;

        internal int DirectlyDependentObjects => _missingElementsRemaining;

        internal int TotalDependentObjects => _missingElementsRemaining + _missingDecendents;

        internal bool Reachable { get { return _reachable; } set { _reachable = value; } }

        internal bool TypeLoadExceptionReachable => _typeLoad != null;

        internal TypeLoadExceptionHolder? TypeLoadException { get { return _typeLoad; } set { _typeLoad = value; } }

        internal object? ObjectValue => _object;

        internal void SetObjectValue(object? obj, ObjectManager manager)
        {
            _object = obj;
            if (obj == manager.TopObject)
            {
                _reachable = true;
            }
            if (obj is TypeLoadExceptionHolder)
            {
                _typeLoad = (TypeLoadExceptionHolder)obj;
            }

            if (_markForFixupWhenAvailable)
            {
                manager.CompleteObject(this, true);
            }
        }

        internal SerializationInfo? SerializationInfo { get { return _serInfo; } set { _serInfo = value; } }

        internal ISerializationSurrogate? Surrogate => _surrogate;

        internal LongList? DependentObjects { get { return _dependentObjects; } set { _dependentObjects = value; } }

        internal bool RequiresSerInfoFixup
        {
            get
            {
                if (((_flags & HAS_SURROGATE) == 0) && ((_flags & HAS_ISERIALIZABLE) == 0))
                {
                    return false;
                }

                return (_flags & SER_INFO_FIXED) == 0;
            }
            set
            {
                if (!value)
                {
                    _flags |= SER_INFO_FIXED;
                }
                else
                {
                    _flags &= ~SER_INFO_FIXED;
                }
            }
        }

        internal ValueTypeFixupInfo? ValueFixup => _valueFixup;

        internal bool CompletelyFixed => !RequiresSerInfoFixup && !IsIncompleteObjectReference;

        internal long ContainerID => _valueFixup != null ? _valueFixup.ContainerID : 0;
    }

    internal sealed class FixupHolder
    {
        internal const int ArrayFixup = 0x1;
        internal const int MemberFixup = 0x2;
        internal const int DelayedFixup = 0x4;

        internal long _id;
        internal object _fixupInfo; //This is either an array index, a String, or a MemberInfo
        internal readonly int _fixupType;

        internal FixupHolder(long id, object fixupInfo, int fixupType)
        {
            Debug.Assert(id > 0, "id>0");
            Debug.Assert(fixupInfo != null, "fixupInfo!=null");
            Debug.Assert(fixupType == ArrayFixup || fixupType == MemberFixup || fixupType == DelayedFixup, "fixupType==ArrayFixup || fixupType == MemberFixup || fixupType==DelayedFixup");

            _id = id;
            _fixupInfo = fixupInfo;
            _fixupType = fixupType;
        }
    }

    internal sealed class FixupHolderList
    {
        internal const int InitialSize = 2;

        internal FixupHolder?[] _values;
        internal int _count;

        internal FixupHolderList() : this(InitialSize)
        {
        }

        internal FixupHolderList(int startingSize)
        {
            _count = 0;
            _values = new FixupHolder[startingSize];
        }

        internal void Add(FixupHolder fixup)
        {
            if (_count == _values.Length)
            {
                EnlargeArray();
            }
            _values[_count++] = fixup;
        }

        private void EnlargeArray()
        {
            int newLength = _values.Length * 2;
            if (newLength < 0)
            {
                newLength = int.MaxValue;
            }

            FixupHolder[] temp = new FixupHolder[newLength];
            Array.Copy(_values, temp, _count);
            _values = temp;
        }
    }

    internal sealed class LongList
    {
        private const int InitialSize = 2;

        private long[] _values;
        private int _count; //The total number of valid items still in the list;
        private int _totalItems; //The total number of allocated entries. This includes space for items which have been marked as deleted.
        private int _currentItem; //Used when doing an enumeration over the list.

        // An m_currentItem of -1 indicates that the enumeration hasn't been started.
        // An m_values[xx] of -1 indicates that the item has been deleted.
        internal LongList() : this(InitialSize)
        {
        }

        internal LongList(int startingSize)
        {
            _count = 0;
            _totalItems = 0;
            _values = new long[startingSize];
        }

        internal void Add(long value)
        {
            if (_totalItems == _values.Length)
            {
                EnlargeArray();
            }
            _values[_totalItems++] = value;
            _count++;
        }

        internal int Count => _count;

        internal void StartEnumeration() => _currentItem = -1;

        internal bool MoveNext()
        {
            while (++_currentItem < _totalItems && _values[_currentItem] == -1) ;
            return _currentItem != _totalItems;
        }

        internal long Current
        {
            get
            {
                Debug.Assert(_currentItem != -1, "[LongList.Current]m_currentItem!=-1");
                Debug.Assert(_values[_currentItem] != -1, "[LongList.Current]m_values[m_currentItem]!=-1");
                return _values[_currentItem];
            }
        }

        internal bool RemoveElement(long value)
        {
            int i;
            for (i = 0; i < _totalItems; i++)
            {
                if (_values[i] == value)
                {
                    break;
                }
            }
            if (i == _totalItems)
            {
                return false;
            }
            _values[i] = -1;
            return true;
        }

        private void EnlargeArray()
        {
            int newLength = _values.Length * 2;
            if (newLength < 0)
            {
                newLength = int.MaxValue;
            }

            long[] temp = new long[newLength];
            Array.Copy(_values, temp, _count);
            _values = temp;
        }
    }

    internal sealed class ObjectHolderList
    {
        internal const int DefaultInitialSize = 8;

        internal ObjectHolder[] _values;
        internal int _count;

        internal ObjectHolderList() : this(DefaultInitialSize)
        {
        }

        internal ObjectHolderList(int startingSize)
        {
            Debug.Assert(startingSize > 0 && startingSize < 0x1000, "startingSize>0 && startingSize<0x1000");
            _count = 0;
            _values = new ObjectHolder[startingSize];
        }

        internal void Add(ObjectHolder value)
        {
            if (_count == _values.Length)
            {
                EnlargeArray();
            }
            _values[_count++] = value;
        }

        internal ObjectHolderListEnumerator GetFixupEnumerator() => new ObjectHolderListEnumerator(this, true);

        private void EnlargeArray()
        {
            int newLength = _values.Length * 2;
            if (newLength < 0)
            {
                newLength = int.MaxValue;
            }

            ObjectHolder[] temp = new ObjectHolder[newLength];
            Array.Copy(_values, temp, _count);
            _values = temp;
        }

        internal int Version => _count;

        internal int Count => _count;
    }

    internal sealed class ObjectHolderListEnumerator
    {
        private readonly bool _isFixupEnumerator;
        private readonly ObjectHolderList _list;
        private readonly int _startingVersion;
        private int _currPos;

        internal ObjectHolderListEnumerator(ObjectHolderList list, bool isFixupEnumerator)
        {
            Debug.Assert(list != null, "[ObjectHolderListEnumerator.ctor]list!=null");
            _list = list;
            _startingVersion = _list.Version;
            _currPos = -1;
            _isFixupEnumerator = isFixupEnumerator;
        }

        internal bool MoveNext()
        {
            Debug.Assert(_startingVersion == _list.Version, "[ObjectHolderListEnumerator.MoveNext]m_startingVersion==m_list.Version");
            if (_isFixupEnumerator)
            {
                while (++_currPos < _list.Count && _list._values[_currPos].CompletelyFixed);
                return _currPos != _list.Count;
            }
            else
            {
                _currPos++;
                return _currPos != _list.Count;
            }
        }

        internal ObjectHolder Current
        {
            get
            {
                Debug.Assert(_currPos != -1, "[ObjectHolderListEnumerator.Current]m_currPos!=-1");
                Debug.Assert(_currPos < _list.Count, "[ObjectHolderListEnumerator.Current]m_currPos<m_list.Count");
                Debug.Assert(_startingVersion == _list.Version, "[ObjectHolderListEnumerator.Current]m_startingVersion==m_list.Version");
                return _list._values[_currPos];
            }
        }
    }

    public sealed class TypeLoadExceptionHolder
    {
        internal TypeLoadExceptionHolder(string? typeName)
        {
            TypeName = typeName;
        }

        internal string? TypeName { get; }
    }

    internal static class SerializationInfoExtensions
    {
        private static readonly Action<SerializationInfo, string, object, Type> s_updateValue =
            typeof(SerializationInfo)
            .GetMethod("UpdateValue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .CreateDelegate<Action<SerializationInfo, string, object, Type>>();

        public static void UpdateValue(this SerializationInfo si, string name, object value, Type type) =>
            s_updateValue(si, name, value, type);
    }
}
