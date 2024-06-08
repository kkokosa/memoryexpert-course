using System;
using System.Collections.Generic;

namespace GenerationalApp
{
    class Trie<TValue>
    {
        private readonly TrieNode<TValue> _root;

        public Trie()
        {
            _root = new TrieNode<TValue>(default);
        }

        public void Add(string key, TValue value)
        {
            var node = _root;
            foreach (var element in key)
            {
                node = AddElement(node, element);
            }
            node.Key = key;
            node.Value = value;
        }

        public bool TryGetItem(ReadOnlySpan<char> key, out TValue value)
        {
            if (TryGetNode(key, out var node))
            {
                if (node is not null && node.Key is not null)
                {
                    value = node.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private bool TryGetNode(ReadOnlySpan<char> key, out TrieNode<TValue>? node)
        {
            var currentNode = _root;
            foreach (var keyElement in key)
            {
                if (!currentNode.Children.TryGetValue(keyElement, out currentNode))
                {
                    node = null;
                    return false;
                }
            }

            node = currentNode;
            return true;
        }

        public int Count()
        {
            int result = 0;
            foreach (var element in _root.Children)
                result += element.Value.Count();
            return result;
        }

        public IEnumerable<KeyValuePair<string, TValue>> EnumerateNodes()
        {
            return _root.EnumerateChildren();
        }

        private TrieNode<TValue> AddElement(TrieNode<TValue> node,
            char keyElement)
        {
            if (!node.Children.TryGetValue(keyElement, out var childNode))
            {
                childNode = new TrieNode< TValue>(keyElement)
                {
                    Parent = node
                };
                node.Children.Add(keyElement, childNode);
            }

            return childNode;
        }
    }
}