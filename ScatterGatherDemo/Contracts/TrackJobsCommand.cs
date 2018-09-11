using System;
using System.Collections.Generic;
using System.Text;

namespace ScatterGatherDemo.Contracts
{
    public class TrackJobsCommand
    {
        public string CorrelationId { get; set; }
    }
}