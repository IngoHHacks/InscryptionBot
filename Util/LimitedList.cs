namespace InscryptionBot.Util
{
    public class LimitedList<T> : List<T>
    {
        private int capacity;

        public LimitedList(int capacity)
        {
            this.capacity = capacity;
        }

        public new void Add(T item)
        {
            if (this.Count == capacity)
            {
                RemoveAt(0);
            }
            base.Add(item);
        }
    }
}
