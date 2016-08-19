using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleModel.Model
{
    public class Event
    {
        public DateTime Occurrence { get; set; }

        [NotMapped]
        public double Cost { get; set; }
    }
}
