using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avro.Util;
using AvroSchemaGenerator.Attributes;

namespace AvroSchemaGenerator
{
    //https://docs.oracle.com/database/nosql-12.1.3.0/GettingStartedGuide/avroschemas.html
    //https://www.jsonschemavalidator.net/
    //https://json-schema-validator.herokuapp.com/avro.jsp
    //https://stackoverflow.com/questions/13522132/how-to-exclude-static-property-when-using-getproperties-method
    public static class GenerateSchema
    {
        public static string GetSchema(this Type type)
        {
            var schema = new Dictionary<string, object>
            {
                {"type", "record"},
                {"namespace", type.Namespace},
                {"name", type.Name}
            };
            var aliases = GetAliases(type);
            if (aliases != null)
            {
                schema["aliases"] = aliases;
            }
            schema["fields"] = new List<Dictionary<string, object>>();
            var existingTypes = new List<string>();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public) ;
            foreach (var p in properties)
            {
                
                if (!ShouldIgnore(p))
                  PropertyInfo(p, schema, existingTypes);
            }
            return JsonSerializer.Serialize(schema);
        }
        
        private static void PropertyInfo(PropertyInfo property, Dictionary<string, object> finalSchema, List<string> existingTypes)
        {
            var p = property;
            if(p.PropertyType.FullName.StartsWith("Avro."))
            {
                AddFields(p, finalSchema);
            }
            else
            {
                if (IsUserDefined(p))
                {
                    if (p.PropertyType.IsEnum)
                    {
                        var aliases = GetAliases(p);
                        var row = GetEnumField(p);
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        var fd = (List<Dictionary<string, object>>)finalSchema["fields"];
                        fd.Add(row);
                        finalSchema["fields"] = fd;
                        return;
                    }
                    var t = p.PropertyType.Name;
                    var dt = p.DeclaringType?.Name;
                    var recursive = t.Equals(dt);
                    
                    if (existingTypes.Contains(t))
                    {
                        var row = new Dictionary<string, object> { { "name", p.Name }, { "type", t } };
                        var fd = (List<Dictionary<string, object>>)finalSchema["fields"];
                        fd.Add(row);
                        finalSchema["fields"] = fd;
                        return;
                    }
                    if (recursive)
                        AddReuseType(p, finalSchema);
                    else
                        GetUserDefinedProperties(p, finalSchema, existingTypes);

                    existingTypes.Add(t);
                    return;
                }

                if (IsFieldListType(p))
                {
                    var required = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[0];
                    var dt = p.DeclaringType?.Name;
                    var recursive = v.Name.Equals(dt);
                    var aliases = GetAliases(p);
                    Dictionary<string, object> row;
                    if (recursive)
                        AddReuseType(p, finalSchema);
                    else if (IsUserDefined(v))
                    {
                        var schema = GetGenericUserDefinedProperties(v, required);
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        var field = (List<Dictionary<string, object>>)finalSchema["fields"];
                        field.Add(row);
                        finalSchema["fields"] = field;
                    }
                    else
                    {
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        var fd = (List<Dictionary<string, object>>)finalSchema["fields"];
                        fd.Add(row);
                        finalSchema["fields"] = fd;
                    }
                    return;
                }

                if (IsFieldDictionaryType(p))
                {
                    var required = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[1];
                    var dt = p.DeclaringType?.Name;
                    var recursive = v.Name.Equals(dt);
                    var aliases = GetAliases(p);
                    Dictionary<string, object> row;
                    if (recursive)
                        AddReuseType(p, finalSchema, v.Name);
                    else if (IsUserDefined(v))
                    {
                        var schema = GetGenericUserDefinedProperties(v, required);
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        var field = (List<Dictionary<string, object>>)finalSchema["fields"];
                        field.Add(row);
                        finalSchema["fields"] = field;
                    }
                    else
                    {
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        var fd = (List<Dictionary<string, object>>)finalSchema["fields"];
                        fd.Add(row);
                        finalSchema["fields"] = fd;
                    }
                    return;
                }

                if (p.PropertyType.IsEnum)
                {
                    var aliases = GetAliases(p);
                    var row = GetEnumField(p);
                    if (aliases != null)
                    {
                        var rows = row.ToList();
                        rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                        row = rows.ToDictionary(x => x.Key, x => x.Value);
                    }
                    var fd = (List<Dictionary<string, object>>)finalSchema["fields"];
                    fd.Add(row);
                    finalSchema["fields"] = fd;
                    return;
                }

                AddFields(p, finalSchema);

            }
        }
        
