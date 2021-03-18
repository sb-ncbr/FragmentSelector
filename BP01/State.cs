using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BP01
{
    class State
    {
        public MultiChainBuilder Builder { get; set; }
        public SortedSet<Chain> Loops { get; set; }
        public SortedSet<Chain> Ends { get; set; }
        public SortedSet<Chain> Isolated { get; set; }
    }
}
