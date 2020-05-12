using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace AvroSchemaGenerator
{//https://docs.oracle.com/database/nosql-12.1.3.0/GettingStartedGuide/avroschemas.html
    //https://www.jsonschemavalidator.net/
    public static class GenerateSchema
    {
        private static List<string> _recursor = new List<string>();
        private static Dictionary<string, object> _schema = new Dictionary<string, object>();
        private static object _lockObj = new object();
        public static string GetSchema(this Type type)
        {
            lock (_lockObj)
            {
                _schema.Clear(); 
                _schema = new Dictionary<string, object>
                {
                    {"type", "record"}, 
                    {"namespace", type.Namespace}, 
                    {"name", type.Name},
                    {"fields", new List<Dictionary<string, object>>() }
                };
                var properties = type.GetProperties();
                foreach (var p in properties)
                {
                    Parse(p);
                }
                return JsonSerializer.Serialize(_schema);
            }
        }
        
        private static void Parse(PropertyInfo property, int cyclCount = 0)
        {
            var p = property;
            if (IsUserDefined(p))
            {
                var t= p.PropertyType.Name;
                var dt = p.DeclaringType?.Name;
                var recursive = t.Equals(dt);
                GetUserDefinedProperties(p, cyclCount, t, recursive);
                return;
            }

            if (IsList(p))
            {
                var required = p.GetSchemaCustomAttributes().required;
                var v = p.PropertyType.GetGenericArguments()[0];
                var dt = p.DeclaringType?.Name;
                if (IsUserDefined(v))
                {
                    var recursive = v.Name.Equals(dt);
                    var schema = new Dictionary<string, object>
                    {
                        {"type", "record"}, {"namespace", v.Namespace}, {"name", v.Name}
                    };

                    schema["fields"] = GetGenericUserDefinedProperties(v, cyclCount, p.Name, recursive, required);
                    var row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } };
                    var field = (List<Dictionary<string, object>>)_schema["fields"];
                    field.Add(row);
                    _schema["fields"] = field;
                }
                else
                {
                    var rw = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };
                    var fd = (List<Dictionary<string, object>>)_schema["fields"];
                    fd.Add(rw);
                    _schema["fields"] = fd;
                }
                return;
            }

            if (IsDictionary(p))
            {
                var required = p.GetSchemaCustomAttributes().required;
                var v = p.PropertyType.GetGenericArguments()[1];
                var dt = p.DeclaringType?.Name;
                var recursive = v.Name.Equals(dt);
                if (IsUserDefined(v))
                {
                    var schema = new Dictionary<string, object>
                    {
                        {"type", "record"}, {"namespace", v.Namespace}, {"name", v.Name}
                    };

                    schema["fields"] = GetGenericUserDefinedProperties(v, cyclCount, p.Name, recursive, required);
                    var row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } };
                    var field = (List<Dictionary<string, object>>)_schema["fields"];
                    field.Add(row);
                    _schema["fields"] = field;
                }
                else
                {
                    var rw = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } };
                    var fd = (List<Dictionary<string, object>>)_schema["fields"];
                    fd.Add(rw);
                    _schema["fields"] = fd;
                }
                return;
            }

            if (p.PropertyType.IsEnum)
            {
                var dp = new Dictionary<string, object> { { "type", "enum" }, { "name", p.PropertyType.Name }, { "namespace", p.PropertyType.Namespace }, { "symbols", GetEnumValues(p.PropertyType) } };
                var row = new Dictionary<string, object> { { "name", p.PropertyType.Name }, { "type", dp } };
                var fd = (List<Dictionary<string, object>>)_schema["fields"];
                fd.Add(row);
                _schema["fields"] = fd;
                return;
            }

            GetProperties(p);
        }

        private static Dictionary<string, object> ParseList(PropertyInfo property, int cyclCount = 0)
        {
            var p = property;
            var required = p.GetSchemaCustomAttributes().required;
            var v = p.PropertyType.GetGenericArguments()[0];
            var dt = p.DeclaringType?.Name;
            var recursive = v.Name.Equals(dt);
            if (IsUserDefined(v))
            {
                var schema = new Dictionary<string, object>
                {
                    {"type", "record"}, {"namespace", v.Namespace}, {"name", v.Name}
                };

                schema["fields"] = GetGenericUserDefinedProperties(v, cyclCount, p.Name, recursive, required);
                return required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } };
            }
            return required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };

        }

        private static void GetProperties(PropertyInfo p)
        {
            var row = GetField(p);
            var field = (List<Dictionary<string, object>>)_schema["fields"];
            field.Add(row);
            _schema["fields"] = field;
        }
        private static Dictionary<string, object> GetProperty(PropertyInfo p)
        {
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
            var customAttributes = p.GetSchemaCustomAttributes();
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

        private static bool IsUserDefined(PropertyInfo p)
        {
            return p.PropertyType.Namespace != null && ((p.PropertyType.IsClass || p.PropertyType.IsValueType) &&
                                                        !p.PropertyType.Namespace.StartsWith("System"));
        }
        private static bool IsUserDefined(Type p)
        {
            return p.Namespace != null && ((p.IsClass || p.IsValueType) && !p.Namespace.StartsWith("System"));
        }

        private static void GetUserDefinedProperties(PropertyInfo property, int cycleCount, string propertyName, bool isRecursive)
        {
            var schema = new Dictionary<string, object>
            {
                {"type", "record"}, {"namespace", property.PropertyType.Namespace}, {"name", property.PropertyType.Name}
            };

            var properties = property.PropertyType.GetProperties();
            var fieldProperties = new List<Dictionary<string, object>>();
            foreach (var p in properties)
            {
                if (IsUserDefined(p))
                {
                    if (!isRecursive && !propertyName.Equals(p.Name) && cycleCount < 2)
                        Parse(p, cycleCount++);
                }
                else
                {
                    fieldProperties.Add(GetProperty(p));
                }
            }
            var required = property.GetSchemaCustomAttributes().required;
            schema["fields"] = fieldProperties;
            var row = required ? new Dictionary<string, object> { { "name", property.Name }, { "type", schema } } : new Dictionary<string, object> { { "name", property.Name }, { "type", new List<object> { "null", schema } }, { "default", null } };

            var field = (List<Dictionary<string, object>>)_schema["fields"];
            field.Add(row);
            _schema["fields"] = field;
        }
        private static Dictionary<string, object> GetGenericUserDefinedProperties(Type property, int cycleCount, string propertyName, bool isRecursive, bool required)
        {
            var schema = new Dictionary<string, object>
            {
                {"type", "record"}, {"namespace", property.Namespace}, {"name", property.Name}
            };

            var properties = property.GetProperties();
            var fieldProperties = new List<Dictionary<string, object>>();
            foreach (var p in properties)
            {
                if (IsUserDefined(p) || IsList(p) || IsDictionary(p))
                {
                    if (!isRecursive && !_recursor.Contains(p.Name))
                    {
                        _recursor.Add(p.Name);
                        fieldProperties.Add(ParseList(p, cycleCount++));
                    }
                    else
                    {
                        if (!propertyName.Equals(p.Name) && cycleCount < 2 && !_recursor.Contains(p.Name))
                        {
                            _recursor.Add(p.Name);
                            fieldProperties.Add(ParseList(p, cycleCount++));
                        }
                    }
                }
                else
                {
                    fieldProperties.Add(GetProperty(p));
                }
            }
            schema["fields"] = fieldProperties;
            return required ? new Dictionary<string, object> { { "name", property.Name }, { "type", schema } } : new Dictionary<string, object> { { "name", property.Name }, { "type", new List<object> { "null", schema } }, { "default", null } };

        }
        private static bool IsList(PropertyInfo p)
        {
            return p.PropertyType.IsGenericType &&
                   p.PropertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }
        private static bool IsDictionary(PropertyInfo p)
        {
           return p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
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