        private static Dictionary<string, object> PropertyInfo(PropertyInfo property, List<string> existingTypes)
        {
            var p = property;
            if(p.PropertyType.FullName.StartsWith("Avro."))
            {
               return GetField(p);
            }
            else
            {
                if (IsUserDefined(p))
                {
                    if (p.PropertyType.IsEnum)
                    {
                        var aliases = GetAliases(p);
                        var row = GetEnumField(p);
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        return row;
                    }
                    else
                    {

                        var t = p.PropertyType.Name;
                        var dt = p.DeclaringType?.Name;
                        var recursive = t.Equals(dt);

                        if (existingTypes.Contains(t))
                        {
                            return new Dictionary<string, object> { { "name", p.Name }, { "type", t } };
                        }
                        else if (recursive)
                        {
                            existingTypes.Add(t);
                            return Reuse(p);
                        }
                        else
                        {
                            existingTypes.Add(t);
                            return PropertyInfo(p);//throw new Exception($"The limit for user defined property type has been reached: [{dt}] public {p.PropertyType.Name} {p.Name} {{get; set;}}"); 

                        }

                    }
                }

                if (IsFieldListType(p))
                {
                    var required = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[0];
                    var dt = p.DeclaringType?.Name;
                    var recursive = v.Name.Equals(dt);
                    var aliases = GetAliases(p);
                    Dictionary<string, object> row;
                    if (recursive)
                        return Reuse(p);
                    else if (IsUserDefined(v))
                    {
                        var schema = GetGenericUserDefinedProperties(v, required);
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        return row;
                    }
                    else
                    {
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        return row;
                    }
                }

                if (IsFieldDictionaryType(p))
                {
                    var required = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[1];
                    var dt = p.DeclaringType?.Name;
                    var recursive = v.Name.Equals(dt);
                    var aliases = GetAliases(p);
                    Dictionary<string, object> row;
                    if (recursive)
                        return Reuse(p, v.Name);
                    else if (IsUserDefined(v))
                    {
                        var schema = GetGenericUserDefinedProperties(v, required);
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        return row;
                    }
                    else
                    {
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        return row;
                    }
                }

                if (p.PropertyType.IsEnum)
                {
                    var aliases = GetAliases(p);
                    var row = GetEnumField(p);
                    if (aliases != null)
                    {
                        var rows = row.ToList();
                        rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                        row = rows.ToDictionary(x => x.Key, x => x.Value);
                    }
                    return row;
                }

                return GetField(p);

            }
        }
        
        private static Dictionary<string, object> PropertyInfo(PropertyInfo property)
        {
            var p = property;
            if(p.PropertyType.FullName.StartsWith("Avro."))
            {
               return GetField(p);
            }
            else
            {
                if (IsUserDefined(p))
                {
                    if (p.PropertyType.IsEnum)
                    {
                        var aliases = GetAliases(p);
                        var row = GetEnumField(p);
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        return row;
                    }
                    else
                    {

                        var t = p.PropertyType.Name;
                        var dt = p.DeclaringType?.Name;
                        var recursive = t.Equals(dt);
                        if (recursive)
                        {
                            return Reuse(p);
                        }
                        else
                        {
                            //existingTypes.Add(t);
                            throw new Exception($"[Recursive loop] limit for user defined property type has been reached: [{dt}] public {p.PropertyType.Name} {p.Name} {{get; set;}}"); 

                        }

                    }
                }

                if (IsFieldListType(p))
                {
                    var required = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[0];
                    var dt = p.DeclaringType?.Name;
                    var recursive = v.Name.Equals(dt);
                    var aliases = GetAliases(p);
                    Dictionary<string, object> row;
                    if (recursive)
                        return Reuse(p);
                    else if (IsUserDefined(v))
                    {
                        var schema = GetGenericUserDefinedProperties(v, required);
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        return row;
                    }
                    else
                    {
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        return row;
                    }
                }

                if (IsFieldDictionaryType(p))
                {
                    var required = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[1];
                    var dt = p.DeclaringType?.Name;
                    var recursive = v.Name.Equals(dt);
                    var aliases = GetAliases(p);
                    Dictionary<string, object> row;
                    if (recursive)
                        return Reuse(p, v.Name);
                    else if (IsUserDefined(v))
                    {
                        var schema = GetGenericUserDefinedProperties(v, required);
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        return row;
                    }
                    else
                    {
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } };
                        if (aliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                        }
                        return row;
                    }
                }

