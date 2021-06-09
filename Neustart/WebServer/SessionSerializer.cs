using Nancy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Neustart
{
    public class JsonSessionSerialiser : IObjectSerializer
    {
        public static dynamic ToDynamic(object value)
        {
            var expando = new ExpandoObject() as IDictionary<string, object>;

            foreach (var property in value.GetType().GetTypeInfo().DeclaredProperties)
            {
                expando.Add(property.Name, property.GetValue(value));
            }

            return (ExpandoObject)expando;
        }

        public string Serialize(object sourceObject)
        {
            if (sourceObject == null)
                return string.Empty;

            dynamic serializedObject = (sourceObject is string)
                ? sourceObject
                : AddTypeInformation(sourceObject);

            string json = JsonConvert.SerializeObject(serializedObject);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        private static dynamic AddTypeInformation(object sourceObject)
        {
            var assemblyQualifiedName = sourceObject.GetType().GetTypeInfo().AssemblyQualifiedName;

            dynamic serializedObject = ToDynamic(sourceObject);
            serializedObject.TypeObject = assemblyQualifiedName;

            return serializedObject;
        }

        public object Deserialize(string sourceString)
        {
            if (string.IsNullOrEmpty(sourceString))
                return null;

            try
            {
                var inputBytes = Convert.FromBase64String(sourceString);
                var json = Encoding.UTF8.GetString(inputBytes);

                if (!ContainsTypeDescription(json))
                    return JsonConvert.DeserializeObject(json);

                dynamic serializedObject = JsonConvert.DeserializeObject(json);
                Type t = Type.GetType((string)serializedObject.TypeObject);

                return JsonConvert.DeserializeObject(json, t);
            }
            catch (FormatException e)
            {
                Console.WriteLine(e.ToString());
            }
            catch (SerializationException e)
            {
                Console.WriteLine(e.ToString());
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
            }

            return null;
        }

        private static bool ContainsTypeDescription(string json)
        {
            return json.Contains("TypeObject");
        }
    }
}
