using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace JsonToXml
{
    public static class JsonXmlConverter
    {
        private const string GuidFormat = "D";

        public static JObject XmlToJson(XElement xml)
        {
            if (xml == null) throw new ArgumentNullException("xml");

            var json = new JObject();

            foreach (XElement el in xml.Elements())
            {
                object value = ElementToObject(el);
                int arrayCount = -1;

                if (el.Attribute("a") != null)
                    arrayCount = (int) el.Attribute("a");

                string name = el.Name.LocalName;

                if (value is JArray || arrayCount >= 0)
                {
                    if (arrayCount > 0)
                    {
                        if (json[name] == null)
                        {
                            json.Add(new JProperty(name, new JArray()));
                        }

                        ((JArray) json[name]).Add(ElementToObject(el));
                    }
                    else
                    {
                        json.Add(new JProperty(name, new JArray()));
                    }
                }
                else if (value == null)
                {
                    json.Add(new JProperty(name, null));
                }
                else if (value is JObject)
                {
                    json.Add(new JProperty(name, ElementToObject(el)));
                }
                else
                {
                    json.Add(new JProperty(name, value));
                }
            }

            return json;
        }

        public static XElement JsonToXml(JObject json, string rootName = "Data")
        {
            if (json == null) throw new ArgumentNullException("json");
            if (rootName == null) throw new ArgumentNullException("rootName");
            if (rootName == "") throw new ArgumentException("RootName cannot be empty.", "rootName");

            var root = new XElement(rootName);
            SelectOrCreateAttribute(root, "t").Value = GetObjectType(json);

            foreach (JProperty prop in json.Properties())
            {
                var array = prop.Value as JArray;

                if (array != null)
                {
                    if (array.Count == 0)
                    {
                        // empty element with a-attribute only
                        root.Add(new XElement(prop.Name, new XAttribute("a", array.Count)));
                    }
                    else
                    {
                        foreach (JToken token in array)
                        {
                            XElement el = TokenToElement(token, prop.Name);
                            el.Add(new XAttribute("a", array.Count));
                            root.Add(el);
                        }
                    }
                }
                else
                {
                    root.Add(TokenToElement(prop.Value, prop.Name));
                }
            }

            return root;
        }

        private static object ElementToObject(XElement el)
        {
            string type = "o";

            XAttribute attr = el.Attribute("t");

            if (attr != null)
            {
                type = attr.Value;
            }

            switch (type)
            {
                case "n":
                    return null;
                case "s":
                    return (string) el;
                case "t":
                    return (DateTimeOffset) el;
                case "b":
                    return (bool) el;
                case "g":
                    return (Guid) el;
                case "i":
                    return (long) el;
                case "f":
                    return (double) el;
                case "o":
                    return XmlToJson(el);
            }

            throw new ArgumentException("Unknown type in t-attribute.");
        }

        private static XElement TokenToElement(JToken token, string name)
        {
            var obj = token as JObject;

            if (obj != null)
            {
                return JsonToXml(obj, name);
            }

            var el = new XElement(name);
            var value = token.ToObject<object>();

            SelectOrCreateAttribute(el, "t").Value = GetObjectType(value);

            if (value != null)
                el.Value = TokenToXml(token);

            return el;
        }

        private static string TokenToXml(JToken json)
        {
            JTokenType type = json.Type;

            Guid sink;
            if (type == JTokenType.String && Guid.TryParseExact(json.Value<string>(), GuidFormat, out sink))
            {
                type = JTokenType.Guid;
            }

            switch (type)
            {
                case JTokenType.Integer:
                    return XmlConvert.ToString(json.ToObject<long>());
                case JTokenType.Float:
                    return XmlConvert.ToString(json.ToObject<double>());
                case JTokenType.String:
                    return json.ToObject<string>();
                case JTokenType.Boolean:
                    return XmlConvert.ToString(json.ToObject<bool>());
                case JTokenType.Null:
                    return "";
                case JTokenType.Date:
                    return XmlConvert.ToString(json.ToObject<DateTimeOffset>());
                case JTokenType.Guid:
                    return XmlConvert.ToString(json.ToObject<Guid>());
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        private static string GetObjectType(object value)
        {
            if (value == null)
                return "n";

            var strValue = value as string;

            if (strValue != null)
            {
                Guid sink;
                if (Guid.TryParseExact(strValue, GuidFormat, out sink))
                    return "g";

                return "s";
            }

            if (value is byte || value is short || value is int || value is long)
            {
                return "i";
            }

            if (value is float || value is double) return "f";
            if (value is bool) return "b";
            if (value is DateTime || value is DateTimeOffset) return "t";
            if (value is Guid) return "g";

            if (value is JObject) return "o";

            throw new ArgumentOutOfRangeException("value");
        }

        private static XAttribute SelectOrCreateAttribute(XElement parent, string name)
        {
            XAttribute attr = parent.Attribute(name);

            if (attr == null)
            {
                attr = new XAttribute(name, "");
                parent.Add(attr);
            }

            return attr;
        }
    }
}