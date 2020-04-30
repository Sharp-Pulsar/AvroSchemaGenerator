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
        }

        [Fact]
        public void TestStringRequiredDefault()
        {
            var actualSchema = typeof(StringRequiredDefaultTest).GetSchema();
            _output.WriteLine(actualSchema);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"StringRequiredDefaultTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"string\",\"default\":\"100\"}]}";
            Assert.Equal(expectedSchema, actualSchema);
        }
    }
}