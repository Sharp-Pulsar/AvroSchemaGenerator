using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace AvroSchemaGenerator
{
    //https://docs.oracle.com/database/nosql-12.1.3.0/GettingStartedGuide/avroschemas.html
    //https://www.jsonschemavalidator.net/
    //https://json-schema-validator.herokuapp.com/avro.jsp
    public static class GenerateSchema
    {
        public static string GetSchema(this Type type)
        {
            var schema = new Dictionary<string, object>
            {
                {"type", "record"},
                {"namespace", type.Namespace},
                {"name", type.Name},
                {"fields", new List<Dictionary<string, object>>() }
            };
            var properties = type.GetProperties();
            foreach (var p in properties)
            {
                Parse(p, schema);
            }
            return JsonSerializer.Serialize(schema);
        }
        
        private static void Parse(PropertyInfo property, Dictionary<string, object> finalSchema)
        {
            var p = property;
            if (IsUserDefined(p))
            {
                var t = p.PropertyType.Name;
                var dt = p.DeclaringType?.Name;
                var recursive = t.Equals(dt);
                if(recursive)
                    GetResuseProperties(p, finalSchema);
                else
                    GetUserDefinedProperties(p, finalSchema);
                return;
            }

            if (IsList(p))
            {
                var required = p.GetSchemaCustomAttributes().required;
                var v = p.PropertyType.GetGenericArguments()[0];
                var dt = p.DeclaringType?.Name;
                var recursive = v.Name.Equals(dt);
                if (recursive)
                    GetResuseProperties(p, finalSchema);
                else if (IsUserDefined(v))
                {
                    var schema = GetGenericUserDefinedProperties(v, required);
                    var row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } };
                    var field = (List<Dictionary<string, object>>)finalSchema["fields"];
                    field.Add(row);
                    finalSchema["fields"] = field;
                }
                else
                {
                    var rw = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };
                    var fd = (List<Dictionary<string, object>>)finalSchema["fields"];
                    fd.Add(rw);
                    finalSchema["fields"] = fd;
                }
                return;
            }

            if (IsDictionary(p))
            {
                var required = p.GetSchemaCustomAttributes().required;
                var v = p.PropertyType.GetGenericArguments()[1];
                var dt = p.DeclaringType?.Name;
                var recursive = v.Name.Equals(dt);
                if (recursive)
                    GetResuseProperties(p, finalSchema, v.Name);
                else if (IsUserDefined(v))
                {
                    var schema = GetGenericUserDefinedProperties(v, required);
                    var row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } };
                    var field = (List<Dictionary<string, object>>)finalSchema["fields"];
                    field.Add(row);
                    finalSchema["fields"] = field;
                }
                else
                {
                    var rw = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } };
                    var fd = (List<Dictionary<string, object>>)finalSchema["fields"];
                    fd.Add(rw);
                    finalSchema["fields"] = fd;
                }
                return;
            }

            if (p.PropertyType.IsEnum)
            {
                var dp = new Dictionary<string, object> { { "type", "enum" }, { "name", p.PropertyType.Name }, { "namespace", p.PropertyType.Namespace }, { "symbols", GetEnumValues(p.PropertyType) } };
                var row = new Dictionary<string, object> { { "name", p.PropertyType.Name }, { "type", dp } };
                var fd = (List<Dictionary<string, object>>)finalSchema["fields"];
                fd.Add(row);
                finalSchema["fields"] = fd;
                return;
            }

            GetProperties(p, finalSchema);
        }
        
        private static Dictionary<string, object> GetParse(PropertyInfo property)
        {
            var p = property;
            if (IsUserDefined(p))
            {
                var t = p.PropertyType.Name;
                var dt = p.DeclaringType?.Name;
                var recursive = t.Equals(dt);
                return recursive ? ResuseProperties(p) : UserDefinedProperties(p);
            }

            if (IsList(p))
            {
                var required = p.GetSchemaCustomAttributes().required;
                var v = p.PropertyType.GetGenericArguments()[0];
                var dt = p.DeclaringType?.Name;
                var recursive = v.Name.Equals(dt);
                if (recursive)
                    return ResuseProperties(p);
                if (IsUserDefined(v))
                {
                    var schema = GetGenericUserDefinedProperties(v, required);
                    var row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } };
                    return row;
                } 
                return required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };
                
            }

            if (IsDictionary(p))
            {
                var required = p.GetSchemaCustomAttributes().required;
                var v = p.PropertyType.GetGenericArguments()[1];
                var dt = p.DeclaringType?.Name;
                var recursive = v.Name.Equals(dt);
                if (recursive)
                    return ResuseProperties(p);
                if (IsUserDefined(v))
                {
                    var schema = new Dictionary<string, object>
                    {
                        {"type", "record"}, {"namespace", v.Namespace}, {"name", v.Name}
                    };

                    schema["fields"] = GetGenericUserDefinedProperties(v, required);
                    var row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } };
                    return row;
                }
                return required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } };

            }

            if (p.PropertyType.IsEnum)
            {
                var dp = new Dictionary<string, object> { { "type", "enum" }, { "name", p.PropertyType.Name }, { "namespace", p.PropertyType.Namespace }, { "symbols", GetEnumValues(p.PropertyType) } };
                var row = new Dictionary<string, object> { { "name", p.PropertyType.Name }, { "type", dp } };
                return row;
            }

            return GetProperty(p);
        }

        private static void GetProperties(PropertyInfo p, Dictionary<string, object> finalSchema)
        {
            var row = GetField(p);
            var field = (List<Dictionary<string, object>>)finalSchema["fields"];
            field.Add(row);
            finalSchema["fields"] = field;
        }
        private static void GetResuseProperties(PropertyInfo p, Dictionary<string, object> finalSchema, string dt = "")
        {
            var row = ReUseSchema(p, dt);
            var field = (List<Dictionary<string, object>>)finalSchema["fields"];
            field.Add(row);
            finalSchema["fields"] = field;
        }
        
        private static Dictionary<string, object> ResuseProperties(PropertyInfo p)
        {
            return ReUseSchema(p);
        }
        private static Dictionary<string, object> GetProperty(PropertyInfo p)
        {
            return GetField(p);
        }

        private static Dictionary<string, object> ReUseSchema(PropertyInfo p, string type = "")
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
            var customAttributes = p.GetSchemaCustomAttributes();
            var field= Field(string.IsNullOrWhiteSpace(type)? dt.name: type, p.Name, customAttributes.required, customAttributes.hasDefault, customAttributes.defaultValue, dt.isNullable);
            return field;
        }
        private static Dictionary<string, object> ReUseSchema(PropertyInfo p, bool isnullable)
        {
            var customAttributes = p.GetSchemaCustomAttributes();
            var field = Field(p.Name, p.Name, customAttributes.required, customAttributes.hasDefault, customAttributes.defaultValue, isnullable);
            return field;
        }
        private static Dictionary<string, object> ReUseSchema(string name, string type, bool required, bool hasDefault, object defaultValue, bool isNullable)
        {
            var field = Field(type, name, required, hasDefault, defaultValue, isNullable);
            return field;
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
            var field = Field(dT, p.Name, customAttributes.required, customAttributes.hasDefault, customAttributes.defaultValue, dt.isNullable);
            return field;
        }
        private static Dictionary<string, object> Field(string type, string name, bool required, bool hasDefault, object dfault, bool nullable)
        {
            if (required && hasDefault)
            {
                //required and does have default value
                return new Dictionary<string, object> { { "name", name }, { "type", type }, { "default", dfault } };
            }

            if (required)
            {
                //required and does not have default value
                return new Dictionary<string, object> { { "name", name }, { "type", type } };
            }
            if (hasDefault)
            {
                //not required and does have default value
                return nullable ? new Dictionary<string, object> { { "name", name }, { "type", new List<string> { "null", type } }, { "default", dfault } } : new Dictionary<string, object> { { "name", name }, { "type", type }, { "default", dfault } };
            }
            //not required and does not have default value
            return nullable ? new Dictionary<string, object> { { "name", name }, { "type", new List<string> { "null", type } }, { "default", null } } : ReturnField(type, name);
        }

        private static Dictionary<string, object> ReturnField(string type, string name)
        {
            switch (type)
            {
                case "int":
                case "long":
                case "double":
                case "float":
                case "boolean":
                    return new Dictionary<string, object> { { "name", name }, { "type", type } };
                default:
                    return new Dictionary<string, object>
                        {{"name", name}, {"type", new List<string> {"null", type}}, {"default", null}};
            }
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

        private static void GetUserDefinedProperties(PropertyInfo property, Dictionary<string, object> finalSchema)
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
                    var t = p.PropertyType.Name;
                    var dt = p.DeclaringType?.Name;
                    var recursive = t.Equals(dt);
                    if (recursive)
                        fieldProperties.Add(ReUseSchema(p));
                    else
                      Parse(p, finalSchema);
                }
                else if (IsList(p))
                {
                    var require = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[0];
                    var dt = p.DeclaringType?.Name;
                    var recursive = v.Name.Equals(dt);
                    if (recursive)
                        GetResuseProperties(p, finalSchema);
                    else if (IsUserDefined(v))
                    {
                        var schem = GetGenericUserDefinedProperties(v, require);
                        var row1 = require ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schem } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schem } } } } };
                        fieldProperties.Add(row1);
                    }
                    else
                    {
                        var rw = require ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };
                        fieldProperties.Add(rw);
                    }
                }
                else
                {
                    fieldProperties.Add(GetProperty(p));
                }
            }
            var required = property.GetSchemaCustomAttributes().required;
            schema["fields"] = fieldProperties;
            var row = required ? new Dictionary<string, object> { { "name", property.Name }, { "type", schema } } : new Dictionary<string, object> { { "name", property.Name }, { "type", new List<object> { "null", schema } }, { "default", null } };

            var field = (List<Dictionary<string, object>>)finalSchema["fields"];
            field.Add(row);
            finalSchema["fields"] = field;
        }
        private static Dictionary<string, object> UserDefinedProperties(PropertyInfo property)
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
                    var t = p.PropertyType.Name;
                    var dt = p.DeclaringType?.Name;
                    var recursive = t.Equals(dt);
                    fieldProperties.Add(recursive ? ReUseSchema(p) : GetParse(p));
                }
                else if (IsList(p))
                {
                    var require = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[0];
                    var dt = p.DeclaringType?.Name;
                    var isnullable = p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                    var customAttributes = p.GetSchemaCustomAttributes();
                    var recursive = v.Name.Equals(dt);
                    if (recursive)
                    {
                        fieldProperties.Add(ReUseSchema(p.Name, v.Name, customAttributes.required, customAttributes.hasDefault, customAttributes.defaultValue, isnullable));
                    }
                    else if (IsUserDefined(v))
                    {
                        var schem = GetGenericUserDefinedProperties(v, require);
                        var row1 = require ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schem } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schem } } } } };
                        fieldProperties.Add(row1);
                    }
                    else
                    {
                        var rw = require ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };
                        fieldProperties.Add(rw);
                    }
                }
                else
                {
                    fieldProperties.Add(GetProperty(p));
                }
            }
            var required = property.GetSchemaCustomAttributes().required;
            schema["fields"] = fieldProperties;
            var row = required ? new Dictionary<string, object> { { "name", property.Name }, { "type", schema } } : new Dictionary<string, object> { { "name", property.Name }, { "type", new List<object> { "null", schema } }, { "default", null } };
            return row;
        }

        private static Dictionary<string, object> GetGenericUserDefinedProperties(Type property, bool required)
        {
            var schema = new Dictionary<string, object>
            {
                {"type", "record"}, {"namespace", property.Namespace}, {"name", property.Name}
            };

            var properties = property.GetProperties();
            var fieldProperties = new List<Dictionary<string, object>>();
            foreach (var p in properties)
            {
                if (IsUserDefined(p))
                {
                    var t = p.PropertyType.Name;
                    var dt = p.DeclaringType?.Name;
                    var recursive = t.Equals(dt);
                    if (recursive)
                        fieldProperties.Add(ReUseSchema(p));
                    else
                        fieldProperties.Add(GetParse(p));
                }
                else if (IsList(p))
                {
                    var require = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[0];
                    var dt = p.DeclaringType?.Name;
                    var isnullable = p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                    var customAttributes = p.GetSchemaCustomAttributes();
                    var recursive = v.Name.Equals(dt);
                    if (recursive)
                    { 
                        fieldProperties.Add(ReUseSchema(p.Name, v.Name, customAttributes.required, customAttributes.hasDefault, customAttributes.defaultValue, isnullable ));
                    }
                    else if (IsUserDefined(v))
                    {
                        var schema2 = GetGenericUserDefinedProperties(v, require);
                        var row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schema2 } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schema2 } } } } };
                        fieldProperties.Add(row);
                    }
                    else
                    {
                        var rw = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };
                        fieldProperties.Add(rw);
                    }
                }
                else if (IsDictionary(p))
                {
                    var require = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[1];
                    var dt = p.DeclaringType?.Name;
                    var isnullable = p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                    var customAttributes = p.GetSchemaCustomAttributes();

                    var recursive = v.Name.Equals(dt);
                    if (recursive)
                        fieldProperties.Add(ReUseSchema(p.Name, v.Name, customAttributes.required, customAttributes.hasDefault, customAttributes.defaultValue, isnullable));
                    else if (IsUserDefined(v))
                    {
                       var schemaD = GetGenericUserDefinedProperties(v, require);
                        var row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", schemaD } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", schemaD } } } } };
                        fieldProperties.Add(row);
                    }
                    else
                    {
                         var rr = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } };
                         fieldProperties.Add(rr);
                    }
                }
                else
                {
                    fieldProperties.Add(GetProperty(p));
                }
            }
            schema["fields"] = fieldProperties;
            return schema; 

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
        private static bool IsDictionary(Type p)
        {
            return p.IsGenericType &&
                   p.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
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
