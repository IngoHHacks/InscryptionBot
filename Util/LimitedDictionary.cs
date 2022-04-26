namespace InscryptionBot.Util
{
    public class LimitedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private Queue<TKey> keys;
        private int capacity;

        public LimitedDictionary(int capacity)
        {
            this.keys = new Queue<TKey>(capacity);
            this.capacity = capacity;
        }

        public new void Add(TKey key, TValue value)
        {
            if (Count == capacity)
            {
                var oldestKey = keys.Dequeue();
                Remove(oldestKey);
            }

            base.Add(key, value);
            keys.Enqueue(key);
        }
    }
}
