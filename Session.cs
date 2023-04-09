using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace UtilityBot
{
    public class Session
    {
        private readonly long _id;

        public Session(long id)
        {
            _id = id;
        }

        public string? OperationId { get; set; }
    }
}
