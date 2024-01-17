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
            /*
            string input = "Warsaw: 10.0\rParis: 13.4\rLondon: 8,0";
            var processor = new Processor();
            for (int i = 0; i < 40; ++i)
            {
                processor.ProcessNormal(input);
            }

            MemoryProfiler.CollectAllocations(true);
            MemoryProfiler.GetSnapshot("Snapshot #0");
            processor.ProcessNormal(input);
            MemoryProfiler.GetSnapshot("Snapshot #1");
            */

            BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmarks>();
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ProcessCity(string city, decimal temperature)
        {
            //Console.WriteLine($"{city} {temperature}");
        }
    }
}
