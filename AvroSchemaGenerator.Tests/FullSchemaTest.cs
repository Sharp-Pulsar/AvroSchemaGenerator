using AvroSchemaGenerator.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace AvroSchemaGenerator.Tests
{
    public class FullSchemaTest
    {

        private readonly ITestOutputHelper _output;

        public FullSchemaTest(ITestOutputHelper output)
        {
            this._output = output;
        }

        class IntTest
        {
            public int Age { get; set; }
            [Ignore]
            public bool IsTrue => Age > 40;
        }

        [Fact]
        public void TestInt()
        {
            var actualSchema = typeof(IntTest).GetSchema();
            _output.WriteLine(actualSchema);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"IntTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"int\"}]}";
            Assert.Equal(expectedSchema, actualSchema);
        }

        class IntNullableTest
        {
            public int? Age { get; set; }
        }

        [Fact]
        public void TestIntNullable()
        {
            var actualSchema = typeof(IntNullableTest).GetSchema();
            _output.WriteLine(actualSchema);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"IntNullableTest\",\"fields\":[{\"name\":\"Age\",\"type\":[\"null\",\"int\"],\"default\":null}]}";
            Assert.Equal(expectedSchema, actualSchema);
        }

        class IntDefaultTest
        {
            [DefaultValue(100)]
            public int Age { get; set; }
        }

        [Fact]
        public void TestIntDefault()
        {
            var actualSchema = typeof(IntDefaultTest).GetSchema();
            _output.WriteLine(actualSchema);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"IntDefaultTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"int\",\"default\":100}]}";
            Assert.Equal(expectedSchema, actualSchema);
        }

        class StringTest
        {
            public string Age { get; set; }
        }

        [Fact]
        public void TestString()
        {
            var actualSchema = typeof(StringTest).GetSchema();
            _output.WriteLine(actualSchema);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"StringTest\",\"fields\":[{\"name\":\"Age\",\"type\":[\"null\",\"string\"],\"default\":null}]}";
            Assert.Equal(expectedSchema, actualSchema);
        }

        class StringRequiredTest
        {
            [Required]
            public string Age { get; set; }
            [Ignore]
            public bool IsTrue => Age == "45";
        }

        [Fact]
        public void TestStringRequired()
        {
            var actualSchema = typeof(StringRequiredTest).GetSchema();
            _output.WriteLine(actualSchema);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"StringRequiredTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"string\"}]}";
            Assert.Equal(expectedSchema, actualSchema);
        }

        class StringRequiredDefaultTest
        {
            [Required]
            [DefaultValue("100")]
            public string Age { get; set; }

            [Ignore]
            public bool IsTrue => Age == "45";
        }

        [Fact]
        public void TestStringRequiredDefault()
        {
            var actualSchema = typeof(StringRequiredDefaultTest).GetSchema();
            _output.WriteLine(actualSchema);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"StringRequiredDefaultTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"string\",\"default\":\"100\"}]}";
            Assert.Equal(expectedSchema, actualSchema);
        }

        struct StructTest
        {
            public int Age { get; set; }
        }

        [Fact]
        public void TestStructTest()
        {
            var actualSchema = typeof(StructTest).GetSchema();
            _output.WriteLine(actualSchema);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"StructTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"int\"}]}";
            Assert.Equal(expectedSchema, actualSchema);
        }

        class ClassFieldTest
        {
            public IntTest Age { get; set; }
            [Ignore]
            public IntTest AgeIgnore { get; set; }
        }

        [Fact]
        public void TestClassField()
        {
            var actualSchema = typeof(ClassFieldTest).GetSchema();
            _output.WriteLine(actualSchema);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"ClassFieldTest\",\"fields\":[{\"name\":\"Age\",\"type\":[\"null\",{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"IntTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"int\"}]}],\"default\":null}]}";
                                //"{"type":"record","namespace":"AvroSchemaGenerator.Tests","name":"ClassFieldTest","fields":[{"name":"Age","type":["null",{"type":"record","namespace":"AvroSchemaGenerator.Tests","name":"Age","fields":[{"name":"Age","type":"int"}]}],"default":null}]}";
            Assert.Equal(expectedSchema, actualSchema);
        }

        class ClassFieldRequiredTest
        {
            [Required]
            public IntTest Age { get; set; }
            [Ignore]
            public DateTime TimestampAsDateTime => DateTime.FromBinary(long.MaxValue);
        }

        [Fact]
        public void TestClassFieldRequired()
        {
            var actualSchema = typeof(ClassFieldRequiredTest).GetSchema();
           _output.WriteLine(actualSchema);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"ClassFieldRequiredTest\",\"fields\":[{\"name\":\"Age\",\"type\":{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"IntTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"int\"}]}}]}";
            Assert.Equal(expectedSchema, actualSchema);
        }

        class StructFieldTest
        {
            [Required]
            public StructTest Age { get; set; }
        }

        [Fact]
        public void TestStructField()
        {
            var actualSchema = typeof(StructFieldTest).GetSchema();
            _output.WriteLine(actualSchema);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"StructFieldTest\",\"fields\":[{\"name\":\"Age\",\"type\":{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"StructTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"int\"}]}}]}";
            Assert.Equal(expectedSchema, actualSchema);
        }

        class RecType
        {
            public string Name { get; set; }
            public RecType Child { get; set; }
        }
        class RecTypeRequired
        {
            [Required]
            public string Name { get; set; }
            [Required]
            public RecTypeRequired Child { get; set; }
        }

        [Fact]
        public void TestRecType()
        {
            var expected = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"RecType\",\"fields\":[{\"name\":\"Name\",\"type\":[\"null\",\"string\"],\"default\":null},{\"name\":\"Child\",\"type\":[\"null\",\"RecType\"],\"default\":null}]}";
            var actual = typeof(RecType).GetSchema();
            _output.WriteLine(actual);
            Assert.Equal(expected, actual);
        }
        [Fact]
        public void TestRecTypeRequired()
        {
            var expected = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"RecTypeRequired\",\"fields\":[{\"name\":\"Name\",\"type\":\"string\"},{\"name\":\"Child\",\"type\":\"RecTypeRequired\"}]}";
            var actual = typeof(RecTypeRequired).GetSchema();
            _output.WriteLine(actual);
            Assert.Equal(expected, actual);
        }
    }
}