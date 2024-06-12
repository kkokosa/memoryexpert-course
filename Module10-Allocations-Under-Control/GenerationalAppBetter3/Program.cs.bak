using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Spectre.Console;

namespace GenerationalApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Trie<int> stringTrie = new();
            HttpClient client = new HttpClient();
            var url = "http://gutendex.com//books?languages=en&mime_type=text%2Fplain";
            int index = 0;
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var mainTask = ctx.AddTask("[green]Processing books [/]");
                    while (true)
                    {
                        HttpResponseMessage response = await client.GetAsync(url);
                        if (!response.IsSuccessStatusCode)
                            break;
                        var page = await response.Content.ReadFromJsonAsync<ResultsPage>();
                        if (page is null)
                            break;
                        mainTask.MaxValue = page.Count;

                        int pageIndex = 1;
                        var pageTask = ctx.AddTask($"[darkgreen]Processing page {pageIndex}[/]");
                        pageTask.MaxValue = page.Results.Length;
                        foreach (var pageResult in page.Results)
                        {
                            if (pageResult.Formats.TryGetValue("text/plain; charset=utf-8", out var bookUrl) &&
                                bookUrl.EndsWith(".txt"))
                            {
                                try
                                {
                                    var result = await client.GetStringAsync(bookUrl);
                                    ProcessResult(result, stringTrie);
                                    index++;
                                    mainTask.Value = index;
                                    pageTask.Value++;
                                    AnsiConsole.MarkupLine(
                                        $"After parsing '{pageResult.Title}' trie size is {stringTrie.Count()}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }
                        }
                        if (page.Next is null)
                            break;
                        url = page.Next;
                        pageIndex++;
                    }
                });
           }

        private static void ProcessResult(string result, Trie<int> stringTrie)
        {
            Span<char> buffer = stackalloc char[128]; // 😇
            var resultSpan = result.AsSpan();
            foreach (var line in resultSpan.EnumerateLines())
            {
                foreach (var wordCandidateRange in line.Split(' '))
                {
                    var wordCandidate = line[wordCandidateRange];
                    if (TryNormalize(wordCandidate, buffer, out var length))
                    {
                        var word = buffer.Slice(0, length);
                        var newValue = 0;
                        if (stringTrie.TryGetItem(word, out var counter))
                            newValue = ++counter;
                        else
                        {
                            var wordToAdd = new string(word);
                            stringTrie.Add(wordToAdd, newValue);
                        }
                    }
                }
            }
        }

        private static char[] trimChars = new char[] { '.', ',', ';', '!', '?', '"', ':', '(', ')', '_', '[', ']' };
        private static bool TryNormalize(ReadOnlySpan<char> word, Span<char> buffer, out int resultLength)
        {
            bool wordStarted = false;
            resultLength = 0;
            int bi = 0;
            for (int i = 0; i < word.Length; i++)
            {
                if (trimChars.Contains(word[i]))
                {
                    if (!wordStarted)
                        continue;
                    else
                        break;
                }
                else
                {
                    if (!char.IsLetter(word[i]))
                        return false;
                    wordStarted = true;
                    buffer[bi++] = char.ToLowerInvariant(word[i]);
                }
            }
            if (wordStarted && bi > 0)
            {
                resultLength = bi;
                return true;
            }
            else
                return false;
        }
    }

    public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
    {
        private readonly ReadOnlySpan<T> _buffer;

        private readonly ReadOnlySpan<T> _separators;
        private readonly T _separator;

        private readonly int _separatorLength;
        private readonly bool _splitOnSingleToken;

        private readonly bool _isInitialized;

        private int _startCurrent;
        private int _endCurrent;
        private int _startNext;

        /// <summary>
        /// Returns an enumerator that allows for iteration over the split span.
        /// </summary>
        /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/> that can be used to iterate over the split span.</returns>
        public SpanSplitEnumerator<T> GetEnumerator() => this;

        /// <summary>
        /// Returns the current element of the enumeration.
        /// </summary>
        /// <returns>Returns a <see cref="System.Range"/> instance that indicates the bounds of the current element withing the source span.</returns>
        public Range Current => new Range(_startCurrent, _endCurrent);

        internal SpanSplitEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separators)
        {
            _isInitialized = true;
            _buffer = span;
            _separators = separators;
            _separator = default!;
            _splitOnSingleToken = false;
            _separatorLength = _separators.Length != 0 ? _separators.Length : 1;
            _startCurrent = 0;
            _endCurrent = 0;
            _startNext = 0;
        }

        internal SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
        {
            _isInitialized = true;
            _buffer = span;
            _separator = separator;
            _separators = default;
            _splitOnSingleToken = true;
            _separatorLength = 1;
            _startCurrent = 0;
            _endCurrent = 0;
            _startNext = 0;
        }

        /// <summary>
        /// Advances the enumerator to the next element of the enumeration.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the enumeration.</returns>
        public bool MoveNext()
        {
            if (!_isInitialized || _startNext > _buffer.Length)
            {
                return false;
            }

            ReadOnlySpan<T> slice = _buffer.Slice(_startNext);
            _startCurrent = _startNext;

            int separatorIndex = _splitOnSingleToken ? slice.IndexOf(_separator) : slice.IndexOf(_separators);
            int elementLength = (separatorIndex != -1 ? separatorIndex : slice.Length);

            _endCurrent = _startCurrent + elementLength;
            _startNext = _endCurrent + _separatorLength;
            return true;
        }
    }

    public static partial class MemoryExtensions
    {
        /// <summary>
        /// Returns a type that allows for enumeration of each element within a split span
        /// using a single space as a separator character.
        /// </summary>
        /// <param name="span">The source span to be enumerated.</param>
        /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span)
            => new SpanSplitEnumerator<char>(span, ' ');

        /// <summary>
        /// Returns a type that allows for enumeration of each element within a split span
        /// using the provided separator character.
        /// </summary>
        /// <param name="span">The source span to be enumerated.</param>
        /// <param name="separator">The separator character to be used to split the provided span.</param>
        /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span, char separator)
            => new SpanSplitEnumerator<char>(span, separator);

        /// <summary>
        /// Returns a type that allows for enumeration of each element within a split span
        /// using the provided separator string.
        /// </summary>
        /// <param name="span">The source span to be enumerated.</param>
        /// <param name="separator">The separator string to be used to split the provided span.</param>
        /// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span, string separator)
            => new SpanSplitEnumerator<char>(span, separator ?? string.Empty);
    }
}
