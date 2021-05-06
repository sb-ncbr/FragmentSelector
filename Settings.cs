using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FragmentSelector
{
    class Settings
    {
        public String InputFileName { get; set; }
        public AtomQuery CentralAtomQuery { get; set; }
        public Protein Protein { get; set; }
        public Atom[] CentralAtoms { get; set; }
        public double Radius { get; set; }
        public int MaxExcludable { get; set; } //maximum length of chain fragment, that can be excluded from the selection
        public int N { get; set; } //maximum number of selected atoms
        public bool PrintSteps { get; set; }
        public int NumOfSteps { get { return 5; } }
        public String OutputFileName { get; set; }
        
        private int logMode;
        public int LogMode
        {
            get { return logMode; }
            set { if (LOG_MODES.Contains(value)) logMode = value; else throw new ArgumentOutOfRangeException("LogMode"); }
        }
        public static int[] LOG_MODES
        {
            get { return new int[] { 0, 1, 2 }; }
            // 0 = no logging, 1 = just result, 2 = all
        }

        private int mode;
        public int Mode
        {
            get { return mode; }
            set { if (MODES.Contains(value)) mode = value; else throw new ArgumentOutOfRangeException("Mode"); }
        }
        public static int[] MODES
        {
            get { return new int[] { 1, 2, 3, 4, 5}; }
            // 1 = Select - Exclude short fragments - Add loops - Add ends - Pad to N
            // 2 = Select - Add loops - Exclude short fragments - Add ends - Pad to N
            // 3 = Select - Add loops - Add ends - Exclude short fragments - Pad to N
            // 4 = Select - Add loops - Exclude short fragments - Add loops - Add ends - Pad to N
            // 5 = Select - Add loops - Add ends - Exclude short fragments - Add loops - Add ends - Pad to N
        }

        public bool RunScript { get; set; }

        public bool Any { get; set; }

    }
}
