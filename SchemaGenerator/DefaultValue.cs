using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace AvroSchemaGenerator
{
    public static class DefaultValue
    {
        public static (bool required, bool hasDefault, object defaultValue) GetSchemaCustomAttributes(this PropertyInfo property)
        {
            var required = IsRequiredProperty(property);
            var dv = GetDefaultValueForProperty(property);
            return (required, dv.hasDefault, dv.defaultValue);
        }
        private static (bool hasDefault, object defaultValue) GetDefaultValueForProperty(PropertyInfo property)
        {
            var defaultAttr = (DefaultValueAttribute)property.GetCustomAttribute(typeof(DefaultValueAttribute));
            return defaultAttr != null ? (true, defaultAttr.Value) : (false, null);
        }
        private static bool IsRequiredProperty(PropertyInfo property)
        {
            return property.GetCustomAttribute(typeof(RequiredAttribute)) != null;
        }
        
    }
}
