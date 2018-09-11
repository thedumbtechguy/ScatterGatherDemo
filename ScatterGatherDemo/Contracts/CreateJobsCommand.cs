using System;
using System.Collections.Generic;
using System.Text;

namespace ScatterGatherDemo.Contracts
{
    public class CreateJobsCommand
    {
        public string CorrelationId { get; set; }
        public string[] Jobs { get; set; }
    }
}