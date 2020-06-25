using System;
using System.Collections.Generic;
using System.Linq;

namespace AvroSchemaGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    public class AliasesAttribute : Attribute
    {
        private readonly List<string> _values;

        public AliasesAttribute(params string[] value)
        {
            _values = value.ToList();
        }
        public virtual List<string> Values => _values;
    }
}
