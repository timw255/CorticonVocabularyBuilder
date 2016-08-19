using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleModel.Model
{
    public class Woman : TicketHolder
    {
        [Required]
        public Man Match { get; set; }
    }
}
