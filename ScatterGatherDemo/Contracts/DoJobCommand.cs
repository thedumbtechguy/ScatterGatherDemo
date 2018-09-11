using System;
using System.Collections.Generic;
using System.Text;

namespace ScatterGatherDemo.Contracts
{
    public class DoJobCommand
    {
        public string CorrelationId { get; set; }
        public string Job { get; set; }
    }
}