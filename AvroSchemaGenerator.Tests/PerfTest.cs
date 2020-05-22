using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace AvroSchemaGenerator.Tests
{
    public class PerfTest
    {
        private readonly ITestOutputHelper _output;
        private readonly Stopwatch _stopwatch;
        public PerfTest(ITestOutputHelper output)
        {
            _stopwatch = new Stopwatch();
            this._output = output;
        }
        [Fact]
        public void TestDictionaryRecursive_1000()
        {
            _stopwatch.Start();
            for (var i = 0; i < 1000;)
            {
                var actual = typeof(DictionaryRecursive).GetSchema();
                ++i;
            }
            _stopwatch.Start();
            _output.WriteLine($"1000 '{nameof(DictionaryRecursive)}' Avro Schema generated in {_stopwatch.Elapsed.Milliseconds} ms");
        }
        [Fact]
        public void TestRecursiveArray_1000()
        {
            _stopwatch.Start();
            for (var i = 0; i < 1000;)
            {
                var actual = typeof(Family).GetSchema();
                ++i;
            }
            _stopwatch.Start();
            _output.WriteLine($"1000 '{nameof(Family)}' Avro Schema generated in {_stopwatch.Elapsed.Milliseconds} ms");
        }
        [Fact]
        public void TestDictionaryRecursiveArray_1000()
        {
            _stopwatch.Start();
            for (var i = 0; i < 1000;)
            {
                var actual = typeof(Dictionary).GetSchema();
                ++i;
            }
            _stopwatch.Start();
            _output.WriteLine($"1000 '{nameof(Dictionary)}' Avro Schema generated in {_stopwatch.Elapsed.Milliseconds} ms");
        }
        [Fact]
        public void TestDictionaryRecursive_10000()
        {
            _stopwatch.Start();
            for (var i = 0; i < 10_000;)
            {
                var actual = typeof(DictionaryRecursive).GetSchema();
                ++i;
            }
            _stopwatch.Start();
            _output.WriteLine($"10,000 '{nameof(DictionaryRecursive)}' Avro Schema generated in {_stopwatch.Elapsed.Milliseconds} ms");
        }
        [Fact]
        public void TestRecursiveArray_10000()
        {
            _stopwatch.Start();
            for (var i = 0; i < 10_000;)
            {
                var actual = typeof(Family).GetSchema();
                ++i;
            }
            _stopwatch.Start();
            _output.WriteLine($"10,000 '{nameof(Family)}' Avro Schema generated in {_stopwatch.Elapsed.Milliseconds} ms");
        }
        [Fact]
        public void TestDictionaryRecursiveArray_10000()
        {
            _stopwatch.Start();
            for (var i = 0; i < 10_000;)
            {
                var actual = typeof(Dictionary).GetSchema();
                ++i;
            }
            _stopwatch.Start();
            _output.WriteLine($"10,000 '{nameof(Dictionary)}' Avro Schema generated in {_stopwatch.Elapsed.Milliseconds} ms");
        }
        [Fact]
        public void TestDictionaryRecursive_100000()
        {
            _stopwatch.Start();
            for (var i = 0; i < 100_000;)
            {
                var actual = typeof(DictionaryRecursive).GetSchema();
                ++i;
            }
            _stopwatch.Start();
            _output.WriteLine($"100,000 '{nameof(DictionaryRecursive)}' Avro Schema generated in {_stopwatch.Elapsed.Milliseconds} ms");
        }
        [Fact]
        public void TestRecursiveArray_100000()
        {
            _stopwatch.Start();
            for (var i = 0; i < 100_000; i++)
            {
                var actual = typeof(Family).GetSchema();
            }
            _stopwatch.Start();
            _output.WriteLine($"100,000 '{nameof(Family)}' Avro Schema generated in {_stopwatch.Elapsed.Milliseconds} ms");
        }
        [Fact]
        public void TestDictionaryRecursiveArray_100000()
        {
            _stopwatch.Start();
            for (var i = 0; i < 100_000;)
            {
                var actual = typeof(Dictionary).GetSchema();
                ++i;
            }
            _stopwatch.Start();
            _output.WriteLine($"100,000 '{nameof(Dictionary)}' Avro Schema generated in {_stopwatch.Elapsed.Milliseconds} ms");
        }
    }
}
