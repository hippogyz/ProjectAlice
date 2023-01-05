using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectAlice.Runtime.Exception
{
    public class AliceException : System.Exception
    {
        public AliceException(string name) : base(name) { }
    }
}