                if (p.PropertyType.IsEnum)
                {
                    var aliases = GetAliases(p);
                    var row = GetEnumField(p);
                    if (aliases != null)
                    {
                        var rows = row.ToList();
                        rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                        row = rows.ToDictionary(x => x.Key, x => x.Value);
                    }
                    return row;
                }

                return GetField(p);

            }
        }
        
        private static Dictionary<string, object> GetPropertyInfo(PropertyInfo property)
        {
            var p = property;
            var aliases = GetAliases(p);
            Dictionary<string, object> row;
            if (IsUserDefined(p))
            {
                if (p.PropertyType.IsEnum)
                {
                    row = GetEnumField(p);
                    if (aliases != null)
                    {
                        var rows = row.ToList();
                        rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                        return rows.ToDictionary(x => x.Key, x => x.Value);
                    }
                    return row;
                }
                var t = p.PropertyType.Name;
                var dt = p.DeclaringType?.Name;
                var recursive = t.Equals(dt);
                return recursive ? ReuseType(p) : UserDefinedProperties(p);
            }
            if (IsFieldListType(p))
            {
                var required = p.GetSchemaCustomAttributes().required;
                var v = p.PropertyType.GetGenericArguments()[0];
                var dt = p.DeclaringType?.Name;
                var recursive = v.Name.Equals(dt);
                if (recursive)
                    return ReuseType(p);
                if (IsUserDefined(v))
                {
                    var schema = GetGenericUserDefinedProperties(v, required);
                    row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schema } } } } };
                }
                else
                    row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };

                if (aliases != null)
                {
                    var rows = row.ToList();
                    rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                    return rows.ToDictionary(x => x.Key, x => x.Value);
                }
                return row;
            }
            else
            {
                if (IsFieldDictionaryType(p))
                {
                    var required = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[1];
                    var dt = p.DeclaringType?.Name;
                    var recursive = v.Name.Equals(dt);
                    if (recursive)
                        return ReuseType(p);
                    if (IsUserDefined(v))
                    {
                        var schema = new Dictionary<string, object>
                        {
                            {"type", "record"}, {"namespace", v.Namespace}, {"name", v.Name}
                        };

                        schema["fields"] = GetGenericUserDefinedProperties(v, required);
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", schema } } } } };

                    }
                    else
                        row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } };

                    if (aliases != null)
                    {
                        var rows = row.ToList();
                        rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                        return rows.ToDictionary(x => x.Key, x => x.Value);
                    }
                    return row;
                }
            }

            return GetFields(p);
        }

        private static void AddFields(PropertyInfo p, Dictionary<string, object> finalSchema)
        {
            var row = GetField(p);
            var field = (List<Dictionary<string, object>>)finalSchema["fields"];
            field.Add(row);
            finalSchema["fields"] = field;
        }
        private static void AddReuseType(PropertyInfo p, Dictionary<string, object> finalSchema, string dt = "")
        {
            var row = Reuse(p, dt);
            var field = (List<Dictionary<string, object>>)finalSchema["fields"];
            field.Add(row);
            finalSchema["fields"] = field;
        }
        
        private static Dictionary<string, object> ReuseType(PropertyInfo p)
        {
            return Reuse(p);
        }
        private static Dictionary<string, object> GetFields(PropertyInfo p)
        {
            return GetField(p);
        }

        private static Dictionary<string, object> Reuse(PropertyInfo p, string type = "")
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
            var field= Field(string.IsNullOrWhiteSpace(type)? dt.name: type, p.Name, customAttributes.required, customAttributes.hasDefault, customAttributes.defaultValue, dt.isNullable, GetAliases(p));
            return field;
        }
        
        private static Dictionary<string, object> ReUseSchema(string name, string type, bool required, bool hasDefault, object defaultValue, bool isNullable, List<string> aliases)
        {
            var field = Field(type, name, required, hasDefault, defaultValue, isNullable, aliases);
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
            var aliases = GetAliases(p);
            var field = Field(dT, p.Name, customAttributes.required, customAttributes.hasDefault, customAttributes.defaultValue, dt.isNullable, aliases);
            return field;
        }
        private static Dictionary<string, object> Field(object type, string name, bool required, bool hasDefault, object dfault, bool nullable, List<string> aliases)
        {
            Dictionary<string, object> fields;
            if (required && hasDefault)
            {
                //required and does have default value
                fields = new Dictionary<string, object> { { "name", name }, { "type", type }, { "default", dfault } };
            }

            else if (required)
            {
                //required and does not have default value
                fields = new Dictionary<string, object> { { "name", name }, { "type", type } };
            }
            else if (hasDefault)
            {
                //not required and does have default value
                fields = nullable ? new Dictionary<string, object> { { "name", name }, { "type", new List<object> { "null", type } }, { "default", dfault } } : new Dictionary<string, object> { { "name", name }, { "type", type }, { "default", dfault } };
            }
            else
               //not required and does not have default value
               fields = nullable ? new Dictionary<string, object> { { "name", name }, { "type", new List<object> { "null", type } }, { "default", null } } : RequiredOrNullableField(type, name);
            if (aliases != null)
            {
                var rows = fields.ToList();
                rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                return rows.ToDictionary(x => x.Key, x => x.Value);
            }

            return fields;
        }

        private static Dictionary<string, object> RequiredOrNullableField(object type, string name)
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
                        {{"name", name}, {"type", new List<object> {"null", type}}, {"default", null}};
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

        private static List<string> GetAliases(Type type)
        {
            var aliases = (AliasesAttribute)type.GetCustomAttribute(typeof(AliasesAttribute));
            return aliases?.Values;
        }
        private static bool ShouldIgnore(MemberInfo property)
        {
            var ignore = (IgnoreAttribute)property.GetCustomAttribute(typeof(IgnoreAttribute));
            return ignore != null;
        }
        private static List<string> GetAliases(MemberInfo property)
        {
            var aliases = (AliasesAttribute)property.GetCustomAttribute(typeof(AliasesAttribute));
            return aliases?.Values;
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

        private static void GetUserDefinedProperties(PropertyInfo property, Dictionary<string, object> finalSchema, List<string> existing)
        {
            var schema = new Dictionary<string, object>
            {
                {"type", "record"}, {"namespace", property.PropertyType.Namespace}, {"name", property.PropertyType.Name}
            };
            var aliases = GetAliases(property);
            var properties = property.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var fieldProperties = new List<Dictionary<string, object>>();
            foreach (var p in properties)
            {
                if (ShouldIgnore(p))
                    continue;
                if (IsUserDefined(p))
                {
                    var t = p.PropertyType.Name;
                    var dt = p.DeclaringType?.Name;
                    var recursive = t.Equals(dt);
                    if (existing.Contains(t))
                    {
                        var rw = new Dictionary<string, object> { { "name", p.Name }, { "type", t } };
                        fieldProperties.Add(rw);
                    }
                    else if (recursive)
                    {
                        existing.Add(t);
                        fieldProperties.Add(Reuse(p));
                    }
                    else if (p.PropertyType.IsEnum)
                    {
                        var pAli = GetAliases(p);
                        if (aliases != null)
                        {
                            var rows = GetEnumField(p).ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", pAli));
                            fieldProperties.Add(rows.ToDictionary(x => x.Key, x => x.Value));
                        }
                        else fieldProperties.Add(GetEnumField(p));
                        existing.Add(t);
                    }
                    else
                    {
                        //existing.Add(t);
                        var rows = PropertyInfo(p, existing);
                        fieldProperties.Add(rows);
                    }
                }
                else if (IsFieldListType(p))
                {
                    var require = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[0];
                    var dt = p.DeclaringType?.Name;
                    var recursive = v.Name.Equals(dt);
                    if (recursive)
                        AddReuseType(p, finalSchema);
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
                    fieldProperties.Add(GetFields(p));
                }
            }
            var required = property.GetSchemaCustomAttributes().required;
            schema["fields"] = fieldProperties;
            var row = required ? new Dictionary<string, object> { { "name", property.Name }, { "type", schema } } : new Dictionary<string, object> { { "name", property.Name }, { "type", new List<object> { "null", schema } }, { "default", null } };
            if (aliases != null)
            {
                var rows = row.ToList();
                rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                row = rows.ToDictionary(x => x.Key, x => x.Value);
            }
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
            var properties = property.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var fieldProperties = new List<Dictionary<string, object>>();
            foreach (var p in properties)
            {
                if (ShouldIgnore(p))
                    continue;
                if (IsUserDefined(p))
                {
                    var t = p.PropertyType.Name;
                    var dt = p.DeclaringType?.Name;
                    var recursive = t.Equals(dt);
                    fieldProperties.Add(recursive ? Reuse(p) : GetPropertyInfo(p));
                }
                else if (IsFieldListType(p))
                {
                    var require = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[0];
                    var dt = p.DeclaringType?.Name;
                    var isnullable = p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                    var customAttributes = p.GetSchemaCustomAttributes();
                    var recursive = v.Name.Equals(dt);
                    if (recursive)
                    {
                        fieldProperties.Add(ReUseSchema(p.Name, v.Name, customAttributes.required, customAttributes.hasDefault, customAttributes.defaultValue, isnullable, GetAliases(p)));
                    }
                    else
                    {
                        Dictionary<string, object> r;
                        var ali = GetAliases(v);
                        if (IsUserDefined(v))
                        {
                            var schem = GetGenericUserDefinedProperties(v, require);
                            r= require ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schem } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schem } } } } };
                            
                        }
                        else
                        {
                            r = require ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };
                            
                        }

                        if (ali != null)
                        {
                            var rows = r.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", ali));
                            fieldProperties.Add(rows.ToDictionary(x=> x.Key, x=> x.Value));
                        }
                        else fieldProperties.Add(r);
                    }
                }
                else
                {
                    fieldProperties.Add(GetFields(p));
                }
            }
            var required = property.GetSchemaCustomAttributes().required;
            schema["fields"] = fieldProperties;
            var aliases = GetAliases(property);
            var row = required ? new Dictionary<string, object> { { "name", property.Name }, { "type", schema } } : new Dictionary<string, object> { { "name", property.Name }, { "type", new List<object> { "null", schema } }, { "default", null } };
            if (aliases != null)
            {
                var rows = row.ToList();
                rows.Insert(1, new KeyValuePair<string, object>("aliases", aliases));
                return rows.ToDictionary(x => x.Key, x => x.Value);
            }
            return row;
        }

        private static Dictionary<string, object> GetGenericUserDefinedProperties(Type property, bool required)
        {
            var schema = new Dictionary<string, object>
            {
                {"type", "record"}, {"namespace", property.Namespace}, {"name", property.Name}
            };
            var aliases = GetAliases(property);
            if (aliases != null)
                schema["aliases"] = aliases;
            var properties = property.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var fieldProperties = new List<Dictionary<string, object>>();
            foreach (var p in properties)
            {
                if (ShouldIgnore(p))
                    continue;
                if (IsUserDefined(p))
                {
                    var t = p.PropertyType.Name;
                    var dt = p.DeclaringType?.Name;
                    var recursive = t.Equals(dt);
                    if (recursive)
                        fieldProperties.Add(Reuse(p));
                    else
                        fieldProperties.Add(GetPropertyInfo(p));//treat
                }
                else if (IsFieldListType(p))
                {
                    var require = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[0];
                    var dt = p.DeclaringType?.Name;
                    var isnullable = p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                    var customAttributes = p.GetSchemaCustomAttributes();
                    var recursive = v.Name.Equals(dt);
                    if (recursive)
                    { 
                        fieldProperties.Add(ReUseSchema(p.Name, v.Name, customAttributes.required, customAttributes.hasDefault, customAttributes.defaultValue, isnullable, GetAliases(p)));
                    }
                    else if (IsUserDefined(v))
                    {
                        var pAliases = GetAliases(p);
                        var schema2 = GetGenericUserDefinedProperties(v, require);
                        var row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", schema2 } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", schema2 } } } } };
                        if (pAliases != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", pAliases));
                            fieldProperties.Add(rows.ToDictionary(x=> x.Key, x => x.Value));
                        }
                        else fieldProperties.Add(row);
                    }
                    else
                    {
                        var pAliases = GetAliases(p);
                        var rw = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "array" }, { "items", ToAvroDataType(p.PropertyType.GetGenericArguments()[0].Name) } } } } };
                        if (pAliases != null)
                        {
                            var rows = rw.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", pAliases));
                            fieldProperties.Add(rows.ToDictionary(x => x.Key, x => x.Value));
                        }
                        else fieldProperties.Add(rw);
                    }
                }
                else if (IsFieldDictionaryType(p))
                {
                    var require = p.GetSchemaCustomAttributes().required;
                    var v = p.PropertyType.GetGenericArguments()[1];
                    var dt = p.DeclaringType?.Name;
                    var isnullable = p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                    var customAttributes = p.GetSchemaCustomAttributes();

                    var recursive = v.Name.Equals(dt);
                    if (recursive)
                        fieldProperties.Add(ReUseSchema(p.Name, v.Name, customAttributes.required, customAttributes.hasDefault, customAttributes.defaultValue, isnullable, GetAliases(p)));
                    else
                    {
                        var ali = GetAliases(p);
                        Dictionary<string, object> row = null;
                        if (IsUserDefined(v))
                        {
                            var schemaD = GetGenericUserDefinedProperties(v, require);
                            row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", schemaD } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", schemaD } } } } };
                        }
                        else
                        {
                            row = required ? new Dictionary<string, object> { { "name", p.Name }, { "type", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } : new Dictionary<string, object> { { "name", p.Name }, { "type", new List<object> { "null", new Dictionary<string, object> { { "type", "map" }, { "values", ToAvroDataType(p.PropertyType.GetGenericArguments()[1].Name) } } } } };
                        }

                        if (ali != null)
                        {
                            var rows = row.ToList();
                            rows.Insert(1, new KeyValuePair<string, object>("aliases", ali));
                            row = rows.ToDictionary(x => x.Key, x => x.Value);
                            fieldProperties.Add(row);
                        }
                        else fieldProperties.Add(row);
                    }
                }
                else
                {
                    fieldProperties.Add(GetFields(p));
                }
            }
            schema["fields"] = fieldProperties;
            return schema; 

        }
        private static bool IsFieldListType(PropertyInfo p)
        {
            return p.PropertyType.IsGenericType &&
                   p.PropertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }
        private static bool IsFieldDictionaryType(PropertyInfo p)
        {
           return p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }
        private static Dictionary<string, object> GetEnumField(PropertyInfo p)
        {
            var dp = new Dictionary<string, object> { { "type", "enum" }, { "name", p.PropertyType.Name }, { "namespace", p.PropertyType.Namespace }, { "symbols", GetEnumValues(p.PropertyType) } };
            var row = new Dictionary<string, object> { { "name", p.Name }, { "type", dp } };
            return row;
        }
        private static object ToAvroDataType(string type)
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
                case "DateTime":
                case "Date":
                    return new Dictionary<string, object> { { "type", "int"}, { "logicalType", "date"} };                
                case "Decimal":
                case "BigInteger":
                case "AvroDecimal":
                    return new Dictionary<string, object> { { "type", "bytes" }, { "logicalType", "decimal" }, { "precision", 4 }, { "scale", 2 } };
                default:
                    throw new ArgumentException($"{type} not supported");
            }
        }
    }
}
