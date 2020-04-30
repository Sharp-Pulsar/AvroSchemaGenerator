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
            var intTest = typeof(IntTest).GetSchema();
            _output.WriteLine(intTest);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"IntTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"int\"}]}";
            Assert.Equal(intTest, expectedSchema);
        }

        class IntNullableTest
        {
            public int? Age { get; set; }
        }

        [Fact]
        public void TestIntNullable()
        {
            var intTest = typeof(IntNullableTest).GetSchema();
            _output.WriteLine(intTest);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"IntNullableTest\",\"fields\":[{\"name\":\"Age\",\"type\":[\"null\",\"int\"],\"default\":null}]}";
            Assert.Equal(intTest, expectedSchema);
        }

        class IntDefaultTest
        {
            [DefaultValue(100)]
            public int Age { get; set; }
        }

        [Fact]
        public void TestIntDefault()
        {
            var intTest = typeof(IntDefaultTest).GetSchema();
            _output.WriteLine(intTest);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"IntDefaultTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"int\",\"default\":100}]}";
            Assert.Equal(intTest, expectedSchema);
        }

        class StringTest
        {
            public string Age { get; set; }
        }

        [Fact]
        public void TestString()
        {
            var intTest = typeof(StringTest).GetSchema();
            _output.WriteLine(intTest);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"StringTest\",\"fields\":[{\"name\":\"Age\",\"type\":[\"null\",\"string\"],\"default\":null}]}";
            Assert.Equal(intTest, expectedSchema);
        }

        class StringRequiredTest
        {
            [Required]
            public string Age { get; set; }
        }

        [Fact]
        public void TestStringRequired()
        {
            var intTest = typeof(StringRequiredTest).GetSchema();
            _output.WriteLine(intTest);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"StringRequiredTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"string\"}]}";
            Assert.Equal(intTest, expectedSchema);
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
            var intTest = typeof(StringRequiredDefaultTest).GetSchema();
            _output.WriteLine(intTest);
            var expectedSchema = "{\"type\":\"record\",\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"StringRequiredDefaultTest\",\"fields\":[{\"name\":\"Age\",\"type\":\"string\",\"default\":\"100\"}]}";
            Assert.Equal(intTest, expectedSchema);
        }
    }
}
