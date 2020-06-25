using System;

namespace AvroSchemaGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    public class AliasesAttribute : Attribute
    {
        private readonly string _value;

        public AliasesAttribute(string value)
        {
            _value = value;
        }
        public virtual string Value => _value;
    }
}
