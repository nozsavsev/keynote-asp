using System.Collections.Concurrent;
using keynote_asp.Models.Transient;

namespace keynote_asp.Services.Transient
{
    public delegate IQueryable<T> QueryCallback<T>(IQueryable<T> query);

    public class TransientIMServiceBase<T>
        where T : TR_BaseEntity, new()
    {
        private static readonly ConcurrentDictionary<string, T> _items = new();

        public static T GetById(string id)
        {
            return _items[id];
        }

 public static T? GetByRoomCode(string roomCode)
        {
            return _items.Values.FirstOrDefault(x => x.RoomCode == roomCode);
        }

        public static T GetOrCreate(string id)
        {
            return _items.GetOrAdd(id, new T());
        }

        public static T AddOrUpdate(T item)
        {
            return _items.AddOrUpdate(item.Identifier, item, (key, oldValue) => item);
        }

        public static void Remove(string id)
        {
            _items.TryRemove(id, out _);
        }

        public static T? QuerySingle(QueryCallback<T> queryCallback)
        {
            return queryCallback(_items.Values.AsQueryable()).FirstOrDefault();
        }

        public static List<T> QueryMany(QueryCallback<T> queryCallback)
        {
            return queryCallback(_items.Values.AsQueryable()).ToList();
        }

        public static void joinRoom(string id, string roomCode)
        {
            var item = GetById(id);
            item.RoomCode = roomCode;
            AddOrUpdate(item);
        }
    }
}
