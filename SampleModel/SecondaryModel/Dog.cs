using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleModel.SecondaryModel
{
    [NotMapped]
    public class Dog
    {
        public string Name { get; set; }
    }
}
