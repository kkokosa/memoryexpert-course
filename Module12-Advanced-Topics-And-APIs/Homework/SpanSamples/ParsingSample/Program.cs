using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using JetBrains.Profiler.Api;
using Console = System.Console;

namespace ConsoleApp2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string input = "Warsaw: 10.0\rParis: 13.4\rLondon: 8,0";
            var processor = new Processor();
            for (int i = 0; i < 40; ++i)
            {
                processor.ProcessNormal(input);
                processor.ProcessSpan(input);
            }

            MemoryProfiler.CollectAllocations(true);
            MemoryProfiler.GetSnapshot("Snapshot #0");
            processor.ProcessNormal(input);
            MemoryProfiler.GetSnapshot("Snapshot #1");
            processor.ProcessSpan(input);
            MemoryProfiler.GetSnapshot("Snapshot #2");

            //BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmarks>();
        }
    }

    [MemoryDiagnoser]
    public class Benchmarks
    {
        string input = "Warsaw: 10.0\rParis: 13.4\rLondon: 8,0";
        Processor processor = new Processor();

        [Benchmark]
        public void Normal()
        {
            processor.ProcessNormal(input);
        }

        [Benchmark]
        public void Span()
        {
            processor.ProcessSpan(input);
        }
    }

    public class Processor
    {
        static Regex s_regex = new Regex(@"(\w+)\s*:\s*(\d+[,.]\d+)", RegexOptions.Compiled);

        public void ProcessNormal(string input)
        {
            foreach (var line in input.Split('\r'))
            {
                var match = s_regex.Match(line);
                var city = match.Groups[1].Value;
                var temperature = match.Groups[2].Value;
                var normalizedTemperature  = temperature.Replace(',', '.');
                var temperatureValue = decimal.Parse(normalizedTemperature);
                ProcessCity(city, temperatureValue);
            }
        }

        public void ProcessSpan(string input)
        {
            var inputSpan = input.AsSpan();
            foreach (var lineSpan in inputSpan.EnumerateLines())
            {
                //foreach (var matchSpan in s_regex.EnumerateMatches(lineSpan))
                //{
                //    matchSpan.Groups[1].Value;
                //}
                Span<Range> ranges = stackalloc Range[2];
                var rangesCount = lineSpan.Split(ranges, ':');
                Debug.Assert(rangesCount == 2);

                var city = lineSpan[ranges[0]];
                var temperature = lineSpan[ranges[1]];

                Span<char> normalizedTemperature = stackalloc char[temperature.Length];
                NormalizeTemperature(temperature, normalizedTemperature);
                var temperatureValue = decimal.Parse(normalizedTemperature);
                ProcessCity(city, temperatureValue);
            }
        }

        private void NormalizeTemperature(ReadOnlySpan<char> temperature, Span<char> normalizedTemperature)
        {
            for (int i = 0; i < temperature.Length; ++i)
            {
                if (temperature[i] == ',')
                {
                    normalizedTemperature[i] = '.';
                }
                else
                {
                    normalizedTemperature[i] = temperature[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ProcessCity(string city, decimal temperature)
        {
            //Console.WriteLine($"{city} {temperature}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ProcessCity(ReadOnlySpan<char> city, decimal temperature)
        {
            //Console.WriteLine($"{city} {temperature}");
        }
    }
}
