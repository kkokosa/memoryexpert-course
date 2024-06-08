using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GenerationalApp
{
    internal sealed class TrieNode<TValue>
    {
        public TrieNode([NotNull] char keyElement)
        {
            KeyElement = keyElement;
            Children = new Dictionary<char, TrieNode<TValue>>();
        }


        public char KeyElement { get; }

        public string? Key { get; set; }

        public TValue Value { get; set; }

        public Dictionary<char, TrieNode<TValue>> Children { get; }

        public TrieNode<TValue> Parent { get; set; }

        public IEnumerable<KeyValuePair<string, TValue>> EnumerateChildren()
        {
            foreach (var child in Children)
            {
                if (child.Value.Key is not null)
                {
                    yield return new(child.Value.Key, child.Value.Value);
                }

                foreach (var item in child.Value.EnumerateChildren())
                    yield return item;
            }
        }

        public int Count()
        {
            int result = Key is not null ? 1 : 0;
            foreach (var (_, childNode) in Children)
            {
                if (childNode.Key is not null)
                    result++;
                foreach (var (_, grandchildNode) in childNode.Children)
                    result += grandchildNode.Count();
            }
            return result;
        }
    }
}