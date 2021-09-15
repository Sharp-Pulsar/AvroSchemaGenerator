using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Avro;
using Avro.IO;
using Avro.Reflect;
using AvroSchemaGenerator.Attributes;
using AvroSchemaGenerator.Tests;
using Xunit;
using Xunit.Abstractions;

namespace AvroSchemaGenerator.Tests
{
    [Collection("GetSchemaTest")]
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
            var schema = Schema.Parse(simple);
            var writer = new ReflectWriter<SimpleFoo>(schema);
            Assert.Contains("Age", simple);
        }

        [Fact]
        public void TestFoo()
        {
            var simple = typeof(Foo).GetSchema();
            _output.WriteLine(simple);
            var schema = Schema.Parse(simple);
            var writer = new ReflectWriter<Foo>(schema);
            Assert.Contains("Age", simple);
        }
        [Fact]
        public void TestFooCustom()
        {
            var simplecu = typeof(FooCustom).GetSchema();
            _output.WriteLine(simplecu);
            var schema = Schema.Parse(simplecu);
            var writer = new ReflectWriter<FooCustom>(schema);
            Assert.Contains("EntryYear", simplecu);
            Assert.Contains("Level", simplecu);
        }
        [Fact]
        public void TestRecursiveArray()
        {
            var actual = typeof(Family).GetSchema();
            _output.WriteLine(actual);
            var schema = Schema.Parse(actual);
            var writer = new ReflectWriter<Family>(schema);

            Assert.True(true);
        }
        [Fact]
        public void TestDictionaryRecursiveArray()
        {
            var actual = typeof(Dictionary).GetSchema();
            _output.WriteLine(actual);
            var schema = Schema.Parse(actual);
            var writer = new ReflectWriter<Dictionary>(schema);

            Assert.True(true);
        }
        [Fact]
        public void TestDictionaryRecursive()
        {
            try
            {
                var actual = typeof(DictionaryRecursive).GetSchema();
                var schema = Schema.Parse(actual);
                var writer = new ReflectWriter<DictionaryRecursive>(schema);
                _output.WriteLine(actual);
            }
            catch (Exception ex)
            {
                _output.WriteLine(ex.ToString());
                Assert.True(false);
            }
        }
        [Fact(Skip = "Stack overflow exception in Apache Avro")]
        public void TestRecursiveSchema()
        {
            try
            {
                var actual = typeof(Recursive).GetSchema();
                var schema = Schema.Parse(actual);
                _output.WriteLine(actual);
                var recursive = new Recursive
                {
                    Fo = new SimpleFoo
                    {
                        Age = 67,
                        Attending = true,
                        FactTime = 90909099L,
                        Id = new byte[0] { },
                        Name = "Ebere",
                        Point = 888D,
                        Precision = 787F
                    },
                    Recurse = new Recursive
                    {
                        Fo = new SimpleFoo
                        {
                            Age = 6,
                            Attending = false,
                            FactTime = 90L,
                            Id = new byte[0] { },
                            Name = "Ebere Abanonu",
                            Point = 88D,
                            Precision = 78F
                        },
                    }
                };
                var writer = new ReflectWriter<Recursive>(schema);
                var reader = new ReflectReader<Recursive>(schema, schema);
                var msgBytes = Write(recursive, writer);
                using var stream = new MemoryStream((byte[])(object)msgBytes);
                var msg = Read(stream, reader);
                Assert.NotNull(msg);
            }
            catch (Exception ex)
            {
                _output.WriteLine(ex.ToString());
                Assert.True(false);
            }
        }
        [Fact]
        public void TestEnums()
        {
            var expectSchema = "{\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"MediaStream\",\"type\":\"record\",\"fields\":[{\"name\":\"Id\",\"type\":[\"null\",\"string\"]},{\"name\":\"Title\",\"type\":[\"null\",\"string\"]},{\"name\":\"Type\",\"type\":{\"namespace\":\"AvroSchemaGenerator.Tests\",\"type\":\"enum\",\"name\":\"MediaType\",\"symbols\":[\"Video\",\"Audio\"]}},{\"name\":\"Container\",\"type\":{\"namespace\":\"AvroSchemaGenerator.Tests\",\"type\":\"enum\",\"name\":\"MediaContainer\",\"symbols\":[\"Flv\",\"Mp3\",\"Avi\",\"Mp4\"]}},{\"name\":\"Media\",\"type\":[\"null\",\"bytes\"]}]}";
            var actual = typeof(MediaStream).GetSchema();
            var schema = Schema.Parse(actual);
            var writer = new ReflectWriter<MediaStream>(schema);
            _output.WriteLine(actual);

            Assert.Equal(expectSchema, actual);
        }
        [Fact]
        public void TestAliasesList()
        {
            var expectSchema = "{\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"ClassWithAliasesWithList\",\"aliases\":[\"InterLives\",\"CountrySide\"],\"type\":\"record\",\"fields\":[{\"name\":\"City\",\"aliases\":[\"TownHall\",\"Province\"],\"type\":[\"null\",\"string\"]},{\"name\":\"State\",\"type\":[\"null\",\"string\"]},{\"name\":\"Movie\",\"aliases\":[\"PopularMovie\"],\"type\":[\"null\",{\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"InnerAliases\",\"type\":\"record\",\"fields\":[{\"name\":\"Container\",\"aliases\":[\"Media\"],\"type\":{\"namespace\":\"AvroSchemaGenerator.Tests\",\"type\":\"enum\",\"name\":\"MediaContainer\",\"symbols\":[\"Flv\",\"Mp3\",\"Avi\",\"Mp4\"]}},{\"name\":\"Title\",\"type\":[\"null\",\"string\"]}]}]},{\"name\":\"Popular\",\"aliases\":[\"PopularMediaType\"],\"type\":{\"namespace\":\"AvroSchemaGenerator.Tests\",\"type\":\"enum\",\"name\":\"MediaType\",\"symbols\":[\"Video\",\"Audio\"]}},{\"name\":\"Movies\",\"aliases\":[\"MovieCollection\"],\"type\":[\"null\",{\"type\":\"array\",\"items\":{\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"MovieAliase\",\"type\":\"record\",\"aliases\":[\"Movies_Aliase\"],\"fields\":[{\"name\":\"Dated\",\"aliases\":[\"DateCreated\"],\"type\":\"long\"},{\"name\":\"Year\",\"aliases\":[\"ReleaseYear\"],\"type\":\"int\"},{\"name\":\"Month\",\"aliases\":[\"ReleaseMonth\"],\"type\":{\"namespace\":\"AvroSchemaGenerator.Tests\",\"type\":\"enum\",\"name\":\"Month\",\"symbols\":[\"January\",\"February\",\"March\",\"April\",\"June\",\"July\"]}}]}}]}]}";
            var actual = typeof(ClassWithAliasesWithList).GetSchema();
            _output.WriteLine(actual);
            var schema = Schema.Parse(actual);
            var writer = new ReflectWriter<ClassWithAliasesWithList>(schema);

            Assert.Equal(expectSchema, actual);
        }
        [Fact]
        public void TestAliasesDictionary()
        {
            var actual = typeof(ClassWithAliasesWithDictionary).GetSchema();
            _output.WriteLine(actual);
            var schema = Schema.Parse(actual);
            var writer = new ReflectWriter<ClassWithAliasesWithDictionary>(schema);

            Assert.True(true);
        }
        [Fact]
        public void TestStaticFieldsAreIgnored()
        {
            var expectSchema = "{\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"SimpleWithStaticFieldsAndNestedClass\",\"type\":\"record\",\"fields\":[{\"name\":\"FactTime\",\"type\":\"long\"},{\"name\":\"SimpleStaticInner\",\"type\":[\"null\",{\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"SimpleWithStaticFields\",\"type\":\"record\",\"fields\":[{\"name\":\"Name\",\"type\":[\"null\",\"string\"]}]}]}]}";
            var actual = typeof(SimpleWithStaticFieldsAndNestedClass).GetSchema();
            _output.WriteLine(actual);
            var schema = Schema.Parse(actual);
            var writer = new ReflectWriter<SimpleWithStaticFieldsAndNestedClass>(schema);

            Assert.Equal(expectSchema, actual);
        }
        [Fact]
        public void TestEnumType()
        {
            var expectSchema = "{\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"Month\",\"type\":\"enum\",\"symbols\":[\"January\",\"February\",\"March\",\"April\",\"June\",\"July\"]}";
            var actual = typeof(Month).GetSchema();
            _output.WriteLine(actual);
            var schema = Schema.Parse(actual);
            var writer = new ReflectWriter<Month>(schema);

            Assert.Equal(expectSchema, actual);
        }
        [Fact]
        public void TestNoNamespaceType()
        {
            var expectSchema = "{\"name\":\"ClassWithoutNamespace\",\"type\":\"record\",\"fields\":[{\"name\":\"ForReal\",\"type\":\"boolean\"},{\"name\":\"Comment\",\"type\":[\"null\",\"string\"]},{\"name\":\"Book\",\"type\":[\"null\",{\"namespace\":\"AvroSchemaGenerator.Tests\",\"name\":\"Book\",\"type\":\"record\",\"fields\":[{\"name\":\"Author\",\"type\":[\"null\",\"string\"]},{\"name\":\"Title\",\"type\":[\"null\",\"string\"]}]}]}]}";

            var actual = typeof(ClassWithoutNamespace).GetSchema();
            _output.WriteLine(actual);

            Assert.Equal(expectSchema, actual);
            var schema = Schema.Parse(actual);
            var data = new ClassWithoutNamespace
            {
                ForReal = true,
                Comment = "Harry Pull Requests",
                Book = new Book
                {
                    Author = "Ebere Abanonu",
                    Title = "How to skin a PR!!!"
                }
            };
            var writer = new ReflectWriter<ClassWithoutNamespace>(schema);
            var reader = new ReflectReader<ClassWithoutNamespace>(schema, schema);
            var msgBytes = Write(data, writer);
            using var stream = new MemoryStream((byte[])(object)msgBytes);
            var msg = Read(stream, reader);
            Assert.NotNull(msg);
            //Assert.True(msg[0] == 10);
            //Assert.True(msg[1] == 100);
        }
        [Fact]
        public void TestListType()
        {
            var expectSchema = "{\"namespace\":\"System.Collections.Generic\",\"type\":\"array\",\"items\":\"long\",\"default\":[]}";
            var actual = typeof(List<long>).GetSchema();
            _output.WriteLine(actual);

            Assert.Equal(expectSchema, actual);
            var schema = Schema.Parse(actual);
            var data = new List<long>()
            {
                10, 100
            };
            var writer = new ReflectWriter<List<long>>(schema);
            var reader = new ReflectReader<List<long>>(schema, schema);
            var msgBytes = Write(data, writer);
            using var stream = new MemoryStream((byte[])(object)msgBytes);
            var msg = Read(stream, reader);
            Assert.NotNull(msg);
            Assert.True(msg[0] == 10);
            Assert.True(msg[1] == 100);
        }
        [Fact]
        public void TestUserListType()
        {
            var actual = typeof(List<Book>).GetSchema();
            _output.WriteLine(actual);

            var schema = Schema.Parse(actual);
            var data = new List<Book>()
            {
                new Book{ Author = "Ebere Abanonu", Title="Avro 101" },
                new Book{ Author = "Apache Avro", Title = "Avro Schema" }
            };
            var writer = new ReflectWriter<List<Book>>(schema);
            var reader = new ReflectReader<List<Book>>(schema, schema);
            var msgBytes = Write(data, writer);
            using var stream = new MemoryStream((byte[])(object)msgBytes);
            var msg = Read(stream, reader);
            Assert.NotNull(msg);
            Assert.True(msg[0].Author == "Ebere Abanonu");
        }
        [Fact]
        public void TestDictionaryType()
        {
            var expectSchema = "{\"namespace\":\"System.Collections.Generic\",\"type\":\"map\",\"values\":\"long\",\"default\":{}}";
            var actual = typeof(Dictionary<string, long>).GetSchema();
            _output.WriteLine(actual);

            Assert.Equal(expectSchema, actual);
            var schema = Schema.Parse(actual);
            var data = new Dictionary<string, long>()
            {
                {"Index", 10 },
                {"Pos", 100 }
            };
            var writer = new ReflectWriter<Dictionary<string, long>>(schema);
            var reader = new ReflectReader<Dictionary<string, long>>(schema, schema);
            var msgBytes = Write(data, writer);
            using var stream = new MemoryStream((byte[])(object)msgBytes);
            var msg = Read(stream, reader);
            Assert.NotNull(msg);
            Assert.True(msg["Index"] == 10);
            Assert.True(msg["Pos"] == 100);
        }
        [Fact]
        public void TestUserDictionaryType()
        {
            var actual = typeof(Dictionary<string, Book>).GetSchema();
            _output.WriteLine(actual);

            var schema = Schema.Parse(actual);
            var data = new Dictionary<string, Book>()
            {
                {"First", new Book{ Author = "Ebere Abanonu", Title="Avro 101" } },
                {"Second", new Book{ Author = "Apache Avro", Title = "Avro Schema" } }
            };
            var writer = new ReflectWriter<Dictionary<string, Book>>(schema);
            var reader = new ReflectReader<Dictionary<string, Book>>(schema, schema);
            var msgBytes = Write(data, writer);
            using var stream = new MemoryStream((byte[])(object)msgBytes);
            var msg = Read(stream, reader);
            Assert.NotNull(msg);
        }

        private sbyte[] Write<T>(T message, ReflectWriter<T> writer)
        {
            var ms = new MemoryStream();
            Encoder e = new BinaryEncoder(ms);
            writer.Write(message, e);
            ms.Flush();
            ms.Position = 0;
            var b = ms.ToArray();
            return (sbyte[])(object)b;
        }
        public T Read<T>(Stream stream, ReflectReader<T> reader)
        {
            stream.Seek(0, SeekOrigin.Begin);
            return reader.Read(default, new BinaryDecoder(stream));
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
    public class Dictionary
    {
        public SimpleFoo Fo { get; set; }
        [Required]
        public List<string> Courses { get; set; }
        [Required]
        public Dictionary<string, Family> Families { get; set; }

    }
    public class DictionaryRecursive
    {
        public SimpleFoo Fo { get; set; }
        [Required]
        public List<string> Courses { get; set; }
        [Required]
        public Dictionary<string, Family> Diction { get; set; }

    }
    public class FooCustom
    {
        public SimpleFoo Fo { get; set; }
        public List<Course> Courses { get; set; }
        public Dictionary<string, Lecturers> Departments { get; set; }
    }
    public class Recursive
    {
        public SimpleFoo Fo { get; set; }
        public Recursive Recurse { get; set; }
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

    public class School
    {
        public string State { get; set; }
        public string Year { get; set; }
        public List<string> Schools { get; set; }
    }
    public class Lecturers
    {
        public int EntryYear { get; set; }
        public string Name { get; set; }
    }

    public class Book
    {
        public string Author { get; set; }
        public string Title { get; set; }
    }
    public class Family
    {
        [Required]
        public List<Person> Members { get; set; }
    }

    public class Person
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public School Schools { get; set; }
        public List<Book> Books { get; set; }
        [Required]
        public List<string> Children { get; set; }
    }

    [Aliases("InterLives", "CountrySide")]
    public sealed class ClassWithAliasesWithList
    {
        [Aliases("TownHall", "Province")]
        public string City { get; set; }
        public string State { get; set; }
        [Aliases("PopularMovie")]
        public InnerAliases Movie { get; set; }

        [Aliases("PopularMediaType")]
        public MediaType Popular { get; set; }

        [Aliases("MovieCollection")]
        public List<MovieAliase> Movies { get; set; }
    }

    [Aliases("InterLives", "CountrySide")]
    public sealed class ClassWithAliasesWithDictionary
    {
        [Aliases("TownHall", "Province")]
        public string City { get; set; }
        public string State { get; set; }
        [Aliases("PopularMovie")]
        public InnerAliases Movie { get; set; }

        [Aliases("PopularMediaType")]
        public MediaType Popular { get; set; }

        [Aliases("MoviesByYear")]
        public Dictionary<string, MovieAliase> YearlyMovies { get; set; }
    }

    public sealed class InnerAliases
    {
        [Aliases("Media")]
        public MediaContainer Container { get; set; }
        public string Title { get; set; }
    }

    [Aliases("Movies_Aliase")]
    public sealed class MovieAliase
    {
        [Aliases("DateCreated")]
        public long Dated { get; set; }
        [Aliases("ReleaseYear")]
        public int Year { get; set; }
        [Aliases("ReleaseMonth")]
        public Month Month { get; set; }
    }
    public class MediaStream
    {
        public string Id { get; set; }

        public string Title { get; set; }
        public MediaType Type { get; set; }
        public MediaContainer Container { get; set; }
        public byte[] Media { get; set; }

    }

    public enum Month
    {
        January,
        February,
        March, 
        April,
        June, 
        July
    }

    public enum MediaType
    {
        Video,
        Audio
    }

    public enum MediaContainer
    {
        Flv,
        Mp3,
        Avi,
        Mp4
    }

    public class SimpleWithStaticFields
    {
        private static readonly string _staticFieldOnInner = "a field";
        public static string StaticFieldOnInner => _staticFieldOnInner;

        public string Name { get; set; }
    }

    public class SimpleWithStaticFieldsAndNestedClass
    {
        private static readonly string _staticField = "a field";
        public static string StaticField => _staticField;

        public long FactTime { get; set; }

        public SimpleWithStaticFields SimpleStaticInner { get; set; }
    }
}

public class ClassWithoutNamespace
{
    public bool ForReal { get; set; }
    public string Comment { get; set; }
    public Book Book { get; set; }
}