using System;

namespace AvroSchemaGenerator.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class LogicalTypeAttribute : Attribute
    {
        public LogicalTypeKind Kind { get; }
        public LogicalTypeAttribute(LogicalTypeKind kind)
        {
            Kind = kind;
        }
    }
    public enum LogicalTypeKind
    {
        Date,
        TimeMicrosecond,
        TimeMillisecond,
        TimestampMicrosecond,
        TimestampMillisecond
    }
}
