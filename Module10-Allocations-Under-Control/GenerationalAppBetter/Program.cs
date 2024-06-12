using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
                        string cacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                            "_cache",
                            Convert.ToBase64String(Encoding.UTF8.GetBytes(url)).Replace("/", "_") + ".json");
                        HttpResponseMessage response;
                        if (File.Exists(cacheFilePath))
                        {
                            var cachedContent = await File.ReadAllTextAsync(cacheFilePath);
                            response = new HttpResponseMessage(HttpStatusCode.OK)
                            {
                                Content = new StringContent(cachedContent, Encoding.UTF8, "application/json")
                            };
                        }
                        else
                        {
                            response = await client.GetAsync(url);
                            if (response.IsSuccessStatusCode)
                            {
                                var content = await response.Content.ReadAsStringAsync();
                                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_cache"));
                                await File.WriteAllTextAsync(cacheFilePath, content);
                            }
                        }

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
                                    string bookCacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                                        "_cache", 
                                        Convert.ToBase64String(Encoding.UTF8.GetBytes(bookUrl)).Replace("/", "_") + ".txt");
                                    string result;
                                    if (File.Exists(bookCacheFilePath))
                                    {
                                        result = await File.ReadAllTextAsync(bookCacheFilePath);
                                    }
                                    else
                                    {
                                        result = await client.GetStringAsync(bookUrl);
                                        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_cache"));
                                        await File.WriteAllTextAsync(bookCacheFilePath, result);
                                    }

                                    var words = result.Split(new[] { ' ', '\r', '\n' },
                                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                    foreach (var word in words)
                                    {
                                        if (TryNormalize(word, out var newWord))
                                        {
                                            var newValue = 0;
                                            if (stringTrie.TryGetItem(newWord, out var counter))
                                                newValue = ++counter;
                                            stringTrie.Add(newWord, newValue);
                                        }
                                    }

                                    index++;
                                    mainTask.Value = index;
                                    pageTask.Value++;
                                    AnsiConsole.MarkupLine(
                                        $"After parsing '{pageResult.Title}' trie size is {stringTrie.EnumerateNodes().Count()}");
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

        private static bool TryNormalize(string word, out string result)
        {
            result = word.ToLowerInvariant()
                .Trim('.', ',', ';', '!', '?', '"', ':', '(', ')', '_', '[', ']');
            if (result.Any(c => !char.IsLetter(c)))
            {
                return false;
            }
            return true;
        }
    }
}
