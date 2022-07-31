using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Utility
{
    [XmlRoot("dictionary")]
    public class SerializableDictionary<TKey, TValue>
        : Dictionary<TKey, TValue>, IXmlSerializable
    {
        #region IXmlSerializable Members
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            while (reader.NodeType != System.Xml.XmlNodeType.EndElement) {
                reader.ReadStartElement("item");

                reader.ReadStartElement("key");
                TKey key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("value");
                TValue value = (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();

                this.Add(key, value);

                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            foreach (TKey key in this.Keys) {
                writer.WriteStartElement("item");

                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();

                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
        }
        #endregion
    }

    public static class Serialization
    {
        static string m_XmlDateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

        #region XML serialization
        /// <summary>
        /// Return a string representation of a DateTime in xml format
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string DateTimeToXml(DateTime dt)
        {
            if (dt == DateTime.MinValue)
                return string.Empty;

            if (dt.Kind != DateTimeKind.Utc)
                dt = dt.ToUniversalTime();
            return dt.ToString(m_XmlDateTimeFormat);
        } // DateTimeToXml

        /// <summary>
        /// Return a UTC DateTime from a string representation in xml format
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DateTime DateTimeFromXml(string dt)
        {
            DateTime res = DateTime.MinValue;
            DateTime.TryParseExact(dt, m_XmlDateTimeFormat, System.Globalization.CultureInfo.InvariantCulture,
                                   System.Globalization.DateTimeStyles.AdjustToUniversal, out res);
            return res;
        } // DateTimeFromXml

        /// <summary>
        /// Serialize an object to XML
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="data">The object</param>
        /// <returns>The serialized object</returns>
        public static string Serialize<T>(T data)
        {
            try {
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = false,
                    Encoding = Encoding.UTF8
                };

                using (MemoryStream ms = new MemoryStream()) {
                    using (XmlWriter xmlWriter = XmlWriter.Create(ms, xmlWriterSettings)) {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        serializer.Serialize(xmlWriter, data);
                        ms.Seek(0, SeekOrigin.Begin);
                        using (StreamReader sr = new StreamReader(ms, Encoding.UTF8, false, 4096, true)) {
                            return sr.ReadToEnd();
                        }
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(string.Format("Error serializing object: {0}", ex.Message));
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine(ex.InnerException.Message);
            }
            return string.Empty;
        } // Serialize

        /// <summary>
        /// Deserialize a stream to an object
        /// </summary>
        /// <param name="stream">The stream</param>
        /// <returns>The deserialized object or null</returns>
        public static T Deserialize<T>(Stream stream)
        {
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                T result;
                using (StreamReader sr = new StreamReader(stream, Encoding.UTF8)) {
                    result = (T)serializer.Deserialize(sr);
                }
                return result;
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(string.Format("Error deserializing object: {0}", ex.Message));
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine(ex.InnerException.Message);
            }
            return default(T);
        } // Deserialize

        /// <summary>
        /// Deserialize a string to an object
        /// </summary>
        /// <param name="data">The string</param>
        /// <returns>The deserialized object or null</returns>
        public static T Deserialize<T>(string data)
        {
            if (!string.IsNullOrEmpty(data)) {
                try {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    T result;
                    using (StringReader sr = new StringReader(data)) {
                        result = (T)serializer.Deserialize(sr);
                    }
                    return result;
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(string.Format("Error deserializing object: {0}", ex.Message));
                    if (ex.InnerException != null)
                        System.Diagnostics.Debug.WriteLine(ex.InnerException.Message);
                }
            }
            return default(T);
        } // Deserialize

        /// <summary>
        /// Deserialize a byte array (UTF8) to an object
        /// </summary>
        /// <param name="data">The byte array</param>
        /// <returns>The deserialized object or null</returns>
        public static T Deserialize<T>(byte[] data)
        {
            if (data == null)
                return default(T);
            string str = Encoding.UTF8.GetString(data);
            return Deserialize<T>(str);
        } // Deserialize
        #endregion

        #region JSON serialization
        /// <summary>
        /// Serialize a object to a JSON string
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized object</returns>
        public static string SerializeToJSON(object obj)
        {
            if (obj == null)
                return string.Empty;
#if DEBUG
            Newtonsoft.Json.Formatting opt = Newtonsoft.Json.Formatting.Indented;
#else
            Newtonsoft.Json.Formatting opt = Newtonsoft.Json.Formatting.None;
#endif

            string res = JsonConvert.SerializeObject(obj, opt, 
                new JsonConverter[] { new Newtonsoft.Json.Converters.StringEnumConverter() });
            return res;
        } // SerializeToJSON
#endregion
    }
}
