using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace UtilityBot
{
    public class MemorySessionStorage : ISessionStorage
    {
        ConcurrentDictionary<long, Session> _sessions;

        public MemorySessionStorage()
        {
            _sessions = new ConcurrentDictionary<long, Session>();
        }

        public Session GetSession(long id)
        {
            if (_sessions.ContainsKey(id))
                return _sessions[id];

            var newSession = new Session(id);
            _sessions.TryAdd(id, newSession);
            return newSession;
        }
    }
}
