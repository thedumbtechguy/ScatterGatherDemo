using System;
using System.Collections.Generic;
using System.Text;

namespace ScatterGatherDemo.Options
{
    public class RedisOptions
    {
        public string ConnectionString { get; set; }
        public int Database { get; set; }
    }
}