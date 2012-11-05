using System;
using System.IO;
using System.Xml.Serialization;

class Test
{
        static public T DeserializeFromString<T>(string xml) where T : class
        {

            if (String.IsNullOrEmpty(xml))
            {
                return null;
            }

            StringReader reader = null;
            T deserializedObject = null;
            try
            {
                reader = new StringReader(xml);
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                deserializedObject = serializer.Deserialize(reader) as T;
            }
            finally
            {
                if (null != reader)
                {
                    reader.Close();
                }
            }
            return deserializedObject;
        }

	static void Main ()
	{
            string myXML = @"<?xml version=""1.0"" encoding=""utf-8""?><TASK><OptionA/></TASK>";
            TASK data = DeserializeFromString<TASK>(myXML);
            System.Console.WriteLine(data);
	}
}
