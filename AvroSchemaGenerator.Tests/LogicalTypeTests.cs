using System;
using System.IO;
using Avro;
using Avro.Generic;
using Avro.IO;
using Avro.Reflect;
using Avro.Specific;
using Avro.Util;
using AvroSchemaGenerator.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace AvroSchemaGenerator.Tests
{
    [Collection("LogicalTypeTests")]
    public class LogicalTypeTests
    {
        private readonly ITestOutputHelper _output;

        public LogicalTypeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void LogicalTypes()
        {
            SpecificDatumReader<MessageDate> reader;
            SpecificDatumWriter<MessageDate> writer;
            var simple = typeof(MessageDate).GetSchema();
            _output.WriteLine(simple);
            var schema = (RecordSchema)Schema.Parse(simple);
            reader = new SpecificDatumReader<MessageDate>(schema, schema);
            writer = new SpecificDatumWriter<MessageDate>(schema);
            var msgBytes = Write(new MessageDate {Schema = schema, CreatedTime = DateTime.Now, DayOfWeek = "Saturday", Size = new AvroDecimal(102.65M) }, writer);
            using var stream = new MemoryStream((byte[])(object)msgBytes);
            var msg = Read(stream, reader);
            Assert.NotNull(msg);
            Assert.True(msg.Size == 102.65M);
        }
        private sbyte[] Write(MessageDate message, SpecificDatumWriter<MessageDate> writer)
        {
            var ms = new MemoryStream();
            Encoder e = new BinaryEncoder(ms);
            writer.Write(message, e);
            ms.Flush();
            ms.Position = 0;
            var b = ms.ToArray();
            return (sbyte[])(object)b;
        }
        public MessageDate Read(Stream stream, SpecificDatumReader<MessageDate> reader)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return reader.Read(null, new BinaryDecoder(stream));
        }
    }
    public class MessageDate: ISpecificRecord
    {
        public DateTime CreatedTime { get; set; }
        public AvroDecimal Size { get; set; }
        public string DayOfWeek { get; set; }

        [Ignore]
        public Schema Schema { get; set; }

        public object Get(int fieldPos)
        {
            switch (fieldPos)
            {
                case 0: return CreatedTime;
                case 1: return Size;
                case 2: return DayOfWeek;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
            };
        }

        public void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: CreatedTime = (DateTime)fieldValue; break;
                case 1: Size = (AvroDecimal)fieldValue; break;
                case 2: DayOfWeek = (System.String)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            };
        }
    }
}
