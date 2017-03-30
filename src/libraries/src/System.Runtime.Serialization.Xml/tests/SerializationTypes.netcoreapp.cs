// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

namespace SerializationTypes
{
	internal class MyFileStreamSurrogateProvider : ISerializationSurrogateProvider
	{
	    static MyFileStreamSurrogateProvider()
	    {
	        Singleton = new MyFileStreamSurrogateProvider();
	    }

	    internal static MyFileStreamSurrogateProvider Singleton { get; private set; }

	    public Type GetSurrogateType(Type type)
	    {
	        if (type == typeof (MyFileStream))
	        {
	            return typeof (MyFileStreamReference);
	        }

	        return type;
	    }

	    public object GetObjectToSerialize(object obj, Type targetType)
	    {
	        if (obj == null)
	        {
	            return null;
	        }
	        MyFileStream myFileStream = obj as MyFileStream;
	        if (null != myFileStream)
	        {
	            if (targetType != typeof (MyFileStreamReference))
	            {
	                throw new ArgumentException("Target type for serialization must be MyFileStream");
	            }
	            return MyFileStreamReference.Create(myFileStream);
	        }

	        return obj;
	    }

	    public object GetDeserializedObject(object obj, Type targetType)
	    {
	        if (obj == null)
	        {
	            return null;
	        }
	        MyFileStreamReference myFileStreamRef = obj as MyFileStreamReference;
	        if (null != myFileStreamRef)
	        {
	            if (targetType != typeof (MyFileStream))
	            {
	                throw new ArgumentException("Target type for deserialization must be MyFileStream");
	            }
	            return myFileStreamRef.ToMyFileStream();
	        }
	        return obj;
	    }
	}
	
	public class MyPersonSurrogateProvider : ISerializationSurrogateProvider
	{
	    public Type GetSurrogateType(Type type)
	    {
	        if (type == typeof(NonSerializablePerson))
	        {
	            return typeof(NonSerializablePersonSurrogate);
	        }
	        else if (type == typeof(NonSerializablePersonForStress))
	        {
	            return typeof(NonSerializablePersonForStressSurrogate);
	        }
	        else
	        {
	            return type;
	        }
	    }

	    public object GetDeserializedObject(object obj, Type targetType)
	    {
	        if (obj is NonSerializablePersonSurrogate)
	        {
	            NonSerializablePersonSurrogate person = (NonSerializablePersonSurrogate)obj;
	            return new NonSerializablePerson(person.Name, person.Age);
	        }
	        else if (obj is NonSerializablePersonForStressSurrogate)
	        {
	            NonSerializablePersonForStressSurrogate person = (NonSerializablePersonForStressSurrogate)obj;
	            return new NonSerializablePersonForStress(person.Name, person.Age);
	        }

	        return obj;
	    }

	    public object GetObjectToSerialize(object obj, Type targetType)
	    {
	        if (obj is NonSerializablePerson)
	        {
	            NonSerializablePerson nsp = (NonSerializablePerson)obj;
	            NonSerializablePersonSurrogate serializablePerson = new NonSerializablePersonSurrogate
	            {
	                Name = nsp.Name,
	                Age = nsp.Age,
	            };

	            return serializablePerson;
	        }
	        else if (obj is NonSerializablePersonForStress)
	        {
	            NonSerializablePersonForStress nsp = (NonSerializablePersonForStress)obj;
	            NonSerializablePersonForStressSurrogate serializablePerson = new NonSerializablePersonForStressSurrogate
	            {
	                Name = nsp.Name,
	                Age = nsp.Age,
	            };

	            return serializablePerson;
	        }

	        return obj;
	    }
	}
}
