namespace ICacheChallenge
{
    public class Node<TNodeKey, TNodeValue>
    {
        public Node<TNodeKey, TNodeValue> Previous { get; set; }
        public Node<TNodeKey, TNodeValue> Next { get; set; }
        public TNodeKey Key { get; set; }
        public TNodeValue Value { get; set; }

        public Node(Node<TNodeKey, TNodeValue> previous, Node<TNodeKey, TNodeValue> next, TNodeKey key, TNodeValue value)
        {
            Previous = previous;
            Next = next;
            Key = key;
            Value = value;
        }
    }
}