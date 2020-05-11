using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Xunit;
using Xunit.Abstractions;

namespace AvroSchemaGenerator.Tests
{
    [Collection("etSchemaTest")]
    public class GetSchemaTest
    {
        private readonly ITestOutputHelper _output;

        public GetSchemaTest(ITestOutputHelper output)
        {
            this._output = output;
        }
        [Fact]
        public void TestSimpleFoo()
        {
            var simple = typeof(SimpleFoo).GetSchema();
            _output.WriteLine(simple);
            Assert.Contains("Age", simple);
        }

        [Fact]
        public void TestFoo()
        {
            var simple = typeof(Foo).GetSchema();
            _output.WriteLine(simple);
            Assert.Contains("Age", simple);
        }
        [Fact]
        public void TestFooCustom()
        {
            var simplecu = typeof(FooCustom).GetSchema();
            _output.WriteLine(simplecu);
            Assert.Contains("EntryYear", simplecu);
            Assert.Contains("Level", simplecu);
        }
    }

    public class SimpleFoo
    {
        [Required]
        public int Age { get; set; }
        [Required]
        public string Name { get; set; }
        public long FactTime { get; set; }
        public double Point { get; set; }
        public float Precision { get; set; }
        public bool Attending { get; set; }
        public byte[] Id { get; set; }
    }
    public class Foo
    {
        public SimpleFoo Fo { get; set; }
        [Required]
        public List<string> Courses { get; set; }
        [Required]
        public Dictionary<string, string> Normal { get; set; }

    }
    public class FooCustom
    {
        public SimpleFoo Fo { get; set; }
        public List<Course> Courses { get; set; }
        public Dictionary<string, Lecturers> Departments { get; set; }
    }

    public class Course
    {
        [DefaultValue("200")]
        [Required]
        public string Level { get; set; }

        [Required]
        public int Year { get; set; }

        [DefaultValue("Closed")]
        public string State { get; set; }

        public string Gender { get; set; }
    }

    public class Lecturers
    {
        public int EntryYear { get; set; }
        public string Name { get; set; }
    }
}
