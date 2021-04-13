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
        public void LogicalDate()
        {
            SpecificDatumReader<MessageDateKind> reader;
            SpecificDatumWriter<MessageDateKind> writer;
            var simple = typeof(MessageDateKind).GetSchema();
            _output.WriteLine(simple);
            var schema = (RecordSchema)Schema.Parse(simple);
            reader = new SpecificDatumReader<MessageDateKind>(schema, schema);
            writer = new SpecificDatumWriter<MessageDateKind>(schema);
            var msgBytes = Write(new MessageDateKind {Schema = schema, CreatedTime = DateTime.Now, DayOfWeek = "Saturday", Size = new AvroDecimal(102.65M) }, writer);
            using var stream = new MemoryStream((byte[])(object)msgBytes);
            var msg = Read(stream, reader);
            Assert.NotNull(msg);
            Assert.True(msg.Size == 102.65M);
        }
        [Fact]
        public void LogicalTime()
        {
            SpecificDatumReader<MessageTimeKind> reader;
            SpecificDatumWriter<MessageTimeKind> writer;
            var simple = typeof(MessageTimeKind).GetSchema();
            _output.WriteLine(simple);
            var schema = (RecordSchema)Schema.Parse(simple);
            reader = new SpecificDatumReader<MessageTimeKind>(schema, schema);
            writer = new SpecificDatumWriter<MessageTimeKind>(schema);
            var msgBytes = Write(new MessageTimeKind { Schema = schema, TimeMicros = TimeSpan.FromSeconds(60), TimeMillis = TimeSpan.FromSeconds(300) }, writer);
            using var stream = new MemoryStream((byte[])(object)msgBytes);
            var msg = Read(stream, reader);
            Assert.NotNull(msg);
        }
        [Fact]
        public void LogicalTimeStamp()
        {
            SpecificDatumReader<MessageTimestampKind> reader;
            SpecificDatumWriter<MessageTimestampKind> writer;
            var simple = typeof(MessageTimestampKind).GetSchema();
            _output.WriteLine(simple);
            var schema = (RecordSchema)Schema.Parse(simple);
            reader = new SpecificDatumReader<MessageTimestampKind>(schema, schema);
            writer = new SpecificDatumWriter<MessageTimestampKind>(schema);
            var msgBytes = Write(new MessageTimestampKind { Schema = schema, StampMicros = DateTime.Now, StampMillis = DateTime.Now.AddDays(20)}, writer);
            using var stream = new MemoryStream((byte[])(object)msgBytes);
            var msg = Read(stream, reader);
            Assert.NotNull(msg);
        }
        private sbyte[] Write<T>(T message, SpecificDatumWriter<T> writer)
        {
            var ms = new MemoryStream();
            Encoder e = new BinaryEncoder(ms);
            writer.Write(message, e);
            ms.Flush();
            ms.Position = 0;
            var b = ms.ToArray();
            return (sbyte[])(object)b;
        }
        public T Read<T>(Stream stream, SpecificDatumReader<T> reader)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return reader.Read(default, new BinaryDecoder(stream));
        }
    }
    public class MessageTimeKind: ISpecificRecord
    {
        [LogicalType(LogicalTypeKind.TimeMicrosecond)]
        public TimeSpan TimeMicros { get; set; }

        [LogicalType(LogicalTypeKind.TimeMillisecond)]
        public TimeSpan TimeMillis { get; set; }

        [Ignore]
        public Schema Schema { get; set; }

        public object Get(int fieldPos)
        {
            switch (fieldPos)
            {
                case 0: return TimeMicros;
                case 1: return TimeMillis;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
            };
        }

        public void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: TimeMicros = (TimeSpan)fieldValue; break;
                case 1: TimeMillis = (TimeSpan)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            };
        }
    }
    public class MessageTimestampKind: ISpecificRecord
    {
        [LogicalType(LogicalTypeKind.TimestampMicrosecond)]
        public DateTime StampMicros { get; set; }

        [LogicalType(LogicalTypeKind.TimestampMillisecond)]
        public DateTime StampMillis { get; set; }

        [Ignore]
        public Schema Schema { get; set; }

        public object Get(int fieldPos)
        {
            switch (fieldPos)
            {
                case 0: return StampMicros;
                case 1: return StampMillis;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
            };
        }

        public void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: StampMicros = (DateTime)fieldValue; break;
                case 1: StampMillis = (DateTime)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            };
        }
    }
    public class MessageDateKind: ISpecificRecord
    {
        [LogicalType(LogicalTypeKind.Date)]
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
