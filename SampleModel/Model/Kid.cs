using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleModel.Model
{
    public class Kid : TicketHolder
    {
        [Required]
        public Adult Adult { get; set; }

        public ShirtSize ShirtSize { get; set; }
    }
}
