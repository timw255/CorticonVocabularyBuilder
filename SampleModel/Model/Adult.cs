using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleModel.Model
{
    public class Adult : TicketHolder
    {
        public List<Kid> Kids { get; set; }
    }
}
