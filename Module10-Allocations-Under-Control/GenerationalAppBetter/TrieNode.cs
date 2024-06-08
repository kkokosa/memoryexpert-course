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

        public IDictionary<char, TrieNode<TValue>> Children { get; }

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
    }
}