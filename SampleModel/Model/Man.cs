using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleModel.Model
{
    public class Man : TicketHolder
    {
        public Woman Match { get; set; }
    }
}
