namespace lab3;

public class Repository<T>
{
    private readonly List<T> _items = new List<T>();
    public int Size => _items.Count;

    public void Add(T item) => _items.Add(item);
    public void Remove(int index) => _items.RemoveAt(index);
    public void Update(int index, T item) => _items[index] = item;
    public List<T> GetAll() => new List<T>(_items);
    public void Clear() => _items.Clear();
}