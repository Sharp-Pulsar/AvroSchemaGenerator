using System;

namespace AvroSchemaGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AvroSchemaAttribute : Attribute
    {
        public string Value { get; }

        public AvroSchemaAttribute(string value)
        {
            Value = value;
        }
    }
}