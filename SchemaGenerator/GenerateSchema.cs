using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace AvroSchemaGenerator
{//https://docs.oracle.com/database/nosql-12.1.3.0/GettingStartedGuide/avroschemas.html
    public static class GenerateSchema
    {
        private static List<string> _recursor = new List<string>();
        public static string GetSchema(this Type type)
        {
            _recursor.Clear();//need to clear this on each call
            var schema = new Dictionary<string, object>
            {
                {"type", "record"}, {"namespace", type.Namespace}, {"name", type.Name}
            };
            var propertiesCollections = new List<Dictionary<string, object>>();
            var properties = type.GetProperties();
            foreach (var p in properties)
            {
                var parsed = Parse(p);
                if (!parsed.Any())
                    continue;
                propertiesCollections.Add(parsed);
            }
            schema.Add("fields", propertiesCollections);
            return JsonSerializer.Serialize(schema);

        }
        private static List<Dictionary<string, object>> GetClassProperties(PropertyInfo property)
        {
            var t = property.PropertyType.GetProperties();
            var propertiesCollections = new List<Dictionary<string, object>>();
            var properties = property.PropertyType.GetProperties();
            foreach (var p in properties)
            {
                var parsed = Parse(p);
                if (!parsed.Any())
                    continue;
                propertiesCollections.Add(parsed);
            }

            return propertiesCollections;
        }
        private static List<Dictionary<string, object>> GetClassProperties(PropertyInfo[] properties)
        {
            var propertiesCollections = new List<Dictionary<string, object>>();
            foreach (var p in properties)
            {
                var parsed = Parse(p);
                if (!parsed.Any())
                    continue;
                propertiesCollections.Add(parsed);
            }

            return propertiesCollections;
        }

        private static Dictionary<string, object> Parse(PropertyInfo property)
        {
            var p = property;
            if (p.PropertyType.Namespace != null && ((p.PropertyType.IsClass || p.PropertyType.IsValueType) && !p.PropertyType.Namespace.StartsWith("System")))
            {
                var t= p.PropertyType.Name;
                if (!_recursor.Contains(t))
                {
                    _recursor.Add(t);
                    var required = p.GetCustomAttributes().required;
                    var schema2 = new Dictionary<string, object>
                    {
                        {"type", "record"}, {"namespace", p.PropertyType.Namespace}, {"name", p.PropertyType.Name}
                    };
                    var prop = GetClassProperties(p);
                    schema2.Add("fields", prop);
                    return required ? new Dictionary<string, object> { { "name", p.Name }, { "type", schema2 } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", schema2 } }, { "default", null } };
                }
                throw new StackOverflowException($"'{t}' is recursive, please fix it or use an array of '{t}' if that was your intention. More info: https://stackoverflow.com/questions/58757131/avro-schema-and-arrays");
            }

            if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)))
            {
                var required = p.GetCustomAttributes().required;
                var v = p.PropertyType.GetGenericArguments()[0];
                if (v.Namespace != null && (v.IsClass && !v.Namespace.StartsWith("System")))
                {
                    var schema2 = new Dictionary<string, object>
                    {
                        {"type", "record"}, {"namespace", v.Namespace}, {"name", v.Name}
                    };
                    var prop = GetClassProperties(v.GetProperties());
                    schema2.Add("fields", prop);
                    return required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schema2 } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object>{"null", new Dictionary<string, object> { { "type", "array" }, { "items", schema2 } } } } };
                }
                return required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object>{"null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } }};
            }

            if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>)))
            {
                var required = p.GetCustomAttributes().required;
                var v = p.PropertyType.GetGenericArguments()[1];
                if (v.Namespace != null && (v.IsClass && !v.Namespace.StartsWith("System")))
                {
                    var schema2 = new Dictionary<string, object>
                    {
                        {"type", "record"}, {"namespace", v.Namespace}, {"name", v.Name}
                    };
                    var prop = GetClassProperties(v.GetProperties());
                    schema2.Add("fields", prop);
                    return required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object>{{"type", "map"}, { "values", schema2 } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object>{"null", new Dictionary<string, object>{{"type", "map"}, { "values", schema2 } } }} };
                }
                return required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } }}  } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object>{"null", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } }} } };
            }

            if (p.PropertyType.IsEnum)
            {
                var dp = new Dictionary<string, object> { { "type", "enum" }, { "name", p.PropertyType.Name }, { "namespace", p.PropertyType.Namespace }, { "symbols", GetEnumValues(p.PropertyType) } };
                return new Dictionary<string, object> { { "name", p.PropertyType.Name }, { "type", dp } };

            }

            return GetField(p);
        }

        private static Dictionary<string, object> GetField(PropertyInfo p)
        {
            (bool isNullable, string name) dt;
            if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var t = p.PropertyType.GetGenericArguments()[0];
                dt = (true, t.Name);
            }
            else
            {
                dt = (false, p.PropertyType.Name);
            }
            var dT = ToAvroDataType(dt.name);
            var customAttributes = p.GetCustomAttributes();
            if (customAttributes.required && customAttributes.hasDefault)
            {
                //required and does have default value
                return new Dictionary<string, object> { { "name", p.Name }, { "type", dT }, { "default", customAttributes.defaultValue } };
            }

            if (customAttributes.required && !customAttributes.hasDefault)
            {
                //required and does not have default value
                return new Dictionary<string, object> { { "name", p.Name }, { "type", dT } };
            }
            if (!customAttributes.required && customAttributes.hasDefault)
            {
                //not required and does have default value
                return dt.isNullable ? new Dictionary<string, object> { { "name", p.Name }, { "type", new List<string> { "null", dT } }, { "default", customAttributes.defaultValue } } : new Dictionary<string, object> { { "name", p.Name }, { "type", dT }, { "default", customAttributes.defaultValue } };
            }
            //not required and does not have default value
            if(dt.isNullable)
               return new Dictionary<string, object> { { "name", p.Name }, { "type", new List<string> { "null", dT } }, { "default", null } };

            return dT == "string" ? new Dictionary<string, object> { { "name", p.Name }, { "type", new List<string> { "null", dT } }, { "default", null } } : new Dictionary<string, object> { { "name", p.Name }, { "type", dT }  };
        }
        private static List<string> GetEnumValues(Type type)
        {
            var list = new List<string>();
            var values = Enum.GetValues(type);
            foreach (var v in values)
            {
                list.Add(v.ToString());
            }
            return list;
        }
        private static string ToAvroDataType(string type)
        {
            switch (type)
            {
                case "Int32":
                    return "int";
                case "Int64":
                    return "long";
                case "String":
                    return "string";
                case "Double":
                    return "double";
                case "Single":
                    return "float";
                case "Boolean":
                    return "boolean";
                case "Byte[]":
                case "SByte[]":
                    return "bytes";
                default:
                    throw new ArgumentException($"{type} not supported");
            }
        }
    }
}
