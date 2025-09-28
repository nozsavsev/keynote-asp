using System.Collections.Concurrent;
using keynote_asp.Helpers;
using keynote_asp.Models.Transient;

namespace keynote_asp.Services.Transient
{
    public delegate IQueryable<T> QueryCallback<T>(IQueryable<T> query);

    public class TransientIMServiceBase<T>
        where T : TR_BaseEntity, new()
    {
        private static readonly ConcurrentDictionary<string, T> _items = new();

        public static T? GetById(string id)
        {
            _items.TryGetValue(id, out var item);
            return item;
        }

        public static T? GetByRoomCode(string roomCode)
        {
            return _items.Values.FirstOrDefault(x => x.RoomCode == roomCode);
        }

        public static T GetOrCreate(string id)
        {
            // First check if entity exists
            if (id != null && _items.TryGetValue(id, out var existingItem))
            {
                return existingItem;
            }

            // If not found, create new entity with constructor-generated ID
            var newItem = new T();
            newItem.Identifier = id ?? SnowflakeGlobal.Generate().ToString();
            return _items.GetOrAdd(newItem.Identifier, newItem);
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
            if (item == null) return; // Item doesn't exist, silently fail
            
            // Check if room exists by looking in RoomService
            var roomExists = RoomService.GetByRoomCode(roomCode) != null;
            if (!roomExists) return; // Room doesn't exist, silently fail
            
            item.RoomCode = roomCode;
            AddOrUpdate(item);
        }
    }
}
